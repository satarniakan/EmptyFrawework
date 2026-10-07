using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.Application;
using Platform.Application.Services;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;
using Platform.Infrastructure;
using Platform.Infrastructure.Services;
using Xunit;

namespace Platform.Tests;

/// <summary>
/// صحت ثبت سرویس‌های پایه: نمودار DI باید کامل باشد و فرستندهٔ پیامک واقعاً
/// بر اساس «Sms:Provider» انتخاب شود — نه اینکه همیشه شبیه‌ساز بماند.
/// </summary>
public class ServiceRegistrationTests
{
    private const string DummyConnection =
        "Server=(localdb)\\mssqllocaldb;Database=PlatformTest;Trusted_Connection=True;TrustServerCertificate=True";

    private sealed class FakeCatalog : IPermissionCatalog
    {
        public IReadOnlyList<PermissionDescriptor> All { get; } =
            [new PermissionDescriptor("test.perm", "مجوز آزمایشی")];
    }

    private static ServiceProvider BuildProvider(string? smsProvider)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = DummyConnection,
            ["Sms:Provider"] = smsProvider,
            ["Sms:Kavenegar:ApiKey"] = "test-key",
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // در یک میزبان واقعی IConfiguration را خودِ وب‌سرور ثبت می‌کند؛ اینجا دستی می‌دهیم
        services.AddSingleton<IConfiguration>(configuration);

        services.AddPlatformApplication();
        services.AddPlatformInfrastructure(configuration, isDevelopment: true);
        services.AddSingleton<IPermissionCatalog, FakeCatalog>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = false
        });
    }

    [Fact]
    public void BaseServices_AreAllResolvable()
    {
        using var provider = BuildProvider("Fake");
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.NotNull(sp.GetRequiredService<IAuthService>());
        Assert.NotNull(sp.GetRequiredService<IOtpService>());
        Assert.NotNull(sp.GetRequiredService<IPlatformUnitOfWork>());
        Assert.NotNull(sp.GetRequiredService<IAuditService>());
        Assert.NotNull(sp.GetRequiredService<IPermissionService>());
        Assert.NotNull(sp.GetRequiredService<IUserAdminService>());
        Assert.NotNull(sp.GetRequiredService<INotificationService>());
        Assert.NotNull(sp.GetRequiredService<IOutboxService>());
        Assert.NotNull(sp.GetRequiredService<IEmailSender>());
        Assert.NotNull(sp.GetRequiredService<ISmsSender>());
    }

    [Fact]
    public void SmsSender_IsFake_WhenProviderNotSet()
    {
        using var provider = BuildProvider(null);
        using var scope = provider.CreateScope();

        Assert.IsType<FakeSmsSender>(scope.ServiceProvider.GetRequiredService<ISmsSender>());
    }

    [Fact]
    public void SmsSender_IsKavenegar_WhenProviderIsKavenegar()
    {
        using var provider = BuildProvider("Kavenegar");
        using var scope = provider.CreateScope();

        Assert.IsType<KavenegarSmsSender>(scope.ServiceProvider.GetRequiredService<ISmsSender>());
    }
}