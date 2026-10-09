using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Platform.Application.Services;
using Platform.Domain.Entities;
using Platform.Domain.Exceptions;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Services;

namespace Platform.Tests;

/// <summary>
/// پرداخت: مبلغ نامعتبر رد می‌شود، callback لغوشده بدون تماس با درگاه Failed می‌شود،
/// callback تکراریِ پرداخت‌شده درگاه را دوباره صدا نمی‌زند، و وریفای موفق Paid می‌کند.
/// درگاه زرین‌پال با HTTP ساختگی: شروع authority می‌دهد و وریفای refId.
/// </summary>
public class PaymentTests
{
    private sealed class Harness
    {
        public Mock<IPaymentGateway> Gateway { get; } = new();
        public Mock<IPaymentRepository> Repo { get; } = new();
        public Mock<IPlatformUnitOfWork> Uow { get; } = new();

        public Harness()
        {
            Uow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            Uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(action => action());
            Uow.Setup(u => u.Payments).Returns(Repo.Object);
            Gateway.SetupGet(g => g.Name).Returns("test");
        }

        public PaymentService Build() => new(Gateway.Object, Uow.Object);
    }

    private static Payment PendingPayment(string authority = "AUTH-1") => new()
    {
        Id = 5,
        UserId = "u1",
        AmountTomans = 10000,
        Description = "تست",
        Gateway = "test",
        Authority = authority,
        Status = PaymentStatus.Pending,
        CreatedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task CreateAsync_NonPositiveAmount_ThrowsBusinessRule()
    {
        var service = new Harness().Build();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync("u1", 0, "تست", "https://x/cb"));
    }

    [Fact]
    public async Task CreateAsync_StartsGateway_AndStoresPending()
    {
        var h = new Harness();
        h.Gateway.Setup(g => g.StartPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStartResult("AUTH-9", "https://pay/x"));

        Payment? stored = null;
        h.Repo.Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .Callback<Payment>(p => { p.Id = 42; stored = p; })
            .Returns(Task.CompletedTask);

        var (paymentId, url) = await h.Build().CreateAsync("u1", 5000, "تست", "https://x/cb");

        Assert.Equal(42, paymentId);
        Assert.Equal("https://pay/x", url);
        Assert.NotNull(stored);
        Assert.Equal(PaymentStatus.Pending, stored!.Status);
        Assert.Equal("AUTH-9", stored.Authority);
    }

    [Fact]
    public async Task HandleCallbackAsync_NonOkStatus_MarksFailed_WithoutGatewayCall()
    {
        var h = new Harness();
        var payment = PendingPayment();
        h.Repo.Setup(r => r.GetByAuthorityAsync("AUTH-1")).ReturnsAsync(payment);

        var result = await h.Build().HandleCallbackAsync("AUTH-1", "NOK");

        Assert.Equal(PaymentStatus.Failed, result.Status);
        h.Gateway.Verify(g => g.VerifyPaymentAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleCallbackAsync_AlreadyPaid_SkipsGateway()
    {
        var h = new Harness();
        var payment = PendingPayment();
        payment.Status = PaymentStatus.Paid;
        payment.RefId = 111;
        h.Repo.Setup(r => r.GetByAuthorityAsync("AUTH-1")).ReturnsAsync(payment);

        var result = await h.Build().HandleCallbackAsync("AUTH-1", "OK");

        Assert.Equal(PaymentStatus.Paid, result.Status);
        h.Gateway.Verify(g => g.VerifyPaymentAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleCallbackAsync_VerifiedOk_MarksPaid()
    {
        var h = new Harness();
        var payment = PendingPayment();
        h.Repo.Setup(r => r.GetByAuthorityAsync("AUTH-1")).ReturnsAsync(payment);
        h.Gateway.Setup(g => g.VerifyPaymentAsync("AUTH-1", 10000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentVerifyResult(true, RefId: 777, CardPanMasked: "6037**0000"));

        var result = await h.Build().HandleCallbackAsync("AUTH-1", "OK");

        Assert.Equal(PaymentStatus.Paid, result.Status);
        Assert.Equal(777, result.RefId);
        Assert.NotNull(result.PaidAtUtc);
    }

    [Fact]
    public async Task HandleCallbackAsync_UnknownAuthority_ThrowsNotFound()
    {
        var h = new Harness();
        h.Repo.Setup(r => r.GetByAuthorityAsync("NOPE")).ReturnsAsync((Payment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => h.Build().HandleCallbackAsync("NOPE", "OK"));
    }

    private sealed class CannedHandler : HttpMessageHandler
    {
        private readonly string _json;

        public CannedHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            });
    }

    [Fact]
    public async Task ZarinPalGateway_Start_ReturnsAuthorityAndUrl()
    {
        var json = """{"data":{"code":100,"message":"Success","authority":"A0001"},"errors":[]}""";
        var gateway = new ZarinPalGateway(
            new HttpClient(new CannedHandler(json)), "merchant", sandbox: true,
            NullLogger<ZarinPalGateway>.Instance);

        var result = await gateway.StartPaymentAsync(new PaymentRequest(10000, "تست", "https://x/cb"));

        Assert.Equal("A0001", result.Authority);
        Assert.Contains("sandbox.zarinpal.com", result.PaymentUrl);
    }

    [Fact]
    public async Task ZarinPalGateway_Verify_Code100_ReturnsRefId()
    {
        var json = """{"data":{"code":100,"ref_id":98765,"card_pan":"603799******0000"},"errors":[]}""";
        var gateway = new ZarinPalGateway(
            new HttpClient(new CannedHandler(json)), "merchant", sandbox: false,
            NullLogger<ZarinPalGateway>.Instance);

        var result = await gateway.VerifyPaymentAsync("A0001", 10000);

        Assert.True(result.Succeeded);
        Assert.Equal(98765, result.RefId);
    }

    [Fact]
    public async Task ZarinPalGateway_Start_Non100_Throws()
    {
        var json = """{"data":{"code":-9,"message":"Validation error"},"errors":[]}""";
        var gateway = new ZarinPalGateway(
            new HttpClient(new CannedHandler(json)), "merchant", sandbox: false,
            NullLogger<ZarinPalGateway>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gateway.StartPaymentAsync(new PaymentRequest(10000, "تست", "https://x/cb")));
    }
}
