using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Platform.Application.Services;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Services;

namespace Platform.Tests;

/// <summary>
/// کپچا: Turnstile با پاسخ موفق/ناموفق ساختگی، و حالت غیرفعال که همیشه می‌پذیرد.
/// </summary>
public class CaptchaTests
{
    private sealed class CannedHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }

    private static TurnstileCaptchaValidator ValidatorWithSecret(HttpClient http)
    {
        var settings = new Mock<ISettingService>();
        settings.Setup(s => s.GetEffectiveAsync(
                IntegrationSettingKeys.TurnstileSecretKey, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("secret");

        return new TurnstileCaptchaValidator(
            http, settings.Object,
            new ConfigurationBuilder().Build(),
            NullLogger<TurnstileCaptchaValidator>.Instance);
    }

    [Fact]
    public async Task Turnstile_SuccessTrue_ReturnsTrue()
    {
        var validator = ValidatorWithSecret(
            new HttpClient(new CannedHandler("""{"success":true}""")));

        Assert.True(await validator.ValidateAsync("token", "1.2.3.4"));
    }

    [Fact]
    public async Task Turnstile_SuccessFalse_ReturnsFalse()
    {
        var validator = ValidatorWithSecret(
            new HttpClient(new CannedHandler("""{"success":false,"error-codes":["invalid-input-response"]}""")));

        Assert.False(await validator.ValidateAsync("bad-token", null));
    }

    [Fact]
    public async Task Turnstile_EmptyToken_ReturnsFalse_WithoutHttpCall()
    {
        var calls = 0;
        var handler = new CannedHandler("""{"success":true}""");
        var validator = ValidatorWithSecret(
            new HttpClient(new CountingHandler(handler, () => calls++)));

        Assert.False(await validator.ValidateAsync(null, null));
        Assert.False(await validator.ValidateAsync("  ", null));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Permissive_AlwaysReturnsTrue()
    {
        ICaptchaValidator validator = new PermissiveCaptchaValidator(
            NullLogger<PermissiveCaptchaValidator>.Instance);

        Assert.True(await validator.ValidateAsync(null, null));
        Assert.True(await validator.ValidateAsync("anything", "1.2.3.4"));
    }

    private sealed class CountingHandler(HttpMessageHandler inner, Action onCall) : DelegatingHandler(inner)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            onCall();
            return base.SendAsync(request, cancellationToken);
        }
    }
}
