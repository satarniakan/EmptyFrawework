// Platform.Web/Endpoints/AccountEndpoints.cs
using System.Security.Claims;
using Platform.Application.DTOs;
using Platform.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Platform.Web.Endpoints;

/// <summary>
/// Endpointهای احراز هویت. هر کدام فقط IAuthService را صدا می‌زنند، بدون منطق تجاری.
/// (این‌ها Razor Page نیستند چون باید کوکی ست کنند و این کار داخل Blazor Circuit ممکن نیست.)
/// </summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/Account/Login", async (
            IAuthService authService,
            [FromForm] string email,
            [FromForm] string password) =>
        {
            var result = await authService.LoginWithPasswordAsync(email, password);

            return result.Succeeded
                ? Results.Redirect("/")
                : Results.Redirect("/login-password?error=1");
        }).RequireRateLimiting("login");

        app.MapPost("/Account/RequestOtp", async (
            IAuthService authService,
            [FromForm] string phoneNumber) =>
        {
            try
            {
                await authService.RequestOtpAsync(phoneNumber);
                return Results.Redirect($"/verify-otp?phone={Uri.EscapeDataString(phoneNumber)}");
            }
            catch (Platform.Domain.Exceptions.BusinessRuleException ex)
            {
                // مثلاً شماره به‌خاطر تلاش‌های ناموفقِ زیاد موقتاً قفل شده است
                return Results.Redirect($"/login?error={Uri.EscapeDataString(ex.Message)}");
            }
        }).RequireRateLimiting("otp-request");

        app.MapPost("/Account/VerifyOtp", async (
            IAuthService authService,
            [FromForm] string phoneNumber,
            [FromForm] string code) =>
        {
            var result = await authService.VerifyOtpAsync(phoneNumber, code);

            if (!result.Succeeded)
            {
                return Results.Redirect($"/verify-otp?phone={Uri.EscapeDataString(phoneNumber)}&error=1");
            }

            return result.IsNewUser
                ? Results.Redirect("/profile?welcome=true")
                : Results.Redirect("/");
        }).RequireRateLimiting("otp-verify");

        app.MapPost("/Account/UpdateProfile", async (
            HttpContext httpContext,
            IAuthService authService,
            [FromForm] string? fullName,
            [FromForm] string? email,
            [FromForm] string? password,
            [FromForm] string? confirmPassword) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId is null)
            {
                return Results.Redirect("/login");
            }

            var result = await authService.UpdateProfileAsync(
                userId, fullName, email, password, confirmPassword);

            return result.Status switch
            {
                ProfileUpdateStatus.Success => Results.Redirect("/profile?success=1"),
                ProfileUpdateStatus.UserNotFound => Results.Redirect("/login"),
                _ => Results.Redirect($"/profile?error={result.Status}")
            };
        }).RequireRateLimiting("profile");

        app.MapPost("/logout", async (IAuthService authService) =>
        {
            await authService.LogoutAsync();
            return Results.Redirect("/login");
        });

        return app;
    }
}
