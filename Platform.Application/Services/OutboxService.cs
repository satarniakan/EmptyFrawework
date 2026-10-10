using Platform.Application.DTOs;
using Platform.Application.Queries;
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

    /// <summary>
    /// اعلان وب‌پوش در صف: userId شناسهٔ کاربر است و ارسال واقعی را
    /// OutboxProcessor با IPushNotificationService انجام می‌دهد.
    /// </summary>
    Task QueuePushAsync(string userId, string title, string? body, string? linkUrl = null);

    /// <summary>فهرست صفحه‌بندی‌شده برای صفحهٔ نظارت ادمین.</summary>
    Task<PagedResult<OutboxMessage>> GetMessagesPagedAsync(
        OutboxChannel? channel, OutboxStatus? status, int page, int pageSize);

    /// <summary>بازگرداندن پیام Failed به صف برای تلاش دوبارهٔ دستی.</summary>
    Task<bool> RetryAsync(int messageId);
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

    public Task QueuePushAsync(string userId, string title, string? body, string? linkUrl = null) =>
        QueueAsync(OutboxChannel.Push, userId, title, body ?? string.Empty, linkUrl);

    public async Task<PagedResult<OutboxMessage>> GetMessagesPagedAsync(
        OutboxChannel? channel, OutboxStatus? status, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > PagedQueryExtensions.MaxPageSize) pageSize = PagedQueryExtensions.MaxPageSize;

        var (items, totalCount) = await _unitOfWork.Outbox.GetPagedAsync(channel, status, page, pageSize);

        return new PagedResult<OutboxMessage>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<bool> RetryAsync(int messageId) =>
        _unitOfWork.Outbox.RequeueAsync(messageId);

    private async Task QueueAsync(OutboxChannel channel, string recipient, string? subject,
        string body, string? linkUrl = null)
    {
        // عمداً try/catch ندارد: اگر ثبت در صف شکست بخورد، باید خطا به فراخواننده برسد تا
        // تراکنشِ عملیات اصلی rollback شود. بلعیدن خطا یعنی «عملیات موفق، پیام گم» —
        // وضعیتی که هیچ‌وقت نباید بی‌صدا رخ دهد. ارسالِ واقعیِ پیام‌ها (که ممکن است به
        // سرویس بیرونی وصل نشود) جدا است و OutboxProcessor آن را با retry مدیریت می‌کند.
        await _unitOfWork.Outbox.AddAsync(new OutboxMessage
        {
            Channel = channel,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            LinkUrl = linkUrl
        });
        await _unitOfWork.CompleteAsync();
    }
}