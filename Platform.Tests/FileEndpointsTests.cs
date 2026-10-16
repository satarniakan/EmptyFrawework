using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Platform.App.Endpoints;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;
using Xunit;

namespace Platform.Tests;

/// <summary>
/// Endpoint دانلود فایل: ناشناس ۴۰۱، غیرمالک ۴۰۳، مالک/ادمین ۲۰۰ و فایل‑یافت‌نشده ۴۰۴.
/// </summary>
public class FileEndpointsTests
{
    private sealed class TestApp : IAsyncLifetime
    {
        private WebApplication? _app;
        private HttpClient _client = null!;

        public Mock<IFileStorage> Storage { get; } = new();
        public Mock<IFileAccessPolicy> Policy { get; } = new();

        public async Task InitializeAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Development",
                ApplicationName = typeof(FileEndpointsTests).Assembly.GetName().Name
            });
            builder.Services.AddSingleton<IFileStorage>(Storage.Object);
            builder.Services.AddSingleton<IFileAccessPolicy>(Policy.Object);

            // کاربر تست از هدر X-Test-User / X-Test-Admin متعین می‌شود (بدون Authentication واقعی)
            builder.WebHost.UseTestServer();

            var app = builder.Build();
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Headers.TryGetValue("X-Test-User", out var me))
                    ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, me!) }, "test"));
                else if (ctx.Request.Headers.TryGetValue("X-Test-Admin", out var _))
                    ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "u2"),
                            new Claim(ClaimTypes.Role, Roles.Admin)
                        }, "test"));
                await next();
            });
            app.MapFileEndpoints();
            _app = app;
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

        public HttpClient Client => _client;
    }

    [Fact]
    public async Task Anonymous_Returns401()
    {
        var app = new TestApp();
        await app.InitializeAsync();

        var response = await app.Client.GetAsync("/files/u1/photo/sid-1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await app.DisposeAsync();
    }

    [Fact]
    public async Task NonOwner_Returns403()
    {
        var app = new TestApp();
        await app.InitializeAsync();
        app.Policy.Setup(p => p.CanReadAsync("u2", false, "u1", "photo")).ReturnsAsync(false);
        app.Client.DefaultRequestHeaders.Add("X-Test-User", "u2");

        var response = await app.Client.GetAsync("/files/u1/photo/sid-1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await app.DisposeAsync();
    }

    [Fact]
    public async Task Owner_ReturnsStream_WithAttachmentDisposition()
    {
        var app = new TestApp();
        await app.InitializeAsync();
        app.Storage.Setup(s => s.ExistsAsync("u1", "photo", "sid-1")).ReturnsAsync(true);
        app.Storage.Setup(s => s.OpenReadAsync("u1", "photo", "sid-1"))
            .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));
        app.Policy.Setup(p => p.CanReadAsync("u1", false, "u1", "photo")).ReturnsAsync(true);
        app.Client.DefaultRequestHeaders.Add("X-Test-User", "u1");

        var response = await app.Client.GetAsync("/files/u1/photo/sid-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Content.Headers.Contains("Content-Disposition"));
        Assert.Contains("attachment", response.Content.Headers.GetValues("Content-Disposition").First());
        Assert.Equal(new byte[] { 1, 2, 3 }, await response.Content.ReadAsByteArrayAsync());
        await app.DisposeAsync();
    }

    [Fact]
    public async Task Admin_CanReadOthersFiles()
    {
        var app = new TestApp();
        await app.InitializeAsync();
        app.Storage.Setup(s => s.ExistsAsync("u1", "photo", "sid-1")).ReturnsAsync(true);
        app.Storage.Setup(s => s.OpenReadAsync("u1", "photo", "sid-1"))
            .ReturnsAsync(new MemoryStream(new byte[] { 9 }));
        app.Policy.Setup(p => p.CanReadAsync(It.IsAny<string>(), true, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        app.Client.DefaultRequestHeaders.Add("X-Test-Admin", "1");

        var response = await app.Client.GetAsync("/files/u1/photo/sid-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.DisposeAsync();
    }

    [Fact]
    public async Task MissingFile_Returns404()
    {
        var app = new TestApp();
        await app.InitializeAsync();
        app.Policy.Setup(p => p.CanReadAsync("u1", false, "u1", "photo")).ReturnsAsync(true);
        app.Client.DefaultRequestHeaders.Add("X-Test-User", "u1");

        var response = await app.Client.GetAsync("/files/u1/photo/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await app.DisposeAsync();
    }
}