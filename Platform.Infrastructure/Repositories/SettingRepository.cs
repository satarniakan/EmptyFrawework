using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Repositories;

public class SettingRepository : ISettingRepository
{
    private readonly PlatformDbContext _context;

    public SettingRepository(PlatformDbContext context) => _context = context;

    public Task<Setting?> GetAsync(string key) =>
        _context.Settings.FirstOrDefaultAsync(s => s.Key == key);

    public Task<List<Setting>> GetAllAsync() =>
        _context.Settings.OrderBy(s => s.Key).ToListAsync();

    public async Task UpsertAsync(Setting setting)
    {
        var existing = await _context.Settings.FirstOrDefaultAsync(s => s.Key == setting.Key);
        if (existing is null)
        {
            await _context.Settings.AddAsync(setting);
        }
        else
        {
            existing.Value = setting.Value;
            existing.UpdatedAtUtc = setting.UpdatedAtUtc;
        }
    }

    public async Task DeleteAsync(string key)
    {
        var existing = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (existing is not null)
            _context.Settings.Remove(existing);
    }
}
