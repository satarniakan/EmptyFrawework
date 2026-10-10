using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Repositories;

public class NumberSequenceRepository : INumberSequenceRepository
{
    private readonly PlatformDbContext _context;

    public NumberSequenceRepository(PlatformDbContext context) => _context = context;

    public Task<NumberSequence?> GetAsync(string name) =>
        _context.NumberSequences.FirstOrDefaultAsync(s => s.Name == name);

    public async Task AddAsync(NumberSequence sequence)
    {
        await _context.NumberSequences.AddAsync(sequence);
    }
}
