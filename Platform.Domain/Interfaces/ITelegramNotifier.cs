namespace Platform.Domain.Interfaces;

/// <summary>اعلان به کانال/گروه تلگرام (اختیاری).</summary>
public interface ITelegramNotifier
{
    Task SendAsync(string message, CancellationToken cancellationToken = default);
}