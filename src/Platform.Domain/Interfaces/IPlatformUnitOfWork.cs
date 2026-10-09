namespace Platform.Domain.Interfaces;

/// <summary>
/// واحد کارِ پایه. فقط ریپازیتوری‌های زیرساختی را در اختیار می‌گذارد تا پایه
/// به هیچ مفهوم دامنه‌ای (کالا، انبار، سند حسابداری و…) وابسته نباشد.
/// پروژهٔ مصرف‌کننده یک <c>IDomainUnitOfWork : IPlatformUnitOfWork</c> می‌سازد
/// و ریپازیتوری‌های خودش را به آن اضافه می‌کند.
/// </summary>
public interface IPlatformUnitOfWork
{
    IOtpRepository OtpCodes { get; }
    IAuditLogRepository AuditLogs { get; }
    INotificationRepository Notifications { get; }
    IOutboxRepository Outbox { get; }
    IPaymentRepository Payments { get; }

    /// <summary>تغییرات track‌شده را commit می‌کند و تعداد ردیف‌های تغییریافته را برمی‌گرداند.</summary>
    Task<int> CompleteAsync();

    /// <summary>
    /// اجرای یک عملیات درون تراکنش. لازم است وقتی یک سرویس بیش از یک
    /// <see cref="CompleteAsync"/> دارد، چون حالت اجراییِ SQL با
    /// <c>EnableRetryOnFailure</c> سازگار نیست و فراخوانی مستقیم آن خطا می‌دهد.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action);

    /// <summary>
    /// پاک کردن change tracker — لازم پیش از rollback یا تلاش دوباره، وگرنه
    /// entityهای Added/Modified دوباره ثبت می‌شوند و عملیات تکراری می‌سازند.
    /// </summary>
    void ClearChangeTracker();
}