using Platform.Domain.Entities;

namespace Platform.Domain.Interfaces;

public interface ISettingRepository
{
    Task<Setting?> GetAsync(string key);

    Task<List<Setting>> GetAllAsync();

    /// <summary>درج یا به‌روزرسانی. ذخیره با CompleteAsync واحد کار است.</summary>
    Task UpsertAsync(Setting setting);

    /// <summary>حذف ردیف (برای بازگشت به مقدار appsettings/پیش‌فرض).</summary>
    Task DeleteAsync(string key);
}
