using Platform.Domain.Entities;

namespace Platform.Domain.Interfaces;

public interface IPushSubscriptionRepository
{
    Task<List<PushSubscription>> GetForUserAsync(string userId);

    Task<PushSubscription?> GetByEndpointAsync(string userId, string endpoint);

    Task AddAsync(PushSubscription subscription);

    Task RemoveAsync(PushSubscription subscription);

    Task<int> RemoveByEndpointAsync(string endpoint);
}

/// <summary>
/// ارسال سطح‌پایین وب‌پوش به یک اشتراک. خطای «اشتراک منقضی» با استثنای
/// <see cref="PushSubscriptionExpiredException"/> گزارش می‌شود تا فراخواننده پاکش کند.
/// </summary>
public interface IPushSender
{
    Task SendAsync(PushSubscription subscription, string title, string? body, string? url,
        CancellationToken cancellationToken = default);
}

/// <summary>اشتراک مرورگر دیگر معتبر نیست (410 Gone / 404) — رکوردش باید حذف شود.</summary>
public sealed class PushSubscriptionExpiredException(string endpoint)
    : Exception($"Push subscription expired: {endpoint}")
{
    public string Endpoint { get; } = endpoint;
}
