using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Platform.Application.Services;
using Platform.Domain.Identity;

namespace Platform.Web.Endpoints;

/// <summary>
/// جانشینی ادمین: شروع/پایان «ورود به‌جای کاربر». هر دو endpoint فقط نقش Admin و
/// فقط وقتی «Support:ImpersonationEnabled=true» پاسخ می‌دهند؛ در غیر این صورت 404.
/// <para>
/// نشست جانشین با claim «impersonator» (شناسهٔ ادمین) + «impersonated_name» علامت می‌خورد
/// تا بنر MainLayout نمایش داده شود و عملیات حساس (تعیین رمز، جانشینیِ دوباره) بسته شود.
/// </para>
/// </summary>
public static class ImpersonationEndpoints
{
    public static IEndpointRouteBuilder MapImpersonationEndpoints(this IEndpointRouteBuilder app)
    {
        var adminOnly = new AuthorizeAttribute { Roles = Roles.Admin };

        app.MapPost("/admin/impersonate", async (
            HttpContext http,
            ClaimsPrincipal user,
            UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn,
            IImpersonationService impersonation,
            ILoginHistoryService history,
            IAuditService audit,
            [FromForm] string userId) =>
        {
            if (!impersonation.IsEnabled) return Results.NotFound();
            if (user.HasClaim(c => c.Type == ImpersonationClaims.Impersonator)) return Results.Forbid();

            var adminId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (adminId is null) return Results.Unauthorized();

            var (allowed, reason, displayName) = await impersonation.CanImpersonateAsync(adminId, userId);
            if (!allowed) return Results.BadRequest(new { message = reason });

            var target = await users.FindByIdAsync(userId);
            if (target is null) return Results.NotFound();

            await signIn.SignInWithClaimsAsync(target, isPersistent: false,
            [
                new Claim(ImpersonationClaims.Impersonator, adminId),
                new Claim(ImpersonationClaims.ImpersonatedName, displayName ?? target.UserName ?? userId)
            ]);

            var ip = http.Connection.RemoteIpAddress?.ToString();
            var ua = http.Request.Headers.UserAgent.ToString();
            await history.RecordAsync(target.Id, target.UserName, true, "Impersonation", ip, ua);
            await audit.LogEventAsync("impersonation-start", user.Identity?.Name,
                $"ادمین {adminId} به‌جای کاربر {target.Id} وارد شد.");

            return Results.Redirect("/");
        }).RequireAuthorization(adminOnly);

        app.MapPost("/admin/impersonate/stop", async (
            HttpContext http,
            ClaimsPrincipal user,
            UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn,
            IImpersonationService impersonation,
            ILoginHistoryService history) =>
        {
            var adminId = user.FindFirstValue(ImpersonationClaims.Impersonator);
            if (adminId is null) return Results.BadRequest(new { message = "نشست جانشین نیست." });

            var admin = await users.FindByIdAsync(adminId);
            if (admin is null) return Results.Unauthorized();

            await signIn.SignInAsync(admin, isPersistent: true);

            await history.RecordAsync(admin.Id, admin.UserName, true, "Impersonation",
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString());

            return Results.Redirect("/admin/users");
        }).RequireAuthorization();

        return app;
    }
}
