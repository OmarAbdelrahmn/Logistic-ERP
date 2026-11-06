BEGIN TRANSACTION;
ALTER TABLE [hunger].[SettlementDocuments] ADD [ClosingSnapshotId] uniqueidentifier NULL;

ALTER TABLE [hunger].[SettlementDocuments] ADD [ReconciliationEvidence] nvarchar(1000) NULL;

ALTER TABLE [hunger].[SettlementDocuments] ADD [SourceDocumentType] nvarchar(64) NULL;

ALTER TABLE [hunger].[SettlementDocuments] ADD [SourceReference] nvarchar(200) NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [ArrivalEvidence] nvarchar(1000) NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [CompletionDate] date NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [CompletionHash] nvarchar(64) NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [CompletionKey] nvarchar(150) NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [DestinationOwnerId] uniqueidentifier NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [DestinationType] nvarchar(32) NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [SourceWalletBalance] decimal(18,2) NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [Status] nvarchar(32) NOT NULL DEFAULT N'Posted';

ALTER TABLE [hunger].[FinancialEvents] ADD [TransferDate] date NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [TransferReference] nvarchar(200) NULL;

EXEC(N'UPDATE [hunger].[FinancialEvents] SET [Status] = ''Arrived'' WHERE [Kind] IN (''WithdrawalCompany'', ''WithdrawalSupervisor'');');

EXEC(N'UPDATE [hunger].[SettlementDocuments] SET [Status] = ''Unmatched'' WHERE [Kind] = ''Adjustment'';');

CREATE INDEX [IX_SettlementDocuments_ClosingSnapshotId] ON [hunger].[SettlementDocuments] ([ClosingSnapshotId]);

CREATE UNIQUE INDEX [IX_SettlementDocuments_PlatformRiderAccountId_InvoicePeriod_SourceDocumentType_SourceReference] ON [hunger].[SettlementDocuments] ([PlatformRiderAccountId], [InvoicePeriod], [SourceDocumentType], [SourceReference]) WHERE [Kind]='Adjustment' AND [PlatformRiderAccountId] IS NOT NULL AND [SourceDocumentType] IS NOT NULL AND [SourceReference] IS NOT NULL;

