-- ============================================================
-- دادهٔ تستی سامانهٔ جلسات — همهٔ جداول (به‌جز OtpCode که گذراست)
-- قابل اجرای مکرر: هر بخش با NOT EXISTS از درج تکراری جلوگیری می‌کند.
-- اجرا: sqlcmd -S "(localdb)\mssqllocaldb" -d EmptyFrameworkDev -f 65001 -i seed-test-data.sql
-- ============================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @adminId nvarchar(450) = (SELECT Id FROM AspNetUsers WHERE PhoneNumber = N'09125993396');
IF @adminId IS NULL
BEGIN
    PRINT N'ادمین (09125993396) یافت نشد؛ اول برنامه را اجرا کنید تا ساخته شود.';
    RETURN;
END

DECLARE @userRoleId nvarchar(450) = (SELECT Id FROM AspNetRoles WHERE Name = N'User');

-- ===================== ۱) کاربران تستی =====================
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
                         PhoneNumber, PhoneNumberConfirmed, SecurityStamp, ConcurrencyStamp,
                         TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FullName)
SELECT LOWER(REPLACE(NEWID(), '-', '')), v.Phone, UPPER(v.Phone), v.Email, UPPER(v.Email), 1,
       v.Phone, 1, CONVERT(nvarchar(64), NEWID()), CONVERT(nvarchar(64), NEWID()),
       0, 1, 0, v.FullName
FROM (VALUES
    (N'09121112233', N'علی رضایی',   N'ali@example.com'),
    (N'09122223344', N'مریم احمدی',  N'maryam@example.com'),
    (N'09123334455', N'حسین کریمی',  N'hossein@example.com'),
    (N'09124445566', N'زهرا موسوی',  N'zahra@example.com'),
    (N'09125556677', N'رضا قاسمی',   N'reza@example.com')
) AS v(Phone, FullName, Email)
WHERE NOT EXISTS (SELECT 1 FROM AspNetUsers u WHERE u.UserName = v.Phone);

-- نقش «کاربر» برای همهٔ کاربران تستی
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, @userRoleId
FROM AspNetUsers u
WHERE u.UserName IN (N'09121112233', N'09122223344', N'09123334455', N'09124445566', N'09125556677')
  AND @userRoleId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles ur WHERE ur.UserId = u.Id AND ur.RoleId = @userRoleId);

-- ===================== ۲) جلسات =====================
-- DayOffset نسبت به امروز (UTC)، HourUtc ساعت UTC؛ تهران = UTC + 3:30
INSERT INTO Meeting (Title, Description, Kind, Location, MeetingLink, StartAt, CreatedByUserId, CreatedAt, MinutesHtml, MinutesSentAt)
SELECT v.Title, v.Descr, v.Kind, v.Location, v.Link,
       DATEADD(HOUR, v.HourUtc, DATEADD(DAY, v.DayOffset, b.TodayUtc)),
       @adminId, DATEADD(DAY, -1, GETUTCDATE()),
       v.Minutes,
       CASE WHEN v.Minutes IS NOT NULL THEN DATEADD(HOUR, v.HourUtc, DATEADD(DAY, v.DayOffset + 1, b.TodayUtc)) END
FROM (VALUES
    (N'هم‌راستایی فصل پاییز',        N'تصویب اهداف سه‌ماهه',        1, N'اتاق جلسات طبقه دوم', NULL,                                       -7,  6, N'<h3>صورت‌جلسه هم‌راستایی فصل پاییز</h3><ul><li>اهداف فصل تصویب شد.</li><li>مسئول پیگیری: تیم محصول — مهلت دو هفته.</li><li>جلسه بعدی: پس از تحویل گزارش پیشرفت.</li></ul>'),
    (N'بررسی گزارش فروش مهر',        N'مقایسه با تارگت ماهانه',     1, N'سالن کنفرانس',        NULL,                                       -3,  5, N'<h3>صورت‌جلسه بررسی گزارش فروش</h3><ul><li>انحراف ۸٪ از تارگت اعلام شد.</li><li>طرح جبران در کمیته فروش بررسی می‌شود.</li></ul>'),
    (N'کمیته ریسک سازمان',           N'بازنگری ریسک‌های کلیدی',     1, N'اتاق هیئت‌مدیره',     NULL,                                        2,  7, NULL),
    (N'هم‌سویی معماری سامانه',       N'انتخاب معماری سرویس اعلان',  2, NULL,                   N'https://meet.example.com/arch-sync',       4,  5, NULL),
    (N'مرور بودجه سالانه',           N'بودجه واحدها برای سال آینده', 1, N'سالن کنفرانس اصلی',  NULL,                                        9,  6, NULL),
    (N'جلسه ماهانه منابع انسانی',    N'برنامه جذب و آموزش',         2, NULL,                   N'https://meet.example.com/hr-monthly',     14,  8, NULL)
) AS v(Title, Descr, Kind, Location, Link, DayOffset, HourUtc, Minutes)
CROSS APPLY (SELECT DATEADD(DAY, DATEDIFF(DAY, 0, GETUTCDATE()), 0) AS TodayUtc) b
WHERE NOT EXISTS (SELECT 1 FROM Meeting m WHERE m.Title = v.Title);

