using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure;

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
            .UseSqlServer(connectionString)
            .Options;

        // design-time هیچ ماژولی در DI نیست؛ ماژول‌ها در runtime از DI تزریق می‌شوند.
        return new PlatformDbContext(options, modules: []);
    }
}