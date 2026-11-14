namespace Platform.App.Middleware;

/// <summary>
/// هدرهای امنیتی پایه روی همهٔ پاسخ‌ها.
/// CSP به‌صورت Report-Only شروع می‌شود تا بتوان با تست‌های Blazor/Turnstile
/// قدم‌به‌قدم آن را سفت کرد بدون آنکه UI بشکند.
/// </summary>
public static class SecurityHeadersExtensions
{
    /// <summary>
    /// CSP که با Blazor Server و Turnstile سازگار است.
    /// </summary>
    public const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://challenges.cloudflare.com https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' data: https://cdn.jsdelivr.net; " +
        "img-src 'self' data: blob: https:; " +
        "connect-src 'self' ws: wss: https://challenges.cloudflare.com; " +
        "frame-src https://challenges.cloudflare.com; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'self'";

    public static IApplicationBuilder UsePlatformSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            // CSP Enforce: پس از تست‌های مرورگر، Report-Only به Enforce تبدیل شد.
            context.Response.Headers["Content-Security-Policy"] = ContentSecurityPolicy;
            await next();
        });
}
