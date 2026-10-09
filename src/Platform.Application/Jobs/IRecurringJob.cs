namespace Platform.Application.Jobs;

/// <summary>
/// یک کار تکرارشونده (یادآوری، پاک‌سازی، دایجست…). ماژول‌ها با ثبت این در DI
/// (AddSingleton&lt;IRecurringJob, …&gt;) کارشان را به <c>RecurringJobRunner</c> می‌سپارند
/// و دیگر BackgroundService جدا نمی‌خواهند.
/// </summary>
public interface IRecurringJob
{
    /// <summary>نام یکتا برای لاگ (مثلاً «otp-cleanup»).</summary>
    string Name { get; }

    /// <summary>فاصلهٔ بین دو اجرا.</summary>
    TimeSpan Interval { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
}
