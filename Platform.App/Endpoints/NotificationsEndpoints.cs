// Platform.App/Endpoints/NotificationsEndpoints.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Services;

namespace Platform.App.Endpoints;

/// <summary>
/// Endpoint های سبک JSON برای زنگ اعلان‌ها. عمداً از fetch (درخواست HTTP مستقل)
/// استفاده می‌شود نه سرویس مدار — تا هم‌روندی DbContext با صفحات تعاملی پیش نیاید.
/// </summary>
public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        // مسیرهای کوکیِ POST: ضدجعل الزامی است (توکن در هدر X-XSRF-TOKEN از platformApi.post).
        var cookiePost = new RequireAntiforgeryTokenAttribute();

        app.MapGet("/notifications/recent", async (
            HttpContext httpContext,
            [FromServices] INotificationService notifications) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            return Results.Json(await notifications.GetFeedAsync(userId));
        }).RequireAuthorization();

        app.MapPost("/notifications/{id:int}/read", async (
            HttpContext httpContext,
            [FromServices] INotificationService notifications,
            int id) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            await notifications.MarkReadAsync(id, userId);
            return Results.Ok();
        }).RequireAuthorization().WithMetadata(cookiePost);

        app.MapPost("/notifications/read-all", async (
            HttpContext httpContext,
            [FromServices] INotificationService notifications) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            await notifications.MarkAllReadAsync(userId);
            return Results.Ok();
        }).RequireAuthorization().WithMetadata(cookiePost);

        return app;
    }

    /// <summary>
    /// دوقلوهای موبایل با همان منطق: توکن Bearer + خطای 401 (نه ریدایرکت لاگین).
    /// مسیرهای مرورگر بالا کوکی‌محور می‌مانند تا ریدایرکت به /login حفظ شود.
    /// </summary>
    public static IEndpointRouteBuilder MapNotificationsApiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications");
        group.RequireAuthorization("Api");

        group.MapGet("/recent", async (ClaimsPrincipal user,
            [FromServices] INotificationService notifications) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            return Results.Json(await notifications.GetFeedAsync(userId));
        });

        group.MapPost("/{id:int}/read", async (ClaimsPrincipal user,
            [FromServices] INotificationService notifications, int id) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            await notifications.MarkReadAsync(id, userId);
            return Results.Ok();
        });

        group.MapPost("/read-all", async (ClaimsPrincipal user,
            [FromServices] INotificationService notifications) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            await notifications.MarkAllReadAsync(userId);
            return Results.Ok();
        });

        return app;
    }
}
