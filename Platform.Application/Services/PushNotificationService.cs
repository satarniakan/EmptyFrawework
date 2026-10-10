using Platform.Domain.Entities;
using Platform.Domain.Interfaces;

namespace Platform.Application.Services;

/// <summary>
/// اعلان وب‌پوش به دستگاه‌های کاربر. اشتراک‌های منقضی (مرورگر لغو کرده) هنگام
/// ارسال پاک می‌شوند تا جدول باد نکند. مسیر صف (Outbox کانال Push) هم از همین می‌گذرد.
/// </summary>
public interface IPushNotificationService
{
    /// <summary>ارسال به همهٔ دستگاه‌های کاربر. بازگشت: تعداد ارسال‌های موفق.</summary>
    Task<int> NotifyUserAsync(string userId, string title, string? body, string? linkUrl);
}

public class PushNotificationService : IPushNotificationService
{
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly IPushSender _sender;
    private readonly IPlatformUnitOfWork _unitOfWork;

    public PushNotificationService(IPushSubscriptionRepository subscriptions, IPushSender sender,
        IPlatformUnitOfWork unitOfWork)
    {
        _subscriptions = subscriptions;
        _sender = sender;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> NotifyUserAsync(string userId, string title, string? body, string? linkUrl)
    {
        var subscriptions = await _subscriptions.GetForUserAsync(userId);
        var sent = 0;

        foreach (var subscription in subscriptions)
        {
            try
            {
                await _sender.SendAsync(subscription, title, body, linkUrl);
                sent++;
            }
            catch (PushSubscriptionExpiredException)
            {
                // مرورگر اشتراک را لغو کرده — رکورد مرده پاک می‌شود
                await _subscriptions.RemoveAsync(subscription);
            }
        }

        if (sent != subscriptions.Count)
            await _unitOfWork.CompleteAsync();

        return sent;
    }
}
