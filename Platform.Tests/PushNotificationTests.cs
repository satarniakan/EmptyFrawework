using Moq;
using Platform.Application.Services;
using Platform.Domain.Entities;
using Platform.Domain.Interfaces;

namespace Platform.Tests;

/// <summary>
/// وب‌پوش: به همهٔ دستگاه‌ها می‌رود، اشتراک منقضی پاک می‌شود، و بدون اشتراک
/// هیچ ارسالی نیست (و ذخیره‌ای هم لازم نیست).
/// </summary>
public class PushNotificationTests
{
    private sealed class Harness
    {
        public List<PushSubscription> Store { get; } = new();
        public Mock<IPushSubscriptionRepository> Repo { get; } = new();
        public Mock<IPushSender> Sender { get; } = new();
        public Mock<IPlatformUnitOfWork> Uow { get; } = new();

        public Harness()
        {
            Repo.Setup(r => r.GetForUserAsync(It.IsAny<string>()))
                .ReturnsAsync((string userId) => Store.Where(s => s.UserId == userId).ToList());
            Repo.Setup(r => r.RemoveAsync(It.IsAny<PushSubscription>()))
                .Callback<PushSubscription>(s => Store.Remove(s))
                .Returns(Task.CompletedTask);
        }

        public PushNotificationService Build() =>
            new(Repo.Object, Sender.Object, Uow.Object);
    }

    private static PushSubscription Sub(string user, int id) => new()
    {
        Id = id,
        UserId = user,
        Endpoint = $"https://push.example/{user}/{id}",
        P256dh = "p256",
        Auth = "auth",
        CreatedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task NotifyUserAsync_SendsToAllDevices()
    {
        var h = new Harness();
        h.Store.Add(Sub("u1", 1));
        h.Store.Add(Sub("u1", 2));

        var sent = await h.Build().NotifyUserAsync("u1", "سلام", "متن", "/x");

        Assert.Equal(2, sent);
        h.Sender.Verify(s => s.SendAsync(It.IsAny<PushSubscription>(), "سلام", "متن", "/x",
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        h.Uow.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task NotifyUserAsync_ExpiredSubscription_IsRemoved()
    {
        var h = new Harness();
        h.Store.Add(Sub("u1", 1));
        h.Store.Add(Sub("u1", 2));
        h.Sender.Setup(s => s.SendAsync(
                It.Is<PushSubscription>(s => s.Id == 2),
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PushSubscriptionExpiredException("gone"));

        var sent = await h.Build().NotifyUserAsync("u1", "سلام", null, null);

        Assert.Equal(1, sent);
        Assert.Single(h.Store);
        h.Uow.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task NotifyUserAsync_NoSubscriptions_SendsNothing()
    {
        var h = new Harness();

        Assert.Equal(0, await h.Build().NotifyUserAsync("u1", "سلام", null, null));
        h.Sender.Verify(s => s.SendAsync(It.IsAny<PushSubscription>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
