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

/// <summary>یک بند مصوبه با نام‌هایResolve‌شدهٔ مسئولین برای نمایش.</summary>
public record DecisionInfo(
    int Id,
    string Content,
    List<string> Assignees,
    DateTime? DueAt,
    bool IsDone);

/// <summary>ردیف مدعو با نام Resolve‌شده برای گزارش.</summary>
public record ReportInviteeRow(
    string FullName,
    string? PhoneNumber,
    InviteResponse Response,
    DateTime? RespondedAt,
    AttendanceMark? Attendance);

/// <summary>جلسهٔ گزارش‌شده با مدعوینِ نام‌دار.</summary>
public record MeetingReportItem(Meeting Meeting, List<ReportInviteeRow> Invitees);

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

    /// <summary>ثبت فایل صوتی بارگذاری‌شدهٔ جلسه (فایل از قبل روی دیسک ذخیره شده است).</summary>
    Task SetAudioAsync(int meetingId, string storedFileName, string contentType, long sizeBytes, string userId);

    /// <summary>حذف فایل صوتی جلسه (رکورد را خالی می‌کند؛ حذف فایل از دیسک با فراخواننده است).</summary>
    Task<string?> ClearAudioAsync(int meetingId, string userId);

    /// <summary>ثبت عکس جلسه (فایل از قبل روی دیسک ذخیره شده است).</summary>
    Task SetPhotoAsync(int meetingId, string storedFileName, string contentType, long sizeBytes, string userId);

    /// <summary>حذف عکس جلسه (رکورد را خالی می‌کند؛ حذف فایل از دیسک با فراخواننده است).</summary>
    Task<string?> ClearPhotoAsync(int meetingId, string userId);

    // ===== مصوبات =====

    /// <summary>افزودن بند مصوبه با مسئولین (مدعوین و/یا نام آزاد) و مهلت اقدام.</summary>
    Task<DecisionInfo> AddDecisionAsync(int meetingId, string content, DateTime? dueAtUtc,
        IReadOnlyList<string> assigneeUserIds, string? externalAssigneeNames, string createdByUserId);

    Task RemoveDecisionAsync(int decisionId);
    Task ToggleDecisionDoneAsync(int decisionId, bool isDone);
    Task<List<DecisionInfo>> GetDecisionsForMeetingAsync(int meetingId);

    // ===== گزارش‌ها =====

    /// <summary>جلسات در بازهٔ زمانی (UTC) با مدعوینِ نام‌دار — برای گزارش جلسات و مصوبات.</summary>
    Task<List<MeetingReportItem>> GetReportAsync(DateTime fromUtc, DateTime toUtc);

    /// <summary>دعوت‌نامه‌های یک فرد در بازهٔ زمانی (UTC) — برای گزارش حضور.</summary>
    Task<List<MeetingInvitee>> GetPersonAttendanceAsync(string userId, DateTime fromUtc, DateTime toUtc);

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

        // پس از گذشتهٔ زمان جلسه، ثبت یا تغییر پاسخ معنا ندارد
        if (invitee.Meeting.StartAt <= _clock.GetUtcNow().UtcDateTime)
            throw new BusinessRuleException("زمان جلسه گذشته است و امکان ثبت یا تغییر پاسخ وجود ندارد.");

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

        // برای جلسه‌ای که وقتش گذشته، پیشنهاد زمان بی‌معناست
        if (invitee.Meeting.StartAt <= _clock.GetUtcNow().UtcDateTime)
            throw new BusinessRuleException("زمان جلسه گذشته است و امکان پیشنهاد زمان جدید وجود ندارد.");

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

    public async Task SetAudioAsync(int meetingId, string storedFileName, string contentType, long sizeBytes, string userId)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
            throw new BusinessRuleException("نام فایل صوتی نامعتبر است.");

        var meeting = await _meetings.GetByIdAsync(meetingId)
            ?? throw new NotFoundException("جلسه", meetingId);

        // بارگذاری جدید جای فایل قبلی را می‌گیرد
        meeting.AudioFileName = storedFileName.Trim();
        meeting.AudioContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        meeting.AudioSizeBytes = sizeBytes;
        meeting.AudioUploadedByUserId = userId;
        meeting.AudioUploadedAt = _clock.GetUtcNow().UtcDateTime;

        await _unitOfWork.CompleteAsync();
    }

    public async Task<string?> ClearAudioAsync(int meetingId, string userId)
    {
        var meeting = await _meetings.GetByIdAsync(meetingId)
            ?? throw new NotFoundException("جلسه", meetingId);

        var removed = meeting.AudioFileName;
        meeting.AudioFileName = null;
        meeting.AudioContentType = null;
        meeting.AudioSizeBytes = null;
        meeting.AudioUploadedByUserId = null;
        meeting.AudioUploadedAt = null;

        await _unitOfWork.CompleteAsync();
        return removed;
    }

    public async Task SetPhotoAsync(int meetingId, string storedFileName, string contentType, long sizeBytes, string userId)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
            throw new BusinessRuleException("نام فایل عکس نامعتبر است.");

        var meeting = await _meetings.GetByIdAsync(meetingId)
            ?? throw new NotFoundException("جلسه", meetingId);

        // بارگذاری جدید جای فایل قبلی را می‌گیرد
        meeting.PhotoFileName = storedFileName.Trim();
        meeting.PhotoContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        meeting.PhotoSizeBytes = sizeBytes;
        meeting.PhotoUploadedByUserId = userId;
        meeting.PhotoUploadedAt = _clock.GetUtcNow().UtcDateTime;

        await _unitOfWork.CompleteAsync();
    }

    public async Task<string?> ClearPhotoAsync(int meetingId, string userId)
    {
        var meeting = await _meetings.GetByIdAsync(meetingId)
            ?? throw new NotFoundException("جلسه", meetingId);

        var removed = meeting.PhotoFileName;
        meeting.PhotoFileName = null;
        meeting.PhotoContentType = null;
        meeting.PhotoSizeBytes = null;
        meeting.PhotoUploadedByUserId = null;
        meeting.PhotoUploadedAt = null;

        await _unitOfWork.CompleteAsync();
        return removed;
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

    // ===== مصوبات =====

    public async Task<DecisionInfo> AddDecisionAsync(int meetingId, string content, DateTime? dueAtUtc,
        IReadOnlyList<string> assigneeUserIds, string? externalAssigneeNames, string createdByUserId)
    {
        var text = content?.Trim() ?? string.Empty;
        if (text.Length == 0)
            throw new BusinessRuleException("متن مصوبه را وارد کنید.");

        var meeting = await _meetings.GetByIdAsync(meetingId)
            ?? throw new NotFoundException("جلسه", meetingId);

        var userIds = assigneeUserIds.Distinct().ToList();
        var decision = new MeetingDecision
        {
            MeetingId = meetingId,
            Content = text,
            AssigneeUserIds = userIds.Count > 0 ? string.Join("،", userIds) : null,
            AssigneeNames = string.IsNullOrWhiteSpace(externalAssigneeNames) ? null : externalAssigneeNames.Trim(),
            DueAt = dueAtUtc
        };
        await _meetings.AddDecisionAsync(decision);
        await _unitOfWork.CompleteAsync();

        // اطلاع به مسئولین اقدام از طریق کارتابل
        if (userIds.Count > 0)
        {
            await _notifications.NotifyUsersAsync(
                userIds,
                "مسئول اقدام مصوبه",
                $"در جلسهٔ «{meeting.Title}» یک مصوبه ثبت شد و شما مسئول اقدام آن هستید.",
                NotificationType.System,
                $"/my-meetings/{meetingId}");
        }

        return await ToDecisionInfoAsync(decision);
    }

    public async Task RemoveDecisionAsync(int decisionId)
    {
        var decision = await _meetings.GetDecisionAsync(decisionId)
            ?? throw new NotFoundException("مصوبه", decisionId);

        _meetings.RemoveDecision(decision);
        await _unitOfWork.CompleteAsync();
    }

    public async Task ToggleDecisionDoneAsync(int decisionId, bool isDone)
    {
        var decision = await _meetings.GetDecisionAsync(decisionId)
            ?? throw new NotFoundException("مصوبه", decisionId);

        decision.IsDone = isDone;
        await _unitOfWork.CompleteAsync();
    }

    public async Task<List<DecisionInfo>> GetDecisionsForMeetingAsync(int meetingId)
    {
        var decisions = await _meetings.GetDecisionsForMeetingAsync(meetingId);
        var infos = new List<DecisionInfo>(decisions.Count);
        foreach (var decision in decisions)
        {
            infos.Add(await ToDecisionInfoAsync(decision));
        }
        return infos;
    }

    // ===== گزارش‌ها =====

    public async Task<List<MeetingReportItem>> GetReportAsync(DateTime fromUtc, DateTime toUtc)
    {
        var meetings = await _meetings.GetInRangeAsync(fromUtc, toUtc);
        var allUserIds = meetings
            .SelectMany(m => m.Invitees.Select(i => i.UserId))
            .Distinct()
            .ToList();
        var contacts = await _users.GetUsersAsync(allUserIds);
        var byId = contacts.ToDictionary(c => c.UserId);

        var result = new List<MeetingReportItem>(meetings.Count);
        foreach (var m in meetings)
        {
            var rows = m.Invitees
                .Select(i => byId.TryGetValue(i.UserId, out var contact)
                    ? new ReportInviteeRow(contact.DisplayName, contact.PhoneNumber, i.Response, i.RespondedAt, i.Attendance)
                    : new ReportInviteeRow("کاربر حذف‌شده", null, i.Response, i.RespondedAt, i.Attendance))
                .ToList();
            result.Add(new MeetingReportItem(m, rows));
        }
        return result;
    }

    public Task<List<MeetingInvitee>> GetPersonAttendanceAsync(string userId, DateTime fromUtc, DateTime toUtc) =>
        _meetings.GetUserInvitesInRangeAsync(userId, fromUtc, toUtc);

    private async Task<DecisionInfo> ToDecisionInfoAsync(MeetingDecision decision)
    {
        var assignees = new List<string>();

        if (!string.IsNullOrWhiteSpace(decision.AssigneeUserIds))
        {
            var ids = decision.AssigneeUserIds
                .Split('،', ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            var contacts = await _users.GetUsersAsync(ids);
            var byId = contacts.ToDictionary(c => c.UserId);
            foreach (var id in ids)
            {
                assignees.Add(byId.TryGetValue(id, out var contact) ? contact.DisplayName : "کاربر حذف‌شده");
            }
        }

        if (!string.IsNullOrWhiteSpace(decision.AssigneeNames))
        {
            assignees.AddRange(decision.AssigneeNames
                .Split('،', ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return new DecisionInfo(decision.Id, decision.Content, assignees, decision.DueAt, decision.IsDone);
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
