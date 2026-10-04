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

## نگه‌داری نسخه‌ها (به‌روزرسانی وابستگی‌ها)

وابستگی‌های این ریپو سه جنس‌اند و هر کدام روش به‌روزرسانی خودش را دارد.
قاعدهٔ کلی: اول در همین ریپو به‌روز کن و تست بگیر، بعد قالب را دوباره نصب کن تا
پروژه‌های تازه از نسخهٔ جدید ساخته شوند.

### ۱. بسته‌های NuGet (ساده‌ترین)

```bash
# دیدن همهٔ نسخه‌های جدید در کل حل
dotnet list Platform.slnx package --outdated

# به‌روزرسانی یک بسته در همهٔ پروژه‌ها
dotnet add src/Platform.Infrastructure/Platform.Infrastructure.csproj \
    package Microsoft.EntityFrameworkCore.SqlServer

# به‌روزرسانی همه‌چیز به آخرین نسخهٔ پایدار (با احتیاط، یکی‌یکی تست کنید)
dotnet outdated --upgrade  # نیاز به ابزار: dotnet tool install -g dotnet-outdated-tool
```

بعد از هر به‌روزرسانی:

```bash
dotnet build Platform.slnx
dotnet test tests/Platform.Tests/Platform.Tests.csproj
```

نکتهٔ مهم: بسته‌های `Microsoft.EntityFrameworkCore*` (نسخهٔ ۱۰.۰.۹ در این ریپو)
باید **همه با هم و هم‌نسخه** به‌روز شوند؛ وگرنه خطای ناسازگاری runtime می‌گیرید.
ابزار `dotnet-ef` را هم هم‌نسخه نگه دارید:

```bash
dotnet tool update --global dotnet-ef
```

### ۲. خودِ ‎.NET SDK

نسخهٔ فعلی: `net10.0` (در همهٔ `*.csproj`ها، کلید `TargetFramework`).

برای ارتقا به نسخهٔ اصلی بعدی (مثلاً ۱۱):

۱. جدیدترین SDK را از [dotnet.microsoft.com](https://dotnet.microsoft.com/download) نصب کنید
۲. در همهٔ `*.csproj`ها `net10.0` را عوض کنید:

```bash
# ویندوز (PowerShell) — از ریشهٔ ریپو اجرا شود
Get-ChildItem -Recurse -Filter *.csproj |
    ForEach-Object {
        $t = [IO.File]::ReadAllText($_.FullName)
        [IO.File]::WriteAllText($_.FullName, $t.Replace('net10.0', 'net11.0'))
    }
```

۳. بسته‌های NuGet را هم به نسخه‌های سازگار با SDK جدید ببرید (بخش ۱)
۴. `dotnet build` و `dotnet test` بگیرید و migration تازه بسازید تا snapshot مدل با
   نسخهٔ جدید EF هم‌خوان شود

برای **قفل‌کردن نسخهٔ SDK** در تیم (که همه با همان SDK بسازند)، یک فایل `global.json`
در ریشه بسازید:

```json
{
  "sdk": {
    "version": "10.0.303",
    "rollForward": "latestFeature"
  }
}
```

`rollForward: latestFeature` یعنی وصله‌های امنیتی خودکار می‌آیند ولی نسخهٔ اصلی عوض
نمی‌شود.

### ۳. Bootstrap و فایل‌های `wwwroot/lib` (حساس‌ترین)

نسخهٔ فعلی Bootstrap: **۵.۳.۳** (داخل `wwwroot/lib/bootstrap` به‌صورت vendored کپی
شده). فایل `src/Platform.Web/libman.json` دقیقاً مشخص می‌کند هر کتابخانه از کجا آمده
و کدام فایل‌ها کپی شده‌اند — قبل از هر به‌روزرسانی اول آن را بخوانید.

دو راه برای به‌روزرسانی هست — راه اول (LibMan) تمیزتر است:

**راه اول — با LibMan (پیشنهاد):**

۱. در `libman.json` شمارهٔ نسخه را عوض کنید (مثلاً `bootstrap@5.3.3` به `bootstrap@5.3.8`)
۲. restore کنید تا فایل‌ها دوباره دانلود شوند:

```bash
cd src/Platform.Web
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
libman restore
```

۳. اگر پوشهٔ `lib` فایل‌های اضافی دستی هم دارد (مثل `bootstrap.bundle.min.js` که در
   فهرست libman نیست)، آن‌ها را دستی از بستهٔ دانلودشده کپی کنید
۴. نسخهٔ جدید را در جدول «نسخه‌های فعلی» پایین همین بخش ثبت کنید
۵. سایت را با چشم بازبینی کنید: منوی راست‌چین، مودال‌ها، فرم‌ها و حالت موبایل —
   تست خودکار برای CSS نداریم و regression بصری فقط با چشم گرفته می‌شود
   (فایل‌های `*.rtl.css` برای راست‌چین حیاتی‌اند)

**راه دوم — دستی:**

۱. نسخهٔ جدید را از [getbootstrap.com](https://getbootstrap.com) دانلود کنید
   (فایل `bootstrap-5.x.x-dist.zip`) و جایگزین پوشهٔ `wwwroot/lib/bootstrap` کنید
۲. `libman.json` را هم به همان نسخه به‌روز کنید تا سند و واقعیت یکی بمانند

قانون: **Bootstrap را هیچ‌وقت هم‌زمان با ارتقای ‎.NET به‌روز نکنید.** یکی را ببرید
بالا، تست بگیرید، کامیت کنید، بعد سراغ دومی بروید تا اگر چیزی شکست بدانید مقصر کدام است.

### ۴. نسخه‌های فعلی (هنگام به‌روزرسانی این جدول را هم عوض کنید)

| وابستگی | نسخهٔ فعلی | کجا ثبت شده |
|---|---|---|
| .NET / TargetFramework | `net10.0` | همهٔ `*.csproj`ها |
| EF Core | ۱۰.۰.۹ | `Platform.Infrastructure.csproj` + `dotnet-ef` |
| Bootstrap | ۵.۳.۳ | `src/Platform.Web/wwwroot/lib/bootstrap` |
| bootstrap-icons | ۱.۱۱.۳ | `src/Platform.Web/wwwroot/lib/bootstrap-icons` |
| QuestPDF | ۲۰۲۶.۹.۱ | `Platform.Application.csproj` |
| ClosedXML | ۰.۱۰۴.۲ | `Platform.Application.csproj` |
| xunit | ۲.۹.۳ | `Platform.Tests.csproj` |

### ۵. خودکارسازی (پیشنهاد برای بعد)

اگر خواستید به‌روزرسانی‌ها خودکار یادآوری شوند، **Dependabot** گیت‌هاب را فعال کنید
(فایل `.github/dependabot.yml` در همین ریپو). آن‌وقت هر هفته PR خودکار برای نسخه‌های
جدید NuGet می‌آید؛ شما فقط build و test می‌گیرید و merge می‌کنید. برای Bootstrap
چون دستی است، Dependabot فقط خبر می‌دهد و جایگزینی همچنان دستی است.

```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5
```

## قراردادها

- همهٔ متون فارسی، سمت راست‌به‌چپ؛ تاریخ شمسی و اعداد فارسی در UI.
- لاگ با Serilog (کنسول + فایل روزانه، نگهداری ۱۴ روز).
- محدودیت نرخ روی endpointهای auth (`login`، `otp-request`، `otp-verify`، `profile`).
- صفحه‌ها تک‌فرم نگه داشته شوند تا DbContext اسکوپ‌دار مدارها به هم نریزد.