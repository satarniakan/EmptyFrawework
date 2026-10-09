using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;
using Platform.Web.Components.Layout;

namespace Sample.Web;

/// <summary>
/// کاتالوگ مجوزهای سامانهٔ جلسات.
/// </summary>
public sealed class SamplePermissionCatalog : IPermissionCatalog
{
    public IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(Meetings.MeetingPermissions.Manage, "مدیریت جلسات", "جلسات")
    ];
}

/// <summary>
/// کاتالوگ تنظیمات سامانه: کلیدهای پایه + قالب‌های پیام ماژول جلسات.
/// </summary>
public sealed class SampleSettingCatalog : ISettingCatalog
{
    public IReadOnlyList<SettingDescriptor> All { get; } =
    [
        new(FrameworkSettingKeys.SiteName, "نام سامانه", SettingDefaults.Get(FrameworkSettingKeys.SiteName), "عمومی"),
        new(FrameworkSettingKeys.OtpSmsTemplate, "قالب پیامک کد ورود ({Code})",
            SettingDefaults.Get(FrameworkSettingKeys.OtpSmsTemplate), "پیامک"),
        new(Meetings.MeetingSettingKeys.InvitationSmsTemplate, "قالب پیامک دعوت به جلسه ({Title}، {When}، {Where})",
            Meetings.MeetingSettingKeys.DefaultInvitationSmsTemplate, "جلسات"),
        new(Meetings.MeetingSettingKeys.InvitationEmailSubject, "موضوع ایمیل دعوت به جلسه ({Title})",
            Meetings.MeetingSettingKeys.DefaultInvitationEmailSubject, "جلسات")
    ];
}

/// <summary>
/// منوی سامانه — ترتیب گروه‌ها: عملیات روزمره، گزارش‌ها، مدیریت، سیستم.
/// </summary>
public sealed class SampleNavProvider : INavProvider
{
    public IReadOnlyList<NavItem> GetRootItems() =>
    [
        // عملیات روزمره — پرتکرارترین بخش در اول
        NavItem.Group("جلسات", "bi-calendar3",
            NavItem.Link("جلسات من", "my-meetings", "bi-calendar2-check"),
            NavItem.Link("مدیریت جلسات", "meetings", "bi-calendar2-week", Meetings.MeetingPermissions.Manage)),

        // گزارش‌ها — فقط برای دارندگان مجوز مدیریت جلسات
        NavItem.Group("گزارش‌ها", "bi-bar-chart",
            NavItem.Link("گزارش جلسات", "meetings/report", "bi-file-earmark-text", Meetings.MeetingPermissions.Manage),
            NavItem.Link("گزارش حضور", "meetings/attendance-report", "bi-people", Meetings.MeetingPermissions.Manage)),

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
public static class SampleSetup
{
    /// <summary>
    /// کاتالوگ مجوزها و منو: همین دو نقطه‌اند که پایه به دامنه وصل می‌شوند.
    /// </summary>
    public static IServiceCollection AddSampleWeb(this IServiceCollection services)
    {
        services.AddSingleton<IPermissionCatalog, SamplePermissionCatalog>();
        services.AddSingleton<ISettingCatalog, SampleSettingCatalog>();
        services.AddSingleton<INavProvider, SampleNavProvider>();
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