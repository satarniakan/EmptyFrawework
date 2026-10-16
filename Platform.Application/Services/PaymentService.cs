using Platform.Application.DTOs;
using Platform.Application.Queries;
using Platform.Domain.Entities;
using Platform.Domain.Exceptions;
using Platform.Domain.Interfaces;

namespace Platform.Application.Services;

/// <summary>
/// ثبت و پیگیری پرداخت‌ها: رکورد داخلی اول ساخته می‌شود، بعد کاربر به درگاه می‌رود،
/// و برگشت (callback) با وریفای درگاه نهایی می‌شود. callback تکراری امن است.
/// </summary>
public interface IPaymentService
{
    /// <summary>ساخت رکورد + شروع در درگاه. برمی‌گرداند: شناسهٔ رکورد و آدرس درگاه.</summary>
    Task<(int PaymentId, string PaymentUrl)> CreateAsync(string userId, long amountTomans,
        string description, string callbackUrl, string? mobile = null, string? email = null);

    /// <summary>پردازش برگشت از درگاه. اگر قبلاً Paid شده بود، بدون تماس با درگاه موفق برمی‌گردد.</summary>
    Task<Payment> HandleCallbackAsync(string authority, string? gatewayStatus);

    Task<Payment?> GetAsync(int paymentId, string userId);

    /// <summary>فهرست صفحه‌بندی‌شده برای صفحهٔ نظارت ادمین.</summary>
    Task<PagedResult<Payment>> GetPagedAsync(PaymentStatus? status, int page, int pageSize);
}

public class PaymentService : IPaymentService
{
    /// <summary>سقف مبلغ یک پرداخت (به تومان) — جلوی اشتباه تایپی/سوءاستفاده که درگاه را بمباران کند.</summary>
    public const long MaxAmountTomans = 1_000_000_000L;

    /// <summary>سقف طول شرح — با ستون دیتابیس (۵۰۰) هم‌خوان است تا خطای DB نگیریم.</summary>
    public const int MaxDescriptionLength = 500;

    private readonly IPaymentGateway _gateway;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public PaymentService(IPaymentGateway gateway, IPlatformUnitOfWork unitOfWork,
        TimeProvider? clock = null)
    {
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<(int PaymentId, string PaymentUrl)> CreateAsync(string userId, long amountTomans,
        string description, string callbackUrl, string? mobile = null, string? email = null)
    {
        if (amountTomans <= 0)
            throw new BusinessRuleException("مبلغ پرداخت باید مثبت باشد.");
        if (amountTomans > MaxAmountTomans)
            throw new BusinessRuleException("مبلغ پرداخت از سقف مجاز بیشتر است.");
        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleException("شرح پرداخت الزامی است.");
        if (description.Length > MaxDescriptionLength)
            throw new BusinessRuleException("شرح پرداخت بیش از حد طولانی است.");

        // ترتیب ساخت عمداً «اول رکورد محلی، بعد درگاه» است: اگر شروعِ درگاه شکست
        // بخورد، رکورد Pending با Authority موقت می‌ماند (برای پیگیری) و اگر ذخیرهٔ
        // محلی شکست بخورد، اصلاً به درگاه نرفته‌ایم — پس هیچ‌وقت «پول در درگاه،
        // بی‌رکورد در دیتابیس» نداریم. Authority موقت یکتاست تا با قید unique برخورد نکند.
        Payment payment = null!;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            payment = new Payment
            {
                UserId = userId,
                AmountTomans = amountTomans,
                Description = description,
                Gateway = _gateway.Name,
                Authority = $"PENDING-{Guid.NewGuid():N}",
                Status = PaymentStatus.Pending,
                CreatedAtUtc = _clock.GetUtcNow().UtcDateTime
            };
            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.CompleteAsync();
        });

        PaymentStartResult started;
        try
        {
            started = await _gateway.StartPaymentAsync(
                new PaymentRequest(amountTomans, description, callbackUrl, mobile, email));
        }
        catch
        {
            // درگاه جواب نداد: رکورد با دلیل روشن Failed می‌شود تا در لیست ادمین
            // قابل پیگیری باشد، ولی خطا بالا می‌رود تا کاربر «در حال انتظار» نماند.
            payment.FailureReason = "شروع پرداخت در درگاه ناموفق بود.";
            payment.Status = PaymentStatus.Failed;
            await _unitOfWork.CompleteAsync();
            throw;
        }

        payment.Authority = started.Authority;
        payment.FailureReason = null;
        await _unitOfWork.CompleteAsync();

        return (payment.Id, started.PaymentUrl);
    }

    public async Task<Payment> HandleCallbackAsync(string authority, string? gatewayStatus)
    {
        var payment = await _unitOfWork.Payments.GetByAuthorityAsync(authority)
            ?? throw new NotFoundException("پرداخت", authority);

        // کاربر Status=NOK را لغو کرده یا صفحه را refresh زده — بدون تماس با درگاه
        if (!string.Equals(gatewayStatus, "OK", StringComparison.OrdinalIgnoreCase))
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = "پرداخت توسط کاربر لغو شد یا ناموفق بود.";
            await _unitOfWork.CompleteAsync();
            return payment;
        }

        // idempotency: قبلاً نهایی شده — درگاه را دوباره صدا نزن
        if (payment.Status == PaymentStatus.Paid)
            return payment;

        var verified = await _gateway.VerifyPaymentAsync(authority, payment.AmountTomans);
        var now = _clock.GetUtcNow().UtcDateTime;

        if (verified.Succeeded)
        {
            payment.Status = PaymentStatus.Paid;
            payment.RefId = verified.RefId;
            payment.CardPanMasked = verified.CardPanMasked;
            payment.FailureReason = null;
            payment.PaidAtUtc = now;
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = verified.ErrorMessage ?? "وریفای درگاه ناموفق بود.";
        }

        await _unitOfWork.CompleteAsync();
        return payment;
    }

    public Task<Payment?> GetAsync(int paymentId, string userId) =>
        _unitOfWork.Payments.GetByIdForUserAsync(paymentId, userId);

    public async Task<PagedResult<Payment>> GetPagedAsync(PaymentStatus? status, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > PagedQueryExtensions.MaxPageSize) pageSize = PagedQueryExtensions.MaxPageSize;

        var (items, totalCount) = await _unitOfWork.Payments.GetPagedAsync(status, page, pageSize);

        return new PagedResult<Payment>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
