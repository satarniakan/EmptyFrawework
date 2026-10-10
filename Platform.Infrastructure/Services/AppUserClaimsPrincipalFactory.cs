using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Platform.Domain.Identity;

namespace Platform.Infrastructure.Services;

public class AppUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPermissionCatalog _catalog;

    public AppUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options,
        IPermissionCatalog catalog)
        : base(userManager, roleManager, options)
    {
        _roleManager = roleManager;
        _catalog = catalog;
    }

    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        // فقط FullName در پایه هست؛ اگر خالی باشد هیچ claimی اضافه نمی‌شود
        // و رابط کاربری می‌تواند از نام کاربری استفاده کند.
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            identity.AddClaim(new Claim("FullName", user.FullName));
        }

        // مجوزهای مؤثر کاربر در کوکی می‌نشینند تا policyها ([Authorize(Policy=...)])
        // و منو (NavMenu) بدون کوئری دیتابیس در هر درخواست کار کنند:
        // claimهای مستقیم کاربر + claimهای همهٔ نقش‌هایش. ادمین همهٔ کلیدهای
        // کاتالوگ را می‌گیرد تا با افزودن مجوز جدید، نیازی به لاگین مجدد نباشد.
        // توجه: تغییر مجوز نقش‌ها تا لاگین بعدی (یا بازاعتبارسنجی security stamp،
        // هر ۳۰ دقیقه) در کوکی‌های موجود اثر نمی‌کند.
        var permissions = new HashSet<string>(Permissions.Always, StringComparer.Ordinal);

        foreach (var userClaim in await UserManager.GetClaimsAsync(user))
        {
            if (userClaim.Type == Permissions.ClaimType)
                permissions.Add(userClaim.Value);
        }

        foreach (var roleName in await UserManager.GetRolesAsync(user))
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            foreach (var roleClaim in await _roleManager.GetClaimsAsync(role))
            {
                if (roleClaim.Type == Permissions.ClaimType)
                    permissions.Add(roleClaim.Value);
            }
        }

        if (await UserManager.IsInRoleAsync(user, Roles.Admin))
        {
            foreach (var descriptor in _catalog.All)
                permissions.Add(descriptor.Key);
        }

        foreach (var permission in permissions)
            identity.AddClaim(new Claim(Permissions.ClaimType, permission));

        return principal;
    }
}
