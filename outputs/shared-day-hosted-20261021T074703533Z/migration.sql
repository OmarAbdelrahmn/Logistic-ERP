BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    DROP INDEX [IX_NinjaDailyPerformance_PlatformRiderAccountId_ReportDate] ON [ninja].[NinjaDailyPerformance];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    DROP INDEX [IX_NinjaDailyDeductions_PlatformRiderAccountId_ReportDate] ON [ninja].[NinjaDailyDeductions];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    DROP INDEX [IX_DailyPerformance_PlatformRiderAccountId_ReportDate] ON [chefz].[DailyPerformance];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    ALTER TABLE [jahez_v2].[Operations] ADD [DayUsageJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    ALTER TABLE [ninja].[NinjaDailyPerformance] ADD [SourceId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    ALTER TABLE [ninja].[NinjaDailyDeductions] ADD [SourceId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    ALTER TABLE [chefz].[DailyPerformance] ADD [SourceId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    ALTER TABLE [jahez_v2].[DailyOrders] ADD [DayUsageId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE TABLE [chefz].[DailyPerformanceSources] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [ImportId] uniqueidentifier NOT NULL,
        [ReportDate] date NOT NULL,
        [ReportIdNumber] nvarchar(100) NOT NULL,
        [SourceRowNumber] int NOT NULL,
        [TotalValidOffers] int NOT NULL,
        [AcceptedOffers] int NOT NULL,
        [DeclinedOffers] int NOT NULL,
        [MissedOffers] int NOT NULL,
        [AcceptanceRate] decimal(5,2) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_DailyPerformanceSources] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChefzPerformanceSource_Values] CHECK ([TotalValidOffers] >= 0 AND [AcceptedOffers] >= 0 AND [DeclinedOffers] >= 0 AND [MissedOffers] >= 0 AND [AcceptanceRate] BETWEEN 0 AND 100),
        CONSTRAINT [FK_DailyPerformanceSources_PerformanceImports_ImportId] FOREIGN KEY ([ImportId]) REFERENCES [chefz].[PerformanceImports] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DailyPerformanceSources_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE TABLE [chefz].[ImportedOrderRows] (
        [Id] uniqueidentifier NOT NULL,
        [ImportId] uniqueidentifier NOT NULL,
        [SourceRowNumber] int NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [RiderClientAssignmentId] uniqueidentifier NOT NULL,
        [BusinessDate] date NOT NULL,
        [UsageSequence] int NOT NULL,
        [OrderId] nvarchar(100) NOT NULL,
        [DeliveryFees] decimal(18,2) NOT NULL,
        [Tips] decimal(18,2) NOT NULL,
        [Compensation] decimal(18,2) NOT NULL,
        [Bonuses] decimal(18,2) NOT NULL,
        [Deductions] decimal(18,2) NOT NULL,
        [CashCollected] decimal(18,2) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_ImportedOrderRows] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChefzImportedRow_Values] CHECK ([UsageSequence] > 0 AND [SourceRowNumber] > 1 AND [Deductions] >= 0 AND [CashCollected] >= 0),
        CONSTRAINT [FK_ImportedOrderRows_OrderSettlementImports_ImportId] FOREIGN KEY ([ImportId]) REFERENCES [chefz].[OrderSettlementImports] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ImportedOrderRows_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ImportedOrderRows_RiderClientAssignments_RiderClientAssignmentId] FOREIGN KEY ([RiderClientAssignmentId]) REFERENCES [app].[RiderClientAssignments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE TABLE [ninja].[NinjaDailyDeductionSources] (
        [Id] uniqueidentifier NOT NULL,
        [TotalCollectedAmountAtDelivery] decimal(18,2) NOT NULL,
        [TotalDeductions] decimal(18,2) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [ImportId] uniqueidentifier NOT NULL,
        [ReportDate] date NOT NULL,
        [CaptainId] nvarchar(150) NOT NULL,
        [SourceRowNumber] int NOT NULL,
        CONSTRAINT [PK_NinjaDailyDeductionSources] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_NinjaDeductionSource_Values] CHECK ([TotalCollectedAmountAtDelivery] >= 0),
        CONSTRAINT [FK_NinjaDailyDeductionSources_NinjaDailyImports_ImportId] FOREIGN KEY ([ImportId]) REFERENCES [ninja].[NinjaDailyImports] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NinjaDailyDeductionSources_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE TABLE [ninja].[NinjaDailyPerformanceSources] (
        [Id] uniqueidentifier NOT NULL,
        [TotalOrders] int NOT NULL,
        [TotalWorkingHours] decimal(18,8) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [ImportId] uniqueidentifier NOT NULL,
        [ReportDate] date NOT NULL,
        [CaptainId] nvarchar(150) NOT NULL,
        [SourceRowNumber] int NOT NULL,
        CONSTRAINT [PK_NinjaDailyPerformanceSources] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_NinjaPerformanceSource_Values] CHECK ([TotalOrders] >= 0 AND [TotalWorkingHours] BETWEEN 0 AND 24),
        CONSTRAINT [FK_NinjaDailyPerformanceSources_NinjaDailyImports_ImportId] FOREIGN KEY ([ImportId]) REFERENCES [ninja].[NinjaDailyImports] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NinjaDailyPerformanceSources_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE TABLE [app].[PlatformAccountDayUsages] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [RiderClientAssignmentId] uniqueidentifier NOT NULL,
        [BusinessDate] date NOT NULL,
        [Sequence] int NULL,
        [OrdersCount] int NULL,
        [WorkingHours] decimal(18,8) NULL,
        [CollectedAmount] decimal(18,2) NULL,
        [DeductionsAmount] decimal(18,2) NULL,
        [TotalValidOffers] int NULL,
        [DeclinedOffers] int NULL,
        [MissedOffers] int NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_PlatformAccountDayUsages] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PlatformAccountDayUsage_Values] CHECK (([Sequence] IS NULL OR [Sequence] > 0) AND ([OrdersCount] IS NULL OR [OrdersCount] >= 0) AND ([WorkingHours] IS NULL OR [WorkingHours] BETWEEN 0 AND 24) AND ([CollectedAmount] IS NULL OR [CollectedAmount] >= 0) AND ([TotalValidOffers] IS NULL OR [TotalValidOffers] >= 0) AND ([DeclinedOffers] IS NULL OR [DeclinedOffers] >= 0) AND ([MissedOffers] IS NULL OR [MissedOffers] >= 0)),
        CONSTRAINT [FK_PlatformAccountDayUsages_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlatformAccountDayUsages_RiderClientAssignments_RiderClientAssignmentId] FOREIGN KEY ([RiderClientAssignmentId]) REFERENCES [app].[RiderClientAssignments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    EXEC(N'
    INSERT INTO ninja.NinjaDailyPerformanceSources
      (Id, PlatformRiderAccountId, ImportId, ReportDate, CaptainId, SourceRowNumber, TotalOrders, TotalWorkingHours, CreatedAtUtc, CreatedByUserId)
    SELECT Id, PlatformRiderAccountId, ImportId, ReportDate, CaptainId, SourceRowNumber, TotalOrders, TotalWorkingHours, CreatedAtUtc, CreatedByUserId
    FROM ninja.NinjaDailyPerformance;
    UPDATE ninja.NinjaDailyPerformance SET SourceId = Id;
    INSERT INTO ninja.NinjaDailyDeductionSources
      (Id, PlatformRiderAccountId, ImportId, ReportDate, CaptainId, SourceRowNumber, TotalCollectedAmountAtDelivery, TotalDeductions, CreatedAtUtc, CreatedByUserId)
    SELECT Id, PlatformRiderAccountId, ImportId, ReportDate, CaptainId, SourceRowNumber, TotalCollectedAmountAtDelivery, TotalDeductions, CreatedAtUtc, CreatedByUserId
    FROM ninja.NinjaDailyDeductions;
    UPDATE ninja.NinjaDailyDeductions SET SourceId = Id;
    INSERT INTO chefz.DailyPerformanceSources
      (Id, PlatformRiderAccountId, ImportId, ReportDate, ReportIdNumber, SourceRowNumber, TotalValidOffers,
       AcceptedOffers, DeclinedOffers, MissedOffers, AcceptanceRate, CreatedAtUtc, CreatedByUserId, IsDeleted)
    SELECT Id, PlatformRiderAccountId, ImportId, ReportDate, ReportIdNumber, SourceRowNumber, TotalValidOffers,
           AcceptedOffers, DeclinedOffers, MissedOffers, AcceptanceRate, CreatedAtUtc, CreatedByUserId, 0
    FROM chefz.DailyPerformance;
    UPDATE chefz.DailyPerformance SET SourceId = Id;

    -- Only boundary days are materialized; long-running assignments do not create a row per day.
    ;WITH BoundaryDates AS (
      SELECT PlatformRiderAccountId, EffectiveFrom BusinessDate FROM app.RiderClientAssignments WHERE IsDeleted = 0 AND Status IN (2,3)
      UNION
      SELECT PlatformRiderAccountId, EffectiveTo FROM app.RiderClientAssignments WHERE IsDeleted = 0 AND Status IN (2,3) AND EffectiveTo IS NOT NULL
    ), Participants AS (
      SELECT a.Id AssignmentId, a.PlatformRiderAccountId, d.BusinessDate, a.EffectiveFrom, a.EffectiveTo,
             h.StartedAtUtc, h.EndedAtUtc,
             COUNT(*) OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate) ParticipantCount,
             COUNT(h.Id) OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate) SessionCount,
             MIN(a.EffectiveFrom) OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate) FirstStart,
             MAX(a.EffectiveFrom) OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate) LastStart,
             MIN(a.EffectiveTo) OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate) FirstEnd,
             ROW_NUMBER() OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate ORDER BY a.EffectiveFrom, a.EffectiveTo) DateSequence,
             ROW_NUMBER() OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate ORDER BY h.StartedAtUtc) SessionSequence
      FROM BoundaryDates d JOIN app.RiderClientAssignments a ON a.PlatformRiderAccountId = d.PlatformRiderAccountId
        AND a.IsDeleted = 0 AND a.Status IN (2,3) AND a.EffectiveFrom <= d.BusinessDate
        AND (a.EffectiveTo IS NULL OR a.EffectiveTo >= d.BusinessDate)
      LEFT JOIN jahez_v2.AccountSessions h ON h.RiderClientAssignmentId = a.Id AND h.IsDeleted = 0
      WHERE h.Id IS NULL OR (h.StartedAtUtc < TODATETIMEOFFSET(DATEADD(day,1,CAST(d.BusinessDate AS datetime2)), ''+03:00'')
        AND (h.EndedAtUtc IS NULL OR h.EndedAtUtc > TODATETIMEOFFSET(CAST(d.BusinessDate AS datetime2), ''+03:00'')))
    )
    INSERT INTO app.PlatformAccountDayUsages
      (Id, PlatformRiderAccountId, RiderClientAssignmentId, BusinessDate, [Sequence], Reason, CreatedAtUtc, IsDeleted)
    SELECT NEWID(), p.PlatformRiderAccountId, p.AssignmentId, p.BusinessDate,
      CASE WHEN p.ParticipantCount = 1 THEN 1
           WHEN p.SessionCount = p.ParticipantCount AND NOT EXISTS (
             SELECT 1 FROM Participants x JOIN Participants y ON x.PlatformRiderAccountId = y.PlatformRiderAccountId
               AND x.BusinessDate = y.BusinessDate AND x.AssignmentId <> y.AssignmentId
               AND x.StartedAtUtc <= y.StartedAtUtc
             WHERE x.PlatformRiderAccountId = p.PlatformRiderAccountId AND x.BusinessDate = p.BusinessDate
               AND (x.StartedAtUtc = y.StartedAtUtc OR x.EndedAtUtc IS NULL OR x.EndedAtUtc > y.StartedAtUtc)
           ) THEN CAST(p.SessionSequence AS int)
           WHEN p.ParticipantCount = 2 AND p.FirstStart < p.LastStart AND p.FirstEnd = p.BusinessDate
             AND p.LastStart = p.BusinessDate AND NOT EXISTS (
               SELECT 1 FROM Participants x WHERE x.PlatformRiderAccountId = p.PlatformRiderAccountId
                 AND x.BusinessDate = p.BusinessDate AND x.EffectiveFrom = p.FirstStart
                 AND (x.EffectiveTo IS NULL OR x.EffectiveTo <> p.BusinessDate)
             ) THEN CAST(p.DateSequence AS int)
           ELSE NULL END,
      ''Migration: boundary history; figures not inferred'', SYSUTCDATETIME(), 0
    FROM Participants p;
    ');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NinjaDailyPerformance_PlatformRiderAccountId_ReportDate_RiderClientAssignmentId] ON [ninja].[NinjaDailyPerformance] ([PlatformRiderAccountId], [ReportDate], [RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NinjaDailyPerformance_SourceId_RiderClientAssignmentId] ON [ninja].[NinjaDailyPerformance] ([SourceId], [RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NinjaDailyDeductions_PlatformRiderAccountId_ReportDate_RiderClientAssignmentId] ON [ninja].[NinjaDailyDeductions] ([PlatformRiderAccountId], [ReportDate], [RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NinjaDailyDeductions_SourceId_RiderClientAssignmentId] ON [ninja].[NinjaDailyDeductions] ([SourceId], [RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DailyPerformance_PlatformRiderAccountId_ReportDate_RiderClientAssignmentId] ON [chefz].[DailyPerformance] ([PlatformRiderAccountId], [ReportDate], [RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DailyPerformance_SourceId_RiderClientAssignmentId] ON [chefz].[DailyPerformance] ([SourceId], [RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE INDEX [IX_DailyOrders_DayUsageId] ON [jahez_v2].[DailyOrders] ([DayUsageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE INDEX [IX_DailyPerformanceSources_ImportId] ON [chefz].[DailyPerformanceSources] ([ImportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE INDEX [IX_DailyPerformanceSources_IsDeleted] ON [chefz].[DailyPerformanceSources] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DailyPerformanceSources_PlatformRiderAccountId_ReportDate] ON [chefz].[DailyPerformanceSources] ([PlatformRiderAccountId], [ReportDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ImportedOrderRows_ImportId_SourceRowNumber] ON [chefz].[ImportedOrderRows] ([ImportId], [SourceRowNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE INDEX [IX_ImportedOrderRows_PlatformRiderAccountId_BusinessDate] ON [chefz].[ImportedOrderRows] ([PlatformRiderAccountId], [BusinessDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE INDEX [IX_ImportedOrderRows_RiderClientAssignmentId] ON [chefz].[ImportedOrderRows] ([RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE INDEX [IX_NinjaDailyDeductionSources_ImportId] ON [ninja].[NinjaDailyDeductionSources] ([ImportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NinjaDailyDeductionSources_PlatformRiderAccountId_ReportDate] ON [ninja].[NinjaDailyDeductionSources] ([PlatformRiderAccountId], [ReportDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE INDEX [IX_NinjaDailyPerformanceSources_ImportId] ON [ninja].[NinjaDailyPerformanceSources] ([ImportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NinjaDailyPerformanceSources_PlatformRiderAccountId_ReportDate] ON [ninja].[NinjaDailyPerformanceSources] ([PlatformRiderAccountId], [ReportDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountDayUsages_IsDeleted] ON [app].[PlatformAccountDayUsages] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PlatformAccountDayUsages_PlatformRiderAccountId_BusinessDate_RiderClientAssignmentId] ON [app].[PlatformAccountDayUsages] ([PlatformRiderAccountId], [BusinessDate], [RiderClientAssignmentId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PlatformAccountDayUsages_PlatformRiderAccountId_BusinessDate_Sequence] ON [app].[PlatformAccountDayUsages] ([PlatformRiderAccountId], [BusinessDate], [Sequence]) WHERE [Sequence] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountDayUsages_RiderClientAssignmentId] ON [app].[PlatformAccountDayUsages] ([RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    ALTER TABLE [jahez_v2].[DailyOrders] ADD CONSTRAINT [FK_DailyOrders_PlatformAccountDayUsages_DayUsageId] FOREIGN KEY ([DayUsageId]) REFERENCES [app].[PlatformAccountDayUsages] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    ALTER TABLE [chefz].[DailyPerformance] ADD CONSTRAINT [FK_DailyPerformance_DailyPerformanceSources_SourceId] FOREIGN KEY ([SourceId]) REFERENCES [chefz].[DailyPerformanceSources] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    ALTER TABLE [ninja].[NinjaDailyDeductions] ADD CONSTRAINT [FK_NinjaDailyDeductions_NinjaDailyDeductionSources_SourceId] FOREIGN KEY ([SourceId]) REFERENCES [ninja].[NinjaDailyDeductionSources] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    ALTER TABLE [ninja].[NinjaDailyPerformance] ADD CONSTRAINT [FK_NinjaDailyPerformance_NinjaDailyPerformanceSources_SourceId] FOREIGN KEY ([SourceId]) REFERENCES [ninja].[NinjaDailyPerformanceSources] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261021071103_AddPlatformAccountDayAllocations'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021071103_AddPlatformAccountDayAllocations', N'10.0.11');
END;

COMMIT;
GO