ALTER TABLE [hunger].[SettlementDocuments] ADD CONSTRAINT [FK_SettlementDocuments_SettlementDocuments_ClosingSnapshotId] FOREIGN KEY ([ClosingSnapshotId]) REFERENCES [hunger].[SettlementDocuments] ([Id]) ON DELETE NO ACTION;

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261105145423_HungerWalletV21ReconciliationAndWithdrawalArrival', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [hunger].[ReceiptAllocations] (
    [Id] uniqueidentifier NOT NULL,
    [ConfirmationEventId] uniqueidentifier NOT NULL,
    [RemittanceLineId] uniqueidentifier NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_ReceiptAllocations] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_HungerReceiptAllocation_Amount] CHECK ([Amount]>0),
    CONSTRAINT [FK_ReceiptAllocations_FinancialEvents_ConfirmationEventId] FOREIGN KEY ([ConfirmationEventId]) REFERENCES [hunger].[FinancialEvents] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReceiptAllocations_RemittanceLines_RemittanceLineId] FOREIGN KEY ([RemittanceLineId]) REFERENCES [hunger].[RemittanceLines] ([Id]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
VALUES ('019c18d5-62e1-7000-a000-000000000371', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مالية مستقلة لهنقر.', N'Independent Hunger finance action: FinanceRead.', 371, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.read', N'عرض مالية هنقر', N'Read Hunger finance', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000372', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مالية مستقلة لهنقر.', N'Independent Hunger finance action: FundingRequest.', 372, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.funding.request', N'طلب تمويل عهدة هنقر', N'Request Hunger custody funding', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000373', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مالية مستقلة لهنقر.', N'Independent Hunger finance action: FundingExecute.', 373, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.funding.execute', N'تنفيذ تمويل عهدة هنقر', N'Execute Hunger custody funding', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000374', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مالية مستقلة لهنقر.', N'Independent Hunger finance action: TopUpRequest.', 374, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.topup.request', N'طلب شحن محفظة هنقر', N'Request Hunger wallet top-up', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000375', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مالية مستقلة لهنقر.', N'Independent Hunger finance action: TopUpExecute.', 375, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.topup.execute', N'تنفيذ شحن محفظة هنقر', N'Execute Hunger wallet top-up', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000376', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مالية مستقلة لهنقر.', N'Independent Hunger finance action: Collection.', 376, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.collection', N'تحصيل مستحقات هنقر', N'Collect Hunger rider payments', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000377', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مالية مستقلة لهنقر.', N'Independent Hunger finance action: WithdrawalRequest.', 377, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.withdrawal.request', N'طلب سحب محفظة هنقر', N'Request Hunger wallet withdrawal', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000378', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مالية مستقلة لهنقر.', N'Independent Hunger finance action: WithdrawalArrivalConfirm.', 378, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.withdrawal.arrival.confirm', N'تأكيد وصول سحب هنقر', N'Confirm Hunger withdrawal arrival', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000379', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مالية مستقلة لهنقر.', N'Independent Hunger finance action: RiderPayment.', 379, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.rider.payment', N'صرف مستحقات مندوب هنقر', N'Pay Hunger rider entitlement', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000380', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مالية مستقلة لهنقر.', N'Independent Hunger finance action: WalletReview.', 380, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.wallet.review', N'مراجعة محفظة هنقر', N'Review Hunger wallet', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000381', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مالية مستقلة لهنقر.', N'Independent Hunger finance action: RemittancesSubmit.', 381, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.remittances.submit', N'تقديم توريد هنقر', N'Submit Hunger remittance', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000382', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مالية مستقلة لهنقر.', N'Independent Hunger finance action: ReceiptsCancelReverse.', 382, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.remittances.cancel_reverse', N'إلغاء وعكس توريد هنقر', N'Cancel or reverse Hunger receipt', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;

CREATE UNIQUE INDEX [IX_ReceiptAllocations_ConfirmationEventId_RemittanceLineId] ON [hunger].[ReceiptAllocations] ([ConfirmationEventId], [RemittanceLineId]);

CREATE INDEX [IX_ReceiptAllocations_RemittanceLineId] ON [hunger].[ReceiptAllocations] ([RemittanceLineId]);

DECLARE @confirmation uniqueidentifier, @batch uniqueidentifier, @details nvarchar(max),
    @created datetimeoffset, @actor uniqueidentifier, @amount decimal(18,2);
DECLARE confirmations CURSOR LOCAL FAST_FORWARD FOR
    SELECT [Id], [RelatedEventId], [DetailsJson], [CreatedAtUtc], [CreatedByUserId], [Amount]
    FROM [hunger].[FinancialEvents] WHERE [Kind] = N'RemittanceConfirmation'
    ORDER BY [CreatedAtUtc], [Id];
OPEN confirmations;
FETCH NEXT FROM confirmations INTO @confirmation, @batch, @details, @created, @actor, @amount;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF JSON_QUERY(@details, '$.sources') IS NOT NULL
        INSERT INTO [hunger].[ReceiptAllocations]
            ([Id], [ConfirmationEventId], [RemittanceLineId], [Amount], [CreatedAtUtc], [CreatedByUserId])
        SELECT NEWID(), @confirmation, l.[Id], j.[Amount], @created, @actor
        FROM OPENJSON(@details, '$.sources') WITH
            ([SourceEventId] uniqueidentifier '$.sourceEventId', [Amount] decimal(18,2) '$.amount') j
        JOIN [hunger].[RemittanceLines] l ON l.[RemittanceEventId] = @batch AND l.[SourceEventId] = j.[SourceEventId];
    ELSE
        INSERT INTO [hunger].[ReceiptAllocations]
            ([Id], [ConfirmationEventId], [RemittanceLineId], [Amount], [CreatedAtUtc], [CreatedByUserId])
        SELECT NEWID(), @confirmation, l.[Id], l.[Amount] - COALESCE(a.[Allocated], 0), @created, @actor
        FROM [hunger].[RemittanceLines] l
        OUTER APPLY (SELECT SUM(r.[Amount]) AS [Allocated] FROM [hunger].[ReceiptAllocations] r WHERE r.[RemittanceLineId] = l.[Id]) a
        WHERE l.[RemittanceEventId] = @batch AND l.[Amount] > COALESCE(a.[Allocated], 0);
    IF COALESCE((SELECT SUM([Amount]) FROM [hunger].[ReceiptAllocations] WHERE [ConfirmationEventId] = @confirmation), 0) <> @amount
        THROW 51000, 'Historical Hunger receipt source allocations do not reconcile. Migration rolled back.', 1;
    FETCH NEXT FROM confirmations INTO @confirmation, @batch, @details, @created, @actor, @amount;
END;
CLOSE confirmations;
DEALLOCATE confirmations;
IF EXISTS (
    SELECT 1 FROM [hunger].[RemittanceLines] l
    OUTER APPLY (SELECT SUM(r.[Amount]) AS [Allocated] FROM [hunger].[ReceiptAllocations] r WHERE r.[RemittanceLineId] = l.[Id]) a
    WHERE COALESCE(a.[Allocated], 0) <> l.[ConfirmedAmount])
    THROW 51000, 'Historical Hunger receipt line totals do not reconcile. Migration rolled back.', 1;
UPDATE b SET [Status] = CASE WHEN COALESCE(a.[Confirmed], 0) = b.[Amount] THEN N'Confirmed'
    WHEN COALESCE(a.[Confirmed], 0) > 0 THEN N'PartiallyConfirmed' ELSE N'Submitted' END
FROM [hunger].[FinancialEvents] b
OUTER APPLY (SELECT SUM(l.[ConfirmedAmount]) AS [Confirmed] FROM [hunger].[RemittanceLines] l WHERE l.[RemittanceEventId] = b.[Id]) a
WHERE b.[Kind] = N'Remittance' AND b.[Status] = N'Posted';

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106083232_HungerReceiptLifecycleAndFinancePermissions', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_WalletReviews_PlatformRiderAccountId] ON [hunger].[WalletReviews];

ALTER TABLE [hunger].[WalletReviews] ADD [Status] nvarchar(20) NOT NULL DEFAULT N'Pending';

ALTER TABLE [hunger].[WalletReviews] ADD [SwapId] uniqueidentifier NULL;

UPDATE [hunger].[WalletReviews] SET [Status] = CASE WHEN [ResultJson] IS NOT NULL THEN 'Confirmed' ELSE 'Superseded' END;

CREATE INDEX [IX_WalletReviews_PlatformRiderAccountId_Status] ON [hunger].[WalletReviews] ([PlatformRiderAccountId], [Status]);

CREATE INDEX [IX_WalletReviews_SwapId] ON [hunger].[WalletReviews] ([SwapId]);

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106090616_HungerWalletReviewIntegrityAndRiderSwap', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
VALUES ('019c18d5-62e1-7000-a000-000000000383', N'Fleet', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'تعديل هوية المركبة الأساسية.', N'Change base vehicle identity.', 383, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'fleet.vehicles.fields.identity', N'هوية المركبة', N'Vehicle identity', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106093923_GrantFleetVehicleIdentityFieldPermissions', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[hunger].[DailyOperations]') AND [c].[name] = N'ActualWorkingHours');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [hunger].[DailyOperations] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [hunger].[DailyOperations] ALTER COLUMN [ActualWorkingHours] decimal(18,8) NULL;

ALTER TABLE [hunger].[DailyOperations] ADD [AcceptedOrders] int NULL;

ALTER TABLE [hunger].[DailyOperations] ADD [HoursUnit] nvarchar(32) NOT NULL DEFAULT N'DecimalHours';

ALTER TABLE [hunger].[DailyOperations] ADD [StackedOrders] int NULL;

ALTER TABLE [hunger].[AlertRules] ADD [EffectiveThrough] date NULL;

ALTER TABLE [hunger].[AlertRules] ADD [Kind] nvarchar(16) NOT NULL DEFAULT N'Alert';

ALTER TABLE [hunger].[AlertRules] ADD [Metric] nvarchar(50) NOT NULL DEFAULT N'WorkingHours';

ALTER TABLE [hunger].[AlertRules] ADD [OperatingCityId] uniqueidentifier NULL;

ALTER TABLE [hunger].[AlertRules] ADD [Operator] nvarchar(32) NOT NULL DEFAULT N'LessThan';

ALTER TABLE [hunger].[AlertRules] ADD [Period] nvarchar(16) NOT NULL DEFAULT N'Day';

CREATE TABLE [hunger].[PotentialFines] (
    [Id] uniqueidentifier NOT NULL,
    [DailyOperationId] uniqueidentifier NOT NULL,
    [NoShowCount] int NOT NULL,
    [Status] nvarchar(32) NOT NULL,
    [InvoiceDocumentId] uniqueidentifier NULL,
    [InvoiceSourceKey] nvarchar(200) NULL,
    [ResolutionEvidence] nvarchar(1000) NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAtUtc] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAtUtc] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [DeletionReason] nvarchar(500) NULL,
    CONSTRAINT [PK_PotentialFines] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_HungerPotentialFine_Status] CHECK ([NoShowCount]>0 AND [Status] IN ('Potential','Invoiced','Waived')),
    CONSTRAINT [FK_PotentialFines_DailyOperations_DailyOperationId] FOREIGN KEY ([DailyOperationId]) REFERENCES [hunger].[DailyOperations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PotentialFines_SettlementDocuments_InvoiceDocumentId] FOREIGN KEY ([InvoiceDocumentId]) REFERENCES [hunger].[SettlementDocuments] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AlertRules_OperatingCityId] ON [hunger].[AlertRules] ([OperatingCityId]);

