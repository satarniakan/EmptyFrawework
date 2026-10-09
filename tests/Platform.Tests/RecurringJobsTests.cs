using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Meetings;
using Platform.Application.Services;
using Platform.Domain.Enums;
using Platform.Domain.Interfaces;
using Platform.Web.Services;

namespace Platform.Tests;

/// <summary>
/// کارهای تکرارشونده: پاک‌سازی OTP صدا زده می‌شود و یادآوری جلسه فقط جلسات
/// ۲۴ ساعت آیندهٔ یادآوری‌نشده را اعلان می‌کند (و علامت می‌زند تا تکرار نشود).
/// </summary>
public class RecurringJobsTests
{
    [Fact]
    public async Task OtpCleanupJob_CallsPurge()
    {
        var otp = new Mock<IOtpService>();
        otp.Setup(o => o.PurgeExpiredAsync(It.IsAny<TimeSpan>())).ReturnsAsync(3);

        var services = new ServiceCollection();
        services.AddSingleton(otp.Object);
        var provider = services.BuildServiceProvider();

        var job = new OtpCleanupJob(provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OtpCleanupJob>.Instance);

        Assert.Equal("otp-cleanup", job.Name);
        await job.ExecuteAsync(CancellationToken.None);

        otp.Verify(o => o.PurgeExpiredAsync(It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task MeetingReminderJob_NotifiesUnremindedUpcoming_AndStampsThem()
    {
        var now = new DateTime(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc);
        var meeting = new Meeting
        {
            Id = 1,
            Title = "جلسهٔ فردا",
            StartAt = now.AddHours(5),
            Invitees = [new MeetingInvitee { MeetingId = 1, UserId = "u1" }]
        };

        var repo = new Mock<IMeetingRepository>();
        repo.Setup(r => r.GetUnremindedStartingBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([meeting]);

        var notifications = new Mock<INotificationService>();
        var uow = new Mock<IPlatformUnitOfWork>();
        uow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(action => action());

        var services = new ServiceCollection();
        services.AddSingleton(repo.Object);
        services.AddSingleton(notifications.Object);
        services.AddSingleton(uow.Object);
        services.AddSingleton(TimeProvider.System);
        var provider = services.BuildServiceProvider();

        var job = new MeetingReminderJob(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new FixedClock(now));

        await job.ExecuteAsync(CancellationToken.None);

        notifications.Verify(n => n.NotifyUsersAsync(
            It.Is<IEnumerable<string>>(ids => ids.Count() == 1),
            "یادآوری جلسه", It.IsAny<string?>(), NotificationType.System, "/my-meetings"),
            Times.Once);
        Assert.NotNull(meeting.ReminderSentAt);
    }

    [Fact]
    public async Task MeetingReminderJob_SkipsAlreadyReminded()
    {
        var repo = new Mock<IMeetingRepository>();
        repo.Setup(r => r.GetUnremindedStartingBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        var notifications = new Mock<INotificationService>();
        var uow = new Mock<IPlatformUnitOfWork>();

        var services = new ServiceCollection();
        services.AddSingleton(repo.Object);
        services.AddSingleton(notifications.Object);
        services.AddSingleton(uow.Object);
        services.AddSingleton(TimeProvider.System);
        var provider = services.BuildServiceProvider();

        var job = new MeetingReminderJob(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new FixedClock(DateTime.UtcNow));

        await job.ExecuteAsync(CancellationToken.None);

        notifications.Verify(n => n.NotifyUsersAsync(
            It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<NotificationType>(), It.IsAny<string?>()),
            Times.Never);
    }

    private sealed class FixedClock(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
    }
}
