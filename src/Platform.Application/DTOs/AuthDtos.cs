namespace Platform.Application.DTOs;

public record PasswordLoginResult(bool Succeeded);

public record OtpVerificationResult(bool Succeeded, bool IsNewUser);

public enum ProfileUpdateStatus
{
    Success,
    UserNotFound,
    FullNameTooLong,
    EmailAlreadyExists,
    EmailUpdateFailed,
    PasswordAlreadySet,
    PasswordMismatch,
    PasswordUpdateFailed,

    /// <summary>نشست جانشین (ادمینِ به‌جای کاربر) حق تعیین رمز ندارد.</summary>
    ImpersonationBlocked
}

public record ProfileUpdateResult(ProfileUpdateStatus Status);

/// <summary>
/// پروفایل پایهٔ کاربر. عمداً فقط شماره، ایمیل، نام و وضعیت رمز است؛ هر فیلد دامنه‌ای
/// (آدرس، سازمان، …) باید در ماژول پروژهٔ مصرف‌کننده تعریف شود.
/// </summary>
public record UserProfileDto(
    string? UserName,
    string? PhoneNumber,
    string? FullName,
    string? Email,
    bool HasPassword);