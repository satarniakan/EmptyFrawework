using Platform.Domain.Entities;

namespace Platform.Domain.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment);

    Task<Payment?> GetByAuthorityAsync(string authority);

    Task<Payment?> GetByIdForUserAsync(int paymentId, string userId);
}
