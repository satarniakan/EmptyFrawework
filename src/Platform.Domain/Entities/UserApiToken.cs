namespace Platform.Domain.Entities;

/// <summary>
/// توکن API بلندمدت برای کلاینت‌های غیرمرورگری (اپ موبایل، اسکریپت‌ها).
/// <para>
/// چرا جدا از کوکی: کوکی به مرورگر و antiforgery گره خورده و برای موبایل مناسب نیست.
/// توکن opaque است (JWT نیست): فقط هش آن ذخیره می‌شود، پس لو رفتن دیتابیس به‌تنهایی
/// ورود نمی‌دهد؛ لغو تکی (revoke) هم بدون لیست‌سیاه ممکن است — چیزی که با JWT سخت است.
/// متن خام توکن فقط لحظهٔ صدور دیده می‌شود و هیچ‌جا ذخیره نمی‌شود.
/// </para>
/// </summary>
public class UserApiToken
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>هش SHA-256 (hex) توکن خام.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>نام دستگاه/کاربرد («گوشی سامسونگ»، «اسکریپت گزارش») — برای مدیریت نشست‌ها.</summary>
    public string? DeviceName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAtUtc { get; set; }
}
