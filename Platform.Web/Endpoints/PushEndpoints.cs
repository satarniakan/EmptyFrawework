using System.Security.Claims;
using Platform.Application.Services;
using Platform.Domain.Entities;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;

namespace Platform.Web.Endpoints;

/// <summary>
/// اشتراک وب‌پوش مرورگر: کلید عمومی VAPID، ثبت/حذف اشتراک. احراز هویت کوکی (مرورگر).
/// </summary>
public static class PushEndpoints
{
    public record PushSubscriptionDto(string Endpoint, string P256dh, string Auth);

    public static IEndpointRouteBuilder MapPushEndpoints(this IEndpointRouteBuilder app)
    {
        // منبع کلید عمومی: ابتدا ردیف تنظیم یکپارچه‌سازی (دیتابیس)، سپس appsettings.
        // خواندن از ISettingService (نه IConfiguration خالص) تا کلیدی که ادمین در
        // «تنظیمات» ذخیره کرده هم معتبر باشد. endpoint در scope درخواست HTTP خودش است،
        // پس با DbContext مدار Blazor برخورد هم‌روندی ندارد.
        app.MapGet("/api/v1/push/public-key", async (
            ISettingService settings) =>
        {
            var key = await settings.GetEffectiveAsync(
                IntegrationSettingKeys.VapidPublicKey,
                "Push:Vapid:PublicKey");

            return Results.Ok(new
            {
                publicKey = key,
                enabled = !string.IsNullOrWhiteSpace(key)
            });
        });

        app.MapPost("/api/v1/push/subscriptions", async (
            ClaimsPrincipal user,
            IPushSubscriptionRepository subscriptions,
            IPlatformUnitOfWork unitOfWork,
            PushSubscriptionDto dto) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Endpoint) || dto.Endpoint.Length > 500
                || string.IsNullOrWhiteSpace(dto.P256dh) || string.IsNullOrWhiteSpace(dto.Auth))
            {
                return Results.BadRequest(new { message = "اشتراک نامعتبر است." });
            }

            var existing = await subscriptions.GetByEndpointAsync(userId, dto.Endpoint);
            if (existing is null)
            {
                await subscriptions.AddAsync(new PushSubscription
                {
                    UserId = userId,
                    Endpoint = dto.Endpoint,
                    P256dh = dto.P256dh,
                    Auth = dto.Auth,
                    CreatedAtUtc = DateTime.UtcNow
                });
                await unitOfWork.CompleteAsync();
            }

            return Results.Ok();
        }).RequireAuthorization();

        app.MapDelete("/api/v1/push/subscriptions", async (
            ClaimsPrincipal user,
            IPushSubscriptionRepository subscriptions,
            IPlatformUnitOfWork unitOfWork,
            string endpoint) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();

            var existing = await subscriptions.GetByEndpointAsync(userId, endpoint);
            if (existing is not null)
            {
                await subscriptions.RemoveAsync(existing);
                await unitOfWork.CompleteAsync();
            }

            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }
}
