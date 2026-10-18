BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    IF SCHEMA_ID(N'chefz') IS NULL EXEC(N'CREATE SCHEMA [chefz];');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE TABLE [chefz].[AccountDetails] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [ReportIdNumber] nvarchar(100) NOT NULL,
        [AccountType] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_AccountDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChefzDetails_Type] CHECK ([AccountType] IN (1, 2)),
        CONSTRAINT [FK_AccountDetails_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE TABLE [chefz].[PerformanceImports] (
        [Id] uniqueidentifier NOT NULL,
        [ReportDate] date NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [ContentHash] nvarchar(64) NOT NULL,
        [ImportedRows] int NOT NULL,
        [SkippedRows] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_PerformanceImports] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE TABLE [chefz].[WalletSettlements] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [RiderClientAssignmentId] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [AccountType] int NOT NULL,
        [PeriodFrom] date NOT NULL,
        [PeriodTo] date NOT NULL,
        [ChargedFrom] date NOT NULL,
        [ChargedTo] date NOT NULL,
        [AssignedDays] int NOT NULL,
        [StartWalletAmount] decimal(18,2) NOT NULL,
        [EndWalletAmount] decimal(18,2) NOT NULL,
        [GrossEarnings] decimal(18,2) NOT NULL,
        [CommissionRate] decimal(5,4) NOT NULL,
        [DailyFee] decimal(18,2) NOT NULL,
        [CompanyAmount] decimal(18,2) NOT NULL,
        [RiderAmount] decimal(18,2) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_WalletSettlements] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChefzSettlement_Amounts] CHECK ([StartWalletAmount] >= 0 AND [EndWalletAmount] >= [StartWalletAmount] AND [GrossEarnings] = [EndWalletAmount] - [StartWalletAmount] AND [RiderAmount] = [GrossEarnings] - [CompanyAmount] AND [AssignedDays] > 0 AND [ChargedFrom] >= [PeriodFrom] AND [ChargedTo] <= [PeriodTo] AND [ChargedTo] >= [ChargedFrom] AND [AccountType] IN (1, 2)),
        CONSTRAINT [FK_WalletSettlements_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WalletSettlements_RiderClientAssignments_RiderClientAssignmentId] FOREIGN KEY ([RiderClientAssignmentId]) REFERENCES [app].[RiderClientAssignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WalletSettlements_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE TABLE [chefz].[DailyPerformance] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [RiderClientAssignmentId] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
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
        CONSTRAINT [PK_DailyPerformance] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChefzPerformance_Values] CHECK ([TotalValidOffers] >= 0 AND [AcceptedOffers] >= 0 AND [DeclinedOffers] >= 0 AND [MissedOffers] >= 0 AND [AcceptanceRate] BETWEEN 0 AND 100),
        CONSTRAINT [FK_DailyPerformance_PerformanceImports_ImportId] FOREIGN KEY ([ImportId]) REFERENCES [chefz].[PerformanceImports] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DailyPerformance_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DailyPerformance_RiderClientAssignments_RiderClientAssignmentId] FOREIGN KEY ([RiderClientAssignmentId]) REFERENCES [app].[RiderClientAssignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DailyPerformance_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000306'', N''Chefz'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض حسابات وأداء وتصفيات شيفز.'', N''Read Chefz accounts, performance and settlements.'', 306, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''chefz.read'', N''عرض شيفز'', N''Read Chefz'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000307'', N''Chefz'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تحديد نوع الحساب ورقم الهوية في شيفز.'', N''Configure Chefz account type and report identity.'', 307, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''chefz.accounts.update'', N''تعديل حسابات شيفز'', N''Edit Chefz accounts'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000308'', N''Chefz'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''استيراد أداء المندوب اليومي.'', N''Import daily rider performance.'', 308, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''chefz.performance.import'', N''استيراد أداء شيفز'', N''Import Chefz performance'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000309'', N''Chefz'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حفظ تصفيات محافظ شيفز نصف الشهرية.'', N''Save half-month Chefz wallet settlements.'', 309, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''chefz.settlements.create'', N''إنشاء تصفيات شيفز'', N''Create Chefz settlements'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE INDEX [IX_AccountDetails_IsDeleted] ON [chefz].[AccountDetails] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountDetails_PlatformRiderAccountId] ON [chefz].[AccountDetails] ([PlatformRiderAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AccountDetails_ReportIdNumber] ON [chefz].[AccountDetails] ([ReportIdNumber]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE INDEX [IX_DailyPerformance_ImportId] ON [chefz].[DailyPerformance] ([ImportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DailyPerformance_PlatformRiderAccountId_ReportDate] ON [chefz].[DailyPerformance] ([PlatformRiderAccountId], [ReportDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE INDEX [IX_DailyPerformance_RiderClientAssignmentId] ON [chefz].[DailyPerformance] ([RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE INDEX [IX_DailyPerformance_RiderProfileId_ReportDate] ON [chefz].[DailyPerformance] ([RiderProfileId], [ReportDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PerformanceImports_ReportDate_ContentHash] ON [chefz].[PerformanceImports] ([ReportDate], [ContentHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE INDEX [IX_WalletSettlements_PlatformRiderAccountId] ON [chefz].[WalletSettlements] ([PlatformRiderAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WalletSettlements_RiderClientAssignmentId_PeriodFrom] ON [chefz].[WalletSettlements] ([RiderClientAssignmentId], [PeriodFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    CREATE INDEX [IX_WalletSettlements_RiderProfileId_PeriodFrom] ON [chefz].[WalletSettlements] ([RiderProfileId], [PeriodFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010112122_AddChefzAccountsSettlementsAndPerformance'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261010112122_AddChefzAccountsSettlementsAndPerformance', N'10.0.11');
END;

COMMIT;
GO

