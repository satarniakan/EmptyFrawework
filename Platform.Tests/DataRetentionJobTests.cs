using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Platform.App.Services;
using Platform.Domain.Interfaces;

namespace Platform.Tests;

/// <summary>
/// خانه‌تکانی داده‌ها: هر قلم با سقفِ خودش اجرا می‌شود؛ صفر/منفی → غیرفعال؛
/// شکست یک قلم بقیهٔ قلم‌ها را متوقف نمی‌کند.
/// </summary>
public class DataRetentionJobTests
{
    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FixedClock(DateTime now) => _now = new DateTimeOffset(now, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class Harness
    {
        public Mock<IServiceScopeFactory> ScopeFactory { get; } = new();
        public Mock<IServiceScope> Scope { get; } = new();
        public Mock<IServiceProvider> Sp { get; } = new();
        public Mock<ILoginHistoryRepository> LoginHistory { get; } = new();
        public Mock<IApiTokenRepository> ApiToken { get; } = new();
        public Mock<INotificationRepository> Notification { get; } = new();
        public Mock<IAuditLogRepository> AuditLog { get; } = new();

        public Harness()
        {
            LoginHistory.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>())).ReturnsAsync(0);
            ApiToken.Setup(r => r.DeleteExpiredAsync(It.IsAny<DateTime>())).ReturnsAsync(0);
            Notification.Setup(r => r.DeleteReadOlderThanAsync(It.IsAny<DateTime>())).ReturnsAsync(0);
            AuditLog.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>())).ReturnsAsync(0);

            Sp.Setup(s => s.GetService(typeof(ILoginHistoryRepository))).Returns(LoginHistory.Object);
            Sp.Setup(s => s.GetService(typeof(IApiTokenRepository))).Returns(ApiToken.Object);
            Sp.Setup(s => s.GetService(typeof(INotificationRepository))).Returns(Notification.Object);
            Sp.Setup(s => s.GetService(typeof(IAuditLogRepository))).Returns(AuditLog.Object);

            Scope.Setup(s => s.ServiceProvider).Returns(Sp.Object);
            ScopeFactory.Setup(f => f.CreateScope()).Returns(Scope.Object);
        }

        public DataRetentionJob Build(IConfiguration config, DateTime now) =>
            new(ScopeFactory.Object, config, NullLogger<DataRetentionJob>.Instance,
                new FixedClock(now));
    }

    private static IConfiguration Config(params (string key, string value)[] items) =>
        new ConfigurationBuilder().AddInMemoryCollection(items.ToDictionary(i => i.key, i => (string?)i.value)).Build();

    private static readonly DateTime Now = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_ZeroOrNegativeDays_DoesNotCallRepository()
    {
        var h = new Harness();
        var job = h.Build(Config(
            ("Retention:LoginHistoryDays", "0"),
            ("Retention:ApiTokenDays", "-1")), Now);

        await job.ExecuteAsync(default);

        h.LoginHistory.Verify(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>()), Times.Never);
        h.ApiToken.Verify(r => r.DeleteExpiredAsync(It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_MissingKeys_UseDefaultDays()
    {
        var h = new Harness();
        var job = h.Build(new ConfigurationBuilder().Build(), Now);

        await job.ExecuteAsync(default);

        h.LoginHistory.Verify(r => r.DeleteOlderThanAsync(Now.AddDays(-DataRetentionJob.Defaults.LoginHistoryDays)), Times.Once);
        h.ApiToken.Verify(r => r.DeleteExpiredAsync(Now.AddDays(-DataRetentionJob.Defaults.ApiTokenDays)), Times.Once);
        h.Notification.Verify(r => r.DeleteReadOlderThanAsync(Now.AddDays(-DataRetentionJob.Defaults.NotificationDays)), Times.Once);
        h.AuditLog.Verify(r => r.DeleteOlderThanAsync(Now.AddDays(-DataRetentionJob.Defaults.AuditLogDays)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OneRepositoryThrows_StillRunsOthers()
    {
        var h = new Harness();
        h.LoginHistory.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        h.ApiToken.Setup(r => r.DeleteExpiredAsync(It.IsAny<DateTime>())).ReturnsAsync(7);

        var job = h.Build(Config(
            ("Retention:LoginHistoryDays", "30"),
            ("Retention:ApiTokenDays", "60")), Now);

        await job.ExecuteAsync(default);

        h.ApiToken.Verify(r => r.DeleteExpiredAsync(Now.AddDays(-60)), Times.Once);
        h.Notification.Verify(r => r.DeleteReadOlderThanAsync(It.IsAny<DateTime>()), Times.Once);
        h.AuditLog.Verify(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DeletesEachTableAtConfiguredCutoff()
    {
        var h = new Harness();
        h.LoginHistory.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>())).ReturnsAsync(1);
        h.ApiToken.Setup(r => r.DeleteExpiredAsync(It.IsAny<DateTime>())).ReturnsAsync(2);
        h.Notification.Setup(r => r.DeleteReadOlderThanAsync(It.IsAny<DateTime>())).ReturnsAsync(3);
        h.AuditLog.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>())).ReturnsAsync(4);

        var job = h.Build(Config(
            ("Retention:LoginHistoryDays", "30"),
            ("Retention:ApiTokenDays", "60"),
            ("Retention:NotificationDays", "7"),
            ("Retention:AuditLogDays", "365")), Now);

        await job.ExecuteAsync(default);

        h.LoginHistory.Verify(r => r.DeleteOlderThanAsync(Now.AddDays(-30)), Times.Once);
        h.ApiToken.Verify(r => r.DeleteExpiredAsync(Now.AddDays(-60)), Times.Once);
        h.Notification.Verify(r => r.DeleteReadOlderThanAsync(Now.AddDays(-7)), Times.Once);
        h.AuditLog.Verify(r => r.DeleteOlderThanAsync(Now.AddDays(-365)), Times.Once);
    }
}