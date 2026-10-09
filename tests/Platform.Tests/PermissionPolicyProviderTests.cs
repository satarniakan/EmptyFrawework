using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Platform.Domain.Identity;
using Platform.Web.Authorization;

namespace Platform.Tests;

/// <summary>
/// هر کلید کاتالوگ باید خودش یک policy باشد؛ ناشناخته‌ها به provider پیش‌فرض می‌روند.
/// </summary>
public class PermissionPolicyProviderTests
{
    private sealed class TestCatalog : IPermissionCatalog
    {
        public IReadOnlyList<PermissionDescriptor> All { get; } =
            [new("meetings.manage", "مدیریت جلسات", "جلسات")];
    }

    private static PermissionPolicyProvider CreateProvider() =>
        new(new OptionsWrapper<AuthorizationOptions>(new AuthorizationOptions()), new TestCatalog());

    private static ClaimsPrincipal UserWith(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));

    private static async Task<bool> AuthorizeAsync(AuthorizationPolicy policy, ClaimsPrincipal user)
    {
        var context = new AuthorizationHandlerContext(policy.Requirements, user, resource: null);
        foreach (var requirement in policy.Requirements)
        {
            if (requirement is IAuthorizationHandler handler)
                await handler.HandleAsync(context);
        }

        return context.HasSucceeded;
    }

    [Fact]
    public async Task KnownPermission_AllowsUserWithClaim()
    {
        var policy = await CreateProvider().GetPolicyAsync("meetings.manage");

        Assert.NotNull(policy);
        var user = UserWith(new Claim(Permissions.ClaimType, "meetings.manage"));
        Assert.True(await AuthorizeAsync(policy, user));
    }

    [Fact]
    public async Task KnownPermission_AllowsAdminWithoutClaim()
    {
        var policy = await CreateProvider().GetPolicyAsync("meetings.manage");

        Assert.NotNull(policy);
        var admin = UserWith(new Claim(ClaimTypes.Role, Roles.Admin));
        Assert.True(await AuthorizeAsync(policy, admin));
    }

    [Fact]
    public async Task KnownPermission_DeniesOrdinaryUser()
    {
        var policy = await CreateProvider().GetPolicyAsync("meetings.manage");

        Assert.NotNull(policy);
        var user = UserWith(new Claim(ClaimTypes.Role, Roles.User));
        Assert.False(await AuthorizeAsync(policy, user));
    }

    [Fact]
    public async Task UnknownPolicy_FallsBackToDefault_ReturnsNull()
    {
        var policy = await CreateProvider().GetPolicyAsync("no-such-policy");

        Assert.Null(policy);
    }
}
