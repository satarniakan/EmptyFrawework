// Platform.Web/Endpoints/AccountEndpoints.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Platform.Application.DTOs;
using Platform.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Platform.Domain.Interfaces;

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
        }).WithMetadata(new RequireAntiforgeryTokenAttribute()).RequireRateLimiting("login");

        app.MapPost("/Account/RequestOtp", async (
            HttpContext httpContext,
            IAuthService authService,
            ICaptchaValidator captcha,
            [FromForm] string phoneNumber,
            [FromForm(Name = "cf-turnstile-response")] string? captchaToken) =>
        {
            try
            {
                if (!await captcha.ValidateAsync(captchaToken,
                        httpContext.Connection.RemoteIpAddress?.ToString()))
                {
                    return Results.Redirect("/login?error=captcha");
                }

                await authService.RequestOtpAsync(phoneNumber);
                return Results.Redirect($"/verify-otp?phone={Uri.EscapeDataString(phoneNumber)}");
            }
            catch (Platform.Domain.Exceptions.BusinessRuleException ex)
            {
                // مثلاً شماره به‌خاطر تلاش‌های ناموفقِ زیاد موقتاً قفل شده است
                return Results.Redirect($"/login?error={Uri.EscapeDataString(ex.Message)}");
            }
        }).WithMetadata(new RequireAntiforgeryTokenAttribute()).RequireRateLimiting("otp-request");

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
        }).WithMetadata(new RequireAntiforgeryTokenAttribute()).RequireRateLimiting("otp-verify");

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
        }).WithMetadata(new RequireAntiforgeryTokenAttribute()).RequireRateLimiting("profile");

        // مرحلهٔ ۱ «فراموشی/تعیین رمز موبایل»: کد پیامکی می‌فرستد و به صفحهٔ تعیین رمز می‌رود.
        app.MapPost("/Account/RequestPasswordReset", async (
            HttpContext httpContext,
            IAuthService authService,
            ICaptchaValidator captcha,
            [FromForm] string phoneNumber,
            [FromForm(Name = "cf-turnstile-response")] string? captchaToken) =>
        {
            try
            {
                if (!await captcha.ValidateAsync(captchaToken,
                        httpContext.Connection.RemoteIpAddress?.ToString()))
                {
                    return Results.Redirect("/reset-password?error=captcha");
                }

                await authService.RequestOtpAsync(phoneNumber);
                return Results.Redirect($"/reset-password?phone={Uri.EscapeDataString(phoneNumber)}");
            }
            catch (Platform.Domain.Exceptions.BusinessRuleException ex)
            {
                return Results.Redirect($"/reset-password?error={Uri.EscapeDataString(ex.Message)}");
            }
        }).WithMetadata(new RequireAntiforgeryTokenAttribute()).RequireRateLimiting("otp-request");

        // مرحلهٔ ۲: کد + رمز جدید را می‌گیرد و رمز را تعیین می‌کند (سرویس، کد را می‌سوزاند).
        app.MapPost("/Account/ResetPassword", async (
            IAuthService authService,
            [FromForm] string phoneNumber,
            [FromForm] string code,
            [FromForm] string newPassword,
            [FromForm] string confirmPassword) =>
        {
            var result = await authService.ResetPasswordWithOtpAsync(
                phoneNumber, code, newPassword, confirmPassword);

            return result.Status switch
            {
                PasswordResetStatus.Success => Results.Redirect("/login-password?reset=success"),
                PasswordResetStatus.InvalidOtp =>
                    Results.Redirect($"/reset-password?phone={Uri.EscapeDataString(phoneNumber)}&error=otp"),
                PasswordResetStatus.UserNotFound =>
                    Results.Redirect($"/reset-password?phone={Uri.EscapeDataString(phoneNumber)}&error=user"),
                PasswordResetStatus.PasswordMismatch =>
                    Results.Redirect($"/reset-password?phone={Uri.EscapeDataString(phoneNumber)}&error=mismatch"),
                _ =>
                    Results.Redirect($"/reset-password?phone={Uri.EscapeDataString(phoneNumber)}&error=failed")
            };
        }).WithMetadata(new RequireAntiforgeryTokenAttribute()).RequireRateLimiting("otp-verify");

        app.MapPost("/logout", async (IAuthService authService) =>
        {
            await authService.LogoutAsync();
            return Results.Redirect("/login");
        }).WithMetadata(new RequireAntiforgeryTokenAttribute());

        return app;
    }
}