-- ===================== ۳) مدعوین و حضور و غیاب =====================
-- Response: 0=در انتظار، 1=پذیرفته، 2=رد شده | Attendance: NULL/1=حاضر/2=غایب
DECLARE @links TABLE (MeetingTitle nvarchar(200), Phone nvarchar(450), Response int, Attendance int);
INSERT INTO @links VALUES
    (N'هم‌راستایی فصل پاییز',        N'09121112233', 1, 1),
    (N'هم‌راستایی فصل پاییز',        N'09122223344', 1, 1),
    (N'هم‌راستایی فصل پاییز',        N'09123334455', 2, 2),
    (N'هم‌راستایی فصل پاییز',        N'09124445566', 1, 1),
    (N'بررسی گزارش فروش مهر',        N'09121112233', 1, 1),
    (N'بررسی گزارش فروش مهر',        N'09124445566', 2, 2),
    (N'بررسی گزارش فروش مهر',        N'09125556677', 1, 1),
    (N'جلسه همراستایی تیم محصول',    N'09121112233', 1, NULL),
    (N'جلسه همراستایی تیم محصول',    N'09122223344', 0, NULL),
    (N'جلسه همراستایی تیم محصول',    N'09123334455', 1, NULL),
    (N'جلسه فنی معماری سامانه',      N'09122223344', 0, NULL),
    (N'جلسه فنی معماری سامانه',      N'09125556677', 1, NULL),
    (N'بازنگری فرآیندهای سازمان',    N'09124445566', 0, NULL),
    (N'بازنگری فرآیندهای سازمان',    N'09123334455', 1, NULL),
    (N'کمیته ریسک سازمان',           N'09121112233', 1, NULL),
    (N'کمیته ریسک سازمان',           N'09122223344', 0, NULL),
    (N'کمیته ریسک سازمان',           N'09125556677', 0, NULL),
    (N'هم‌سویی معماری سامانه',       N'09123334455', 1, NULL),
    (N'هم‌سویی معماری سامانه',       N'09124445566', 1, NULL),
    (N'هم‌سویی معماری سامانه',       N'09121112233', 2, NULL),
    (N'مرور بودجه سالانه',           N'09121112233', 0, NULL),
    (N'مرور بودجه سالانه',           N'09122223344', 0, NULL),
    (N'مرور بودجه سالانه',           N'09123334455', 0, NULL),
    (N'مرور بودجه سالانه',           N'09124445566', 0, NULL),
    (N'جلسه ماهانه منابع انسانی',    N'09122223344', 1, NULL),
    (N'جلسه ماهانه منابع انسانی',    N'09125556677', 0, NULL);

INSERT INTO MeetingInvitee (MeetingId, UserId, Response, RespondedAt, Attendance)
SELECT m.Id, u.Id, l.Response,
       CASE WHEN l.Response > 0 THEN DATEADD(HOUR, -3, GETUTCDATE()) END,
       l.Attendance
FROM @links l
JOIN Meeting m ON m.Title = l.MeetingTitle
JOIN AspNetUsers u ON u.UserName = l.Phone
WHERE NOT EXISTS (SELECT 1 FROM MeetingInvitee x WHERE x.MeetingId = m.Id AND x.UserId = u.Id);

-- ===================== ۴) پیشنهادهای زمان جدید =====================
DECLARE @proposals TABLE (MeetingTitle nvarchar(200), Phone nvarchar(450), DayOffset int, HourUtc int, Note nvarchar(1000), Status int);
INSERT INTO @proposals VALUES
    (N'مرور بودجه سالانه',        N'09121112233', 10, 6, N'در آن بازه سفر دارم؛ یک روز بعد پیشنهاد می‌کنم.',      0),
    (N'کمیته ریسک سازمان',        N'09122223344',  2, 9, N'ساعت ۱۲:۳۰ تهران برای من مناسب‌تر است.',               0),
    (N'بررسی گزارش فروش مهر',     N'09123334455', -2, 5, N'ساعت زودتر، پیش از شروع فروش روزانه.',                 2);

