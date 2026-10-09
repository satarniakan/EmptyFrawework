using Stream = System.IO.Stream;

namespace Platform.Domain.Interfaces;

/// <summary>
/// ذخیره‌سازی فایل‌های بالا‌آمده از کاربر. نسبت به <c>HttpContext</c> تمیزتر است،
/// چون **هیچ‌وقت آدرس فایل را بر اساس نام فایل کاربر می‌سازد**.
/// <para>
/// <c>SaveAsync</c> شناسه‌ی فایل (storage ID) برمی‌گرداند — این شناسه، نام فایل روی دیسک نیست.
/// چون شناسه‌ی ناگهانی است، کاربران نمی‌توانند فایل‌ها را لیست کنند، لیست کنند یا تخریب کنند؛
/// حذف فایل‌ها تنها از طریق مجموعه‌ای که این فایل را به‌همراه آن‌ساخته است، امکان‌پذیر است.
/// </para>
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// فایل را ذخیره می‌کند و <c>storageId</c> را برمی‌گرداند.
    /// <paramref name="ownerId"/> که ساخت کار است؛ می‌تواند قالب حذف شود
    /// برای حذف سراسری فایل‌ها.
    /// <paramref name="fileType"/> کلید برچسب‌گذاری آن فایل است (مثلاً «audio»، «photo»، «document»).
    /// <paramref name="stream"/> جریان داده؛ پس‌زمینه موجود از <paramref name="fileName"/>
    /// برای پسوند پیکربندی می‌شود و نباید از <paramref name="fileName"/> به‌عنوان آدرس یا نام
    /// فایل کلیدی در کد استفاده شود.
    /// </summary>
    /// <exception cref="ArgumentNullException">اگر <paramref name="stream"/> خالی است.</exception>
    /// <exception cref="ArgumentException">اگر پسوند <paramref name="fileName"/> به‌عنوان نام
    /// فایل یا محتوا قابل قبول نیست، یا <paramref name="ownerId"/> خالی است.</exception>
    Task<string> SaveAsync(
        string? ownerId, string fileType, Stream stream, string fileName, string? contentType = null);

    /// <summary>حذف فایل با <paramref name="storageId"/> آن از پوشهٔ <paramref name="fileType"/>
    /// مملک <paramref name="ownerId"/> خاص.</summary>
    Task DeleteAsync(string ownerId, string fileType, string storageId);

    /// <summary>آیا فایل با <paramref name="storageId"/> در <paramref name="fileType" />
    /// مملک <paramref name="ownerId" /> وجود دارد؟</summary>
    Task<bool> ExistsAsync(string ownerId, string fileType, string storageId);

    /// <summary>
    /// بازکردن فایل برای خواندن (مثلاً برای دانلود). فراخواننده مالک stream است و
    /// باید آن را dispose کند. اگر فایل نباشد null برمی‌گردد.
    /// </summary>
    Task<Stream?> OpenReadAsync(string ownerId, string fileType, string storageId);
}
