using System.Collections.Concurrent;
using Platform.Application.Jobs;

namespace Platform.App.Services;

/// <summary>وضعیت آخرین اجرای یک کار تکرارشونده — برای پنل ادمین (/admin/jobs).</summary>
public record RecurringJobStatus(string Name, TimeSpan Interval, DateTime? LastRunUtc,
    DateTime? LastSuccessUtc, string? LastError, int RunCount, int ErrorCount);

/// <summary>
/// ثبت وضعیت اجراهای <see cref="RecurringJobRunner"/> برای نمایش در پنل ادمین.
/// فقط runner به‌روزش می‌کند؛ صفحهٔ ادمین فقط می‌خواند. Thread-safe چون runner و
/// درخواست‌های وب هم‌زمان هستند.
/// </summary>
public sealed class RecurringJobStatusTracker
{
    private readonly ConcurrentDictionary<string, JobState> _states = new(StringComparer.Ordinal);

    /// <summary>فهرست وضعیت همهٔ کارها، مرتب‌شده بر اساس نام (برای نمایش پایدار).</summary>
    public IReadOnlyList<RecurringJobStatus> Snapshot() =>
        _states.Select(kv => kv.Value.Snapshot(kv.Key))
            .OrderBy(s => s.Name, StringComparer.Ordinal).ToList();

    /// <summary>ثبت شروع کار (تا فهرست همهٔ jobها حتی قبل از اولین اجرا هم دیده شود).</summary>
    public void EnsureRegistered(IRecurringJob job) => _states.GetOrAdd(job.Name, _ => new JobState());

    /// <summary>ثبت یک اجرای موفق.</summary>
    public void RecordSuccess(IRecurringJob job, DateTime nowUtc) =>
        _states.GetOrAdd(job.Name, _ => new JobState())
            .Record(runSuccess: true, nowUtc, error: null, job.Interval);

    /// <summary>ثبت یک اجرای ناموفق (خطا لاگ شده ولی job ادامه می‌یابد).</summary>
    public void RecordFailure(IRecurringJob job, DateTime nowUtc, string error) =>
        _states.GetOrAdd(job.Name, _ => new JobState())
            .Record(runSuccess: false, nowUtc, error, job.Interval);

    /// <summary>وضعیت در حال تغییر یک کار؛ با قفل کوچک تا چند write هم‌زمان نسوزند.</summary>
    private sealed class JobState
    {
        private readonly object _gate = new();
        private DateTime? _lastRunUtc;
        private DateTime? _lastSuccessUtc;
        private string? _lastError;
        private int _runCount;
        private int _errorCount;
        private TimeSpan _interval;

        public void Record(bool runSuccess, DateTime nowUtc, string? error, TimeSpan interval)
        {
            lock (_gate)
            {
                _lastRunUtc = nowUtc;
                if (runSuccess)
                {
                    _lastSuccessUtc = nowUtc;
                    _lastError = null;
                }
                else
                {
                    _lastError = error;
                    _errorCount++;
                }
                _runCount++;
                _interval = interval;
            }
        }

        public RecurringJobStatus Snapshot(string name)
        {
            lock (_gate)
            {
                return new RecurringJobStatus(name, _interval, _lastRunUtc,
                    _lastSuccessUtc, _lastError, _runCount, _errorCount);
            }
        }
    }
}