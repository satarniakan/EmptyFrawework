using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.Domain.Interfaces;

namespace Platform.Infrastructure.Services;

/// <summary>
/// کپچای Cloudflare Turnstile. تنظیمات از «Captcha:Turnstile:SecretKey» (سرور) و
/// «Captcha:Turnstile:SiteKey» (ویجت فرم لاگین). هر دو خالی = این provider انتخاب نمی‌شود.
/// </summary>
public class TurnstileCaptchaValidator : ICaptchaValidator
{
    public string Name => "turnstile";

    private readonly HttpClient _http;
    private readonly string _secretKey;
    private readonly ILogger<TurnstileCaptchaValidator> _logger;

    public TurnstileCaptchaValidator(HttpClient http, string secretKey,
        ILogger<TurnstileCaptchaValidator> logger)
    {
        _http = http;
        _secretKey = secretKey;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(string? token, string? remoteIp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        using var response = await _http.PostAsJsonAsync(
            "https://challenges.cloudflare.com/turnstile/v0/siteverify",
            new { secret = _secretKey, response = token, remoteip = remoteIp },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        var ok = doc.RootElement.TryGetProperty("success", out var success)
            && success.GetBoolean();

        if (!ok)
            _logger.LogWarning("Turnstile validation failed");

        return ok;
    }
}

/// <summary>
/// کپچای غیرفعال: همیشه موفق. برای وقتی که «Captcha:Provider» تنظیم نشده
/// (توسعهٔ محلی) یا صریحاً «Fake» است. در حالت Fake توکن لاگ می‌شود.
/// </summary>
public class PermissiveCaptchaValidator : ICaptchaValidator
{
    public string Name { get; }

    private readonly ILogger<PermissiveCaptchaValidator> _logger;
    private readonly bool _logTokens;

    public PermissiveCaptchaValidator(ILogger<PermissiveCaptchaValidator> logger,
        string name = "none", bool logTokens = false)
    {
        _logger = logger;
        Name = name;
        _logTokens = logTokens;
    }

    public Task<bool> ValidateAsync(string? token, string? remoteIp,
        CancellationToken cancellationToken = default)
    {
        if (_logTokens)
            _logger.LogInformation("FAKE captcha accepted (token present: {HasToken})",
                !string.IsNullOrWhiteSpace(token));

        return Task.FromResult(true);
    }
}
