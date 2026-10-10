-- ============================================================
-- اسکیمای دیتابیس فریم‌ورک (تولیدشده از مایگریشن‌ها — دستی ویرایش نکنید)
-- بازسازی با: dotnet ef migrations script --project Platform.App --startup-project Platform.App -o docs/database.sql
-- ------------------------------------------------------------
-- ریستور روی دیتابیس تازه (SQL Server):
--   sqlcmd -S <server> -U sa -P '<pass>' -Q "CREATE DATABASE [EmptyFramework]"
--   sqlcmd -S <server> -U sa -P '<pass>' -d EmptyFramework -i docs/database.sql
-- روی داکر (سرویس sqlserver همین ریپو):
--   docker compose up -d sqlserver
--   docker compose cp docs/database.sql sqlserver:/tmp/database.sql
--   docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Dev_Pass123!' -C -Q "CREATE DATABASE [EmptyFramework]"
--   docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Dev_Pass123!' -C -d EmptyFramework -i /tmp/database.sql
-- نکته: نقش‌ها (Admin/User) را خودِ اپ هنگام استارتاپ می‌سازد (RoleSeeder) و
-- ادمین اول با Identity:FirstAdminPhoneNumber در اولین ورود OTP ساخته می‌شود؛
-- پس سید اضافه لازم نیست. خودِ اپ هم با MigrateAsync همین اسکیما را می‌سازد؛
-- این فایل برای وقتی است که بخواهید دیتابیس را بیرون از اپ بسازید/ریستور کنید.
-- ============================================================

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [ApiTokens] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [TokenHash] nvarchar(64) NOT NULL,
    [DeviceName] nvarchar(100) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [LastUsedAtUtc] datetime2 NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [RevokedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_ApiTokens] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [FullName] nvarchar(100) NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [AuditLogs] (
    [Id] int NOT NULL IDENTITY,
    [EventType] nvarchar(100) NOT NULL,
    [UserEmail] nvarchar(256) NULL,
    [Details] nvarchar(max) NOT NULL,
    [OccurredAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);

CREATE TABLE [LoginHistories] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NULL,
    [UserName] nvarchar(256) NULL,
    [Succeeded] bit NOT NULL,
    [Method] nvarchar(32) NOT NULL,
    [IpAddress] nvarchar(64) NULL,
    [UserAgent] nvarchar(512) NULL,
    [FailureReason] nvarchar(256) NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_LoginHistories] PRIMARY KEY ([Id])
);

CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Body] nvarchar(max) NULL,
    [Type] int NOT NULL,
    [LinkUrl] nvarchar(max) NULL,
    [IsRead] bit NOT NULL,
    [ReadAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
);

CREATE TABLE [NumberSequences] (
    [Name] nvarchar(128) NOT NULL,
    [LastValue] bigint NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_NumberSequences] PRIMARY KEY ([Name])
);

CREATE TABLE [OtpCodes] (
    [Id] int NOT NULL IDENTITY,
    [PhoneNumber] nvarchar(450) NOT NULL,
    [Code] nvarchar(max) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsUsed] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OtpCodes] PRIMARY KEY ([Id])
);

CREATE TABLE [OtpThrottles] (
    [PhoneNumber] nvarchar(32) NOT NULL,
    [FailedCount] int NOT NULL,
    [FailedWindowStartUtc] datetime2 NULL,
    [GenerationCount] int NOT NULL,
    [GenerationWindowStartUtc] datetime2 NULL,
    CONSTRAINT [PK_OtpThrottles] PRIMARY KEY ([PhoneNumber])
);

CREATE TABLE [OutboxMessages] (
    [Id] int NOT NULL IDENTITY,
    [Channel] int NOT NULL,
    [Recipient] nvarchar(max) NOT NULL,
    [Subject] nvarchar(max) NULL,
    [Body] nvarchar(max) NOT NULL,
    [LinkUrl] nvarchar(1000) NULL,
    [Status] int NOT NULL,
    [Attempts] int NOT NULL,
    [LastError] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [SentAt] datetime2 NULL,
    [ProcessingStartedAt] datetime2 NULL,
    CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
);

CREATE TABLE [Payments] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [AmountTomans] bigint NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Gateway] nvarchar(32) NOT NULL,
    [Authority] nvarchar(64) NOT NULL,
    [Status] int NOT NULL,
    [RefId] bigint NULL,
    [CardPanMasked] nvarchar(32) NULL,
    [FailureReason] nvarchar(500) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [PaidAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id])
);

CREATE TABLE [PushSubscriptions] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [Endpoint] nvarchar(500) NOT NULL,
    [P256dh] nvarchar(200) NOT NULL,
    [Auth] nvarchar(100) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_PushSubscriptions] PRIMARY KEY ([Id])
);

CREATE TABLE [Settings] (
    [Key] nvarchar(128) NOT NULL,
    [Value] nvarchar(max) NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_Settings] PRIMARY KEY ([Key])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_ApiTokens_TokenHash] ON [ApiTokens] ([TokenHash]);

CREATE INDEX [IX_ApiTokens_UserId_IsRevoked] ON [ApiTokens] ([UserId], [IsRevoked]);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_AuditLogs_OccurredAt] ON [AuditLogs] ([OccurredAt]);

CREATE INDEX [IX_LoginHistories_UserId_OccurredAtUtc] ON [LoginHistories] ([UserId], [OccurredAtUtc]);

CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);

CREATE INDEX [IX_OtpCodes_ExpiresAt] ON [OtpCodes] ([ExpiresAt]);

CREATE INDEX [IX_OtpCodes_PhoneNumber] ON [OtpCodes] ([PhoneNumber]);

CREATE INDEX [IX_OutboxMessages_Status] ON [OutboxMessages] ([Status]);

CREATE INDEX [IX_OutboxMessages_Status_Attempts] ON [OutboxMessages] ([Status], [Attempts]);

CREATE UNIQUE INDEX [IX_Payments_Authority] ON [Payments] ([Authority]);

CREATE INDEX [IX_Payments_UserId_Status] ON [Payments] ([UserId], [Status]);

CREATE UNIQUE INDEX [IX_PushSubscriptions_Endpoint] ON [PushSubscriptions] ([Endpoint]);

CREATE INDEX [IX_PushSubscriptions_UserId] ON [PushSubscriptions] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261010075248_InitialCreate', N'10.0.12');

COMMIT;
GO

