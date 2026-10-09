# Platform Framework — فریم‌ورک مدیریت جلسات (سامانه مدیریت جلسات)

این ریپو همان چیزهایی است که از پروژهٔ اصلی جدا شد: هویت و ورود,
مدیریت کاربران و نقش‌ها، مجوزها، منوی داده‌محور، لاگ رویدادها، اعلان‌ها، صف پیام (Outbox),
ابزارهای فارسی‌سازی و خروجی اکسل/PDF — همراه با ماژول کامل مدیریت جلسات.

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

یک پوشهٔ **خالی** بسازید و قالب را داخلش باز کنید (اسم پروژه را به‌جای `MyProject` بگذارید):

```bash
mkdir MyProject
cd MyProject
dotnet new empty-platform -n MyProject
```

اگر اسم پروژهٔ میزبان نمونه را می‌خواهید عوض کنید (نه ماژول جلسات — آن `Meetings` می‌ماند):

```bash
dotnet new empty-platform -n MyProject --SampleName Orders
```

> ماژول نمونه (`samples/`) بخشی از خروجی قالب است. برای حذف کامل آن، پوشهٔ
> `samples/` را پاک کنید و دو خط `<Project Path="samples/..." />` را از
> `MyProject.slnx` بردارید؛ بقیهٔ پایه کاملاً مستقل بالا می‌آید.

## گام ۳ — تنظیم اتصال دیتابیس

میزبان اجراشونده، پروژهٔ `samples/Sample.Web` است. فایل
`samples/Sample.Web/appsettings.json.example` را به `appsettings.json` (کنار خودش)
کپی کنید و مقادیر واقعی (نام دیتابیس و رمز `sa`) را بگذارید. خودِ `appsettings.json`
عمداً در `.gitignore` است و هیچ‌وقت در گیت ثبت نمی‌شود.

## گام ۴ — ساخت و تست

```bash
dotnet build MyProject.slnx
dotnet test tests/MyProject.Tests/MyProject.Tests.csproj
```

هر دو باید سبز شوند. اگر قرمز شدند، همان خطا را بخوانید — معمولاً یا رشتهٔ اتصال
اشتباه است یا ابزار EF نصب نیست.

## گام ۵ — اولین اجرا

```bash
dotnet run --project samples/Sample.Web
```

جدول‌های پایه (کاربران، نقش‌ها، OTP، اعلان‌ها، لاگ‌ها، Outbox) با `MigrateAsync`
هنگام startup خودکار ساخته می‌شوند. مرورگر را روی همان آدرسی که در ترمینال چاپ شد
باز کنید. برای اینکه خودتان ادمین شوید، قبلاً `Identity:FirstAdminPhoneNumber` را
روی شمارهٔ خودتان گذاشته باشید؛ آن شماره با اولین ورودِ OTP نقش «ادمین» می‌گیرد
و همهٔ کاربران تازه نقش «کاربر» می‌گیرند.

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

الگوی هر سه در `samples/Meetings` هست (ماژول «مدیریت جلسات»). وقتی مدل دامنه را اضافه
کردید، مایگریشن تازه بسازید:

```bash
dotnet ef migrations add <Name> --project samples/Sample.Web --startup-project samples/Sample.Web
```

## سامانهٔ مدیریت جلسات (ماژول `samples/Meetings`)

یک ماژول دامنهٔ کامل روی همین نقاط اتصال است — هم به‌عنوان محصول آماده و هم الگوی
«ماژول واقعی با UI». مسیرهای اصلی:

| مسیر | چه کاری؟ | دسترسی |
|---|---|---|
| `/my-meetings` | داشبورد جلسات من: پذیرش/رد دعوت، پیشنهاد زمان جدید، مشاهده صورت‌جلسه | همهٔ کاربران واردشده |
| `/meetings` | فهرست جلسات و ورود به مدیریت هر جلسه | مجوز `meetings.manage` یا ادمین |
| `/meetings/new` | تعریف جلسه (موضوع، زمان شمسی، حضوری/غیرحضوری، مکان یا لینک) و انتخاب مدعوین | همان |
| `/meetings/{id}/manage` | مدیریت جلسه: علامت‌زدن حاضر/غایب، تصمیم دربارهٔ پیشنهادهای زمان، نوشتن صورت‌جلسه در ویرایشگر فارسی راست‌چین و ارسال آن به کارتابل اعضا | همان |
| `/admin/users` | مدیریت افراد: درج، ویرایش، حذف و نقش‌ها | ادمین |

جریان دعوت: ثبت جلسه → دعوت‌نامهٔ هر مدعو هم‌زمان به کارتابل اعلان‌ها + صف ایمیل و
پیامک (Outbox؛ ارسال واقعی با `OutboxProcessor` بر اساس `Email:Smtp:*` و `Sms:Provider`)
می‌رود. اگر مدعو زمان دیگری پیشنهاد دهد، مسئول جلسه در کارتابلش خبر می‌شود؛ پذیرش
پیشنهاد، زمان جلسه را جابه‌جا می‌کند و پاسخ‌های قبلی را به «در انتظار» برمی‌گرداند.

