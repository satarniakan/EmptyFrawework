using System.ComponentModel.DataAnnotations;

namespace Platform.Application.DTOs;

public record UserListItemDto(
    string UserId,
    string? PhoneNumber,
    string? FullName,
    string? Email,
    List<string> Roles);

/// <summary>ویرایش اطلاعات یک کاربر توسط ادمین (نقش‌ها از صفحهٔ جداگانه مدیریت می‌شوند).</summary>
public class UpdateUserDto
{
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "نام کامل الزامی است.")]
    [StringLength(100, ErrorMessage = "نام کامل نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل الزامی است.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    public string? Email { get; set; }
}