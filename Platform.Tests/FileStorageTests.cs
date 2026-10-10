using System.Text;
using Microsoft.Extensions.Configuration;
using Platform.Infrastructure.Services;

namespace Platform.Tests;

/// <summary>
/// LocalFileStorage: رفت‌وبرگشت بایت‌ها، whitelist پسوند، سقف حجم و
/// ماندن مسیر زیر Root (دفاع در برابر ../).
/// </summary>
public class FileStorageTests : IDisposable
{
    private readonly string _root;

    public FileStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"storage-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); }
        catch { /* تمیزکاری تست نباید خطا بدهد */ }
    }

    private LocalFileStorage Build(Dictionary<string, string?>? settings = null)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["Storage:RootPath"] = _root,
            ["Storage:MaxFileSizeBytes"] = "1024",
            ["Storage:AllowedExtensions"] = "txt,png"
        };
        if (settings is not null)
        {
            foreach (var kv in settings)
                defaults[kv.Key] = kv.Value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(defaults)
            .Build();

        return new LocalFileStorage(configuration);
    }

    private static MemoryStream StreamOf(string text) =>
        new(Encoding.UTF8.GetBytes(text));

    [Fact]
    public async Task SaveAndOpen_RoundTripsBytes()
    {
        var storage = Build();

        var id = await storage.SaveAsync("owner-1", "doc", StreamOf("سلام"), "note.txt");
        await using var read = await storage.OpenReadAsync("owner-1", "doc", id);

        Assert.NotNull(read);
        using var reader = new StreamReader(read!);
        Assert.Equal("سلام", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Save_DisallowedExtension_Throws()
    {
        var storage = Build();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.SaveAsync("owner-1", "doc", StreamOf("x"), "evil.exe"));
    }

    [Fact]
    public async Task Save_OverSizeLimit_Throws()
    {
        var storage = Build();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.SaveAsync("owner-1", "doc", StreamOf(new string('x', 2000)), "big.txt"));
    }

    [Fact]
    public async Task Save_PathTraversalInFileType_Throws()
    {
        var storage = Build();

        // دو سطح بالاتر می‌رود و از Root خارج می‌شود — باید رد شود
        await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.SaveAsync("owner-1", "../../escape", StreamOf("x"), "note.txt"));

        // و هیچ فایل نیمه‌کاره‌ای هم نباید مانده باشد
        Assert.Empty(Directory.GetFiles(_root, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Delete_RemovesFile_AndOpenReturnsNull()
    {
        var storage = Build();

        var id = await storage.SaveAsync("owner-1", "doc", StreamOf("bye"), "note.txt");
        Assert.True(await storage.ExistsAsync("owner-1", "doc", id));

        await storage.DeleteAsync("owner-1", "doc", id);

        Assert.False(await storage.ExistsAsync("owner-1", "doc", id));
        Assert.Null(await storage.OpenReadAsync("owner-1", "doc", id));
    }
}
