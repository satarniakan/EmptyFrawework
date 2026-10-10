namespace Platform.Domain.Identity;

/// <summary>نوع claimهای نشست جانشین (ادمینِ به‌جای کاربر). این قرارداد بین endpointهای
/// جانشینی (Hosting)، گاردهای سرویس (Application) و بنر طرح (Web) مشترک است.</summary>
public static class ImpersonationClaims
{
    /// <summary>شناسهٔ ادمینی که جانشین شده.</summary>
    public const string Impersonator = "impersonator";

    /// <summary>نام نمایشی کاربری که به‌جایش وارد شده‌اند (برای بنر).</summary>
    public const string ImpersonatedName = "impersonated_name";
}
