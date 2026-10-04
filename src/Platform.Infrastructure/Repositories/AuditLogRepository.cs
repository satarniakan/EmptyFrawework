// Platform.Infrastructure/Repositories/AuditLogRepository.cs
using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly PlatformDbContext _context;

    public AuditLogRepository(PlatformDbContext context) => _context = context;

    public async Task AddAsync(AuditLog entry)
    {
        await _context.AuditLogs.AddAsync(entry);
        //await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100) =>
        await _context.AuditLogs
            .OrderByDescending(a => a.OccurredAt)
            .Take(count)
            .ToListAsync();
    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a =>
                a.EventType.Contains(search) ||
                (a.UserEmail != null && a.UserEmail.Contains(search)) ||
                a.Details.Contains(search));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}