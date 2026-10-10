using Platform.Domain.Entities;

namespace Platform.Domain.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment);

    Task<Payment?> GetByAuthorityAsync(string authority);

    Task<Payment?> GetByIdForUserAsync(int paymentId, string userId);

    /// <summary>فهرست صفحه‌بندی‌شده برای صفحهٔ نظارت ادمین (جدیدترین اول).</summary>
    Task<(IEnumerable<Payment> Items, int TotalCount)> GetPagedAsync(
        PaymentStatus? status, int page, int pageSize);
}
