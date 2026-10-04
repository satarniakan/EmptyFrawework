using Microsoft.Extensions.DependencyInjection;
using Platform.Domain.Identity;
using Platform.Infrastructure.Data;
using Platform.Domain.Interfaces;
using Platform.Web.Components.Layout;

namespace Sample.Web;

/// <summary>
/// کاتالوگ مجوزهای پروژهٔ نمونه: فقط دو مجوز عمومی. پروژهٔ واقعی خودش را دارد.
/// </summary>
public sealed class SamplePermissionCatalog : IPermissionCatalog
{
    public const string TasksManage = "tasks.manage";

    public IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(TasksManage, "مدیریت وظایف", "وظایف")
    ];
}

/// <summary>
/// منوی پروژهٔ نمونه. هیچ آدرس یا مجوزی از دامنهٔ دیگری نمی‌شناسد.
/// </summary>
public sealed class SampleNavProvider : INavProvider
{
    public IReadOnlyList<NavItem> GetRootItems() =>
    [
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

        // واحد کار دامنه جای واحد کار پایه ثبت می‌شود — سرویس‌های پایه هر دو را می‌پذیرند
        services.AddScoped<IPlatformUnitOfWork,
            SampleDomain.SampleUnitOfWork>();
        services.AddScoped<SampleDomain.ISampleUnitOfWork,
            SampleDomain.SampleUnitOfWork>();

        services.AddScoped<SampleDomain.IWorkTaskRepository,
            SampleDomain.WorkTaskRepository>();
        services.AddScoped<SampleDomain.IWorkTaskService,
            SampleDomain.WorkTaskService>();

        // کاتالوگ مجوزها و منو: همین دو نقطه‌اند که پایه را به دامنه وصل می‌کنند
        services.AddSingleton<IPermissionCatalog, SamplePermissionCatalog>();
        services.AddSingleton<INavProvider, SampleNavProvider>();

        return services;
    }
}