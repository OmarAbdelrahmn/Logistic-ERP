BEGIN TRANSACTION;
IF SCHEMA_ID(N'hunger') IS NULL EXEC(N'CREATE SCHEMA [hunger];');

ALTER TABLE [app].[RiderClientAssignments] ADD [CompensationModel] nvarchar(150) NULL;

ALTER TABLE [jahez_v3].[Postings] ADD [ClientPlatformId] uniqueidentifier NULL;

ALTER TABLE [jahez_v3].[Postings] ADD [SourceEventId] uniqueidentifier NULL;

ALTER TABLE [jahez_v3].[Postings] ADD [SupervisorEmployeeId] uniqueidentifier NULL;

ALTER TABLE [app].[PlatformAccountDayUsages] ADD [QualityCompletedOrders] int NULL;

ALTER TABLE [app].[PlatformAccountDayUsages] ADD [QualityCompletedOrdersInTime] int NULL;

ALTER TABLE [app].[PlatformAccountDayUsages] ADD [QualityFailedOrders] int NULL;

ALTER TABLE [app].[PlatformAccountDayUsages] ADD [QualityGrossOrders] int NULL;

ALTER TABLE [app].[PlatformAccountDayUsages] ADD [QualitySuccessfulVerifications] int NULL;

ALTER TABLE [app].[PlatformAccountDayUsages] ADD [QualityVerificationRequests] int NULL;

ALTER TABLE [app].[PhoneSimCards] ADD [Ownership] nvarchar(16) NOT NULL DEFAULT N'Unknown';

ALTER TABLE [app].[PhoneSimCards] ADD [OwnershipEffectiveFrom] date NULL;

ALTER TABLE [app].[PhoneSimCards] ADD [OwnershipEvidence] nvarchar(1000) NULL;

