using Platform.Application.Jobs;
using Platform.Application.Services;

namespace Platform.Web.Services;

/// <summary>
/// پاک‌سازی دوره‌ای کدهای OTP منقضی و رکوردهای تروتل کهنه (هر ۶ ساعت).
/// </summary>
public class OtpCleanupJob : IRecurringJob
{
    public string Name => "otp-cleanup";

    public TimeSpan Interval => TimeSpan.FromHours(6);

    private static readonly TimeSpan Grace = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OtpCleanupJob> _logger;

    public OtpCleanupJob(IServiceScopeFactory scopeFactory, ILogger<OtpCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var otpService = scope.ServiceProvider.GetRequiredService<IOtpService>();
        var deleted = await otpService.PurgeExpiredAsync(Grace);
        if (deleted > 0)
            _logger.LogInformation("کدهای منقضیِ OTP حذف‌شده: {Count}", deleted);
    }
}
