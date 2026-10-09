namespace Platform.Domain.Entities;

/// <summary>
/// شمارندهٔ محدودیت نرخِ OTP به‌ازای هر شماره — در دیتابیس، نه حافظه.
/// <para>
/// چرا دیتابیس: شمارندهٔ درون‌حافظه (IMemoryCache) با هر restart صفر می‌شد و در
/// استقرار چندنمونه‌ای هر instance سقف جداگانه داشت؛ مهاجم با چرخش نمونه‌ها سقف
/// را دور می‌زد. این رکورد با همان تراکنشِ عملیات خوانده/نوشته می‌شود.
/// </para>
/// </summary>
public class OtpThrottle
{
    /// <summary>کلید: شماره موبایل نرمال‌شده.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>تعداد تأییدهای ناموفق در پنجرهٔ جاری.</summary>
    public int FailedCount { get; set; }

    /// <summary>شروع پنجرهٔ تلاش ناموفق؛ null یعنی هنوز پنجره‌ای باز نشده.</summary>
    public DateTime? FailedWindowStartUtc { get; set; }

    /// <summary>تعداد کدهای تولیدشده در پنجرهٔ جاری.</summary>
    public int GenerationCount { get; set; }

    /// <summary>شروع پنجرهٔ تولید؛ null یعنی هنوز پنجره‌ای باز نشده.</summary>
    public DateTime? GenerationWindowStartUtc { get; set; }
}
