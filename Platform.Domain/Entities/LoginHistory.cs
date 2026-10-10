namespace Platform.Domain.Entities;

/// <summary>
/// یک ردیف به‌ازای هر ورود (موفق یا ناموفق): چه کسی، چه وقت، از کجا، با چه روشی.
/// برای «خروج از همهٔ دستگاه‌ها»، بررسی نفوذ و پشتیبانی.
/// </summary>
public class LoginHistory
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    /// <summary>شماره/نام‌کاربریِ واردشده حتی اگر کاربر وجود نداشته باشد (برای تشخیص enumeration).</summary>
    public string? UserName { get; set; }

    public bool Succeeded { get; set; }

    /// <summary>روش ورود: Password، Otp، ApiToken، DevLogin، Impersonation.</summary>
    public string Method { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? FailureReason { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
