using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Application.Services;

namespace Platform.Web.Authentication;

/// <summary>
/// احراز هویت توکن API برای کلاینت‌های غیرمرورگری (اپ موبایل).
/// هدر: <c>Authorization: Bearer pat_...</c>. طرح کوکی (پیش‌فرض) دست نمی‌خورد؛
/// endpointهای API صریحاً همین scheme را می‌خواهند.
/// </summary>
public class ApiTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiToken";

    private readonly IApiTokenService _tokens;

    public ApiTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiTokenService tokens)
        : base(options, logger, encoder)
    {
        _tokens = tokens;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var header))
            return AuthenticateResult.NoResult();

        const string prefix = "Bearer ";
        var value = header.ToString();
        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = value[prefix.Length..].Trim();
        if (token.Length == 0)
            return AuthenticateResult.Fail("توکن خالی است.");

        ClaimsPrincipal? principal;
        try
        {
            principal = await _tokens.ValidateAsync(token);
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail($"خطا در اعتبارسنجی توکن: {ex.Message}");
        }

        if (principal is null)
            return AuthenticateResult.Fail("توکن نامعتبر است.");

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
