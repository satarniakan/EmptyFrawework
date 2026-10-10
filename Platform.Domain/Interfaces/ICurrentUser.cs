namespace Platform.Domain.Interfaces;

/// <summary>
/// کاربر جاریِ درخواست HTTP. پایه برای ثبت خودکار «چه کسی ساخت/ویرایش کرد» روی
/// انتیتی‌های <c>AuditableEntity</c> استفاده‌اش می‌کند.
/// <para>
/// خارج از یک درخواست (کرون، design-time، تست) مقدار <c>null</c> برمی‌گردد؛
/// پس هیچ‌جا نباید بدون بررسی null فرض شود.
/// </para>
/// </summary>
public interface ICurrentUser
{
    /// <summary>شناسهٔ کاربر واردشده یا null اگر درخواست احراز نشده باشد.</summary>
    string? UserId { get; }
}

/// <summary>
/// نسخهٔ بدون کاربر برای design-time (ساخت مایگریشن) و تست‌ها.
/// </summary>
public sealed class NullCurrentUser : ICurrentUser
{
    public static readonly NullCurrentUser Instance = new();

    public string? UserId => null;
}
