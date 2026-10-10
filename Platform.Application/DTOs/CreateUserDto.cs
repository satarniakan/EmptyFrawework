using System.ComponentModel.DataAnnotations;

namespace Platform.Application.DTOs;

public class CreateUserDto
{
    [Required(ErrorMessage = "نام کامل الزامی است.")]
    [StringLength(100, ErrorMessage = "نام کامل نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل الزامی است.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    public string? Email { get; set; }

    /// <summary>
    /// خالی یعنی «بدون رمز» — کاربر فقط با پیامک وارد می‌شود (برای ایمپورت گروهی).
    /// اگر داده شود، سیاست رمز Identity (حداقل ۶ کاراکتر و…) روی همان اعمال می‌شود.
    /// </summary>
    public string? Password { get; set; }

    [MinLength(1, ErrorMessage = "انتخاب حداقل یک نقش الزامی است.")]
    public List<string> RoleNames { get; set; } = new();
}