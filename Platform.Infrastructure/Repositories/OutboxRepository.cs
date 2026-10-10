// Platform.Infrastructure/Repositories/OutboxRepository.cs
using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;
using Platform.Domain.Enums;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly PlatformDbContext _context;

    public OutboxRepository(PlatformDbContext context) => _context = context;

    // AsNoTracking: نوشتنِ وضعیت با ExecuteUpdateهای شرطی پایین انجام می‌شود؛
    // track‌کردن این رکوردها باعث می‌شد SaveChanges بعدی، مقدارِ کهنهٔ وضعیت را بازنویسی کند.
    public async Task<List<OutboxMessage>> GetPendingAsync(OutboxChannel channel, int maxAttempts, int take) =>
        await _context.OutboxMessages
            .AsNoTracking()
            .Where(m => m.Channel == channel && m.Status == OutboxStatus.Pending && m.Attempts < maxAttempts)
            .OrderBy(m => m.CreatedAt)
            .Take(take)
            .ToListAsync();

    public async Task AddAsync(OutboxMessage message) =>
        await _context.OutboxMessages.AddAsync(message);

    public Task UpdateAsync(OutboxMessage message)
    {
        _context.OutboxMessages.Update(message);
        return Task.CompletedTask;
    }

    public async Task<bool> TryClaimForSendingAsync(int messageId, int maxAttempts, DateTime now)
    {
        var rows = await _context.OutboxMessages
            .Where(m => m.Id == messageId && m.Status == OutboxStatus.Pending && m.Attempts < maxAttempts)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, OutboxStatus.Processing)
                .SetProperty(m => m.Attempts, m => m.Attempts + 1)
                // «شروع قفل»: مبنای تشخیص رکورد بی‌صاحب از ارسالِ جاریِ instance دیگر
                .SetProperty(m => m.ProcessingStartedAt, now));
        return rows > 0;
    }

    public async Task FinishSendingAsync(int messageId, bool success, int attempts, int maxAttempts,
        string? error, DateTime now)
    {
        var target = success
            ? OutboxStatus.Sent
            : attempts >= maxAttempts ? OutboxStatus.Failed : OutboxStatus.Pending;
        var sentAt = success ? (DateTime?)now : null;

        await _context.OutboxMessages
            .Where(m => m.Id == messageId && m.Status == OutboxStatus.Processing)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, target)
                .SetProperty(m => m.SentAt, sentAt)
                .SetProperty(m => m.LastError, success ? null : error));
    }

    public async Task<int> ReclaimAbandonedAsync(TimeSpan lease)
    {
        // آستانه در C# حساب می‌شود: `DateTime.UtcNow - lease` داخل کوئری در EF Core 10
        // ترجمه نمی‌شود و ExecuteUpdate کل عبارت را بی‌ترجمه می‌کند.
        var cutoff = DateTime.UtcNow - lease;

        return await _context.OutboxMessages
            // رکورد Processingِ تازه = «جاری» (نه یتیم). بدون این آستانه، در استقرار
            // چندنمونه‌ای پیامِ در دستِ instance دیگر فوراً به Pending برمی‌گشت و دوباره ارسال می‌شد.
            // ProcessingStartedAt برای رکوردهای قدیمی‌تر از این ستون null است؛ آن‌ها هم باید
            // آزاد شوند، وگرنه تا ابد در Processing می‌مانند.
            .Where(m => m.Status == OutboxStatus.Processing
                     && (m.ProcessingStartedAt == null || m.ProcessingStartedAt < cutoff))
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, OutboxStatus.Pending)
                // شکلِ per-row برای null گذاشتن: ثابتِ null در SetProperty ترجمه نمی‌شود
                .SetProperty(m => m.ProcessingStartedAt, m => (DateTime?)null));
    }

    public async Task<(IEnumerable<OutboxMessage> Items, int TotalCount)> GetPagedAsync(
        OutboxChannel? channel, OutboxStatus? status, int page, int pageSize)
    {
        var query = _context.OutboxMessages.AsNoTracking().AsQueryable();

        if (channel.HasValue)
            query = query.Where(m => m.Channel == channel.Value);
        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> RequeueAsync(int messageId)
    {
        var rows = await _context.OutboxMessages
            .Where(m => m.Id == messageId && m.Status == OutboxStatus.Failed)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, OutboxStatus.Pending)
                .SetProperty(m => m.Attempts, 0)
                .SetProperty(m => m.LastError, m => (string?)null)
                .SetProperty(m => m.ProcessingStartedAt, m => (DateTime?)null));
        return rows > 0;
    }

    public Task<int> CountAsync(OutboxStatus status) =>
        _context.OutboxMessages.CountAsync(m => m.Status == status);
}
