using Microsoft.AspNetCore.Mvc;

namespace Platform.App.Endpoints;

/// <summary>
/// قرارداد خطای یکدست API (همهٔ endpointهای /api/v1): یک بدنهٔ ProblemDetails فارسی
/// با <c>status</c>، <c>title</c> (خلاصهٔ فارسی) و <c>detail</c> (پیام قابل نمایش).
/// <para>
/// چرا مرکزی: قبلاً هر endpoint یک شکل پاسخ می‌داد (<c>{message}</c> در بعضی، redirect
/// در بعضی) و کلاینت موبایل مجبور به پارس چند قالب بود. اینجا فقط از این متد استفاده
/// کن تا همهٔ خطاها یک شکل و قابل پیش‌بینی باشند (RFC 9457 با متن فارسی).
/// </para>
/// <example>
/// <code>
/// return ApiProblem.BadRequest("شماره موبایل معتبر نیست.");
/// return ApiProblem.Unauthorized("توکن نامعتبر است.");
/// </code>
/// </example>
/// </summary>
public static class ApiProblem
{
    /// <summary>بدنهٔ استاندارد برای خطای ۴۰۰ — رایج‌ترین حالت (ورودی نامعتبر).</summary>
    public static IResult BadRequest(string detail) => Problem(400, "درخواست نامعتبر است", detail);

    /// <summary>بدنهٔ استاندارد برای خطای ۴۰۱ — عدم احراز هویت/توکن نامعتبر.</summary>
    public static IResult Unauthorized(string detail = "احراز هویت لازم است.") =>
        Problem(401, "احراز هویت ناموفق", detail);

    /// <summary>بدنهٔ استاندارد برای خطای ۴۰۳ — مجوز کافی نیست.</summary>
    public static IResult Forbidden(string detail = "مجوز کافی ندارید.") =>
        Problem(403, "دسترسی ممنوع", detail);

    /// <summary>بدنهٔ استاندارد برای خطای ۴۰۴ — منبع یافت نشد.</summary>
    public static IResult NotFound(string detail = "مورد درخواستی یافت نشد.") =>
        Problem(404, "یافت نشد", detail);

    /// <summary>بدنهٔ استاندارد برای ۴۲۹ — سقف نرخ درخواست.</summary>
    public static IResult TooManyRequests(string detail = "تعداد درخواست‌ها بیش از حد مجاز است؛ کمی بعد تلاش کنید.") =>
        Problem(429, "درخواست زیاد", detail);

    /// <summary>بدنهٔ استاندارد برای ۵۰۰ — خطای غیرمنتظره (جزئیات فقط در لاگ).</summary>
    public static IResult InternalServerError(string detail = "خطای غیرمنتظره‌ای رخ داد.") =>
        Problem(500, "خطای سرور", detail);

    /// <summary>ساخت ProblemDetails فارسی با کد وضعیت دلخواه.</summary>
    public static IResult Problem(int status, string title, string detail) =>
        Results.Problem(
            title: title,
            detail: detail,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["status"] = status });
}