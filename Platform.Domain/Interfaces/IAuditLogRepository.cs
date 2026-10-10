// Platform.Domain/Interfaces/IAuditLogRepository.cs
using Platform.Domain.Entities;

namespace Platform.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry);
    Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100);
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
}