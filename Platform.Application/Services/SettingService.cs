using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;

namespace Platform.Application.Services;

/// <summary>
/// خواندن/نوشتن تنظیمات. خواندن از کش حافظه است (تنظیمات پرتکرار ولی کم‌تغییرند)؛
/// نوشتن کش را باطل می‌کند. مقدار پیش‌فرض از کد می‌آید تا بدون سیدینگ هم کار کند.
/// <para>
/// ترتیب مقدار مؤثر (<see cref="GetEffectiveAsync"/>): ردیف دیتابیس ← appsettings ←
/// پیش‌فرض داخلی. پس ادمین همهٔ مقادیر عملیاتی را از UI عوض می‌کند و appsettings
/// فقط نقش «مقدار اولیه/پشتیبان» را دارد. مقادیر محرمانه (کلید API، رمز) در دیتابیس
/// رمزنگاری‌شده ذخیره می‌شوند.
/// </para>
/// </summary>
public interface ISettingService
{
    Task<string> GetAsync(string key);

    Task SetAsync(string key, string? value);

    /// <summary>
    /// مقدار مؤثر: ردیف دیتابیس (اگر غیرخالی)، وگرنه کلید appsettings، وگرنه پیش‌فرض.
    /// برای تنظیمات اتصال‌ها (API keys و…) که در هر دو جا می‌توانند باشند.
    /// </summary>
    Task<string> GetEffectiveAsync(string key, string? configurationKey = null, string defaultValue = "");

    /// <summary>آیا ردیفی غیرخالی در دیتابیس هست؟ (برای نمایش وضعیت در صفحهٔ ادمین، بدون خواندن مقدار secret)</summary>
    Task<bool> HasValueAsync(string key);

    /// <summary>حذف ردیف دیتابیس — مقدار به appsettings/پیش‌فرض برمی‌گردد.</summary>
    Task ClearAsync(string key);
}

/// <summary>
/// کاتالوگ خالی پیش‌فرض: اگر میزبان <see cref="ISettingCatalog"/> خودش را ثبت نکند،
/// پایه فقط با پیش‌فرض‌های کد کار می‌کند و بالا می‌آید (به‌جای خطای DI هنگام استارتاپ).
/// </summary>
public sealed class EmptySettingCatalog : ISettingCatalog
{
    public IReadOnlyList<SettingDescriptor> All { get; } = [];
}

public class SettingService : ISettingService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    /// <summary>هدف محافظت داده برای مقادیر محرمانه (کلیدها در DataProtection-Keys میزبان‌اند).</summary>
    public const string ProtectorPurpose = "Platform.Settings.Secrets";

    private readonly ISettingRepository _repository;
    private readonly ISettingCatalog _catalog;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IDataProtector? _protector;
    private readonly ILogger<SettingService> _logger;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContext;
    private readonly TimeProvider _clock;

    public SettingService(ISettingRepository repository, ISettingCatalog catalog,
        IPlatformUnitOfWork unitOfWork, IMemoryCache cache, IConfiguration configuration,
        ILogger<SettingService> logger, IAuditService audit, IHttpContextAccessor httpContext,
        TimeProvider? clock = null, IDataProtectionProvider? dataProtection = null)
    {
        _repository = repository;
        _catalog = catalog;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _audit = audit;
        _httpContext = httpContext;
        _clock = clock ?? TimeProvider.System;
        _protector = dataProtection?.CreateProtector(ProtectorPurpose);
    }

    public async Task<string> GetAsync(string key)
    {
        if (_cache.TryGetValue(CacheKey(key), out string? cached) && cached is not null)
            return cached;

        var setting = await _repository.GetAsync(key);
        var value = string.IsNullOrEmpty(setting?.Value)
            ? DefaultFor(key)
            : UnprotectIfNeeded(key, setting.Value);

        _cache.Set(CacheKey(key), value, CacheLifetime);
        return value;
    }

    public async Task<string> GetEffectiveAsync(string key, string? configurationKey = null,
        string defaultValue = "")
    {
        var setting = await _repository.GetAsync(key);
        if (!string.IsNullOrEmpty(setting?.Value))
            return UnprotectIfNeeded(key, setting.Value);

        var configured = configurationKey is null ? null : _configuration[configurationKey];
        if (!string.IsNullOrEmpty(configured))
            return configured;

        return defaultValue;
    }

    public async Task SetAsync(string key, string? value)
    {
        var stored = string.IsNullOrEmpty(value) ? value : ProtectIfNeeded(key, value);

        await _repository.UpsertAsync(new Domain.Entities.Setting
        {
            Key = key,
            Value = stored,
            UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime
        });
        await _unitOfWork.CompleteAsync();
        _cache.Remove(CacheKey(key));

        // فقط کلید ثبت می‌شود، هرگز مقدار (محرمانه‌ها قابل‌بازگشت نیستند)
        await _audit.LogEventAsync("setting.changed", ActorName, $"تنظیم «{key}» تغییر کرد.");
    }

    public async Task<bool> HasValueAsync(string key)
    {
        var setting = await _repository.GetAsync(key);
        return !string.IsNullOrEmpty(setting?.Value);
    }

    public async Task ClearAsync(string key)
    {
        await _repository.DeleteAsync(key);
        await _unitOfWork.CompleteAsync();
        _cache.Remove(CacheKey(key));

        await _audit.LogEventAsync("setting.cleared", ActorName,
            $"تنظیم «{key}» به مقدار appsettings/پیش‌فرض برگشت.");
    }

    private string? ActorName => _httpContext.HttpContext?.User.Identity?.Name;

    /// <summary>
    /// پیش‌فرض اول از کاتالوگ میزبان (که ماژول‌ها هم می‌توانند به آن اضافه کنند)،
    /// بعد پیش‌فرض‌های خودِ پایه. پس بدون هیچ ردیفی در دیتابیس هم کار می‌کند.
    /// </summary>
    private string DefaultFor(string key) =>
        _catalog.All.FirstOrDefault(d => d.Key == key)?.DefaultValue
        ?? SettingDefaults.Get(key);

    private bool IsSecret(string key) =>
        _catalog.All.Any(d => d.Key == key && d.IsSecret);

    private string ProtectIfNeeded(string key, string value)
    {
        if (!IsSecret(key))
            return value;

        if (_protector is null)
        {
            // fail-closed: secret هرگز plaintext ذخیره نمی‌شود
            _logger.LogError("Refusing to store secret {Key} without DataProtection", key);
            throw new InvalidOperationException(
                "ذخیرهٔ مقدار محرمانه بدون DataProtection ممکن نیست.");
        }

        return _protector.Protect(value);
    }

    private string UnprotectIfNeeded(string key, string stored)
    {
        if (!IsSecret(key))
            return stored;

        if (_protector is null)
        {
            _logger.LogError("Cannot read secret {Key} without DataProtection", key);
            throw new InvalidOperationException(
                "خواندن مقدار محرمانه بدون DataProtection ممکن نیست.");
        }

        return _protector.Unprotect(stored);
    }

    private static string CacheKey(string key) => $"setting:{key}";
}
