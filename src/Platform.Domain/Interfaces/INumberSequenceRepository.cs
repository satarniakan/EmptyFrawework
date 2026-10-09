using Platform.Domain.Entities;

namespace Platform.Domain.Interfaces;

public interface INumberSequenceRepository
{
    Task<NumberSequence?> GetAsync(string name);

    Task AddAsync(NumberSequence sequence);
}
