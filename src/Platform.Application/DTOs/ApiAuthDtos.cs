namespace Platform.Application.DTOs;

/// <summary>ورودی‌های JSON مسیرهای <c>/api/v1</c> (اپ موبایل و کلاینت‌های غیرمرورگری).</summary>
public record OtpRequestApiDto(string PhoneNumber);

public record OtpVerifyApiDto(string PhoneNumber, string Code, string? DeviceName);

/// <summary>پاسخ صدور توکن — متن خام فقط همین‌بار دیده می‌شود.</summary>
public record TokenResponseDto(string Token, DateTime ExpiresAtUtc);

public record ApiTokenDto(
    int Id,
    string? DeviceName,
    DateTime CreatedAtUtc,
    DateTime? LastUsedAtUtc,
    DateTime ExpiresAtUtc);
