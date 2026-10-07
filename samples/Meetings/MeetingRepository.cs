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
}
