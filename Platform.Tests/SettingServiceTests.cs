using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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
            new("test:greeting", "خوش‌آمد", "سلام پیش‌فرض", "تست"),
            new("test:secret", "کلید محرمانه", "", "تست", IsSecret: true)
        ];
    }

    private static (SettingService service, PlatformDbContext db) Build(
        Dictionary<string, string?>? configValues = null,
        IDataProtectionProvider? dataProtection = null)
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

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues ?? new Dictionary<string, string?>())
            .Build();

        var service = new SettingService(
            new SettingRepository(db), new TestCatalog(), uow.Object,
            new MemoryCache(new MemoryCacheOptions()), configuration,
            NullLogger<SettingService>.Instance,
            dataProtection: dataProtection);

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

    [Fact]
    public async Task GetEffectiveAsync_PrefersDb_ThenConfig_ThenDefault()
    {
        var (service, _) = Build(new Dictionary<string, string?>
        {
            ["App:Name"] = "از کانفیگ"
        });

        // نه دیتابیس نه کانفیگ → پیش‌فرض
        Assert.Equal("dflt", await service.GetEffectiveAsync("test:missing", "App:Missing", "dflt"));

        // کانفیگ
        Assert.Equal("از کانفیگ", await service.GetEffectiveAsync("test:other", "App:Name", "dflt"));

        // دیتابیس بر کانفیگ مقدم است
        await service.SetAsync("test:other", "از دیتابیس");
        Assert.Equal("از دیتابیس", await service.GetEffectiveAsync("test:other", "App:Name", "dflt"));
    }

    [Fact]
    public async Task SecretValue_IsEncryptedAtRest_AndReadable()
    {
        var dp = new EphemeralDataProtectionProvider();
        var (service, db) = Build(dataProtection: dp);

        await service.SetAsync("test:secret", "s3cr3t-value");

        var row = await db.Settings.SingleAsync(s => s.Key == "test:secret");
        Assert.NotEqual("s3cr3t-value", row.Value); // در دیتابیس plaintext نیست

        Assert.Equal("s3cr3t-value", await service.GetAsync("test:secret"));
        Assert.True(await service.HasValueAsync("test:secret"));
    }

    [Fact]
    public async Task SecretValue_WithoutDataProtection_RefusesToStore()
    {
        var (service, _) = Build(); // بدون DataProtection

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetAsync("test:secret", "s3cr3t-value"));
    }

    [Fact]
    public async Task ClearAsync_RemovesRow_FallsBackToDefault()
    {
        var (service, _) = Build();

        await service.SetAsync("test:greeting", "مقداری");
        Assert.True(await service.HasValueAsync("test:greeting"));

        await service.ClearAsync("test:greeting");

        Assert.False(await service.HasValueAsync("test:greeting"));
        Assert.Equal("سلام پیش‌فرض", await service.GetAsync("test:greeting"));
    }
}
