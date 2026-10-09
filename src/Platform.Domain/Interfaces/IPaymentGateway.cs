namespace Platform.Domain.Interfaces;

/// <summary>درخواست شروع پرداخت (مبالغ به تومان).</summary>
public record PaymentRequest(long AmountTomans, string Description, string CallbackUrl,
    string? Mobile = null, string? Email = null);

/// <summary>پاسخ شروع: شناسهٔ پیگیری درگاه + آدرسی که کاربر باید به آن برود.</summary>
public record PaymentStartResult(string Authority, string PaymentUrl);

/// <summary>پاسخ وریفای درگاه.</summary>
public record PaymentVerifyResult(bool Succeeded, long? RefId = null, string? CardPanMasked = null,
    string? ErrorMessage = null);

/// <summary>
/// قرارداد درگاه پرداخت — الگوی ISmsSender: پیاده‌سازی واقعی (زرین‌پال) و Fake برای توسعه.
/// پروژهٔ بعدی درگاه دیگر خواست، فقط همین را پیاده می‌کند.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>نام درگاه برای ثبت در رکورد پرداخت (مثل «zarinpal» یا «fake»).</summary>
    string Name { get; }

    Task<PaymentStartResult> StartPaymentAsync(PaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentVerifyResult> VerifyPaymentAsync(string authority, long amountTomans,
        CancellationToken cancellationToken = default);
}
