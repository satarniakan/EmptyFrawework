using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Domain.Enums;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;

namespace Platform.Application.Services;

/// <summary>آمار صفحهٔ خانه: دو عدد شخصی + دو عدد مدیریتی (فقط برای ادمین پر می‌شوند).</summary>
public record HomeStats(
    int UnreadNotifications,
    int ActiveTokens,
    int? TotalUsers,
    int? FailedOutbox);

public interface IDashboardService
{
    Task<HomeStats> GetHomeStatsAsync(string userId, bool isAdmin);
}

public class DashboardService : IDashboardService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOutboxRepository _outbox;
    private readonly INotificationService _notifications;
    private readonly IApiTokenService _tokens;

    public DashboardService(UserManager<ApplicationUser> userManager, IOutboxRepository outbox,
        INotificationService notifications, IApiTokenService tokens)
    {
        _userManager = userManager;
        _outbox = outbox;
        _notifications = notifications;
        _tokens = tokens;
    }

    public async Task<HomeStats> GetHomeStatsAsync(string userId, bool isAdmin)
    {
        var unread = await _notifications.GetUnreadCountAsync(userId);
        var tokens = await _tokens.GetActiveAsync(userId);

        int? totalUsers = null;
        int? failedOutbox = null;
        if (isAdmin)
        {
            totalUsers = await _userManager.Users.CountAsync();
            failedOutbox = await _outbox.CountAsync(OutboxStatus.Failed);
        }

        return new HomeStats(unread, tokens.Count, totalUsers, failedOutbox);
    }
}
