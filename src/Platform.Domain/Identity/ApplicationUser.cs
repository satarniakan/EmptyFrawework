using Microsoft.AspNetCore.Identity;

namespace Platform.Domain.Identity;

/// <summary>
/// کاربر سامانه. عمداً فقط شامل چیزهایی است که خودِ پایه لازم دارد —
/// هر فیلد دامنه‌ای (آدرس، سازمان، شمارهٔ پرسنلی و…) باید در پروژهٔ مصرف‌کننده
/// و ترجیحاً در جدول پروفایل جداگانه تعریف شود.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// نام نمایشی. اگر خالی باشد، <c>AppUserClaimsPrincipalFactory</c> مقداری نمی‌سازد
    /// و رابط کاربری می‌تواند از نام کاربری استفاده کند.
    /// </summary>
    public string? FullName { get; set; }
}