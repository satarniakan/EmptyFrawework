// Platform.Infrastructure/Services/SmtpEmailSender.cs
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Platform.Application.Services;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;

namespace Platform.Infrastructure.Services;

/// <summary>
/// ارسال ایمیل با SMTP (بدون وابستگی به پکیج بیرونی).
/// مقادیر مؤثر هر بار ارسال خوانده می‌شوند: ردیف دیتابیس ← appsettings
/// («Email:Smtp:Host/Port/Username/Password/FromAddress/FromName»).
/// Host خالی یعنی پیکربندی نشده.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly ISettingService _settings;
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(ISettingService settings, IConfiguration configuration)
    {
        _settings = settings;
        _configuration = configuration;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var host = await _settings.GetEffectiveAsync(IntegrationSettingKeys.SmtpHost, "Email:Smtp:Host");
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("پیکربندی ایمیل ناقص است (Email:Smtp:Host).");

        var portText = await _settings.GetEffectiveAsync(
            IntegrationSettingKeys.SmtpPort, "Email:Smtp:Port", "587");
        var username = await _settings.GetEffectiveAsync(
            IntegrationSettingKeys.SmtpUsername, "Email:Smtp:Username");
        var password = await _settings.GetEffectiveAsync(
            IntegrationSettingKeys.SmtpPassword, "Email:Smtp:Password");
        var fromAddress = await _settings.GetEffectiveAsync(
            IntegrationSettingKeys.SmtpFromAddress, "Email:Smtp:FromAddress", "no-reply@localhost");
        var fromName = await _settings.GetEffectiveAsync(
            IntegrationSettingKeys.SmtpFromName, "Email:Smtp:FromName", "سامانه");

        var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
            SubjectEncoding = System.Text.Encoding.UTF8,
            BodyEncoding = System.Text.Encoding.UTF8
        };
        message.To.Add(to);

        using var client = new SmtpClient(host, int.TryParse(portText, out var port) ? port : 587)
        {
            EnableSsl = true,
            Credentials = string.IsNullOrWhiteSpace(username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(username, password)
        };

        // await الزامی است: با بازگرداندن Task، «using var client» پیش از اتمام
        // ارسال دیسپوز می‌شد و هر ایمیل با ObjectDisposedException شکست می‌خورد.
        await client.SendMailAsync(message);
    }
}
