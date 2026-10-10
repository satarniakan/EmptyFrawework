using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Platform.Application.Services;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Repositories;

namespace Platform.Tests;

/// <summary>
/// تنظیمات: پیش‌فرض بدون ردیف دیتابیس، ذخیره/خواندن، و باطل‌شدن کش پس از Set.
/// </summary>
public class SettingServiceTests
{
    private sealed class TestCatalog : ISettingCatalog
    {
        public IReadOnlyList<SettingDescriptor> All { get; } =
        [
            new("test:greeting", "خوش‌آمد", "سلام پیش‌فرض", "تست")
        ];
    }

    private static (SettingService service, PlatformDbContext db) Build()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .ReplaceService<IModelCacheKeyFactory, PlatformModelCacheKeyFactory>()
            .UseInMemoryDatabase($"settings-{Guid.NewGuid()}")
            .Options;

        var db = new PlatformDbContext(options, Array.Empty<IPlatformModule>());

        var uow = new Mock<IPlatformUnitOfWork>();
        uow.Setup(u => u.CompleteAsync())
            .Callback(() => db.SaveChanges())
            .ReturnsAsync(1);

        var service = new SettingService(
            new SettingRepository(db), new TestCatalog(), uow.Object,
            new MemoryCache(new MemoryCacheOptions()));

        return (service, db);
    }

    [Fact]
    public async Task GetAsync_FrameworkKeyWithoutRow_ReturnsFrameworkDefault()
    {
        var (service, _) = Build();

        Assert.Equal("سامانه", await service.GetAsync(FrameworkSettingKeys.SiteName));
        Assert.Contains("{Code}", await service.GetAsync(FrameworkSettingKeys.OtpSmsTemplate));
    }

    [Fact]
    public async Task GetAsync_ModuleKeyWithoutRow_ReturnsCatalogDefault()
    {
        var (service, _) = Build();

        Assert.Equal("سلام پیش‌فرض", await service.GetAsync("test:greeting"));
    }

    [Fact]
    public async Task SetThenGet_ReturnsSavedValue_AndInvalidatesCache()
    {
        var (service, _) = Build();

        await service.SetAsync("test:greeting", "سلام اول");
        Assert.Equal("سلام اول", await service.GetAsync("test:greeting"));

        await service.SetAsync("test:greeting", "سلام دوم");
        Assert.Equal("سلام دوم", await service.GetAsync("test:greeting"));
    }

    [Fact]
    public async Task SetAsync_EmptyValue_FallsBackToDefault()
    {
        var (service, _) = Build();

        await service.SetAsync("test:greeting", "مقداری");
        await service.SetAsync("test:greeting", "");

        Assert.Equal("سلام پیش‌فرض", await service.GetAsync("test:greeting"));
    }
}
