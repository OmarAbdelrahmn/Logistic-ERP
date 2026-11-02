BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031112328_AddOilChangeConcurrency'
)
BEGIN
    ALTER TABLE [maintenance].[OilChangeOperations] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031112328_AddOilChangeConcurrency'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031112328_AddOilChangeConcurrency', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    IF EXISTS (SELECT ReversalOfUsageId FROM maintenance.MaterialUsages WHERE ReversalOfUsageId IS NOT NULL GROUP BY ReversalOfUsageId HAVING COUNT(*) > 1)
        THROW 51000, 'Duplicate material reversals require reconciliation before migration.', 1;
    IF EXISTS (SELECT ReversalOfAllocationId FROM maintenance.OilBarrelUsageAllocations WHERE ReversalOfAllocationId IS NOT NULL GROUP BY ReversalOfAllocationId HAVING COUNT(*) > 1)
        THROW 51000, 'Duplicate barrel reversals require reconciliation before migration.', 1;
    IF EXISTS (SELECT ReversalOfExpenseId FROM maintenance.VehicleExpenses WHERE ReversalOfExpenseId IS NOT NULL GROUP BY ReversalOfExpenseId HAVING COUNT(*) > 1)
        THROW 51000, 'Duplicate expense reversals require reconciliation before migration.', 1;
    IF EXISTS (SELECT ReversalOfEntryId FROM maintenance.ExternalFinancialEntries WHERE ReversalOfEntryId IS NOT NULL GROUP BY ReversalOfEntryId HAVING COUNT(*) > 1)
        THROW 51000, 'Duplicate workshop reversals require reconciliation before migration.', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    DROP INDEX [IX_VehicleExpenses_ReversalOfExpenseId] ON [maintenance].[VehicleExpenses];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    DROP INDEX [IX_OilBarrelUsageAllocations_ReversalOfAllocationId] ON [maintenance].[OilBarrelUsageAllocations];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    DROP INDEX [IX_MaterialUsages_ReversalOfUsageId] ON [maintenance].[MaterialUsages];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    DROP INDEX [IX_ExternalFinancialEntries_ReversalOfEntryId] ON [maintenance].[ExternalFinancialEntries];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    ALTER TABLE [app].[VehicleOdometerReadings] ADD [IsVoided] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    ALTER TABLE [app].[VehicleOdometerReadings] ADD [VoidedByRevisionId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    ALTER TABLE [maintenance].[OilChangeOperations] ADD [CancellationReason] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    ALTER TABLE [maintenance].[OilChangeOperations] ADD [CancelledAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    ALTER TABLE [maintenance].[OilChangeOperations] ADD [CancelledByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    ALTER TABLE [maintenance].[OilChangeOperations] ADD [IsCancelled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    ALTER TABLE [maintenance].[OilChangeOperations] ADD [RevisionNumber] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    ALTER TABLE [maintenance].[OilBarrelLosses] ADD [CorrectionRevisionId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    ALTER TABLE [maintenance].[MaterialUsages] ADD [EffectiveServiceAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    CREATE TABLE [maintenance].[OilChangeRevisions] (
        [Id] uniqueidentifier NOT NULL,
        [OilChangeOperationId] uniqueidentifier NOT NULL,
        [RevisionNumber] int NOT NULL,
        [Action] nvarchar(20) NOT NULL,
        [IdempotencyKey] nvarchar(200) NOT NULL,
        [RequestHash] nvarchar(64) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [PostedAtUtc] datetimeoffset NOT NULL,
        [OldOilUsageId] uniqueidentifier NOT NULL,
        [OldFilterUsageId] uniqueidentifier NULL,
        [BeforeJson] nvarchar(max) NOT NULL,
        [AfterJson] nvarchar(max) NOT NULL,
        [EffectsJson] nvarchar(max) NOT NULL,
        [ResultJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_OilChangeRevisions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OilChangeRevisions_Number] CHECK ([RevisionNumber] > 0),
        CONSTRAINT [FK_OilChangeRevisions_MaterialUsages_OldFilterUsageId] FOREIGN KEY ([OldFilterUsageId]) REFERENCES [maintenance].[MaterialUsages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OilChangeRevisions_MaterialUsages_OldOilUsageId] FOREIGN KEY ([OldOilUsageId]) REFERENCES [maintenance].[MaterialUsages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OilChangeRevisions_OilChangeOperations_OilChangeOperationId] FOREIGN KEY ([OilChangeOperationId]) REFERENCES [maintenance].[OilChangeOperations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000900'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل عملية زيت مكتملة مع تدقيق.'', N''Correct completed oil changes with audit.'', 900, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.oil.edit'', N''تعديل تغيير الزيت'', N''Edit oil changes'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000901'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تصحيح تاريخ خدمة الزيت.'', N''Correct oil service dates.'', 901, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.oil.date_edit'', N''تعديل تاريخ تغيير الزيت'', N''Edit oil-change dates'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000902'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إلغاء الزيت بعكس المخزون والتكاليف.'', N''Cancel oil changes with linked reversals.'', 902, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.oil.cancel'', N''إلغاء تغيير الزيت'', N''Cancel oil changes'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    CREATE INDEX [IX_VehicleOdometerReadings_VoidedByRevisionId] ON [app].[VehicleOdometerReadings] ([VoidedByRevisionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    EXEC(N'ALTER TABLE [app].[VehicleOdometerReadings] ADD CONSTRAINT [CK_VehicleOdometerReadings_Void] CHECK (([IsVoided] = 0 AND [VoidedByRevisionId] IS NULL) OR ([IsVoided] = 1 AND [VoidedByRevisionId] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VehicleExpenses_ReversalOfExpenseId] ON [maintenance].[VehicleExpenses] ([ReversalOfExpenseId]) WHERE [ReversalOfExpenseId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    EXEC(N'ALTER TABLE [maintenance].[OilChangeOperations] ADD CONSTRAINT [CK_OilChangeOperations_Lifecycle] CHECK ([RevisionNumber] >= 0 AND (([IsCancelled] = 0 AND [CancelledAtUtc] IS NULL AND [CancelledByUserId] IS NULL AND [CancellationReason] IS NULL) OR ([IsCancelled] = 1 AND [CancelledAtUtc] IS NOT NULL AND [CancelledByUserId] IS NOT NULL AND [CancellationReason] IS NOT NULL)))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OilBarrelUsageAllocations_ReversalOfAllocationId] ON [maintenance].[OilBarrelUsageAllocations] ([ReversalOfAllocationId]) WHERE [ReversalOfAllocationId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    CREATE INDEX [IX_OilBarrelLosses_CorrectionRevisionId] ON [maintenance].[OilBarrelLosses] ([CorrectionRevisionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MaterialUsages_ReversalOfUsageId] ON [maintenance].[MaterialUsages] ([ReversalOfUsageId]) WHERE [ReversalOfUsageId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ExternalFinancialEntries_ReversalOfEntryId] ON [maintenance].[ExternalFinancialEntries] ([ReversalOfEntryId]) WHERE [ReversalOfEntryId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OilChangeRevisions_OilChangeOperationId_IdempotencyKey] ON [maintenance].[OilChangeRevisions] ([OilChangeOperationId], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OilChangeRevisions_OilChangeOperationId_RevisionNumber] ON [maintenance].[OilChangeRevisions] ([OilChangeOperationId], [RevisionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    CREATE INDEX [IX_OilChangeRevisions_OldFilterUsageId] ON [maintenance].[OilChangeRevisions] ([OldFilterUsageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    CREATE INDEX [IX_OilChangeRevisions_OldOilUsageId] ON [maintenance].[OilChangeRevisions] ([OldOilUsageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    ALTER TABLE [maintenance].[OilBarrelLosses] ADD CONSTRAINT [FK_OilBarrelLosses_OilChangeRevisions_CorrectionRevisionId] FOREIGN KEY ([CorrectionRevisionId]) REFERENCES [maintenance].[OilChangeRevisions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    ALTER TABLE [app].[VehicleOdometerReadings] ADD CONSTRAINT [FK_VehicleOdometerReadings_OilChangeRevisions_VoidedByRevisionId] FOREIGN KEY ([VoidedByRevisionId]) REFERENCES [maintenance].[OilChangeRevisions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261031132700_AddOilChangeEditCancellationWorkflow'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031132700_AddOilChangeEditCancellationWorkflow', N'10.0.11');
END;

COMMIT;
GO

