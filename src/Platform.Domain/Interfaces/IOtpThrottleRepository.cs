using Platform.Domain.Entities;

namespace Platform.Domain.Interfaces;

public interface IOtpThrottleRepository
{
    Task<OtpThrottle?> GetByPhoneAsync(string phoneNumber);

    Task AddAsync(OtpThrottle throttle);

    /// <summary>
    /// حذف رکوردهای کهنه‌ای که هر دو پنجره‌شان قدیمی‌تر از cutoff است (یا هرگز باز نشده).
    /// جدول فقط به‌ازای شماره‌های فعال رشد می‌کند. بازگشت: تعداد حذف‌شده.
    /// </summary>
    Task<int> DeleteStaleAsync(DateTime cutoffUtc);
}
