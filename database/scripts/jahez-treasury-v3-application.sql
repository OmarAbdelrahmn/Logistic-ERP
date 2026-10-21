BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    IF SCHEMA_ID(N'jahez_v3') IS NULL EXEC(N'CREATE SCHEMA [jahez_v3];');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    ALTER TABLE [jahez_v2].[CashboxHandovers] ADD [RequestedByNameAr] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    ALTER TABLE [jahez_v2].[CashboxHandovers] ADD [AccountantNameAr] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    ALTER TABLE [jahez_v2].[Operations] ADD [ExpectedSponsorId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    ALTER TABLE [jahez_v2].[CashboxHandovers] ADD [DocumentNumber] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    ALTER TABLE [jahez_v2].[CashboxHandovers] ADD [TreasuryVersion] int NOT NULL DEFAULT 2;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE TABLE [jahez_v3].[Ownership] (
        [Id] uniqueidentifier NOT NULL,
        [Source] int NOT NULL,
        [SourceId] uniqueidentifier NOT NULL,
        [PlatformAccountId] uniqueidentifier NOT NULL,
        [SponsorId] uniqueidentifier NOT NULL,
        [SponsorIdentifier] nvarchar(100) NOT NULL,
        [SponsorNameAr] nvarchar(200) NOT NULL,
        [Evidence] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_Ownership] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Ownership_Sponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE TABLE [jahez_v3].[PetroEvidence] (
        [Id] uniqueidentifier NOT NULL,
        [SponsorId] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [BusinessDate] date NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [AllocatedCollections] decimal(18,2) NOT NULL,
        [Reference] nvarchar(500) NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [ReplacesId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_PetroEvidence] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PetroEvidence_Amounts] CHECK ([Amount]>=0 AND [AllocatedCollections]>=0 AND [Kind] IN(1,2) AND ([Kind]=1 OR [AllocatedCollections]=0)),
        CONSTRAINT [FK_PetroEvidence_PetroEvidence_ReplacesId] FOREIGN KEY ([ReplacesId]) REFERENCES [jahez_v3].[PetroEvidence] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PetroEvidence_Sponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE TABLE [jahez_v3].[State] (
        [Id] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [ActivatedAtUtc] datetimeoffset NULL,
        [NextHandoverNumber] bigint NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_State] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TreasuryState_Singleton] CHECK ([Id]='019d0000-0000-7000-8000-000000000001' AND [NextHandoverNumber]>0 AND [IsDeleted]=0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE TABLE [jahez_v3].[TreasurySubAccounts] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [ParentId] uniqueidentifier NULL,
        [SponsorId] uniqueidentifier NULL,
        [Location] int NOT NULL,
        [Purpose] int NOT NULL,
        [AllowedDirections] int NOT NULL,
        [IsPosting] bit NOT NULL,
        [IsDefault] bit NOT NULL,
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
        CONSTRAINT [PK_TreasurySubAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TreasuryAccount_Classification] CHECK ([Location] BETWEEN 1 AND 4 AND [Purpose] BETWEEN 1 AND 3 AND [AllowedDirections] BETWEEN 1 AND 3 AND ([IsPosting]=0 OR [SponsorId] IS NOT NULL) AND [IsDeleted]=0),
        CONSTRAINT [FK_TreasurySubAccounts_Sponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TreasurySubAccounts_TreasurySubAccounts_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [jahez_v3].[TreasurySubAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE TABLE [jahez_v3].[BalanceVerifications] (
        [Id] uniqueidentifier NOT NULL,
        [SubAccountId] uniqueidentifier NOT NULL,
        [VerifiedBalance] decimal(18,2) NOT NULL,
        [VerifiedAtUtc] datetimeoffset NOT NULL,
        [Reference] nvarchar(500) NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_BalanceVerifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BalanceVerifications_TreasurySubAccounts_SubAccountId] FOREIGN KEY ([SubAccountId]) REFERENCES [jahez_v3].[TreasurySubAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE TABLE [jahez_v3].[HandoverLines] (
        [AccountCode] nvarchar(100) NOT NULL,
        [AccountNameAr] nvarchar(200) NOT NULL,
        [Id] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [SubAccountId] uniqueidentifier NOT NULL,
        [SponsorId] uniqueidentifier NOT NULL,
        [SponsorNameAr] nvarchar(200) NOT NULL,
        [SponsorIdentifier] nvarchar(100) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [MovementCount] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_HandoverLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HandoverLines_CashboxHandovers_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[CashboxHandovers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HandoverLines_TreasurySubAccounts_SubAccountId] FOREIGN KEY ([SubAccountId]) REFERENCES [jahez_v3].[TreasurySubAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE TABLE [jahez_v3].[Postings] (
        [ActorNameAr] nvarchar(200) NOT NULL,
        [Id] uniqueidentifier NOT NULL,
        [SubAccountId] uniqueidentifier NOT NULL,
        [SponsorId] uniqueidentifier NOT NULL,
        [MoneyMovementId] uniqueidentifier NULL,
        [CashboxHandoverId] uniqueidentifier NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [Revision] int NOT NULL,
        [ReversesPostingId] uniqueidentifier NULL,
        [Kind] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [Reference] nvarchar(500) NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_Postings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TreasuryPosting_Amount] CHECK ([Amount]<>0 AND [Kind] BETWEEN 1 AND 5 AND [Revision]>=0),
        CONSTRAINT [FK_Postings_CashboxHandovers_CashboxHandoverId] FOREIGN KEY ([CashboxHandoverId]) REFERENCES [jahez_v2].[CashboxHandovers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Postings_MoneyMovements_MoneyMovementId] FOREIGN KEY ([MoneyMovementId]) REFERENCES [jahez_v2].[MoneyMovements] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Postings_Postings_ReversesPostingId] FOREIGN KEY ([ReversesPostingId]) REFERENCES [jahez_v3].[Postings] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Postings_Sponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Postings_TreasurySubAccounts_SubAccountId] FOREIGN KEY ([SubAccountId]) REFERENCES [jahez_v3].[TreasurySubAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE TABLE [jahez_v3].[HandoverPostings] (
        [Id] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [PostingId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_HandoverPostings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HandoverPostings_CashboxHandovers_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[CashboxHandovers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HandoverPostings_Postings_PostingId] FOREIGN KEY ([PostingId]) REFERENCES [jahez_v3].[Postings] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000750'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''jahez.treasury.read'', N''jahez.treasury.read'', 750, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.treasury.read'', N''عرض الخزينة'', N''jahez.treasury.read'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000751'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''jahez.treasury.manage'', N''jahez.treasury.manage'', 751, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.treasury.manage'', N''إدارة الخزينة'', N''jahez.treasury.manage'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000752'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''jahez.treasury.adjust'', N''jahez.treasury.adjust'', 752, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.treasury.adjust'', N''تسوية وترحيل الخزينة'', N''jahez.treasury.adjust'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000753'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''jahez.financial-reports.read'', N''jahez.financial-reports.read'', 753, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.financial-reports.read'', N''عرض التقارير المالية'', N''jahez.financial-reports.read'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000754'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''jahez.financial-reports.export'', N''jahez.financial-reports.export'', 754, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.financial-reports.export'', N''تصدير التقارير المالية'', N''jahez.financial-reports.export'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActivatedAtUtc', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsActive', N'IsDeleted', N'NextHandoverNumber', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[jahez_v3].[State]'))
        SET IDENTITY_INSERT [jahez_v3].[State] ON;
    EXEC(N'INSERT INTO [jahez_v3].[State] ([Id], [ActivatedAtUtc], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsActive], [IsDeleted], [NextHandoverNumber], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019d0000-0000-7000-8000-000000000001'', NULL, ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bigint), NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActivatedAtUtc', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsActive', N'IsDeleted', N'NextHandoverNumber', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[jahez_v3].[State]'))
        SET IDENTITY_INSERT [jahez_v3].[State] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AllowedDirections', N'Code', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsActive', N'IsDefault', N'IsDeleted', N'IsPosting', N'Location', N'NameAr', N'NameEn', N'ParentId', N'Purpose', N'SponsorId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[jahez_v3].[TreasurySubAccounts]'))
        SET IDENTITY_INSERT [jahez_v3].[TreasurySubAccounts] ON;
    EXEC(N'INSERT INTO [jahez_v3].[TreasurySubAccounts] ([Id], [AllowedDirections], [Code], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsActive], [IsDefault], [IsDeleted], [IsPosting], [Location], [NameAr], [NameEn], [ParentId], [Purpose], [SponsorId], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019d0000-0000-7000-8000-000000000010'', 3, N''SUP'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 1, N''عهدة مشرف جاهز'', N''Jahez supervisor custody'', NULL, 3, NULL, NULL, NULL),
    (''019d0000-0000-7000-8000-000000000011'', 3, N''MAIN'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 2, N''الخزينة المركزية'', N''Central treasury'', NULL, 3, NULL, NULL, NULL),
    (''019d0000-0000-7000-8000-000000000012'', 3, N''BANK'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 3, N''البنك'', N''Bank'', NULL, 3, NULL, NULL, NULL),
    (''019d0000-0000-7000-8000-000000000013'', 3, N''PETRO'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 4, N''بترو آب - مقاصة التحصيل'', N''PetroApp collection clearing'', NULL, 3, NULL, NULL, NULL),
    (''019d0000-0000-7000-8000-000000000014'', 3, N''MAIN.FEES'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 2, N''رسوم البوابا وإكسبرس'', N''Al-Bwaba and Express fees'', ''019d0000-0000-7000-8000-000000000011'', 2, NULL, NULL, NULL),
    (''019d0000-0000-7000-8000-000000000015'', 3, N''BANK.BWEXP'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 3, N''بنك البوابا وإكسبرس'', N''Al-Bwaba and Express bank'', ''019d0000-0000-7000-8000-000000000012'', 3, NULL, NULL, NULL),
    (''019d0000-0000-7000-8000-000000000016'', 3, N''SUP.BW.SET'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 1, N''عهدة تصفيات البوابا'', N''BW settlement custody'', ''019d0000-0000-7000-8000-000000000010'', 1, ''019c18d5-62e1-7000-8000-000000000041'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000017'', 1, N''SUP.BW.FEE'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 1, N''عهدة رسوم البوابا'', N''BW fee custody'', ''019d0000-0000-7000-8000-000000000010'', 2, ''019c18d5-62e1-7000-8000-000000000041'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000019'', 3, N''MAIN.BW.SET'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 2, N''تصفيات البوابا'', N''BW central settlements'', ''019d0000-0000-7000-8000-000000000011'', 1, ''019c18d5-62e1-7000-8000-000000000041'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000021'', 1, N''PETRO.BW'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 4, N''بترو آب البوابا'', N''BW PetroApp clearing'', ''019d0000-0000-7000-8000-000000000013'', 3, ''019c18d5-62e1-7000-8000-000000000041'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000022'', 3, N''SUP.EX.SET'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 1, N''عهدة تصفيات إكسبرس'', N''EX settlement custody'', ''019d0000-0000-7000-8000-000000000010'', 1, ''019c18d5-62e1-7000-8000-000000000042'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000023'', 1, N''SUP.EX.FEE'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 1, N''عهدة رسوم إكسبرس'', N''EX fee custody'', ''019d0000-0000-7000-8000-000000000010'', 2, ''019c18d5-62e1-7000-8000-000000000042'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000025'', 3, N''MAIN.EX.SET'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 2, N''تصفيات إكسبرس'', N''EX central settlements'', ''019d0000-0000-7000-8000-000000000011'', 1, ''019c18d5-62e1-7000-8000-000000000042'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000027'', 1, N''PETRO.EX'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 4, N''بترو آب إكسبرس'', N''EX PetroApp clearing'', ''019d0000-0000-7000-8000-000000000013'', 3, ''019c18d5-62e1-7000-8000-000000000042'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000028'', 3, N''SUP.TR.SET'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 1, N''عهدة تصفيات التجارية'', N''TR settlement custody'', ''019d0000-0000-7000-8000-000000000010'', 1, ''019c18d5-62e1-7000-8000-000000000040'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000029'', 1, N''SUP.TR.FEE'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 1, N''عهدة رسوم التجارية'', N''TR fee custody'', ''019d0000-0000-7000-8000-000000000010'', 2, ''019c18d5-62e1-7000-8000-000000000040'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000030'', 3, N''MAIN.TR'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 2, N''صندوق التجارية'', N''Trading central treasury'', ''019d0000-0000-7000-8000-000000000011'', 3, ''019c18d5-62e1-7000-8000-000000000040'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000031'', 2, N''BANK.TR'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 3, N''بنك التجارية'', N''TR bank'', ''019d0000-0000-7000-8000-000000000012'', 3, ''019c18d5-62e1-7000-8000-000000000040'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000032'', 1, N''PETRO.TR'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 4, N''بترو آب التجارية'', N''TR PetroApp clearing'', ''019d0000-0000-7000-8000-000000000013'', 3, ''019c18d5-62e1-7000-8000-000000000040'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000018'', 1, N''MAIN.BW.FEE'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 2, N''رسوم البوابا'', N''BW central fees'', ''019d0000-0000-7000-8000-000000000014'', 2, ''019c18d5-62e1-7000-8000-000000000041'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000020'', 2, N''BANK.BW'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 3, N''بنك البوابا'', N''BW bank'', ''019d0000-0000-7000-8000-000000000015'', 3, ''019c18d5-62e1-7000-8000-000000000041'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000024'', 1, N''MAIN.EX.FEE'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 2, N''رسوم إكسبرس'', N''EX central fees'', ''019d0000-0000-7000-8000-000000000014'', 2, ''019c18d5-62e1-7000-8000-000000000042'', NULL, NULL),
    (''019d0000-0000-7000-8000-000000000026'', 2, N''BANK.EX'', ''2026-10-19T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 3, N''بنك إكسبرس'', N''EX bank'', ''019d0000-0000-7000-8000-000000000015'', 3, ''019c18d5-62e1-7000-8000-000000000042'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AllowedDirections', N'Code', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsActive', N'IsDefault', N'IsDeleted', N'IsPosting', N'Location', N'NameAr', N'NameEn', N'ParentId', N'Purpose', N'SponsorId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[jahez_v3].[TreasurySubAccounts]'))
        SET IDENTITY_INSERT [jahez_v3].[TreasurySubAccounts] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CashboxHandovers_DocumentNumber] ON [jahez_v2].[CashboxHandovers] ([DocumentNumber]) WHERE [DocumentNumber] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_BalanceVerifications_SubAccountId_VerifiedAtUtc] ON [jahez_v3].[BalanceVerifications] ([SubAccountId], [VerifiedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HandoverLines_HandoverId_SubAccountId] ON [jahez_v3].[HandoverLines] ([HandoverId], [SubAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_HandoverLines_SubAccountId] ON [jahez_v3].[HandoverLines] ([SubAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HandoverPostings_HandoverId_PostingId] ON [jahez_v3].[HandoverPostings] ([HandoverId], [PostingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_HandoverPostings_PostingId] ON [jahez_v3].[HandoverPostings] ([PostingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_Ownership_PlatformAccountId_SponsorId] ON [jahez_v3].[Ownership] ([PlatformAccountId], [SponsorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Ownership_Source_SourceId] ON [jahez_v3].[Ownership] ([Source], [SourceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_Ownership_SponsorId] ON [jahez_v3].[Ownership] ([SponsorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PetroEvidence_ReplacesId] ON [jahez_v3].[PetroEvidence] ([ReplacesId]) WHERE [ReplacesId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_PetroEvidence_SponsorId_BusinessDate] ON [jahez_v3].[PetroEvidence] ([SponsorId], [BusinessDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PetroEvidence_SponsorId_Reference] ON [jahez_v3].[PetroEvidence] ([SponsorId], [Reference]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_Postings_BatchId] ON [jahez_v3].[Postings] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_Postings_CashboxHandoverId] ON [jahez_v3].[Postings] ([CashboxHandoverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Postings_MoneyMovementId_Revision] ON [jahez_v3].[Postings] ([MoneyMovementId], [Revision]) WHERE [MoneyMovementId] IS NOT NULL AND [Kind] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Postings_ReversesPostingId] ON [jahez_v3].[Postings] ([ReversesPostingId]) WHERE [ReversesPostingId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_Postings_SponsorId_OccurredAtUtc] ON [jahez_v3].[Postings] ([SponsorId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_Postings_SubAccountId_OccurredAtUtc_Id] ON [jahez_v3].[Postings] ([SubAccountId], [OccurredAtUtc], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_State_IsDeleted] ON [jahez_v3].[State] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TreasurySubAccounts_Code] ON [jahez_v3].[TreasurySubAccounts] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_TreasurySubAccounts_IsDeleted] ON [jahez_v3].[TreasurySubAccounts] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    CREATE INDEX [IX_TreasurySubAccounts_ParentId] ON [jahez_v3].[TreasurySubAccounts] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TreasurySubAccounts_SponsorId_Location_Purpose] ON [jahez_v3].[TreasurySubAccounts] ([SponsorId], [Location], [Purpose]) WHERE [IsDefault] = 1 AND [IsPosting] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE TRIGGER [jahez_v3].[TR_Ownership_Immutable] ON [jahez_v3].[Ownership]
    AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''Treasury evidence is append-only. Record an audited reversal or replacement.'', 1;
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE TRIGGER [jahez_v3].[TR_Postings_Immutable] ON [jahez_v3].[Postings]
    AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''Treasury evidence is append-only. Record an audited reversal or replacement.'', 1;
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE TRIGGER [jahez_v3].[TR_BalanceVerifications_Immutable] ON [jahez_v3].[BalanceVerifications]
    AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''Treasury evidence is append-only. Record an audited reversal or replacement.'', 1;
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE TRIGGER [jahez_v3].[TR_HandoverLines_Immutable] ON [jahez_v3].[HandoverLines]
    AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''Treasury evidence is append-only. Record an audited reversal or replacement.'', 1;
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE TRIGGER [jahez_v3].[TR_HandoverPostings_Immutable] ON [jahez_v3].[HandoverPostings]
    AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''Treasury evidence is append-only. Record an audited reversal or replacement.'', 1;
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE TRIGGER [jahez_v3].[TR_PetroEvidence_Immutable] ON [jahez_v3].[PetroEvidence]
    AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''Treasury evidence is append-only. Record an audited reversal or replacement.'', 1;
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE TRIGGER [jahez_v3].[TR_Postings_Ownership] ON [jahez_v3].[Postings]
    AFTER INSERT AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS(SELECT 1 FROM inserted i JOIN jahez_v3.TreasurySubAccounts a ON a.Id=i.SubAccountId
            WHERE a.IsPosting=0 OR a.SponsorId IS NULL OR a.SponsorId<>i.SponsorId)
            THROW 51000, ''Posting sponsor must match its posting account.'', 1;
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    EXEC(N'CREATE TRIGGER [jahez_v3].[TR_TreasuryAccounts_Identity] ON [jahez_v3].[TreasurySubAccounts]
    AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        IF NOT EXISTS(SELECT 1 FROM inserted) OR UPDATE(Code) OR UPDATE(ParentId) OR UPDATE(SponsorId)
            OR UPDATE(Location) OR UPDATE(Purpose) OR UPDATE(AllowedDirections) OR UPDATE(IsPosting) OR UPDATE(IsDefault)
            THROW 51000, ''Treasury account identity and routing are immutable.'', 1;
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019075320_AddJahezTreasuryV3'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019075320_AddJahezTreasuryV3', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019114428_UseGranularJahezTreasuryPermissions'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''jahez.treasury.accounts.create'', [DescriptionEn] = N''jahez.treasury.accounts.create'', [Key] = N''jahez.treasury.accounts.create'', [NameAr] = N''إنشاء حساب خزينة'', [NameEn] = N''jahez.treasury.accounts.create''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000751'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019114428_UseGranularJahezTreasuryPermissions'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''jahez.treasury.accounts.update'', [DescriptionEn] = N''jahez.treasury.accounts.update'', [Key] = N''jahez.treasury.accounts.update'', [NameAr] = N''تحديث حساب خزينة'', [NameEn] = N''jahez.treasury.accounts.update''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000752'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019114428_UseGranularJahezTreasuryPermissions'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''jahez.treasury.transfers.create'', [DescriptionEn] = N''jahez.treasury.transfers.create'', [Key] = N''jahez.treasury.transfers.create'', [NameAr] = N''إنشاء تحويل خزينة'', [NameEn] = N''jahez.treasury.transfers.create''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000753'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019114428_UseGranularJahezTreasuryPermissions'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''jahez.treasury.entries.create'', [DescriptionEn] = N''jahez.treasury.entries.create'', [Key] = N''jahez.treasury.entries.create'', [NameAr] = N''إنشاء قبض وصرف مركزي'', [NameEn] = N''jahez.treasury.entries.create''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000754'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019114428_UseGranularJahezTreasuryPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000755'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''jahez.treasury.petroapp-evidence.create'', N''jahez.treasury.petroapp-evidence.create'', 755, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.treasury.petroapp-evidence.create'', N''إنشاء دليل بترو آب'', N''jahez.treasury.petroapp-evidence.create'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000756'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''jahez.treasury.adjust'', N''jahez.treasury.adjust'', 756, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.treasury.adjust'', N''تسوية وترحيل الخزينة'', N''jahez.treasury.adjust'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000757'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''jahez.financial-reports.read'', N''jahez.financial-reports.read'', 757, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.financial-reports.read'', N''عرض التقارير المالية'', N''jahez.financial-reports.read'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000758'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''jahez.financial-reports.export'', N''jahez.financial-reports.export'', 758, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.financial-reports.export'', N''تصدير التقارير المالية'', N''jahez.financial-reports.export'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261019114428_UseGranularJahezTreasuryPermissions'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019114428_UseGranularJahezTreasuryPermissions', N'10.0.11');
END;

COMMIT;
GO

