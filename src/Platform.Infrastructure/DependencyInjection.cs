using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Repositories;
using Platform.Infrastructure.Services;

namespace Platform.Infrastructure;

public static class DependencyInjection
{
    /// <param name="isDevelopment">
    /// پیش‌فرض false (امن‌ترین حالت). فقط در Development متن پیامک‌های شبیه‌سازی‌شده
    /// (شامل کد OTP) لاگ می‌شود.
    /// </param>
    public static IServiceCollection AddPlatformInfrastructure(
        this IServiceCollection services, IConfiguration configuration, bool isDevelopment = false,
        string? migrationsAssembly = null)
    {
        services.AddDbContext<PlatformDbContext>(opt =>
        {
            // مدلِ این context به ماژول‌های دامنه بستگی دارد؛ بدون این، EF مدلِ هر
            // ماژول‌دار را برای contextهای بدون ماژول هم برمی‌گرداند.
            opt.ReplaceService<IModelCacheKeyFactory, PlatformModelCacheKeyFactory>();
            opt.UseSqlServer(configuration.GetConnectionString("Default"),
                sql =>
                {
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);

                    // مایگریشن‌ها در مونتاژ میزبان‌اند نه پایه، تا اسکیمای دامنهٔ هر
                    // پروژه وارد فریم‌ورک مشترک نشود. میزبان اسم مونتاژ خودش را می‌دهد:
                    // AddPlatformInfrastructure(config, env.IsDevelopment(),
                    //     migrationsAssembly: typeof(Program).Assembly.GetName().Name)
                    if (!string.IsNullOrWhiteSpace(migrationsAssembly))
                        sql.MigrationsAssembly(migrationsAssembly);
                });
        });

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // تنظیمات قفل موقت حساب — AuthService با lockoutOnFailure:true کار می‌کند
            // (در Identity پیش‌فرض FALSE است)؛ بدون این، مهاجم می‌توانست بی‌نهایت رمز
            // روی یک حساب امتحان کند. محدودیت نرخ فقط جلوی انبوه IP را می‌گیرد.
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<PlatformDbContext>()
        .AddDefaultTokenProviders()
        .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>();

        // AddIdentity به‌صورت پیش‌فرض "/Account/Login" را برای صفحهٔ ورود می‌داند،
        // در حالی که صفحهٔ واقعی ورود اینجا "/login" است. بدون این، کاربر لاگ‌اوت‌شده
        // به مسیر اشتباه ریدایرکت می‌شود. (باید بعد از AddIdentity بیاید.)
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/login";
        });

        services.AddScoped<IPlatformUnitOfWork, PlatformUnitOfWork>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IOtpRepository, OtpCodeRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // «چه کسی ساخت/ویرایش کرد» روی همهٔ AuditableEntityها ثبت می‌شود.
        // AddHttpContextAccessor لازم است؛ بیرون از درخواست HTTP مقدار null برمی‌گردد.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<IFileStorage, LocalFileStorage>();

        // منبع زمان مشترک — سرویس‌ها و DbContext همگی از همین می‌خوانند تا در تست قابل ثابت‌کردن باشد
        services.TryAddSingleton(TimeProvider.System);

        // پیامک: بر اساس «Sms:Provider» — Kavenegar واقعی یا Fake.
        // FakeSmsSender فقط در Development متن پیامک (شامل کد OTP) را لاگ می‌کند.
        services.AddHttpClient();
        services.AddScoped<ISmsSender>(sp =>
        {
            if (string.Equals(configuration["Sms:Provider"], "Kavenegar", StringComparison.OrdinalIgnoreCase))
            {
                var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("kavenegar");
                return new KavenegarSmsSender(
                    http,
                    configuration["Sms:Kavenegar:ApiKey"] ?? string.Empty,
                    configuration["Sms:Kavenegar:Sender"],
                    sp.GetRequiredService<ILogger<KavenegarSmsSender>>());
            }

            return new FakeSmsSender(
                sp.GetRequiredService<ILogger<FakeSmsSender>>(), logMessageBody: isDevelopment);
        });

        // ایمیل تراکنشی — SMTP از «Email:Smtp:*»؛ Host خالی یعنی ارسال با خطای روشن در Outbox ثبت می‌شود
        services.AddScoped<IEmailSender>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new SmtpEmailSender(
                config["Email:Smtp:Host"] ?? string.Empty,
                config.GetValue("Email:Smtp:Port", 587),
                config["Email:Smtp:Username"],
                config["Email:Smtp:Password"],
                config["Email:Smtp:FromAddress"] ?? "no-reply@localhost",
                config["Email:Smtp:FromName"] ?? "سامانه");
        });

        return services;
    }

    /// <summary>
    /// ساخت نقش‌های پایه و همگام‌سازی مجوزهای ادمین. باید بعد از
    /// <c>app.Build()</c>، و پس از مایگریشن دیتابیس و قبل از <c>app.Run()</c> صدا زده شود.
    /// </summary>
    public static async Task InitializePlatformAsync(
        this IServiceProvider services, IReadOnlyList<string>? extraRoles = null)
    {
        await RoleSeeder.SeedAsync(services, extraRoles);
    }
}