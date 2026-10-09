using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Application;
using Platform.Infrastructure;
using Platform.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Platform.Web.Authentication;
using Platform.Web.Authorization;
using Platform.Web.Endpoints;
using Platform.Web.Services;

namespace Platform.Web;

/// <summary>
/// متدهای راه‌اندازی مشترک برای هر برنامه‌ای که روی Platform می‌نشیند.
/// هر کدام فقط به پایه اشاره دارند؛ هیچ تنظیم دامنه‌ای اینجا نیست.
/// ترتیب فراخوانی در <c>Program.cs</c> مهم است و در README مستند شده.
/// </summary>
public static class PlatformSetup
{
    /// <summary>
    /// ثبت سرویس‌ها: Application + Infrastructure + پیام‌رسانی + OTP + Outbox.
    /// </summary>
    public static IServiceCollection AddPlatform(this IServiceCollection services,
        IConfiguration configuration, bool isDevelopment, string? migrationsAssembly = null)
    {
        services.AddPlatformApplication();
        services.AddPlatformInfrastructure(configuration, isDevelopment, migrationsAssembly);

        services.AddCascadingAuthenticationState();

        // پایه خودش سرویس‌های مجوزدهی را ثبت می‌کند تا به «تصادفی» بودنِ
        // AddRazorComponents در میزبان وابسته نباشد (وگرنه هر میزبانی که
        // Razor Components اضافه نکند، هنگام UseAuthorization خطا می‌گیرد).
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ApiTokenAuthenticationHandler>(
                ApiTokenAuthenticationHandler.SchemeName, displayName: "API Token", configureOptions: _ => { });

        services.AddAuthorizationBuilder()
            // endpointهای API موبایل: فقط توکن، نه کوکی
            .AddPolicy("Api", policy => policy
                .AddAuthenticationSchemes(ApiTokenAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser());

        // سیاست‌ساز خودکار: هر کلید IPermissionCatalog خودش یک policy است.
        // باید بعد از AddAuthorization بیاید تا جایگزین provider پیش‌فرض شود.
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        services.AddScoped<ToastService>();
        services.AddHostedService<OtpCleanupService>();
        services.AddHostedService<OutboxProcessor>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // درخواست‌های کد پیامکی: حداکثر ۳ در هر ۵ دقیقه از هر IP.
            // (سقف دقیق‌ترِ «به‌ازای هر شماره» در خود OtpService است، چون HTTP فقط IP را می‌بیند.)
            options.AddPolicy("otp-request", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    }));

            // حدس کد: حداکثر ۸ تلاش در هر ۵ دقیقه.
            options.AddPolicy("otp-verify", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 8,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    }));

            // ورود با رمز: حداکثر ۱۰ تلاش در هر ۱۵ دقیقه برای جلوگیری از حدس زدن رمز.
            options.AddPolicy("login", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    }));

            // تکمیل پروفایل/گذاشتن رمز — عملیات گرانِ bcrypt است و قبلاً هیچ محدودیتی نداشت،
            // پس یک اسکریپت می‌توانست با درخواست‌های مکرر، CPU سرور را بسوزاند.
            options.AddPolicy("profile", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    /// <summary>
    /// مسیریابی پیش‌فرض پایه: احراز هویت + اعلان‌ها. مسیریابی دامنه بعد از این می‌آید.
    /// </summary>
    public static IEndpointRouteBuilder MapPlatform(this IEndpointRouteBuilder app)
    {
        app.MapAccountEndpoints();
        app.MapNotificationsEndpoints();
        app.MapAuthApiEndpoints();
        return app;
    }

    /// <summary>
    /// تنظیمات پایه که باید در <c>Program.cs</c> میزبان اعمال شود، به‌ترتیب:
    /// هدرهای forwarded، فرهنگ فارسی، مدیریت خطا، middlewareهای امنیتی.
    /// </summary>
    public static RequestLocalizationOptions PersianLocalization() => new()
    {
        DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("fa-IR"),
        SupportedCultures = [new CultureInfo("fa-IR")],
        SupportedUICultures = [new CultureInfo("fa-IR")]
    };
}