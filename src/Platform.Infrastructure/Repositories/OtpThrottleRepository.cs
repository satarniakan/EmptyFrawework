using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Repositories;

public class OtpThrottleRepository : IOtpThrottleRepository
{
    private readonly PlatformDbContext _context;

    public OtpThrottleRepository(PlatformDbContext context) => _context = context;

    public Task<OtpThrottle?> GetByPhoneAsync(string phoneNumber) =>
        _context.OtpThrottles.FirstOrDefaultAsync(t => t.PhoneNumber == phoneNumber);

    public async Task AddAsync(OtpThrottle throttle)
    {
        await _context.OtpThrottles.AddAsync(throttle);
    }

    public Task<int> DeleteStaleAsync(DateTime cutoffUtc) =>
        _context.OtpThrottles
            .Where(t => (t.FailedWindowStartUtc == null || t.FailedWindowStartUtc < cutoffUtc)
                     && (t.GenerationWindowStartUtc == null || t.GenerationWindowStartUtc < cutoffUtc))
            .ExecuteDeleteAsync();
}
