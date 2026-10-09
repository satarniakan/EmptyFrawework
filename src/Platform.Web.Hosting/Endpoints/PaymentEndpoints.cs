using System.Security.Claims;
using Platform.Application.DTOs;
using Platform.Application.Services;
using Platform.Domain.Entities;

namespace Platform.Web.Endpoints;

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

            var callbackUrl = string.IsNullOrWhiteSpace(dto.CallbackUrl)
                ? $"{http.Request.Scheme}://{http.Request.Host}/payments/callback"
                : dto.CallbackUrl;

            try
            {
                var (paymentId, paymentUrl) = await payments.CreateAsync(
                    userId, dto.AmountTomans, dto.Description, callbackUrl);
                return Results.Ok(new { paymentId, paymentUrl });
            }
            catch (Platform.Domain.Exceptions.BusinessRuleException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).RequireAuthorization();

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
}