CREATE TABLE [hunger].[AlertRules] (
    [Id] uniqueidentifier NOT NULL,
    [ClientPlatformId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Threshold] decimal(18,8) NOT NULL,
    [Severity] nvarchar(32) NOT NULL,
    [EffectiveFrom] date NOT NULL,
    [Enabled] bit NOT NULL,
    [EvidenceReference] nvarchar(1000) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_AlertRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AlertRules_ClientPlatforms_ClientPlatformId] FOREIGN KEY ([ClientPlatformId]) REFERENCES [platform].[ClientPlatforms] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [hunger].[FinanceConfigurations] (
    [Id] uniqueidentifier NOT NULL,
    [ClientPlatformId] uniqueidentifier NOT NULL,
    [SupervisorEmployeeId] uniqueidentifier NOT NULL,
    [SponsorId] uniqueidentifier NOT NULL,
    [CustodySubAccountId] uniqueidentifier NOT NULL,
    [CollectionsSubAccountId] uniqueidentifier NOT NULL,
    [WalletSubAccountId] uniqueidentifier NOT NULL,
    [RiderClearingSubAccountId] uniqueidentifier NOT NULL,
    [EvidenceReference] nvarchar(1000) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAtUtc] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAtUtc] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [DeletionReason] nvarchar(500) NULL,
    CONSTRAINT [PK_FinanceConfigurations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FinanceConfigurations_ClientPlatforms_ClientPlatformId] FOREIGN KEY ([ClientPlatformId]) REFERENCES [platform].[ClientPlatforms] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinanceConfigurations_Employees_SupervisorEmployeeId] FOREIGN KEY ([SupervisorEmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinanceConfigurations_Sponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinanceConfigurations_TreasurySubAccounts_CollectionsSubAccountId] FOREIGN KEY ([CollectionsSubAccountId]) REFERENCES [jahez_v3].[TreasurySubAccounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinanceConfigurations_TreasurySubAccounts_CustodySubAccountId] FOREIGN KEY ([CustodySubAccountId]) REFERENCES [jahez_v3].[TreasurySubAccounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinanceConfigurations_TreasurySubAccounts_RiderClearingSubAccountId] FOREIGN KEY ([RiderClearingSubAccountId]) REFERENCES [jahez_v3].[TreasurySubAccounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinanceConfigurations_TreasurySubAccounts_WalletSubAccountId] FOREIGN KEY ([WalletSubAccountId]) REFERENCES [jahez_v3].[TreasurySubAccounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [hunger].[FinancialEvents] (
    [Id] uniqueidentifier NOT NULL,
    [ClientPlatformId] uniqueidentifier NOT NULL,
    [SupervisorEmployeeId] uniqueidentifier NOT NULL,
    [PlatformRiderAccountId] uniqueidentifier NULL,
    [RiderProfileId] uniqueidentifier NULL,
    [RelatedEventId] uniqueidentifier NULL,
    [FromSubAccountId] uniqueidentifier NULL,
    [ToSubAccountId] uniqueidentifier NULL,
    [Kind] nvarchar(40) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [RiderBalanceChange] decimal(18,2) NOT NULL,
    [BusinessDate] date NOT NULL,
    [IdempotencyKey] nvarchar(150) NOT NULL,
    [RequestHash] nvarchar(64) NOT NULL,
    [EvidenceReference] nvarchar(1000) NOT NULL,
    [DetailsJson] nvarchar(max) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_FinancialEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FinancialEvents_ClientPlatforms_ClientPlatformId] FOREIGN KEY ([ClientPlatformId]) REFERENCES [platform].[ClientPlatforms] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinancialEvents_Employees_SupervisorEmployeeId] FOREIGN KEY ([SupervisorEmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinancialEvents_FinancialEvents_RelatedEventId] FOREIGN KEY ([RelatedEventId]) REFERENCES [hunger].[FinancialEvents] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinancialEvents_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinancialEvents_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinancialEvents_TreasurySubAccounts_FromSubAccountId] FOREIGN KEY ([FromSubAccountId]) REFERENCES [jahez_v3].[TreasurySubAccounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinancialEvents_TreasurySubAccounts_ToSubAccountId] FOREIGN KEY ([ToSubAccountId]) REFERENCES [jahez_v3].[TreasurySubAccounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [hunger].[ImportPreviews] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Kind] nvarchar(32) NOT NULL,
    [ReportDate] date NOT NULL,
    [FileName] nvarchar(255) NOT NULL,
    [ContentHash] nvarchar(64) NOT NULL,
    [InputRevision] nvarchar(64) NOT NULL,
    [RowsJson] nvarchar(max) NOT NULL,
    [ResultJson] nvarchar(max) NULL,
    [ExpiresAtUtc] datetimeoffset NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAtUtc] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAtUtc] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [DeletionReason] nvarchar(500) NULL,
    CONSTRAINT [PK_ImportPreviews] PRIMARY KEY ([Id])
);

