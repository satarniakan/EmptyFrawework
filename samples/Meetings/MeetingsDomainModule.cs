using Microsoft.EntityFrameworkCore;
using Platform.Infrastructure.Data;

namespace Meetings;

/// <summary>
/// ماژول دامنهٔ «جلسات»: تنها جایی که PlatformDbContext از وجود جدول‌های جلسات باخبر می‌شود.
/// </summary>
public class MeetingsDomainModule : IPlatformModule
{
    public string Name => "Meetings";

    public void ConfigureModel(ModelBuilder builder)
    {
        builder.Entity<Meeting>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Title).HasMaxLength(200).IsRequired();
            e.Property(m => m.Description).HasMaxLength(2000);
            e.Property(m => m.Location).HasMaxLength(300);
            e.Property(m => m.MeetingLink).HasMaxLength(1000);
            e.Property(m => m.CreatedByUserId).HasMaxLength(450).IsRequired();
            // MinutesHtml عمداً بدون سقف طول است (nvarchar(max)) — متن صورت‌جلسه از ویرایشگر می‌آید.
            e.Property(m => m.AudioFileName).HasMaxLength(260);
            e.Property(m => m.AudioContentType).HasMaxLength(100);
            e.Property(m => m.AudioUploadedByUserId).HasMaxLength(450);
            e.Property(m => m.PhotoFileName).HasMaxLength(260);
            e.Property(m => m.PhotoContentType).HasMaxLength(100);
            e.Property(m => m.PhotoUploadedByUserId).HasMaxLength(450);
            e.HasIndex(m => m.StartAt);
            e.HasIndex(m => m.CreatedByUserId);
        });

        builder.Entity<MeetingInvitee>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.UserId).HasMaxLength(450).IsRequired();
            // یک کاربر حداکثر یک دعوت‌نامه برای هر جلسه دارد.
            e.HasIndex(i => new { i.MeetingId, i.UserId }).IsUnique();
            e.HasIndex(i => i.UserId);
            e.HasOne(i => i.Meeting)
                .WithMany(m => m.Invitees)
                .HasForeignKey(i => i.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MeetingDecision>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Content).HasMaxLength(1000).IsRequired();
            e.Property(d => d.AssigneeUserIds).HasMaxLength(2000);
            e.Property(d => d.AssigneeNames).HasMaxLength(500);
            e.HasIndex(d => new { d.MeetingId, d.IsDone });
            e.HasOne(d => d.Meeting)
                .WithMany(m => m.Decisions)
                .HasForeignKey(d => d.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MeetingTimeProposal>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.ProposedByUserId).HasMaxLength(450).IsRequired();
            e.Property(p => p.Note).HasMaxLength(1000);
            e.HasIndex(p => new { p.MeetingId, p.Status });
            e.HasOne(p => p.Meeting)
                .WithMany(m => m.TimeProposals)
                .HasForeignKey(p => p.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
