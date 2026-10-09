using Platform.Domain.Exceptions;

namespace Platform.Web.Components.Shared;

public static class ErrorMessageHelper
{
    // پیام‌های خودِ پایه (قاعدهٔ کسب‌وکار، یافت‌نشدن، محدودیت دیتابیس) عمداً فارسی و امن نوشته
    // شده‌اند و مستقیم نشان داده می‌شوند. هر خطای دیگری (نقص برنامه، وابستگی بیرونی)
    // فقط یک پیام عمومی می‌گیرد تا جزئیات فنی لو نرود.
    public static string ToUserMessage(Exception ex) => ex switch
    {
        BusinessRuleException businessEx => businessEx.Message,
        NotFoundException notFoundEx => notFoundEx.Message,
        DataIntegrityException integrityEx => integrityEx.Message,
        _ => ErrorMessages.Unexpected
    };
}