BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE TABLE [ninja].[AllocationProvenance] (
        [Id] uniqueidentifier NOT NULL,
        [SourceId] uniqueidentifier NOT NULL,
        [RiderClientAssignmentId] uniqueidentifier NOT NULL,
        [Origin] nvarchar(32) NOT NULL,
        [EvidenceReference] nvarchar(200) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_AllocationProvenance] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AllocationProvenance_NinjaDailyPerformanceSources_SourceId] FOREIGN KEY ([SourceId]) REFERENCES [ninja].[NinjaDailyPerformanceSources] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AllocationProvenance_RiderClientAssignments_RiderClientAssignmentId] FOREIGN KEY ([RiderClientAssignmentId]) REFERENCES [app].[RiderClientAssignments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE TABLE [ninja].[EvaluationSnapshots] (
        [Id] uniqueidentifier NOT NULL,
        [PolicyVersion] nvarchar(64) NOT NULL,
        [InputRevision] nvarchar(64) NOT NULL,
        [QueryJson] nvarchar(max) NOT NULL,
        [AccountIdsJson] nvarchar(max) NOT NULL,
        [ResultJson] nvarchar(max) NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [Frozen] bit NOT NULL,
        [SupersedesId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EvaluationSnapshots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EvaluationSnapshots_EvaluationSnapshots_SupersedesId] FOREIGN KEY ([SupersedesId]) REFERENCES [ninja].[EvaluationSnapshots] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE TABLE [ninja].[PerformancePreviews] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ContentHash] nvarchar(64) NOT NULL,
        [OptionsJson] nvarchar(max) NOT NULL,
        [InputRevision] nvarchar(64) NOT NULL,
        [ExpiresAtUtc] datetimeoffset NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_PerformancePreviews] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE TABLE [ninja].[SuggestionPolicies] (
        [Id] uniqueidentifier NOT NULL,
        [Version] nvarchar(64) NOT NULL,
        [DefinitionJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_SuggestionPolicies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE TABLE [ninja].[EvaluationContexts] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [Revision] int NOT NULL,
        [SupersedesId] uniqueidentifier NULL,
        [ConfirmedFirstDeliveryDate] date NULL,
        [ReviewedSnapshotId] uniqueidentifier NULL,
        [PricingPeriodsJson] nvarchar(max) NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EvaluationContexts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EvaluationContexts_EvaluationContexts_SupersedesId] FOREIGN KEY ([SupersedesId]) REFERENCES [ninja].[EvaluationContexts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EvaluationContexts_EvaluationSnapshots_ReviewedSnapshotId] FOREIGN KEY ([ReviewedSnapshotId]) REFERENCES [ninja].[EvaluationSnapshots] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EvaluationContexts_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE TABLE [ninja].[ResponsibilityRevisions] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [Month] date NOT NULL,
        [Revision] int NOT NULL,
        [SupersedesId] uniqueidentifier NULL,
        [ReviewedSnapshotId] uniqueidentifier NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_ResponsibilityRevisions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ResponsibilityRevisions_EvaluationSnapshots_ReviewedSnapshotId] FOREIGN KEY ([ReviewedSnapshotId]) REFERENCES [ninja].[EvaluationSnapshots] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ResponsibilityRevisions_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ResponsibilityRevisions_ResponsibilityRevisions_SupersedesId] FOREIGN KEY ([SupersedesId]) REFERENCES [ninja].[ResponsibilityRevisions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE TABLE [ninja].[PerformanceArtifacts] (
        [Id] uniqueidentifier NOT NULL,
        [OriginalArtifactId] uniqueidentifier NULL,
        [PreviewId] uniqueidentifier NULL,
        [ContentHash] nvarchar(64) NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [Format] nvarchar(16) NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [ResultJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_PerformanceArtifacts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PerformanceArtifacts_PerformanceArtifacts_OriginalArtifactId] FOREIGN KEY ([OriginalArtifactId]) REFERENCES [ninja].[PerformanceArtifacts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PerformanceArtifacts_PerformancePreviews_PreviewId] FOREIGN KEY ([PreviewId]) REFERENCES [ninja].[PerformancePreviews] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE TABLE [ninja].[ResponsibilityLines] (
        [Id] uniqueidentifier NOT NULL,
        [RevisionId] uniqueidentifier NOT NULL,
        [RiderClientAssignmentId] uniqueidentifier NOT NULL,
        [BusinessDate] date NOT NULL,
        [Weight] decimal(18,8) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_ResponsibilityLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_NinjaResponsibility_Weight] CHECK ([Weight] BETWEEN 0 AND 1),
        CONSTRAINT [FK_ResponsibilityLines_ResponsibilityRevisions_RevisionId] FOREIGN KEY ([RevisionId]) REFERENCES [ninja].[ResponsibilityRevisions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ResponsibilityLines_RiderClientAssignments_RiderClientAssignmentId] FOREIGN KEY ([RiderClientAssignmentId]) REFERENCES [app].[RiderClientAssignments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE TABLE [ninja].[PerformanceImportArtifacts] (
        [Id] uniqueidentifier NOT NULL,
        [ArtifactId] uniqueidentifier NOT NULL,
        [ImportId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_PerformanceImportArtifacts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PerformanceImportArtifacts_NinjaDailyImports_ImportId] FOREIGN KEY ([ImportId]) REFERENCES [ninja].[NinjaDailyImports] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PerformanceImportArtifacts_PerformanceArtifacts_ArtifactId] FOREIGN KEY ([ArtifactId]) REFERENCES [ninja].[PerformanceArtifacts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000317'', N''Ninja'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة المسؤولية والتسعير وحفظ نسخ التقييم التقديري.'', N''Manage suggestion responsibility, pricing context and snapshots.'', 317, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''ninja.evaluation.manage'', N''إدارة تقييم نينجا'', N''Manage Ninja evaluation'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000318'', N''Ninja'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تصدير التقييم التقديري المسموح.'', N''Export authorized Ninja monitoring suggestions.'', 318, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''ninja.evaluation.export'', N''تصدير تقييم نينجا'', N''Export Ninja evaluation'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DefinitionJson', N'Version') AND [object_id] = OBJECT_ID(N'[ninja].[SuggestionPolicies]'))
        SET IDENTITY_INSERT [ninja].[SuggestionPolicies] ON;
    EXEC(N'INSERT INTO [ninja].[SuggestionPolicies] ([Id], [CreatedAtUtc], [CreatedByUserId], [DefinitionJson], [Version])
    VALUES (''019c18d5-62e1-7000-8000-000000000390'', ''2026-10-26T00:00:00.0000000+00:00'', NULL, N''{"monthBasis":"CalendarDays","attendance":0.85,"hours":8,"reduction":0.15,"rounding":"AwayFromZero","bike":[450,5700,11,16,17,18,9],"car":[460,6500,12,22,22.5,23,10],"invoice":"External"}'', N''NINJA-SUGGESTION-V1'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DefinitionJson', N'Version') AND [object_id] = OBJECT_ID(N'[ninja].[SuggestionPolicies]'))
        SET IDENTITY_INSERT [ninja].[SuggestionPolicies] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE INDEX [IX_AllocationProvenance_RiderClientAssignmentId] ON [ninja].[AllocationProvenance] ([RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AllocationProvenance_SourceId_RiderClientAssignmentId] ON [ninja].[AllocationProvenance] ([SourceId], [RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EvaluationContexts_PlatformRiderAccountId_Revision] ON [ninja].[EvaluationContexts] ([PlatformRiderAccountId], [Revision]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE INDEX [IX_EvaluationContexts_ReviewedSnapshotId] ON [ninja].[EvaluationContexts] ([ReviewedSnapshotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE INDEX [IX_EvaluationContexts_SupersedesId] ON [ninja].[EvaluationContexts] ([SupersedesId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE INDEX [IX_EvaluationSnapshots_CreatedAtUtc] ON [ninja].[EvaluationSnapshots] ([CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE INDEX [IX_EvaluationSnapshots_SupersedesId] ON [ninja].[EvaluationSnapshots] ([SupersedesId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE INDEX [IX_PerformanceArtifacts_ContentHash] ON [ninja].[PerformanceArtifacts] ([ContentHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE INDEX [IX_PerformanceArtifacts_OriginalArtifactId] ON [ninja].[PerformanceArtifacts] ([OriginalArtifactId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PerformanceArtifacts_PreviewId] ON [ninja].[PerformanceArtifacts] ([PreviewId]) WHERE [PreviewId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PerformanceImportArtifacts_ArtifactId_ImportId] ON [ninja].[PerformanceImportArtifacts] ([ArtifactId], [ImportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE INDEX [IX_PerformanceImportArtifacts_ImportId] ON [ninja].[PerformanceImportArtifacts] ([ImportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE INDEX [IX_PerformancePreviews_ExpiresAtUtc] ON [ninja].[PerformancePreviews] ([ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ResponsibilityLines_RevisionId_BusinessDate_RiderClientAssignmentId] ON [ninja].[ResponsibilityLines] ([RevisionId], [BusinessDate], [RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE INDEX [IX_ResponsibilityLines_RiderClientAssignmentId] ON [ninja].[ResponsibilityLines] ([RiderClientAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ResponsibilityRevisions_PlatformRiderAccountId_Month_Revision] ON [ninja].[ResponsibilityRevisions] ([PlatformRiderAccountId], [Month], [Revision]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE INDEX [IX_ResponsibilityRevisions_ReviewedSnapshotId] ON [ninja].[ResponsibilityRevisions] ([ReviewedSnapshotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE INDEX [IX_ResponsibilityRevisions_SupersedesId] ON [ninja].[ResponsibilityRevisions] ([SupersedesId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SuggestionPolicies_Version] ON [ninja].[SuggestionPolicies] ([Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261026180608_AddNinjaEvaluationSuggestions'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026180608_AddNinjaEvaluationSuggestions', N'10.0.11');
END;

COMMIT;
GO

