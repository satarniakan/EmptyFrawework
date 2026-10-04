namespace Platform.Domain.Identity;

/// <summary>
/// زیرساخت مجوزها. عمداً فقط «نوع claim» اینجا نگه‌داری می‌شود؛ فهرست مجورها و
/// متن فارسی‌شان کاملاً دامنه‌ای است و باید در پروژهٔ مصرف‌کننده تعریف شود
/// (به‌صورت یک <c>IPermissionCatalog</c>) تا پایه نیازی به ویرایش نداشته باشد.
/// </summary>
public static class Permissions
{
    /// <summary>نوع claim‌ای که مجوزها روی نقش‌ها با آن ذخیره می‌شوند.</summary>
    public const string ClaimType = "Permission";

    /// <summary>
    /// مجوزهایی که همیشه به همهٔ کاربران اجازه می‌دهند و نیازی به ثبت در نقش ندارند.
    /// عمداً خالی است تا پروژهٔ مصرف‌کننده خودش تصمیم بگیرد.
    /// </summary>
    public static readonly string[] Always = [];
}