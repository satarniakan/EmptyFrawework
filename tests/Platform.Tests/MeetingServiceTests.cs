using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Meetings;
using Platform.Application.Services;
using Platform.Domain.Enums;
using Platform.Domain.Exceptions;
using Platform.Domain.Interfaces;
using Xunit;

namespace Platform.Tests;

/// <summary>
/// قواعد کسب‌وکار جلسات: دعوت‌نامه همزمان به کارتابل و صف ایمیل/پیامک می‌رود،
/// پذیرش پیشنهاد زمان، پاسخ‌های قبلی را باطل می‌کند و صورت‌جلسه بدون متن ارسال نمی‌شود.
/// </summary>
public class MeetingServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 8, 0, 0, DateTimeKind.Utc);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
    }

    private static (MeetingService service,
        Mock<IMeetingRepository> repo,
        Mock<IOutboxService> outbox,
        Mock<INotificationService> notifications) Build(Dictionary<string, MeetingUser>? directory = null)
    {
        var repo = new Mock<IMeetingRepository>();
        var uow = new Mock<IPlatformUnitOfWork>();
        uow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        // تراکنش در تست: فقط اکشن را اجرا کن (بدون تراکنش واقعی دیتابیس)
        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(action => action());

        var users = new Mock<IMeetingUserDirectory>();
        users.Setup(d => d.GetUsersAsync(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync((IReadOnlyCollection<string> ids) =>
            {
                var found = new List<MeetingUser>();
                foreach (var id in ids)
                {
                    if (directory is not null && directory.TryGetValue(id, out var user))
                        found.Add(user);
                }
                return found;
            });

        var outbox = new Mock<IOutboxService>();
        var notifications = new Mock<INotificationService>();

        var service = new MeetingService(
            repo.Object, uow.Object, users.Object, notifications.Object, outbox.Object,
            new FixedTimeProvider());

        return (service, repo, outbox, notifications);
    }

    private static MeetingUser Contact(string id) =>
        new(id, $"کاربر {id}", $"091200000{id[^1]}", $"{id}@test.ir");

    [Fact]
    public async Task CreateAsync_QueuesEmailAndSms_AndNotifiesInvitees()
    {
        var directory = new Dictionary<string, MeetingUser> { ["u1"] = Contact("u1"), ["u2"] = Contact("u2") };
        var (service, repo, outbox, notifications) = Build(directory);

        var meeting = await service.CreateAsync(new MeetingCreateRequest(
            "جلسهٔ هفتگی", "بررسی وضعیت", MeetingKind.InPerson, "اتاق جلسات", null,
            Now.AddDays(1), "admin", ["u1", "u2"]));

        Assert.Equal(2, meeting.Invitees.Count);
        repo.Verify(r => r.AddAsync(It.IsAny<Meeting>()), Times.Once);

        notifications.Verify(n => n.NotifyUsersAsync(
            It.Is<IEnumerable<string>>(ids => ids.Count() == 2),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<NotificationType>(), It.IsAny<string?>()),
            Times.Once);

        outbox.Verify(o => o.QueueEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        outbox.Verify(o => o.QueueSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAsync_ContactWithoutEmail_GetsOnlySms()
    {
        var directory = new Dictionary<string, MeetingUser>
        {
            ["u1"] = new("u1", "کاربر یک", "09120000001", Email: null)
        };
        var (service, _, outbox, _) = Build(directory);

        await service.CreateAsync(new MeetingCreateRequest(
            "جلسه", null, MeetingKind.Virtual, null, "https://meet.example.com/x",
            Now.AddDays(1), "admin", ["u1"]));

        outbox.Verify(o => o.QueueEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        outbox.Verify(o => o.QueueSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_VirtualWithoutLink_ThrowsBusinessRule()
    {
        var (service, _, _, _) = Build();

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new MeetingCreateRequest(
            "جلسهٔ آنلاین", null, MeetingKind.Virtual, null, null, Now.AddDays(1), "admin", ["u1"])));
    }

    [Fact]
    public async Task CreateAsync_WithoutInvitees_ThrowsBusinessRule()
    {
        var (service, _, _, _) = Build();

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new MeetingCreateRequest(
            "جلسه", null, MeetingKind.InPerson, "اتاق", null, Now.AddDays(1), "admin", [])));
    }

    [Fact]
    public async Task RespondAsync_NotifiesMeetingCreator()
    {
        var meeting = new Meeting
        {
            Id = 5,
            Title = "جلسهٔ آزمون",
            CreatedByUserId = "admin",
            // گارد «زمان جلسه گذشته» در RespondAsync فعال است؛ جلسه باید آینده باشد
            StartAt = Now.AddHours(1)
        };
        var invitee = new MeetingInvitee { MeetingId = 5, UserId = "u1", Meeting = meeting };
        var (service, repo, _, notifications) = Build();

        repo.Setup(r => r.GetInviteeAsync(5, "u1")).ReturnsAsync(invitee);

        var result = await service.RespondAsync(5, "u1", InviteResponse.Accepted);

        Assert.True(result);
        Assert.Equal(InviteResponse.Accepted, invitee.Response);
        Assert.Equal(Now, invitee.RespondedAt);
        notifications.Verify(n => n.NotifyAsync(
            "admin", It.IsAny<string>(), It.Is<string?>(b => b != null && b.Contains("پذیرفت")),
            It.IsAny<NotificationType>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task RespondAsync_NotInvited_ReturnsFalse()
    {
        var (service, repo, _, notifications) = Build();
        repo.Setup(r => r.GetInviteeAsync(5, "u9")).ReturnsAsync((MeetingInvitee?)null);

        var result = await service.RespondAsync(5, "u9", InviteResponse.Accepted);

        Assert.False(result);
        notifications.Verify(n => n.NotifyAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<NotificationType>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task ProposeNewTimeAsync_PastTime_ThrowsBusinessRule()
    {
        var meeting = new Meeting { Id = 5, Title = "جلسه", CreatedByUserId = "admin" };
        var invitee = new MeetingInvitee { MeetingId = 5, UserId = "u1", Meeting = meeting };
        var (service, repo, _, _) = Build();
        repo.Setup(r => r.GetInviteeAsync(5, "u1")).ReturnsAsync(invitee);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ProposeNewTimeAsync(5, "u1", Now.AddHours(-1), null));
    }

    [Fact]
    public async Task DecideProposalAsync_Accept_MovesMeetingTime_AndResetsResponses()
    {
        var meeting = new Meeting
        {
            Id = 5,
            Title = "جلسه",
            CreatedByUserId = "admin",
            StartAt = Now.AddDays(2),
            Invitees =
            [
                new MeetingInvitee { UserId = "u1", Response = InviteResponse.Accepted },
                new MeetingInvitee { UserId = "u2", Response = InviteResponse.Declined }
            ],
            TimeProposals =
            [
                new MeetingTimeProposal { Id = 10, ProposedByUserId = "u1", ProposedStartAt = Now.AddDays(3) },
                new MeetingTimeProposal { Id = 11, ProposedByUserId = "u2", ProposedStartAt = Now.AddDays(4) }
            ]
        };
        meeting.TimeProposals[0].Meeting = meeting;
        meeting.TimeProposals[1].Meeting = meeting;
        var proposal = meeting.TimeProposals[0];

        var (service, repo, _, notifications) = Build();
        repo.Setup(r => r.GetProposalAsync(10)).ReturnsAsync(proposal);

        var result = await service.DecideProposalAsync(10, accept: true);

        Assert.True(result);
        Assert.Equal(ProposalStatus.Accepted, proposal.Status);
        Assert.Equal(proposal.ProposedStartAt, meeting.StartAt);
        Assert.All(meeting.Invitees, i =>
        {
            Assert.Equal(InviteResponse.Pending, i.Response);
            Assert.Null(i.RespondedAt);
            Assert.Null(i.Attendance);
        });
        Assert.Equal(ProposalStatus.Rejected, meeting.TimeProposals[1].Status);

        notifications.Verify(n => n.NotifyUsersAsync(
            It.Is<IEnumerable<string>>(ids => ids.Count() == 2),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<NotificationType>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task DecideProposalAsync_AlreadyDecided_ReturnsFalse()
    {
        var proposal = new MeetingTimeProposal
        {
            Id = 10,
            Status = ProposalStatus.Rejected,
            Meeting = new Meeting { Id = 5 }
        };
        var (service, repo, _, _) = Build();
        repo.Setup(r => r.GetProposalAsync(10)).ReturnsAsync(proposal);

        var result = await service.DecideProposalAsync(10, accept: true);

        Assert.False(result);
    }

    [Fact]
    public async Task SendMinutesAsync_WithoutMinutes_ThrowsBusinessRule()
    {
        var meeting = new Meeting { Id = 5, Title = "جلسه", Invitees = [new MeetingInvitee { UserId = "u1" }] };
        var (service, repo, _, _) = Build();
        repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(meeting);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.SendMinutesAsync(5));
    }

    [Fact]
    public async Task SendMinutesAsync_WithMinutes_NotifiesAllInvitees_AndStampsSentAt()
    {
        var meeting = new Meeting
        {
            Id = 5,
            Title = "جلسهٔ صورت‌جلسه‌دار",
            MinutesHtml = "<p>متن صورت‌جلسه</p>",
            Invitees = [new MeetingInvitee { UserId = "u1" }, new MeetingInvitee { UserId = "u2" }]
        };
        var (service, repo, _, notifications) = Build();
        repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(meeting);

        var count = await service.SendMinutesAsync(5);

        Assert.Equal(2, count);
        Assert.Equal(Now, meeting.MinutesSentAt);
        notifications.Verify(n => n.NotifyUsersAsync(
            It.Is<IEnumerable<string>>(ids => ids.Count() == 2),
            It.Is<string>(t => t.Contains("صورت‌جلسه")),
            It.IsAny<string?>(), It.IsAny<NotificationType>(),
            It.Is<string?>(link => link!.EndsWith("/5"))),
            Times.Once);
    }

    [Fact]
    public async Task CanViewAsync_CreatorOrInvitee_IsTrue_OthersFalse()
    {
        var meeting = new Meeting
        {
            Id = 5,
            CreatedByUserId = "admin",
            Invitees = [new MeetingInvitee { UserId = "u1" }]
        };
        var (service, repo, _, _) = Build();
        repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(meeting);

        Assert.True(await service.CanViewAsync(5, "admin"));
        Assert.True(await service.CanViewAsync(5, "u1"));
        Assert.False(await service.CanViewAsync(5, "u9"));
    }
}

/// <summary>پاک‌سازی HTML صورت‌جلسه: عناصر خطرناک حذف می‌شوند و قالب‌بندی مجاز می‌ماند.</summary>
public class MinutesSanitizerTests
{
    [Fact]
    public void Clean_RemovesScriptBlocks_AndInlineHandlers()
    {
        var dirty = "<p onclick=\"alert('x')\">متن جلسه</p><script>alert('boom')</script><b>پایان</b>";

        var clean = Meetings.MinutesSanitizer.Clean(dirty);

        Assert.DoesNotContain("script", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", clean, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("متن جلسه", clean);
        Assert.Contains("<b>", clean);
    }

    [Fact]
    public void Clean_RemovesJavaScriptUrls()
    {
        var dirty = "<a href=\"javascript:alert('x')\">لینک</a>";

        var clean = Meetings.MinutesSanitizer.Clean(dirty);

        Assert.DoesNotContain("javascript:", clean, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    // حمله‌هایی که sanitizer مبتنی بر regex را دور می‌زدند
    [InlineData("<ScRiPt>alert(1)</ScRiPt>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<svg onload=alert(1)>")]
    [InlineData("<a href=\"JaVaScRiPt:alert(1)\">x</a>")]
    [InlineData("<a href=\"data:text/html,<script>alert(1)</script>\">x</a>")]
    [InlineData("<p style=\"x:expression(alert(1))\">متن</p>")]
    [InlineData("<iframe src=\"https://evil.example\"></iframe>")]
    public void Clean_NeutralizesXssBypasses(string dirty)
    {
        var clean = Meetings.MinutesSanitizer.Clean(dirty);

        Assert.DoesNotContain("alert(", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onload", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", clean, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Clean_KeepsEditorFormatting()
    {
        var html = "<h2>سرخط</h2><p>متن <strong>مهم</strong></p><ul><li>یک</li></ul>" +
                   "<table><tr><td>سلول</td></tr></table>" +
                   "<a href=\"https://example.com\">پیوند</a>";

        var clean = Meetings.MinutesSanitizer.Clean(html);

        Assert.Contains("<h2>", clean);
        Assert.Contains("<strong>", clean);
        Assert.Contains("<table>", clean);
        Assert.Contains("https://example.com", clean);
    }

    [Fact]
    public void Clean_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, Meetings.MinutesSanitizer.Clean(null));
        Assert.Equal(string.Empty, Meetings.MinutesSanitizer.Clean("   "));
    }
}
