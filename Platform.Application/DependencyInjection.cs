using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Platform.Application.Services;
using Platform.Domain.Identity;

namespace Platform.Application;

public static class DependencyInjection
{
    /// <summary>
    /// سرویس‌های پایه. هیچ ماژول دامنه‌ای اینجا ثبت نمی‌شود؛ پروژهٔ مصرف‌کننده
    /// متدهای خودش را بعد از این فراخوانی می‌زند.
    /// </summary>
    public static IServiceCollection AddPlatformApplication(this IServiceCollection services)
    {
        // شمارندهٔ تلاش ناموفقِ OTP (و هر حافظهٔ موقت دیگر) — سرویس‌های اسکوپ‌دار به آن تزریق می‌شوند
        services.AddMemoryCache();

        // کاتالوگ پیش‌فرض تنظیمات: میزبان با AddSingleton خودش جایگزینش می‌کند
        services.TryAddSingleton<ISettingCatalog, EmptySettingCatalog>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IUserImportService, UserImportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IApiTokenService, ApiTokenService>();
        services.AddScoped<ILoginHistoryService, LoginHistoryService>();
        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<INumberSeries, NumberSeries>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<IImpersonationService, ImpersonationService>();

        return services;
    }
}