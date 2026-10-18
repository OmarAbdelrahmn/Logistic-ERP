BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011191524_AddChefzWorkbookReportsAndStatistics'
)
BEGIN
    ALTER TABLE [chefz].[AccountDetails] ADD [DashboardStatus] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011191524_AddChefzWorkbookReportsAndStatistics'
)
BEGIN
    ALTER TABLE [chefz].[AccountDetails] ADD [VehicleType] nvarchar(20) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011191524_AddChefzWorkbookReportsAndStatistics'
)
BEGIN
    CREATE TABLE [chefz].[PerformanceSettings] (
        [Id] uniqueidentifier NOT NULL,
        [OverallAOrders] int NOT NULL,
        [OverallBOrders] int NOT NULL,
        [OverallCOrders] int NOT NULL,
        [DailyAOrders] int NOT NULL,
        [DailyBOrders] int NOT NULL,
        [DailyCOrders] int NOT NULL,
        [AAcceptanceRate] decimal(5,2) NOT NULL,
        [BAcceptanceRate] decimal(5,2) NOT NULL,
        [CAcceptanceRate] decimal(5,2) NOT NULL,
        [ChangeAfterNoOrderDays] int NOT NULL,
        [FollowUpOnZeroOrdersWithMissedOffers] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_PerformanceSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChefzSettings_Thresholds] CHECK ([OverallAOrders] > [OverallBOrders] AND [OverallBOrders] > [OverallCOrders] AND [OverallCOrders] > 0 AND [DailyAOrders] > [DailyBOrders] AND [DailyBOrders] > [DailyCOrders] AND [DailyCOrders] > 0 AND [AAcceptanceRate] BETWEEN 0 AND 100 AND [BAcceptanceRate] BETWEEN 0 AND [AAcceptanceRate] AND [CAcceptanceRate] BETWEEN 0 AND [BAcceptanceRate] AND [ChangeAfterNoOrderDays] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011191524_AddChefzWorkbookReportsAndStatistics'
)
BEGIN
    CREATE TABLE [chefz].[SponsorStatistics] (
        [Id] uniqueidentifier NOT NULL,
        [SponsorId] uniqueidentifier NOT NULL,
        [SponsorshipCount] int NOT NULL,
        [Working] int NOT NULL,
        [NotWorking] int NOT NULL,
        [Blocked] int NOT NULL,
        [PermanentlyBlocked] int NOT NULL,
        [UpdateBlocked] int NOT NULL,
        [Uploaded] int NOT NULL,
        [PrivateLicense] int NOT NULL,
        [PendingUpload] int NOT NULL,
        [NoLicense] int NOT NULL,
        [ManagementSuspended] int NOT NULL,
        [Motorcycle] int NOT NULL,
        [WorkingOutsideSponsorship] int NOT NULL,
        [WorkingWithoutPerformance] int NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_SponsorStatistics] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChefzStatistics_Nonnegative] CHECK ([SponsorshipCount] >= 0 AND [Working] >= 0 AND [NotWorking] >= 0 AND [Blocked] >= 0 AND [PermanentlyBlocked] >= 0 AND [UpdateBlocked] >= 0 AND [Uploaded] >= 0 AND [PrivateLicense] >= 0 AND [PendingUpload] >= 0 AND [NoLicense] >= 0 AND [ManagementSuspended] >= 0 AND [Motorcycle] >= 0 AND [WorkingOutsideSponsorship] >= 0 AND [WorkingWithoutPerformance] >= 0),
        CONSTRAINT [FK_SponsorStatistics_Sponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011191524_AddChefzWorkbookReportsAndStatistics'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AAcceptanceRate', N'BAcceptanceRate', N'CAcceptanceRate', N'ChangeAfterNoOrderDays', N'CreatedAtUtc', N'CreatedByUserId', N'DailyAOrders', N'DailyBOrders', N'DailyCOrders', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'FollowUpOnZeroOrdersWithMissedOffers', N'IsDeleted', N'OverallAOrders', N'OverallBOrders', N'OverallCOrders', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[chefz].[PerformanceSettings]'))
        SET IDENTITY_INSERT [chefz].[PerformanceSettings] ON;
    EXEC(N'INSERT INTO [chefz].[PerformanceSettings] ([Id], [AAcceptanceRate], [BAcceptanceRate], [CAcceptanceRate], [ChangeAfterNoOrderDays], [CreatedAtUtc], [CreatedByUserId], [DailyAOrders], [DailyBOrders], [DailyCOrders], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [FollowUpOnZeroOrdersWithMissedOffers], [IsDeleted], [OverallAOrders], [OverallBOrders], [OverallCOrders], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019d18d5-62e1-7000-8000-000000000001'', 80.0, 70.0, 60.0, 2, ''2026-10-11T00:00:00.0000000+00:00'', NULL, 7, 5, 3, NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), 80, 60, 40, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AAcceptanceRate', N'BAcceptanceRate', N'CAcceptanceRate', N'ChangeAfterNoOrderDays', N'CreatedAtUtc', N'CreatedByUserId', N'DailyAOrders', N'DailyBOrders', N'DailyCOrders', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'FollowUpOnZeroOrdersWithMissedOffers', N'IsDeleted', N'OverallAOrders', N'OverallBOrders', N'OverallCOrders', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[chefz].[PerformanceSettings]'))
        SET IDENTITY_INSERT [chefz].[PerformanceSettings] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011191524_AddChefzWorkbookReportsAndStatistics'
)
BEGIN
    CREATE INDEX [IX_PerformanceSettings_IsDeleted] ON [chefz].[PerformanceSettings] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011191524_AddChefzWorkbookReportsAndStatistics'
)
BEGIN
    CREATE INDEX [IX_SponsorStatistics_IsDeleted] ON [chefz].[SponsorStatistics] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011191524_AddChefzWorkbookReportsAndStatistics'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SponsorStatistics_SponsorId] ON [chefz].[SponsorStatistics] ([SponsorId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011191524_AddChefzWorkbookReportsAndStatistics'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011191524_AddChefzWorkbookReportsAndStatistics', N'10.0.11');
END;

COMMIT;
GO

