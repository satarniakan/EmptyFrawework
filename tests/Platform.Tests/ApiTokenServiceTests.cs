using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Platform.Application.Services;
using Platform.Domain.Entities;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;

namespace Platform.Tests;

/// <summary>
/// توکن API: متن خام فقط لحظهٔ صدور دیده می‌شود (هش ذخیره می‌شود)، توکن لغوشده/منقضی
/// رد می‌شود، و لغو فقط برای توکن‌های خودِ کاربر اثر می‌کند.
/// </summary>
public class ApiTokenServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
    }

    private sealed class Harness
    {
        public Dictionary<string, UserApiToken> TokensByHash { get; } = new(StringComparer.Ordinal);
        public Mock<IApiTokenRepository> Repo { get; } = new();
        public Mock<IOtpService> Otp { get; } = new();
        public Mock<UserManager<ApplicationUser>> Users { get; } = CreateUserManager();
        public Mock<IUserClaimsPrincipalFactory<ApplicationUser>> Factory { get; } = new();
        public Mock<IPlatformUnitOfWork> Uow { get; } = new();

        public Harness()
        {
            Uow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            Repo.Setup(r => r.GetByHashAsync(It.IsAny<string>()))
                .ReturnsAsync((string h) => TokensByHash.TryGetValue(h, out var t) ? t : null);
            Repo.Setup(r => r.AddAsync(It.IsAny<UserApiToken>()))
                .Callback<UserApiToken>(t => TokensByHash[t.TokenHash] = t)
                .Returns(Task.CompletedTask);
            Repo.Setup(r => r.GetActiveForUserAsync(It.IsAny<string>()))
                .ReturnsAsync((string userId) => TokensByHash.Values
                    .Where(t => t.UserId == userId && !t.IsRevoked).ToList());
        }

        public ApiTokenService Build() => new(
            Repo.Object, Otp.Object, Users.Object, Factory.Object, Uow.Object,
            new ConfigurationBuilder().Build(),
            NullLogger<ApiTokenService>.Instance,
            new FixedTimeProvider());

        private static Mock<UserManager<ApplicationUser>> CreateUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }
    }

    private static ApplicationUser User(string id) => new() { Id = id, UserName = "09120000000" };

    [Fact]
    public async Task IssueAsync_StoresOnlyHash_ReturnsRawOnce()
    {
        var h = new Harness();
        var service = h.Build();

        var (raw, expiresAt) = await service.IssueAsync("u1", "گوشی من");

        Assert.StartsWith(ApiTokenService.TokenPrefix, raw);
        Assert.Equal(Now.AddDays(180), expiresAt);

        var stored = Assert.Single(h.TokensByHash.Values);
        Assert.Equal(ApiTokenService.HashToken(raw), stored.TokenHash);
        Assert.NotEqual(raw, stored.TokenHash);
        Assert.Equal("گوشی من", stored.DeviceName);
    }

    [Fact]
    public async Task ValidateAsync_ValidToken_ReturnsPrincipal_AndStampsLastUsed()
    {
        var h = new Harness();
        var service = h.Build();
        var user = User("u1");

        var (raw, _) = await service.IssueAsync("u1", null);
        h.Users.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(user);
        h.Users.Setup(u => u.IsLockedOutAsync(user)).ReturnsAsync(false);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("x", "y")], "test"));
        h.Factory.Setup(f => f.CreateAsync(user)).ReturnsAsync(principal);

        var result = await service.ValidateAsync(raw);

        Assert.Same(principal, result);
        Assert.Equal(Now, h.TokensByHash.Values.Single().LastUsedAtUtc);
    }

    [Fact]
    public async Task ValidateAsync_RevokedToken_ReturnsNull()
    {
        var h = new Harness();
        var service = h.Build();

        var (raw, _) = await service.IssueAsync("u1", null);
        Assert.Equal(1, await service.RevokeAllAsync("u1"));

        Assert.Null(await service.ValidateAsync(raw));
    }

    [Fact]
    public async Task ValidateAsync_ExpiredToken_ReturnsNull()
    {
        var h = new Harness();
        var service = h.Build();

        var (raw, _) = await service.IssueAsync("u1", null);
        h.TokensByHash.Values.Single().ExpiresAtUtc = Now.AddMinutes(-1);
        h.Users.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(User("u1"));

        Assert.Null(await service.ValidateAsync(raw));
    }

    [Fact]
    public async Task RevokeAsync_OtherUsersToken_ReturnsFalse()
    {
        var h = new Harness();
        var service = h.Build();

        var (raw, _) = await service.IssueAsync("u1", null);
        var tokenId = h.TokensByHash.Values.Single().Id;

        Assert.False(await service.RevokeAsync(tokenId, "u2"));

        var user = User("u1");
        h.Users.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(user);
        h.Users.Setup(u => u.IsLockedOutAsync(user)).ReturnsAsync(false);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("x", "y")], "test"));
        h.Factory.Setup(f => f.CreateAsync(user)).ReturnsAsync(principal);

        Assert.NotNull(await service.ValidateAsync(raw));
    }

    [Fact]
    public async Task VerifyOtpAndIssueTokenAsync_InvalidOtp_ReturnsFailure()
    {
        var h = new Harness();
        var service = h.Build();
        h.Otp.Setup(o => o.VerifyOtpAsync("09120000000", "000000")).ReturnsAsync(false);

        var result = await service.VerifyOtpAndIssueTokenAsync("09120000000", "000000", null);

        Assert.False(result.Succeeded);
        Assert.Null(result.Token);
        Assert.Empty(h.TokensByHash);
    }
}
