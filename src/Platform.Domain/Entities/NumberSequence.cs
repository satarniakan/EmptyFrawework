using System.ComponentModel.DataAnnotations;

namespace Platform.Domain.Entities;

/// <summary>
/// شمارندهٔ همروند-امن برای شماره‌گذاری اسناد (فاکتور، سند، پرونده…).
/// هر نام یک دنبالهٔ مستقل است؛ مقدارها پشت‌سرهم و بدون تکرار صادر می‌شوند.
/// </summary>
public class NumberSequence
{
    /// <summary>نام دنباله، مثل «invoice» یا «contract-1405».</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>آخرین عدد صادرشده.</summary>
    public long LastValue { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// نگهبان همزمانی: دو درخواست موازی که هم‌زمان بخوانند، فقط یکی commit می‌شود
    /// و دیگری با DbUpdateConcurrencyException به حلقهٔ retry برمی‌گردد.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