CREATE TABLE [app].[PhoneSimOwnershipRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [PhoneSimCardId] uniqueidentifier NOT NULL,
    [Ownership] nvarchar(16) NOT NULL,
    [EffectiveFrom] date NOT NULL,
    [EvidenceReference] nvarchar(1000) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_PhoneSimOwnershipRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PhoneSimOwnershipRevisions_PhoneSimCards_PhoneSimCardId] FOREIGN KEY ([PhoneSimCardId]) REFERENCES [app].[PhoneSimCards] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [app].[PlatformAccountBusinessDetails] (
    [Id] uniqueidentifier NOT NULL,
    [PlatformRiderAccountId] uniqueidentifier NOT NULL,
    [PhoneNumber] nvarchar(32) NULL,
    [RegistryIdentifier] nvarchar(150) NULL,
    [SettlementModel] nvarchar(150) NULL,
    [RegisteredVehicleType] nvarchar(32) NULL,
    [RegisteredVehiclePlate] nvarchar(32) NULL,
    [RegisteredVehicleSerial] nvarchar(100) NULL,
    [OperationalDisposition] nvarchar(32) NOT NULL,
    [DispositionReason] nvarchar(2000) NULL,
    [DeferredUntil] date NULL,
    [SimOwnership] nvarchar(32) NULL,
    [SourceStatus] nvarchar(150) NULL,
    [SourceActualRiderProfileId] uniqueidentifier NULL,
    [SourceActualIqama] nvarchar(50) NULL,
    [SourceCompensationModel] nvarchar(150) NULL,
    [SourceVehicleDescription] nvarchar(150) NULL,
    [SourceAssignmentFrom] date NULL,
    [SourceOpeningWallet] decimal(18,2) NULL,
    [SourceOpeningOrders] int NULL,
    [SourceReference] nvarchar(500) NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAtUtc] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAtUtc] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [DeletionReason] nvarchar(500) NULL,
    CONSTRAINT [PK_PlatformAccountBusinessDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_PlatformBusiness_Disposition] CHECK ([OperationalDisposition] IN ('Normal','Problem','AdministrativeStop','Blocked','DeferredNextMonth')),
    CONSTRAINT [FK_PlatformAccountBusinessDetails_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PlatformAccountBusinessDetails_RiderProfiles_SourceActualRiderProfileId] FOREIGN KEY ([SourceActualRiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [app].[PlatformAccountBusinessRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [PlatformRiderAccountId] uniqueidentifier NOT NULL,
    [EffectiveFrom] date NOT NULL,
    [Reason] nvarchar(2000) NOT NULL,
    [BeforeJson] nvarchar(max) NOT NULL,
    [AfterJson] nvarchar(max) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_PlatformAccountBusinessRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PlatformAccountBusinessRevisions_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [hunger].[SettlementDocuments] (
    [Id] uniqueidentifier NOT NULL,
    [ClientPlatformId] uniqueidentifier NOT NULL,
    [PlatformRiderAccountId] uniqueidentifier NULL,
    [InvoiceDocumentId] uniqueidentifier NULL,
    [Kind] nvarchar(32) NOT NULL,
    [InvoicePeriod] date NOT NULL,
    [ChargeFrom] date NOT NULL,
    [ChargeThrough] date NOT NULL,
    [ObservedOn] date NOT NULL,
    [WalletBalance] decimal(18,2) NULL,
    [CompanyControlTotal] decimal(18,2) NULL,
    [CanonicalGrossTotal] decimal(18,2) NULL,
    [Status] nvarchar(32) NOT NULL,
    [IdempotencyKey] nvarchar(150) NOT NULL,
    [RequestHash] nvarchar(64) NOT NULL,
    [EvidenceReference] nvarchar(1000) NOT NULL,
    [DataJson] nvarchar(max) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_SettlementDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SettlementDocuments_ClientPlatforms_ClientPlatformId] FOREIGN KEY ([ClientPlatformId]) REFERENCES [platform].[ClientPlatforms] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SettlementDocuments_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SettlementDocuments_SettlementDocuments_InvoiceDocumentId] FOREIGN KEY ([InvoiceDocumentId]) REFERENCES [hunger].[SettlementDocuments] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [hunger].[WalletReviews] (
    [Id] uniqueidentifier NOT NULL,
    [PlatformRiderAccountId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [InputRevision] nvarchar(64) NOT NULL,
    [RequestJson] nvarchar(max) NOT NULL,
    [ResultJson] nvarchar(max) NULL,
    [ExpiresAtUtc] datetimeoffset NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAtUtc] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAtUtc] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [DeletionReason] nvarchar(500) NULL,
    CONSTRAINT [PK_WalletReviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WalletReviews_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [hunger].[RemittanceLines] (
    [Id] uniqueidentifier NOT NULL,
    [RemittanceEventId] uniqueidentifier NOT NULL,
    [SourceEventId] uniqueidentifier NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [ConfirmedAmount] decimal(18,2) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAtUtc] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAtUtc] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [DeletionReason] nvarchar(500) NULL,
    CONSTRAINT [PK_RemittanceLines] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_HungerRemittance_Amounts] CHECK ([Amount]>0 AND [ConfirmedAmount]>=0 AND [ConfirmedAmount]<=[Amount]),
    CONSTRAINT [FK_RemittanceLines_FinancialEvents_RemittanceEventId] FOREIGN KEY ([RemittanceEventId]) REFERENCES [hunger].[FinancialEvents] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RemittanceLines_FinancialEvents_SourceEventId] FOREIGN KEY ([SourceEventId]) REFERENCES [hunger].[FinancialEvents] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [hunger].[DailyOperations] (
    [Id] uniqueidentifier NOT NULL,
    [PlatformRiderAccountId] uniqueidentifier NOT NULL,
    [ImportId] uniqueidentifier NOT NULL,
    [ReportDate] date NOT NULL,
    [SourceRowNumber] int NOT NULL,
    [CompletedOrders] int NOT NULL,
    [ActualWorkingHours] decimal(18,8) NOT NULL,
    [RejectedOrders] int NULL,
    [CancelledOrders] int NULL,
    [NoShowCount] int NULL,
    [ManualOrders] int NULL,
    [ManualHours] decimal(18,8) NULL,
    [ManualReason] nvarchar(2000) NULL,
    [AllocationsJson] nvarchar(max) NOT NULL,
    [AllocationStatus] nvarchar(32) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAtUtc] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAtUtc] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [DeletionReason] nvarchar(500) NULL,
    CONSTRAINT [PK_DailyOperations] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_HungerDaily_Values] CHECK ([CompletedOrders]>=0 AND [ActualWorkingHours] BETWEEN 0 AND 24 AND ([RejectedOrders] IS NULL OR [RejectedOrders]>=0) AND ([CancelledOrders] IS NULL OR [CancelledOrders]>=0) AND ([NoShowCount] IS NULL OR [NoShowCount]>=0) AND ([ManualOrders] IS NULL OR [ManualOrders]>=0) AND ([ManualHours] IS NULL OR [ManualHours] BETWEEN 0 AND 24)),
    CONSTRAINT [FK_DailyOperations_ImportPreviews_ImportId] FOREIGN KEY ([ImportId]) REFERENCES [hunger].[ImportPreviews] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DailyOperations_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [hunger].[QualitySnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [PlatformRiderAccountId] uniqueidentifier NOT NULL,
    [ImportId] uniqueidentifier NOT NULL,
    [ReportDate] date NOT NULL,
    [CoverageFrom] date NOT NULL,
    [SourceRowNumber] int NOT NULL,
    [CountersJson] nvarchar(max) NOT NULL,
    [DailyCountersJson] nvarchar(max) NOT NULL,
    [Segment] nvarchar(50) NOT NULL,
    [VerificationSuccessRate] decimal(9,6) NOT NULL,
    [OnTimeDeliveryScore] decimal(9,6) NOT NULL,
    [FinalDeliveryQualityScore] decimal(9,6) NOT NULL,
    [AllocationStatus] nvarchar(32) NOT NULL,
    [SoleRiderProfileId] uniqueidentifier NULL,
    [AllocationsJson] nvarchar(max) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAtUtc] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAtUtc] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [DeletionReason] nvarchar(500) NULL,
    CONSTRAINT [PK_QualitySnapshots] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_HungerQuality_Rates] CHECK ([VerificationSuccessRate] BETWEEN 0 AND 1 AND [OnTimeDeliveryScore] BETWEEN 0 AND 1 AND [FinalDeliveryQualityScore] BETWEEN 0 AND 1),
    CONSTRAINT [FK_QualitySnapshots_ImportPreviews_ImportId] FOREIGN KEY ([ImportId]) REFERENCES [hunger].[ImportPreviews] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QualitySnapshots_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QualitySnapshots_RiderProfiles_SoleRiderProfileId] FOREIGN KEY ([SoleRiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [hunger].[QualityRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [SnapshotId] uniqueidentifier NOT NULL,
    [TriggerImportId] uniqueidentifier NOT NULL,
    [BeforeJson] nvarchar(max) NOT NULL,
    [AfterJson] nvarchar(max) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_QualityRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QualityRevisions_ImportPreviews_TriggerImportId] FOREIGN KEY ([TriggerImportId]) REFERENCES [hunger].[ImportPreviews] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QualityRevisions_QualitySnapshots_SnapshotId] FOREIGN KEY ([SnapshotId]) REFERENCES [hunger].[QualitySnapshots] ([Id]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
VALUES ('019c18d5-62e1-7000-a000-000000000350', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'عرض حسابات وتقارير هنقر.', N'Read Hunger accounts and reports.', 350, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.read', N'عرض هنقرستيشن', N'Read Hunger', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000351', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'معاينة واعتماد الجودة التراكمية.', N'Preview and confirm cumulative quality.', 351, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.quality.import', N'استيراد جودة هنقر', N'Import Hunger quality', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000352', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'تصدير الجودة المسموح بها.', N'Export authorized quality.', 352, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.quality.export', N'تصدير جودة هنقر', N'Export Hunger quality', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000353', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'مطابقة واستيراد الحسابات وأصحابها.', N'Match and import accounts and owners.', 353, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.accounts.import', N'استيراد حسابات هنقر', N'Import Hunger accounts', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000354', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'تعديل بيانات وحالة هنقر.', N'Update Hunger account details and disposition.', 354, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.accounts.update', N'تعديل حسابات هنقر', N'Update Hunger accounts', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000355', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'استيراد التشغيل اليومي.', N'Import daily operational performance.', 355, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.performance.import', N'استيراد تشغيل هنقر', N'Import Hunger operations', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000356', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'مراجعة واعتماد عمليات محافظ هنقر.', N'Review and confirm Hunger wallet operations.', 356, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.manage', N'إدارة مالية هنقر', N'Manage Hunger finance', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000357', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'تأكيد استلام التوريد الفعلي.', N'Confirm actual remittance receipts.', 357, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.remittances.confirm', N'تأكيد توريد هنقر', N'Confirm Hunger remittance', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000359', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'اعتماد الربط المحاسبي الصريح للمشرف.', N'Approve explicit supervisor accounting mappings.', 359, N'HIGH_TRUST_ONLY', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N'hunger.finance.configure', N'ربط حسابات هنقر المحاسبية', N'Configure Hunger finance', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000360', N'PlatformFields', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مستقلة: هوية الحساب.', N'Independent Identity field access.', 360, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'platform_accounts.fields.identity', N'هوية الحساب', N'Identity field access', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000361', N'PlatformFields', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مستقلة: صاحب الحساب.', N'Independent Owner field access.', 361, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'platform_accounts.fields.owner', N'صاحب الحساب', N'Owner field access', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000362', N'PlatformFields', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مستقلة: مدينة الحساب.', N'Independent City field access.', 362, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'platform_accounts.fields.city', N'مدينة الحساب', N'City field access', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000363', N'PlatformFields', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مستقلة: سجل الحساب.', N'Independent Registry field access.', 363, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'platform_accounts.fields.registry', N'سجل الحساب', N'Registry field access', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000364', N'PlatformFields', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مستقلة: هاتف وشريحة الحساب.', N'Independent Contact field access.', 364, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'platform_accounts.fields.contact', N'هاتف وشريحة الحساب', N'Contact field access', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000365', N'PlatformFields', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مستقلة: المركبة المسجلة.', N'Independent Vehicle field access.', 365, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'platform_accounts.fields.registered_vehicle', N'المركبة المسجلة', N'Vehicle field access', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000366', N'PlatformFields', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مستقلة: نظام تسوية الحساب.', N'Independent Settlement field access.', 366, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'platform_accounts.fields.settlement', N'نظام تسوية الحساب', N'Settlement field access', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000367', N'PlatformFields', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مستقلة: محاسبة المندوب.', N'Independent Compensation field access.', 367, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'platform_assignments.fields.compensation', N'محاسبة المندوب', N'Compensation field access', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000368', N'PlatformFields', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'صلاحية مستقلة: قراءة كلمة مرور الحساب.', N'Independent Reveal field access.', 368, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'platform_accounts.credentials.reveal', N'قراءة كلمة مرور الحساب', N'Reveal field access', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000369', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'تصدير كامل النطاق المسموح للتشغيل.', N'Export all authorized operational rows.', 369, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.performance.export', N'تصدير تشغيل هنقر', N'Export Hunger operations', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000370', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'إضافة نسخ مؤرخة من قواعد المتابعة.', N'Add versioned monitoring rules.', 370, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.alerts.manage', N'إدارة تنبيهات هنقر', N'Manage Hunger alerts', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;

CREATE INDEX [IX_Postings_ClientPlatformId_SupervisorEmployeeId_SubAccountId] ON [jahez_v3].[Postings] ([ClientPlatformId], [SupervisorEmployeeId], [SubAccountId]);

CREATE UNIQUE INDEX [IX_Postings_SourceEventId_SubAccountId] ON [jahez_v3].[Postings] ([SourceEventId], [SubAccountId]) WHERE [SourceEventId] IS NOT NULL;

ALTER TABLE [app].[PhoneSimCards] ADD CONSTRAINT [CK_PhoneSim_Ownership] CHECK ([Ownership] IN ('Company','Private','Unknown'));

CREATE INDEX [IX_AlertRules_ClientPlatformId_Code_EffectiveFrom_CreatedAtUtc] ON [hunger].[AlertRules] ([ClientPlatformId], [Code], [EffectiveFrom], [CreatedAtUtc]);

CREATE INDEX [IX_DailyOperations_ImportId] ON [hunger].[DailyOperations] ([ImportId]);

CREATE INDEX [IX_DailyOperations_IsDeleted] ON [hunger].[DailyOperations] ([IsDeleted]);

CREATE UNIQUE INDEX [IX_DailyOperations_PlatformRiderAccountId_ReportDate] ON [hunger].[DailyOperations] ([PlatformRiderAccountId], [ReportDate]) WHERE [IsDeleted] = 0;

CREATE UNIQUE INDEX [IX_FinanceConfigurations_ClientPlatformId_SupervisorEmployeeId] ON [hunger].[FinanceConfigurations] ([ClientPlatformId], [SupervisorEmployeeId]) WHERE [IsDeleted] = 0;

CREATE INDEX [IX_FinanceConfigurations_CollectionsSubAccountId] ON [hunger].[FinanceConfigurations] ([CollectionsSubAccountId]);

CREATE INDEX [IX_FinanceConfigurations_CustodySubAccountId] ON [hunger].[FinanceConfigurations] ([CustodySubAccountId]);

CREATE INDEX [IX_FinanceConfigurations_IsDeleted] ON [hunger].[FinanceConfigurations] ([IsDeleted]);

CREATE INDEX [IX_FinanceConfigurations_RiderClearingSubAccountId] ON [hunger].[FinanceConfigurations] ([RiderClearingSubAccountId]);

CREATE INDEX [IX_FinanceConfigurations_SponsorId] ON [hunger].[FinanceConfigurations] ([SponsorId]);

CREATE INDEX [IX_FinanceConfigurations_SupervisorEmployeeId] ON [hunger].[FinanceConfigurations] ([SupervisorEmployeeId]);

CREATE INDEX [IX_FinanceConfigurations_WalletSubAccountId] ON [hunger].[FinanceConfigurations] ([WalletSubAccountId]);

CREATE UNIQUE INDEX [IX_FinancialEvents_ClientPlatformId_IdempotencyKey] ON [hunger].[FinancialEvents] ([ClientPlatformId], [IdempotencyKey]);

CREATE INDEX [IX_FinancialEvents_FromSubAccountId] ON [hunger].[FinancialEvents] ([FromSubAccountId]);

CREATE INDEX [IX_FinancialEvents_PlatformRiderAccountId_BusinessDate] ON [hunger].[FinancialEvents] ([PlatformRiderAccountId], [BusinessDate]);

CREATE INDEX [IX_FinancialEvents_RelatedEventId] ON [hunger].[FinancialEvents] ([RelatedEventId]);

CREATE INDEX [IX_FinancialEvents_RiderProfileId] ON [hunger].[FinancialEvents] ([RiderProfileId]);

CREATE INDEX [IX_FinancialEvents_SupervisorEmployeeId_BusinessDate] ON [hunger].[FinancialEvents] ([SupervisorEmployeeId], [BusinessDate]);

CREATE INDEX [IX_FinancialEvents_ToSubAccountId] ON [hunger].[FinancialEvents] ([ToSubAccountId]);

CREATE INDEX [IX_ImportPreviews_IsDeleted] ON [hunger].[ImportPreviews] ([IsDeleted]);

CREATE INDEX [IX_ImportPreviews_UserId_ExpiresAtUtc] ON [hunger].[ImportPreviews] ([UserId], [ExpiresAtUtc]);

CREATE INDEX [IX_PhoneSimOwnershipRevisions_PhoneSimCardId_EffectiveFrom] ON [app].[PhoneSimOwnershipRevisions] ([PhoneSimCardId], [EffectiveFrom]);

CREATE INDEX [IX_PlatformAccountBusinessDetails_IsDeleted] ON [app].[PlatformAccountBusinessDetails] ([IsDeleted]);

CREATE UNIQUE INDEX [IX_PlatformAccountBusinessDetails_PlatformRiderAccountId] ON [app].[PlatformAccountBusinessDetails] ([PlatformRiderAccountId]) WHERE [IsDeleted] = 0;

CREATE INDEX [IX_PlatformAccountBusinessDetails_SourceActualRiderProfileId] ON [app].[PlatformAccountBusinessDetails] ([SourceActualRiderProfileId]);

CREATE INDEX [IX_PlatformAccountBusinessRevisions_PlatformRiderAccountId_EffectiveFrom] ON [app].[PlatformAccountBusinessRevisions] ([PlatformRiderAccountId], [EffectiveFrom]);

CREATE INDEX [IX_QualityRevisions_SnapshotId] ON [hunger].[QualityRevisions] ([SnapshotId]);

CREATE INDEX [IX_QualityRevisions_TriggerImportId] ON [hunger].[QualityRevisions] ([TriggerImportId]);

CREATE INDEX [IX_QualitySnapshots_ImportId] ON [hunger].[QualitySnapshots] ([ImportId]);

CREATE INDEX [IX_QualitySnapshots_IsDeleted] ON [hunger].[QualitySnapshots] ([IsDeleted]);

CREATE UNIQUE INDEX [IX_QualitySnapshots_PlatformRiderAccountId_ReportDate] ON [hunger].[QualitySnapshots] ([PlatformRiderAccountId], [ReportDate]) WHERE [IsDeleted] = 0;

CREATE INDEX [IX_QualitySnapshots_SoleRiderProfileId] ON [hunger].[QualitySnapshots] ([SoleRiderProfileId]);

CREATE INDEX [IX_RemittanceLines_IsDeleted] ON [hunger].[RemittanceLines] ([IsDeleted]);

CREATE UNIQUE INDEX [IX_RemittanceLines_RemittanceEventId_SourceEventId] ON [hunger].[RemittanceLines] ([RemittanceEventId], [SourceEventId]);

CREATE INDEX [IX_RemittanceLines_SourceEventId] ON [hunger].[RemittanceLines] ([SourceEventId]);

CREATE UNIQUE INDEX [IX_SettlementDocuments_ClientPlatformId_IdempotencyKey] ON [hunger].[SettlementDocuments] ([ClientPlatformId], [IdempotencyKey]);

CREATE INDEX [IX_SettlementDocuments_InvoiceDocumentId] ON [hunger].[SettlementDocuments] ([InvoiceDocumentId]);

CREATE UNIQUE INDEX [IX_SettlementDocuments_PlatformRiderAccountId_Kind_ChargeFrom_ChargeThrough] ON [hunger].[SettlementDocuments] ([PlatformRiderAccountId], [Kind], [ChargeFrom], [ChargeThrough]) WHERE [Kind]='MonthClose' AND [PlatformRiderAccountId] IS NOT NULL;

CREATE INDEX [IX_WalletReviews_IsDeleted] ON [hunger].[WalletReviews] ([IsDeleted]);

CREATE INDEX [IX_WalletReviews_PlatformRiderAccountId] ON [hunger].[WalletReviews] ([PlatformRiderAccountId]);

CREATE INDEX [IX_WalletReviews_UserId_ExpiresAtUtc] ON [hunger].[WalletReviews] ([UserId], [ExpiresAtUtc]);

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261104115133_AddHungerOperationsQualityAndSharedAccountContext', N'10.0.11');

COMMIT;
GO

