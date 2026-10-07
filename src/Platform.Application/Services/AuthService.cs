using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Application.DTOs;
using Platform.Domain.Enums;
using Platform.Domain.Identity;

namespace Platform.Application.Services;

public interface IAuthService
{
    Task<PasswordLoginResult> LoginWithPasswordAsync(string userName, string password);
    Task RequestOtpAsync(string phoneNumber);
    Task<OtpVerificationResult> VerifyOtpAsync(string phoneNumber, string code);
    Task LogoutAsync();
    Task<UserProfileDto?> GetProfileAsync(string userId);
    Task<ProfileUpdateResult> UpdateProfileAsync(
        string userId, string? fullName, string? email, string? password, string? confirmPassword);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOtpService _otpService;
    private readonly ILogger<AuthService> _logger;
    private readonly INotificationService _notifications;
    private readonly string? _firstAdminPhoneNumber;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOtpService otpService,
        INotificationService notifications,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _otpService = otpService;
        _notifications = notifications;
        _logger = logger;
        // شمارهٔ ادمین اول فقط از تنظیمات «Identity:FirstAdminPhoneNumber» خوانده می‌شود.
        // اگر تنظیم نباشد، هیچ شماره‌ای خودکار نقش Admin نمی‌گیرد.
        _firstAdminPhoneNumber = configuration["Identity:FirstAdminPhoneNumber"];
    }

    public async Task<PasswordLoginResult> LoginWithPasswordAsync(string userName, string password)
    {
        var user = await FindByUserNameOrEmailAsync(userName);

        if (user is null)
        {
            _logger.LogWarning("Login failed: no user found for {UserName}", userName);
            return new PasswordLoginResult(false);
        }

        // lockoutOnFailure:true (پیش‌فرض Identity خودش FALSE است) قفل موقت حساب را می‌خواهد.
        var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Login failed for {UserName}: {Reason}", userName,
                result.IsLockedOut ? "locked out" : result.IsNotAllowed ? "not allowed" : "invalid password");
        }

        return new PasswordLoginResult(result.Succeeded);
    }

    public Task RequestOtpAsync(string phoneNumber) => _otpService.GenerateAndSendOtpAsync(phoneNumber);

    public async Task<OtpVerificationResult> VerifyOtpAsync(string phoneNumber, string code)
    {
        if (!await _otpService.VerifyOtpAsync(phoneNumber, code))
            return new OtpVerificationResult(false, false);

        var user = await _userManager.FindByNameAsync(phoneNumber);
        var isNewUser = user is null;
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = phoneNumber,
                PhoneNumber = phoneNumber,
                PhoneNumberConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                // دو درخواست موازی VerifyOtp برای شمارهٔ جدید: درخواست بازنده با
                // DuplicateUserName شکست می‌خورد — کاربر تازه‌ساخته‌شده را دوباره می‌خوانیم
                // و به‌جای برگرداندن «ناموفق»، او را لاگین می‌کنیم.
                if (createResult.Errors.Any(e => e.Code == "DuplicateUserName"))
                {
                    user = await _userManager.FindByNameAsync(phoneNumber);
                    isNewUser = false;
                }

                if (user is null)
                {
                    _logger.LogWarning("User creation failed for {PhoneNumber}: {Errors}",
                        phoneNumber, string.Join(" | ", createResult.Errors.Select(e => e.Description)));
                    return new OtpVerificationResult(false, false);
                }
            }

            if (isNewUser)
            {
                // نقش پیش‌فرضِ کاربر تازه‌وارد «کاربر» است، نه ادمین. تنها استثنا شماره‌ای
                // است که در «Identity:FirstAdminPhoneNumber» تنظیم شده — همان «ادمین اول»
                // روی دیتابیس خالی. بدون این گارد، هر شماره‌ای که OTP را تأیید می‌کرد
                // خودکار ادمین می‌شد (نقص امنیتی). قانون در Roles.DefaultRoleFor است.
                await _userManager.AddToRoleAsync(
                    user, Roles.DefaultRoleFor(_firstAdminPhoneNumber, phoneNumber));

                await _notifications.NotifyRoleAsync(Roles.Admin, "کاربر جدید ثبت‌نام کرد",
                    $"شماره {phoneNumber}", NotificationType.System, "/admin/users");
            }
        }

        await _signInManager.SignInAsync(user, isPersistent: true);
        return new OtpVerificationResult(true, isNewUser);
    }

    public Task LogoutAsync() => _signInManager.SignOutAsync();

    public async Task<UserProfileDto?> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        return new UserProfileDto(
            user.UserName,
            user.PhoneNumber,
            user.FullName,
            user.Email,
            await _userManager.HasPasswordAsync(user));
    }

    public async Task<ProfileUpdateResult> UpdateProfileAsync(
        string userId, string? fullName, string? email, string? password, string? confirmPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return new ProfileUpdateResult(ProfileUpdateStatus.UserNotFound);

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            user.FullName = fullName.Trim();
            await _userManager.UpdateAsync(user);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser is not null && existingUser.Id != user.Id)
                return new ProfileUpdateResult(ProfileUpdateStatus.EmailAlreadyExists);

            var emailResult = await _userManager.SetEmailAsync(user, email);
            if (!emailResult.Succeeded)
            {
                _logger.LogWarning("SetEmail failed for user {UserId}: {Errors}",
                    userId, string.Join(" | ", emailResult.Errors.Select(e => e.Description)));
                return new ProfileUpdateResult(ProfileUpdateStatus.EmailUpdateFailed);
            }
        }

        if (string.IsNullOrWhiteSpace(password)) return new ProfileUpdateResult(ProfileUpdateStatus.Success);

        if (await _userManager.HasPasswordAsync(user))
            return new ProfileUpdateResult(ProfileUpdateStatus.PasswordAlreadySet);

        if (password != confirmPassword)
            return new ProfileUpdateResult(ProfileUpdateStatus.PasswordMismatch);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, password);
        return result.Succeeded
            ? new ProfileUpdateResult(ProfileUpdateStatus.Success)
            : new ProfileUpdateResult(ProfileUpdateStatus.PasswordUpdateFailed);
    }

    private async Task<ApplicationUser?> FindByUserNameOrEmailAsync(string userName)
    {
        var byEmail = await _userManager.FindByEmailAsync(userName);
        return byEmail ?? await _userManager.FindByNameAsync(userName);
    }
}