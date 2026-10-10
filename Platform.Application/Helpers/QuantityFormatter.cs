// Platform.Application/Helpers/QuantityFormatter.cs
using System.Globalization;

namespace Platform.Application.Helpers;

/// <summary>
/// نمایش یک عدد اعشاری بدون صفرهای اضافهٔ اعشار.
/// دیتابیس معمولاً اعداد اعشاری را با مقیاس کاملِ ستون برمی‌گرداند (مثلاً ۲ به‌جای ۲.۰۰۰)؛
/// اگر همان عددِ خام روی صفحه چاپ شود، کاربر به‌جای «۲» متن «2.000» می‌بیند.
/// این متدها صفرهای انتهایی را حذف می‌کنند و مقدار کسری واقعی را نگه می‌دارند.
/// </summary>
public static class QuantityFormatter
{
    // همون دلیل CurrencyFormatter: به‌جای CultureInfo("fa-IR")، جداکننده‌ها رو صریح مشخص می‌کنیم
    // تا همیشه «,» و «.» باشن، نه جداکننده‌های عربی «٬»/«٫» که بسته به نسخه‌ی دات‌نت ممکنه برگرده.
    // نکته: اسم این فیلد نباید «Format» باشه، چون با متد پایین (که اونم Format نام داره) تداخل پیدا می‌کنه.
    private static readonly NumberFormatInfo NumberFormat = new()
    {
        NumberGroupSeparator = ",",
        NumberDecimalSeparator = "."
    };

    public static string Format(decimal quantity)
    {
        // "#,0.###" یعنی: جداکننده‌ی هزارگان بذار، تا ۳ رقم اعشار نشون بده ولی صفرهای
        // اضافه‌ی انتهایی رو (اگه لازم نبودن) حذف کن.
        return quantity.ToString("#,0.###", NumberFormat);
    }
}
