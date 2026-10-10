using Platform.Domain.Identity;

namespace Platform.Web.Components.Layout;

/// <summary>
/// یک آیتم منو. هر پروژه‌ای ساختار خودش را می‌سازد؛ پایه فقط روی آن حلقه می‌زند.
/// </summary>
/// <param name="Title">متن نمایشی آیتم.</param>
/// <param name="Href">آدرس نسبی. برای گروه‌های بدون لینک null بگذارید.</param>
/// <param name="Icon">کلاس آیکون bootstrap-icons، مثلاً <c>bi-house-door</c>.</param>
/// <param name="RequiredPermission">
/// اگر مقدار داشته باشد، آیتم فقط به کاربرانی نشان داده می‌شود که آن مجوز را دارند
/// (یا مجوز «مشاهده» معادلش). null یعنی برای همهٔ کاربران واردشده.
/// </param>
/// <param name="Children">زیرآیتم‌ها؛ اگر null باشد آیتم یک لینک ساده است.</param>
public sealed record NavItem(
    string Title,
    string? Href = null,
    string? Icon = null,
    string? RequiredPermission = null,
    IReadOnlyList<NavItem>? Children = null)
{
    /// <summary>یک آیتم سادهٔ تکی (بدون زیرمنو).</summary>
    public static NavItem Link(string title, string href, string icon, string? permission = null) =>
        new(title, href, icon, permission);

    /// <summary>یک گروه بازشونده (زیرمنو).</summary>
    public static NavItem Group(string title, string icon, params NavItem[] children) =>
        new(title, null, icon, null, children);
}

/// <summary>
/// منبع منوی پروژه. پیاده‌سازی‌اش در پروژهٔ مصرف‌کننده است تا پایه هیچ آدرس یا
/// مجوزی از دامنه نشناسد. سرویس Singleton تزریق می‌شود؛ پس پیاده‌سازی باید
/// بدون وضعیت (stateless) باشد.
/// </summary>
public interface INavProvider
{
    /// <summary>ریشهٔ منو؛ ترتیب آیتم‌ها همان ترتیب نمایش است.</summary>
    IReadOnlyList<NavItem> GetRootItems();
}