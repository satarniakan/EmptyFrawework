using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Platform.Application.Services;
using Platform.Domain.Identity;

namespace Platform.Tests;

/// <summary>
/// جانشینی: خاموش = همیشه ممنوع؛ خود و ادمین دیگر ممنوع؛ کاربر عادی با نام نمایشی مجاز.
/// </summary>
public class ImpersonationTests
{
    private static ImpersonationService Build(bool enabled, Mock<UserManager<ApplicationUser>>? users = null)
    {
        users ??= CreateUserManager();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Support:ImpersonationEnabled"] = enabled ? "true" : "false"
            })
            .Build();

        return new ImpersonationService(users.Object, config);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task CanImpersonate_WhenDisabled_ReturnsFalse()
    {
        var service = Build(enabled: false);

        var (allowed, _, _) = await service.CanImpersonateAsync("admin", "user");

        Assert.False(allowed);
        Assert.False(service.IsEnabled);
    }

    [Fact]
    public async Task CanImpersonate_Self_ReturnsFalse()
    {
        var service = Build(enabled: true);

        var (allowed, reason, _) = await service.CanImpersonateAsync("u1", "u1");

        Assert.False(allowed);
        Assert.NotNull(reason);
    }

    [Fact]
    public async Task CanImpersonate_AdminTarget_ReturnsFalse()
    {
        var users = CreateUserManager();
        var target = new ApplicationUser { Id = "admin2", UserName = "09000000002" };
        users.Setup(u => u.FindByIdAsync("admin2")).ReturnsAsync(target);
        users.Setup(u => u.IsInRoleAsync(target, Roles.Admin)).ReturnsAsync(true);

        var (allowed, _, _) = await Build(enabled: true, users).CanImpersonateAsync("admin1", "admin2");

        Assert.False(allowed);
    }

    [Fact]
    public async Task CanImpersonate_OrdinaryUser_ReturnsTrueWithDisplayName()
    {
        var users = CreateUserManager();
        var target = new ApplicationUser
        {
            Id = "u9",
            UserName = "09120000009",
            PhoneNumber = "09120000009",
            FullName = "کاربر تست"
        };
        users.Setup(u => u.FindByIdAsync("u9")).ReturnsAsync(target);
        users.Setup(u => u.IsInRoleAsync(target, Roles.Admin)).ReturnsAsync(false);

        var (allowed, reason, displayName) =
            await Build(enabled: true, users).CanImpersonateAsync("admin1", "u9");

        Assert.True(allowed);
        Assert.Null(reason);
        Assert.Equal("کاربر تست", displayName);
    }

    [Fact]
    public async Task CanImpersonate_UnknownUser_ReturnsFalse()
    {
        var users = CreateUserManager();
        users.Setup(u => u.FindByIdAsync("nope")).ReturnsAsync((ApplicationUser?)null);

        var (allowed, _, _) = await Build(enabled: true, users).CanImpersonateAsync("admin1", "nope");

        Assert.False(allowed);
    }
}
