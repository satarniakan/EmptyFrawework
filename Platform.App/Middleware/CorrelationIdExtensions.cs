namespace Platform.App.Middleware;

/// <summary>
/// شناسهٔ همبستگی درخواست (correlation id): از هدر <c>X-Correlation-ID</c> کلاینت
/// (یا میزبانِ جلوتر مثل nginx) خوانده می‌شود، وگرنه یک GUID تازه ساخته می‌شود؛
/// در هدر پاسخ برمی‌گردد و در LogContext سریال لاگ می‌نشیند تا «همهٔ لاگهای یک
/// درخواست» با یک جست‌وجوی ساده پیدا شوند.
/// <para>
/// چرا لازم است: با چند پروسه/چند instance، پیگیری یک درخواست خراب از لاگ‌های
/// پراکنده بدون شناسه مشترک تقریباً غیرممکن است.
/// </para>
/// </summary>
public static class CorrelationIdExtensions
{
    public const string HeaderName = "X-Correlation-ID";
    public const string LogPropertyName = "CorrelationId";

    public static IApplicationBuilder UsePlatformCorrelationId(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            // ورودی نامعتبر (کاراکتر کنترلی/خیلی بلند) قبول نمی‌شود تا هدرِ لاگ/پاسخ بشکند
            var incoming = context.Request.Headers[HeaderName].ToString();
            var correlationId = incoming is { Length: > 0 and <= 64 } && IsValidId(incoming)
                ? incoming
                : Guid.NewGuid().ToString("N");

            context.Response.Headers[HeaderName] = correlationId;

            // سریالِ لاگ این درخواست + هر خطایی که به لاگر برسد این شناسه را دارد
            using (Serilog.Context.LogContext.PushProperty(CorrelationIdExtensions.LogPropertyName, correlationId))
            {
                await next();
            }
        });

    private static bool IsValidId(string value) =>
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}