// Platform.App/Program.cs
// میزبان نمونه: پایهٔ خالی را بالا می‌آورد تا ثابت شود بدون هیچ ماژول دامنه‌ای کار می‌کند.
// ماژول نمونهٔ قدیمی (جلسات) حذف شده؛ الگوی افزودن ماژول در README بخش «گام‌های بعدی» است.
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Platform.Domain.Identity;
using Platform.Infrastructure;
using Platform.Infrastructure.Data;
using Platform.Web;
using Platform.Web.Middleware;
using Platform.App;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Razor Components (Blazor Server)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(o => o.DetailedErrors = builder.Environment.IsDevelopment());

// لاگر نهایی از appsettings خوانده می‌شود؛ سطح لاگ بین Development و Production فرق می‌کند.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/log-.txt",
        rollingInterval: Serilog.RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

// Persist Data Protection keys so cookies/antiforgery tokens survive app restarts
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")));

// هر لایه تنظیمات سرویس‌های خودش را رجیستر می‌کند.
// مایگریشن‌ها در همین مونتاژ میزبان‌اند (پوشهٔ Migrations) تا اسکیمای دامنه وارد پایه نشود.
builder.Services.AddPlatform(builder.Configuration, builder.Environment.IsDevelopment(),
    migrationsAssembly: typeof(Program).Assembly.GetName().Name);
builder.Services.AddAppWeb();

// سیاست‌های دسترسی خودکارند: PermissionPolicyProvider پایه هر کلید IPermissionCatalog
// را به policy تبدیل می‌کند، پس ثبت دستی لازم نیست.

builder.Services.AddHealthChecks().AddDbContextCheck<PlatformDbContext>();

var app = builder.Build();

// Forwarded Headers — باید اولین middleware باشد تا RemoteIpAddress واقعیِ کاربر
// (پشت reverse proxy) برای Rate Limiter و لاگ در دسترس باشد.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
};
if (builder.Configuration.GetValue<bool>("ForwardedHeaders:TrustAllProxies"))
{
    forwardedHeadersOptions.KnownIPNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
}
app.UseForwardedHeaders(forwardedHeadersOptions);

// هدرهای امنیتی (nosniff، SAMEORIGIN، Referrer-Policy) — روی همهٔ پاسخ‌ها، از جمله خطاها.
app.UsePlatformSecurityHeaders();

// مدیریت خطای سراسری — باید اولِ پایپ‌لاین باشد تا همهٔ خطاها را ببیند.
// پیام فارسیِ امن + کد وضعیت درست؛ جزئیات فنی فقط در لاگ (و در Development در پاسخ).
app.UsePlatformExceptionHandler();

// اعمال خودکار مایگریشن‌های EF هنگام استارتاپ — دیگر فراموش نمی‌شوند
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    await db.Database.MigrateAsync();
}

// مجوزها و نقش‌های پایه + مجوزهای نمونه
await app.Services.InitializePlatformAsync();

// حالت توسعه: ساخت کاربر «ورود مستقیم» ادمین (Dev:AutoLoginPhone) برای /dev-login
if (app.Environment.IsDevelopment())
{
    await app.Services.EnsureDevAdminAsync();
}
app.UseSerilogRequestLogging();

app.UseRequestLocalization(PlatformSetup.PersianLocalization());

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// درخواست‌هایی که با هیچ صفحه‌ای مطابقت ندارند به صفحهٔ طراحی‌شدهٔ not-found هدایت می‌شوند.
app.UseStatusCodePagesWithReExecute("/not-found", "?from={0}");

// Antiforgery به هویت کاربر نیاز دارد، پس باید بعد از Authentication/Authorization بیاید
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseRateLimiter();

// endpointهای پایه (احراز هویت و اعلان‌ها). endpoint دامنه بعد از این می‌آید.
app.MapPlatform();
app.MapHealthChecks("/health");

// مستندات خودکار API موبایل — فقط در Development (شامل کلید عمومی پوش و مسیرهای auth).
// مرور JSON در: /openapi/v1.json
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ورود مستقیم بدون OTP — فقط در Development و فقط برای شمارهٔ Dev:AutoLoginPhone.
// در Production همین مسیر ۴۰۴ می‌دهد و سیدینگ EnsureDevAdminAsync هم اجرا نمی‌شود.
app.MapGet("/dev-login", async (
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    IConfiguration configuration) =>
{
    if (!app.Environment.IsDevelopment()) return Results.NotFound();

    var phone = configuration["Dev:AutoLoginPhone"];
    if (string.IsNullOrWhiteSpace(phone)) return Results.NotFound();

    var user = await users.FindByNameAsync(phone);
    if (user is null) return Results.NotFound();

    await signIn.SignInAsync(user, isPersistent: true);
    return Results.Redirect("/");
});

app.MapRazorComponents<Platform.App.Components.App>()
    // صفحات پایه (login، profile، admin و…) در مونتاژ Platform.Web هستند؛
    // بدون این، فقط صفحات میزبان به‌عنوان endpoint نگاشت می‌شوند.
    .AddAdditionalAssemblies(typeof(Platform.Web.Components.Routes).Assembly)
    .AddInteractiveServerRenderMode();

try
{
    Log.Information("Starting Platform.App application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// نقطهٔ ورود قابل ارجاع برای WebApplicationFactory در تست‌های یکپارچگی
public partial class Program;
