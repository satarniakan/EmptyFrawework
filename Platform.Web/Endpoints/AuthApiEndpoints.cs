using System.Security.Claims;
using Platform.Application.DTOs;
using Platform.Application.Services;
using Platform.Domain.Exceptions;
using Platform.Domain.Interfaces;

namespace Platform.Web.Endpoints;

/// <summary>
/// API موبایل (نسخه۱): ورود OTP و مدیریت توکن‌ها/نشست‌ها با JSON.
/// احراز هویت با <c>Authorization: Bearer pat_...</c> و policy مخصوص «Api» است، نه کوکی.
/// محدودیت نرخِ همان مسیرهای وب (otp-request / otp-verify) اینجا هم اعمال می‌شود.
/// </summary>
public static class AuthApiEndpoints
{
    public static IEndpointRouteBuilder MapAuthApiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/otp/request", async (HttpContext http, IAuthService auth,
            ICaptchaValidator captcha, OtpRequestApiDto dto) =>
        {
            try
            {
                if (!await captcha.ValidateAsync(dto.CaptchaToken,
                        http.Connection.RemoteIpAddress?.ToString()))
                {
                    return Results.BadRequest(new { message = "اعتبارسنجی امنیتی ناموفق بود." });
                }

                await auth.RequestOtpAsync(dto.PhoneNumber);
                return Results.Ok(new { message = "کد ورود ارسال شد." });
            }
            catch (BusinessRuleException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).RequireRateLimiting("otp-request");

        group.MapPost("/otp/verify", async (
            HttpContext http,
            IApiTokenService tokens,
            ILoginHistoryService history,
            OtpVerifyApiDto dto) =>
        {
            var result = await tokens.VerifyOtpAndIssueTokenAsync(dto.PhoneNumber, dto.Code, dto.DeviceName);

            await history.RecordAsync(
                userId: result.UserId,
                userName: dto.PhoneNumber,
                succeeded: result.Succeeded,
                method: "ApiToken",
                ipAddress: http.Connection.RemoteIpAddress?.ToString(),
                userAgent: http.Request.Headers.UserAgent.ToString(),
                failureReason: result.Succeeded ? null : "invalid-otp");

            return result.Succeeded
                ? Results.Ok(new TokenResponseDto(result.Token!, result.ExpiresAtUtc!.Value))
                : Results.Unauthorized();
        }).RequireRateLimiting("otp-verify");

        group.MapPost("/login", async (
            HttpContext http,
            IApiTokenService tokens,
            ILoginHistoryService history,
            PasswordLoginApiDto dto) =>
        {
            var result = await tokens.LoginWithPasswordAndIssueTokenAsync(
                dto.Username, dto.Password, dto.DeviceName);

            await history.RecordAsync(
                userId: result.UserId,
                userName: dto.Username,
                succeeded: result.Succeeded,
                method: "ApiToken",
                ipAddress: http.Connection.RemoteIpAddress?.ToString(),
                userAgent: http.Request.Headers.UserAgent.ToString(),
                failureReason: result.Succeeded ? null : "invalid-credentials");

            return result.Succeeded
                ? Results.Ok(new TokenResponseDto(result.Token!, result.ExpiresAtUtc!.Value))
                : Results.Unauthorized();
        }).RequireRateLimiting("login");

        group.MapPost("/password/reset", async (
            HttpContext http,
            IAuthService auth,
            ILoginHistoryService history,
            PasswordResetApiDto dto) =>
        {
            var result = await auth.ResetPasswordWithOtpAsync(
                dto.PhoneNumber, dto.Code, dto.NewPassword, dto.ConfirmPassword);

            await history.RecordAsync(
                userId: result.UserId,
                userName: dto.PhoneNumber,
                succeeded: result.Status == PasswordResetStatus.Success,
                method: "ApiToken",
                ipAddress: http.Connection.RemoteIpAddress?.ToString(),
                userAgent: http.Request.Headers.UserAgent.ToString(),
                failureReason: result.Status == PasswordResetStatus.Success
                    ? null
                    : result.Status.ToString());

            return result.Status switch
            {
                PasswordResetStatus.Success => Results.Ok(new { message = "رمز عبور تعیین شد." }),
                PasswordResetStatus.InvalidOtp => Results.Unauthorized(),
                PasswordResetStatus.UserNotFound => Results.NotFound(new { message = "کاربر یافت نشد." }),
                PasswordResetStatus.PasswordMismatch => Results.BadRequest(new { message = "رمز و تکرار آن یکسان نیستند." }),
                _ => Results.BadRequest(new { message = "تعیین رمز ناموفق بود." })
            };
        }).RequireRateLimiting("otp-verify");

        group.MapGet("/tokens", async (ClaimsPrincipal user, IApiTokenService tokens) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();

            var list = await tokens.GetActiveAsync(userId);
            return Results.Ok(list.Select(t => new ApiTokenDto(
                t.Id, t.DeviceName, t.CreatedAtUtc, t.LastUsedAtUtc, t.ExpiresAtUtc)).ToList());
        }).RequireAuthorization("Api");

        group.MapDelete("/tokens/{id:int}", async (int id, ClaimsPrincipal user, IApiTokenService tokens) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();

            return await tokens.RevokeAsync(id, userId) ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("Api");

        // «خروج از همهٔ دستگاه‌ها»
        group.MapDelete("/tokens", async (ClaimsPrincipal user, IApiTokenService tokens) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();

            var count = await tokens.RevokeAllAsync(userId);
            return Results.Ok(new { revoked = count });
        }).RequireAuthorization("Api");

        app.MapGet("/api/v1/profile", async (ClaimsPrincipal user, IAuthService auth) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();

            var profile = await auth.GetProfileAsync(userId);
            return profile is null ? Results.NotFound() : Results.Ok(profile);
        }).RequireAuthorization("Api");

        return app;
    }
}
