using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Platform.Application.Queries;
using Platform.Domain.Entities;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;
using Xunit;

namespace Platform.Tests;

/// <summary>
/// صفحه‌بندی باید در دیتابیس انجام شود: تعداد کل درست و فقط ردیف‌های همان صفحه برگردد.
/// </summary>
public class PagedQueryExtensionsTests
{
    private static PlatformDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .ReplaceService<IModelCacheKeyFactory, PlatformModelCacheKeyFactory>()
            .UseInMemoryDatabase($"paged-{Guid.NewGuid()}")
            .Options;

        return new PlatformDbContext(options, Array.Empty<IPlatformModule>());
    }

    private static async Task SeedAsync(PlatformDbContext db, int count)
    {
        for (var i = 1; i <= count; i++)
        {
            db.AuditLogs.Add(new AuditLog($"type-{i}", $"user{i}@test.ir", $"جزئیات {i}")
            {
                OccurredAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear(); // تا کوئری‌ها واقعاً از provider بیایند نه از tracker
    }

    [Fact]
    public async Task SecondPage_ReturnsOnlyItsRows_WithTotalCount()
    {
        await using var db = CreateContext();
        await SeedAsync(db, 25);

        var result = await db.AuditLogs
            .OrderByDescending(a => a.OccurredAt)
            .ToPagedAsync(page: 2, pageSize: 10);

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public async Task LastPage_ReturnsRemainder()
    {
        await using var db = CreateContext();
        await SeedAsync(db, 25);

        var result = await db.AuditLogs.ToPagedAsync(page: 3, pageSize: 10);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task PageBeyondRange_ReturnsEmptyButKeepsRealTotal()
    {
        await using var db = CreateContext();
        await SeedAsync(db, 25);

        var result = await db.AuditLogs.ToPagedAsync(page: 99, pageSize: 10);

        Assert.Empty(result.Items);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task InvalidInput_IsNormalized_InsteadOfThrowing()
    {
        await using var db = CreateContext();
        await SeedAsync(db, 5);

        var result = await db.AuditLogs.ToPagedAsync(page: 0, pageSize: -3);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(5, result.Items.Count);
    }

    [Fact]
    public async Task OversizedPageSize_IsClamped_SoOneParamCannotPullWholeTable()
    {
        await using var db = CreateContext();
        await SeedAsync(db, 30);

        var result = await db.AuditLogs.ToPagedAsync(page: 1, pageSize: 10_000_000);

        Assert.Equal(PagedQueryExtensions.MaxPageSize, result.PageSize);
        Assert.Equal(30, result.TotalCount);
    }

    [Fact]
    public async Task FilteredQuery_CountsAfterFilter_NotBefore()
    {
        await using var db = CreateContext();
        await SeedAsync(db, 30);

        // این تست اصلِ مشکل را می‌خواهد که شمارش (TotalCount) قبل از Skip/Take
        // انجام شود؛ اگر پیش از فیلتر شمارش شود، ۳۰ می‌گشت.
        var filtered = new[] { "type-1", "type-11", "type-21" };
        var result = await db.AuditLogs
            .Where(a => filtered.Contains(a.EventType))
            .ToPagedAsync(page: 1, pageSize: 10);

        // Count باید قبل از Skip/Take باشه: اگر قبل از فیلتر شمارش می‌شد، ۳۰ می‌گشت
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
    }
}
