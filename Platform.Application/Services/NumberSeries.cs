using Microsoft.EntityFrameworkCore;
using Platform.Domain.Exceptions;
using Platform.Domain.Interfaces;

namespace Platform.Application.Services;

/// <summary>
/// صدور شمارهٔ ترتیبی همروند-امن (شماره فاکتور/سند/پرونده).
/// <para>
/// چرا حلقهٔ retry: خواندن-افزایش-ذخیره ذاتاً race دارد. نگهبان RowVersion باعث می‌شود
/// بازندهٔ مسابقه با DbUpdateConcurrencyException برگردد و با خوانش تازه دوباره تلاش کند؛
/// مسابقهٔ «ساخت هم‌زمان ردیف» هم با خطای یکتایی به همان مسیر می‌رود. پس از چند تلاش،
/// خطا واقعی تلقی می‌شود (قفل دائمی یا خرابی دیتابیس) و بالا می‌آید.
/// </para>
/// </summary>
public interface INumberSeries
{
    /// <summary>عدد بعدی دنباله (از ۱ شروع می‌شود).</summary>
    Task<long> NextAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>شمارهٔ قالب‌بندی‌شده: پیشوند + عدد با صفرِ سمت چپ (مثل «1405-000123»).</summary>
    Task<string> NextFormattedAsync(string name, string prefix, int width = 6,
        CancellationToken cancellationToken = default);
}

public class NumberSeries : INumberSeries
{
    private const int MaxAttempts = 5;

    private readonly INumberSequenceRepository _repository;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public NumberSeries(INumberSequenceRepository repository, IPlatformUnitOfWork unitOfWork,
        TimeProvider? clock = null)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<long> NextAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            // خوانش تازهٔ هر دور: تلاش قبلی ممکن است tracker را کثیف کرده باشد
            if (attempt > 0)
                _unitOfWork.ClearChangeTracker();

            try
            {
                var sequence = await _repository.GetAsync(name);
                if (sequence is null)
                {
                    sequence = new Domain.Entities.NumberSequence
                    {
                        Name = name,
                        LastValue = 1,
                        UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime
                    };
                    await _repository.AddAsync(sequence);
                }
                else
                {
                    sequence.LastValue++;
                    sequence.UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime;
                }

                await _unitOfWork.CompleteAsync();
                return sequence.LastValue;
            }
            catch (DbUpdateConcurrencyException)
            {
                // بازندهٔ مسابقه — با خوانش تازه دوباره
            }
            catch (DataIntegrityException)
            {
                // ساخت هم‌زمان ردیف توسط نمونهٔ دیگر (نقض یکتایی) — دور بعد می‌خواندش
            }
        }

        throw new BusinessRuleException("صدور شمارهٔ ترتیبی پس از چند تلاش ناموفق بود؛ دوباره تلاش کنید.");
    }

    public async Task<string> NextFormattedAsync(string name, string prefix, int width = 6,
        CancellationToken cancellationToken = default)
    {
        var next = await NextAsync(name, cancellationToken);
        return $"{prefix}{next.ToString().PadLeft(width, '0')}";
    }
}
