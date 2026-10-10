// Platform.Infrastructure/Repositories/AuditLogRepository.cs
using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;
using Platform.Domain.Interfaces;
using Platform.Domain.Queries;
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
        {
            var term = PersianSearch.Normalize(search);
            if (term.Length > 0)
            {
                query = query.Where(PersianSearch.ContainsNormalized<AuditLog>(
                    term, a => a.EventType, a => a.UserEmail, a => a.Details));
            }
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}