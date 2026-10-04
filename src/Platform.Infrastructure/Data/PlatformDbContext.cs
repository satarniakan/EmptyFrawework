using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;
using Platform.Domain.Identity;

namespace Platform.Infrastructure.Data;

/// <summary>
/// کنتراست ماژول دامنه. هر ماژول (مثلاً ماژول پروژهٔ نمونه) پیکربندی مدل خودش
/// را از این طریق اضافه می‌کند، بدون آنکه پایه مجبور به ویرایش شود.
/// </summary>
public interface IPlatformModule
{
    /// <summary>نام ماژول؛ فقط برای پیام‌های خطا و تشخیص.</summary>
    string Name { get; }

    /// <summary>افزودن <c>DbSet</c>ها و پیکربندی EF به مدل.</summary>
    void ConfigureModel(ModelBuilder builder);
}

/// <summary>
/// کنتراست پایه. فقط جدول‌های Identity و زیرساخت را دارد؛ هر چیز دامنه‌ای
/// باید از راه <see cref="IPlatformModule"/> اضافه شود.
/// </summary>
public class PlatformDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IEnumerable<IPlatformModule> _modules;

    public PlatformDbContext(
        DbContextOptions<PlatformDbContext> options,
        IEnumerable<IPlatformModule> modules)
        : base(options)
    {
        _modules = modules;
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // الزامی — جدول‌های Identity را این پایه می‌سازد.
        base.OnModelCreating(builder);

        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            e.Property(x => x.UserEmail).HasMaxLength(256);
            e.Property(x => x.Details).IsRequired();
            e.HasIndex(x => x.OccurredAt);
        });

        builder.Entity<OtpCode>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PhoneNumber);
            e.HasIndex(x => x.ExpiresAt);
        });

        builder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => new { x.Status, x.Attempts });
        });

        builder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.IsRead });
        });

        foreach (var module in _modules)
        {
            module.ConfigureModel(builder);
        }
    }
}