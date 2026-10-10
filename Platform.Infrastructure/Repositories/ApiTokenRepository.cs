using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Repositories;

public class ApiTokenRepository : IApiTokenRepository
{
    private readonly PlatformDbContext _context;

    public ApiTokenRepository(PlatformDbContext context) => _context = context;

    public Task<UserApiToken?> GetByHashAsync(string tokenHash) =>
        _context.ApiTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    public async Task AddAsync(UserApiToken token)
    {
        await _context.ApiTokens.AddAsync(token);
    }

    public Task<List<UserApiToken>> GetActiveForUserAsync(string userId) =>
        _context.ApiTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync();

    public Task<int> DeleteExpiredAsync(DateTime cutoffUtc) =>
        _context.ApiTokens
            .Where(t => (t.ExpiresAtUtc < cutoffUtc || t.IsRevoked) && t.CreatedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync();
}

public class LoginHistoryRepository : ILoginHistoryRepository
{
    private readonly PlatformDbContext _context;

    public LoginHistoryRepository(PlatformDbContext context) => _context = context;

    public async Task AddAsync(LoginHistory entry)
    {
        await _context.LoginHistories.AddAsync(entry);
    }

    public Task<List<LoginHistory>> GetRecentForUserAsync(string userId, int take = 20) =>
        _context.LoginHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.OccurredAtUtc)
            .Take(take)
            .ToListAsync();
}
