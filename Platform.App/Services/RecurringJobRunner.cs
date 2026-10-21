using Platform.Application.Jobs;

namespace Platform.App.Services;

/// <summary>
/// اجرای همهٔ <see cref="IRecurringJob"/>های ثبت‌شده در DI.
/// <para>
/// چرا یکی برای همه: هر BackgroundService جدا یک حلقه، یک try/catch و یک Delay
/// تکراری می‌خواست و خطای یکی، الگوی خطای دیگری را کپی می‌کرد. اینجا خطای یک job
/// فقط لاگ می‌شود و بقیه سرِ وقت اجرا می‌شوند (error isolation). اولین اجرای هر
/// job بلافاصله پس از استارتاپ انجام می‌شود (تا کارهای عقب‌افتاده معطل نمانند)،
/// و اجراهای بعدی به‌اندازهٔ Interval خودش فاصله می‌گیرند.
/// </para>
/// </summary>
public class RecurringJobRunner : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringJobRunner> _logger;
    private readonly RecurringJobStatusTracker _status;
    private readonly TimeProvider _clock;

    public RecurringJobRunner(IServiceScopeFactory scopeFactory, ILogger<RecurringJobRunner> logger,
        RecurringJobStatusTracker status, TimeProvider? clock = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _status = status;
        _clock = clock ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var states = new Dictionary<string, DateTime>();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = _clock.GetUtcNow().UtcDateTime;

                List<IRecurringJob> jobs;
                using (var scope = _scopeFactory.CreateScope())
                {
                    jobs = scope.ServiceProvider
                        .GetServices<IRecurringJob>()
                        .OrderBy(j => j.Name, StringComparer.Ordinal)
                        .ToList();
                }

                foreach (var job in jobs)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    _status.EnsureRegistered(job);

                    if (states.TryGetValue(job.Name, out var lastRun) && now - lastRun < job.Interval)
                        continue;

                    await RunOneAsync(job, stoppingToken);
                    states[job.Name] = _clock.GetUtcNow().UtcDateTime;
                }

                try { await Task.Delay(Tick, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
        catch (OperationCanceledException)
        {
            // توقف عادی اپلیکیشن
        }
    }

    private async Task RunOneAsync(IRecurringJob job, CancellationToken stoppingToken)
    {
        // خطای یک job نباید runner و بقیهٔ jobها را بکشد: دور بعدی دوباره تلاش می‌کند
        try
        {
            using var scope = _scopeFactory.CreateScope();
            // job اسکوپ‌دارِ تازه از اسکوپ خودش گرفته می‌شود تا DbContext مشترک نشود
            var scoped = scope.ServiceProvider.GetServices<IRecurringJob>()
                .FirstOrDefault(j => j.Name == job.Name);
            if (scoped is null)
                return;

            await scoped.ExecuteAsync(stoppingToken);
            _status.RecordSuccess(job, _clock.GetUtcNow().UtcDateTime);
        }
        catch (Exception ex)
        {
            _status.RecordFailure(job, _clock.GetUtcNow().UtcDateTime, ex.Message);
            _logger.LogError(ex, "خطا در اجرای کار تکرارشوندهٔ {Job}", job.Name);
        }
    }
}
