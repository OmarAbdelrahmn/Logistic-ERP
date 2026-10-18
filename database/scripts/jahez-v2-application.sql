IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalRequest] ADD [AdjustmentTarget] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalRequest] ADD [DebtAdjustment] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalRequest] ADD [IsV2] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalRequest] ADD [OperationId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalRequest] ADD [ProposedCommission] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalRequest] ADD [ProposedNet] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalDecision] ADD [DecisionJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    CREATE TABLE [jahez].[JahezOperation] (
        [Id] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [Mode] int NOT NULL,
        [Status] int NOT NULL,
        [PaymentStatus] int NOT NULL,
        [ThroughDate] date NOT NULL,
        [EffectiveAtUtc] datetimeoffset NOT NULL,
        [Net] decimal(18,2) NOT NULL,
        [Commission] decimal(18,2) NOT NULL,
        [CalculatedCommission] decimal(18,2) NOT NULL,
        [Remaining] decimal(18,2) NOT NULL,
        [CalculationJson] nvarchar(max) NOT NULL,
        [Fund] int NULL,
        [Purpose] int NOT NULL,
        [TransferToRider] bit NOT NULL,
        [PaymentReference] nvarchar(500) NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [ApprovedRequestId] uniqueidentifier NULL,
        [ExecutedAtUtc] datetimeoffset NULL,
        [ExecutedByUserId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_JahezOperation] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezOperation_Amounts] CHECK ([Commission] >= 0 AND [CalculatedCommission] >= 0 AND [Remaining] = [Net] + [Commission]),
        CONSTRAINT [CK_JahezOperation_State] CHECK ([Mode] BETWEEN 1 AND 3 AND [Status] BETWEEN 1 AND 3 AND [PaymentStatus] BETWEEN 1 AND 4 AND (([Status] = 3 AND [ExecutedAtUtc] IS NOT NULL) OR ([Status] <> 3 AND [ExecutedAtUtc] IS NULL))),
        CONSTRAINT [FK_JahezOperation_JahezAccountHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez].[JahezAccountHandover] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezOperation_JahezApprovalRequest_ApprovedRequestId] FOREIGN KEY ([ApprovedRequestId]) REFERENCES [jahez].[JahezApprovalRequest] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    CREATE TABLE [jahez].[JahezTransferredDebt] (
        [Id] uniqueidentifier NOT NULL,
        [OperationId] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaidAtUtc] datetimeoffset NULL,
        [PaidByUserId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_JahezTransferredDebt] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezTransferredDebt_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_JahezTransferredDebt_JahezOperation_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [jahez].[JahezOperation] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezTransferredDebt_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    CREATE TABLE [jahez].[JahezMoneyMovement] (
        [Id] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [OperationId] uniqueidentifier NULL,
        [TransferredDebtId] uniqueidentifier NULL,
        [LegacyCashboxEntryId] uniqueidentifier NULL,
        [Fund] int NOT NULL,
        [Direction] int NOT NULL,
        [Purpose] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [Reference] nvarchar(500) NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_JahezMoneyMovement] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezMoney_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_JahezMoney_Routing] CHECK (([Direction] = 1 AND ([Fund] = 1 OR ([Fund] = 2 AND [Purpose] = 2))) OR ([Direction] = 2 AND [Fund] IN (1, 3) AND [Purpose] <> 2)),
        CONSTRAINT [FK_JahezMoneyMovement_JahezAccountHandover_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez].[JahezAccountHandover] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezMoneyMovement_JahezCashboxEntry_LegacyCashboxEntryId] FOREIGN KEY ([LegacyCashboxEntryId]) REFERENCES [jahez].[JahezCashboxEntry] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezMoneyMovement_JahezOperation_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [jahez].[JahezOperation] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezMoneyMovement_JahezTransferredDebt_TransferredDebtId] FOREIGN KEY ([TransferredDebtId]) REFERENCES [jahez].[JahezTransferredDebt] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JahezMoneyMovement_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000700'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''سحب حساب جاهز'', N''jahez.withdraw'', 700, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.withdraw'', N''سحب حساب جاهز'', N''jahez.withdraw'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000701'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تنفيذ تصفية جاهز'', N''jahez.settle'', 701, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.settle'', N''تنفيذ تصفية جاهز'', N''jahez.settle'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000702'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تسجيل قبض رسوم جاهز'', N''jahez.fees.record'', 702, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.fees.record'', N''تسجيل قبض رسوم جاهز'', N''jahez.fees.record'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000703'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تحديث حالة حساب جاهز'', N''jahez.accounts.status.update'', 703, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.accounts.status.update'', N''تحديث حالة حساب جاهز'', N''jahez.accounts.status.update'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000704'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''قبض أموال جاهز'', N''jahez.money.receive'', 704, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.money.receive'', N''قبض أموال جاهز'', N''jahez.money.receive'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000705'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''صرف أموال جاهز'', N''jahez.money.disburse'', 705, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.money.disburse'', N''صرف أموال جاهز'', N''jahez.money.disburse'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000706'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل المديونية الإدارية'', N''jahez.administrative-debt.adjust'', 706, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.administrative-debt.adjust'', N''تعديل المديونية الإدارية'', N''jahez.administrative-debt.adjust'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000707'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تأكيد سداد المستحق المرحل'', N''jahez.transferred-debt.repay'', 707, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.transferred-debt.repay'', N''تأكيد سداد المستحق المرحل'', N''jahez.transferred-debt.repay'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000708'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''طلب إعفاء رسوم'', N''jahez.requests.fee.create'', 708, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.requests.fee.create'', N''طلب إعفاء رسوم'', N''jahez.requests.fee.create'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000709'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''اعتماد إعفاء رسوم'', N''jahez.requests.fee.approve'', 709, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.requests.fee.approve'', N''اعتماد إعفاء رسوم'', N''jahez.requests.fee.approve'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000710'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''طلب تبديل حساب'', N''jahez.requests.switch.create'', 710, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.requests.switch.create'', N''طلب تبديل حساب'', N''jahez.requests.switch.create'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000711'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''اعتماد تبديل حساب'', N''jahez.requests.switch.approve'', 711, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.requests.switch.approve'', N''اعتماد تبديل حساب'', N''jahez.requests.switch.approve'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000712'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''طلب ترحيل مستحق'', N''jahez.requests.transfer.create'', 712, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.requests.transfer.create'', N''طلب ترحيل مستحق'', N''jahez.requests.transfer.create'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000713'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''اعتماد ترحيل مستحق'', N''jahez.requests.transfer.approve'', 713, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.requests.transfer.approve'', N''اعتماد ترحيل مستحق'', N''jahez.requests.transfer.approve'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000714'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''طلب سياسة عمولة'', N''jahez.requests.commission.create'', 714, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.requests.commission.create'', N''طلب سياسة عمولة'', N''jahez.requests.commission.create'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000715'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''اعتماد سياسة عمولة'', N''jahez.requests.commission.approve'', 715, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.requests.commission.approve'', N''اعتماد سياسة عمولة'', N''jahez.requests.commission.approve'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000716'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''طلب تعديل تصفية أو مديونية'', N''jahez.requests.adjustment.create'', 716, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.requests.adjustment.create'', N''طلب تعديل تصفية أو مديونية'', N''jahez.requests.adjustment.create'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000717'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''اعتماد تعديل تصفية أو مديونية'', N''jahez.requests.adjustment.approve'', 717, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.requests.adjustment.approve'', N''اعتماد تعديل تصفية أو مديونية'', N''jahez.requests.adjustment.approve'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_JahezApprovalRequest_OperationId] ON [jahez].[JahezApprovalRequest] ([OperationId]) WHERE [OperationId] IS NOT NULL AND [Status] = 1 AND [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    CREATE INDEX [IX_JahezMoneyMovement_Fund_OccurredAtUtc] ON [jahez].[JahezMoneyMovement] ([Fund], [OccurredAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    CREATE INDEX [IX_JahezMoneyMovement_HandoverId] ON [jahez].[JahezMoneyMovement] ([HandoverId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_JahezMoneyMovement_LegacyCashboxEntryId] ON [jahez].[JahezMoneyMovement] ([LegacyCashboxEntryId]) WHERE [LegacyCashboxEntryId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_JahezMoneyMovement_OperationId] ON [jahez].[JahezMoneyMovement] ([OperationId]) WHERE [OperationId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    CREATE INDEX [IX_JahezMoneyMovement_RiderProfileId] ON [jahez].[JahezMoneyMovement] ([RiderProfileId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_JahezMoneyMovement_TransferredDebtId] ON [jahez].[JahezMoneyMovement] ([TransferredDebtId]) WHERE [TransferredDebtId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    CREATE INDEX [IX_JahezOperation_ApprovedRequestId] ON [jahez].[JahezOperation] ([ApprovedRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_JahezOperation_HandoverId] ON [jahez].[JahezOperation] ([HandoverId]) WHERE [ExecutedAtUtc] IS NULL AND [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    CREATE INDEX [IX_JahezOperation_HandoverId_CreatedAtUtc] ON [jahez].[JahezOperation] ([HandoverId], [CreatedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    CREATE INDEX [IX_JahezOperation_IsDeleted] ON [jahez].[JahezOperation] ([IsDeleted]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    CREATE INDEX [IX_JahezTransferredDebt_IsDeleted] ON [jahez].[JahezTransferredDebt] ([IsDeleted]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JahezTransferredDebt_OperationId] ON [jahez].[JahezTransferredDebt] ([OperationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    CREATE INDEX [IX_JahezTransferredDebt_RiderProfileId_PaidAtUtc] ON [jahez].[JahezTransferredDebt] ([RiderProfileId], [PaidAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalRequest] ADD CONSTRAINT [FK_JahezApprovalRequest_JahezOperation_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [jahez].[JahezOperation] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    INSERT INTO [jahez].[JahezMoneyMovement]
        ([Id], [HandoverId], [RiderProfileId], [LegacyCashboxEntryId], [Fund], [Direction], [Purpose],
         [Amount], [ActorUserId], [OccurredAtUtc], [Reference], [Reason], [CreatedAtUtc], [CreatedByUserId])
    SELECT e.[Id], e.[HandoverId], h.[RiderProfileId], e.[Id], 1, 1,
        CASE WHEN e.[Section] = 1 THEN 3 ELSE 1 END,
        e.[Amount], e.[CollectedByUserId], e.[ReceivedAtUtc],
        CONCAT('legacy:', CONVERT(nvarchar(36), e.[Id])), N'ترحيل المقبوضات التاريخية مرة واحدة',
        e.[CreatedAtUtc], e.[CreatedByUserId]
    FROM [jahez].[JahezCashboxEntry] e
    JOIN [jahez].[JahezAccountHandover] h ON h.[Id] = e.[HandoverId]
    WHERE e.[IsDeleted] = 0 AND NOT EXISTS
        (SELECT 1 FROM [jahez].[JahezMoneyMovement] m WHERE m.[LegacyCashboxEntryId] = e.[Id]);
    IF (SELECT COALESCE(SUM([Amount]), 0) FROM [jahez].[JahezCashboxEntry] WHERE [IsDeleted] = 0)
       <> (SELECT COALESCE(SUM([Amount]), 0) FROM [jahez].[JahezMoneyMovement] WHERE [LegacyCashboxEntryId] IS NOT NULL)
        THROW 51000, 'Jahez legacy receipt reconciliation failed.', 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015072958_AddJahezWorkflowV2'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015072958_AddJahezWorkflowV2', N'10.0.11');
END;
GO

