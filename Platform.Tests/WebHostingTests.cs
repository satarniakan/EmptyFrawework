using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Platform.Application.Services;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;
using Platform.Web;
using Xunit;

namespace Platform.Tests;

/// <summary>
/// تست دود از لایهٔ وب پایه: برنامه بالا می‌آید، endpointهای پایه نگاشت می‌شوند و
/// endpointهای محافظت‌شده بدون کوکی ۴۰۱ می‌دهند (یعنی [Authorize] و ترتیب
/// middlewareها درست است). دیتابیس اصلاً لمس نمی‌شود.
/// </summary>
public class WebHostingTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient _client = null!;

    private sealed class FakeCatalog : IPermissionCatalog
    {
        public IReadOnlyList<PermissionDescriptor> All { get; } =
            [new PermissionDescriptor("test.perm", "مجوز آزمایشی")];
    }

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
            ApplicationName = typeof(WebHostingTests).Assembly.GetName().Name
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] =
                "Server=(localdb)\\mssqllocaldb;Database=PlatformTest;Trusted_Connection=True;TrustServerCertificate=True",
            ["Sms:Provider"] = "Fake"
        });

        builder.WebHost.UseTestServer();

        builder.Services.AddPlatform(builder.Configuration, isDevelopment: true);
        builder.Services.AddSingleton<IPermissionCatalog, FakeCatalog>();

        // سرویس‌های پس‌زمینه (پردازش Outbox و پاک‌سازی OTP) هنگام اجرا به دیتابیس
        // وصل می‌شوند. این تست عمداً آن‌ها را اجرا نمی‌کند تا به هیچ دیتابیسی
        // نیاز نباشد و اجرای تست سریع بماند.
        builder.Services.RemoveAll<IHostedService>();

        _app = builder.Build();

        // همان ترتیبی که میزبان واقعی به کار می‌برد
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseRateLimiter();
        _app.MapPlatform();
        _app.MapGet("/ping", () => Results.Text("pong"));

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task Application_StartsAndServesRequests()
    {
        var response = await _client.GetAsync("/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProtectedPlatformEndpoint_RedirectsAnonymousCallerToLogin()
    {
        var response = await _client.GetAsync("/notifications/recent");

        // کوکیِ Identity به‌جای ۴۰۱ ریدایرکت می‌کند؛ مسیرش هم دقیقاً همان
        // LoginPath‌ای است که پایه پیکربندی کرده («/login»، نه مسیر پیش‌فرض Identity)
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("/login", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task AllPlatformServices_ResolveInsideARealScope()
    {
        using var scope = _app!.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAuthService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IOtpService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPlatformUnitOfWork>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISmsSender>());
    }
}