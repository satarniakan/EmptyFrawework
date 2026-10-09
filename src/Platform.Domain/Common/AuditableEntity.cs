namespace Platform.Domain.Common;

/// <summary>
/// نشانهٔ «قابل‌حذف‌نرم». انتیتی‌هایی که این را پیاده می‌کنند، حذفِ واقعی نمی‌شوند:
/// <c>PlatformDbContext</c> حالت <c>Deleted</c> را به <c>IsDeleted = true</c> تبدیل می‌کند
/// و کوئری‌های خواندن آن را نمی‌بینند.
/// <para>
/// مهم: اگر یک انتیتیِ دارای رابطهٔ «الزامی» با فرزندانش نرم حذف شود، خودِ فرزندان هم باید
/// نرم حذف شوند، وگرنه رکورد یتیم با مقدار <c>null</c> در navigation می‌آید. پس الگو این است:
/// کل خانوادهٔ یک ماژول از یک پایهٔ نرم‌حذفی ارث می‌برند.
/// </para>
/// </summary>
public interface ISoftDeletable
{
    /// <summary>آیا این رکورد حذف شده است؟ کوئری‌های خواندن فقط <c>false</c> را برمی‌گردانند.</summary>
    bool IsDeleted { get; set; }
}

/// <summary>
/// پایهٔ مشترک انتیتی‌های دامنه: شناسه، ردِ پیگیریِ ایجاد/ویرایش و امکان حذف نرم.
/// <para>
/// فقط چیزهایی اینجاست که <b>هر</b> دامنه‌ای لازم دارد. فیلدهای دامنه‌ای (عنوان، زمان جلسه،
/// مبلغ و…) باید در خودِ انتیتی بمانند تا پایه به هیچ ماژولی وابسته نشود.
/// </para>
/// <para>
/// <see cref="CreatedAt"/> عمداً پیش‌مقدار UTC دارد؛ پرکردن خودکار در
/// <c>PlatformDbContext.SaveChanges</c> انجام می‌شود و فقط مقدارِ پیش‌فرض را رعایت می‌کند.
/// </para>
/// </summary>
public abstract class AuditableEntity : ISoftDeletable
{
    /// <summary>شناسهٔ سراسری ردیف.</summary>
    public int Id { get; set; }

    /// <summary>
    /// کاربر سازنده (<c>IdentityUser.Id</c>). رشتهٔ خالی یعنی سیستم/کار مهمان.
    /// خالیِ اولیه نگه داشته می‌شود چون بسیاری از عملیات سیستمی (سید، صف) کاربر ندارند.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// زمان ایجاد (UTC). عمداً پیش‌مقدار ندارد تا پیش از commit مقدار دلخواهِ
    /// فراخواننده (برای داده‌های ورودی/برون‌بری) حفظ شود؛ اگر خالی مانده باشد
    /// <c>PlatformDbContext</c> آن را از <c>TimeProvider</c> پر می‌کند.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>آخرین ویرایش‌کننده؛ null یعنی هنوز ویرایش نشده یا توسط سیستم.</summary>
    public string? UpdatedByUserId { get; set; }

    /// <summary>زمان آخرین ویرایش (UTC)؛ null یعنی هنوز ویرایش نشده.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>پرچم حذف نرم. مستقیم دست نزنید — توسط DbContext از روی حالت tracking تنظیم می‌شود.</summary>
    public bool IsDeleted { get; set; }
}
