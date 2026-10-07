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
/// </summary>
public class Meeting
{
    public int Id { get; set; }

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

    /// <summary>کاربر ادمینی که جلسه را تعریف کرده (IdentityUser.Id).</summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>صورت‌جلسه (HTML راست‌چین از ویرایشگر) — پس از «ارسال» برای اعضا در کارتابل می‌رود.</summary>
    public string? MinutesHtml { get; set; }

    /// <summary>آخرین زمانی که صورت‌جلسه به کارتابل اعضا ارسال شد؛ null یعنی هنوز ارسال نشده.</summary>
    public DateTime? MinutesSentAt { get; set; }

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
public class MeetingDecision
{
    public int Id { get; set; }
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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>دعوت‌نامهٔ یک کاربر به یک جلسه.</summary>
public class MeetingInvitee
{
    public int Id { get; set; }
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
public class MeetingTimeProposal
{
    public int Id { get; set; }
    public int MeetingId { get; set; }
    public Meeting? Meeting { get; set; }

    public string ProposedByUserId { get; set; } = string.Empty;

    /// <summary>زمان پیشنهادی (UTC).</summary>
    public DateTime ProposedStartAt { get; set; }

    /// <summary>توضیح دلخواه پیشنهاددهنده.</summary>
    public string? Note { get; set; }

    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
}
