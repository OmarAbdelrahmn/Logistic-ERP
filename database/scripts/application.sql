IF OBJECT_ID(N'[migration].[__ApplicationMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'migration') IS NULL EXEC(N'CREATE SCHEMA [migration];');
    CREATE TABLE [migration].[__ApplicationMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___ApplicationMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    IF SCHEMA_ID(N'audit') IS NULL EXEC(N'CREATE SCHEMA [audit];');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    IF SCHEMA_ID(N'app') IS NULL EXEC(N'CREATE SCHEMA [app];');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    IF SCHEMA_ID(N'platform') IS NULL EXEC(N'CREATE SCHEMA [platform];');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE SEQUENCE [audit].[AuditEntrySequence] START WITH 1 INCREMENT BY 1 NO CYCLE;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [audit].[AuditEntries] (
        [Id] uniqueidentifier NOT NULL,
        [Sequence] bigint NOT NULL DEFAULT (NEXT VALUE FOR [audit].[AuditEntrySequence]),
        [EventId] uniqueidentifier NOT NULL,
        [ActorUserId] uniqueidentifier NULL,
        [ActorType] nvarchar(50) NOT NULL,
        [SessionId] uniqueidentifier NULL,
        [SupportAccessGrantId] uniqueidentifier NULL,
        [Action] nvarchar(150) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [EntityType] nvarchar(150) NOT NULL,
        [EntityId] uniqueidentifier NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [CorrelationId] nvarchar(100) NOT NULL,
        [TraceId] nvarchar(100) NULL,
        [IpAddress] nvarchar(64) NULL,
        [UserAgent] nvarchar(1000) NULL,
        [Reason] nvarchar(1000) NULL,
        [BeforeJson] nvarchar(max) NULL,
        [AfterJson] nvarchar(max) NULL,
        [Source] nvarchar(100) NOT NULL,
        [PreviousHash] nchar(64) NULL,
        [CurrentHash] nchar(64) NULL,
        [SchemaVersion] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_AuditEntries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [platform].[ClientPlatforms] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [DescriptionAr] nvarchar(500) NULL,
        [DescriptionEn] nvarchar(500) NULL,
        [LogoAssetKey] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(4000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_ClientPlatforms] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [platform].[CompanyProfile] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [LegalNameAr] nvarchar(200) NOT NULL,
        [LegalNameEn] nvarchar(200) NOT NULL,
        [DisplayNameAr] nvarchar(200) NOT NULL,
        [DisplayNameEn] nvarchar(200) NOT NULL,
        [CommercialRegistrationNumber] nvarchar(100) NULL,
        [UnifiedNationalNumber] nvarchar(100) NULL,
        [VatNumber] nvarchar(100) NULL,
        [ContactPhone] nvarchar(32) NOT NULL,
        [ContactEmail] nvarchar(320) NULL,
        [AddressBuildingNumber] nvarchar(32) NULL,
        [AddressStreet] nvarchar(200) NULL,
        [AddressDistrict] nvarchar(200) NULL,
        [AddressCity] nvarchar(200) NULL,
        [AddressPostalCode] nvarchar(32) NULL,
        [AddressAdditionalNumber] nvarchar(32) NULL,
        [LogoAssetKey] nvarchar(500) NULL,
        [DefaultLocale] nvarchar(10) NOT NULL,
        [TimeZoneId] nvarchar(100) NOT NULL,
        [NextEmployeeSequence] bigint NOT NULL,
        [Status] int NOT NULL,
        [SuspensionReason] nvarchar(500) NULL,
        [SuspendedAtUtc] datetimeoffset NULL,
        [SuspendedByUserId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_CompanyProfile] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CompanyProfile_SingleRow] CHECK ([Id] = '019c18d5-62e1-7000-8000-000000000001')
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[DatasetVersions] (
        [Id] uniqueidentifier NOT NULL,
        [ModuleKey] nvarchar(100) NOT NULL,
        [Version] bigint NOT NULL,
        [LastChangedAtUtc] datetimeoffset NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_DatasetVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DatasetVersions_Version] CHECK ([Version] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [platform].[DocumentTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [DescriptionAr] nvarchar(500) NULL,
        [DescriptionEn] nvarchar(500) NULL,
        [AppliesToSponsoredInternal] bit NOT NULL,
        [AppliesToOutsideRider] bit NOT NULL,
        [AppliesToRiderProfile] bit NOT NULL,
        [RequiresNumber] bit NOT NULL,
        [RequiresIssueDate] bit NOT NULL,
        [RequiresExpiryDate] bit NOT NULL,
        [RequiresFile] bit NOT NULL,
        [AllowedMimeTypes] nvarchar(500) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_DocumentTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[Employees] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeNumber] nvarchar(32) NOT NULL,
        [FullNameAr] nvarchar(200) NOT NULL,
        [FullNameEn] nvarchar(200) NOT NULL,
        [NormalizedNameAr] nvarchar(200) NOT NULL,
        [NormalizedNameEn] nvarchar(200) NOT NULL,
        [PrimaryPhone] nvarchar(32) NOT NULL,
        [CurrentStatus] int NOT NULL,
        [CurrentRelationshipType] int NULL,
        [Notes] nvarchar(4000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[ExportJobs] (
        [Id] uniqueidentifier NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [ReportType] nvarchar(100) NOT NULL,
        [ReportVersion] int NOT NULL,
        [ScopeSnapshotJson] nvarchar(max) NOT NULL,
        [FilterSnapshotJson] nvarchar(max) NOT NULL,
        [Format] int NOT NULL,
        [IncludesSensitiveValues] bit NOT NULL,
        [SensitiveExportReason] nvarchar(1000) NULL,
        [Status] int NOT NULL,
        [ProgressPercentage] int NOT NULL,
        [RequestedAtUtc] datetimeoffset NOT NULL,
        [StartedAtUtc] datetimeoffset NULL,
        [CompletedAtUtc] datetimeoffset NULL,
        [ArtifactPath] nvarchar(1000) NULL,
        [ArtifactChecksum] nchar(64) NULL,
        [ArtifactSizeBytes] bigint NULL,
        [ArtifactExpiresAtUtc] datetimeoffset NULL,
        [ErrorCode] nvarchar(100) NULL,
        [ErrorDetails] nvarchar(4000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_ExportJobs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ExportJobs_ProgressPercentage] CHECK ([ProgressPercentage] >= 0 AND [ProgressPercentage] <= 100)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [platform].[GlobalCities] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [RegionAr] nvarchar(200) NOT NULL,
        [RegionEn] nvarchar(200) NOT NULL,
        [CountryCode] nchar(2) NOT NULL,
        [Latitude] decimal(9,6) NULL,
        [Longitude] decimal(9,6) NULL,
        [DisplayOrder] int NOT NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_GlobalCities] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[JobTitles] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [DescriptionAr] nvarchar(500) NULL,
        [DescriptionEn] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_JobTitles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[LeaveTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [NameAr] nvarchar(150) NOT NULL,
        [NameEn] nvarchar(150) NOT NULL,
        [DescriptionAr] nvarchar(500) NULL,
        [DescriptionEn] nvarchar(500) NULL,
        [RequiresBalance] bit NOT NULL,
        [RequiresHrDocuments] bit NOT NULL,
        [RequiresExitReentryVisa] bit NOT NULL,
        [MaximumCalendarDays] int NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_LeaveTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LeaveTypes_MaximumCalendarDays] CHECK ([MaximumCalendarDays] IS NULL OR [MaximumCalendarDays] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [RecipientUserId] uniqueidentifier NOT NULL,
        [EventType] nvarchar(100) NOT NULL,
        [Severity] int NOT NULL,
        [TitleAr] nvarchar(250) NOT NULL,
        [TitleEn] nvarchar(250) NOT NULL,
        [BodyAr] nvarchar(2000) NOT NULL,
        [BodyEn] nvarchar(2000) NOT NULL,
        [SourceEntityType] nvarchar(100) NULL,
        [SourceEntityId] uniqueidentifier NULL,
        [DeepLink] nvarchar(1000) NULL,
        [ScopeSnapshotJson] nvarchar(max) NULL,
        [DeduplicationKey] nvarchar(200) NOT NULL,
        [VisibleAtUtc] datetimeoffset NOT NULL,
        [ExpiresAtUtc] datetimeoffset NULL,
        [ReadAtUtc] datetimeoffset NULL,
        [AcknowledgedAtUtc] datetimeoffset NULL,
        [AcknowledgedByUserId] uniqueidentifier NULL,
        [ArchivedAtUtc] datetimeoffset NULL,
        [ArchivedByUserId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [platform].[PermissionDefinitions] (
        [Id] uniqueidentifier NOT NULL,
        [Key] nvarchar(150) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [DescriptionAr] nvarchar(500) NOT NULL,
        [DescriptionEn] nvarchar(500) NOT NULL,
        [RequiresHousingScope] bit NOT NULL,
        [RequiresClientScope] bit NOT NULL,
        [IsSensitive] bit NOT NULL,
        [IsHighTrust] bit NOT NULL,
        [GrantabilityRule] nvarchar(500) NULL,
        [Version] int NOT NULL,
        [IsDeprecated] bit NOT NULL,
        [ReplacementKey] nvarchar(150) NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_PermissionDefinitions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[SavedViews] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ModuleKey] nvarchar(100) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [SchemaVersion] int NOT NULL,
        [FiltersJson] nvarchar(max) NOT NULL,
        [SortingJson] nvarchar(max) NOT NULL,
        [ColumnsJson] nvarchar(max) NOT NULL,
        [ColumnOrderJson] nvarchar(max) NOT NULL,
        [Density] nvarchar(32) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_SavedViews] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[Tags] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(100) NOT NULL,
        [NameEn] nvarchar(100) NOT NULL,
        [Color] nvarchar(32) NOT NULL,
        [AppliesToEmployees] bit NOT NULL,
        [AppliesToHousing] bit NOT NULL,
        [AppliesToClientContracts] bit NOT NULL,
        [AppliesToPlatformAccounts] bit NOT NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_Tags] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[ClientContracts] (
        [Id] uniqueidentifier NOT NULL,
        [ClientPlatformId] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [DisplayNameAr] nvarchar(200) NOT NULL,
        [DisplayNameEn] nvarchar(200) NOT NULL,
        [ExternalBusinessAccountId] nvarchar(150) NULL,
        [StartDate] date NULL,
        [EndDate] date NULL,
        [Status] int NOT NULL,
        [StatusReason] nvarchar(500) NULL,
        [ContactName] nvarchar(200) NULL,
        [ContactPhone] nvarchar(32) NULL,
        [ContactEmail] nvarchar(320) NULL,
        [Notes] nvarchar(4000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_ClientContracts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ClientContracts_DateRange] CHECK ([EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]),
        CONSTRAINT [FK_ClientContracts_ClientPlatforms_ClientPlatformId] FOREIGN KEY ([ClientPlatformId]) REFERENCES [platform].[ClientPlatforms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[DocumentRequirements] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentTypeId] uniqueidentifier NOT NULL,
        [RelationshipType] int NULL,
        [AppliesToRiderProfile] bit NOT NULL,
        [IsRequired] bit NOT NULL,
        [ReminderOffsetsDays] nvarchar(100) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_DocumentRequirements] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DocumentRequirements_EffectiveRange] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [FK_DocumentRequirements_DocumentTypes_DocumentTypeId] FOREIGN KEY ([DocumentTypeId]) REFERENCES [platform].[DocumentTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[EmployeeAbsenceComplianceCases] (
        [Id] uniqueidentifier NOT NULL,
        [CaseNumber] nvarchar(32) NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [AbsenceDate] date NOT NULL,
        [CurrentPath] int NOT NULL,
        [Status] int NOT NULL,
        [ReportedToAuthoritiesDate] date NULL,
        [AuthorityReportReference] nvarchar(150) NULL,
        [ExitOrOutageDate] date NULL,
        [ExitVisaNumber] nvarchar(150) NULL,
        [RemovalDeadline] date NOT NULL,
        [Notes] nvarchar(4000) NULL,
        [ResolvedAtUtc] datetimeoffset NULL,
        [ResolvedByUserId] uniqueidentifier NULL,
        [ResolutionCode] nvarchar(100) NULL,
        [ResolutionNotes] nvarchar(2000) NULL,
        [ClosedAtUtc] datetimeoffset NULL,
        [ClosedByUserId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_EmployeeAbsenceComplianceCases] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeAbsenceComplianceCases_Deadline] CHECK (([CurrentPath] = 1 AND [RemovalDeadline] >= [ReportedToAuthoritiesDate]) OR ([CurrentPath] = 2 AND [RemovalDeadline] >= [ExitOrOutageDate])),
        CONSTRAINT [CK_EmployeeAbsenceComplianceCases_PathData] CHECK (([CurrentPath] = 1 AND [ReportedToAuthoritiesDate] IS NOT NULL AND [ExitOrOutageDate] IS NULL) OR ([CurrentPath] = 2 AND [ExitOrOutageDate] IS NOT NULL AND [ReportedToAuthoritiesDate] IS NULL)),
        CONSTRAINT [FK_EmployeeAbsenceComplianceCases_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[EmployeeRelationshipPeriods] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [RelationshipType] int NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [ReasonCode] nvarchar(100) NULL,
        [Reason] nvarchar(1000) NULL,
        [SourceReference] nvarchar(200) NULL,
        [ChangedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EmployeeRelationshipPeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeRelationshipPeriods_EffectiveRange] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [FK_EmployeeRelationshipPeriods_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[EmployeeStatusPeriods] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [ReasonCode] nvarchar(100) NULL,
        [Reason] nvarchar(1000) NULL,
        [ChangedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EmployeeStatusPeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeStatusPeriods_EffectiveRange] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [FK_EmployeeStatusPeriods_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[OutsideRiderDetails] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [NationalityCountryCode] nchar(2) NULL,
        [AlternateContactName] nvarchar(200) NULL,
        [AlternateContactPhone] nvarchar(32) NULL,
        [EngagementReference] nvarchar(200) NULL,
        [EngagementNotes] nvarchar(4000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_OutsideRiderDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OutsideRiderDetails_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[Housing] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [CityId] uniqueidentifier NOT NULL,
        [AddressBuildingNumber] nvarchar(32) NULL,
        [AddressStreet] nvarchar(200) NULL,
        [AddressDistrict] nvarchar(200) NULL,
        [AddressCity] nvarchar(200) NULL,
        [AddressPostalCode] nvarchar(32) NULL,
        [AddressAdditionalNumber] nvarchar(32) NULL,
        [Latitude] decimal(9,6) NULL,
        [Longitude] decimal(9,6) NULL,
        [TotalCapacity] int NOT NULL,
        [ContactPhone] nvarchar(32) NULL,
        [OpenedDate] date NULL,
        [ClosedDate] date NULL,
        [Status] int NOT NULL,
        [StatusReason] nvarchar(500) NULL,
        [Notes] nvarchar(4000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_Housing] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Housing_DateRange] CHECK ([ClosedDate] IS NULL OR [OpenedDate] IS NULL OR [ClosedDate] >= [OpenedDate]),
        CONSTRAINT [CK_Housing_TotalCapacity] CHECK ([TotalCapacity] > 0),
        CONSTRAINT [FK_Housing_GlobalCities_CityId] FOREIGN KEY ([CityId]) REFERENCES [platform].[GlobalCities] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[OperatingCities] (
        [Id] uniqueidentifier NOT NULL,
        [GlobalCityId] uniqueidentifier NOT NULL,
        [EnabledFrom] date NOT NULL,
        [DisabledAt] date NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_OperatingCities] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OperatingCities_DateRange] CHECK ([DisabledAt] IS NULL OR [DisabledAt] >= [EnabledFrom]),
        CONSTRAINT [FK_OperatingCities_GlobalCities_GlobalCityId] FOREIGN KEY ([GlobalCityId]) REFERENCES [platform].[GlobalCities] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[EmployeeJobTitlePeriods] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [JobTitleId] uniqueidentifier NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [Reason] nvarchar(1000) NULL,
        [ChangedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EmployeeJobTitlePeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeJobTitlePeriods_EffectiveRange] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [FK_EmployeeJobTitlePeriods_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeJobTitlePeriods_JobTitles_JobTitleId] FOREIGN KEY ([JobTitleId]) REFERENCES [app].[JobTitles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[LeaveApprovalWorkflows] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [NameAr] nvarchar(150) NOT NULL,
        [NameEn] nvarchar(150) NOT NULL,
        [Version] int NOT NULL,
        [LeaveTypeId] uniqueidentifier NULL,
        [RelationshipType] int NULL,
        [AppliesToRider] bit NULL,
        [ClientPlatformId] uniqueidentifier NULL,
        [Priority] int NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_LeaveApprovalWorkflows] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LeaveApprovalWorkflows_DateRange] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [CK_LeaveApprovalWorkflows_Version] CHECK ([Version] > 0),
        CONSTRAINT [FK_LeaveApprovalWorkflows_ClientPlatforms_ClientPlatformId] FOREIGN KEY ([ClientPlatformId]) REFERENCES [platform].[ClientPlatforms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LeaveApprovalWorkflows_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [app].[LeaveTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[EmployeeTags] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [TagId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_EmployeeTags] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeTags_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeTags_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [app].[Tags] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[ClientContractTags] (
        [Id] uniqueidentifier NOT NULL,
        [ClientContractId] uniqueidentifier NOT NULL,
        [TagId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_ClientContractTags] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClientContractTags_ClientContracts_ClientContractId] FOREIGN KEY ([ClientContractId]) REFERENCES [app].[ClientContracts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ClientContractTags_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [app].[Tags] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[PlatformRiderAccounts] (
        [Id] uniqueidentifier NOT NULL,
        [ClientContractId] uniqueidentifier NOT NULL,
        [ClientPlatformId] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [ExternalAccountId] nvarchar(150) NOT NULL,
        [NormalizedExternalAccountId] nvarchar(150) NOT NULL,
        [UserName] nvarchar(150) NULL,
        [LabelAr] nvarchar(200) NULL,
        [LabelEn] nvarchar(200) NULL,
        [Status] int NOT NULL,
        [StatusReason] nvarchar(500) NULL,
        [AcquisitionDate] date NULL,
        [StartDate] date NULL,
        [EndDate] date NULL,
        [OwnershipNotes] nvarchar(4000) NULL,
        [OperationalNotes] nvarchar(4000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_PlatformRiderAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PlatformRiderAccounts_DateRange] CHECK ([EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]),
        CONSTRAINT [FK_PlatformRiderAccounts_ClientContracts_ClientContractId] FOREIGN KEY ([ClientContractId]) REFERENCES [app].[ClientContracts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlatformRiderAccounts_ClientPlatforms_ClientPlatformId] FOREIGN KEY ([ClientPlatformId]) REFERENCES [platform].[ClientPlatforms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[EmployeeAbsenceComplianceCaseEvents] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeAbsenceComplianceCaseId] uniqueidentifier NOT NULL,
        [EventType] int NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [BeforeJson] nvarchar(max) NULL,
        [AfterJson] nvarchar(max) NOT NULL,
        [CorrelationId] nvarchar(100) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EmployeeAbsenceComplianceCaseEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeAbsenceComplianceCaseEvents_EmployeeAbsenceComplianceCases_EmployeeAbsenceComplianceCaseId] FOREIGN KEY ([EmployeeAbsenceComplianceCaseId]) REFERENCES [app].[EmployeeAbsenceComplianceCases] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[EmployeeStatusChangeRequests] (
        [Id] uniqueidentifier NOT NULL,
        [RequestNumber] nvarchar(32) NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [FromStatus] int NOT NULL,
        [RequestedStatus] int NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [Status] int NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [RequestedAtUtc] datetimeoffset NOT NULL,
        [ResolvedByUserId] uniqueidentifier NULL,
        [ResolvedAtUtc] datetimeoffset NULL,
        [ResolutionReason] nvarchar(1000) NULL,
        [ResultingStatusPeriodId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_EmployeeStatusChangeRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeStatusChangeRequests_StatusChanged] CHECK ([FromStatus] <> [RequestedStatus]),
        CONSTRAINT [FK_EmployeeStatusChangeRequests_EmployeeStatusPeriods_ResultingStatusPeriodId] FOREIGN KEY ([ResultingStatusPeriodId]) REFERENCES [app].[EmployeeStatusPeriods] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeStatusChangeRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[HousingResidencePeriods] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [HousingId] uniqueidentifier NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [MoveInReason] nvarchar(1000) NULL,
        [MoveOutReason] nvarchar(1000) NULL,
        [SourceReference] nvarchar(200) NULL,
        [DestinationReference] nvarchar(200) NULL,
        [CapacityOverrideUsed] bit NOT NULL,
        [CapacityOverrideReason] nvarchar(1000) NULL,
        [AssignedByUserId] uniqueidentifier NOT NULL,
        [EndedByUserId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_HousingResidencePeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_HousingResidencePeriods_CapacityOverrideReason] CHECK ([CapacityOverrideUsed] = 0 OR [CapacityOverrideReason] IS NOT NULL),
        CONSTRAINT [CK_HousingResidencePeriods_EffectiveRange] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [FK_HousingResidencePeriods_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HousingResidencePeriods_Housing_HousingId] FOREIGN KEY ([HousingId]) REFERENCES [app].[Housing] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[HousingSupervisorPeriods] (
        [Id] uniqueidentifier NOT NULL,
        [HousingId] uniqueidentifier NOT NULL,
        [SupervisorEmployeeId] uniqueidentifier NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [AssignmentReason] nvarchar(1000) NULL,
        [EndReason] nvarchar(1000) NULL,
        [AssignedByUserId] uniqueidentifier NOT NULL,
        [EndedByUserId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_HousingSupervisorPeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_HousingSupervisorPeriods_EffectiveRange] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [FK_HousingSupervisorPeriods_Employees_SupervisorEmployeeId] FOREIGN KEY ([SupervisorEmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HousingSupervisorPeriods_Housing_HousingId] FOREIGN KEY ([HousingId]) REFERENCES [app].[Housing] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[HousingTags] (
        [Id] uniqueidentifier NOT NULL,
        [HousingId] uniqueidentifier NOT NULL,
        [TagId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_HousingTags] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HousingTags_Housing_HousingId] FOREIGN KEY ([HousingId]) REFERENCES [app].[Housing] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HousingTags_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [app].[Tags] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[LeaveApprovalWorkflowSteps] (
        [Id] uniqueidentifier NOT NULL,
        [LeaveApprovalWorkflowId] uniqueidentifier NOT NULL,
        [StepKey] nvarchar(100) NOT NULL,
        [Sequence] int NOT NULL,
        [NameAr] nvarchar(150) NOT NULL,
        [NameEn] nvarchar(150) NOT NULL,
        [RequiredPermissionKey] nvarchar(150) NOT NULL,
        [ScopeSource] int NOT NULL,
        [AllowsReturnForChanges] bit NOT NULL,
        [RequiresCommentOnApproval] bit NOT NULL,
        [TargetResponseHours] int NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_LeaveApprovalWorkflowSteps] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LeaveApprovalWorkflowSteps_Sequence] CHECK ([Sequence] > 0),
        CONSTRAINT [CK_LeaveApprovalWorkflowSteps_TargetHours] CHECK ([TargetResponseHours] IS NULL OR [TargetResponseHours] > 0),
        CONSTRAINT [FK_LeaveApprovalWorkflowSteps_LeaveApprovalWorkflows_LeaveApprovalWorkflowId] FOREIGN KEY ([LeaveApprovalWorkflowId]) REFERENCES [app].[LeaveApprovalWorkflows] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[LeaveRequests] (
        [Id] uniqueidentifier NOT NULL,
        [RequestNumber] nvarchar(32) NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [LeaveTypeId] uniqueidentifier NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [ExpectedReturnDate] date NOT NULL,
        [CalendarDays] int NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [DestinationCountryCode] nchar(2) NULL,
        [ContactPhoneDuringLeave] nvarchar(32) NULL,
        [EmergencyContactName] nvarchar(200) NULL,
        [EmergencyContactPhone] nvarchar(32) NULL,
        [Status] int NOT NULL,
        [ApprovalWorkflowId] uniqueidentifier NULL,
        [ApprovalWorkflowVersion] int NULL,
        [ApprovalWorkflowSnapshotJson] nvarchar(max) NULL,
        [CurrentApprovalStepKey] nvarchar(100) NULL,
        [CurrentApprovalStepSequence] int NULL,
        [HrStatus] int NOT NULL,
        [SubmittedAtUtc] datetimeoffset NULL,
        [ApprovedAtUtc] datetimeoffset NULL,
        [ApprovedByUserId] uniqueidentifier NULL,
        [RejectedAtUtc] datetimeoffset NULL,
        [RejectedByUserId] uniqueidentifier NULL,
        [RejectionReason] nvarchar(1000) NULL,
        [ActivatedAtUtc] datetimeoffset NULL,
        [CompletedAtUtc] datetimeoffset NULL,
        [CancelledAtUtc] datetimeoffset NULL,
        [CancelledByUserId] uniqueidentifier NULL,
        [CancellationReason] nvarchar(1000) NULL,
        [RelatedClientContractId] uniqueidentifier NULL,
        [Notes] nvarchar(4000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_LeaveRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LeaveRequests_CalendarDays] CHECK ([CalendarDays] = DATEDIFF(DAY, [StartDate], [EndDate]) + 1),
        CONSTRAINT [CK_LeaveRequests_DateRange] CHECK ([EndDate] >= [StartDate]),
        CONSTRAINT [CK_LeaveRequests_ExpectedReturn] CHECK ([ExpectedReturnDate] >= [EndDate]),
        CONSTRAINT [FK_LeaveRequests_ClientContracts_RelatedClientContractId] FOREIGN KEY ([RelatedClientContractId]) REFERENCES [app].[ClientContracts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LeaveRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LeaveRequests_LeaveApprovalWorkflows_ApprovalWorkflowId] FOREIGN KEY ([ApprovalWorkflowId]) REFERENCES [app].[LeaveApprovalWorkflows] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LeaveRequests_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [app].[LeaveTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[PlatformAccountCredentialVersions] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [Ciphertext] varbinary(max) NOT NULL,
        [Nonce] varbinary(32) NOT NULL,
        [AuthenticationTag] varbinary(32) NOT NULL,
        [KeyVersion] int NOT NULL,
        [RotatedAtUtc] datetimeoffset NOT NULL,
        [RotatedByUserId] uniqueidentifier NOT NULL,
        [SupersededVersionId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_PlatformAccountCredentialVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlatformAccountCredentialVersions_PlatformAccountCredentialVersions_SupersededVersionId] FOREIGN KEY ([SupersededVersionId]) REFERENCES [app].[PlatformAccountCredentialVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlatformAccountCredentialVersions_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[PlatformRiderAccountTags] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [TagId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_PlatformRiderAccountTags] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlatformRiderAccountTags_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlatformRiderAccountTags_Tags_TagId] FOREIGN KEY ([TagId]) REFERENCES [app].[Tags] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[LeaveApprovalDecisions] (
        [Id] uniqueidentifier NOT NULL,
        [LeaveRequestId] uniqueidentifier NOT NULL,
        [StepKey] nvarchar(100) NOT NULL,
        [StepSequence] int NOT NULL,
        [RequiredPermissionKey] nvarchar(150) NOT NULL,
        [DecidedByUserId] uniqueidentifier NOT NULL,
        [DecidedAtUtc] datetimeoffset NOT NULL,
        [Decision] int NOT NULL,
        [FromStatus] int NOT NULL,
        [ToStatus] int NOT NULL,
        [ReturnedToStepKey] nvarchar(100) NULL,
        [Comment] nvarchar(2000) NOT NULL,
        [AuthorizationSnapshotJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_LeaveApprovalDecisions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LeaveApprovalDecisions_StepSequence] CHECK ([StepSequence] > 0),
        CONSTRAINT [FK_LeaveApprovalDecisions_LeaveRequests_LeaveRequestId] FOREIGN KEY ([LeaveRequestId]) REFERENCES [app].[LeaveRequests] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[LeaveCancellationRequests] (
        [Id] uniqueidentifier NOT NULL,
        [LeaveRequestId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [Status] int NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [RequestedAtUtc] datetimeoffset NOT NULL,
        [ResolvedByUserId] uniqueidentifier NULL,
        [ResolvedAtUtc] datetimeoffset NULL,
        [ResolutionReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_LeaveCancellationRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LeaveCancellationRequests_LeaveRequests_LeaveRequestId] FOREIGN KEY ([LeaveRequestId]) REFERENCES [app].[LeaveRequests] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[LeaveDateChangeRequests] (
        [Id] uniqueidentifier NOT NULL,
        [LeaveRequestId] uniqueidentifier NOT NULL,
        [PreviousStartDate] date NOT NULL,
        [PreviousEndDate] date NOT NULL,
        [RequestedStartDate] date NOT NULL,
        [RequestedEndDate] date NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [Status] int NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [RequestedAtUtc] datetimeoffset NOT NULL,
        [ResolvedByUserId] uniqueidentifier NULL,
        [ResolvedAtUtc] datetimeoffset NULL,
        [ResolutionReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_LeaveDateChangeRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LeaveDateChangeRequests_PreviousRange] CHECK ([PreviousEndDate] >= [PreviousStartDate]),
        CONSTRAINT [CK_LeaveDateChangeRequests_RequestedRange] CHECK ([RequestedEndDate] >= [RequestedStartDate]),
        CONSTRAINT [FK_LeaveDateChangeRequests_LeaveRequests_LeaveRequestId] FOREIGN KEY ([LeaveRequestId]) REFERENCES [app].[LeaveRequests] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[EmployeeDocuments] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [DocumentTypeId] uniqueidentifier NOT NULL,
        [DocumentNumber] nvarchar(150) NULL,
        [NormalizedDocumentNumber] nvarchar(150) NULL,
        [IssuingCountryCode] nchar(2) NULL,
        [IssuingAuthority] nvarchar(200) NULL,
        [IssueDate] date NULL,
        [ExpiryDate] date NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(4000) NULL,
        [CurrentVersionId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_EmployeeDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeDocuments_DateRange] CHECK ([ExpiryDate] IS NULL OR [IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]),
        CONSTRAINT [FK_EmployeeDocuments_DocumentTypes_DocumentTypeId] FOREIGN KEY ([DocumentTypeId]) REFERENCES [platform].[DocumentTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeDocuments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[EmployeeDocumentVersions] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeDocumentId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [StoredFileName] nvarchar(255) NOT NULL,
        [ContentType] nvarchar(150) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [Sha256Checksum] nchar(64) NOT NULL,
        [StoragePath] nvarchar(1000) NOT NULL,
        [UploadedByUserId] uniqueidentifier NOT NULL,
        [UploadedAtUtc] datetimeoffset NOT NULL,
        [SupersededVersionId] uniqueidentifier NULL,
        [PreviewStatus] nvarchar(50) NULL,
        [PreviewStoragePath] nvarchar(1000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EmployeeDocumentVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeDocumentVersions_FileSize] CHECK ([FileSizeBytes] > 0),
        CONSTRAINT [CK_EmployeeDocumentVersions_VersionNumber] CHECK ([VersionNumber] > 0),
        CONSTRAINT [FK_EmployeeDocumentVersions_EmployeeDocumentVersions_SupersededVersionId] FOREIGN KEY ([SupersededVersionId]) REFERENCES [app].[EmployeeDocumentVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeDocumentVersions_EmployeeDocuments_EmployeeDocumentId] FOREIGN KEY ([EmployeeDocumentId]) REFERENCES [app].[EmployeeDocuments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[RiderProfiles] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [RiderStartDate] date NULL,
        [RiderEndDate] date NULL,
        [PreferredCityId] uniqueidentifier NULL,
        [LicenseDocumentId] uniqueidentifier NULL,
        [OperationalNotes] nvarchar(4000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_RiderProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RiderProfiles_DateRange] CHECK ([RiderEndDate] IS NULL OR [RiderStartDate] IS NULL OR [RiderEndDate] >= [RiderStartDate]),
        CONSTRAINT [FK_RiderProfiles_EmployeeDocuments_LicenseDocumentId] FOREIGN KEY ([LicenseDocumentId]) REFERENCES [app].[EmployeeDocuments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderProfiles_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderProfiles_GlobalCities_PreferredCityId] FOREIGN KEY ([PreferredCityId]) REFERENCES [platform].[GlobalCities] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[SponsoredInternalDetails] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [Gender] int NULL,
        [BirthDate] date NULL,
        [NationalityCountryCode] nchar(2) NULL,
        [SecondaryPhone] nvarchar(32) NULL,
        [Email] nvarchar(320) NULL,
        [ProfilePhotoDocumentId] uniqueidentifier NULL,
        [MaritalStatus] int NULL,
        [DependentsCount] int NULL,
        [EducationLevel] nvarchar(100) NULL,
        [EducationDetails] nvarchar(1000) NULL,
        [Profession] nvarchar(200) NULL,
        [HomeAddressBuildingNumber] nvarchar(32) NULL,
        [HomeAddressStreet] nvarchar(200) NULL,
        [HomeAddressDistrict] nvarchar(200) NULL,
        [HomeAddressCity] nvarchar(200) NULL,
        [HomeAddressPostalCode] nvarchar(32) NULL,
        [HomeAddressAdditionalNumber] nvarchar(32) NULL,
        [EmergencyContactName] nvarchar(200) NULL,
        [EmergencyContactRelationship] nvarchar(100) NULL,
        [EmergencyContactPhone] nvarchar(32) NULL,
        [HireDate] date NULL,
        [ContractStartDate] date NULL,
        [ContractEndDate] date NULL,
        [ProbationEndDate] date NULL,
        [TerminationDate] date NULL,
        [ManagerEmployeeId] uniqueidentifier NULL,
        [SponsorLegalReference] nvarchar(200) NULL,
        [CurrentJobTitleId] uniqueidentifier NULL,
        [InternalNotes] nvarchar(4000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_SponsoredInternalDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SponsoredInternalDetails_ContractRange] CHECK ([ContractEndDate] IS NULL OR [ContractStartDate] IS NULL OR [ContractEndDate] >= [ContractStartDate]),
        CONSTRAINT [CK_SponsoredInternalDetails_Dependents] CHECK ([DependentsCount] IS NULL OR [DependentsCount] >= 0),
        CONSTRAINT [FK_SponsoredInternalDetails_EmployeeDocuments_ProfilePhotoDocumentId] FOREIGN KEY ([ProfilePhotoDocumentId]) REFERENCES [app].[EmployeeDocuments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SponsoredInternalDetails_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SponsoredInternalDetails_Employees_ManagerEmployeeId] FOREIGN KEY ([ManagerEmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SponsoredInternalDetails_JobTitles_CurrentJobTitleId] FOREIGN KEY ([CurrentJobTitleId]) REFERENCES [app].[JobTitles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[RiderClientAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [ClientContractId] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [Status] int NOT NULL,
        [StartReason] nvarchar(1000) NULL,
        [EndReason] nvarchar(1000) NULL,
        [OperationalAgreementReference] nvarchar(200) NULL,
        [OperationalAgreementNotes] nvarchar(4000) NULL,
        [AssignedByUserId] uniqueidentifier NOT NULL,
        [EndedByUserId] uniqueidentifier NULL,
        [WasBackdated] bit NOT NULL,
        [BackdatedReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_RiderClientAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RiderClientAssignments_BackdatedReason] CHECK ([WasBackdated] = 0 OR [BackdatedReason] IS NOT NULL),
        CONSTRAINT [CK_RiderClientAssignments_EffectiveRange] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [FK_RiderClientAssignments_ClientContracts_ClientContractId] FOREIGN KEY ([ClientContractId]) REFERENCES [app].[ClientContracts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderClientAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderClientAssignments_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderClientAssignments_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[RiderAssignmentEvents] (
        [Id] uniqueidentifier NOT NULL,
        [RiderClientAssignmentId] uniqueidentifier NOT NULL,
        [FromStatus] int NOT NULL,
        [ToStatus] int NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [ChangeSnapshotJson] nvarchar(max) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_RiderAssignmentEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RiderAssignmentEvents_RiderClientAssignments_RiderClientAssignmentId] FOREIGN KEY ([RiderClientAssignmentId]) REFERENCES [app].[RiderClientAssignments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[LeaveRequestDocuments] (
        [Id] uniqueidentifier NOT NULL,
        [LeaveRequestId] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [ReferenceNumber] nvarchar(150) NULL,
        [IssuedOn] date NULL,
        [ExpiresOn] date NULL,
        [CurrentVersionId] uniqueidentifier NULL,
        [Notes] nvarchar(2000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_LeaveRequestDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LeaveRequestDocuments_DateRange] CHECK ([ExpiresOn] IS NULL OR [IssuedOn] IS NULL OR [ExpiresOn] >= [IssuedOn]),
        CONSTRAINT [FK_LeaveRequestDocuments_LeaveRequests_LeaveRequestId] FOREIGN KEY ([LeaveRequestId]) REFERENCES [app].[LeaveRequests] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE TABLE [app].[LeaveRequestDocumentVersions] (
        [Id] uniqueidentifier NOT NULL,
        [LeaveRequestDocumentId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [ContentType] nvarchar(150) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [Sha256Checksum] nchar(64) NOT NULL,
        [StoragePath] nvarchar(1000) NOT NULL,
        [UploadedByUserId] uniqueidentifier NOT NULL,
        [UploadedAtUtc] datetimeoffset NOT NULL,
        [SupersededVersionId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_LeaveRequestDocumentVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_LeaveRequestDocumentVersions_FileSize] CHECK ([FileSizeBytes] > 0),
        CONSTRAINT [CK_LeaveRequestDocumentVersions_Version] CHECK ([VersionNumber] > 0),
        CONSTRAINT [FK_LeaveRequestDocumentVersions_LeaveRequestDocumentVersions_SupersededVersionId] FOREIGN KEY ([SupersededVersionId]) REFERENCES [app].[LeaveRequestDocumentVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LeaveRequestDocumentVersions_LeaveRequestDocuments_LeaveRequestDocumentId] FOREIGN KEY ([LeaveRequestDocumentId]) REFERENCES [app].[LeaveRequestDocuments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CommercialRegistrationNumber', N'ContactEmail', N'ContactPhone', N'CreatedAtUtc', N'CreatedByUserId', N'DefaultLocale', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DisplayNameAr', N'DisplayNameEn', N'IsDeleted', N'LegalNameAr', N'LegalNameEn', N'LogoAssetKey', N'NextEmployeeSequence', N'Status', N'SuspendedAtUtc', N'SuspendedByUserId', N'SuspensionReason', N'TimeZoneId', N'UnifiedNationalNumber', N'UpdatedAtUtc', N'UpdatedByUserId', N'VatNumber') AND [object_id] = OBJECT_ID(N'[platform].[CompanyProfile]'))
        SET IDENTITY_INSERT [platform].[CompanyProfile] ON;
    EXEC(N'INSERT INTO [platform].[CompanyProfile] ([Id], [Code], [CommercialRegistrationNumber], [ContactEmail], [ContactPhone], [CreatedAtUtc], [CreatedByUserId], [DefaultLocale], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DisplayNameAr], [DisplayNameEn], [IsDeleted], [LegalNameAr], [LegalNameEn], [LogoAssetKey], [NextEmployeeSequence], [Status], [SuspendedAtUtc], [SuspendedByUserId], [SuspensionReason], [TimeZoneId], [UnifiedNationalNumber], [UpdatedAtUtc], [UpdatedByUserId], [VatNumber])
    VALUES (''019c18d5-62e1-7000-8000-000000000001'', N''ALBAWABA'', NULL, NULL, N'''', ''2026-01-01T00:00:00.0000000+00:00'', NULL, N''ar'', NULL, NULL, NULL, N''البوابة للخدمات اللوجستية'', N''Al Bawaba Logistics'', CAST(0 AS bit), N''البوابة للخدمات اللوجستية'', N''Al Bawaba Logistics Services'', NULL, CAST(1 AS bigint), 1, NULL, NULL, NULL, N''Asia/Riyadh'', NULL, NULL, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CommercialRegistrationNumber', N'ContactEmail', N'ContactPhone', N'CreatedAtUtc', N'CreatedByUserId', N'DefaultLocale', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DisplayNameAr', N'DisplayNameEn', N'IsDeleted', N'LegalNameAr', N'LegalNameEn', N'LogoAssetKey', N'NextEmployeeSequence', N'Status', N'SuspendedAtUtc', N'SuspendedByUserId', N'SuspensionReason', N'TimeZoneId', N'UnifiedNationalNumber', N'UpdatedAtUtc', N'UpdatedByUserId', N'VatNumber') AND [object_id] = OBJECT_ID(N'[platform].[CompanyProfile]'))
        SET IDENTITY_INSERT [platform].[CompanyProfile] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CountryCode', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DisplayOrder', N'IsDeleted', N'Latitude', N'Longitude', N'NameAr', N'NameEn', N'RegionAr', N'RegionEn', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[platform].[GlobalCities]'))
        SET IDENTITY_INSERT [platform].[GlobalCities] ON;
    EXEC(N'INSERT INTO [platform].[GlobalCities] ([Id], [Code], [CountryCode], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DisplayOrder], [IsDeleted], [Latitude], [Longitude], [NameAr], [NameEn], [RegionAr], [RegionEn], [Status], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-8000-000000000002'', N''JEDDAH'', N''SA'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, 1, CAST(0 AS bit), 21.4858, 39.1925, N''جدة'', N''Jeddah'', N''منطقة مكة المكرمة'', N''Makkah Region'', 1, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CountryCode', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DisplayOrder', N'IsDeleted', N'Latitude', N'Longitude', N'NameAr', N'NameEn', N'RegionAr', N'RegionEn', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[platform].[GlobalCities]'))
        SET IDENTITY_INSERT [platform].[GlobalCities] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DisabledAt', N'EnabledFrom', N'GlobalCityId', N'IsDeleted', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[app].[OperatingCities]'))
        SET IDENTITY_INSERT [app].[OperatingCities] ON;
    EXEC(N'INSERT INTO [app].[OperatingCities] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DisabledAt], [EnabledFrom], [GlobalCityId], [IsDeleted], [Status], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-8000-000000000003'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, NULL, ''2026-01-01'', ''019c18d5-62e1-7000-8000-000000000002'', CAST(0 AS bit), 1, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DisabledAt', N'EnabledFrom', N'GlobalCityId', N'IsDeleted', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[app].[OperatingCities]'))
        SET IDENTITY_INSERT [app].[OperatingCities] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_ActorUserId_OccurredAtUtc] ON [audit].[AuditEntries] ([ActorUserId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_EntityType_EntityId_OccurredAtUtc] ON [audit].[AuditEntries] ([EntityType], [EntityId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AuditEntries_EventId] ON [audit].[AuditEntries] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_AuditEntries_OccurredAtUtc] ON [audit].[AuditEntries] ([OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AuditEntries_Sequence] ON [audit].[AuditEntries] ([Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ClientContracts_ClientPlatformId_ExternalBusinessAccountId] ON [app].[ClientContracts] ([ClientPlatformId], [ExternalBusinessAccountId]) WHERE [ExternalBusinessAccountId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_ClientContracts_ClientPlatformId_Status] ON [app].[ClientContracts] ([ClientPlatformId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ClientContracts_Code] ON [app].[ClientContracts] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_ClientContracts_IsDeleted] ON [app].[ClientContracts] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ClientContractTags_ClientContractId_TagId] ON [app].[ClientContractTags] ([ClientContractId], [TagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_ClientContractTags_IsDeleted] ON [app].[ClientContractTags] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_ClientContractTags_TagId] ON [app].[ClientContractTags] ([TagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ClientPlatforms_Code] ON [platform].[ClientPlatforms] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_ClientPlatforms_IsDeleted] ON [platform].[ClientPlatforms] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CompanyProfile_Code] ON [platform].[CompanyProfile] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_CompanyProfile_IsDeleted] ON [platform].[CompanyProfile] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_DatasetVersions_IsDeleted] ON [app].[DatasetVersions] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DatasetVersions_ModuleKey] ON [app].[DatasetVersions] ([ModuleKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DocumentRequirements_DocumentTypeId_RelationshipType_AppliesToRiderProfile_EffectiveFrom] ON [app].[DocumentRequirements] ([DocumentTypeId], [RelationshipType], [AppliesToRiderProfile], [EffectiveFrom]) WHERE [RelationshipType] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_DocumentRequirements_IsDeleted] ON [app].[DocumentRequirements] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_DocumentRequirements_Status_EffectiveTo] ON [app].[DocumentRequirements] ([Status], [EffectiveTo]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DocumentTypes_Code] ON [platform].[DocumentTypes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_DocumentTypes_IsDeleted] ON [platform].[DocumentTypes] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeAbsenceComplianceCaseEvents_EmployeeAbsenceComplianceCaseId_OccurredAtUtc] ON [app].[EmployeeAbsenceComplianceCaseEvents] ([EmployeeAbsenceComplianceCaseId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployeeAbsenceComplianceCases_CaseNumber] ON [app].[EmployeeAbsenceComplianceCases] ([CaseNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeeAbsenceComplianceCases_EmployeeId] ON [app].[EmployeeAbsenceComplianceCases] ([EmployeeId]) WHERE [Status] IN (1, 2, 3, 4) AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeAbsenceComplianceCases_IsDeleted] ON [app].[EmployeeAbsenceComplianceCases] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeAbsenceComplianceCases_Status_RemovalDeadline] ON [app].[EmployeeAbsenceComplianceCases] ([Status], [RemovalDeadline]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeDocuments_CurrentVersionId] ON [app].[EmployeeDocuments] ([CurrentVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeeDocuments_DocumentTypeId_NormalizedDocumentNumber] ON [app].[EmployeeDocuments] ([DocumentTypeId], [NormalizedDocumentNumber]) WHERE [NormalizedDocumentNumber] IS NOT NULL AND [IsDeleted] = 0 AND [Status] <> 3');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeDocuments_EmployeeId_DocumentTypeId_Status] ON [app].[EmployeeDocuments] ([EmployeeId], [DocumentTypeId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeDocuments_IsDeleted] ON [app].[EmployeeDocuments] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployeeDocumentVersions_EmployeeDocumentId_VersionNumber] ON [app].[EmployeeDocumentVersions] ([EmployeeDocumentId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeDocumentVersions_Sha256Checksum] ON [app].[EmployeeDocumentVersions] ([Sha256Checksum]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeDocumentVersions_SupersededVersionId] ON [app].[EmployeeDocumentVersions] ([SupersededVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeeJobTitlePeriods_EmployeeId] ON [app].[EmployeeJobTitlePeriods] ([EmployeeId]) WHERE [EffectiveTo] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeJobTitlePeriods_EmployeeId_EffectiveFrom] ON [app].[EmployeeJobTitlePeriods] ([EmployeeId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeJobTitlePeriods_JobTitleId] ON [app].[EmployeeJobTitlePeriods] ([JobTitleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeeRelationshipPeriods_EmployeeId] ON [app].[EmployeeRelationshipPeriods] ([EmployeeId]) WHERE [EffectiveTo] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeRelationshipPeriods_EmployeeId_EffectiveFrom] ON [app].[EmployeeRelationshipPeriods] ([EmployeeId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_Employees_CurrentStatus] ON [app].[Employees] ([CurrentStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_EmployeeNumber] ON [app].[Employees] ([EmployeeNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_Employees_IsDeleted] ON [app].[Employees] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_Employees_NormalizedNameAr] ON [app].[Employees] ([NormalizedNameAr]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_Employees_NormalizedNameEn] ON [app].[Employees] ([NormalizedNameEn]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_Employees_PrimaryPhone] ON [app].[Employees] ([PrimaryPhone]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeeStatusChangeRequests_EmployeeId] ON [app].[EmployeeStatusChangeRequests] ([EmployeeId]) WHERE [Status] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeStatusChangeRequests_IsDeleted] ON [app].[EmployeeStatusChangeRequests] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployeeStatusChangeRequests_RequestNumber] ON [app].[EmployeeStatusChangeRequests] ([RequestNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeStatusChangeRequests_ResultingStatusPeriodId] ON [app].[EmployeeStatusChangeRequests] ([ResultingStatusPeriodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeStatusChangeRequests_Status_RequestedAtUtc] ON [app].[EmployeeStatusChangeRequests] ([Status], [RequestedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeeStatusPeriods_EmployeeId] ON [app].[EmployeeStatusPeriods] ([EmployeeId]) WHERE [EffectiveTo] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeStatusPeriods_EmployeeId_EffectiveFrom] ON [app].[EmployeeStatusPeriods] ([EmployeeId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployeeTags_EmployeeId_TagId] ON [app].[EmployeeTags] ([EmployeeId], [TagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeTags_IsDeleted] ON [app].[EmployeeTags] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_EmployeeTags_TagId] ON [app].[EmployeeTags] ([TagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_ExportJobs_IsDeleted] ON [app].[ExportJobs] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_ExportJobs_RequestedByUserId_RequestedAtUtc] ON [app].[ExportJobs] ([RequestedByUserId], [RequestedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_ExportJobs_Status_RequestedAtUtc] ON [app].[ExportJobs] ([Status], [RequestedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GlobalCities_Code] ON [platform].[GlobalCities] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_GlobalCities_IsDeleted] ON [platform].[GlobalCities] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_GlobalCities_NameAr_NameEn] ON [platform].[GlobalCities] ([NameAr], [NameEn]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_Housing_CityId] ON [app].[Housing] ([CityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Housing_Code] ON [app].[Housing] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_Housing_IsDeleted] ON [app].[Housing] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_Housing_Status_CityId] ON [app].[Housing] ([Status], [CityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_HousingResidencePeriods_EmployeeId] ON [app].[HousingResidencePeriods] ([EmployeeId]) WHERE [EffectiveTo] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_HousingResidencePeriods_EmployeeId_EffectiveFrom] ON [app].[HousingResidencePeriods] ([EmployeeId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_HousingResidencePeriods_HousingId_EffectiveFrom] ON [app].[HousingResidencePeriods] ([HousingId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_HousingSupervisorPeriods_HousingId] ON [app].[HousingSupervisorPeriods] ([HousingId]) WHERE [EffectiveTo] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_HousingSupervisorPeriods_HousingId_EffectiveFrom] ON [app].[HousingSupervisorPeriods] ([HousingId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_HousingSupervisorPeriods_SupervisorEmployeeId] ON [app].[HousingSupervisorPeriods] ([SupervisorEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HousingTags_HousingId_TagId] ON [app].[HousingTags] ([HousingId], [TagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_HousingTags_IsDeleted] ON [app].[HousingTags] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_HousingTags_TagId] ON [app].[HousingTags] ([TagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JobTitles_Code] ON [app].[JobTitles] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_JobTitles_IsDeleted] ON [app].[JobTitles] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveApprovalDecisions_DecidedByUserId_DecidedAtUtc] ON [app].[LeaveApprovalDecisions] ([DecidedByUserId], [DecidedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveApprovalDecisions_LeaveRequestId_StepSequence_DecidedAtUtc] ON [app].[LeaveApprovalDecisions] ([LeaveRequestId], [StepSequence], [DecidedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveApprovalWorkflows_ClientPlatformId] ON [app].[LeaveApprovalWorkflows] ([ClientPlatformId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LeaveApprovalWorkflows_Code_Version] ON [app].[LeaveApprovalWorkflows] ([Code], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveApprovalWorkflows_IsDeleted] ON [app].[LeaveApprovalWorkflows] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveApprovalWorkflows_LeaveTypeId] ON [app].[LeaveApprovalWorkflows] ([LeaveTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveApprovalWorkflows_Status_Priority_LeaveTypeId_RelationshipType_AppliesToRider_ClientPlatformId] ON [app].[LeaveApprovalWorkflows] ([Status], [Priority], [LeaveTypeId], [RelationshipType], [AppliesToRider], [ClientPlatformId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveApprovalWorkflowSteps_IsDeleted] ON [app].[LeaveApprovalWorkflowSteps] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LeaveApprovalWorkflowSteps_LeaveApprovalWorkflowId_Sequence] ON [app].[LeaveApprovalWorkflowSteps] ([LeaveApprovalWorkflowId], [Sequence]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LeaveApprovalWorkflowSteps_LeaveApprovalWorkflowId_StepKey] ON [app].[LeaveApprovalWorkflowSteps] ([LeaveApprovalWorkflowId], [StepKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveApprovalWorkflowSteps_RequiredPermissionKey] ON [app].[LeaveApprovalWorkflowSteps] ([RequiredPermissionKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveCancellationRequests_IsDeleted] ON [app].[LeaveCancellationRequests] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LeaveCancellationRequests_LeaveRequestId] ON [app].[LeaveCancellationRequests] ([LeaveRequestId]) WHERE [Status] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveCancellationRequests_LeaveRequestId_RequestedAtUtc] ON [app].[LeaveCancellationRequests] ([LeaveRequestId], [RequestedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveDateChangeRequests_IsDeleted] ON [app].[LeaveDateChangeRequests] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LeaveDateChangeRequests_LeaveRequestId] ON [app].[LeaveDateChangeRequests] ([LeaveRequestId]) WHERE [Status] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveDateChangeRequests_LeaveRequestId_RequestedAtUtc] ON [app].[LeaveDateChangeRequests] ([LeaveRequestId], [RequestedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveRequestDocuments_CurrentVersionId] ON [app].[LeaveRequestDocuments] ([CurrentVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveRequestDocuments_IsDeleted] ON [app].[LeaveRequestDocuments] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveRequestDocuments_LeaveRequestId_Kind] ON [app].[LeaveRequestDocuments] ([LeaveRequestId], [Kind]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LeaveRequestDocumentVersions_LeaveRequestDocumentId_VersionNumber] ON [app].[LeaveRequestDocumentVersions] ([LeaveRequestDocumentId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveRequestDocumentVersions_Sha256Checksum] ON [app].[LeaveRequestDocumentVersions] ([Sha256Checksum]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveRequestDocumentVersions_SupersededVersionId] ON [app].[LeaveRequestDocumentVersions] ([SupersededVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_ApprovalWorkflowId] ON [app].[LeaveRequests] ([ApprovalWorkflowId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_EmployeeId_StartDate_EndDate] ON [app].[LeaveRequests] ([EmployeeId], [StartDate], [EndDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_HrStatus_StartDate] ON [app].[LeaveRequests] ([HrStatus], [StartDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_IsDeleted] ON [app].[LeaveRequests] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_LeaveTypeId] ON [app].[LeaveRequests] ([LeaveTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_RelatedClientContractId] ON [app].[LeaveRequests] ([RelatedClientContractId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LeaveRequests_RequestNumber] ON [app].[LeaveRequests] ([RequestNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_Status_CurrentApprovalStepKey_SubmittedAtUtc] ON [app].[LeaveRequests] ([Status], [CurrentApprovalStepKey], [SubmittedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LeaveTypes_Code] ON [app].[LeaveTypes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_LeaveTypes_IsDeleted] ON [app].[LeaveTypes] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_Notifications_ExpiresAtUtc] ON [app].[Notifications] ([ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_Notifications_IsDeleted] ON [app].[Notifications] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Notifications_RecipientUserId_DeduplicationKey] ON [app].[Notifications] ([RecipientUserId], [DeduplicationKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_Notifications_RecipientUserId_ReadAtUtc_VisibleAtUtc] ON [app].[Notifications] ([RecipientUserId], [ReadAtUtc], [VisibleAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OperatingCities_GlobalCityId] ON [app].[OperatingCities] ([GlobalCityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_OperatingCities_IsDeleted] ON [app].[OperatingCities] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OutsideRiderDetails_EmployeeId] ON [app].[OutsideRiderDetails] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_OutsideRiderDetails_IsDeleted] ON [app].[OutsideRiderDetails] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_PermissionDefinitions_Category_DisplayOrder] ON [platform].[PermissionDefinitions] ([Category], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_PermissionDefinitions_IsDeleted] ON [platform].[PermissionDefinitions] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PermissionDefinitions_Key] ON [platform].[PermissionDefinitions] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlatformAccountCredentialVersions_PlatformRiderAccountId_KeyVersion] ON [app].[PlatformAccountCredentialVersions] ([PlatformRiderAccountId], [KeyVersion]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountCredentialVersions_PlatformRiderAccountId_RotatedAtUtc] ON [app].[PlatformAccountCredentialVersions] ([PlatformRiderAccountId], [RotatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountCredentialVersions_SupersededVersionId] ON [app].[PlatformAccountCredentialVersions] ([SupersededVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_PlatformRiderAccounts_ClientContractId_Status] ON [app].[PlatformRiderAccounts] ([ClientContractId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlatformRiderAccounts_ClientPlatformId_NormalizedExternalAccountId] ON [app].[PlatformRiderAccounts] ([ClientPlatformId], [NormalizedExternalAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlatformRiderAccounts_Code] ON [app].[PlatformRiderAccounts] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_PlatformRiderAccounts_IsDeleted] ON [app].[PlatformRiderAccounts] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_PlatformRiderAccountTags_IsDeleted] ON [app].[PlatformRiderAccountTags] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlatformRiderAccountTags_PlatformRiderAccountId_TagId] ON [app].[PlatformRiderAccountTags] ([PlatformRiderAccountId], [TagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_PlatformRiderAccountTags_TagId] ON [app].[PlatformRiderAccountTags] ([TagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_RiderAssignmentEvents_RiderClientAssignmentId_OccurredAtUtc] ON [app].[RiderAssignmentEvents] ([RiderClientAssignmentId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_RiderClientAssignments_ClientContractId_Status] ON [app].[RiderClientAssignments] ([ClientContractId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RiderClientAssignments_EmployeeId] ON [app].[RiderClientAssignments] ([EmployeeId]) WHERE [EffectiveTo] IS NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_RiderClientAssignments_EmployeeId_EffectiveFrom] ON [app].[RiderClientAssignments] ([EmployeeId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_RiderClientAssignments_IsDeleted] ON [app].[RiderClientAssignments] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RiderClientAssignments_PlatformRiderAccountId] ON [app].[RiderClientAssignments] ([PlatformRiderAccountId]) WHERE [EffectiveTo] IS NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_RiderClientAssignments_RiderProfileId] ON [app].[RiderClientAssignments] ([RiderProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RiderProfiles_EmployeeId] ON [app].[RiderProfiles] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_RiderProfiles_IsDeleted] ON [app].[RiderProfiles] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_RiderProfiles_LicenseDocumentId] ON [app].[RiderProfiles] ([LicenseDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_RiderProfiles_PreferredCityId] ON [app].[RiderProfiles] ([PreferredCityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_RiderProfiles_Status] ON [app].[RiderProfiles] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_SavedViews_IsDeleted] ON [app].[SavedViews] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SavedViews_UserId_ModuleKey_Name] ON [app].[SavedViews] ([UserId], [ModuleKey], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_SponsoredInternalDetails_CurrentJobTitleId] ON [app].[SponsoredInternalDetails] ([CurrentJobTitleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SponsoredInternalDetails_EmployeeId] ON [app].[SponsoredInternalDetails] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_SponsoredInternalDetails_IsDeleted] ON [app].[SponsoredInternalDetails] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_SponsoredInternalDetails_ManagerEmployeeId] ON [app].[SponsoredInternalDetails] ([ManagerEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_SponsoredInternalDetails_ProfilePhotoDocumentId] ON [app].[SponsoredInternalDetails] ([ProfilePhotoDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tags_Code] ON [app].[Tags] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    CREATE INDEX [IX_Tags_IsDeleted] ON [app].[Tags] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    ALTER TABLE [app].[EmployeeDocuments] ADD CONSTRAINT [FK_EmployeeDocuments_EmployeeDocumentVersions_CurrentVersionId] FOREIGN KEY ([CurrentVersionId]) REFERENCES [app].[EmployeeDocumentVersions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    ALTER TABLE [app].[LeaveRequestDocuments] ADD CONSTRAINT [FK_LeaveRequestDocuments_LeaveRequestDocumentVersions_CurrentVersionId] FOREIGN KEY ([CurrentVersionId]) REFERENCES [app].[LeaveRequestDocumentVersions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822091635_InitialApplication'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822091635_InitialApplication', N'10.0.11');
END;

COMMIT;
GO

