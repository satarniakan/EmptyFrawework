# EmptyFrawework — فریم‌ورک پایهٔ خالی (Platform)

این ریپو همان چیزهایی است که از پروژهٔ حسابداری/انبارداری جدا شد: هویت و ورود،
مدیریت کاربران و نقش‌ها، مجوزها، منوی داده‌محور، لاگ رویدادها، اعلان‌ها، صف پیام (Outbox)،
ابزارهای فارسی‌سازی و خروجی اکسل/PDF — و نه بیشتر.

## پیش‌نیازها

۱. نصب [.NET 10 SDK](https://dotnet.microsoft.com/download)
۲. نصب SQL Server (یا دسترسی به یک نمونهٔ در دسترس) — رشتهٔ اتصال را بعداً در
   `appsettings.json` می‌گذارید
۳. نصب ابزار EF (فقط برای ساخت مایگریشن‌های بعدی):

```bash
dotnet tool install --global dotnet-ef
```

## گام ۱ — نصب قالب (فقط یک‌بار در هر دستگاه)

```bash
git clone https://github.com/satarniakan/EmptyFrawework.git
cd EmptyFrawework
dotnet new install .
```

برای اطمینان که نصب شده:

```bash
dotnet new list empty-platform
```

باید یک سطر با نام `Empty Platform (Persian RTL Blazor)` ببینید.

## گام ۲ — ساخت پروژهٔ جدید

یک پوشهٔ **خالی** بسازید و قالب را داخلش باز کنید (اسم پروژه را به‌جای `MyShop` بگذارید):

```bash
mkdir MyShop
cd MyShop
dotnet new empty-platform -n MyShop
```

دو حالت کم‌حجم‌تر هم هست:

```bash
# بدون ماژول نمونه (پروژهٔ کاملاً تمیز)
dotnet new empty-platform -n MyShop --includeSample false

# با ماژول نمونه ولی با اسم دلخواه
dotnet new empty-platform -n MyShop --SampleName Orders
```

## گام ۳ — تنظیم اتصال دیتابیس

فایل `src/MyShop.Web/appsettings.json.example` را به `appsettings.json` کپی کنید و
مقادیر واقعی (نام دیتابیس و رمز `sa`) را بگذارید. خودِ `appsettings.json` عمداً در
`.gitignore` است و هیچ‌وقت در گیت ثبت نمی‌شود.

## گام ۴ — ساخت و تست

```bash
dotnet build MyShop.slnx
dotnet test tests/MyShop.Tests/MyShop.Tests.csproj
```

هر دو باید سبز شوند. اگر قرمز شدند، همان خطا را بخوانید — معمولاً یا رشتهٔ اتصال
اشتباه است یا ابزار EF نصب نیست.

## گام ۵ — اولین اجرا

```bash
cd src/MyShop.Web
dotnet run
```

جدول‌های پایه (کاربران، نقش‌ها، OTP، اعلان‌ها، لاگ‌ها، Outbox) با `MigrateAsync`
هنگام startup خودکار ساخته می‌شوند. مرورگر را روی همان آدرسی که در ترمینال چاپ شد
باز کنید و با شمارهٔ ادمینی که در تنظیمات گذاشتید وارد شوید.

## گام ۶ — نسخهٔ خودتان را در گیت ثبت کنید

```bash
git init -b master
git add -A
git commit -m "Start from empty-platform template"
```

بعد اگر ریپوی خالی در گیت‌هاب ساختید، وصلش کنید و push بزنید:

```bash
git remote add origin https://github.com/<user>/<repo>.git
git push -u origin master
```

## گام‌های بعدی (دامنهٔ خودتان)

از این‌جا به بعد فقط سه نقطهٔ اتصال را پیاده کنید — **پایه را ویرایش نکنید**:

| # | چه چیزی بدهید | کجا |
|---|---|---|
| ۱ | فهرست مجوزهای سامانه | `IPermissionCatalog` |
| ۲ | ساختار منو | `INavProvider` |
| ۳ | مدل EF دامنه + ریپازیتوری‌ها | `IPlatformModule` و `IDomainUnitOfWork : IPlatformUnitOfWork` |

الگوی هر سه در `samples/Sample.Module` هست (ماژول «وظایف»). وقتی مدل دامنه را اضافه
کردید، مایگریشن تازه بسازید:

```bash
dotnet ef migrations add <Name> --project src/MyShop.Infrastructure
```

## به‌روزرسانی قالب در آینده

وقتی این ریپو به‌روز شد، روی دستگاه خودتان:

```bash
cd EmptyFrawework
git pull
dotnet new install .
```

توجه: پروژه‌هایی که قبلاً ساخته‌اید خودکار به‌روز **نمی‌شوند** (خروجی قالب snapshot
است)؛ اصلاحات بعدی پایه را دستی یا با انتشار بستهٔ جدا منتقل می‌کنید.

## ساختار

| مسیر | نقش |
|---|---|
| `src/Platform.Domain` | هویت (`ApplicationUser`)، مجوز (`Permissions` فقط `ClaimType` دارد)، نقش‌های پایه + `IPermissionCatalog`، انتیتی‌های پایه (AuditLog، OtpCode، OutboxMessage، Notification)، `IPlatformUnitOfWork` |
| `src/Platform.Application` | سرویس‌های پایه (Auth، Otp، Audit، Permission، UserAdmin، Notification، Outbox) + Helpers فارسی + Exports اکسل/PDF |
| `src/Platform.Infrastructure` | `PlatformDbContext` (ماژولار با `IPlatformModule`) + `PlatformUnitOfWork`، `RoleSeeder`، فرستنده‌های پیامک/ایمیل |
| `src/Platform.Web` | کتابخانهٔ Razor: صفحات ورود/OTP/پروفایل/ادمین، `MainLayout`، `NavMenu` داده‌محور (`INavProvider`)، `Routes`، کامپوننت‌های `Shared/App*`، endpointهای auth و اعلان، `Setup` |
| `samples/Sample.Module` | ماژول دامنهٔ نمونه: «وظایف» — هیچ ربطی به حسابداری ندارد؛ الگوی اتصال دامنه است |
| `samples/Sample.Web` | میزبان نمونه که ثابت می‌کند پایه به‌تنهایی بالا می‌آید |

## خطاهای پرتکرار

| علامت | علت و راه‌حل |
|---|---|
| `dotnet new list` قالب را نشان نمی‌دهد | `dotnet new install .` را از داخل پوشهٔ `EmptyFrawework` اجرا کنید |
| خروجی قالب پوشهٔ تودرتو ساخت (`MyShop/MyShop`) | طبیعی است (`preferNameDirectory`)؛ داخل پوشهٔ داخلی کار کنید |
| خطای اتصال SQL هنگام `run` | رشتهٔ اتصال در `appsettings.json` را چک کنید و مطمئن شوید SQL Server بالاست |
| `dotnet-ef` شناخته نمی‌شود | گام پیش‌نیازها (نصب global) را انجام دهید |

## قراردادها

- همهٔ متون فارسی، سمت راست‌به‌چپ؛ تاریخ شمسی و اعداد فارسی در UI.
- لاگ با Serilog (کنسول + فایل روزانه، نگهداری ۱۴ روز).
- محدودیت نرخ روی endpointهای auth (`login`، `otp-request`، `otp-verify`، `profile`).
- صفحه‌ها تک‌فرم نگه داشته شوند تا DbContext اسکوپ‌دار مدارها به هم نریزد.