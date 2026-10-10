using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Application.Services;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;
using WebPush;

namespace Platform.Infrastructure.Services;

/// <summary>
/// فرستندهٔ وب‌پوش با کتابخانهٔ WebPush و کلیدهای VAPID.
/// مقدار مؤثر هر بار ارسال خوانده می‌شود: ردیف دیتابیس ← appsettings («Push:Vapid:*»).
/// اگر کلیدها هیچ‌جا تنظیم نباشند، ارسال رد می‌شود (لاگ هشدار، بدون خطا) تا
/// میزبانی که پوش نمی‌خواهد، بی‌نیاز از تنظیم بالا بیاید.
/// </summary>
public class VapidPushSender : IPushSender
{
    private readonly ISettingService _settings;
    private readonly IConfiguration _configuration;
    private readonly WebPushClient _client;
    private readonly ILogger<VapidPushSender> _logger;

    public VapidPushSender(ISettingService settings, IConfiguration configuration,
        ILogger<VapidPushSender> logger)
    {
        _settings = settings;
        _configuration = configuration;
        _logger = logger;
        _client = new WebPushClient();
    }

    public async Task SendAsync(Domain.Entities.PushSubscription subscription, string title,
        string? body, string? url, CancellationToken cancellationToken = default)
    {
        var subject = await _settings.GetEffectiveAsync(
            IntegrationSettingKeys.VapidSubject, "Push:Vapid:Subject");
        var publicKey = await _settings.GetEffectiveAsync(
            IntegrationSettingKeys.VapidPublicKey, "Push:Vapid:PublicKey");
        var privateKey = await _settings.GetEffectiveAsync(
            IntegrationSettingKeys.VapidPrivateKey, "Push:Vapid:PrivateKey");

        if (string.IsNullOrWhiteSpace(subject)
            || string.IsNullOrWhiteSpace(publicKey)
            || string.IsNullOrWhiteSpace(privateKey))
        {
            _logger.LogWarning("Push keys not configured; skipping push to {Endpoint}", subscription.Endpoint);
            return;
        }

        var vapid = new VapidDetails(subject, publicKey, privateKey);
        var payload = JsonSerializer.Serialize(new { title, body, url });
        var webPushSubscription = new WebPush.PushSubscription(
            subscription.Endpoint, subscription.P256dh, subscription.Auth);

        try
        {
            await _client.SendNotificationAsync(webPushSubscription, payload, vapid, cancellationToken);
        }
        catch (WebPushException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Gone
            or System.Net.HttpStatusCode.NotFound)
        {
            throw new PushSubscriptionExpiredException(subscription.Endpoint);
        }
    }
}
