# EmptyFrawework — فریم‌ورک پایهٔ خالی (Platform)

این ریپو همان چیزهایی است که از پروژهٔ حسابداری/انبارداری جدا شد: هویت و ورود،
مدیریت کاربران و نقش‌ها، مجوزها، منوی داده‌محور، لاگ رویدادها، اعلان‌ها، صف پیام (Outbox)،
ابزارهای فارسی‌سازی و خروجی اکسل/PDF — و نه بیشتر.

## ساختار

| مسیر | نقش |
|---|---|
| `src/Platform.Domain` | هویت (`ApplicationUser`)، مجوز (`Permissions` فقط `ClaimType` دارد)، نقش‌های پایه + `IPermissionCatalog`، انتیتی‌های پایه (AuditLog، OtpCode، OutboxMessage، Notification)، `IPlatformUnitOfWork` |
| `src/Platform.Application` | سرویس‌های پایه (Auth، Otp، Audit، Permission، UserAdmin، Notification، Outbox) + Helpers فارسی + Exports اکسل/PDF |
| `src/Platform.Infrastructure` | `PlatformDbContext` (ماژولار با `IPlatformModule`) + `PlatformUnitOfWork`، `RoleSeeder`، فرستنده‌های پیامک/ایمیل |
| `src/Platform.Web` | کتابخانهٔ Razor: صفحات ورود/OTP/پروفایل/ادمین، `MainLayout`، `NavMenu` داده‌محور (`INavProvider`)، `Routes`، کامپوننت‌های `Shared/App*`، endpointهای auth و اعلان، `Setup` |
| `samples/Sample.Module` | ماژول دامنهٔ نمونه: «وظایف» — هیچ ربطی به حسابداری ندارد؛ الگوی اتصال دامنه است |
| `samples/Sample.Web` | میزبان نمونه که ثابت می‌کند پایه به‌تنهایی بالا می‌آید |

## سه نقطهٔ اتصال دامنه (تغییرناپذیرِ پایه)

پروژهٔ جدید **پایه را ویرایش نمی‌کند**؛ فقط این سه چیز را می‌دهد:

۱. `IPermissionCatalog` — فهرست مجوزهای سامانه
۲. `INavProvider` — ساختار منو
۳. `IPlatformModule` — مدل EF دامنه + `IDomainUnitOfWork : IPlatformUnitOfWork` و ریپازیتوری‌ها

## راه‌اندازی

```bash
cd samples/Sample.Web
dotnet run
```

مایگریشن اولیه را EF هنگام startup می‌سازد (`MigrateAsync`). برای ساخت مایگریشن جدید
(پس از افزودن ماژول) از پروژهٔ میزبان اجرا کنید:

```bash
dotnet ef migrations add <Name> --project samples/Sample.Web
```

## قراردادها

- همهٔ متون فارسی، سمت راست‌به‌چپ؛ تاریخ شمسی و اعداد فارسی در UI.
- لاگ با Serilog (کنسول + فایل روزانه، نگهداری ۱۴ روز).
- محدودیت نرخ روی endpointهای auth (`login`، `otp-request`، `otp-verify`، `profile`).
- صفحه‌ها تک‌فرم نگه داشته شوند تا DbContext اسکوپ‌دار مدارها به هم نریزد.