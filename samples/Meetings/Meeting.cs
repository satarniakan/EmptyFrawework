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

    public List<MeetingInvitee> Invitees { get; set; } = [];
    public List<MeetingTimeProposal> TimeProposals { get; set; } = [];
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
