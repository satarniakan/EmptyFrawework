namespace Platform.Domain.Entities;

/// <summary>وضعیت پرداخت.</summary>
public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2
}

/// <summary>
/// رکورد پرداخت: ثبت داخلی پیش از رفتن به درگاه + نتیجهٔ برگشتی.
/// پول فقط با verify موفقِ درگاه «Paid» می‌شود؛ برگشت دوبارهٔ کاربر (refresh/callback تکراری)
/// با چک وضعیت، دوباره‌کاری نمی‌کند (idempotent).
/// </summary>
public class Payment
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// <summary>مبلغ به تومان.</summary>
    public long AmountTomans { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Gateway { get; set; } = string.Empty;

    /// <summary>شناسهٔ پیگیری درگاه (یکتا).</summary>
    public string Authority { get; set; } = string.Empty;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>شمارهٔ پیگیری نهایی درگاه پس از پرداخت موفق.</summary>
    public long? RefId { get; set; }

    /// <summary>شمارهٔ کارت ماسک‌شده (۴ رقم اول و آخر) — فقط برای نمایش به کاربر.</summary>
    public string? CardPanMasked { get; set; }

    public string? FailureReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? PaidAtUtc { get; set; }
}
