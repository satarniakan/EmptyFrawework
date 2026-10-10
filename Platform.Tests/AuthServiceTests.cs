using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Platform.Application.DTOs;
using Platform.Application.Services;
using Platform.Domain.Identity;

namespace Platform.Tests;

/// <summary>
/// تعیین رمز با کد پیامکی: کد نامعتبر/کاربر ناموجود/عدم تطابق/شکست Identity/موفق.
/// </summary>
public class AuthServiceTests
{
    private sealed class Harness
    {
        public Mock<UserManager<ApplicationUser>> Users { get; } = CreateUserManager();
        public Mock<IOtpService> Otp { get; } = new();

        public AuthService Build()
        {
            var signIn = new Mock<SignInManager<ApplicationUser>>(
                Users.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                null!, null!, null!, null!);

            return new AuthService(
                Users.Object,
                signIn.Object,
                Otp.Object,
                Mock.Of<INotificationService>(),
                Mock.Of<ILoginHistoryService>(),
                Mock.Of<IHttpContextAccessor>(),
                new ConfigurationBuilder().Build(),
                NullLogger<AuthService>.Instance);
        }

        private static Mock<UserManager<ApplicationUser>> CreateUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }
    }

    private static ApplicationUser User(string id = "u1") => new()
    {
        Id = id,
        UserName = "09120000000",
        PhoneNumber = "09120000000"
    };

    [Fact]
    public async Task ResetPassword_InvalidOtp_ReturnsInvalidOtp_WithoutUserLookup()
    {
        var h = new Harness();
        h.Otp.Setup(o => o.VerifyOtpAsync("09120000000", "000000")).ReturnsAsync(false);

        var result = await h.Build().ResetPasswordWithOtpAsync("09120000000", "000000", "new-pass-1", "new-pass-1");

        Assert.Equal(PasswordResetStatus.InvalidOtp, result.Status);
        h.Users.Verify(u => u.FindByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_UnknownUser_ReturnsUserNotFound()
    {
        var h = new Harness();
        h.Otp.Setup(o => o.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        h.Users.Setup(u => u.FindByNameAsync("09120000000")).ReturnsAsync((ApplicationUser?)null);

        var result = await h.Build().ResetPasswordWithOtpAsync("09120000000", "123456", "new-pass-1", "new-pass-1");

        Assert.Equal(PasswordResetStatus.UserNotFound, result.Status);
    }

    [Fact]
    public async Task ResetPassword_Mismatch_ReturnsPasswordMismatch()
    {
        var h = new Harness();
        h.Otp.Setup(o => o.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        h.Users.Setup(u => u.FindByNameAsync("09120000000")).ReturnsAsync(User());

        var result = await h.Build().ResetPasswordWithOtpAsync("09120000000", "123456", "aaa", "bbb");

        Assert.Equal(PasswordResetStatus.PasswordMismatch, result.Status);
        Assert.Equal("u1", result.UserId);
        h.Users.Verify(u => u.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_IdentityRejects_ReturnsResetFailed()
    {
        var h = new Harness();
        var user = User();
        h.Otp.Setup(o => o.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        h.Users.Setup(u => u.FindByNameAsync("09120000000")).ReturnsAsync(user);
        h.Users.Setup(u => u.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("tok");
        h.Users.Setup(u => u.ResetPasswordAsync(user, "tok", "123"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "کوتاه است." }));

        var result = await h.Build().ResetPasswordWithOtpAsync("09120000000", "123456", "123", "123");

        Assert.Equal(PasswordResetStatus.ResetFailed, result.Status);
    }

    [Fact]
    public async Task ResetPassword_ValidFlow_ReturnsSuccess()
    {
        var h = new Harness();
        var user = User();
        h.Otp.Setup(o => o.VerifyOtpAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        h.Users.Setup(u => u.FindByNameAsync("09120000000")).ReturnsAsync(user);
        h.Users.Setup(u => u.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("tok");
        h.Users.Setup(u => u.ResetPasswordAsync(user, "tok", "new-pass-1"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await h.Build().ResetPasswordWithOtpAsync(
            // ارقام فارسی هم باید کار کنند (نرمال‌سازی شماره)
            "۰۹۱۲۰۰۰۰۰۰۰", "123456", "new-pass-1", "new-pass-1");

        Assert.Equal(PasswordResetStatus.Success, result.Status);
        Assert.Equal("u1", result.UserId);
    }
}
