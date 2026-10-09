using Meetings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Platform.Domain.Common;
using Platform.Domain.Interfaces;
using Platform.Infrastructure.Data;
using Xunit;

namespace Platform.Tests;

/// <summary>
/// قرارداد پایهٔ انتیتی: ثبت خودکار «چه کسی/چه وقت» و تبدیل حذف به حذف نرم.
/// </summary>
public class AuditableEntityTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);

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
    /// ماژول جلسات لازم است چون پیکربندی مدل Meeting همان‌جاست.
    /// </summary>
    private static PlatformDbContext CreateContext(string? userId = "user-1")
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .ReplaceService<IModelCacheKeyFactory, PlatformModelCacheKeyFactory>()
            .UseInMemoryDatabase($"auditable-{Guid.NewGuid()}")
            .Options;

        return new PlatformDbContext(
            options,
            new IPlatformModule[] { new MeetingsDomainModule() },
            new FakeUser(userId),
            new FixedClock());
    }

    [Fact]
    public async Task Add_FillsCreatedAtAndCreatedBy_WhenNotSet()
    {
        await using var db = CreateContext();

        db.Set<Meeting>().Add(new Meeting { Title = "جلسهٔ تازه" });
        await db.SaveChangesAsync();

        var saved = await db.Set<Meeting>().SingleAsync();
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

        db.Set<Meeting>().Add(new Meeting { Title = "قدیمی", CreatedAt = explicitDate });
        await db.SaveChangesAsync();

        var saved = await db.Set<Meeting>().SingleAsync();
        Assert.Equal(explicitDate, saved.CreatedAt);
    }

    [Fact]
    public async Task Update_RecordsWhoAndWhen()
    {
        await using var db = CreateContext();
        db.Set<Meeting>().Add(new Meeting { Title = "اول" });
        await db.SaveChangesAsync();

        var meeting = await db.Set<Meeting>().SingleAsync();
        meeting.Title = "دوم";
        await db.SaveChangesAsync();

        // همان اسکوپ — ردیف از change tracker خوانده شده پس مقادیر به‌روز هستند
        var updated = await db.Set<Meeting>().SingleAsync();
        Assert.Equal(Now, updated.UpdatedAt);
        Assert.Equal("user-1", updated.UpdatedByUserId);
    }

    [Fact]
    public async Task Delete_MarksRowSoftDeleted_InsteadOfRemovingIt()
    {
        await using var db = CreateContext();
        db.Set<Meeting>().Add(new Meeting { Title = "برای حذف" });
        await db.SaveChangesAsync();

        var meeting = await db.Set<Meeting>().SingleAsync();
        db.Set<Meeting>().Remove(meeting);
        await db.SaveChangesAsync();

        // کوئری عادی نباید چیزی برگرداند...
        Assert.Empty(await db.Set<Meeting>().ToListAsync());

        // ...ولی ردیف فیزیکاً پابرجاست و فقط پرچم حذف خورده
        var kept = db.Set<Meeting>().Local.Single();
        Assert.True(kept.IsDeleted);
        Assert.Equal(Now, kept.UpdatedAt);
    }

    [Fact]
    public async Task RemoveDecision_SoftDeletes_SoCascadeStaysConsistent()
    {
        await using var db = CreateContext();

        var meeting = new Meeting { Title = "میزبان" };
        meeting.Decisions.Add(new MeetingDecision { Content = "مصوبه" });
        db.Set<Meeting>().Add(meeting);
        await db.SaveChangesAsync();

        var decision = await db.Set<MeetingDecision>().SingleAsync();
        db.Set<MeetingDecision>().Remove(decision);
        await db.SaveChangesAsync();

        Assert.Empty(await db.Set<MeetingDecision>().ToListAsync());
        var kept = db.Set<MeetingDecision>().Local.Single();
        Assert.True(kept.IsDeleted);
    }

    [Fact]
    public async Task WithoutUser_DoesNotWriteEmptyCreatedByAsIfItWereAUser()
    {
        await using var db = CreateContext(userId: null);

        db.Set<Meeting>().Add(new Meeting { Title = "سیستمی" });
        await db.SaveChangesAsync();

        var saved = await db.Set<Meeting>().SingleAsync();
        Assert.Equal(string.Empty, saved.CreatedByUserId);
        Assert.Equal(Now, saved.CreatedAt);
    }

    [Fact]
    public void AuditableEntity_ImplementsSoftDeletable()
    {
        Assert.IsAssignableFrom<ISoftDeletable>(new Meeting());
    }
}
