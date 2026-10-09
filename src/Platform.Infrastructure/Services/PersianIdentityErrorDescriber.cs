using Microsoft.AspNetCore.Identity;

namespace Platform.Infrastructure.Services;

/// <summary>
/// پیام‌های فارسی خطاهای Identity. بدون این، کاربر نهایی پیام‌های انگلیسی
/// («Passwords must be at least 6 characters.») می‌دید.
/// </summary>
public class PersianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"ایمیل «{email}» قبلاً استفاده شده است." };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"نام کاربری «{userName}» قبلاً استفاده شده است." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "ایمیل معتبر نیست." };

    public override IdentityError DuplicateRoleName(string role) =>
        new() { Code = nameof(DuplicateRoleName), Description = $"نقش «{role}» قبلاً ساخته شده است." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"رمز عبور باید حداقل {length} کاراکتر باشد." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = "رمز عبور باید تنوع کاراکتری بیشتری داشته باشد." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "رمز عبور باید حداقل یک کاراکتر غیرحرفی (مثل ! یا @) داشته باشد." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "رمز عبور باید حداقل یک رقم داشته باشد." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "رمز عبور باید حداقل یک حرف کوچک انگلیسی داشته باشد." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "رمز عبور باید حداقل یک حرف بزرگ انگلیسی داشته باشد." };

    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "رمز عبور اشتباه است." };

    public override IdentityError InvalidToken() =>
        new() { Code = nameof(InvalidToken), Description = "توکن نامعتبر است." };

    public override IdentityError UserAlreadyHasPassword() =>
        new() { Code = nameof(UserAlreadyHasPassword), Description = "برای این کاربر قبلاً رمز عبور تنظیم شده است." };

    public override IdentityError UserLockoutNotEnabled() =>
        new() { Code = nameof(UserLockoutNotEnabled), Description = "قفل موقت برای این کاربر فعال نیست." };

    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = "خطای ناشناخته‌ای رخ داد." };
}
