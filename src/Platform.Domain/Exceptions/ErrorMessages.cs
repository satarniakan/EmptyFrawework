namespace Platform.Domain.Exceptions;

/// <summary>
/// پیام‌های ثابتِ خطای سراسری. اینجا می‌نشیند نه در لایهٔ وب، چون هم لایهٔ وب
/// (<c>Platform.Web</c>) و هم زیرساخت اجرایی (<c>Platform.Web.Hosting</c>)
/// به آن نیاز دارند و هیچ‌کدام به دیگری وابسته نیست.
/// </summary>
public static class ErrorMessages
{
    /// <summary>
    /// پیام خطاهای غیرمنتظره. جزئیات فنی هرگز به کاربر نمی‌رسد؛ فقط در لاگ ثبت می‌شود.
    /// </summary>
    public const string Unexpected =
        "خطای غیرمنتظره‌ای رخ داد. لطفاً دوباره تلاش کنید یا با پشتیبانی تماس بگیرید.";
}
