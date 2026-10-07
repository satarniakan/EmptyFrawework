using Platform.Application.Helpers;
using Platform.Application.Services;
using Platform.Domain.Entities;
using Platform.Domain.Enums;
using Platform.Domain.Exceptions;
using Platform.Domain.Interfaces;

namespace Meetings;

/// <summary>درخواست ساخت جلسه.</summary>
public record MeetingCreateRequest(
    string Title,
    string? Description,
    MeetingKind Kind,
    string? Location,
    string? MeetingLink,
    DateTime StartAt,
    string CreatorUserId,
    IReadOnlyList<string> InviteeUserIds);

/// <summary>اطلاعات یک مدعو برای نمایش در صفحات ادمین.</summary>
public record InviteeInfo(
    string UserId,
    string FullName,
    string? PhoneNumber,
    string? Email,
    InviteResponse Response,
    DateTime? RespondedAt,
    AttendanceMark? Attendance);

/// <summary>اطلاعات یک پیشنهاد زمان برای نمایش در صفحات ادمین.</summary>
public record ProposalInfo(
    int Id,
    string ProposerName,
    DateTime ProposedStartAt,
    string? Note,
    ProposalStatus Status,
    DateTime CreatedAt);

public interface IMeetingService
{
    /// <summary>همهٔ جلسات برای فهرست ادمین.</summary>
    Task<List<Meeting>> GetAllMeetingsAsync();

    /// <summary>تعریف جلسه و ارسال دعوت‌نامه (ایمیل/پیامک در صف Outbox + اعلان کارتابل) به مدعوین.</summary>
    Task<Meeting> CreateAsync(MeetingCreateRequest request);

    /// <summary>دعوت‌نامه‌های یک کاربر برای داشبورد «جلسات من».</summary>
    Task<List<MeetingInvitee>> GetInvitesForUserAsync(string userId);

    /// <summary>جلساتی که کاربر سازنده یا مدعوی آن‌هاست — برای تقویم صفحهٔ اصلی.</summary>
    Task<List<Meeting>> GetCalendarMeetingsAsync(string userId);

    /// <summary>جلسه با دعوت‌شدگان و پیشنهادهای زمان.</summary>
    Task<Meeting?> GetMeetingAsync(int meetingId);

    Task<List<InviteeInfo>> GetInviteeDetailsAsync(int meetingId);
    Task<List<ProposalInfo>> GetProposalDetailsAsync(int meetingId);

    /// <summary>پذیرش یا رد دعوت توسط خودِ مدعو.</summary>
    Task<bool> RespondAsync(int meetingId, string userId, InviteResponse response);

    /// <summary>پیشنهاد زمان جدید توسط مدعو.</summary>
    Task<bool> ProposeNewTimeAsync(int meetingId, string userId, DateTime proposedStartAt, string? note);

    /// <summary>تصمیم ادمین دربارهٔ پیشنهاد زمان. پذیرش، زمان جلسه را جابه‌جا می‌کند و پاسخ‌ها را به «در انتظار» برمی‌گرداند.</summary>
    Task<bool> DecideProposalAsync(int proposalId, bool accept);

    /// <summary>علامت‌زدن حاضر/غایب توسط ادمین.</summary>
    Task SetAttendanceAsync(int meetingId, string userId, AttendanceMark? mark);

    /// <summary>ذخیرهٔ متن صورت‌جلسه (پس از پاک‌سازی HTML).</summary>
    Task SaveMinutesAsync(int meetingId, string? html);

    /// <summary>ارسال صورت‌جلسه به کارتابل همهٔ مدعوین؛ تعداد گیرندگان را برمی‌گرداند.</summary>
    Task<int> SendMinutesAsync(int meetingId);

    /// <summary>آیا این کاربر اجازهٔ دیدن جزئیات این جلسه را دارد؟ (مدعو یا سازنده)</summary>
    Task<bool> CanViewAsync(int meetingId, string userId);
}

/// <summary>
/// منطق کسب‌وکار جلسات. به واحد کارِ پایه (IPlatformUnitOfWork) و ریپازیتوری خودش وصل است؛
/// از واحد کارِ مشتق‌شدهٔ ماژول نمونه استفاده نمی‌کند تا هر دو ماژول مستقل از هم حذف‌شدنی بمانند
/// (هر دو در نهایت روی همان DbContext اسکوپ‌دار کار می‌کنند).
/// </summary>
public class MeetingService : IMeetingService
{
    private readonly IMeetingRepository _meetings;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IMeetingUserDirectory _users;
    private readonly INotificationService _notifications;
    private readonly IOutboxService _outbox;
    private readonly TimeProvider _clock;

