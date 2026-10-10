using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Platform.Application.Errors;

namespace Platform.Web.Middleware;

/// <summary>پیام و کد وضعیتِ هر نوع خطا — جدا شده تا قابل تست باشد.</summary>
/// <param name="StatusCode">کد وضعیت HTTP.</param>
/// <param name="Message">پیام فارسیِ امن که به کاربر نشان داده می‌شود؛ جزئیات فنی هرگز نیست.</param>
/// <param name="IsExpected">خطای قابل‌انتظار (قاعدهٔ کسب‌وکار/داده) است نه نقص برنامه.</param>
public record ExceptionTranslation(int StatusCode, string Message, bool IsExpected);

/// <summary>
/// مدیریت خطای سراسری سامانه: همهٔ استثناهای رخ‌داده در پایپ‌لاین به یک کد وضعیت و
/// یک پیام فارسیِ امن نگاشت می‌شوند؛ جزئیات فنی فقط در لاگ می‌رود.
/// <para>
/// چرا مرکزی: بدون این، هر endpoint و هر صفحه باید خودش <c>try/catch</c> بنویسد و
/// پیام‌ها پراکنده و ناهماهنگ می‌شوند. سرویس‌ها فقط <c>throw</c> می‌کنند، اینجا یک‌جا ترجمه می‌شود.
/// </para>
/// <para>
/// برای صفحات HTML هدایت به صفحهٔ خطای پایه انجام می‌شود؛ برای endpointهای JSON و
/// <c>fetch</c>ها یک بدنهٔ JSON با همان پیام فارسی برمی‌گردد.
/// </para>
/// </summary>
public static class PlatformExceptionHandler
{
    /// <summary>پیام عمومی برای خطاهای غیرمنتظره — جزئیات داخل آن نمی‌آید.</summary>
    public const string UnexpectedMessage = ExceptionTranslator.UnexpectedMessage;

    /// <summary>
    /// نگاشت استثنا به کد وضعیت و پیام. منطق در <see cref="ExceptionTranslator"/> است تا
    /// کامپوننت‌های Blazor هم همان پیام را نشان دهند؛ اینجا فقط رکورد خودش را می‌سازد.
    /// </summary>
    public static ExceptionTranslation Translate(Exception? exception)
    {
        var translation = ExceptionTranslator.Translate(exception);
        return new(translation.StatusCode, translation.Message, translation.IsExpected);
    }

    /// <summary>
    /// ثبت مدیریت خطای سراسری. باید اولِ پایپ‌لاین باشد تا همهٔ middlewareها را پوشش دهد.
    /// </summary>
    public static IApplicationBuilder UsePlatformExceptionHandler(this IApplicationBuilder app)
        => app.UseExceptionHandler(handler => handler.Run(async context =>
        {
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var translation = Translate(exception);

            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Platform.ExceptionHandler");

            var path = context.Request.Path.Value ?? "/";

            if (translation.IsExpected)
            {
                // خطای قابل‌انتظار (قانون کسب‌وکار) فقط Warning است؛ stack در سطح Debug
                logger.LogWarning(exception, "{Status} در {Path}: {Message}",
                    translation.StatusCode, path, translation.Message);
            }
            else
            {
                logger.LogError(exception, "خطای پردازش‌نشده در {Path}", path);
            }

            context.Response.StatusCode = translation.StatusCode;

            // صفحهٔ HTML ← هدایت به صفحهٔ خطای پایه (پیام فارسی همان‌جا نمایش داده می‌شود)
            if (WantsHtml(context.Request))
            {
                var encoded = Uri.EscapeDataString(translation.Message);
                context.Response.Redirect($"/Error?code={translation.StatusCode}&message={encoded}");
                return;
            }

            // endpoint JSON / fetch ← همان پیام با کد وضعیت
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                status = translation.StatusCode,
                message = translation.Message,
                // فقط در Development: جزئیات فنی برای دیباگ؛ هرگز به Production نمی‌رسد
                detail = context.RequestServices.GetRequiredService<IWebHostEnvironment>()
                             .IsDevelopment() && !translation.IsExpected
                    ? exception?.ToString()
                    : null
            });
        }));

    /// <summary>
    /// آیا این درخواست از صفحهٔ HTML است؟ آغازگر پیش‌فرض مرورگر <c>*/*</c> است،
    /// پس فقط وقتی صریحاً <c>text/html</c> خواسته شده یا Accept ندارد، صفحه فرض می‌شود.
    /// </summary>
    private static bool WantsHtml(HttpRequest request)
    {
        var accept = request.Headers.Accept.ToString();

        if (accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.IsNullOrWhiteSpace(accept)) return true;

        return request.Path.StartsWithSegments("/Error");
    }
}
