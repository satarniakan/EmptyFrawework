namespace Platform.Domain.Identity;

/// <summary>
/// توصیف یک تنظیم شناخته‌شده: کلید، عنوان فارسی، مقدار پیش‌فرض و گروه نمایشی.
/// مثل IPermissionCatalog، پیاده‌سازی‌اش در میزبان است تا پایه هیچ مقدار دامنه‌ای نداشته باشد.
/// </summary>
public record SettingDescriptor(string Key, string Title, string DefaultValue, string Group = "عمومی");

public interface ISettingCatalog
{
    IReadOnlyList<SettingDescriptor> All { get; }
}

/// <summary>کلیدهای تنظیمات خودِ پایه (مقادیر پیش‌فرض در SettingDefaults).</summary>
public static class FrameworkSettingKeys
{
    public const string SiteName = "site:name";
    public const string OtpSmsTemplate = "sms:otp-template";
}

/// <summary>مقادیر پیش‌فرض تنظیمات پایه — وقتی ردیفی در دیتابیس نیست، این‌ها برمی‌گردند.</summary>
public static class SettingDefaults
{
    public static string Get(string key) => key switch
    {
        FrameworkSettingKeys.SiteName => "سامانه",
        FrameworkSettingKeys.OtpSmsTemplate => "کد ورود شما: {Code}",
        _ => string.Empty
    };
}
