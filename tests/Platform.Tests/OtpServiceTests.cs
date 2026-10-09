using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Platform.Application.Services;
using Platform.Domain.Entities;
using Platform.Domain.Exceptions;
using Platform.Domain.Interfaces;
using Xunit;

namespace Platform.Tests;

/// <summary>
/// قواعد امنیتی <see cref="OtpService"/>: کد خام هرگز در دیتابیس ذخیره نمی‌شود،
/// حدس ناموفق محدود می‌شود و پس از سقف، همان شماره موقتاً قفل می‌شود.
/// </summary>
public class OtpServiceTests
{
    private const string Phone = "09120000000";

    private static (OtpService service, Mock<IOtpRepository> repo, Mock<ISmsSender> sms) Build()
    {
        var repo = new Mock<IOtpRepository>();
        var sms = new Mock<ISmsSender>();
        var uow = new Mock<IPlatformUnitOfWork>();
        uow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
        sms.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // تروتل دیتابیسی با حافظهٔ درون‌تستی: رفتار واقعی شمارنده حفظ می‌شود
        var throttles = new Dictionary<string, OtpThrottle>(StringComparer.Ordinal);
        var throttleRepo = new Mock<IOtpThrottleRepository>();
        throttleRepo.Setup(r => r.GetByPhoneAsync(It.IsAny<string>()))
            .ReturnsAsync((string phone) => throttles.TryGetValue(phone, out var t) ? t : null);
        throttleRepo.Setup(r => r.AddAsync(It.IsAny<OtpThrottle>()))
            .Callback<OtpThrottle>(t => throttles[t.PhoneNumber] = t)
            .Returns(Task.CompletedTask);

        var service = new OtpService(
            repo.Object, throttleRepo.Object, sms.Object, uow.Object,
            NullLogger<OtpService>.Instance);

        return (service, repo, sms);
    }

    [Fact]
    public async Task GenerateAndSendOtpAsync_SendsSixDigitCode_AndStoresOnlyItsHash()
    {
        var (service, repo, sms) = Build();

        string? sentMessage = null;
        sms.Setup(s => s.SendAsync(Phone, It.IsAny<string>()))
           .Callback<string, string>((_, m) => sentMessage = m)
           .Returns(Task.CompletedTask);

        OtpCode? stored = null;
        repo.Setup(r => r.AddAsync(It.IsAny<OtpCode>()))
            .Callback<OtpCode>(o => stored = o)
            .Returns(Task.CompletedTask);

        await service.GenerateAndSendOtpAsync(Phone);

        Assert.NotNull(sentMessage);
        var code = new string(sentMessage!.Where(char.IsAsciiDigit).ToArray());
        Assert.Equal(6, code.Length);

        Assert.NotNull(stored);
        Assert.NotEqual(code, stored!.Code);      // کد خام ذخیره نمی‌شود
        Assert.Equal(64, stored.Code.Length);     // SHA-256 به‌صورت hex
        Assert.Matches("^[0-9A-F]{64}$", stored.Code);
    }

    [Fact]
    public async Task VerifyOtpAsync_InvalidCode_ReturnsFalse()
    {
        var (service, repo, _) = Build();
        repo.Setup(r => r.GetLatestValidAsync(Phone, It.IsAny<string>()))
            .ReturnsAsync((OtpCode?)null);

        Assert.False(await service.VerifyOtpAsync(Phone, "123456"));
    }

    [Fact]
    public async Task VerifyOtpAsync_ValidCode_MarksUsedAndReturnsTrue()
    {
        var (service, repo, _) = Build();
        repo.Setup(r => r.GetLatestValidAsync(Phone, It.IsAny<string>()))
            .ReturnsAsync(new OtpCode { Id = 7, PhoneNumber = Phone, Code = "HASH" });
        repo.Setup(r => r.TryMarkAsUsedAsync(7)).ReturnsAsync(true);

        Assert.True(await service.VerifyOtpAsync(Phone, "123456"));
        repo.Verify(r => r.TryMarkAsUsedAsync(7), Times.Once);
    }

    [Fact]
    public async Task VerifyOtpAsync_AfterMaxFailures_StopsTouchingRepository()
    {
        var (service, repo, _) = Build();
        repo.Setup(r => r.GetLatestValidAsync(Phone, It.IsAny<string>()))
            .ReturnsAsync((OtpCode?)null);

        for (var i = 0; i < 5; i++)
            Assert.False(await service.VerifyOtpAsync(Phone, "000000"));

        // تلاش ششم باید پیش از هر کوئری، توسط قفل شماره متوقف شود
        Assert.False(await service.VerifyOtpAsync(Phone, "000000"));
        repo.Verify(r => r.GetLatestValidAsync(Phone, It.IsAny<string>()), Times.Exactly(5));
    }

    [Fact]
    public async Task GenerateAndSendOtpAsync_WhenPhoneBlocked_ThrowsBusinessRuleException()
    {
        var (service, repo, _) = Build();
        repo.Setup(r => r.GetLatestValidAsync(Phone, It.IsAny<string>()))
            .ReturnsAsync((OtpCode?)null);

        for (var i = 0; i < 5; i++)
            await service.VerifyOtpAsync(Phone, "000000");

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.GenerateAndSendOtpAsync(Phone));
    }
}
