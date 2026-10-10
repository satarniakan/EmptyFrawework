// Platform.Infrastructure/Services/KavenegarSmsSender.cs
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Platform.Application.Services;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Platform.Infrastructure.Services;

/// <summary>
/// ارسال پیامک واقعی از طریق کاوه‌نگار (REST).
/// مقدار مؤثر هر بار ارسال خوانده می‌شود: ردیف دیتابیس ← appsettings
/// («Sms:Kavenegar:ApiKey» و «Sms:Kavenegar:Sender»). بدون ApiKey، ارسال با
/// خطای روشن شکست می‌خورد تا در Outbox دیده شود.
/// </summary>
public class KavenegarSmsSender : ISmsSender
{
    private readonly HttpClient _http;
    private readonly ISettingService _settings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KavenegarSmsSender> _logger;

    public KavenegarSmsSender(HttpClient http, ISettingService settings,
        IConfiguration configuration, ILogger<KavenegarSmsSender> logger)
    {
        _http = http;
        _settings = settings;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string phoneNumber, string message)
    {
        var apiKey = await _settings.GetEffectiveAsync(
            IntegrationSettingKeys.SmsKavenegarApiKey, "Sms:Kavenegar:ApiKey");
        var sender = await _settings.GetEffectiveAsync(
            IntegrationSettingKeys.SmsKavenegarSender, "Sms:Kavenegar:Sender");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "پیکربندی پیامک ناقص است (کلید کاوه‌نگار در تنظیمات یا Sms:Kavenegar:ApiKey).");

        var payload = new Dictionary<string, string> { ["receptor"] = phoneNumber, ["message"] = message };
        if (!string.IsNullOrWhiteSpace(sender))
            payload["sender"] = sender;

        var response = await _http.PostAsync(
            $"https://api.kavenegar.com/v1/{apiKey}/sms/send.json",
            new FormUrlEncodedContent(payload));

        var body = await response.Content.ReadFromJsonAsync<KavenegarResponse>();
        if (body?.Return?.Status == 200)
            return;

        _logger.LogWarning("ارسال پیامک کاوه‌نگار ناموفق: {Error}", body?.Return?.Message ?? response.StatusCode.ToString());
        throw new InvalidOperationException($"ارسال پیامک ناموفق: {body?.Return?.Message ?? response.StatusCode.ToString()}");
    }

    private sealed class KavenegarResponse
    {
        [JsonPropertyName("return")] public KavenegarReturn? Return { get; set; }
    }

    private sealed class KavenegarReturn
    {
        [JsonPropertyName("status")] public int Status { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
    }
}
