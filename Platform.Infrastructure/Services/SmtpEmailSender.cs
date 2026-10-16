// Platform.Infrastructure/Services/SmtpEmailSender.cs
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Platform.Application.Services;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;

namespace Platform.Infrastructure.Services;

/// <summary>
/// ارسال ایمیل با MailKit (جایگزین SmtpClient قدیمیِ منسوخ‌شده).
/// مقادیر مؤثر هر بار ارسال خوانده می‌شوند: ردیف دیتابیس ← appsettings
/// («Email:Smtp:Host/Port/Username/Password/FromAddress/FromName»).
/// Host خالی یعنی پیکربندی نشده. ارسال HTML از <see cref="SendHtmlAsync"/> و متن
/// ساده از <see cref="SendAsync"/> است؛ هر دو با STARTTLS (یا SSL صریح برای پورت ۴۶۵).
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(30);

    private readonly ISettingService _settings;
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(ISettingService settings, IConfiguration configuration)
    {
        _settings = settings;
        _configuration = configuration;
    }

    public Task SendAsync(string to, string subject, string body) =>
        SendCoreAsync(to, subject, body, isHtml: false);

    public Task SendHtmlAsync(string to, string subject, string htmlBody) =>
        SendCoreAsync(to, subject, htmlBody, isHtml: true);

    private async Task SendCoreAsync(string to, string subject, string body, bool isHtml)
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

        var port = int.TryParse(portText, out var p) ? p : 587;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = isHtml
            ? new TextPart("html") { Text = body }
            : new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        // پورت ۴۶۵ = SSL صریح؛ بقیه (معمولاً ۵۸۷) STARTTLS — بدون امن هرگز.
        var socketOptions = port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

        // timeout اتصال/ارسال با CancellationToken اعمال می‌شود (امضای ConnectAsync پارامتر timeout ندارد)
        using var timeoutCts = new CancellationTokenSource(ConnectTimeout);
        await client.ConnectAsync(host, port, socketOptions, timeoutCts.Token);

        if (!string.IsNullOrWhiteSpace(username))
            await client.AuthenticateAsync(username, password);

        try
        {
            await client.SendAsync(message);
        }
        finally
        {
            await client.DisconnectAsync(quit: true);
        }
    }
}