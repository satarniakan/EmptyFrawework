using Ganss.Xss;

namespace Meetings;

/// <summary>
/// پاک‌سازی HTML صورت‌جلسه پیش از ذخیره، با allowlist واقعی (HtmlSanitizer) نه regex.
/// <para>
/// چرا نه regex: HTML را نمی‌شود با عبارت منظم امن کرد — ترکیب attributeها،
/// encodingها و تگ‌های ناقص همیشه یک بایپس دارد. این متن توسط ادمین نوشته ولی برای
/// همهٔ اعضا با <c>MarkupString</c> رندر می‌شود، پس XSS ذخیره‌شده یعنی اجرای اسکریپت
/// در مرورگر قربانی. کتابخانه فقط تگ‌ها/attributeهای شناخته‌شدهٔ امن را نگه می‌دارد.
/// </para>
/// </summary>
public static class MinutesSanitizer
{
    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        // ویرایشگر فارسی (متن/جدول/فهرست/پیوند/راست‌چین) — فقط همین‌ها لازم‌اند
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
            { "p", "br", "strong", "b", "em", "i", "u", "s", "ul", "ol", "li",
              "h1", "h2", "h3", "h4", "blockquote", "a", "table", "thead", "tbody",
              "tr", "th", "td", "span", "div", "hr" })
        {
            sanitizer.AllowedTags.Add(tag);
        }

        sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in new[] { "href", "title", "dir", "align", "colspan", "rowspan" })
        {
            sanitizer.AllowedAttributes.Add(attribute);
        }

        // فقط http/https/mailto — javascript: و data: حذف می‌شوند
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("mailto");

        // استایل inline و class حذف می‌شود (class می‌توانست برای exfiltration استفاده شود)
        sanitizer.AllowedCssProperties.Clear();

        return sanitizer;
    }

    public static string Clean(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        return Sanitizer.Sanitize(html.Trim());
    }
}
