BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    IF SCHEMA_ID(N'jahez') IS NULL EXEC(N'CREATE SCHEMA [jahez];');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] ADD [DashboardSponsorId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezAccountHandover] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [RiderClientAssignmentId] uniqueidentifier NOT NULL,
        [StartedAtUtc] datetimeoffset NOT NULL,
        [EndedAtUtc] datetimeoffset NULL,
        [CommissionStartsOn] date NOT NULL,
        [CommissionPostedThrough] date NULL,
        [LastSettlementPaymentAtUtc] datetimeoffset NULL,
        [IsLegacy] bit NOT NULL,
        [DebtTransferred] bit NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(2000) NULL,
        CONSTRAINT [PK_JahezAccountHandover] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezHandover_Range] CHECK ([EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]),
        CONSTRAINT [FK_JahezAccountHandover_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezAccountHandover_RiderClientAssignments_RiderClientAssignmentId] FOREIGN KEY ([RiderClientAssignmentId]) REFERENCES [app].[RiderClientAssignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezAccountHandover_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezCashboxHandover] (
        [Id] uniqueidentifier NOT NULL,
        [BusinessDate] date NOT NULL,
        [Status] int NOT NULL,
        [FeeAmount] decimal(19,6) NOT NULL,
        [SettlementAmount] decimal(19,6) NOT NULL,
        [AccountantFeeAmount] decimal(19,6) NULL,
        [AccountantSettlementAmount] decimal(19,6) NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [AccountantUserId] uniqueidentifier NULL,
        [ApprovedByUserId] uniqueidentifier NULL,
        [ConfirmedAtUtc] datetimeoffset NULL,
        [DecidedAtUtc] datetimeoffset NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(2000) NULL,
        CONSTRAINT [PK_JahezCashboxHandover] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezCashboxHandover_Amounts] CHECK ([FeeAmount] >= 0 AND [SettlementAmount] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezCommandReceipt] (
        [Id] uniqueidentifier NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [CommandKey] nvarchar(150) NOT NULL,
        [Operation] nvarchar(100) NOT NULL,
        [PayloadHash] nvarchar(64) NOT NULL,
        [ResultJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_JahezCommandReceipt] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezImportBatch] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [ContentHash] nvarchar(64) NOT NULL,
        [UploadedByUserId] uniqueidentifier NOT NULL,
        [CommittedAtUtc] datetimeoffset NULL,
        [ReplacesBatchId] uniqueidentifier NULL,
        [CorrectionReason] nvarchar(2000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(2000) NULL,
        CONSTRAINT [PK_JahezImportBatch] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JahezImportBatch_JahezImportBatch_ReplacesBatchId] FOREIGN KEY ([ReplacesBatchId]) REFERENCES [jahez].[JahezImportBatch] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezAccountFee] (
        [Id] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [Amount] decimal(19,6) NOT NULL,
        [WaivedAmount] decimal(19,6) NOT NULL,
        [ApprovalRequestId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(2000) NULL,
        CONSTRAINT [PK_JahezAccountFee] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezFee_Amount] CHECK ([Amount] >= 0 AND [WaivedAmount] >= 0 AND [WaivedAmount] <= [Amount]),
        CONSTRAINT [FK_JahezAccountFee_JahezAccountHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez].[JahezAccountHandover] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezApprovalRequest] (
        [Id] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Status] int NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [TargetAccountId] uniqueidentifier NULL,
        [WaiverAmount] decimal(19,6) NOT NULL,
        [FromDate] date NULL,
        [ToDate] date NULL,
        [EffectiveAtUtc] datetimeoffset NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [ExternalResetReference] nvarchar(max) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(2000) NULL,
        CONSTRAINT [PK_JahezApprovalRequest] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JahezApprovalRequest_JahezAccountHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez].[JahezAccountHandover] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezApprovalRequest_PlatformRiderAccounts_TargetAccountId] FOREIGN KEY ([TargetAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezEarningsStatement] (
        [Id] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [FromDate] date NOT NULL,
        [ToDate] date NOT NULL,
        [TotalDeliveryPrice] decimal(19,6) NOT NULL,
        [TotalPenalties] decimal(19,6) NOT NULL,
        [TotalCashAmount] decimal(19,6) NOT NULL,
        [TotalDriverDebit] decimal(19,6) NOT NULL,
        [TotalServiceDeduction] decimal(19,6) NOT NULL,
        [TotalDriverCredit] decimal(19,6) NOT NULL,
        [TotalBonuses] decimal(19,6) NOT NULL,
        [TotalTips] decimal(19,6) NOT NULL,
        [TotalFreeOrders] decimal(19,6) NOT NULL,
        [SupersedesId] uniqueidentifier NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_JahezEarningsStatement] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezEarnings_Range] CHECK ([ToDate] >= [FromDate]),
        CONSTRAINT [FK_JahezEarningsStatement_JahezAccountHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez].[JahezAccountHandover] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezEarningsStatement_JahezEarningsStatement_SupersedesId] FOREIGN KEY ([SupersedesId]) REFERENCES [jahez].[JahezEarningsStatement] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezLedgerEntry] (
        [Id] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [Bucket] int NOT NULL,
        [Kind] int NOT NULL,
        [Amount] decimal(19,6) NOT NULL,
        [SourceId] uniqueidentifier NOT NULL,
        [ReversesEntryId] uniqueidentifier NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_JahezLedgerEntry] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JahezLedgerEntry_JahezAccountHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez].[JahezAccountHandover] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezLedgerEntry_JahezLedgerEntry_ReversesEntryId] FOREIGN KEY ([ReversesEntryId]) REFERENCES [jahez].[JahezLedgerEntry] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezReminderState] (
        [Id] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [AnchorAtUtc] datetimeoffset NOT NULL,
        [ResolvedAtUtc] datetimeoffset NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(2000) NULL,
        CONSTRAINT [PK_JahezReminderState] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JahezReminderState_JahezAccountHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez].[JahezAccountHandover] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezRiderSettlement] (
        [Id] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [ThroughDate] date NOT NULL,
        [RecordedAtUtc] datetimeoffset NOT NULL,
        [CollectedByUserId] uniqueidentifier NOT NULL,
        [FeePayment] decimal(19,6) NOT NULL,
        [DebtPayment] decimal(19,6) NOT NULL,
        [CommissionPayment] decimal(19,6) NOT NULL,
        [CountsAsSettlement] bit NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_JahezRiderSettlement] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezSettlement_Payments] CHECK ([FeePayment] >= 0 AND [DebtPayment] >= 0 AND [CommissionPayment] >= 0),
        CONSTRAINT [FK_JahezRiderSettlement_JahezAccountHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez].[JahezAccountHandover] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezImportFile] (
        [Id] uniqueidentifier NOT NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(260) NOT NULL,
        [ContentHash] nvarchar(64) NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_JahezImportFile] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JahezImportFile_JahezImportBatch_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [jahez].[JahezImportBatch] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezApprovalDecision] (
        [Id] uniqueidentifier NOT NULL,
        [RequestId] uniqueidentifier NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [DecidedAtUtc] datetimeoffset NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_JahezApprovalDecision] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JahezApprovalDecision_JahezApprovalRequest_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [jahez].[JahezApprovalRequest] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezCommissionPolicyPeriod] (
        [Id] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [ApprovalRequestId] uniqueidentifier NOT NULL,
        [FromDate] date NOT NULL,
        [ToDate] date NOT NULL,
        [Rate] decimal(19,6) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_JahezCommissionPolicyPeriod] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezPolicy_Range] CHECK ([ToDate] >= [FromDate] AND [Rate] = 0.15),
        CONSTRAINT [FK_JahezCommissionPolicyPeriod_JahezAccountHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez].[JahezAccountHandover] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezCommissionPolicyPeriod_JahezApprovalRequest_ApprovalRequestId] FOREIGN KEY ([ApprovalRequestId]) REFERENCES [jahez].[JahezApprovalRequest] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezCashboxEntry] (
        [Id] uniqueidentifier NOT NULL,
        [SettlementId] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [Section] int NOT NULL,
        [Amount] decimal(19,6) NOT NULL,
        [CollectedByUserId] uniqueidentifier NOT NULL,
        [ReceivedAtUtc] datetimeoffset NOT NULL,
        [CashboxHandoverId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(2000) NULL,
        CONSTRAINT [PK_JahezCashboxEntry] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezCashbox_Positive] CHECK ([Amount] > 0),
        CONSTRAINT [FK_JahezCashboxEntry_JahezAccountHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez].[JahezAccountHandover] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezCashboxEntry_JahezCashboxHandover_CashboxHandoverId] FOREIGN KEY ([CashboxHandoverId]) REFERENCES [jahez].[JahezCashboxHandover] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezCashboxEntry_JahezRiderSettlement_SettlementId] FOREIGN KEY ([SettlementId]) REFERENCES [jahez].[JahezRiderSettlement] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezImportRow] (
        [Id] uniqueidentifier NOT NULL,
        [FileId] uniqueidentifier NOT NULL,
        [RowNumber] int NOT NULL,
        [DriverId] nvarchar(150) NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [ToDate] date NULL,
        [DeliveryPrice] decimal(19,6) NOT NULL,
        [CashAmount] decimal(19,6) NOT NULL,
        [NetAmount] decimal(19,6) NOT NULL,
        [DriverAdjustment] decimal(19,6) NOT NULL,
        [Dispatches] int NULL,
        [RawValuesJson] nvarchar(max) NOT NULL,
        [ParseError] nvarchar(max) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_JahezImportRow] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JahezImportRow_JahezImportFile_FileId] FOREIGN KEY ([FileId]) REFERENCES [jahez].[JahezImportFile] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezDailyDispatch] (
        [Id] uniqueidentifier NOT NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [ImportRowId] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [Date] date NOT NULL,
        [Count] int NOT NULL,
        [AllocationReason] nvarchar(2000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_JahezDailyDispatch] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezDispatch_Count] CHECK ([Count] >= 0),
        CONSTRAINT [FK_JahezDailyDispatch_JahezAccountHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez].[JahezAccountHandover] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezDailyDispatch_JahezImportBatch_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [jahez].[JahezImportBatch] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezDailyDispatch_JahezImportRow_ImportRowId] FOREIGN KEY ([ImportRowId]) REFERENCES [jahez].[JahezImportRow] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE TABLE [jahez].[JahezTransaction] (
        [Id] uniqueidentifier NOT NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [ImportRowId] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [DeliveryPrice] decimal(19,6) NOT NULL,
        [CashAmount] decimal(19,6) NOT NULL,
        [NetAmount] decimal(19,6) NOT NULL,
        [DriverAdjustment] decimal(19,6) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_JahezTransaction] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JahezTransaction_JahezAccountHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez].[JahezAccountHandover] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezTransaction_JahezImportBatch_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [jahez].[JahezImportBatch] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezTransaction_JahezImportRow_ImportRowId] FOREIGN KEY ([ImportRowId]) REFERENCES [jahez].[JahezImportRow] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000130'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض جاهز ضمن نطاق جاهز.'', N''Read Jahez within Jahez scope.'', 130, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.read'', N''عرض جاهز'', N''Read Jahez'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000131'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تسليم وإغلاق حسابات جاهز ضمن نطاق جاهز.'', N''Manage Jahez handovers within Jahez scope.'', 131, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.handovers.manage'', N''تسليم وإغلاق حسابات جاهز'', N''Manage Jahez handovers'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000132'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تحصيل مبالغ جاهز ضمن نطاق جاهز.'', N''Collect Jahez payments within Jahez scope.'', 132, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.collections.manage'', N''تحصيل مبالغ جاهز'', N''Collect Jahez payments'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000133'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إنشاء طلبات جاهز ضمن نطاق جاهز.'', N''Create Jahez requests within Jahez scope.'', 133, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.requests.create'', N''إنشاء طلبات جاهز'', N''Create Jahez requests'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000134'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''اعتماد استثناءات جاهز ضمن نطاق جاهز.'', N''Approve Jahez exceptions within Jahez scope.'', 134, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.requests.approve'', N''اعتماد استثناءات جاهز'', N''Approve Jahez exceptions'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000135'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''اعتماد تصفير حسابات جاهز ضمن نطاق جاهز.'', N''Approve Jahez resets within Jahez scope.'', 135, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.resets.approve'', N''اعتماد تصفير حسابات جاهز'', N''Approve Jahez resets'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000136'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تسجيل أرباح جاهز ضمن نطاق جاهز.'', N''Manage Jahez earnings within Jahez scope.'', 136, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.earnings.manage'', N''تسجيل أرباح جاهز'', N''Manage Jahez earnings'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000137'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''استيراد تقارير جاهز ضمن نطاق جاهز.'', N''Import Jahez reports within Jahez scope.'', 137, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.imports.manage'', N''استيراد تقارير جاهز'', N''Import Jahez reports'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000138'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تسويات وأرصدة جاهز ضمن نطاق جاهز.'', N''Adjust Jahez balances within Jahez scope.'', 138, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.adjustments.manage'', N''تسويات وأرصدة جاهز'', N''Adjust Jahez balances'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000139'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض صندوق جاهز ضمن نطاق جاهز.'', N''Read Jahez cashbox within Jahez scope.'', 139, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.cashbox.read'', N''عرض صندوق جاهز'', N''Read Jahez cashbox'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000140'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تسليم صندوق جاهز ضمن نطاق جاهز.'', N''Submit Jahez cashbox within Jahez scope.'', 140, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.cashbox.submit'', N''تسليم صندوق جاهز'', N''Submit Jahez cashbox'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000141'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تأكيد استلام صندوق جاهز ضمن نطاق جاهز.'', N''Confirm Jahez cashbox receipt within Jahez scope.'', 141, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.cashbox.confirm'', N''تأكيد استلام صندوق جاهز'', N''Confirm Jahez cashbox receipt'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000142'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''اعتماد تسليم صندوق جاهز ضمن نطاق جاهز.'', N''Approve Jahez cashbox handovers within Jahez scope.'', 142, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.cashbox.approve'', N''اعتماد تسليم صندوق جاهز'', N''Approve Jahez cashbox handovers'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_PlatformRiderAccounts_DashboardSponsorId] ON [app].[PlatformRiderAccounts] ([DashboardSponsorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezAccountFee_HandoverId] ON [jahez].[JahezAccountFee] ([HandoverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezAccountFee_IsDeleted] ON [jahez].[JahezAccountFee] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezAccountHandover_IsDeleted] ON [jahez].[JahezAccountHandover] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_JahezAccountHandover_PlatformRiderAccountId] ON [jahez].[JahezAccountHandover] ([PlatformRiderAccountId]) WHERE [EndedAtUtc] IS NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezAccountHandover_RiderClientAssignmentId] ON [jahez].[JahezAccountHandover] ([RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezAccountHandover_RiderProfileId_StartedAtUtc] ON [jahez].[JahezAccountHandover] ([RiderProfileId], [StartedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezApprovalDecision_RequestId] ON [jahez].[JahezApprovalDecision] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezApprovalRequest_HandoverId] ON [jahez].[JahezApprovalRequest] ([HandoverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezApprovalRequest_IsDeleted] ON [jahez].[JahezApprovalRequest] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezApprovalRequest_Status_CreatedAtUtc] ON [jahez].[JahezApprovalRequest] ([Status], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezApprovalRequest_TargetAccountId] ON [jahez].[JahezApprovalRequest] ([TargetAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezCashboxEntry_CashboxHandoverId_ReceivedAtUtc] ON [jahez].[JahezCashboxEntry] ([CashboxHandoverId], [ReceivedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezCashboxEntry_HandoverId] ON [jahez].[JahezCashboxEntry] ([HandoverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezCashboxEntry_IsDeleted] ON [jahez].[JahezCashboxEntry] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezCashboxEntry_SettlementId_Section] ON [jahez].[JahezCashboxEntry] ([SettlementId], [Section]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezCashboxHandover_IsDeleted] ON [jahez].[JahezCashboxHandover] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezCashboxHandover_Status_BusinessDate] ON [jahez].[JahezCashboxHandover] ([Status], [BusinessDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezCommandReceipt_ActorUserId_Operation_CommandKey] ON [jahez].[JahezCommandReceipt] ([ActorUserId], [Operation], [CommandKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezCommissionPolicyPeriod_ApprovalRequestId] ON [jahez].[JahezCommissionPolicyPeriod] ([ApprovalRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezCommissionPolicyPeriod_HandoverId_FromDate_ToDate] ON [jahez].[JahezCommissionPolicyPeriod] ([HandoverId], [FromDate], [ToDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezDailyDispatch_BatchId] ON [jahez].[JahezDailyDispatch] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezDailyDispatch_HandoverId_Date] ON [jahez].[JahezDailyDispatch] ([HandoverId], [Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezDailyDispatch_ImportRowId_HandoverId] ON [jahez].[JahezDailyDispatch] ([ImportRowId], [HandoverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezEarningsStatement_HandoverId_FromDate_ToDate] ON [jahez].[JahezEarningsStatement] ([HandoverId], [FromDate], [ToDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_JahezEarningsStatement_SupersedesId] ON [jahez].[JahezEarningsStatement] ([SupersedesId]) WHERE [SupersedesId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezImportBatch_IsDeleted] ON [jahez].[JahezImportBatch] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezImportBatch_Kind_ContentHash] ON [jahez].[JahezImportBatch] ([Kind], [ContentHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_JahezImportBatch_ReplacesBatchId] ON [jahez].[JahezImportBatch] ([ReplacesBatchId]) WHERE [ReplacesBatchId] IS NOT NULL AND [CommittedAtUtc] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezImportFile_BatchId_ContentHash] ON [jahez].[JahezImportFile] ([BatchId], [ContentHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezImportRow_FileId_RowNumber] ON [jahez].[JahezImportRow] ([FileId], [RowNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezLedgerEntry_HandoverId_Bucket_OccurredAtUtc] ON [jahez].[JahezLedgerEntry] ([HandoverId], [Bucket], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_JahezLedgerEntry_ReversesEntryId] ON [jahez].[JahezLedgerEntry] ([ReversesEntryId]) WHERE [ReversesEntryId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezLedgerEntry_SourceId_Bucket_Kind] ON [jahez].[JahezLedgerEntry] ([SourceId], [Bucket], [Kind]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezReminderState_HandoverId_AnchorAtUtc] ON [jahez].[JahezReminderState] ([HandoverId], [AnchorAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezReminderState_IsDeleted] ON [jahez].[JahezReminderState] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezRiderSettlement_HandoverId_RecordedAtUtc] ON [jahez].[JahezRiderSettlement] ([HandoverId], [RecordedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezTransaction_BatchId] ON [jahez].[JahezTransaction] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE INDEX [IX_JahezTransaction_HandoverId_OccurredAtUtc] ON [jahez].[JahezTransaction] ([HandoverId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezTransaction_ImportRowId] ON [jahez].[JahezTransaction] ([ImportRowId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] ADD CONSTRAINT [FK_PlatformRiderAccounts_Sponsors_DashboardSponsorId] FOREIGN KEY ([DashboardSponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003151506_AddJahezOperations'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003151506_AddJahezOperations', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003153822_PreserveJahezFinancialEvidence'
)
BEGIN
    ALTER TABLE [jahez].[JahezLedgerEntry] ADD [CalculationJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003153822_PreserveJahezFinancialEvidence'
)
BEGIN
    ALTER TABLE [jahez].[JahezLedgerEntry] ADD [FromDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003153822_PreserveJahezFinancialEvidence'
)
BEGIN
    ALTER TABLE [jahez].[JahezLedgerEntry] ADD [ThroughDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003153822_PreserveJahezFinancialEvidence'
)
BEGIN
    ALTER TABLE [jahez].[JahezCashboxHandover] ADD [ConfirmationReason] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003153822_PreserveJahezFinancialEvidence'
)
BEGIN
    ALTER TABLE [jahez].[JahezCashboxHandover] ADD [DecisionReason] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003153822_PreserveJahezFinancialEvidence'
)
BEGIN
    CREATE INDEX [IX_JahezAccountFee_ApprovalRequestId] ON [jahez].[JahezAccountFee] ([ApprovalRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003153822_PreserveJahezFinancialEvidence'
)
BEGIN
    ALTER TABLE [jahez].[JahezAccountFee] ADD CONSTRAINT [FK_JahezAccountFee_JahezApprovalRequest_ApprovalRequestId] FOREIGN KEY ([ApprovalRequestId]) REFERENCES [jahez].[JahezApprovalRequest] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003153822_PreserveJahezFinancialEvidence'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003153822_PreserveJahezFinancialEvidence', N'10.0.11');
END;

COMMIT;
GO

