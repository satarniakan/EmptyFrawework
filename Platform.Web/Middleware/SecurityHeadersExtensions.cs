namespace Platform.Web.Middleware;

/// <summary>
/// هدرهای امنیتی پایه روی همهٔ پاسخ‌ها. عمداً فقط سه هدرِ بی‌خطر برای Blazor:
/// <list type="bullet">
/// <item><c>X-Content-Type-Options: nosniff</c> — جلوگیری از اجرای فایلِ آپلودشده به‌جای دانلود (sniffing).</item>
/// <item><c>X-Frame-Options: SAMEORIGIN</c> — صفحات ادمین داخل iframe سایت دیگر باز نمی‌شوند (clickjacking).</item>
/// <item><c>Referrer-Policy: strict-origin-when-cross-origin</c> — مسیرهای داخلی (مثل توکن در query) به سایت بیرونی لو نمی‌رود.</item>
/// </list>
/// CSP کامل عمداً اینجا نیست: با اسکریپت‌های Blazor و ویجت‌های بیرونی (Turnstile)
/// باید قدم‌به‌قدم و با تست جلو رفت، نه یک‌جا.
/// </summary>
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UsePlatformSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            await next();
        });
}