INSERT INTO MeetingTimeProposal (MeetingId, ProposedByUserId, ProposedStartAt, Note, Status, CreatedAt, DecidedAt)
SELECT m.Id, u.Id,
       DATEADD(HOUR, p.HourUtc, DATEADD(DAY, p.DayOffset, DATEADD(DAY, DATEDIFF(DAY, 0, GETUTCDATE()), 0))),
       p.Note, p.Status, DATEADD(DAY, -1, GETUTCDATE()),
       CASE WHEN p.Status = 2 THEN DATEADD(HOUR, -12, GETUTCDATE()) END
FROM @proposals p
JOIN Meeting m ON m.Title = p.MeetingTitle
JOIN AspNetUsers u ON u.UserName = p.Phone
WHERE NOT EXISTS (SELECT 1 FROM MeetingTimeProposal x WHERE x.MeetingId = m.Id AND x.ProposedByUserId = u.Id);

-- ===================== ۵) اعلان‌های کارتابل =====================
-- دعوت به جلسات آینده (خوانده‌نشده)
INSERT INTO Notifications (UserId, Title, Body, Type, LinkUrl, IsRead, ReadAt, CreatedAt)
SELECT i.UserId, N'دعوت به جلسه', N'به جلسهٔ «' + m.Title + N'» دعوت شده‌اید. لطفاً پاسخ خود را ثبت کنید.',
       3, N'/my-meetings', 0, NULL, DATEADD(HOUR, -5, GETUTCDATE())
FROM MeetingInvitee i
JOIN Meeting m ON m.Id = i.MeetingId
WHERE m.StartAt > GETUTCDATE() AND i.Response IN (0, 1)
  AND NOT EXISTS (SELECT 1 FROM Notifications n WHERE n.UserId = i.UserId AND n.Title = N'دعوت به جلسه' AND n.Body LIKE N'%' + m.Title + N'%');

-- صورت‌جلسهٔ جلسات گذشته (بعضی خوانده‌شده)
INSERT INTO Notifications (UserId, Title, Body, Type, LinkUrl, IsRead, ReadAt, CreatedAt)
SELECT i.UserId, N'صورت‌جلسه: ' + m.Title, N'صورت‌جلسهٔ جلسه برای شما ارسال شد.',
       3, N'/my-meetings/' + CONVERT(nvarchar(20), m.Id),
       CASE WHEN i.Response = 1 THEN 1 ELSE 0 END,
       CASE WHEN i.Response = 1 THEN DATEADD(HOUR, -2, GETUTCDATE()) END,
       DATEADD(DAY, -1, GETUTCDATE())
FROM MeetingInvitee i
JOIN Meeting m ON m.Id = i.MeetingId
WHERE m.StartAt <= GETUTCDATE() AND m.MinutesSentAt IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Notifications n WHERE n.UserId = i.UserId AND n.Title = N'صورت‌جلسه: ' + m.Title);

-- اعلان ادمین: پیشنهاد زمان در انتظار تصمیم
INSERT INTO Notifications (UserId, Title, Body, Type, LinkUrl, IsRead, ReadAt, CreatedAt)
SELECT @adminId, N'پیشنهاد زمان جدید',
       N'«' + u.FullName + N'» برای جلسهٔ «' + m.Title + N'» زمان جدیدی پیشنهاد داد.',
       3, N'/meetings/' + CONVERT(nvarchar(20), m.Id) + N'/manage', 0, NULL, DATEADD(HOUR, -4, GETUTCDATE())
FROM MeetingTimeProposal p
JOIN Meeting m ON m.Id = p.MeetingId
JOIN AspNetUsers u ON u.Id = p.ProposedByUserId
WHERE p.Status = 0
  AND NOT EXISTS (SELECT 1 FROM Notifications n WHERE n.UserId = @adminId AND n.Title = N'پیشنهاد زمان جدید' AND n.Body LIKE N'%' + m.Title + N'%');

