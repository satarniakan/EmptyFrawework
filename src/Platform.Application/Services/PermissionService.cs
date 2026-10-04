using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Platform.Domain.Identity;

namespace Platform.Application.Services;

/// <summary>
/// خواندن/نوشتن مجوزهای یک نقش. فهرست مجوزها از <see cref="IPermissionCatalog"/>
/// می‌آید که پروژهٔ مصرف‌کننده پیاده‌سازی‌اش می‌کند — پایه هیچ لیست ثابتی ندارد.
/// </summary>
public interface IPermissionService
{
    /// <summary>همهٔ مجوزها به‌همراه وضعیت اینکه آیا به نقش داده شده است.</summary>
    Task<IReadOnlyList<PermissionStateDto>> GetPermissionStatesAsync(string roleName);

    /// <summary>مجوزهای یک نقش، به‌همراه متن فارسی‌شان (برای صفحهٔ مدیریت مجوزها).</summary>
    Task<List<PermissionDescriptor>> GetPermissionsForRoleAsync(string roleName);

    Task<IdentityResult> SetPermissionsForRoleAsync(string roleName, IReadOnlyList<string> permissions);
}

/// <summary>وضعیت یک مجوز برای یک نقش.</summary>
/// <param name="Permission">توصیف کامل مجوز.</param>
/// <param name="IsGranted">آیا این نقش این مجوز را دارد؟</param>
public record PermissionStateDto(PermissionDescriptor Permission, bool IsGranted);

public class PermissionService : IPermissionService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPermissionCatalog _catalog;

    public PermissionService(RoleManager<IdentityRole> roleManager, IPermissionCatalog catalog)
    {
        _roleManager = roleManager;
        _catalog = catalog;
    }

    public async Task<IReadOnlyList<PermissionStateDto>> GetPermissionStatesAsync(string roleName)
    {
        var granted = await GetGrantedKeysAsync(roleName);
        return _catalog.All
            .Select(p => new PermissionStateDto(p, granted.Contains(p.Key)))
            .ToList();
    }

    public async Task<List<PermissionDescriptor>> GetPermissionsForRoleAsync(string roleName)
    {
        var granted = await GetGrantedKeysAsync(roleName);
        return _catalog.All.Where(p => granted.Contains(p.Key)).ToList();
    }

    public async Task<IdentityResult> SetPermissionsForRoleAsync(string roleName, IReadOnlyList<string> permissions)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
            return IdentityResult.Failed(new IdentityError { Description = "نقش یافت نشد." });

        // مجوزهای ناشناخته (تایپو یا مقدار ساختگی) عملاً هیچ دسترسی‌ای نمی‌دهند چون authorization
        // فقط مقادیر شناخته‌شده را می‌پذیرد — ولی بی‌صدا ذخیره می‌شدند و UI «ذخیره شد» نشان می‌داد.
        var knownKeys = _catalog.All.Select(p => p.Key).ToHashSet();
        var unknown = permissions.Except(knownKeys).ToList();
        if (unknown.Count > 0)
            return IdentityResult.Failed(new IdentityError
            {
                Description = $"این مجوزها ناشناخته‌اند: {string.Join("، ", unknown)}"
            });

        var currentClaims = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in currentClaims.Where(c => c.Type == Permissions.ClaimType))
        {
            var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
            if (!removeResult.Succeeded) return removeResult;
        }

        foreach (var permission in permissions.Distinct())
        {
            var addResult = await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
            if (!addResult.Succeeded) return addResult;
        }

        return IdentityResult.Success;
    }

    private async Task<HashSet<string>> GetGrantedKeysAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null) return [];

        var claims = await _roleManager.GetClaimsAsync(role);
        return claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet();
    }
}