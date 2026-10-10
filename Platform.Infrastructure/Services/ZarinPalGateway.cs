using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.Domain.Interfaces;

namespace Platform.Infrastructure.Services;

/// <summary>
/// درگاه زرین‌پال (نسخهٔ ۴، مبالغ به تومان). تنظیمات از «Payment:ZarinPal:*»:
/// MerchantId (اجباری)، Sandbox (پیش‌فرض false).
/// </summary>
public class ZarinPalGateway : IPaymentGateway
{
    public string Name => "zarinpal";

    private readonly HttpClient _http;
    private readonly string _merchantId;
    private readonly bool _sandbox;
    private readonly ILogger<ZarinPalGateway> _logger;

    public ZarinPalGateway(HttpClient http, string merchantId, bool sandbox,
        ILogger<ZarinPalGateway> logger)
    {
        _http = http;
        _merchantId = merchantId;
        _sandbox = sandbox;
        _logger = logger;
    }

    private string BaseUrl => _sandbox
        ? "https://sandbox.zarinpal.com/pg/v4/payment"
        : "https://api.zarinpal.com/pg/v4/payment";

    private string StartPayUrl(string authority) =>
        (_sandbox ? "https://sandbox.zarinpal.com/pg/StartPay/" : "https://www.zarinpal.com/pg/StartPay/")
        + authority;

    public async Task<PaymentStartResult> StartPaymentAsync(PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_merchantId))
            throw new InvalidOperationException("کلید پذیرندهٔ زرین‌پال (Payment:ZarinPal:MerchantId) تنظیم نشده است.");

        using var response = await _http.PostAsJsonAsync($"{BaseUrl}/request.json",
            new
            {
                merchant_id = _merchantId,
                amount = request.AmountTomans,
                callback_url = request.CallbackUrl,
                description = request.Description,
                metadata = new { mobile = request.Mobile, email = request.Email }
            }, cancellationToken);

        var doc = await ReadJsonAsync(response, cancellationToken);
        var data = doc.RootElement.GetProperty("data");
        var code = data.GetProperty("code").GetInt32();

        if (code != 100)
        {
            var message = data.TryGetProperty("message", out var m) ? m.GetString() : "خطای درگاه";
            _logger.LogWarning("ZarinPal request failed with code {Code}: {Message}", code, message);
            throw new InvalidOperationException($"شروع پرداخت ناموفق بود (کد {code}).");
        }

        var authority = data.GetProperty("authority").GetString()!;
        return new PaymentStartResult(authority, StartPayUrl(authority));
    }

    public async Task<PaymentVerifyResult> VerifyPaymentAsync(string authority, long amountTomans,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"{BaseUrl}/verify.json",
            new { merchant_id = _merchantId, authority, amount = amountTomans },
            cancellationToken);

        var doc = await ReadJsonAsync(response, cancellationToken);
        var data = doc.RootElement.GetProperty("data");
        var code = data.GetProperty("code").GetInt32();

        // ۱۰۰ = موفق، ۱۰۱ = قبلاً وریفای شده (callback تکراری) — هر دو یعنی پول رسیده
        if (code is 100 or 101)
        {
            var refId = data.TryGetProperty("ref_id", out var r) ? r.GetInt64() : (long?)null;
            var pan = data.TryGetProperty("card_pan", out var p) ? p.GetString() : null;
            return new PaymentVerifyResult(true, refId, pan);
        }

        var message = data.TryGetProperty("message", out var msg) ? msg.GetString() : "ناموفق";
        return new PaymentVerifyResult(false, ErrorMessage: message);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }
}

/// <summary>
/// درگاه نمایشی برای Development/تست: شروع همیشه موفق با authority ساختگی،
/// وریفای همیشه موفق با refId ثابت. هیچ تماس شبکه‌ای ندارد.
/// </summary>
public class FakePaymentGateway : IPaymentGateway
{
    public string Name => "fake";

    private readonly ILogger<FakePaymentGateway> _logger;
    private readonly bool _logDetails;

    public FakePaymentGateway(ILogger<FakePaymentGateway> logger, bool logDetails = false)
    {
        _logger = logger;
        _logDetails = logDetails;
    }

    public Task<PaymentStartResult> StartPaymentAsync(PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var authority = $"FAKE-{Guid.NewGuid():N}";
        if (_logDetails)
            _logger.LogInformation("FAKE payment start: {Amount} — {Authority}", request.AmountTomans, authority);

        // در حالت نمایشی، «رفتن به درگاه» یعنی برگشت مستقیم به callback میزبان
        return Task.FromResult(new PaymentStartResult(authority, $"/payments/callback?Authority={authority}&Status=OK"));
    }

    public Task<PaymentVerifyResult> VerifyPaymentAsync(string authority, long amountTomans,
        CancellationToken cancellationToken = default)
    {
        if (_logDetails)
            _logger.LogInformation("FAKE payment verify: {Authority}", authority);

        return Task.FromResult(new PaymentVerifyResult(true, RefId: 123456, CardPanMasked: "603799******0000"));
    }
}
