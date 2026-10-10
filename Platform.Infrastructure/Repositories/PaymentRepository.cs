using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PlatformDbContext _context;

    public PaymentRepository(PlatformDbContext context) => _context = context;

    public async Task AddAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
    }

    public Task<Payment?> GetByAuthorityAsync(string authority) =>
        _context.Payments.FirstOrDefaultAsync(p => p.Authority == authority);

    public Task<Payment?> GetByIdForUserAsync(int paymentId, string userId) =>
        _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);
}
