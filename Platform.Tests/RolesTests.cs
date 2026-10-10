using Platform.Domain.Identity;
using Xunit;

namespace Platform.Tests;

/// <summary>
/// رگرسیونِ امنیتیِ ثبت‌نام خودکار: هیچ شماره‌ای نباید بدون تنظیم صریحِ
/// «Identity:FirstAdminPhoneNumber» نقش ادمین بگیرد.
/// </summary>
public class RolesTests
{
    [Theory]
    [InlineData(null, "09120000000", Roles.User)]
    [InlineData("", "09120000000", Roles.User)]
    [InlineData("   ", "09120000000", Roles.User)]
    [InlineData("09120000000", "09120000000", Roles.Admin)]
    [InlineData("09120000000", "09121111111", Roles.User)]
    public void DefaultRoleFor_GrantsAdminOnlyToConfiguredFirstAdmin(
        string? firstAdminPhoneNumber, string phoneNumber, string expected)
        => Assert.Equal(expected, Roles.DefaultRoleFor(firstAdminPhoneNumber, phoneNumber));

    [Fact]
    public void DefaultRoles_SeededByBase_ContainBothAdminAndUser()
    {
        Assert.Contains(Roles.Admin, Roles.Default);
        Assert.Contains(Roles.User, Roles.Default);
    }

    [Fact]
    public void ToPersian_KnowsBaseRoles_AndPassesUnknownThrough()
    {
        Assert.Equal("ادمین", Roles.ToPersian(Roles.Admin));
        Assert.Equal("کاربر", Roles.ToPersian(Roles.User));
        Assert.Equal("Warehouse", Roles.ToPersian("Warehouse"));
    }
}