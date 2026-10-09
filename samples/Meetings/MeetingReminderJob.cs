using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Helpers;
using Platform.Application.Jobs;
using Platform.Application.Services;
using Platform.Domain.Enums;
using Platform.Domain.Interfaces;

namespace Meetings;

/// <summary>
/// یادآوری خودکار جلسات: هر ساعت، جلساتی که در ۲۴ ساعت آینده شروع می‌شوند و هنوز
/// یادآوری نشده‌اند، به کارتابل مدعوین می‌روند. اثبات seam کارهای تکرارشونده:
/// ماژول فقط یک <see cref="IRecurringJob"/> در DI ثبت می‌کند، بدون هیچ تغییری در پایه.
/// </summary>
public class MeetingReminderJob : IRecurringJob
{
    public string Name => "meeting-reminder";

    public TimeSpan Interval => TimeSpan.FromHours(1);

    private static readonly TimeSpan LeadTime = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _clock;

    public MeetingReminderJob(IServiceScopeFactory scopeFactory, TimeProvider? clock = null)
    {
        _scopeFactory = scopeFactory;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var meetings = scope.ServiceProvider.GetRequiredService<IMeetingRepository>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IPlatformUnitOfWork>();

        var now = _clock.GetUtcNow().UtcDateTime;
        var upcoming = await meetings.GetUnremindedStartingBetweenAsync(now, now + LeadTime);

        foreach (var meeting in upcoming)
        {
            var inviteeIds = meeting.Invitees.Select(i => i.UserId).ToList();
            if (inviteeIds.Count == 0)
            {
                // جلسهٔ بدون مدعو هم علامت می‌خورد تا هر دور دوباره دیده نشود
                meeting.ReminderSentAt = now;
                continue;
            }

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await notifications.NotifyUsersAsync(
                    inviteeIds,
                    "یادآوری جلسه",
                    $"جلسهٔ «{meeting.Title}» در {PersianDateHelper.ToPersianDateTime(meeting.StartAt)} برگزار می‌شود.",
                    NotificationType.System,
                    "/my-meetings");

                meeting.ReminderSentAt = now;
                await unitOfWork.CompleteAsync();
            });
        }
    }
}
