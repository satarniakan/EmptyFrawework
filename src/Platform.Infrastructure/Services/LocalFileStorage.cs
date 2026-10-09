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
/// </summary>
public class LocalFileStorage : IFileStorage
{
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

        var buffer = new byte[_maxFileSizeBytes + 1];
        var total = 0;
        int read;
        while ((read = await ReadAsync(stream, buffer, total, buffer.Length - total)) > 0)
        {
            total += read;
            if (total > _maxFileSizeBytes)
                throw new ArgumentException(
                    $"حجم فایل از {MaxFileSizeLabel()} بزرگ شده است.", nameof(fileName));
        }

        var data = buffer.AsSpan(0, total);
        var storageId = $"{Guid.NewGuid()}{extension}";
        var relativePath = $"{Hash8(ownerId)}/{fileType}/{storageId}";
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fileStream = new FileStream(
            fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, FileOptions.Asynchronous);
        await fileStream.WriteAsync(data.ToArray());
        await fileStream.FlushAsync();

        return storageId;
    }

    public Task DeleteAsync(string ownerId, string fileType, string storageId)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, $"{Hash8(ownerId)}/{fileType}/{storageId}"));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string ownerId, string fileType, string storageId)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, $"{Hash8(ownerId)}/{fileType}/{storageId}"));
        return Task.FromResult(File.Exists(fullPath));
    }

    private static string Hash8(string input) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).AsSpan(0, 8).ToString();

    private string MaxFileSizeLabel() =>
        $"~{Math.Round((double)_maxFileSizeMb, 1):F1} مگابایت";

    private static async Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            var remaining = count - totalRead;
            var read = await stream.ReadAsync(buffer, offset + totalRead, remaining);
            if (read == 0) break;
            totalRead += read;
        }

        return totalRead;
    }
}
