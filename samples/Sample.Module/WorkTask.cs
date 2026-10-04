using Platform.Domain.Interfaces;

namespace SampleDomain;

/// <summary>
/// یک وظیفهٔ کاری ساده. این موجودیت عمداً هیچ ربطی به حسابداری/انبار ندارد؛
/// فقط نشان می‌دهد یک ماژول دامنه چطور ماژولار به دیتابیس پایه وصل می‌شود
/// و چطور از واحد کار و اعلان‌ها استفاده می‌کند.
/// </summary>
public class WorkTask
{
    public int Id { get; set; }

    /// <summary>عنوان وظیفه.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>توضیح اختیاری.</summary>
    public string? Description { get; set; }

    /// <summary>آیا انجام شده است؟</summary>
    public bool IsDone { get; set; }

    /// <summary>کاربری که وظیفه برای او ساخته شده (IdentityUser.Id).</summary>
    public string? AssignedUserId { get; set; }

    /// <summary>زمان ایجاد (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// واحد کارِ دامنهٔ نمونه: نسخهٔ پایه را گسترش می‌دهد تا سرویس‌های ماژول
/// ریپازیتوری‌های خودش را هم از یک واحد کار ببینند.
/// </summary>
public interface ISampleUnitOfWork : IPlatformUnitOfWork
{
    IWorkTaskRepository WorkTasks { get; }
}

/// <summary>ریپازیتوری وظایف.</summary>
public interface IWorkTaskRepository
{
    Task<List<WorkTask>> GetForUserAsync(string userId);
    Task<WorkTask?> GetByIdAsync(int id);
    Task AddAsync(WorkTask task);
}
