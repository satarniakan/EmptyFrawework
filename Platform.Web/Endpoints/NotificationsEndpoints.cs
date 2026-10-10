// Platform.Web/Endpoints/NotificationsEndpoints.cs
using System.Security.Claims;
using Platform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Platform.Web.Endpoints;

/// <summary>
/// Endpoint های سبک JSON برای زنگ اعلان‌ها. عمداً از fetch (درخواست HTTP مستقل)
/// استفاده می‌شود نه سرویس مدار — تا هم‌روندی DbContext با صفحات تعاملی پیش نیاید.
/// </summary>
public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/notifications/recent", [Authorize] async (
            HttpContext httpContext,
            [FromServices] INotificationService notifications) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            return Results.Json(await notifications.GetFeedAsync(userId));
        });

        app.MapPost("/notifications/{id:int}/read", [Authorize] async (
            HttpContext httpContext,
            [FromServices] INotificationService notifications,
            int id) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            await notifications.MarkReadAsync(id, userId);
            return Results.Ok();
        });

        app.MapPost("/notifications/read-all", [Authorize] async (
            HttpContext httpContext,
            [FromServices] INotificationService notifications) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            await notifications.MarkAllReadAsync(userId);
            return Results.Ok();
        });

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
