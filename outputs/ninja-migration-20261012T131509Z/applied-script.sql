BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    IF SCHEMA_ID(N'ninja') IS NULL EXEC(N'CREATE SCHEMA [ninja];');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE TABLE [ninja].[NinjaAccountDetails] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [PhoneNumber] nvarchar(32) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_NinjaAccountDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NinjaAccountDetails_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE TABLE [ninja].[NinjaDailyImports] (
        [Id] uniqueidentifier NOT NULL,
        [ReportDate] date NOT NULL,
        [IsDeductions] bit NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [ContentHash] nvarchar(64) NOT NULL,
        [ImportedRows] int NOT NULL,
        [SkippedRows] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_NinjaDailyImports] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE TABLE [ninja].[NinjaDailyDeductions] (
        [Id] uniqueidentifier NOT NULL,
        [TotalCollectedAmountAtDelivery] decimal(18,2) NOT NULL,
        [TotalDeductions] decimal(18,2) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [RiderClientAssignmentId] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [ImportId] uniqueidentifier NOT NULL,
        [ReportDate] date NOT NULL,
        [CaptainId] nvarchar(150) NOT NULL,
        [SourceRowNumber] int NOT NULL,
        CONSTRAINT [PK_NinjaDailyDeductions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_NinjaDeductions_CollectedAmount] CHECK ([TotalCollectedAmountAtDelivery] >= 0),
        CONSTRAINT [FK_NinjaDailyDeductions_NinjaDailyImports_ImportId] FOREIGN KEY ([ImportId]) REFERENCES [ninja].[NinjaDailyImports] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NinjaDailyDeductions_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NinjaDailyDeductions_RiderClientAssignments_RiderClientAssignmentId] FOREIGN KEY ([RiderClientAssignmentId]) REFERENCES [app].[RiderClientAssignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NinjaDailyDeductions_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE TABLE [ninja].[NinjaDailyPerformance] (
        [Id] uniqueidentifier NOT NULL,
        [TotalOrders] int NOT NULL,
        [TotalWorkingHours] decimal(18,8) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [RiderClientAssignmentId] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [ImportId] uniqueidentifier NOT NULL,
        [ReportDate] date NOT NULL,
        [CaptainId] nvarchar(150) NOT NULL,
        [SourceRowNumber] int NOT NULL,
        CONSTRAINT [PK_NinjaDailyPerformance] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_NinjaPerformance_Values] CHECK ([TotalOrders] >= 0 AND [TotalWorkingHours] BETWEEN 0 AND 24),
        CONSTRAINT [FK_NinjaDailyPerformance_NinjaDailyImports_ImportId] FOREIGN KEY ([ImportId]) REFERENCES [ninja].[NinjaDailyImports] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NinjaDailyPerformance_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NinjaDailyPerformance_RiderClientAssignments_RiderClientAssignmentId] FOREIGN KEY ([RiderClientAssignmentId]) REFERENCES [app].[RiderClientAssignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NinjaDailyPerformance_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE INDEX [IX_NinjaAccountDetails_IsDeleted] ON [ninja].[NinjaAccountDetails] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NinjaAccountDetails_PlatformRiderAccountId] ON [ninja].[NinjaAccountDetails] ([PlatformRiderAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE INDEX [IX_NinjaDailyDeductions_ImportId] ON [ninja].[NinjaDailyDeductions] ([ImportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NinjaDailyDeductions_PlatformRiderAccountId_ReportDate] ON [ninja].[NinjaDailyDeductions] ([PlatformRiderAccountId], [ReportDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE INDEX [IX_NinjaDailyDeductions_RiderClientAssignmentId] ON [ninja].[NinjaDailyDeductions] ([RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE INDEX [IX_NinjaDailyDeductions_RiderProfileId_ReportDate] ON [ninja].[NinjaDailyDeductions] ([RiderProfileId], [ReportDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NinjaDailyImports_ReportDate_IsDeductions_ContentHash] ON [ninja].[NinjaDailyImports] ([ReportDate], [IsDeductions], [ContentHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE INDEX [IX_NinjaDailyPerformance_ImportId] ON [ninja].[NinjaDailyPerformance] ([ImportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NinjaDailyPerformance_PlatformRiderAccountId_ReportDate] ON [ninja].[NinjaDailyPerformance] ([PlatformRiderAccountId], [ReportDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE INDEX [IX_NinjaDailyPerformance_RiderClientAssignmentId] ON [ninja].[NinjaDailyPerformance] ([RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    CREATE INDEX [IX_NinjaDailyPerformance_RiderProfileId_ReportDate] ON [ninja].[NinjaDailyPerformance] ([RiderProfileId], [ReportDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions', N'10.0.11');
END;

COMMIT;
GO

