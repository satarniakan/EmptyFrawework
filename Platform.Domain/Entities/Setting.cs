namespace Platform.Domain.Entities;

/// <summary>
/// تنظیم کلید-مقدار قابل‌ویرایش از پنل ادمین (نام سایت، متن پیامک‌ها، سقف‌ها…).
/// چیزهایی که قبلاً hardcode یا نیازمند deploy بودند.
/// </summary>
public class Setting
{
    /// <summary>کلید یکتا، مثل «site:name» یا «sms:otp-template».</summary>
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
