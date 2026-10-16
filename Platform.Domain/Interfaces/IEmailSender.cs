// Platform.Domain/Interfaces/IEmailSender.cs
namespace Platform.Domain.Interfaces;

/// <summary>ارسال ایمیل تراکنشی — پیاده‌سازی SMTP در Infrastructure</summary>
public interface IEmailSender
{
    /// <summary>ارسال متن ساده (غیرHTML).</summary>
    Task SendAsync(string to, string subject, string body);

    /// <summary>
    /// ارسال HTML. پیش‌فرض پیاده‌سازی‌های قدیمیِ فقط-متن، همان متن ساده است تا
    /// شکسته نشوند؛ پیاده‌سازی MailKit اینجا واقعاً HTML می‌فرستد.
    /// </summary>
    Task SendHtmlAsync(string to, string subject, string htmlBody) => SendAsync(to, subject, htmlBody);
}
