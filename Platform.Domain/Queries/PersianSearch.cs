using System.Linq.Expressions;
using System.Text;

namespace Platform.Domain.Queries;

/// <summary>
/// نرمال‌سازی متن فارسی برای جست‌وجو و ذخیرهٔ یکدست.
/// <para>
/// مسئله: «ي» و «ك» عربی با «ی» و «ک» فارسی حروف متفاوتی‌اند و کاربر با هر کیبوردی
/// ممکن است بنویسد؛ ارقام فارسی/عربی با انگلیسی فرق می‌کنند؛ اعراب و کشیده (ـ) هم
/// جست‌وجو را می‌شکنند. این کلاس هر دو سمت (عبارت جست‌وجو و ستون دیتابیس) را به یک
/// شکل می‌آورد. زنجیرهٔ Replace عمداً با متدهای قابل‌ترجمهٔ EF نوشته شده تا روی
/// SQL Server به REPLACE تودرتو تبدیل شود، نه ارزیابی در حافظه.
/// </para>
/// </summary>
public static class PersianSearch
{
    /// <summary>نرمال‌سازی کامل برای عبارت جست‌وجو و مقایسه‌های درون‌حافظه.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            // اعراب و کشیده حذف می‌شوند
            if (IsDiacritic(ch) || ch == 'ـ')
                continue;

            builder.Append(ch switch
            {
                'ي' => 'ی',
                'ك' => 'ک',
                'ة' => 'ه',
                '۰' or '٠' => '0',
                '۱' or '١' => '1',
                '۲' or '٢' => '2',
                '۳' or '٣' => '3',
                '۴' or '٤' => '4',
                '۵' or '٥' => '5',
                '۶' or '٦' => '6',
                '۷' or '٧' => '7',
                '۸' or '٨' => '8',
                '۹' or '٩' => '9',
                _ => ch
            });
        }

        return CollapseSpaces(builder.ToString());
    }

    /// <summary>فقط ارقام به انگلیسی + trim — برای شماره موبایل پیش از ذخیره/جست‌وجو.</summary>
    public static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var builder = new StringBuilder(phone.Length);
        foreach (var ch in phone.Trim())
        {
            builder.Append(ch switch
            {
                '۰' or '٠' => '0',
                '۱' or '١' => '1',
                '۲' or '٢' => '2',
                '۳' or '٣' => '3',
                '۴' or '٤' => '4',
                '۵' or '٥' => '5',
                '۶' or '٦' => '6',
                '۷' or '٧' => '7',
                '۸' or '٨' => '8',
                '۹' or '٩' => '9',
                _ => ch
            });
        }

        return builder.ToString();
    }

    private static bool IsDiacritic(char ch) =>
        (ch >= '\u064B' && ch <= '\u0652') || ch == '\u0670';

    private static string CollapseSpaces(string text)
    {
        var parts = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts);
    }

    private static readonly (string From, string To)[] LetterReplacements =
    [
        ("ي", "ی"),
        ("ك", "ک"),
        ("ة", "ه")
    ];

    private static readonly System.Reflection.MethodInfo ReplaceMethod =
        typeof(string).GetMethod(nameof(string.Replace),
            [typeof(string), typeof(string)])!;

    private static readonly System.Reflection.MethodInfo ContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains),
            [typeof(string)])!;

    /// <summary>
    /// عبارت «هر کدام از این ستون‌ها (پس از نرمال‌سازی حروف) شامل عبارتِ نرمال‌شده باشد».
    /// عمداً overloadهای دوآرگومانی که EF به REPLACE/CHARINDEX ترجمه می‌کند استفاده شده؛
    /// حساسیت حروف مثل قبل از collation دیتابیس می‌آید.
    /// </summary>
    public static Expression<Func<T, bool>> ContainsNormalized<T>(
        string normalizedTerm,
        params Expression<Func<T, string?>>[] properties)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? body = null;

        foreach (var property in properties)
        {
            var inlined = new ParameterReplacer(property.Parameters[0], parameter)
                .Visit(property.Body)!;

            Expression normalized = inlined;
            foreach (var (from, to) in LetterReplacements)
            {
                normalized = Expression.Call(normalized, ReplaceMethod,
                    Expression.Constant(from), Expression.Constant(to));
            }

            var contains = Expression.Call(normalized, ContainsMethod,
                Expression.Constant(normalizedTerm));

            var notNull = Expression.NotEqual(inlined, Expression.Constant(null, typeof(string)));
            var clause = Expression.AndAlso(notNull, contains);
            body = body is null ? clause : Expression.OrElse(body, clause);
        }

        return Expression.Lambda<Func<T, bool>>(
            body ?? Expression.Constant(false), parameter);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == from ? to : base.VisitParameter(node);
    }
}
