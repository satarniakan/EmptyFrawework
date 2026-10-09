using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Domain.Entities;
using Platform.Domain.Interfaces;
using WebPush;

namespace Platform.Infrastructure.Services;

/// <summary>
/// فرستندهٔ وب‌پوش با کتابخانهٔ WebPush و کلیدهای VAPID از «Push:Vapid:*».
/// اگر کلیدها تنظیم نباشند، پیاده‌سازی غیرفعال می‌شود (لاگ هشدار، بدون خطا) تا
/// میزبانی که پوش نمی‌خواهد، بی‌نیاز از تنظیم بالا بیاید.
/// </summary>
public class VapidPushSender : IPushSender
{
    private readonly VapidDetails? _vapid;
    private readonly WebPushClient _client;
    private readonly ILogger<VapidPushSender> _logger;

    public VapidPushSender(IConfiguration configuration, ILogger<VapidPushSender> logger)
    {
        _logger = logger;
        var subject = configuration["Push:Vapid:Subject"];
        var publicKey = configuration["Push:Vapid:PublicKey"];
        var privateKey = configuration["Push:Vapid:PrivateKey"];

        if (!string.IsNullOrWhiteSpace(subject)
            && !string.IsNullOrWhiteSpace(publicKey)
            && !string.IsNullOrWhiteSpace(privateKey))
        {
            _vapid = new VapidDetails(subject, publicKey, privateKey);
        }

        _client = new WebPushClient();
    }

    public async Task SendAsync(Domain.Entities.PushSubscription subscription, string title,
        string? body, string? url, CancellationToken cancellationToken = default)
    {
        if (_vapid is null)
        {
            _logger.LogWarning("Push keys not configured; skipping push to {Endpoint}", subscription.Endpoint);
            return;
        }

        var payload = JsonSerializer.Serialize(new { title, body, url });
        var webPushSubscription = new WebPush.PushSubscription(
            subscription.Endpoint, subscription.P256dh, subscription.Auth);

        try
        {
            await _client.SendNotificationAsync(webPushSubscription, payload, _vapid, cancellationToken);
        }
        catch (WebPushException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Gone
            or System.Net.HttpStatusCode.NotFound)
        {
            throw new PushSubscriptionExpiredException(subscription.Endpoint);
        }
    }
}
