using Platform.Application.DTOs;
using Platform.Domain.Entities;
using Platform.Domain.Enums;
using Platform.Domain.Interfaces;

namespace Platform.Application.Services;

/// <summary>
/// اعلان‌های درون‌برنامه‌ای. کاملاً عمومی است و هیچ قاعدهٔ کسب‌وکاری ندارد؛ هر قاعدهٔ
/// دامنه‌ای (مثلاً «اعلام کاهش موجودی») باید در ماژول دامنهٔ پروژهٔ مصرف‌کننده بنشیند.
/// </summary>
public interface INotificationService
{
    /// <summary>ایجاد اعلان برای یک کاربر.</summary>
    Task NotifyAsync(string userId, string title, string? body = null,
        NotificationType type = NotificationType.System, string? linkUrl = null);

    Task<NotificationFeedDto> GetFeedAsync(string userId, int take = 20);

    Task<int> GetUnreadCountAsync(string userId);

    Task MarkReadAsync(int notificationId, string userId);

    Task MarkAllReadAsync(string userId);

    /// <summary>اعلان گروهی برای فهرست کاربران.</summary>
    Task NotifyUsersAsync(IEnumerable<string> userIds, string title, string? body,
        NotificationType type, string? linkUrl);

    /// <summary>اعلان برای اعضای یک نقش.</summary>
    Task NotifyRoleAsync(string roleName, string title, string? body, NotificationType type, string? linkUrl);

    /// <summary>اعلان سراسری — به همهٔ کاربران یا اعضای یک نقش.</summary>
    Task<int> BroadcastAsync(string title, string? body, NotificationType type, string? linkUrl, string? roleName = null);
}

public class NotificationService : INotificationService
{
    private readonly IPlatformUnitOfWork _unitOfWork;

    public NotificationService(IPlatformUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task NotifyAsync(string userId, string title, string? body = null,
        NotificationType type = NotificationType.System, string? linkUrl = null)
    {
        await _unitOfWork.Notifications.AddAsync(new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            LinkUrl = linkUrl
        });
        await _unitOfWork.CompleteAsync();
    }

    public async Task<NotificationFeedDto> GetFeedAsync(string userId, int take = 20)
    {
        var unreadCount = await _unitOfWork.Notifications.GetUnreadCountAsync(userId);
        var items = await _unitOfWork.Notifications.GetForUserAsync(userId, take);
        return new NotificationFeedDto(unreadCount, items.Select(ToDto).ToList());
    }

    public Task<int> GetUnreadCountAsync(string userId) =>
        _unitOfWork.Notifications.GetUnreadCountAsync(userId);

    public async Task MarkReadAsync(int notificationId, string userId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification is null || notification.UserId != userId || notification.IsRead)
            return;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _unitOfWork.Notifications.UpdateAsync(notification);
        await _unitOfWork.CompleteAsync();
    }

    public async Task MarkAllReadAsync(string userId)
    {
        await _unitOfWork.Notifications.MarkAllReadAsync(userId);
        await _unitOfWork.CompleteAsync();
    }

    public async Task NotifyUsersAsync(IEnumerable<string> userIds, string title, string? body,
        NotificationType type, string? linkUrl)
    {
        foreach (var userId in userIds)
        {
            await _unitOfWork.Notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                LinkUrl = linkUrl
            });
        }
        await _unitOfWork.CompleteAsync();
    }

    public async Task NotifyRoleAsync(string roleName, string title, string? body, NotificationType type, string? linkUrl) =>
        await NotifyUsersAsync(await _unitOfWork.Notifications.GetUserIdsInRoleAsync(roleName), title, body, type, linkUrl);

    public async Task<int> BroadcastAsync(string title, string? body, NotificationType type, string? linkUrl, string? roleName = null)
    {
        var userIds = roleName is null
            ? await _unitOfWork.Notifications.GetAllUserIdsAsync()
            : await _unitOfWork.Notifications.GetUserIdsInRoleAsync(roleName);

        // ارسال دسته‌ای در یک Save — برای تعداد زیاد کاربر هم فقط یک رفت‌وبرگشت دیتابیس
        foreach (var userId in userIds)
        {
            await _unitOfWork.Notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                LinkUrl = linkUrl
            });
        }

        await _unitOfWork.CompleteAsync();
        return userIds.Count;
    }

    private static NotificationDto ToDto(Notification n)
    {
        var dto = new NotificationDto(n.Id, n.Title, n.Body, n.Type, n.LinkUrl, n.CreatedAt)
        {
            IsRead = n.IsRead
        };
        return dto;
    }
}