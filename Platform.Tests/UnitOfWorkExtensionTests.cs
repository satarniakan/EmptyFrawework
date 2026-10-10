using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Platform.Domain.Entities;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Repositories;

namespace Platform.Tests;

/// <summary>
/// نقطهٔ اتصال سوم (واحد کار دامنه): پروژهٔ مصرف‌کننده از
/// <see cref="PlatformUnitOfWork"/> ارث می‌برد و ریپوی خودش را با همان DbContext
/// اضافه می‌کند — بدون ویرایش پایه، با یک تراکنش مشترک.
/// </summary>
public class UnitOfWorkExtensionTests
{
    /// <summary>ریپوی دامنهٔ نمونه: فقط برای اثبات seam.</summary>
    private sealed class NoteRepository(PlatformDbContext context)
    {
        public Task AddAsync(Setting note) => context.Settings.AddAsync(note).AsTask();
        public Task<int> CountAsync() => context.Settings.CountAsync();
    }

    private interface ITestUnitOfWork : IPlatformUnitOfWork
    {
        NoteRepository Notes { get; }
    }

    private sealed class TestUnitOfWork : PlatformUnitOfWork, ITestUnitOfWork
    {
        public TestUnitOfWork(PlatformDbContext context, IServiceProvider services)
            : base(context, services)
        {
            Notes = new NoteRepository(context);
        }

        public NoteRepository Notes { get; }
    }

    [Fact]
    public async Task DerivedUnitOfWork_SharesOneContext_AndCommitsTogether()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .ReplaceService<IModelCacheKeyFactory, PlatformModelCacheKeyFactory>()
            .UseInMemoryDatabase($"uow-{Guid.NewGuid()}")
            .Options;

        await using var context = new PlatformDbContext(options, Array.Empty<IPlatformModule>());

        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton(Mock.Of<IOtpRepository>());
        services.AddSingleton(Mock.Of<IAuditLogRepository>());
        services.AddSingleton(Mock.Of<INotificationRepository>());
        services.AddSingleton(Mock.Of<IOutboxRepository>());
        services.AddSingleton(Mock.Of<IPaymentRepository>());
        await using var provider = services.BuildServiceProvider();

        ITestUnitOfWork uow = new TestUnitOfWork(context, provider);

        await uow.Notes.AddAsync(new Setting { Key = "a", Value = "1", UpdatedAtUtc = DateTime.UtcNow });
        await uow.AuditLogs.AddAsync(new AuditLog("test", null, "جزئیات"));
        await uow.CompleteAsync();

        Assert.Equal(1, await uow.Notes.CountAsync());
    }
}
