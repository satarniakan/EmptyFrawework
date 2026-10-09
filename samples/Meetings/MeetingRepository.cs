using Microsoft.EntityFrameworkCore;
using Platform.Infrastructure.Data;

namespace Meetings;

/// <summary>
/// ریپازیتوری جلسات. مثل الگوی ماژول نمونه، مستقیماً از DbContext استفاده می‌کند
/// تا ماژول بدون DbSet جداگانه و بدون ویرایش پایه شفاف بماند.
/// </summary>
public interface IMeetingRepository
{
    Task<List<Meeting>> GetAllAsync();
    Task<Meeting?> GetByIdAsync(int id);
    Task<MeetingInvitee?> GetInviteeAsync(int meetingId, string userId);
    Task<List<MeetingInvitee>> GetInvitesForUserAsync(string userId);
    Task<List<Meeting>> GetForUserOrCreatorAsync(string userId);
    Task<MeetingTimeProposal?> GetProposalAsync(int proposalId);
    Task AddAsync(Meeting meeting);
    Task AddProposalAsync(MeetingTimeProposal proposal);

    // ===== مصوبات =====
    Task<List<MeetingDecision>> GetDecisionsForMeetingAsync(int meetingId);
    Task<MeetingDecision?> GetDecisionAsync(int decisionId);
    Task AddDecisionAsync(MeetingDecision decision);
    void RemoveDecision(MeetingDecision decision);

    // ===== گزارش‌ها =====
    Task<List<Meeting>> GetInRangeAsync(DateTime fromUtc, DateTime toUtc);
    Task<List<MeetingInvitee>> GetUserInvitesInRangeAsync(string userId, DateTime fromUtc, DateTime toUtc);

    /// <summary>
    /// جلساتی که در بازه شروع می‌شوند و هنوز یادآوری نشده‌اند — برای job یادآوری.
    /// فقط جلسات آیندهٔ دارای مدعو برمی‌گردند.
    /// </summary>
    Task<List<Meeting>> GetUnremindedStartingBetweenAsync(DateTime fromUtc, DateTime toUtc);
}

public class MeetingRepository : IMeetingRepository
{
    private readonly PlatformDbContext _context;

    public MeetingRepository(PlatformDbContext context) => _context = context;

    public Task<List<Meeting>> GetAllAsync() =>
        _context.Set<Meeting>()
            .Include(m => m.Invitees)
            .OrderByDescending(m => m.StartAt)
            .ToListAsync();

    public Task<Meeting?> GetByIdAsync(int id) =>
        _context.Set<Meeting>()
            .Include(m => m.Invitees)
            .Include(m => m.TimeProposals)
            .FirstOrDefaultAsync(m => m.Id == id);

    public Task<MeetingInvitee?> GetInviteeAsync(int meetingId, string userId) =>
        _context.Set<MeetingInvitee>()
            .Include(i => i.Meeting)
            .FirstOrDefaultAsync(i => i.MeetingId == meetingId && i.UserId == userId);

    public Task<List<MeetingInvitee>> GetInvitesForUserAsync(string userId) =>
        _context.Set<MeetingInvitee>()
            .Include(i => i.Meeting)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.Meeting!.StartAt)
            .ToListAsync();

    /// <summary>جلساتی که کاربر سازنده یا مدعوی آن‌هاست — برای تقویم داشبورد.</summary>
    public Task<List<Meeting>> GetForUserOrCreatorAsync(string userId) =>
        _context.Set<Meeting>()
            .Include(m => m.Invitees)
            .Where(m => m.CreatedByUserId == userId || m.Invitees.Any(i => i.UserId == userId))
            .OrderBy(m => m.StartAt)
            .ToListAsync();

    public Task<MeetingTimeProposal?> GetProposalAsync(int proposalId) =>
        _context.Set<MeetingTimeProposal>()
            .Include(p => p.Meeting)
            .FirstOrDefaultAsync(p => p.Id == proposalId);

    public async Task AddAsync(Meeting meeting) =>
        await _context.Set<Meeting>().AddAsync(meeting);

    public async Task AddProposalAsync(MeetingTimeProposal proposal) =>
        await _context.Set<MeetingTimeProposal>().AddAsync(proposal);

    // ===== مصوبات =====

    public Task<List<MeetingDecision>> GetDecisionsForMeetingAsync(int meetingId) =>
        _context.Set<MeetingDecision>()
            .Where(d => d.MeetingId == meetingId)
            .OrderBy(d => d.Id)
            .ToListAsync();

    public Task<MeetingDecision?> GetDecisionAsync(int decisionId) =>
        _context.Set<MeetingDecision>().FirstOrDefaultAsync(d => d.Id == decisionId);

    public async Task AddDecisionAsync(MeetingDecision decision) =>
        await _context.Set<MeetingDecision>().AddAsync(decision);

    public void RemoveDecision(MeetingDecision decision) =>
        _context.Set<MeetingDecision>().Remove(decision);

    // ===== گزارش‌ها =====

    public Task<List<Meeting>> GetInRangeAsync(DateTime fromUtc, DateTime toUtc) =>
        _context.Set<Meeting>()
            .Include(m => m.Invitees)
            .Where(m => m.StartAt >= fromUtc && m.StartAt < toUtc)
            .OrderBy(m => m.StartAt)
            .ToListAsync();

    public Task<List<MeetingInvitee>> GetUserInvitesInRangeAsync(string userId, DateTime fromUtc, DateTime toUtc) =>
        _context.Set<MeetingInvitee>()
            .Include(i => i.Meeting)
            .Where(i => i.UserId == userId && i.Meeting!.StartAt >= fromUtc && i.Meeting!.StartAt < toUtc)
            .OrderBy(i => i.Meeting!.StartAt)
            .ToListAsync();

    public Task<List<Meeting>> GetUnremindedStartingBetweenAsync(DateTime fromUtc, DateTime toUtc) =>
        _context.Set<Meeting>()
            .Include(m => m.Invitees)
            .Where(m => m.ReminderSentAt == null && m.StartAt >= fromUtc && m.StartAt < toUtc)
            .OrderBy(m => m.StartAt)
            .ToListAsync();
}
