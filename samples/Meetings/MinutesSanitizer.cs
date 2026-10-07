using System.Text.RegularExpressions;

namespace Meetings;

/// <summary>
/// پاک‌سازی حداقلی HTML صورت‌جلسه پیش از ذخیره. متن فقط توسط ادمین‌ها نوشته می‌شود،
/// ولی چون برای همهٔ اعضا نمایش داده می‌شود، عناصر خطرناک (script و رویدادهای inline
/// و href جاوااسکریپتی) پیش از ذخیره حذف می‌شوند.
/// </summary>
public static partial class MinutesSanitizer
{
    [GeneratedRegex(@"<\s*(script|style|iframe|object|embed|form|link|meta)\b[^>]*>.*?<\s*/\s*\1\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex BlockedElementWithBody();

    [GeneratedRegex(@"<\s*/?\s*(script|style|iframe|object|embed|form|link|meta)\b[^>]*/?>",
        RegexOptions.IgnoreCase)]
    private static partial Regex BlockedElementTag();

    [GeneratedRegex(@"\son\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase)]
    private static partial Regex InlineEventHandler();

    [GeneratedRegex(@"(href|src)\s*=\s*(""[^""]*javascript:[^""]*""|'[^']*javascript:[^']*'|javascript:[^\s>]+)",
        RegexOptions.IgnoreCase)]
    private static partial Regex JavaScriptUrl();

    public static string Clean(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var clean = html.Trim();
        clean = BlockedElementWithBody().Replace(clean, string.Empty);
        clean = BlockedElementTag().Replace(clean, string.Empty);
        clean = InlineEventHandler().Replace(clean, string.Empty);
        clean = JavaScriptUrl().Replace(clean, @"$1=""#""");
        return clean;
    }
}
