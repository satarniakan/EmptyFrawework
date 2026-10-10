using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Platform.Application.Services;
using Platform.Domain.Interfaces;
using Platform.Web.Services;

namespace Platform.Tests;

/// <summary>
/// کار تکرارشوندهٔ پاک‌سازی OTP: در هر اجرا، رکوردهای منقضی حذف می‌شوند.
/// </summary>
public class RecurringJobsTests
{
    [Fact]
    public async Task OtpCleanupJob_CallsPurge()
    {
        var otp = new Mock<IOtpService>();
        otp.Setup(o => o.PurgeExpiredAsync(It.IsAny<TimeSpan>())).ReturnsAsync(3);

        var services = new ServiceCollection();
        services.AddSingleton(otp.Object);
        var provider = services.BuildServiceProvider();

        var job = new OtpCleanupJob(provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OtpCleanupJob>.Instance);

        Assert.Equal("otp-cleanup", job.Name);
        await job.ExecuteAsync(CancellationToken.None);

        otp.Verify(o => o.PurgeExpiredAsync(It.IsAny<TimeSpan>()), Times.Once);
    }
}
