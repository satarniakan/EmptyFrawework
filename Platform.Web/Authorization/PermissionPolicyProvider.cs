using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Platform.Domain.Identity;

namespace Platform.Web.Authorization;

/// <summary>
/// سیاست‌ساز خودکار از <see cref="IPermissionCatalog"/>: هر کلید مجوز، خودش نام یک policy است.
/// <para>
/// با این، <c>[Authorize(Policy = "meetings.manage")]</c> بدون هیچ ثبت دستی در
/// <c>Program.cs</c> میزبان کار می‌کند — کاتالوگ تنها منبع حقیقت است. نام‌هایی که در
/// کاتالوگ نیستند (و policy پیش‌فرض/سفارشی میزبان‌اند) به provider پیش‌فرض واگذار می‌شوند.
/// </para>
/// <para>
/// قانون دسترسی: ادمین همیشه مجاز است (تا با افزودن مجوز جدید به کاتالوگ، نیازی به
/// لاگین مجدد ادمین نباشد)؛ بقیه باید claim همان مجوز را در کوکی داشته باشند —
/// claimها را <c>AppUserClaimsPrincipalFactory</c> هنگام ورود می‌سازد.
/// </para>
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;
    private readonly IPermissionCatalog _catalog;
    private readonly ConcurrentDictionary<string, AuthorizationPolicy?> _cache = new(StringComparer.Ordinal);

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options, IPermissionCatalog catalog)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
        _catalog = catalog;
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = _cache.GetOrAdd(policyName, BuildPolicy);
        return policy is not null
            ? Task.FromResult<AuthorizationPolicy?>(policy)
            : _fallback.GetPolicyAsync(policyName);
    }

    private AuthorizationPolicy? BuildPolicy(string policyName)
    {
        // مجوزهای «همیشه‌مجاز» فقط ورود می‌خواهند، نه claim خاص
        if (Permissions.Always.Contains(policyName, StringComparer.Ordinal))
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        }

        if (!_catalog.All.Any(p => p.Key == policyName))
            return null;

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(ctx =>
                ctx.User.IsInRole(Roles.Admin) ||
                ctx.User.HasClaim(Permissions.ClaimType, policyName))
            .Build();
    }
}
