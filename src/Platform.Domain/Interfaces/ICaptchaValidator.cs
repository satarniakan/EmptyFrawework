namespace Platform.Domain.Interfaces;

/// <summary>
/// اعتبارسنجی کپچا روی endpointهای عمومی (درخواست OTP). وقتی در تنظیمات سرویسی
/// انتخاب نشده، پیاده‌سازیِ غیرفعال همیشه موفق برمی‌گرداند تا محیط توسعه ساده بماند؛
/// در Production باید Turnstile (یا مشابه) با SiteKey/SecretKey تنظیم شود.
/// </summary>
public interface ICaptchaValidator
{
    /// <summary>نام پیاده‌سازی برای لاگ (مثل «turnstile»، «fake»، «none»).</summary>
    string Name { get; }

    Task<bool> ValidateAsync(string? token, string? remoteIp,
        CancellationToken cancellationToken = default);
}
