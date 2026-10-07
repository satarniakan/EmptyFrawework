using Microsoft.AspNetCore.Identity;
using Platform.Application.DTOs;
using Platform.Domain.Identity;

namespace Platform.Application.Services;

public interface IUserAdminService
{
    Task<IEnumerable<UserListItemDto>> GetAllUsersAsync();
    Task<UserListItemDto?> GetUserAsync(string userId);
    Task<IdentityResult> SetRolesAsync(string userId, List<string> roleNames);

    Task<IdentityResult> CreateUserAsync(CreateUserDto model);
    Task<IdentityResult> UpdateUserAsync(UpdateUserDto model);
    Task<IdentityResult> DeleteUserAsync(string userId, string currentUserId);
    Task<List<RoleDto>> GetAllRolesAsync();
}

public class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserAdminService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<UserListItemDto>> GetAllUsersAsync()
    {
        var users = _userManager.Users.ToList();
        var result = new List<UserListItemDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserListItemDto(
                user.Id, user.PhoneNumber, user.FullName, user.Email, roles.ToList()));
        }

        return result;
    }

    public async Task<UserListItemDto?> GetUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserListItemDto(user.Id, user.PhoneNumber, user.FullName, user.Email, roles.ToList());
    }

    public async Task<IdentityResult> SetRolesAsync(string userId, List<string> roleNames)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = "کاربر یافت نشد." });

        // اعتبارسنجی وجود نقش‌ها پیش از هر تغییری: در غیر این صورت AddToRolesAsync شکست
        // می‌خورد ولی قبلاً بی‌صدا نادیده گرفته می‌شد و UI «موفق» نشان می‌داد.
        var unknown = new List<string>();
        foreach (var roleName in roleNames)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                unknown.Add(roleName);
        }
        if (unknown.Any())
            return IdentityResult.Failed(new IdentityError { Description = $"این نقش‌ها وجود ندارند: {string.Join("، ", unknown)}" });

        var currentRoles = await _userManager.GetRolesAsync(user);

        // گارد «آخرین ادمین»: صفحات ادمین با [Authorize(Roles=Admin)] محافظت می‌شوند، پس اگر
        // نقش Admin از تنها ادمین باقی‌مانده برداشته شود هیچ‌کس نمی‌تواند وارد شود و
        // این قفل از داخل خود سیستم باز نمی‌شود (نیاز به مداخلهٔ دستی در دیتابیس).
        var demotesFromAdmin = currentRoles.Contains(Roles.Admin) && !roleNames.Contains(Roles.Admin);
        if (demotesFromAdmin)
        {
            var otherAdmins = (await _userManager.GetUsersInRoleAsync(Roles.Admin))
                .Count(u => u.Id != userId);
            if (otherAdmins == 0)
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "نمی‌توان نقش ادمین را از تنها ادمین سیستم حذف کرد؛ ابتدا یک ادمین دیگر اضافه کنید."
                });
        }

        if (currentRoles.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded) return removeResult;
        }

        if (roleNames.Any())
        {
            var addResult = await _userManager.AddToRolesAsync(user, roleNames);
            if (!addResult.Succeeded) return addResult;
        }

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> CreateUserAsync(CreateUserDto model)
    {
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var existingByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingByEmail is not null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "این ایمیل قبلاً استفاده شده است." });
            }
        }

        var user = new ApplicationUser
        {
            UserName = model.PhoneNumber,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded && model.RoleNames is { Count: > 0 })
        {
            var validRoles = new List<string>();
            foreach (var roleName in model.RoleNames)
            {
                if (await _roleManager.RoleExistsAsync(roleName))
                {
                    validRoles.Add(roleName);
                }
            }

            if (validRoles.Any())
            {
                await _userManager.AddToRolesAsync(user, validRoles);
            }
        }

        return result;
    }

    public async Task<IdentityResult> UpdateUserAsync(UpdateUserDto model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = "کاربر یافت نشد." });

        var phone = model.PhoneNumber.Trim();
        var phoneOwner = await _userManager.FindByNameAsync(phone);
        if (phoneOwner is not null && phoneOwner.Id != user.Id)
            return IdentityResult.Failed(new IdentityError { Description = "این شماره موبایل برای کاربر دیگری ثبت شده است." });

        var email = model.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailOwner = await _userManager.FindByEmailAsync(email);
            if (emailOwner is not null && emailOwner.Id != user.Id)
                return IdentityResult.Failed(new IdentityError { Description = "این ایمیل قبلاً استفاده شده است." });
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = phone;
        // ورود با OTP و نام کاربری بر اساس شماره است؛ شماره و UserName باید با هم بمانند
        user.UserName = phone;
        user.Email = string.IsNullOrWhiteSpace(email) ? null : email;

        return await _userManager.UpdateAsync(user);
    }

    public async Task<IdentityResult> DeleteUserAsync(string userId, string currentUserId)
    {
        // گارد «حذف خود»: ادمینی که خودش را حذف کند راهی برای بازگشت ندارد
        if (userId == currentUserId)
            return IdentityResult.Failed(new IdentityError { Description = "نمی‌توانید حساب کاربری خودتان را حذف کنید." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = "کاربر یافت نشد." });

        // گارد «آخرین ادمین» — همان منطق SetRolesAsync
        if (await _userManager.IsInRoleAsync(user, Roles.Admin))
        {
            var otherAdmins = (await _userManager.GetUsersInRoleAsync(Roles.Admin))
                .Count(u => u.Id != userId);
            if (otherAdmins == 0)
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "نمی‌توان تنها ادمین سیستم را حذف کرد؛ ابتدا یک ادمین دیگر اضافه کنید."
                });
        }

        return await _userManager.DeleteAsync(user);
    }

    public async Task<List<RoleDto>> GetAllRolesAsync()
    {
        var roles = _roleManager.Roles.ToList();
        return roles.Select(r => new RoleDto
        {
            Id = r.Id ?? string.Empty,
            Name = r.Name ?? string.Empty,
            PersianName = Platform.Domain.Identity.Roles.ToPersian(r.Name ?? string.Empty)
        }).ToList();
    }
}