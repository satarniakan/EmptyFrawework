using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IO;
using Platform.Domain.Interfaces;

namespace Platform.Infrastructure.Services;

/// <summary>
/// پیاده‌سازی مکان‌محور برای <see cref="IFileStorage"/> برای فایل‌های کوچک تا محدود <c>Storage:MaxFileSizeBytes</c>.
/// <para>
/// ساختار پوشه‌ها به صورت <c>Root/{۸کاراکتریٔ مملک}/{Type}/{storageId}</c> است تا:
/// <list type="bullet">
/// <item>جلوگیری از لیست‌زدایی مدار فایل‌ها توسط کاربر (مسیرها ناشناس و تک‌اطلاعیه‌ای هستند).</item>
/// <item>جلوگیری از همپوشانی با نام‌های قابل پیش‌بینی Hazards (شناسه‌ی ناگهانی).</item>
/// <item>دراپ کامل فایل‌ها با <c>DeleteAsync</c> ساده است — یک پوشه = یک فایل.</item>
/// </list>
/// </para>
/// <para>
/// نکات امنیتی: هیچ بخشی از مسیر از ورودی کاربر ساخته نمی‌شود (فقط هش owner و
/// شناسهٔ تولیدشده)؛ با این حال هر مسیر نهایی بررسی می‌شود که زیر Root بماند
/// (دفاع در عمق در برابر <c>../</c> در fileType یا رکوردهای قدیمی). نوشتن اتمیک
/// است (فایل موقت + جابه‌جایی) و استریم‌محور — بافر ۵۰ مگابایتی در حافظه نگه داشته نمی‌شود.
/// </para>
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private const int CopyBufferSize = 81920;

    private readonly IConfiguration _configuration;
    private readonly string _rootPath;
    private readonly long _maxFileSizeBytes;
    private readonly long _maxFileSizeMb;
    private readonly HashSet<string> _allowedExtensions;

    public LocalFileStorage(IConfiguration configuration)
    {
        _configuration = configuration;
        _rootPath = configuration["Storage:RootPath"] ?? "AppData";

        if (!long.TryParse(configuration["Storage:MaxFileSizeBytes"], out var max))
            max = 50 * 1024 * 1024;
        _maxFileSizeBytes = max;
        _maxFileSizeMb = max / (1024 * 1024);

        var allowed = configuration["Storage:AllowedExtensions"] ?? "jpg,jpeg,png,gif,bmp,webp,mp3,mp4,pdf,docx,xlsx,pptx,zip";
        _allowedExtensions = allowed
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
    }

    public async Task<string> SaveAsync(string? ownerId, string fileType, Stream stream, string fileName, string? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (stream.CanRead == false)
            throw new ArgumentException("جریان فایل قابل خواندن نیست.", nameof(stream));
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("مشخصهٔ مملک فایل الزامی است تا مسیر ذخیره‌سازی امن تولید شود.", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(fileType))
            throw new ArgumentException("نوع فایل الزامی است.", nameof(fileType));

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            throw new ArgumentException("نام فایل باید پسوند داشته باشد.", nameof(fileName));

        var extensionWithoutDot = extension.TrimStart('.').ToLowerInvariant();
        if (!_allowedExtensions.Contains(extensionWithoutDot))
            throw new ArgumentException($"پسوند '{extensionWithoutDot}' مجاز نیست و فایل نمی‌تواند ذخیره شود.", nameof(fileName));

        var storageId = $"{Guid.NewGuid()}{extension}";
        var fullPath = ResolveWithinRoot(Path.Combine(Hash8(ownerId), fileType, storageId));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // نوشتن اتمیک: اول فایل موقت، بعد جابه‌جایی. اگر وسط راه خطا بیاید،
        // فایل نیمه‌کاره با نام نهایی باقی نمی‌ماند.
        var tempPath = fullPath + $".tmp-{Guid.NewGuid():N}";
        try
        {
            await using (var target = new FileStream(
                tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: CopyBufferSize, FileOptions.Asynchronous))
            {
                var buffer = new byte[CopyBufferSize];
                long total = 0;
                int read;
                while ((read = await stream.ReadAsync(buffer)) > 0)
                {
                    total += read;
                    if (total > _maxFileSizeBytes)
                        throw new ArgumentException(
                            $"حجم فایل از {MaxFileSizeLabel()} بزرگ شده است.", nameof(fileName));

                    await target.WriteAsync(buffer.AsMemory(0, read));
                }

                if (total == 0)
                    throw new ArgumentException("فایل خالی است.", nameof(stream));
            }

            File.Move(tempPath, fullPath);
        }
        catch
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch { /* پاک‌سازی موقت نباید خطای اصلی را بپوشاند */ }
            throw;
        }

        return storageId;
    }

    public Task DeleteAsync(string ownerId, string fileType, string storageId)
    {
        if (string.IsNullOrWhiteSpace(storageId))
            return Task.CompletedTask;

        var fullPath = ResolveWithinRoot(Path.Combine(Hash8(ownerId), fileType, storageId));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string ownerId, string fileType, string storageId)
    {
        if (string.IsNullOrWhiteSpace(storageId))
            return Task.FromResult(false);

        var fullPath = ResolveWithinRoot(Path.Combine(Hash8(ownerId), fileType, storageId));
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<Stream?> OpenReadAsync(string ownerId, string fileType, string storageId)
    {
        if (string.IsNullOrWhiteSpace(storageId))
            return Task.FromResult<Stream?>(null);

        var fullPath = ResolveWithinRoot(Path.Combine(Hash8(ownerId), fileType, storageId));
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: CopyBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    /// <summary>
    /// مسیر نهایی باید زیر Root بماند؛ وگرنه <c>../</c> در fileType یا storageId
    /// (مثلاً از رکوردهای قدیمیِ پیش از این گارد) می‌توانست به بیرون بنویسد/بخواند.
    /// </summary>
    private string ResolveWithinRoot(string relativePath)
    {
        var root = Path.GetFullPath(_rootPath);
        var full = Path.GetFullPath(Path.Combine(root, relativePath));

        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("مسیر فایل خارج از پوشهٔ ذخیره‌سازی است.");

        return full;
    }

    private static string Hash8(string input) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).AsSpan(0, 8).ToString();

    private string MaxFileSizeLabel() =>
        $"~{Math.Round((double)_maxFileSizeMb, 1):F1} مگابایت";
}
