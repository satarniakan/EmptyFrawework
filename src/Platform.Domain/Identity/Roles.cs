namespace Platform.Domain.Identity;

/// <summary>
/// نقش‌های پایه. فقط نقش‌هایی که خودِ پایه به آن‌ها نیاز دارد اینجا هستند؛
/// هر نقش دامنه‌ای (کاربر انبار، حسابدار و…) را پروژهٔ مصرف‌کننده از طریق
/// <see cref="IPermissionCatalog"/> یا سیدِر خودش اضافه می‌کند.
/// </summary>
public static class Roles
{
    /// <summary>مدیر کامل سامانه.</summary>
    public const string Admin = "Admin";

    /// <summary>نقش‌هایی که پایه به‌صورت پیش‌فرض می‌سازد.</summary>
    public static readonly string[] Default = [Admin];

    /// <summary>نام فارسی نقش، اگر پروژه برایش تعریف کرده باشد.</summary>
    public static string ToPersian(string roleName) => roleName switch
    {
        Admin => "ادمین",
        _ => roleName
    };
}

/// <summary>
/// منبع مجوزهای سامانه. پیاده‌سازی‌اش در پروژهٔ مصرف‌کننده است تا پایه
/// مجود و نام‌گذاری دامنه‌ای نداشته باشد.
/// </summary>
public interface IPermissionCatalog
{
    /// <summary>همهٔ مجوزهای معتبر سامانه، به‌همراه متن فارسی نمایشی.</summary>
    IReadOnlyList<PermissionDescriptor> All { get; }
}

/// <summary>توصیف یک مجوز: کلید ذخیره‌سازی و متن فارسی نمایشی.</summary>
/// <param name="Key">کلید مجوز؛ همان چیزی که روی نقش به‌صورت claim ذخیره می‌شود.</param>
/// <param name="Title">متن فارسی برای نمایش در صفحهٔ مدیریت مجوزها.</param>
/// <param name="Group">گروه نمایشی (مثلاً «کاربران» یا «گزارش‌ها»).</param>
public record PermissionDescriptor(string Key, string Title, string Group = "عمومی");