using Platform.Domain.Common;

namespace Meetings;

/// <summary>نوع برگزاری جلسه.</summary>
public enum MeetingKind
{
    InPerson = 1,
    Virtual = 2
}

/// <summary>پاسخ مدعو به دعوت‌نامه.</summary>
public enum InviteResponse
{
    Pending = 0,
    Accepted = 1,
    Declined = 2
}

/// <summary>علامت حضور که ادمین پس از جلسه در صفحهٔ مدیریت ثبت می‌کند.</summary>
public enum AttendanceMark
{
    Attended = 1,
    Absent = 2
}

/// <summary>وضعیت پیشنهاد زمان جدید.</summary>
public enum ProposalStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2
}

/// <summary>
/// یک جلسه. زمان‌ها UTC ذخیره می‌شوند و در UI با <c>PersianDateHelper</c> شمسی نمایش داده می‌شوند.
/// <para>
/// شناسه، سازنده، زمان ایجاد/ویرایش و حذف نرم از <see cref="AuditableEntity"/> می‌آید.
/// کل خانوادهٔ جلسات از همین پایه ارث می‌برند تا حذف نرمِ یک جلسه، فرزندانش را هم
/// بپوشاند و رکورد یتیم ساخته نشود.
/// </para>
/// </summary>
public class Meeting : AuditableEntity
{
    /// <summary>موضوع جلسه.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>توضیح/دستور جلسه (اختیاری).</summary>
    public string? Description { get; set; }

    public MeetingKind Kind { get; set; } = MeetingKind.InPerson;

    /// <summary>محل برگزاری — برای جلسهٔ حضوری.</summary>
    public string? Location { get; set; }

    /// <summary>لینک فضای جلسه — برای جلسهٔ غیرحضوری.</summary>
    public string? MeetingLink { get; set; }

    /// <summary>زمان برگزاری (UTC).</summary>
    public DateTime StartAt { get; set; }

    /// <summary>صورت‌جلسه (HTML راست‌چین از ویرایشگر) — پس از «ارسال» برای اعضا در کارتابل می‌رود.</summary>
    public string? MinutesHtml { get; set; }

    /// <summary>آخرین زمانی که صورت‌جلسه به کارتابل اعضا ارسال شد؛ null یعنی هنوز ارسال نشده.</summary>
    public DateTime? MinutesSentAt { get; set; }

    /// <summary>آخرین زمانی که یادآوری خودکار برای این جلسه ارسال شد؛ null یعنی هنوز یادآوری نشده.</summary>
    public DateTime? ReminderSentAt { get; set; }

    /// <summary>فایل صوتی جلسه — نام فایل ذخیره‌شده روی دیسک (پوشهٔ AppData/meeting-audio میزبان).</summary>
    public string? AudioFileName { get; set; }

    public string? AudioContentType { get; set; }
    public long? AudioSizeBytes { get; set; }
    public string? AudioUploadedByUserId { get; set; }
    public DateTime? AudioUploadedAt { get; set; }

    /// <summary>عکس جلسه (مثلاً عکس گروهی) — نام فایل ذخیره‌شده روی دیسک (پوشهٔ AppData/meeting-photos میزبان).</summary>
    public string? PhotoFileName { get; set; }
    public string? PhotoContentType { get; set; }
    public long? PhotoSizeBytes { get; set; }
    public string? PhotoUploadedByUserId { get; set; }
    public DateTime? PhotoUploadedAt { get; set; }

    public List<MeetingInvitee> Invitees { get; set; } = [];
    public List<MeetingTimeProposal> TimeProposals { get; set; } = [];
    public List<MeetingDecision> Decisions { get; set; } = [];
}

/// <summary>
/// یک بند مصوبهٔ جلسه: متن، مسئول(ین) اقدام (از بین مدعوین و/یا نام آزاد) و مهلت اقدام.
/// </summary>
public class MeetingDecision : AuditableEntity
{
    public int MeetingId { get; set; }
    public Meeting? Meeting { get; set; }

    /// <summary>متن مصوبه.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>مسئولین اقدام از بین مدعوین — جداشده با ویرگول (IdentityUser.Id).</summary>
    public string? AssigneeUserIds { get; set; }

    /// <summary>مسئولین تایپ‌شدهٔ آزاد (خارج از سامانه) — جداشده با ویرگول.</summary>
    public string? AssigneeNames { get; set; }

    /// <summary>مهلت اقدام (UTC).</summary>
    public DateTime? DueAt { get; set; }

    /// <summary>آیا انجام شده است؟</summary>
    public bool IsDone { get; set; }
}

/// <summary>دعوت‌نامهٔ یک کاربر به یک جلسه.</summary>
public class MeetingInvitee : AuditableEntity
{
    public int MeetingId { get; set; }
    public Meeting? Meeting { get; set; }

    /// <summary>کاربر Identity (GUID).</summary>
    public string UserId { get; set; } = string.Empty;

    public InviteResponse Response { get; set; } = InviteResponse.Pending;
    public DateTime? RespondedAt { get; set; }

    /// <summary>حاضر/غایب که ادمین علامت می‌زند؛ null یعنی هنوز علامت نخورده.</summary>
    public AttendanceMark? Attendance { get; set; }
}

/// <summary>پیشنهاد زمان جدید از سوی یک مدعو.</summary>
public class MeetingTimeProposal : AuditableEntity
{
    public int MeetingId { get; set; }
    public Meeting? Meeting { get; set; }

    public string ProposedByUserId { get; set; } = string.Empty;

    /// <summary>زمان پیشنهادی (UTC).</summary>
    public DateTime ProposedStartAt { get; set; }

    /// <summary>توضیح دلخواه پیشنهاددهنده.</summary>
    public string? Note { get; set; }

    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;

    public DateTime? DecidedAt { get; set; }
}
