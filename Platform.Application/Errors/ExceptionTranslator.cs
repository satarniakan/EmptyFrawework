using Platform.Domain.Exceptions;

namespace Platform.Application.Errors;

/// <summary>پیام و کد وضعیتِ هر نوع خطا — جدا شده تا قابل تست باشد.</summary>
/// <param name="StatusCode">کد وضعیت HTTP.</param>
/// <param name="Message">پیام فارسیِ امن که به کاربر نشان داده می‌شود؛ جزئیات فنی هرگز نیست.</param>
/// <param name="IsExpected">خطای قابل‌انتظار (قاعدهٔ کسب‌وکار/داده) است نه نقص برنامه.</param>
public record ExceptionTranslation(int StatusCode, string Message, bool IsExpected);

/// <summary>
/// تنها منبع حقیقتِ «استثنا ← پیام امن کاربر». هم middleware سراسری
/// (پاسخ JSON/ریدایرکت) و هم کامپوننت‌های Blazor (نمایش خطای فرم) از همین استفاده
/// می‌کنند تا با افزودن نوع استثنای جدید، فقط یک‌جا به‌روز شود.
/// </summary>
public static class ExceptionTranslator
{
    /// <summary>پیام عمومی برای خطاهای غیرمنتظره — جزئیات داخل آن نمی‌آید.</summary>
    public const string UnexpectedMessage = ErrorMessages.Unexpected;

    /// <summary>
    /// نگاشت استثنا به کد وضعیت و پیام. فقط پیام‌هایی که خودِ پایه می‌سازد امن‌اند
    /// و نشان داده می‌شوند؛ هر خطای دیگری پیام عمومی می‌گیرد.
    /// </summary>
    public static ExceptionTranslation Translate(Exception? exception) => exception switch
    {
        BusinessRuleException business => new(400, business.Message, IsExpected: true),
        NotFoundException notFound => new(404, notFound.Message, IsExpected: true),
        // محدودیت یکتایی/طول فیلد — پیامش از قبل فارسی و روشن است
        DataIntegrityException integrity => new(409, integrity.Message, IsExpected: true),
        // لغو درخواست توسط خودِ کاربر/مرورگر، خطای برنامه نیست
        OperationCanceledException => new(499, "درخواست لغو شد.", IsExpected: true),
        _ => new(500, UnexpectedMessage, IsExpected: false)
    };

    /// <summary>فقط پیام امن کاربر، بدون کد وضعیت — برای نمایش در فرم‌ها و Toastها.</summary>
    public static string ToUserMessage(Exception ex) => Translate(ex).Message;
}
