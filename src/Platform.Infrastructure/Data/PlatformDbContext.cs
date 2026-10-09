using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Platform.Domain.Common;
using Platform.Domain.Entities;
using Platform.Domain.Identity;
using Platform.Domain.Interfaces;

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
    private readonly IReadOnlyList<IPlatformModule> _modules;
    private readonly ICurrentUser? _currentUser;
    private readonly TimeProvider _clock;

    /// <param name="currentUser">برای ثبت «چه کسی»؛ در design-time و تست null است.</param>
    /// <param name="clock">منبع زمان؛ پیش‌فرض <see cref="TimeProvider.System"/>.</param>
    public PlatformDbContext(
        DbContextOptions<PlatformDbContext> options,
        IEnumerable<IPlatformModule> modules,
        ICurrentUser? currentUser = null,
        TimeProvider? clock = null)
        : base(options)
    {
        _modules = modules.ToList();
        _currentUser = currentUser;
        _clock = clock ?? TimeProvider.System;
    }

    /// <summary>ماژول‌های ثبت‌شده؛ برای کلید مدل EF استفاده می‌شود.</summary>
    internal IReadOnlyList<IPlatformModule> Modules => _modules;

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<OtpThrottle> OtpThrottles => Set<OtpThrottle>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserApiToken> ApiTokens => Set<UserApiToken>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();

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

        builder.Entity<OtpThrottle>(e =>
        {
            e.HasKey(x => x.PhoneNumber);
            e.Property(x => x.PhoneNumber).HasMaxLength(32);
        });

        builder.Entity<UserApiToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.DeviceName).HasMaxLength(100);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.UserId, x.IsRevoked });
        });

        builder.Entity<LoginHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(450);
            e.Property(x => x.UserName).HasMaxLength(256);
            e.Property(x => x.Method).HasMaxLength(32).IsRequired();
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(512);
            e.Property(x => x.FailureReason).HasMaxLength(256);
            e.HasIndex(x => new { x.UserId, x.OccurredAtUtc });
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

        // سقف نام نمایشی در دیتابیس هم هست، چون اعتبارسنجی فرم (DataAnnotations)
        // روی مسیرهای API/endpoint اجرا نمی‌شود — آخرین دیوار، خودِ ستون است
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(100);
        });

        foreach (var module in _modules)
        {
            module.ConfigureModel(builder);
        }

        // بعد از ماژول‌ها: خودِ ماژول‌ها ستون‌ها را می‌سازند، اینجا لایهٔ مشترک رویشان می‌نشیند.
        ApplyAuditableConfiguration(builder);
    }

    /// <summary>
    /// پیکربندی مشترک همهٔ انتیتی‌های <see cref="AuditableEntity"/>: طول فیلدها،
    /// ایندکس حذف نرم و فیلتر سراسری که رکوردهای حذف‌شده را از کوئری‌های خواندن پنهان کند.
    /// </summary>
    private static void ApplyAuditableConfiguration(ModelBuilder builder)
    {
        var auditableTypes = builder.Model.GetEntityTypes()
            .Where(t => !t.IsOwned() && typeof(AuditableEntity).IsAssignableFrom(t.ClrType))
            .Select(t => t.ClrType)
            .ToList();

        foreach (var clrType in auditableTypes)
        {
            var entity = builder.Entity(clrType);

            entity.Property(nameof(AuditableEntity.CreatedByUserId)).HasMaxLength(450);
            entity.Property(nameof(AuditableEntity.UpdatedByUserId)).HasMaxLength(450);

            // پیش‌مقدار لازم است تا مایگریشنِ افزودن ستون به جدول پر بتواند ساخته شود
            entity.Property(nameof(AuditableEntity.IsDeleted)).HasDefaultValue(false);
            entity.HasIndex(nameof(AuditableEntity.IsDeleted));

            // e => !e.IsDeleted — به‌صورت عمومی روی نوع اجرا می‌شود
            var parameter = Expression.Parameter(clrType, "e");
            var notDeleted = Expression.Not(
                Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));
            entity.HasQueryFilter(Expression.Lambda(notDeleted, parameter));
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// ثبت خودکار ردِ پیگیری و تبدیل حذف به حذف نرم، پیش از هر commit.
    /// <para>
    /// حذف نرم اینجا انجام می‌شود نه در سرویس‌ها، تا هیچ مسیری — سرویس، ریپازیتوری،
    /// یا حذف آبشاریِ EF — نتواند سهواً ردیف را فیزیکی پاک کند.
    /// </para>
    /// </summary>
    private void ApplyAuditAndSoftDelete()
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var userId = _currentUser?.UserId;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // فقط وقتی خودِ فراخواننده مقدار نگذاشته باشد
                    if (entry.Entity.CreatedAt == default)
                        entry.Entity.CreatedAt = now;

                    if (string.IsNullOrEmpty(entry.Entity.CreatedByUserId) && userId is not null)
                        entry.Entity.CreatedByUserId = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedByUserId = userId;
                    break;

                case EntityState.Deleted:
                    // حذف نرم: ردیف فیزیکی هیچ‌وقت پاک نمی‌شود
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    // حذف هم یک تغییر است؛ بدون این، «چه وقت حذف شد» ثبت نمی‌شد
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedByUserId = userId;
                    break;
            }
        }
    }
}

/// <summary>
/// کلید مدل برای <see cref="PlatformDbContext"/>.
/// <para>
/// پیش‌فرض EF فقط نوع context را کلید می‌گیرد، در حالی که مدلِ این context به
/// ماژول‌های ساخته‌شده در سازنده بستگی دارد. بدون این کلاس، دو context با مجموعهٔ
/// ماژول متفاوت (مثلاً دو کلاس تست یا یک تست بدون ماژول در کنار تستِ ماژول‌دار)
/// یک مدلِ مشترک برمی‌دارند و دومی، مدلِ اولی را می‌بیند — خطایی که فقط در بعضی
/// ترتیب‌ها ظاهر می‌شود.
/// </para>
/// </summary>
public sealed class PlatformModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <inheritdoc />
    public object Create(DbContext context, bool designTime)
        => context is PlatformDbContext platform
            ? new PlatformModelCacheKey(platform.GetType(), designTime, platform.Modules)
            : new object();
}

/// <summary>کلید مدل = نوع context + حالت design-time + مجموعهٔ نوعِ ماژول‌ها.</summary>
internal sealed class PlatformModelCacheKey : IEquatable<PlatformModelCacheKey>
{
    private readonly IReadOnlyList<Type> _moduleTypes;

    public PlatformModelCacheKey(
        Type contextType, bool designTime, IReadOnlyList<IPlatformModule> modules)
    {
        ContextType = contextType;
        DesignTime = designTime;
        _moduleTypes = modules.Select(m => m.GetType()).ToArray();
    }

    public Type ContextType { get; }

    public bool DesignTime { get; }

    public bool Equals(PlatformModelCacheKey? other)
        => other is not null
            && ContextType == other.ContextType
            && DesignTime == other.DesignTime
            && _moduleTypes.SequenceEqual(other._moduleTypes);

    public override bool Equals(object? obj) => Equals(obj as PlatformModelCacheKey);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContextType);
        hash.Add(DesignTime);
        foreach (var type in _moduleTypes) hash.Add(type);
        return hash.ToHashCode();
    }
}