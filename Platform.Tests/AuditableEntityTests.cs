using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Platform.Domain.Common;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;
using Xunit;

namespace Platform.Tests;

/// <summary>
/// قرارداد پایهٔ انتیتی: ثبت خودکار «چه کسی/چه وقت» و تبدیل حذف به حذف نرم.
/// با انتیتی‌های آزمایشیِ همین فایل (نه ماژول نمونه) تا تست‌های پایه به هیچ دامنه‌ای گره نخورند.
/// </summary>
public class AuditableEntityTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>انتیتی آزمایشی به‌جای انتیتی دامنه.</summary>
    private sealed class TestNote : AuditableEntity
    {
        public string Title { get; set; } = string.Empty;
        public List<TestNoteLine> Lines { get; set; } = [];
    }

    private sealed class TestNoteLine : AuditableEntity
    {
        public int TestNoteId { get; set; }
        public TestNote? TestNote { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    private sealed class TestNotesModule : IPlatformModule
    {
        public string Name => "TestNotes";

        public void ConfigureModel(ModelBuilder builder)
        {
            builder.Entity<TestNote>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            });
            builder.Entity<TestNoteLine>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.TestNote)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.TestNoteId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
    }

    private sealed class FakeUser(string? userId) : ICurrentUser
    {
        public string? UserId { get; } = userId;
    }

    /// <summary>
    /// هر بار دیتابیس in-memory با نام یکتا تا تست‌ها روی یکدیگر اثر نگذارند.
    /// ماژول آزمایشی لازم است چون پیکربندی مدل انتیتی تست همان‌جاست.
    /// </summary>
    private static PlatformDbContext CreateContext(string? userId = "user-1")
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .ReplaceService<IModelCacheKeyFactory, PlatformModelCacheKeyFactory>()
            .UseInMemoryDatabase($"auditable-{Guid.NewGuid()}")
            .Options;

        return new PlatformDbContext(
            options,
            new IPlatformModule[] { new TestNotesModule() },
            new FakeUser(userId),
            new FixedClock());
    }

    [Fact]
    public async Task Add_FillsCreatedAtAndCreatedBy_WhenNotSet()
    {
        await using var db = CreateContext();

        db.Set<TestNote>().Add(new TestNote { Title = "یادداشت تازه" });
        await db.SaveChangesAsync();

        var saved = await db.Set<TestNote>().SingleAsync();
        Assert.Equal(Now, saved.CreatedAt);
        Assert.Equal("user-1", saved.CreatedByUserId);
        Assert.Null(saved.UpdatedAt);
        Assert.False(saved.IsDeleted);
    }

    [Fact]
    public async Task Add_KeepsExplicitCreatedAt_WhenCallerProvidedIt()
    {
        await using var db = CreateContext();
        var explicitDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        db.Set<TestNote>().Add(new TestNote { Title = "قدیمی", CreatedAt = explicitDate });
        await db.SaveChangesAsync();

        var saved = await db.Set<TestNote>().SingleAsync();
        Assert.Equal(explicitDate, saved.CreatedAt);
    }

    [Fact]
    public async Task Update_RecordsWhoAndWhen()
    {
        await using var db = CreateContext();
        db.Set<TestNote>().Add(new TestNote { Title = "اول" });
        await db.SaveChangesAsync();

        var note = await db.Set<TestNote>().SingleAsync();
        note.Title = "دوم";
        await db.SaveChangesAsync();

        // همان اسکوپ — ردیف از change tracker خوانده شده پس مقادیر به‌روز هستند
        var updated = await db.Set<TestNote>().SingleAsync();
        Assert.Equal(Now, updated.UpdatedAt);
        Assert.Equal("user-1", updated.UpdatedByUserId);
    }

    [Fact]
    public async Task Delete_MarksRowSoftDeleted_InsteadOfRemovingIt()
    {
        await using var db = CreateContext();
        db.Set<TestNote>().Add(new TestNote { Title = "برای حذف" });
        await db.SaveChangesAsync();

        var note = await db.Set<TestNote>().SingleAsync();
        db.Set<TestNote>().Remove(note);
        await db.SaveChangesAsync();

        // کوئری عادی نباید چیزی برگرداند...
        Assert.Empty(await db.Set<TestNote>().ToListAsync());

        // ...ولی ردیف فیزیکاً پابرجاست و فقط پرچم حذف خورده
        var kept = db.Set<TestNote>().Local.Single();
        Assert.True(kept.IsDeleted);
        Assert.Equal(Now, kept.UpdatedAt);
    }

    [Fact]
    public async Task RemoveChild_SoftDeletes_SoCascadeStaysConsistent()
    {
        await using var db = CreateContext();

        var note = new TestNote { Title = "میزبان" };
        note.Lines.Add(new TestNoteLine { Content = "سطر" });
        db.Set<TestNote>().Add(note);
        await db.SaveChangesAsync();

        var line = await db.Set<TestNoteLine>().SingleAsync();
        db.Set<TestNoteLine>().Remove(line);
        await db.SaveChangesAsync();

        Assert.Empty(await db.Set<TestNoteLine>().ToListAsync());
        var kept = db.Set<TestNoteLine>().Local.Single();
        Assert.True(kept.IsDeleted);
    }

    [Fact]
    public async Task WithoutUser_DoesNotWriteEmptyCreatedByAsIfItWereAUser()
    {
        await using var db = CreateContext(userId: null);

        db.Set<TestNote>().Add(new TestNote { Title = "سیستمی" });
        await db.SaveChangesAsync();

        var saved = await db.Set<TestNote>().SingleAsync();
        Assert.Equal(string.Empty, saved.CreatedByUserId);
        Assert.Equal(Now, saved.CreatedAt);
    }

    [Fact]
    public void AuditableEntity_ImplementsSoftDeletable()
    {
        Assert.IsAssignableFrom<ISoftDeletable>(new TestNote());
    }
}
