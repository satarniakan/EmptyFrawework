using Platform.Domain.Entities;
using Platform.Domain.Enums;
using Platform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Platform.Application.Services;

/// <summary>
/// صف پیام‌ها (الگوی Outbox) — فراخوانی‌ها فقط رکورد می‌سازند و سریع برمی‌گردند؛
/// ارسال واقعی توسط OutboxProcessor انجام می‌شود تا اختلال سرویس بیرونی،
/// جریان اصلی را نشکند.
/// پیامک‌های فوری (مثل OTP) از این مسیر رد نمی‌شوند و مستقیم می‌روند.
/// </summary>
public interface IOutboxService
{
    Task QueueSmsAsync(string phone, string body);

    Task QueueEmailAsync(string to, string subject, string body);
}

public class OutboxService : IOutboxService
{
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(IPlatformUnitOfWork unitOfWork, ILogger<OutboxService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task QueueSmsAsync(string phone, string body) =>
        QueueAsync(OutboxChannel.Sms, phone, null, body);

    public Task QueueEmailAsync(string to, string subject, string body) =>
        QueueAsync(OutboxChannel.Email, to, subject, body);

    private async Task QueueAsync(OutboxChannel channel, string recipient, string? subject, string body)
    {
        try
        {
            await _unitOfWork.Outbox.AddAsync(new OutboxMessage
            {
                Channel = channel,
                Recipient = recipient,
                Subject = subject,
                Body = body
            });
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            // صف نباید جریان اصلی را بشکند؛ فقط لاگ می‌شود
            _logger.LogError(ex, "خطا در ثبت پیام در صف برای {Recipient}", recipient);
        }
    }
}