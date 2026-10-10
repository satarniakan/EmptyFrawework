namespace Platform.Domain.Identity;

/// <summary>
/// کلیدهای استاندارد تنظیمات اتصال‌ها (integration). مقدار مؤثر هر کلید با ترتیب
/// «ردیف دیتابیس ← appsettings ← پیش‌فرض داخلی» خوانده می‌شود، پس ادمین می‌تواند
/// همه را از صفحهٔ تنظیمات عوض کند بدون اینکه appsettings دست بخورد.
/// <para>
/// انتخابِ درگاه (کدام provider) عمداً در appsettings می‌ماند (توپولوژی استقرار است،
/// نه مقدار عملیاتی)؛ کلیدها و مقادیر عملیاتی اینجایند.
/// </para>
/// </summary>
public static class IntegrationSettingKeys
{
    // پیامک (کاوه‌نگار) — appsettings: Sms:Kavenegar:*
    public const string SmsKavenegarApiKey = "integration:sms-kavenegar-apikey";
    public const string SmsKavenegarSender = "integration:sms-kavenegar-sender";

    // ایمیل SMTP — appsettings: Email:Smtp:*
    public const string SmtpHost = "integration:smtp-host";
    public const string SmtpPort = "integration:smtp-port";
    public const string SmtpUsername = "integration:smtp-username";
    public const string SmtpPassword = "integration:smtp-password";
    public const string SmtpFromAddress = "integration:smtp-from-address";
    public const string SmtpFromName = "integration:smtp-from-name";

    // پرداخت (زرین‌پال) — appsettings: Payment:ZarinPal:*
    public const string ZarinPalMerchantId = "integration:zarinpal-merchant-id";
    public const string ZarinPalSandbox = "integration:zarinpal-sandbox";

    // کپچا (Turnstile) — appsettings: Captcha:Turnstile:*
    public const string TurnstileSecretKey = "integration:turnstile-secret-key";
    public const string TurnstileSiteKey = "integration:turnstile-sitekey";

    // وب‌پوش (VAPID) — appsettings: Push:Vapid:*
    public const string VapidSubject = "integration:vapid-subject";
    public const string VapidPublicKey = "integration:vapid-public-key";
    public const string VapidPrivateKey = "integration:vapid-private-key";

    // توکن API — appsettings: ApiTokens:*
    public const string ApiTokenLifetimeDays = "integration:apitoken-lifetime-days";
}
