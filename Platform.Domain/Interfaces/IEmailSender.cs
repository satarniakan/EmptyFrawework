// Platform.Domain/Interfaces/IEmailSender.cs
namespace Platform.Domain.Interfaces;

/// <summary>ارسال ایمیل تراکنشی — پیاده‌سازی SMTP در Infrastructure</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
