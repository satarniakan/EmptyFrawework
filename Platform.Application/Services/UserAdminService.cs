using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Application.DTOs;
using Platform.Application.Queries;
using Platform.Domain.Identity;
using Platform.Domain.Queries;

namespace Platform.Application.Services;

public interface IUserAdminService
{
    /// <summary>
    /// فهرست صفحه‌بندی‌شدهٔ کاربران با جست‌وجو (شماره، نام، ایمیل). نقش‌های هر کاربر
    /// فقط برای همان صفحه خوانده می‌شوند تا لیست هزاران‌نفره N+1 نشود.
    /// </summary>
    Task<PagedResult<UserListItemDto>> GetUsersPagedAsync(string? search, int page, int pageSize);

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
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContext;

    public UserAdminService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
        IAuditService audit, IHttpContextAccessor httpContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _audit = audit;
        _httpContext = httpContext;
    }

    /// <summary>عامل فعلی (ادمین انجام‌دهنده) برای رد حسابرسی؛ بیرون از درخواست HTTP تهی است.</summary>
    private string? ActorName => _httpContext.HttpContext?.User.Identity?.Name;

    public async Task<PagedResult<UserListItemDto>> GetUsersPagedAsync(string? search, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > PagedQueryExtensions.MaxPageSize) pageSize = PagedQueryExtensions.MaxPageSize;

        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // عبارت نرمال می‌شود (ي/ك عربی، ارقام فارسی) و ستون‌ها هم در کوئری
            // همان نرمال‌سازی را می‌بینند — به REPLACE تودرتو در SQL ترجمه می‌شود
            var term = PersianSearch.Normalize(search);
            if (term.Length > 0)
            {
                query = query.Where(PersianSearch.ContainsNormalized<ApplicationUser>(
                    term, u => u.PhoneNumber, u => u.FullName, u => u.Email));
            }
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<UserListItemDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserListItemDto(
                user.Id, user.PhoneNumber, user.FullName, user.Email, roles.ToList()));
        }

        return new PagedResult<UserListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
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

        await _audit.LogEventAsync("user.roles-changed", ActorName,
            $"نقش‌های کاربر {user.PhoneNumber} به «{string.Join("، ", roleNames)}» تغییر کرد.");
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> CreateUserAsync(CreateUserDto model)
    {
        if (!string.IsNullOrWhiteSpace(model.FullName) && model.FullName.Trim().Length > 100)
        {
            return IdentityResult.Failed(new IdentityError
                { Description = "نام کامل نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد." });
        }

        // شماره همیشه با ارقام انگلیسی ذخیره می‌شود تا جست‌وجو و ورود یکدست باشد
        var phone = PersianSearch.NormalizePhone(model.PhoneNumber);

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
            UserName = phone,
            PhoneNumber = phone,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true
        };

        var result = string.IsNullOrWhiteSpace(model.Password)
            ? await _userManager.CreateAsync(user)
            : await _userManager.CreateAsync(user, model.Password);

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

        if (result.Succeeded)
        {
            await _audit.LogEventAsync("user.created", ActorName,
                $"کاربر {phone} ساخته شد.");
        }

        return result;
    }

    public async Task<IdentityResult> UpdateUserAsync(UpdateUserDto model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = "کاربر یافت نشد." });

        if (model.FullName.Trim().Length > 100)
        {
            return IdentityResult.Failed(new IdentityError
                { Description = "نام کامل نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد." });
        }

        var phone = PersianSearch.NormalizePhone(model.PhoneNumber);
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

        var updateResult = await _userManager.UpdateAsync(user);
        if (updateResult.Succeeded)
        {
            await _audit.LogEventAsync("user.updated", ActorName,
                $"کاربر {phone} ویرایش شد.");
        }

        return updateResult;
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

        var deleteResult = await _userManager.DeleteAsync(user);
        if (deleteResult.Succeeded)
        {
            await _audit.LogEventAsync("user.deleted", ActorName,
                $"کاربر {user.PhoneNumber} حذف شد.");
        }

        return deleteResult;
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