    public MeetingService(
        IMeetingRepository meetings,
        IPlatformUnitOfWork unitOfWork,
        IMeetingUserDirectory users,
        INotificationService notifications,
        IOutboxService outbox,
        TimeProvider? clock = null)
    {
        _meetings = meetings;
        _unitOfWork = unitOfWork;
        _users = users;
        _notifications = notifications;
        _outbox = outbox;
        _clock = clock ?? TimeProvider.System;
    }

    public Task<List<Meeting>> GetAllMeetingsAsync() =>
        _meetings.GetAllAsync();

    public async Task<Meeting> CreateAsync(MeetingCreateRequest request)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length == 0)
            throw new BusinessRuleException("موضوع جلسه الزامی است.");

        if (request.Kind == MeetingKind.Virtual && string.IsNullOrWhiteSpace(request.MeetingLink))
            throw new BusinessRuleException("برای جلسهٔ غیرحضوری، لینک فضای جلسه الزامی است.");

        var inviteeIds = request.InviteeUserIds.Distinct().ToList();
        if (inviteeIds.Count == 0)
            throw new BusinessRuleException("حداقل یک مدعو انتخاب کنید.");

        var meeting = new Meeting
        {
            Title = title,
            Description = request.Description?.Trim(),
            Kind = request.Kind,
            Location = request.Kind == MeetingKind.InPerson ? request.Location?.Trim() : null,
            MeetingLink = request.Kind == MeetingKind.Virtual ? request.MeetingLink?.Trim() : null,
            StartAt = request.StartAt,
            CreatedByUserId = request.CreatorUserId
        };

        foreach (var userId in inviteeIds)
        {
            meeting.Invitees.Add(new MeetingInvitee { UserId = userId });
        }

        await _meetings.AddAsync(meeting);
        await _unitOfWork.CompleteAsync();

        // اعلان کارتابل برای همهٔ مدعوین (یک Save مشترک)
        await _notifications.NotifyUsersAsync(
            inviteeIds,
            "دعوت به جلسه",
            $"به جلسهٔ «{meeting.Title}» دعوت شده‌اید. لطفاً پاسخ خود را ثبت کنید.",
            NotificationType.System,
            "/my-meetings");

        // ایمیل/پیامک در صف Outbox — ارسال واقعی را OutboxProcessor انجام می‌دهد
        var contacts = await _users.GetUsersAsync(inviteeIds);
        foreach (var contact in contacts)
        {
            if (!string.IsNullOrWhiteSpace(contact.Email))
            {
                await _outbox.QueueEmailAsync(
                    contact.Email!,
                    $"دعوت به جلسه: {meeting.Title}",
                    BuildInvitationEmail(meeting, contact));
            }

            if (!string.IsNullOrWhiteSpace(contact.PhoneNumber))
            {
                await _outbox.QueueSmsAsync(contact.PhoneNumber!, BuildInvitationSms(meeting));
            }
        }

        return meeting;
    }

    public Task<List<MeetingInvitee>> GetInvitesForUserAsync(string userId) =>
        _meetings.GetInvitesForUserAsync(userId);

    public Task<List<Meeting>> GetCalendarMeetingsAsync(string userId) =>
        _meetings.GetForUserOrCreatorAsync(userId);

    public Task<Meeting?> GetMeetingAsync(int meetingId) =>
        _meetings.GetByIdAsync(meetingId);

    public async Task<List<InviteeInfo>> GetInviteeDetailsAsync(int meetingId)
    {
        var meeting = await _meetings.GetByIdAsync(meetingId)
            ?? throw new NotFoundException("جلسه", meetingId);

        var contacts = await _users.GetUsersAsync(meeting.Invitees.Select(i => i.UserId).ToList());
        var byId = contacts.ToDictionary(c => c.UserId);

        return meeting.Invitees
            .Select(i => byId.TryGetValue(i.UserId, out var contact)
                ? new InviteeInfo(i.UserId, contact.DisplayName, contact.PhoneNumber, contact.Email,
                    i.Response, i.RespondedAt, i.Attendance)
                : new InviteeInfo(i.UserId, "کاربر حذف‌شده", null, null, i.Response, i.RespondedAt, i.Attendance))
            .ToList();
    }

    public async Task<List<ProposalInfo>> GetProposalDetailsAsync(int meetingId)
    {
        var meeting = await _meetings.GetByIdAsync(meetingId)
            ?? throw new NotFoundException("جلسه", meetingId);

        var pendingIds = meeting.TimeProposals.Select(p => p.ProposedByUserId).ToList();
        var contacts = await _users.GetUsersAsync(pendingIds);
        var byId = contacts.ToDictionary(c => c.UserId);

        return meeting.TimeProposals
            .Select(p => new ProposalInfo(
                p.Id,
                byId.TryGetValue(p.ProposedByUserId, out var contact) ? contact.DisplayName : "کاربر حذف‌شده",
                p.ProposedStartAt,
                p.Note,
                p.Status,
                p.CreatedAt))
            .ToList();
    }

    public async Task<bool> RespondAsync(int meetingId, string userId, InviteResponse response)
    {
        var invitee = await _meetings.GetInviteeAsync(meetingId, userId);
        if (invitee?.Meeting is null) return false;

        if (invitee.Response != response)
        {
            invitee.Response = response;
            invitee.RespondedAt = _clock.GetUtcNow().UtcDateTime;
            await _unitOfWork.CompleteAsync();
        }

        if (invitee.Meeting.CreatedByUserId != userId)
        {
            var responder = (await _users.GetUsersAsync([userId])).FirstOrDefault();
            var verb = response == InviteResponse.Accepted ? "پذیرفت" : "رد کرد";
            await _notifications.NotifyAsync(
                invitee.Meeting.CreatedByUserId,
                "پاسخ به دعوت جلسه",
                $"«{responder?.DisplayName ?? "کاربر"}» دعوت جلسهٔ «{invitee.Meeting.Title}» را {verb}.",
                NotificationType.System,
                $"/meetings/{meetingId}/manage");
        }

        return true;
    }

    public async Task<bool> ProposeNewTimeAsync(int meetingId, string userId, DateTime proposedStartAt, string? note)
    {
        var invitee = await _meetings.GetInviteeAsync(meetingId, userId);
        if (invitee?.Meeting is null) return false;

        if (proposedStartAt <= _clock.GetUtcNow().UtcDateTime)
            throw new BusinessRuleException("زمان پیشنهادی باید در آینده باشد.");

        await _meetings.AddProposalAsync(new MeetingTimeProposal
        {
            MeetingId = meetingId,
            ProposedByUserId = userId,
            ProposedStartAt = proposedStartAt,
            Note = note?.Trim()
        });
        await _unitOfWork.CompleteAsync();

        if (invitee.Meeting.CreatedByUserId != userId)
        {
            var proposer = (await _users.GetUsersAsync([userId])).FirstOrDefault();
            await _notifications.NotifyAsync(
                invitee.Meeting.CreatedByUserId,
                "پیشنهاد زمان جدید",
                $"«{proposer?.DisplayName ?? "کاربر"}» برای جلسهٔ «{invitee.Meeting.Title}» زمان {PersianDateHelper.ToPersianDateTime(proposedStartAt)} را پیشنهاد داد.",
                NotificationType.System,
                $"/meetings/{meetingId}/manage");
        }

        return true;
    }

    public async Task<bool> DecideProposalAsync(int proposalId, bool accept)
    {
        var proposal = await _meetings.GetProposalAsync(proposalId);
        if (proposal?.Meeting is null || proposal.Status != ProposalStatus.Pending) return false;

        var meeting = proposal.Meeting;
        proposal.Status = accept ? ProposalStatus.Accepted : ProposalStatus.Rejected;
        proposal.DecidedAt = _clock.GetUtcNow().UtcDateTime;

        if (accept)
        {
            meeting.StartAt = proposal.ProposedStartAt;

            // زمان جلسه عوض شده؛ پاسخ‌های قبلی به زمان قدیم معتبر نیستند — همه به «در انتظار» برمی‌گردند
            foreach (var invitee in meeting.Invitees)
            {
                invitee.Response = InviteResponse.Pending;
                invitee.RespondedAt = null;
                invitee.Attendance = null;
            }

            // بقیهٔ پیشنهادهای در انتظار دیگر موضوعیت ندارند
            foreach (var other in meeting.TimeProposals.Where(p => p.Id != proposal.Id && p.Status == ProposalStatus.Pending))
            {
                other.Status = ProposalStatus.Rejected;
                other.DecidedAt = proposal.DecidedAt;
            }
        }

        await _unitOfWork.CompleteAsync();

        if (accept)
        {
            var inviteeIds = meeting.Invitees.Select(i => i.UserId).ToList();
            await _notifications.NotifyUsersAsync(
                inviteeIds,
                "تغییر زمان جلسه",
                $"جلسهٔ «{meeting.Title}» به {PersianDateHelper.ToPersianDateTime(meeting.StartAt)} موکول شد. لطفاً پاسخ خود را به‌روز کنید.",
                NotificationType.System,
                "/my-meetings");

            if (proposal.ProposedByUserId != meeting.CreatedByUserId)
            {
                await _notifications.NotifyAsync(
                    proposal.ProposedByUserId,
                    "پیشنهاد زمان شما پذیرفته شد",
                    $"پیشنهاد شما برای جلسهٔ «{meeting.Title}» پذیرفته شد.",
                    NotificationType.System,
                    "/my-meetings");
            }
        }
        else if (proposal.ProposedByUserId != meeting.CreatedByUserId)
        {
            await _notifications.NotifyAsync(
                proposal.ProposedByUserId,
                "پیشنهاد زمان شما رد شد",
                $"پیشنهاد شما برای جلسهٔ «{meeting.Title}» پذیرفته نشد.",
                NotificationType.System,
                "/my-meetings");
        }

        return true;
    }

    public async Task SetAttendanceAsync(int meetingId, string userId, AttendanceMark? mark)
    {
        var invitee = await _meetings.GetInviteeAsync(meetingId, userId)
            ?? throw new NotFoundException("دعوت‌شده", userId);

        invitee.Attendance = mark;
        await _unitOfWork.CompleteAsync();
    }

    public async Task SaveMinutesAsync(int meetingId, string? html)
    {
        var meeting = await _meetings.GetByIdAsync(meetingId)
            ?? throw new NotFoundException("جلسه", meetingId);

        meeting.MinutesHtml = MinutesSanitizer.Clean(html);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<int> SendMinutesAsync(int meetingId)
    {
        var meeting = await _meetings.GetByIdAsync(meetingId)
            ?? throw new NotFoundException("جلسه", meetingId);

        if (string.IsNullOrWhiteSpace(meeting.MinutesHtml))
            throw new BusinessRuleException("ابتدا متن صورت‌جلسه را ذخیره کنید.");

        var inviteeIds = meeting.Invitees.Select(i => i.UserId).ToList();
        if (inviteeIds.Count == 0)
            throw new BusinessRuleException("این جلسه مدعویی ندارد.");

        await _notifications.NotifyUsersAsync(
            inviteeIds,
            $"صورت‌جلسه: {meeting.Title}",
            "صورت‌جلسهٔ جلسه برای شما ارسال شد.",
            NotificationType.System,
            $"/my-meetings/{meetingId}");

        meeting.MinutesSentAt = _clock.GetUtcNow().UtcDateTime;
        await _unitOfWork.CompleteAsync();
        return inviteeIds.Count;
    }

    public async Task<bool> CanViewAsync(int meetingId, string userId)
    {
        var meeting = await _meetings.GetByIdAsync(meetingId);
        if (meeting is null) return false;
        return meeting.CreatedByUserId == userId || meeting.Invitees.Any(i => i.UserId == userId);
    }

    private static string KindLabel(MeetingKind kind) =>
        kind == MeetingKind.InPerson ? "حضوری" : "غیرحضوری";

    private static string WhereLabel(Meeting meeting) =>
        meeting.Kind == MeetingKind.InPerson
            ? $"حضوری — مکان: {meeting.Location ?? "نامشخص"}"
            : $"غیرحضوری — لینک جلسه: {meeting.MeetingLink ?? "نامشخص"}";

    private static string BuildInvitationEmail(Meeting meeting, MeetingUser contact) =>
        $"""
        {contact.DisplayName} عزیز،

        شما به جلسهٔ زیر دعوت شده‌اید:

        موضوع: {meeting.Title}
        زمان: {PersianDateHelper.ToPersianDateTime(meeting.StartAt)}
        نوع برگزاری: {WhereLabel(meeting)}
        {(string.IsNullOrWhiteSpace(meeting.Description) ? string.Empty : $"\nتوضیحات: {meeting.Description}\n")}

        برای پذیرش یا رد دعوت (و در صورت نیاز پیشنهاد زمان جدید) وارد سامانه شوید
        و در بخش «جلسات من» پاسخ خود را ثبت کنید.
        """;

    private static string BuildInvitationSms(Meeting meeting) =>
        $"دعوت به جلسه «{meeting.Title}» — {PersianDateHelper.ToPersianDateTime(meeting.StartAt)} — " +
        (meeting.Kind == MeetingKind.InPerson
            ? $"حضوری ({meeting.Location ?? "نامشخص"})"
            : "غیرحضوری؛ لینک در سامانه") +
        ". پاسخ در بخش «جلسات من».";
}
