using Microsoft.Extensions.Caching.Memory;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;

namespace Platform.Application.Services;

/// <summary>
/// خواندن/نوشتن تنظیمات. خواندن از کش حافظه است (تنظیمات پرتکرار ولی کم‌تغییرند)؛
/// نوشتن کش را باطل می‌کند. مقدار پیش‌فرض از کد می‌آید تا بدون سیدینگ هم کار کند.
/// </summary>
public interface ISettingService
{
    Task<string> GetAsync(string key);

    Task SetAsync(string key, string? value);
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

    private readonly ISettingRepository _repository;
    private readonly ISettingCatalog _catalog;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _clock;

    public SettingService(ISettingRepository repository, ISettingCatalog catalog,
        IPlatformUnitOfWork unitOfWork, IMemoryCache cache, TimeProvider? clock = null)
    {
        _repository = repository;
        _catalog = catalog;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<string> GetAsync(string key)
    {
        if (_cache.TryGetValue(CacheKey(key), out string? cached) && cached is not null)
            return cached;

        var setting = await _repository.GetAsync(key);
        var value = string.IsNullOrEmpty(setting?.Value)
            ? DefaultFor(key)
            : setting.Value;

        _cache.Set(CacheKey(key), value, CacheLifetime);
        return value;
    }

    public async Task SetAsync(string key, string? value)
    {
        await _repository.UpsertAsync(new Domain.Entities.Setting
        {
            Key = key,
            Value = value,
            UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime
        });
        await _unitOfWork.CompleteAsync();
        _cache.Remove(CacheKey(key));
    }

    /// <summary>
    /// پیش‌فرض اول از کاتالوگ میزبان (که ماژول‌ها هم می‌توانند به آن اضافه کنند)،
    /// بعد پیش‌فرض‌های خودِ پایه. پس بدون هیچ ردیفی در دیتابیس هم کار می‌کند.
    /// </summary>
    private string DefaultFor(string key) =>
        _catalog.All.FirstOrDefault(d => d.Key == key)?.DefaultValue
        ?? SettingDefaults.Get(key);

    private static string CacheKey(string key) => $"setting:{key}";
}
