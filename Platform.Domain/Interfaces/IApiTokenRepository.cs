using Platform.Domain.Entities;

namespace Platform.Domain.Interfaces;

public interface IApiTokenRepository
{
    Task<UserApiToken?> GetByHashAsync(string tokenHash);

    Task AddAsync(UserApiToken token);

    Task<List<UserApiToken>> GetActiveForUserAsync(string userId);

    /// <summary>حذف فیزیکی توکن‌های منقضی/لغوشدهٔ قدیمی (خانه‌تکانی).</summary>
    Task<int> DeleteExpiredAsync(DateTime cutoffUtc);
}

public interface ILoginHistoryRepository
{
    Task AddAsync(LoginHistory entry);

    Task<List<LoginHistory>> GetRecentForUserAsync(string userId, int take = 20);

    /// <summary>حذف فیزیکی رکوردهای قدیمی‌تر از cutoff (خانه‌تکانی). بازگشت: تعداد حذف‌شده.</summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc);
}