-- ===================== ۶) صف پیام (Outbox) =====================
-- Channel: 1=پیامک، 2=ایمیل | Status: 1=در انتظار، 2=ارسال‌شده، 3=ناموفق، 4=در حال ارسال
INSERT INTO OutboxMessages (Channel, Recipient, Subject, Body, Status, Attempts, LastError, CreatedAt, SentAt, ProcessingStartedAt)
SELECT v.Channel, v.Recipient, v.Subject, v.Body, v.Status, v.Attempts, v.LastError, v.CreatedAt, v.SentAt, NULL
FROM (VALUES
    (2, N'ali@example.com',     N'دعوت به جلسه: کمیته ریسک سازمان', N'شما به جلسهٔ «کمیته ریسک سازمان» دعوت شده‌اید. برای پاسخ به سامانه مراجعه کنید.',        1, 0, NULL,                                                        DATEADD(HOUR, -5, GETUTCDATE()), NULL),
    (1, N'09121112233',         NULL,                               N'دعوت به جلسه «مرور بودجه سالانه» — پاسخ در بخش «جلسات من» سامانه.',                    1, 0, NULL,                                                        DATEADD(HOUR, -3, GETUTCDATE()), NULL),
    (2, N'maryam@example.com',  N'دعوت به جلسه: هم‌سویی معماری',    N'شما به جلسهٔ «هم‌سویی معماری سامانه» دعوت شده‌اید.',                                    3, 3, N'پیکربندی ایمیل ناقص است (Email:Smtp:Host).',               DATEADD(HOUR, -30, GETUTCDATE()), NULL),
    (1, N'09122223344',         NULL,                               N'کد ورود شما: 482913',                                                                  2, 1, NULL,                                                        DATEADD(HOUR, -26, GETUTCDATE()), DATEADD(HOUR, -26, GETUTCDATE())),
    (2, N'hossein@example.com', N'صورت‌جلسه: بررسی گزارش فروش مهر', N'صورت‌جلسهٔ جلسه برای شما ارسال شد.',                                                   1, 0, NULL,                                                        DATEADD(DAY, -1, GETUTCDATE()), NULL)
) AS v(Channel, Recipient, Subject, Body, Status, Attempts, LastError, CreatedAt, SentAt)
WHERE NOT EXISTS (SELECT 1 FROM OutboxMessages o WHERE o.Recipient = v.Recipient AND o.Body = v.Body);

-- ===================== ۷) لاگ رویدادها =====================
INSERT INTO AuditLogs (EventType, UserEmail, Details, OccurredAt)
SELECT v.EventType, v.UserEmail, v.Details, v.OccurredAt
FROM (VALUES
    (N'Login',          N'09125993396',     N'ورود موفق با OTP از IP 127.0.0.1',                        DATEADD(HOUR, -26, GETUTCDATE())),
    (N'MeetingCreated', N'09125993396',     N'جلسهٔ «کمیته ریسک سازمان» با ۳ مدعو ایجاد شد.',           DATEADD(DAY, -1, GETUTCDATE())),
    (N'UserCreated',    N'09125993396',     N'کاربر «علی رضایی» توسط ادمین ایجاد شد.',                  DATEADD(DAY, -2, GETUTCDATE())),
    (N'MinutesSent',    N'09125993396',     N'صورت‌جلسهٔ «بررسی گزارش فروش مهر» برای ۳ نفر ارسال شد.',  DATEADD(HOUR, -20, GETUTCDATE())),
    (N'LoginFailed',    N'09121112233',     N'تلاش ناموفق ورود — رمز عبور نادرست.',                     DATEADD(HOUR, -10, GETUTCDATE()))
) AS v(EventType, UserEmail, Details, OccurredAt)
WHERE NOT EXISTS (SELECT 1 FROM AuditLogs a WHERE a.EventType = v.EventType AND a.Details = v.Details);

PRINT N'دادهٔ تستی با موفقیت درج شد.';

-- ===================== ۹) دعوت خود ادمین به چند جلسه =====================
-- تا داشبورد ادمین هم کارت دعوت (پذیرش/رد/پیشنهاد زمان) داشته باشد
INSERT INTO MeetingInvitee (MeetingId, UserId, Response, RespondedAt, Attendance)
SELECT m.Id, @adminId, l.Response,
       CASE WHEN l.Response > 0 THEN DATEADD(HOUR, -3, GETUTCDATE()) END,
       l.Attendance
FROM (VALUES
    (N'کمیته ریسک سازمان',        0, NULL),
    (N'مرور بودجه سالانه',        0, NULL),
    (N'جلسه ماهانه منابع انسانی', 0, NULL),
    (N'هم‌راستایی فصل پاییز',     1, 1)
) AS l(MeetingTitle, Response, Attendance)
JOIN Meeting m ON m.Title = l.MeetingTitle
WHERE NOT EXISTS (SELECT 1 FROM MeetingInvitee x WHERE x.MeetingId = m.Id AND x.UserId = @adminId);