جدول‌های جلسات با مایگریشن‌های `AddMeetings` به بعد ساخته می‌شوند — همهٔ مایگریشن‌ها
در پوشهٔ `samples/Sample.Web/Migrations` خودِ میزبان‌اند، نه در پایه؛ پس اسکیمای دامنهٔ
شما هرگز وارد فریم‌ورک مشترک نمی‌شود. برای مایگریشن‌های بعدیِ ماژول‌ها حتماً با
پروژهٔ میزبان بزنید تا ماژول‌ها وارد مدل شوند:

```bash
dotnet ef migrations add <Name> --project samples/Sample.Web --startup-project samples/Sample.Web
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
| `src/Platform.Domain` | هویت (`ApplicationUser`)، مجوز (`Permissions` فقط `ClaimType` دارد)، نقش‌های پایه + `IPermissionCatalog`، قراردادهای زیرساخت (`IFileStorage`، `ISmsSender`، `IEmailSender`، `IPaymentGateway`، `ICaptchaValidator`)، انتیتی‌های پایه (AuditLog، OtpCode، OtpThrottle، OutboxMessage، Notification، Setting، Payment، ApiToken، LoginHistory، PushSubscription)، `IPlatformUnitOfWork`، جست‌وجوی فارسی (`PersianSearch`) |
| `src/Platform.Application` | سرویس‌های پایه (Auth، Otp، Audit، Permission، UserAdmin، Notification، Outbox، ApiToken، LoginHistory، Setting، NumberSeries، Payment، Push، Impersonation) + Helpers فارسی + Exports اکسل/PDF + ایمپورت اکسل + ترجمهٔ یکتای خطا (`ExceptionTranslator`) + کارهای تکرارشونده (`IRecurringJob`) |
| `src/Platform.Infrastructure` | `PlatformDbContext` (ماژولار با `IPlatformModule`) + `PlatformUnitOfWork` (تراکنش واقعی)، `RoleSeeder`، فرستنده‌های پیامک/ایمیل، درگاه زرین‌پال، Turnstile، وب‌پوش VAPID — بدون هیچ مایگریشن (مال میزبان است) |
| `src/Platform.Web` | کتابخانهٔ Razor: صفحات ورود/OTP/پروفایل/نشست‌ها/ادمین/تنظیمات/نتیجهٔ پرداخت، `MainLayout` (بنر جانشینی)، `NavMenu` داده‌محور (`INavProvider`)، `Routes`، کامپوننت‌های `Shared/App*` + دکمهٔ وب‌پوش |
| `src/Platform.Web.Hosting` | زیرساخت اجرای پایه: `PlatformSetup` (ثبت سرویس‌ها، endpointها، policyهای خودکار از کاتالوگ، احراز هویت توکن API، سرویس‌های پس‌زمینه) + دارایی‌های سطح اپ (PWA، آیکون) |
| `samples/Meetings` | ماژول دامنهٔ «مدیریت جلسات»: تعریف جلسه (حضوری/غیرحضوری)، دعوت با ایمیل/پیامک (Outbox) و اعلان کارتابل، داشبورد «جلسات من» (پذیرش/رد/پیشنهاد زمان)، حضور و غیاب و صورت‌جلسهٔ فارسی راست‌چین، ایمپورت مدعوین از اکسل، یادآوری خودکار (`MeetingReminderJob`) |
| `samples/Sample.Web` | میزبان نمونه که پایه + ماژول جلسات را بالا می‌آورد؛ مایگریشن‌ها (`Migrations/`) و فکتوری design-time اینجاست |

## خطاهای پرتکرار

| علامت | علت و راه‌حل |
|---|---|
| `dotnet new list` قالب را نشان نمی‌دهد | `dotnet new install .` را از داخل پوشهٔ `EmptyFrawework` اجرا کنید |
| خروجی قالب پوشهٔ تودرتو ساخت (`MyProject/MyProject`) | طبیعی است (`preferNameDirectory`)؛ داخل پوشهٔ داخلی کار کنید |
| خطای اتصال SQL هنگام `run` | رشتهٔ اتصال در `appsettings.json` را چک کنید و مطمئن شوید SQL Server بالاست |
| `dotnet-ef` شناخته نمی‌شود | گام پیش‌نیازها (نصب global) را انجام دهید |
| خطای «رکورد ساختار پیکربندی» یا رشتهٔ اتصالِ خالی هنگام `run` | `samples/<Name>.Web/appsettings.json` ساخته نشده؛ از `appsettings.json.example` کپی کنید |
| ماژول نمونه را نمی‌خواهم | پوشهٔ `samples/` را پاک کنید و دو `<Project Path="samples/..."/>` را از `*.slnx` بردارید |
| وابستگی‌های NuGet گزارش `NU1510`/`NU1605` می‌دهند | نسخه‌ها را فقط از `Directory.Packages.props` عوض کنید و یک‌بار `dotnet restore` بگیرید |

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

نکتهٔ مهم: نسخهٔ همهٔ بسته‌های NuGet از یک جا، یعنی `Directory.Packages.props`
خوانده می‌شود (مدیریت متمرکز نسخه). پس بسته‌های `Microsoft.EntityFrameworkCore*`،
Identity و `Microsoft.Extensions.*` **همه با هم و هم‌نسخه** به‌روز می‌شوند؛ وگرنه
خطای ناسازگاری runtime می‌گیرید. ابزار `dotnet-ef` را هم هم‌نسخه نگه دارید:

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

برای **قفل‌کردن نسخهٔ SDK** در تیم (که همه با همان SDK بسازند)، این ریپو از قبل
فایل `global.json` دارد:

```json
{
  "sdk": {
    "version": "10.0.100",
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
| .NET / TargetFramework | `net10.0` | `Directory.Build.props` |
| همهٔ نسخه‌های NuGet | — | `Directory.Packages.props` (مدیریت متمرکز نسخه) |
| EF Core / Identity / Extensions | ۱۰.۰.۱۲ | `Directory.Packages.props` + `dotnet-ef` |
| Bootstrap | ۵.۳.۳ | `src/Platform.Web/wwwroot/lib/bootstrap` |
| bootstrap-icons | ۱.۱۱.۳ | `src/Platform.Web/wwwroot/lib/bootstrap-icons` |
| QuestPDF | ۲۰۲۶.۹.۱ | `Directory.Packages.props` |
| ClosedXML | ۰.۱۰۴.۲ | `Directory.Packages.props` |
| HtmlSanitizer | ۹.۲.۱۰۳۹ | `Directory.Packages.props` |
| WebPush | ۱.۰.۱۳ | `Directory.Packages.props` |
| xunit | ۲.۹.۳ | `Directory.Packages.props` |

### ۵. خودکارسازی

این ریپو از قبل دو فایل برای این کار دارد:

- `.github/workflows/ci.yml` — روی هر push/PR یک `dotnet restore`، `dotnet build`
  و `dotnet test` می‌گیرد. کافی است ریپو را به گیت‌هاب بدهید.
- `.github/dependabot.yml` — هر هفته PR خودکار برای نسخه‌های جدید NuGet و
  اکشن‌ها می‌آورد؛ شما فقط build/test بگیرید و merge کنید. برای Bootstrap چون
  دستی وارد می‌شود، Dependabot فقط خبر می‌دهد و جایگزینی همچنان دستی است.

```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5
```

## اجرای سریع با داکر (بدون نصب SQL Server)

```bash
docker compose up --build
```

مرورگر: `http://localhost:8080`. رمز dev دیتابیس و بقیه مقادیر داخل `docker-compose.yml`
هست؛ `Identity:FirstAdminPhoneNumber` را همان‌جا با شمارهٔ خودتان پر کنید تا با اولین
ورود ادمین شوید.

## قابلیت‌های پایه (راهنمای تنظیمات)

همه از `appsettings.json` (الگو: `appsettings.json.example`) تنظیم می‌شوند:

| بخش | کلیدها | توضیح |
|---|---|---|
| `Sms` | `Provider`: `Fake`/`Kavenegar` | پیامک نمایشی (لاگ) یا واقعی |
| `Email:Smtp` | `Host`، `Port`، … | `Host` خالی یعنی ارسال با خطا در Outbox ثبت می‌شود |
| `Payment` | `Provider`: `Fake`/`ZarinPal` | شروع: `POST /api/v1/payments`؛ برگشت: `/payments/callback` |
| `Captcha` | `Provider`: `Turnstile`/`Fake`/خالی | کپچای مسیر درخواست OTP؛ ویجت لاگین خودکار می‌آید |
| `Push:Vapid` | `Subject`، `PublicKey`، `PrivateKey` | وب‌پوش؛ ساخت کلید: `npx web-push generate-vapid-keys` |
| `ApiTokens` | `LifetimeDays` (پیش‌فرض ۱۸۰) | عمر توکن موبایل؛ ورود موبایل: `POST /api/v1/auth/otp/verify` |
| `Support` | `ImpersonationEnabled` | جانشینی ادمین (پیش‌فرض خاموش) |
| `Storage` | `RootPath`، `MaxFileSizeBytes`، `AllowedExtensions` | فایل‌استوریج محلی |

نقاط توسعهٔ دامنه (بدون ویرایش پایه): `IPermissionCatalog` (مجوز ← policy خودکار)،
`INavProvider` (منو)، `ISettingCatalog` (تنظیمات قابل‌ویرایش)، `IPlatformModule`
(جدول‌های EF)، `IRecurringJob` (کار دوره‌ای)، `INumberSeries` (شمارهٔ سند)،
`ExcelImporter` (ایمپورت با خطای سطری)، `IPaymentGateway` (درگاه جدید).

## قراردادها

- همهٔ متون فارسی، سمت راست‌به‌چپ؛ تاریخ شمسی و اعداد فارسی در UI.
- لاگ با Serilog (کنسول + فایل روزانه، نگهداری ۱۴ روز).
- محدودیت نرخ روی endpointهای auth (`login`، `otp-request`، `otp-verify`، `profile`).
- صفحه‌ها تک‌فرم نگه داشته شوند تا DbContext اسکوپ‌دار مدارها به هم نریزد.