BEGIN TRANSACTION;
ALTER TABLE [hunger].[FinancialEvents] ADD [ApprovalStatus] nvarchar(16) NOT NULL DEFAULT N'NotRequired';

ALTER TABLE [hunger].[FinancialEvents] ADD [ApprovedAtUtc] datetimeoffset NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [ApprovedByUserId] uniqueidentifier NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [DestinationChannel] nvarchar(16) NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [ExceptionApprovalId] uniqueidentifier NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [RejectionReason] nvarchar(1000) NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [Role] nvarchar(16) NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [SealAccountsJson] nvarchar(max) NULL;

ALTER TABLE [hunger].[FinancialEvents] ADD [SupersedesEventId] uniqueidentifier NULL;

CREATE TABLE [hunger].[EvidenceFiles] (
    [Id] uniqueidentifier NOT NULL,
    [ClientPlatformId] uniqueidentifier NOT NULL,
    [PlatformRiderAccountId] uniqueidentifier NOT NULL,
    [UploadedByUserId] uniqueidentifier NOT NULL,
    [StoragePath] nvarchar(500) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [Purpose] nvarchar(16) NOT NULL,
    [ObservedOn] date NOT NULL,
    [FinancialEventId] uniqueidentifier NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAtUtc] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAtUtc] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [DeletionReason] nvarchar(500) NULL,
    CONSTRAINT [PK_EvidenceFiles] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_HungerEvidenceFiles_Purpose] CHECK ([Purpose] IN ('WalletBefore','WalletAfter','Wallet','Arrival')),
    CONSTRAINT [FK_EvidenceFiles_ClientPlatforms_ClientPlatformId] FOREIGN KEY ([ClientPlatformId]) REFERENCES [platform].[ClientPlatforms] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EvidenceFiles_FinancialEvents_FinancialEventId] FOREIGN KEY ([FinancialEventId]) REFERENCES [hunger].[FinancialEvents] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EvidenceFiles_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [hunger].[ExceptionApprovals] (
    [Id] uniqueidentifier NOT NULL,
    [ClientPlatformId] uniqueidentifier NOT NULL,
    [PlatformRiderAccountId] uniqueidentifier NULL,
    [SupervisorEmployeeId] uniqueidentifier NULL,
    [Kind] nvarchar(32) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Reason] nvarchar(1000) NOT NULL,
    [EvidenceReference] nvarchar(1000) NULL,
    [Status] nvarchar(16) NOT NULL DEFAULT N'Requested',
    [RequestedByUserId] uniqueidentifier NOT NULL,
    [DecidedByUserId] uniqueidentifier NULL,
    [DecidedAtUtc] datetimeoffset NULL,
    [DecisionNote] nvarchar(1000) NULL,
    [ConsumedByEventId] uniqueidentifier NULL,
    [IdempotencyKey] nvarchar(150) NOT NULL,
    [RequestHash] nvarchar(64) NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAtUtc] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAtUtc] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [DeletionReason] nvarchar(500) NULL,
    CONSTRAINT [PK_ExceptionApprovals] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_HungerExceptionApprovals_Values] CHECK ([Kind] IN ('NonZeroHandover','CustodyIdentityMismatch') AND [Status] IN ('Requested','Approved','Rejected','Consumed') AND [Amount]<>0),
    CONSTRAINT [FK_ExceptionApprovals_ClientPlatforms_ClientPlatformId] FOREIGN KEY ([ClientPlatformId]) REFERENCES [platform].[ClientPlatforms] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ExceptionApprovals_Employees_SupervisorEmployeeId] FOREIGN KEY ([SupervisorEmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ExceptionApprovals_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
VALUES ('019c18d5-62e1-7000-a000-000000000384', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'اعتماد أو رفض المحاسب للعمليات المالية التي رحّلها المشرف.', N'Accountant seal or rejection of supervisor-posted finance events.', 384, N'HIGH_TRUST_ONLY', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N'hunger.finance.seal', N'اعتماد المحاسب لعمليات هنقر', N'Seal Hunger finance events', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000385', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'اعتماد أو تعديل أو رفض طلب تمويل العهدة قبل التسليم.', N'Approve, edit or reject a custody funding request before delivery.', 385, N'HIGH_TRUST_ONLY', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N'hunger.finance.funding.approve', N'اعتماد طلب عهدة هنقر', N'Approve Hunger custody funding', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000386', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'طلب استثناء تسليم برصيد غير صفري أو فرق تسوية عهدة.', N'Request an exception for a non-zero hand-over or a custody settlement mismatch.', 386, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.exception.request', N'طلب استثناء مالي لهنقر', N'Request Hunger finance exception', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000387', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'اعتماد أو رفض طلبات الاستثناء المالية.', N'Approve or reject finance exception requests.', 387, N'HIGH_TRUST_ONLY', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N'hunger.finance.exception.approve', N'اعتماد استثناء مالي لهنقر', N'Approve Hunger finance exception', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000388', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'تأكيد استلام نقد التحصيل في خزنة أو حساب تحصيلات المالك.', N'Confirm receipt of collected cash into the owner collections vault or account.', 388, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'hunger.finance.owner_collections.confirm', N'تأكيد استلام تحصيلات المالك', N'Confirm owner collections receipt', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;

CREATE INDEX [IX_FinancialEvents_ClientPlatformId_ApprovalStatus] ON [hunger].[FinancialEvents] ([ClientPlatformId], [ApprovalStatus]);

CREATE INDEX [IX_FinancialEvents_ExceptionApprovalId] ON [hunger].[FinancialEvents] ([ExceptionApprovalId]);

CREATE INDEX [IX_FinancialEvents_SupersedesEventId] ON [hunger].[FinancialEvents] ([SupersedesEventId]);

ALTER TABLE [hunger].[FinancialEvents] ADD CONSTRAINT [CK_HungerFinancialEvents_ApprovalStatus] CHECK ([ApprovalStatus] IN ('NotRequired','PendingSeal','Sealed','Rejected','Cancelled'));

UPDATE [hunger].[FinancialEvents]
SET [ApprovalStatus] = 'Sealed', [ApprovedAtUtc] = [CreatedAtUtc]
WHERE [Kind] IN ('TopUpAdvance','TopUpCollected','Collection','WithdrawalCompany','WithdrawalSupervisor','RiderPayment','WalletDelivery','WalletReturn');
UPDATE [hunger].[FinancialEvents]
SET [Role] = CASE
    WHEN [Kind] IN ('Funding','TopUpAdvance','TopUpCollected') THEN 'Custody'
    WHEN [Kind] = 'Collection' THEN 'Collections'
    WHEN [Kind] IN ('WithdrawalCompany','WithdrawalSupervisor','WalletDelivery','WalletReturn') THEN 'Wallet'
    WHEN [Kind] = 'RiderPayment' THEN 'RiderClearing' END
WHERE [Role] IS NULL;

CREATE INDEX [IX_EvidenceFiles_ClientPlatformId] ON [hunger].[EvidenceFiles] ([ClientPlatformId]);

CREATE INDEX [IX_EvidenceFiles_FinancialEventId] ON [hunger].[EvidenceFiles] ([FinancialEventId]);

CREATE INDEX [IX_EvidenceFiles_IsDeleted] ON [hunger].[EvidenceFiles] ([IsDeleted]);

CREATE INDEX [IX_EvidenceFiles_PlatformRiderAccountId_ObservedOn] ON [hunger].[EvidenceFiles] ([PlatformRiderAccountId], [ObservedOn]);

CREATE UNIQUE INDEX [IX_ExceptionApprovals_ClientPlatformId_IdempotencyKey] ON [hunger].[ExceptionApprovals] ([ClientPlatformId], [IdempotencyKey]);

CREATE INDEX [IX_ExceptionApprovals_ClientPlatformId_Status] ON [hunger].[ExceptionApprovals] ([ClientPlatformId], [Status]);

CREATE INDEX [IX_ExceptionApprovals_IsDeleted] ON [hunger].[ExceptionApprovals] ([IsDeleted]);

CREATE INDEX [IX_ExceptionApprovals_PlatformRiderAccountId] ON [hunger].[ExceptionApprovals] ([PlatformRiderAccountId]);

CREATE INDEX [IX_ExceptionApprovals_SupervisorEmployeeId] ON [hunger].[ExceptionApprovals] ([SupervisorEmployeeId]);

ALTER TABLE [hunger].[FinancialEvents] ADD CONSTRAINT [FK_FinancialEvents_ExceptionApprovals_ExceptionApprovalId] FOREIGN KEY ([ExceptionApprovalId]) REFERENCES [hunger].[ExceptionApprovals] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [hunger].[FinancialEvents] ADD CONSTRAINT [FK_FinancialEvents_FinancialEvents_SupersedesEventId] FOREIGN KEY ([SupersedesEventId]) REFERENCES [hunger].[FinancialEvents] ([Id]) ON DELETE NO ACTION;

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261111071640_HungerCr005SealEvidenceExceptions', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [hunger].[SettlementDocuments] ADD [RiderBalanceTotal] decimal(18,2) NULL;

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261111073327_HungerCr005InvoiceFedMonthClose', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [hunger].[SettlementDocuments] ADD [ApprovalDate] date NULL;

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261111074551_HungerCr005AdjustmentApprovalDate', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [app].[PlatformAccountBusinessDetails] DROP CONSTRAINT [CK_PlatformBusiness_Disposition];

ALTER TABLE [app].[PlatformAccountBusinessDetails] ADD CONSTRAINT [CK_PlatformBusiness_Disposition] CHECK ([OperationalDisposition] IN ('Normal','Problem','AdministrativeStop','Blocked','DeferredNextMonth','WalletPending'));

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261111081325_HungerCr005WalletPendingDisposition', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
EXEC(N'IF SCHEMA_ID(''finance_read'') IS NULL EXEC(''CREATE SCHEMA finance_read'')')

EXEC(N'CREATE OR ALTER VIEW finance_read.vw_hunger_financial_events_v1 AS
SELECT e.Id AS EventId, e.ClientPlatformId, e.SupervisorEmployeeId, e.PlatformRiderAccountId, e.RiderProfileId,
       e.RelatedEventId, e.SupersedesEventId, e.Kind, e.Status, e.Role, e.Amount, e.RiderBalanceChange, e.BusinessDate,
       e.FromSubAccountId, e.ToSubAccountId, e.DestinationType, e.DestinationChannel, e.DestinationOwnerId,
       e.ApprovalStatus, e.ApprovedAtUtc, e.ApprovedByUserId, e.RejectionReason, e.SealAccountsJson, e.ExceptionApprovalId,
       CAST(CASE WHEN e.ApprovalStatus = ''Sealed'' THEN 1 ELSE 0 END AS bit) AS IsSealed,
       CASE WHEN e.Kind = ''TopUpCollected'' AND e.DestinationChannel = ''OwnerVault'' THEN ''HungerWalletTopUpExpense'' END AS ExpenseLabel,
       e.EvidenceReference, e.CreatedAtUtc, e.CreatedByUserId
FROM hunger.FinancialEvents e')

EXEC(N'CREATE OR ALTER VIEW finance_read.vw_hunger_wallet_reviews_v1 AS
SELECT r.Id AS ReviewId, r.PlatformRiderAccountId, r.Status, r.SwapId, r.UserId AS ReviewedByUserId, r.CreatedAtUtc,
       JSON_VALUE(r.RequestJson, ''$.action'') AS Direction,
       TRY_CONVERT(uniqueidentifier, JSON_VALUE(r.RequestJson, ''$.riderProfileId'')) AS RiderProfileId,
       TRY_CONVERT(uniqueidentifier, JSON_VALUE(r.RequestJson, ''$.supervisorEmployeeId'')) AS SupervisorEmployeeId,
       TRY_CONVERT(date, JSON_VALUE(r.RequestJson, ''$.businessDate'')) AS MObservedAt,
       TRY_CONVERT(decimal(18,2), JSON_VALUE(r.RequestJson, ''$.mobileBalance'')) AS M,
       TRY_CONVERT(decimal(18,2), JSON_VALUE(e.DetailsJson, ''$.closingBalance'')) AS C,
       TRY_CONVERT(decimal(18,2), JSON_VALUE(e.DetailsJson, ''$.provenAdjustments'')) AS A,
       JSON_VALUE(e.DetailsJson, ''$.closingSource'') AS ClosingSource,
       TRY_CONVERT(decimal(18,2), JSON_VALUE(e.DetailsJson, ''$.attributedBalance'')) AS ExplicitBalance,
       e.ExceptionApprovalId AS ApprovalId, e.Id AS EventId, e.Kind AS EventKind, e.RiderBalanceChange,
       e.ApprovalStatus, e.ApprovedAtUtc, e.ApprovedByUserId, e.EvidenceReference AS WalletEvidence
FROM hunger.WalletReviews r
LEFT JOIN hunger.FinancialEvents e ON e.IdempotencyKey = CONCAT(''WalletReview:'', LOWER(CONVERT(nvarchar(36), r.Id)))
WHERE r.IsDeleted = 0')

EXEC(N'CREATE OR ALTER VIEW finance_read.vw_hunger_month_close_v1 AS
SELECT d.Id AS DocumentId, d.ClientPlatformId, d.PlatformRiderAccountId, d.ChargeFrom AS PeriodStart, d.ChargeThrough AS PeriodEnd,
       d.WalletBalance AS C, d.Status, d.ObservedOn, d.InvoiceDocumentId,
       CASE WHEN d.IdempotencyKey LIKE ''InvoiceClose:%'' THEN ''Invoice'' ELSE ''Manual'' END AS Source,
       ISNULL((SELECT SUM(a.WalletBalance) FROM hunger.SettlementDocuments a
               WHERE a.Kind = ''Adjustment'' AND a.Status = ''Matched'' AND a.ClosingSnapshotId = d.Id), 0) AS A,
       d.EvidenceReference, d.CreatedAtUtc
FROM hunger.SettlementDocuments d
WHERE d.Kind = ''MonthClose''')

EXEC(N'CREATE OR ALTER VIEW finance_read.vw_hunger_adjustments_v1 AS
SELECT d.Id AS DocumentId, d.ClientPlatformId, d.PlatformRiderAccountId, d.ClosingSnapshotId AS MonthCloseDocumentId,
       d.InvoicePeriod, d.WalletBalance AS A, d.Status, d.ApprovalDate, d.ObservedOn,
       d.SourceDocumentType, d.SourceReference, d.ReconciliationEvidence, d.CreatedAtUtc
FROM hunger.SettlementDocuments d
WHERE d.Kind = ''Adjustment''')

EXEC(N'CREATE OR ALTER VIEW finance_read.vw_hunger_custody_v1 AS
SELECT x.ClientPlatformId, x.SupervisorEmployeeId, x.SponsorId, x.CustodySubAccountId,
       x.FundingDelivered, x.TopUpAdvance, x.TopUpCollected,
       x.FundingDelivered - x.TopUpAdvance - x.TopUpCollected AS ExpectedBalance, x.PostingBalance,
       x.PostingBalance - (x.FundingDelivered - x.TopUpAdvance - x.TopUpCollected) AS Difference
FROM (
    SELECT c.ClientPlatformId, c.SupervisorEmployeeId, c.SponsorId, c.CustodySubAccountId,
           ISNULL((SELECT SUM(e.Amount) FROM hunger.FinancialEvents e WHERE e.ClientPlatformId = c.ClientPlatformId
                   AND e.SupervisorEmployeeId = c.SupervisorEmployeeId AND e.Kind = ''Funding''), 0) AS FundingDelivered,
           ISNULL((SELECT SUM(e.Amount) FROM hunger.FinancialEvents e WHERE e.ClientPlatformId = c.ClientPlatformId
                   AND e.SupervisorEmployeeId = c.SupervisorEmployeeId AND e.Kind = ''TopUpAdvance'' AND e.Status = ''Posted''), 0) AS TopUpAdvance,
           ISNULL((SELECT SUM(e.Amount) FROM hunger.FinancialEvents e WHERE e.ClientPlatformId = c.ClientPlatformId
                   AND e.SupervisorEmployeeId = c.SupervisorEmployeeId AND e.Kind = ''TopUpCollected'' AND e.Status = ''Posted''), 0) AS TopUpCollected,
           ISNULL((SELECT SUM(p.Amount) FROM jahez_v3.Postings p WHERE p.ClientPlatformId = c.ClientPlatformId
                   AND p.SupervisorEmployeeId = c.SupervisorEmployeeId AND p.SubAccountId = c.CustodySubAccountId), 0) AS PostingBalance
    FROM hunger.FinanceConfigurations c
    WHERE c.IsDeleted = 0
) x')

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261111082612_HungerCr005FinanceReadViews', N'10.0.11');

COMMIT;
GO