CREATE UNIQUE INDEX [IX_PotentialFines_DailyOperationId] ON [hunger].[PotentialFines] ([DailyOperationId]) WHERE [IsDeleted] = 0;

CREATE INDEX [IX_PotentialFines_InvoiceDocumentId] ON [hunger].[PotentialFines] ([InvoiceDocumentId]);

CREATE INDEX [IX_PotentialFines_IsDeleted] ON [hunger].[PotentialFines] ([IsDeleted]);

ALTER TABLE [hunger].[AlertRules] ADD CONSTRAINT [FK_AlertRules_OperatingCities_OperatingCityId] FOREIGN KEY ([OperatingCityId]) REFERENCES [app].[OperatingCities] ([Id]) ON DELETE NO ACTION;

UPDATE [hunger].[AlertRules]
SET [Metric] = 'NoShowCount', [Operator] = 'GreaterThan'
WHERE [Code] = 'NoShowPositive';
INSERT INTO [hunger].[PotentialFines]
    ([Id], [DailyOperationId], [NoShowCount], [Status], [CreatedAtUtc], [CreatedByUserId], [IsDeleted])
SELECT NEWID(), [Id], [NoShowCount], 'Potential', [CreatedAtUtc], [CreatedByUserId], 0
FROM [hunger].[DailyOperations]
WHERE [IsDeleted] = 0 AND [NoShowCount] > 0;

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106131847_HungerPerformanceCatalogAndPotentialFines', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [hunger].[DailyOperations] ADD [CaptainClassification] nvarchar(100) NULL;

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106143740_HungerSourceCaptainClassification', N'10.0.11');

COMMIT;
GO

