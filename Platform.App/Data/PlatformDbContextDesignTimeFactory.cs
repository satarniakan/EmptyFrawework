using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Platform.Infrastructure.Data;

namespace Platform.App.Data;

/// <summary>
/// فکتوری design-time برای دستورهای <c>dotnet ef migrations</c>.
/// اتصال را از ConnectionStrings__Default یا مقدار پیش‌فرض LocalDB می‌خواند.
/// </summary>
public class PlatformDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Server=(localdb)\\mssqllocaldb;Database=PlatformDev;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .ReplaceService<IModelCacheKeyFactory, PlatformModelCacheKeyFactory>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(
                typeof(PlatformDbContextDesignTimeFactory).Assembly.GetName().Name))
            .Options;

        // ماژول‌های دامنه را از مونتاژهای لودشده پیدا می‌کنیم. وقتی dotnet ef با
        // --startup-project پروژهٔ میزبان صدا زده شود، مونتاژ ماژول‌ها هم در دسترس است و
        // مدلِ migration شامل جدول‌های دامنه می‌شود؛ بدون این، مایگریشن‌ها فقط
        // جدول‌های پایه را می‌سازند و جدول‌های ماژول هرگز به دیتابیس نمی‌رسند.
        var modules = DiscoverModules();

        return new PlatformDbContext(options, modules);
    }

    private static IReadOnlyList<IPlatformModule> DiscoverModules()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var modules = new List<IPlatformModule>();

        foreach (var assembly in CandidateAssemblies())
        {
            foreach (var type in GetTypesSafe(assembly))
            {
                if (type is not { IsClass: true, IsAbstract: false } || !typeof(IPlatformModule).IsAssignableFrom(type))
                    continue;

                if (!seen.Add(type.FullName!)) continue;

                try
                {
                    if (Activator.CreateInstance(type) is IPlatformModule module)
                        modules.Add(module);
                }
                catch
                {
                    // ماژولی که سازندهٔ بدون‌پارامتر ندارد در design-time قابل ساخت نیست؛ نادیده گرفته می‌شود
                }
            }
        }

        return modules;
    }

    private static IEnumerable<Assembly> CandidateAssemblies()
    {
        var loaded = new Dictionary<string, Assembly>();

        void Add(Assembly? assembly)
        {
            if (assembly is null || assembly.IsDynamic) return;
            loaded[assembly.GetName().FullName!] = assembly;
        }

        // مونتاژهای لودشده + زنجیرهٔ ارجاع‌های مونتاژ شروع (مونتاژهای .NET تنبل لود می‌شوند)
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) Add(assembly);

        var roots = new[] { Assembly.GetEntryAssembly(), typeof(PlatformDbContextDesignTimeFactory).Assembly }
            .Where(a => a is not null)
            .Select(a => a!);
        foreach (var root in roots)
        {
            var queue = new Queue<Assembly>();
            if (loaded.TryGetValue(root.GetName().FullName!, out var cached)) queue.Enqueue(cached);
            else { Add(root); queue.Enqueue(root); }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var reference in current.GetReferencedAssemblies())
                {
                    if (loaded.ContainsKey(reference.FullName!)) continue;
                    try
                    {
                        var referenced = Assembly.Load(reference);
                        Add(referenced);
                        queue.Enqueue(referenced);
                    }
                    catch
                    {
                        // مونتاژی که لود نمی‌شود بررسی نوع نمی‌خواهد
                    }
                }
            }
        }

        // پوشهٔ خروجی میزبان: همهٔ dllهای کنار هم (مونتاژهای ماژول‌های دامنه).
        // وقتی dotnet ef با --startup-project اجرا شود، BaseDirectory همان پوشهٔ bin میزبان است.
        try
        {
            foreach (var dll in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            {
                try
                {
                    var name = AssemblyName.GetAssemblyName(dll);
                    if (loaded.ContainsKey(name.FullName!)) continue;
                    Add(Assembly.Load(name));
                }
                catch
                {
                    // dll غیرمدیریتی یا غیرلودشدنی — نادیده گرفته می‌شود
                }
            }
        }
        catch
        {
            // پوشهٔ خروجی در دسترس نیست — همان مونتاژهای لودشده کافی است
        }

        return loaded.Values;
    }

    private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch
        {
            return [];
        }
    }
}
