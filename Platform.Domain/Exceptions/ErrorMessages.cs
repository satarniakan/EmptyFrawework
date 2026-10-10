namespace Platform.Domain.Exceptions;

/// <summary>
/// پیام‌های ثابتِ خطای سراسری. اینجا می‌نشیند نه در لایهٔ وب، چون هم کامپوننت‌ها و
/// هم middleware پایه به آن نیاز دارند.
/// </summary>
public static class ErrorMessages
{
    /// <summary>
    /// پیام خطاهای غیرمنتظره. جزئیات فنی هرگز به کاربر نمی‌رسد؛ فقط در لاگ ثبت می‌شود.
    /// </summary>
    public const string Unexpected =
        "خطای غیرمنتظره‌ای رخ داد. لطفاً دوباره تلاش کنید یا با پشتیبانی تماس بگیرید.";
}
