using Microsoft.Extensions.DependencyInjection;
using Platform.Application.Jobs;
using Platform.Infrastructure.Data;

namespace Meetings;

/// <summary>مجوزهای ماژول جلسات — کلیدهای claim روی نقش‌ها.</summary>
public static class MeetingPermissions
{
    /// <summary>مدیریت جلسات (تعریف، مدیریت حضور و غیاب، صورت‌جلسه).</summary>
    public const string Manage = "meetings.manage";
}

/// <summary>
/// نوع فایل در <c>IFileStorage</c> برای فایل‌های جلسه. مالک، شناسهٔ جلسه است.
/// قرارداد مشترک آپلود (MeetingManage) و دانلود (Program) — یک‌جا تعریف می‌شود
/// تا رشته‌ها در دو فایل از هم جدا نمانند.
/// </summary>
public static class MeetingFileTypes
{
    public const string Audio = "meeting-audio";
    public const string Photo = "meeting-photo";
}

/// <summary>
/// کلیدهای تنظیمات ماژول جلسات (قالب‌های پیام دعوت) + مقادیر پیش‌فرض‌شان.
/// جاگذاری‌ها: {Title} موضوع، {When} زمان شمسی، {Where} مکان/لینک، {Name} نام مدعو.
/// </summary>
public static class MeetingSettingKeys
{
    public const string InvitationSmsTemplate = "meetings:invitation-sms-template";
    public const string InvitationEmailSubject = "meetings:invitation-email-subject";

    public const string DefaultInvitationSmsTemplate =
        "دعوت به جلسه «{Title}» — {When} — {Where}. پاسخ در بخش «جلسات من».";

    public const string DefaultInvitationEmailSubject = "دعوت به جلسه: {Title}";
}

/// <summary>ثبت سرویس‌های ماژول جلسات. در Program.cs میزبان بعد از AddSampleModule فراخوانی می‌شود.</summary>
public static class MeetingsSetup
{
    public static IServiceCollection AddMeetingsModule(this IServiceCollection services)
    {
        // ماژول EF: پیکربندی مدل «جلسات» به DbContext پایه تزریق می‌شود
        services.AddSingleton<IPlatformModule, MeetingsDomainModule>();

        services.AddScoped<IMeetingRepository, MeetingRepository>();
        services.AddScoped<IMeetingUserDirectory, MeetingUserDirectory>();
        services.AddScoped<IMeetingService, MeetingService>();

        // یادآوری خودکار جلسات — روی RecurringJobRunner پایه می‌نشیند
        services.AddSingleton<IRecurringJob, MeetingReminderJob>();

        return services;
    }
}
