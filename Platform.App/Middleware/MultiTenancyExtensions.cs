using System.Security.Claims;

namespace Platform.App.Middleware;

/// <summary>شناسهٔ tenant جاری درخواست (برای scoping داده‌ها).</summary>
public interface ITenantContext
{
    string? TenantId { get; }
}

public sealed class TenantContext : ITenantContext
{
    public string? TenantId { get; set; }
}

/// <summary>
/// تشخیص tenant از هدر <c>X-Tenant-ID</c> یا subdomain. در صورت نبود، null می‌ماند
/// (حالت single-tenant پیش‌فرض).
/// </summary>
public static class MultiTenancyExtensions
{
    public const string HeaderName = "X-Tenant-ID";

    public static IApplicationBuilder UsePlatformMultiTenancy(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var tenantId = context.Request.Headers[HeaderName].ToString();
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                // تلاش برای استخراج از subdomain (مثلاً acme.example.com)
                var host = context.Request.Host.Host;
                var parts = host.Split('.');
                if (parts.Length >= 3)
                {
                    tenantId = parts[0];
                }
            }

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                var ctx = context.RequestServices.GetRequiredService<TenantContext>();
                ctx.TenantId = tenantId;
            }

            await next();
        });
}