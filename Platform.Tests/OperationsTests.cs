using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Platform.Application.DTOs;
using Platform.Application.Services;
using Platform.Domain.Entities;
using Platform.Domain.Enums;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Repositories;

namespace Platform.Tests;

/// <summary>
/// موارد جدید: داشبورد، صفحه‌بندی/تلاش‌مجدد صف، فهرست پرداخت‌ها، ایمپورت کاربران.
/// </summary>
public class OperationsTests
{
    private static PlatformDbContext InMemoryContext(string name)
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .ReplaceService<IModelCacheKeyFactory, PlatformModelCacheKeyFactory>()
            .UseInMemoryDatabase($"{name}-{Guid.NewGuid()}")
            .Options;

        return new PlatformDbContext(options, Array.Empty<IPlatformModule>());
    }

    // ---------- داشبورد ----------

    [Fact]
    public async Task Dashboard_ReturnsPersonalCounts_AndAdminCountsOnlyForAdmin()
    {
        await using var db = InMemoryContext("dashboard");
        var store = new UserStore<ApplicationUser>(db);
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "09120000001" });
        db.Users.Add(new ApplicationUser { Id = "u2", UserName = "09120000002" });
        await db.SaveChangesAsync();

        var userManager = new Mock<UserManager<ApplicationUser>>(
            store, null!, null!, null!, null!, null!, null!, null!, null!);
        // Users واقعیِ store تا CountAsync روی InMemory کار کند
        userManager.Setup(u => u.Users).Returns(store.Users);

        var outbox = new Mock<IOutboxRepository>();
        outbox.Setup(o => o.CountAsync(OutboxStatus.Failed)).ReturnsAsync(3);

        var notifications = new Mock<INotificationService>();
        notifications.Setup(n => n.GetUnreadCountAsync("u1")).ReturnsAsync(2);

        var tokens = new Mock<IApiTokenService>();
        tokens.Setup(t => t.GetActiveAsync("u1"))
            .ReturnsAsync([new UserApiToken { Id = 1, UserId = "u1" }]);

        var service = new DashboardService(
            userManager.Object, outbox.Object, notifications.Object, tokens.Object);

        var adminStats = await service.GetHomeStatsAsync("u1", isAdmin: true);
        Assert.Equal(2, adminStats.UnreadNotifications);
        Assert.Equal(1, adminStats.ActiveTokens);
        // توجه: TotalUsers روی InMemory با CountAsync واقعی حساب می‌شود، نه mock
        Assert.Equal(2, adminStats.TotalUsers);
        Assert.Equal(3, adminStats.FailedOutbox);

        var userStats = await service.GetHomeStatsAsync("u1", isAdmin: false);
        Assert.Null(userStats.TotalUsers);
        Assert.Null(userStats.FailedOutbox);
        Assert.Equal(2, userStats.UnreadNotifications);
    }

    // ---------- صف پیام‌ها ----------

    [Fact]
    public async Task OutboxService_GetPaged_ClampsPageSize()
    {
        var uow = new Mock<IPlatformUnitOfWork>();
        var repo = new Mock<IOutboxRepository>();
        uow.Setup(u => u.Outbox).Returns(repo.Object);
        repo.Setup(r => r.GetPagedAsync(null, OutboxStatus.Failed, 1, 500))
            .ReturnsAsync((Enumerable.Empty<OutboxMessage>(), 0));

        var service = new OutboxService(uow.Object, NullLogger<OutboxService>.Instance);
        var result = await service.GetMessagesPagedAsync(null, OutboxStatus.Failed, 0, 10_000_000);

        Assert.Equal(1, result.Page);
        Assert.Equal(500, result.PageSize);
        repo.Verify(r => r.GetPagedAsync(null, OutboxStatus.Failed, 1, 500), Times.Once);
    }

    [Fact]
    public async Task OutboxService_Retry_DelegatesToRepository()
    {
        var uow = new Mock<IPlatformUnitOfWork>();
        var repo = new Mock<IOutboxRepository>();
        uow.Setup(u => u.Outbox).Returns(repo.Object);
        repo.Setup(r => r.RequeueAsync(7)).ReturnsAsync(true);

        var service = new OutboxService(uow.Object, NullLogger<OutboxService>.Instance);

        Assert.True(await service.RetryAsync(7));
    }

    // ---------- پرداخت‌ها ----------

    [Fact]
    public async Task PaymentService_GetPaged_DelegatesWithClamp()
    {
        var uow = new Mock<IPlatformUnitOfWork>();
        var repo = new Mock<IPaymentRepository>();
        uow.Setup(u => u.Payments).Returns(repo.Object);
        var payments = new List<Payment> { new() { Id = 1, Status = PaymentStatus.Paid } };
        repo.Setup(r => r.GetPagedAsync(PaymentStatus.Paid, 1, 20))
            .ReturnsAsync((payments, 1));

        var service = new PaymentService(Mock.Of<IPaymentGateway>(), uow.Object);
        var result = await service.GetPagedAsync(PaymentStatus.Paid, 1, 20);

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    // ---------- ایمپورت کاربران ----------

    private static MemoryStream Workbook(params string[][] rows)
    {
        var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Sheet1");
            for (var r = 0; r < rows.Length; r++)
                for (var c = 0; c < rows[r].Length; c++)
                    sheet.Cell(r + 1, c + 1).Value = rows[r][c];
            workbook.SaveAs(stream);
        }
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task UserImport_CreatesValidRows_AndReportsRowErrors()
    {
        var users = new Mock<IUserAdminService>();
        users.Setup(u => u.CreateUserAsync(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = new UserImportService(
            users.Object, Mock.Of<IAuditService>(), Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>());

        using var stream = Workbook(
            ["Phone", "FullName", "Email"],
            ["09120000001", "کاربر یک", "u1@test.ir"],
            ["", "بدون شماره", ""],
            ["09120000001", "تکراری", ""],
            ["0999", "نامعتبر", ""]);

        var result = await service.ImportAsync(stream);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(4, result.TotalRows);
        Assert.Equal(3, result.RowErrors.Count);
        // نقش پیش‌فرض «کاربر» به ساخته‌شده داده می‌شود
        users.Verify(u => u.CreateUserAsync(It.Is<CreateUserDto>(d =>
            d.PhoneNumber == "09120000001" && d.RoleNames.Contains(Roles.User))), Times.Once);
    }

    [Fact]
    public async Task UserImport_PersianDigits_AreNormalized()
    {
        var users = new Mock<IUserAdminService>();
        users.Setup(u => u.CreateUserAsync(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = new UserImportService(
            users.Object, Mock.Of<IAuditService>(), Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>());

        using var stream = Workbook(
            ["شماره موبایل", "نام"],
            ["۰۹۱۲۰۰۰۰۰۰۲", "کاربر دو"]);

        var result = await service.ImportAsync(stream);

        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.RowErrors);
        users.Verify(u => u.CreateUserAsync(It.Is<CreateUserDto>(d =>
            d.PhoneNumber == "09120000002")), Times.Once);
    }

    [Fact]
    public async Task UserImport_ServiceFailure_BecomesRowError()
    {
        var users = new Mock<IUserAdminService>();
        users.Setup(u => u.CreateUserAsync(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "تکراری است." }));

        var service = new UserImportService(
            users.Object, Mock.Of<IAuditService>(), Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>());

        using var stream = Workbook(
            ["Phone", "FullName"],
            ["09120000003", "کاربر سه"]);

        var result = await service.ImportAsync(stream);

        Assert.Equal(0, result.ImportedCount);
        var error = Assert.Single(result.RowErrors);
        Assert.Equal(2, error.RowNumber);
        Assert.Contains("تکراری", error.Message);
    }
}
