using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Domain.Identity;
using Platform.Infrastructure.Data;
using Platform.Domain.Interfaces;
using Platform.Web.Components.Layout;

namespace Sample.Web;

/// <summary>
/// کاتالوگ مجوزهای پروژهٔ نمونه. پروژهٔ واقعی خودش را دارد.
/// </summary>
public sealed class SamplePermissionCatalog : IPermissionCatalog
{
    public const string TasksManage = "tasks.manage";

    public IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(TasksManage, "مدیریت وظایف", "وظایف"),
        new(Meetings.MeetingPermissions.Manage, "مدیریت جلسات", "جلسات")
    ];
}

/// <summary>
/// منوی پروژهٔ نمونه. هیچ آدرس یا مجوزی از دامنهٔ دیگری نمی‌شناسد.
/// </summary>
public sealed class SampleNavProvider : INavProvider
{
    public IReadOnlyList<NavItem> GetRootItems() =>
    [
        NavItem.Group("جلسات", "bi-calendar3",
            NavItem.Link("جلسات من", "my-meetings", "bi-calendar2-check"),
            NavItem.Link("مدیریت جلسات", "meetings", "bi-calendar2-week", Meetings.MeetingPermissions.Manage)),
        NavItem.Group("وظایف", "bi-list-check",
            NavItem.Link("وظایف من", "tasks", "bi-check2-square", SamplePermissionCatalog.TasksManage)),
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
/// ثبت سرویس‌های دامنهٔ نمونه.
/// </summary>
public static class SampleSetup
{
    public static IServiceCollection AddSampleModule(this IServiceCollection services)
    {
        // ماژول EF: پیکربندی مدل «وظایف» به DbContext پایه تزریق می‌شود
        services.AddSingleton<IPlatformModule,
            SampleDomain.SampleModule>();

        // واحد کار دامنه: یک نمونه در هر اسکوپ برای هر دو قرارداد، تا سرویس‌های پایه
        // و سرویس دامنه همیشه یک شیء واحد ببینند (ثبتِ دوبارهٔ تایپهای مجزا دو شیء می‌ساخت).
        services.AddScoped<SampleDomain.SampleUnitOfWork>();
        services.AddScoped<IPlatformUnitOfWork>(sp => sp.GetRequiredService<SampleDomain.SampleUnitOfWork>());
        services.AddScoped<SampleDomain.ISampleUnitOfWork>(sp => sp.GetRequiredService<SampleDomain.SampleUnitOfWork>());

        services.AddScoped<SampleDomain.IWorkTaskRepository,
            SampleDomain.WorkTaskRepository>();
        services.AddScoped<SampleDomain.IWorkTaskService,
            SampleDomain.WorkTaskService>();

        // کاتالوگ مجوزها و منو: همین دو نقطه‌اند که پایه را به دامنه وصل می‌کنند
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