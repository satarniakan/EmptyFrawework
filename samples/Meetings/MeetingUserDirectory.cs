using Microsoft.AspNetCore.Identity;
using Platform.Domain.Identity;

namespace Meetings;

/// <summary>اطلاعات تماس یک کاربر برای دعوت/نمایش نام.</summary>
/// <param name="UserId">شناسهٔ Identity.</param>
/// <param name="FullName">نام نمایشی؛ ممکن است null باشد.</param>
/// <param name="PhoneNumber">شمارهٔ موبایل برای پیامک.</param>
/// <param name="Email">ایمیل برای دعوت‌نامه.</param>
public record MeetingUser(string UserId, string? FullName, string? PhoneNumber, string? Email)
{
    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? (PhoneNumber ?? UserId) : FullName;
}

/// <summary>
/// منبع اطلاعات تماس کاربران. جدا از سرویس جلسات است تا منطق جلسات بدون
/// وابستگی مستقیم به UserManager قابل تست بماند.
/// </summary>
public interface IMeetingUserDirectory
{
    Task<List<MeetingUser>> GetUsersAsync(IReadOnlyCollection<string> userIds);
}

public class MeetingUserDirectory : IMeetingUserDirectory
{
    private readonly UserManager<ApplicationUser> _userManager;

    public MeetingUserDirectory(UserManager<ApplicationUser> userManager) =>
        _userManager = userManager;

    public async Task<List<MeetingUser>> GetUsersAsync(IReadOnlyCollection<string> userIds)
    {
        var result = new List<MeetingUser>(userIds.Count);
        foreach (var userId in userIds.Distinct())
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) continue;
            result.Add(new MeetingUser(user.Id, user.FullName, user.PhoneNumber, user.Email));
        }
        return result;
    }
}
