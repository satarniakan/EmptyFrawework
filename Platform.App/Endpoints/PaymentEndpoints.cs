using System.Security.Claims;
using Platform.Application.DTOs;
using Platform.Application.Services;
using Platform.Domain.Entities;

namespace Platform.App.Endpoints;

/// <summary>
/// پرداخت: شروع (کاربر واردشده، کوکی یا توکن) و برگشت از درگاه (بدون احراز هویت —
/// درگاه، مرورگرِ کاربرِ ناشناس را برمی‌گرداند و رکورد با Authority پیدا می‌شود).
/// </summary>
public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/payments", async (
            HttpContext http,
            ClaimsPrincipal user,
            IPaymentService payments,
            StartPaymentDto dto) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();

            // CallbackUrl فقط خالی (callback همین میزبان) یا relative/same-host است؛
            // وگرنه مهاجم می‌توانست برگشت درگاه را به سایت خودش بفرستد.
            string callbackUrl;
            if (string.IsNullOrWhiteSpace(dto.CallbackUrl))
            {
                callbackUrl = $"{http.Request.Scheme}://{http.Request.Host}/payments/callback";
            }
            else if (!TryResolveCallbackUrl(http, dto.CallbackUrl, out callbackUrl!))
            {
                return ApiProblem.BadRequest("آدرس بازگشت معتبر نیست.");
            }

            try
            {
                var (paymentId, paymentUrl) = await payments.CreateAsync(
                    userId, dto.AmountTomans, dto.Description, callbackUrl);
                return Results.Ok(new { paymentId, paymentUrl });
            }
            catch (Platform.Domain.Exceptions.BusinessRuleException ex)
            {
                return ApiProblem.BadRequest(ex.Message);
            }
        }).RequireAuthorization("App").RequireRateLimiting("payment-start");

        app.MapGet("/payments/callback", async (
            string? authority, string? status, IPaymentService payments) =>
        {
            if (string.IsNullOrWhiteSpace(authority))
                return Results.Redirect("/payment-result?ok=false");

            var payment = await payments.HandleCallbackAsync(authority, status);
            var ok = payment.Status == PaymentStatus.Paid;
            return Results.Redirect(
                $"/payment-result?ok={ok.ToString().ToLowerInvariant()}&ref={payment.RefId}");
        });

        return app;
    }

    /// <summary>
    /// اعتبارسنجی CallbackUrl: فقط relative (مثل <c>/x/callback</c>) یا absoluteِ
    /// same-hostِ همین درخواست. خروجی همیشه absolute است چون درگاه URL کامل می‌خواهد.
    /// </summary>
    private static bool TryResolveCallbackUrl(HttpContext http, string raw, out string resolved)
    {
        resolved = string.Empty;
        var candidate = raw.Trim();
        if (candidate.Length == 0 || candidate.Length > 500)
            return false;

        // relative مثل /pay/cb → با scheme/host همین درخواست کامل می‌شود
        if (candidate.StartsWith('/') && !candidate.StartsWith("//", StringComparison.Ordinal))
        {
            resolved = $"{http.Request.Scheme}://{http.Request.Host}{candidate}";
            return true;
        }

        // absolute فقط اگر هاستش دقیقاً همین میزبان باشد (هم‌خانواده/زیردامنه نه)
        if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            var requestHost = http.Request.Host.Host;
            if (string.Equals(uri.Host, requestHost, StringComparison.OrdinalIgnoreCase))
            {
                resolved = candidate;
                return true;
            }
        }

        return false;
    }
}
