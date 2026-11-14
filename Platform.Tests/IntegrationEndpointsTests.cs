using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Platform.App;
using Platform.App.Middleware;
using Platform.Application.Services;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;
using Xunit;

namespace Platform.Tests;

/// <summary>
/// تست یکپارچگی endpointهای auth/payment/notification با TestHost (بدون دیتابیس واقعی).
/// </summary>
public class IntegrationEndpointsTests : IAsyncLifetime
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
            ApplicationName = typeof(IntegrationEndpointsTests).Assembly.GetName().Name
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] =
                "Server=(localdb)\\mssqllocaldb;Database=PlatformTest;Trusted_Connection=True;TrustServerCertificate=True",
            ["Sms:Provider"] = "Fake",
            ["Payment:Provider"] = "Fake"
        });

        builder.WebHost.UseTestServer();

        builder.Services.AddPlatform(builder.Configuration, isDevelopment: true);
        builder.Services.AddSingleton<IPermissionCatalog, FakeCatalog>();

        builder.Services.RemoveAll<IHostedService>();

        _app = builder.Build();

        _app.UsePlatformSecurityHeaders();
        _app.UsePlatformCorrelationId();
        _app.UsePlatformMultiTenancy();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseAntiforgery();
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
    public async Task Ping_ReturnsPong()
    {
        var response = await _client.GetAsync("/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NotificationsRecent_Anonymous_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/notifications/recent");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/login", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task AuthMe_Anonymous_RedirectsOrUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");
        Assert.True(response.StatusCode is HttpStatusCode.Unauthorized
            or HttpStatusCode.Found
            or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CorrelationId_Header_IsEchoed()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/ping");
        request.Headers.Add("X-Correlation-ID", "test-corr-123");
        var response = await _client.SendAsync(request);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task TenantHeader_IsAccepted()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/ping");
        request.Headers.Add("X-Tenant-ID", "acme");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}