using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Domain.Identity;
using Platform.Web.Components.Layout;

namespace Platform.App;

/// <summary>
/// کاتالوگ مجوزهای میزبان نمونه. عمداً خالی است: مجوزها مال دامنه‌اند و هر پروژه
/// مال خودش را اینجا ثبت می‌کند. الگو: <c>new("orders.manage", "مدیریت سفارش‌ها", "سفارش‌ها")</c>.
/// </summary>
public sealed class AppPermissionCatalog : IPermissionCatalog
{
    public IReadOnlyList<PermissionDescriptor> All { get; } = [];
}

/// <summary>
/// کاتالوگ تنظیمات میزبان: کلیدهای پایه + همهٔ تنظیمات اتصال‌ها (پیامک، ایمیل،
/// پرداخت، کپچا، وب‌پوش، توکن). مقادیر محرمانه رمزنگاری‌شده ذخیره می‌شوند.
/// ترتیب مقدار مؤثر هر کلید: ردیف دیتابیس ← appsettings ← پیش‌فرض؛ پس خالی
/// گذاشتن یعنی «از appsettings بخوان».
/// </summary>
public sealed class AppSettingCatalog : ISettingCatalog
{
    public IReadOnlyList<SettingDescriptor> All { get; } =
    [
        new(FrameworkSettingKeys.SiteName, "نام سامانه", SettingDefaults.Get(FrameworkSettingKeys.SiteName), "عمومی"),
        new(FrameworkSettingKeys.OtpSmsTemplate, "قالب پیامک کد ورود ({Code})",
            SettingDefaults.Get(FrameworkSettingKeys.OtpSmsTemplate), "پیامک"),

        new(IntegrationSettingKeys.SmsKavenegarApiKey, "کلید API کاوه‌نگار (Sms:Kavenegar:ApiKey)",
            "", "پیامک", IsSecret: true),
        new(IntegrationSettingKeys.SmsKavenegarSender, "شماره خط ارسال‌کننده (Sms:Kavenegar:Sender)",
            "", "پیامک"),

        new(IntegrationSettingKeys.SmtpHost, "هاست SMTP (Email:Smtp:Host)", "", "ایمیل"),
        new(IntegrationSettingKeys.SmtpPort, "پورت SMTP (پیش‌فرض 587)", "", "ایمیل"),
        new(IntegrationSettingKeys.SmtpUsername, "نام کاربری SMTP", "", "ایمیل"),
        new(IntegrationSettingKeys.SmtpPassword, "رمز SMTP", "", "ایمیل", IsSecret: true),
        new(IntegrationSettingKeys.SmtpFromAddress, "فرستنده (FromAddress)", "", "ایمیل"),
        new(IntegrationSettingKeys.SmtpFromName, "نام فرستنده (FromName)", "", "ایمیل"),

        new(IntegrationSettingKeys.ZarinPalMerchantId, "مرچنت زرین‌پال (Payment:ZarinPal:MerchantId)",
            "", "پرداخت", IsSecret: true),
        new(IntegrationSettingKeys.ZarinPalSandbox, "حالت sandbox زرین‌پال (true/false)", "", "پرداخت"),

        new(IntegrationSettingKeys.TurnstileSecretKey, "کلید محرمانه Turnstile (سرور)",
            "", "کپچا", IsSecret: true),
        new(IntegrationSettingKeys.TurnstileSiteKey, "کلید عمومی Turnstile (ویجت لاگین)",
            "", "کپچا"),

        new(IntegrationSettingKeys.VapidSubject, "آدرس تماس VAPID (مثل mailto:admin@example.com)", "", "وب‌پوش"),
        new(IntegrationSettingKeys.VapidPublicKey, "کلید عمومی VAPID", "", "وب‌پوش"),
        new(IntegrationSettingKeys.VapidPrivateKey, "کلید خصوصی VAPID", "", "وب‌پوش", IsSecret: true),

        new(IntegrationSettingKeys.ApiTokenLifetimeDays, "عمر توکن موبایل به روز (پیش‌فرض ۱۸۰)", "", "توکن")
    ];
}

/// <summary>
/// منوی میزبان نمونه: فقط بخش‌های پایه. آیتم‌های دامنه را پروژه به همین فهرست اضافه می‌کند.
/// </summary>
public sealed class AppNavProvider : INavProvider
{
    public IReadOnlyList<NavItem> GetRootItems() =>
    [
        // مدیریت کاربران، مجوزها و پیام‌رسانی
        NavItem.Group("مدیریت", "bi-shield-lock",
            NavItem.Link("کاربران", "admin/users", "bi-people", Roles.Admin),
            NavItem.Link("مجوز نقش‌ها", "admin/roles", "bi-key", Roles.Admin),
            NavItem.Link("اعلان سراسری", "admin/broadcast", "bi-megaphone", Roles.Admin),
            NavItem.Link("تنظیمات", "admin/settings", "bi-sliders", Roles.Admin)),

        // سیستم — لاگ‌ها و تنظیمات حساب
        NavItem.Group("سیستم", "bi-gear",
            NavItem.Link("گزارش رویدادها", "audit-logs", "bi-journal-text", Roles.Admin),
            NavItem.Link("اعلان‌ها", "notifications", "bi-bell"),
            NavItem.Link("نشست‌های فعال", "sessions", "bi-phone"),
            NavItem.Link("پروفایل", "profile", "bi-person"))
    ];
}

/// <summary>
/// راه‌اندازی میزبان.
/// </summary>
public static class AppSetup
{
    /// <summary>
    /// کاتالوگ مجوزها، کاتالوگ تنظیمات و منو: همین سه نقطه‌اند که پایه به دامنه وصل می‌شوند.
    /// </summary>
    public static IServiceCollection AddAppWeb(this IServiceCollection services)
    {
        services.AddSingleton<IPermissionCatalog, AppPermissionCatalog>();
        services.AddSingleton<ISettingCatalog, AppSettingCatalog>();
        services.AddSingleton<INavProvider, AppNavProvider>();
        return services;
    }

    /// <summary>
    /// حالت توسعه: کاربر «ورود مستقیم» (Dev:AutoLoginPhone) را می‌سازد و ادمین می‌کند تا
    /// مسیر /dev-login بدون OTP کار کند. در Production هرگز فراخوانی نمی‌شود.
    /// </summary>
    public static async Task EnsureDevAdminAsync(this IServiceProvider services)
    {
        var phone = services.GetRequiredService<IConfiguration>()["Dev:AutoLoginPhone"];
        if (string.IsNullOrWhiteSpace(phone)) return;

        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await users.FindByNameAsync(phone);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = phone,
                PhoneNumber = phone,
                FullName = "مدیر سامانه",
                EmailConfirmed = true
            };
            var createResult = await users.CreateAsync(user);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(
                    $"ساخت کاربر توسعه ناموفق بود: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
        }

        if (!await users.IsInRoleAsync(user, Roles.Admin))
        {
            var roleResult = await users.AddToRoleAsync(user, Roles.Admin);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(
                    $"افزودن نقش ادمین به کاربر توسعه ناموفق بود: {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");
        }
    }
}
