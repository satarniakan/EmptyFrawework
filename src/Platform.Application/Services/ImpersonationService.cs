using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Platform.Domain.Identity;

namespace Platform.Application.Services;

/// <summary>
/// جانشینی ادمین («ورود به‌جای کاربر») برای پشتیبانی. قوانین سخت‌گیرانه عمدی‌اند:
/// <list type="bullet">
/// <item>فقط با تنظیم صریح «Support:ImpersonationEnabled=true» (پیش‌فرض خاموش).</item>
/// <item>جانشینیِ خود و جانشینیِ ادمین دیگر ممنوع (وگرنه مدیریت نقش‌ها دور زده می‌شد).</item>
/// <item>نشست جانشین نمی‌تواند رمز تعیین کند (<see cref="AuthService"/>) و نمی‌تواند دوباره جانشین شود.</item>
/// </list>
/// شروع/پایان در LoginHistory (روش Impersonation) و AuditLog ثبت می‌شود.
/// </summary>
public interface IImpersonationService
{
    bool IsEnabled { get; }

    /// <summary>نام نمایشی کاربر هدف برای بنر «به‌جای …» (یا null اگر نامعتبر).</summary>
    Task<(bool Allowed, string? Reason, string? DisplayName)> CanImpersonateAsync(
        string adminUserId, string targetUserId);
}

public class ImpersonationService : IImpersonationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly bool _enabled;

    public ImpersonationService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _enabled = configuration.GetValue("Support:ImpersonationEnabled", false);
    }

    public bool IsEnabled => _enabled;

    public async Task<(bool Allowed, string? Reason, string? DisplayName)> CanImpersonateAsync(
        string adminUserId, string targetUserId)
    {
        if (!_enabled)
            return (false, "جانشینی در تنظیمات غیرفعال است.", null);

        if (string.IsNullOrWhiteSpace(targetUserId) || targetUserId == adminUserId)
            return (false, "جانشینیِ خود مجاز نیست.", null);

        var target = await _userManager.FindByIdAsync(targetUserId);
        if (target is null)
            return (false, "کاربر یافت نشد.", null);

        if (await _userManager.IsInRoleAsync(target, Roles.Admin))
            return (false, "جانشینیِ ادمین دیگر مجاز نیست.", null);

        var displayName = !string.IsNullOrWhiteSpace(target.FullName)
            ? target.FullName
            : target.PhoneNumber ?? target.UserName;

        return (true, null, displayName);
    }
}
