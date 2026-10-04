using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Services;

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

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IOutboxService, OutboxService>();

        return services;
    }
}