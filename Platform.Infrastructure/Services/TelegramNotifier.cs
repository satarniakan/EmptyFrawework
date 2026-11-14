using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Domain.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Platform.Infrastructure.Services;

/// <summary>
/// اعلان به کانال/گروه تلگرام. فقط وقتی BotToken تنظیم شده باشد فعال است.
/// </summary>
public class TelegramNotifier : ITelegramNotifier
{
    private readonly ITelegramBotClient? _bot;
    private readonly long? _chatId;
    private readonly ILogger<TelegramNotifier> _logger;

    public TelegramNotifier(IConfiguration configuration, ILogger<TelegramNotifier> logger)
    {
        _logger = logger;
        var token = configuration["Telegram:BotToken"];
        var chatIdText = configuration["Telegram:ChatId"];
        if (!string.IsNullOrWhiteSpace(token) && long.TryParse(chatIdText, out var chatId))
        {
            _bot = new TelegramBotClient(token);
            _chatId = chatId;
        }
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_bot is null || _chatId is null)
        {
            _logger.LogDebug("Telegram notifier disabled (no BotToken/ChatId).");
            return;
        }

        try
        {
            await _bot.SendMessage(_chatId.Value, message, parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Telegram notification.");
        }
    }
}