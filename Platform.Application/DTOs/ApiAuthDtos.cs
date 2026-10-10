namespace Platform.Application.DTOs;

/// <summary>ورودی‌های JSON مسیرهای <c>/api/v1</c> (اپ موبایل و کلاینت‌های غیرمرورگری).</summary>
public record OtpRequestApiDto(string PhoneNumber, string? CaptchaToken = null);

public record OtpVerifyApiDto(string PhoneNumber, string Code, string? DeviceName);

/// <summary>ورود موبایل با رمز (مسیر جایگزین وقتی پیامک قطع است).</summary>
public record PasswordLoginApiDto(string Username, string Password, string? DeviceName = null);

/// <summary>تعیین رمز جدید با کد پیامکی (فراموشی رمز موبایل).</summary>
public record PasswordResetApiDto(
    string PhoneNumber, string Code, string NewPassword, string? ConfirmPassword);

/// <summary>پاسخ صدور توکن — متن خام فقط همین‌بار دیده می‌شود.</summary>
public record TokenResponseDto(string Token, DateTime ExpiresAtUtc);

public record ApiTokenDto(
    int Id,
    string? DeviceName,
    DateTime CreatedAtUtc,
    DateTime? LastUsedAtUtc,
    DateTime ExpiresAtUtc);
