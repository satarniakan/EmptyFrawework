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
/// منوی سامانه.
/// </summary>
public sealed class SampleNavProvider : INavProvider
{
    public IReadOnlyList<NavItem> GetRootItems() =>
    [
        NavItem.Group("جلسات", "bi-calendar3",
            NavItem.Link("جلسات من", "my-meetings", "bi-calendar2-check"),
            NavItem.Link("مدیریت جلسات", "meetings", "bi-calendar2-week", Meetings.MeetingPermissions.Manage),
            NavItem.Link("گزارش جلسات", "meetings/report", "bi-bar-chart", Meetings.MeetingPermissions.Manage),
            NavItem.Link("گزارش حضور", "meetings/attendance-report", "bi-people", Meetings.MeetingPermissions.Manage)),
        NavItem.Group("مدیریت", "bi-shield-lock",
            NavItem.Link("کاربران", "admin/users", "bi-people", Roles.Admin),
            NavItem.Link("مجوز نقش‌ها", "admin/roles", "bi-key", Roles.Admin),
            NavItem.Link("اعلان سراسری", "admin/broadcast", "bi-megaphone", Roles.Admin)),
        NavItem.Group("سیستم", "bi-gear",
            NavItem.Link("گزارش رویدادها", "audit-logs", "bi-journal-text", Roles.Admin),
            NavItem.Link("اعلان‌ها", "notifications", "bi-bell"),
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