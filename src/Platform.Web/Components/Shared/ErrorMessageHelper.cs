using Platform.Application.Errors;

namespace Platform.Web.Components.Shared;

public static class ErrorMessageHelper
{
    // تنها منبع حقیقت ExceptionTranslator است؛ اینجا فقط میان‌بر همان برای فرم‌هاست تا
    // با افزودن نوع استثنای جدید، دو پیاده‌سازی از هم جدا نمانند.
    public static string ToUserMessage(Exception ex) => ExceptionTranslator.ToUserMessage(ex);
}
