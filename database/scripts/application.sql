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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT [FK_PlatformRiderAccounts_ClientContracts_ClientContractId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] DROP CONSTRAINT [FK_RiderClientAssignments_Employees_EmployeeId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] DROP CONSTRAINT [FK_RiderClientAssignments_PlatformRiderAccounts_PlatformRiderAccountId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] DROP CONSTRAINT [FK_RiderClientAssignments_RiderProfiles_RiderProfileId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[RiderProfiles] DROP CONSTRAINT [FK_RiderProfiles_EmployeeDocuments_LicenseDocumentId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[SponsoredInternalDetails] DROP CONSTRAINT [FK_SponsoredInternalDetails_JobTitles_CurrentJobTitleId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    DROP INDEX [IX_RiderProfiles_LicenseDocumentId] ON [app].[RiderProfiles];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    DROP INDEX [IX_RiderClientAssignments_RiderProfileId] ON [app].[RiderClientAssignments];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC sp_rename N'[app].[SponsoredInternalDetails].[SponsorLegalReference]', N'LegacySponsorReference', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[SponsoredInternalDetails] ADD [CurrentSponsorId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_SponsoredInternalDetails_CurrentSponsorId] ON [app].[SponsoredInternalDetails] ([CurrentSponsorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC sp_rename N'[app].[RiderClientAssignments].[EmployeeId]', N'ActualEmployeeId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC sp_rename N'[app].[RiderClientAssignments].[IX_RiderClientAssignments_EmployeeId_EffectiveFrom]', N'IX_RiderClientAssignments_ActualEmployeeId_EffectiveFrom', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC sp_rename N'[app].[RiderClientAssignments].[IX_RiderClientAssignments_EmployeeId]', N'IX_RiderClientAssignments_ActualEmployeeId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC sp_rename N'[app].[HousingSupervisorPeriods].[EndedByUserId]', N'ClosedByUserId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC sp_rename N'[app].[HousingResidencePeriods].[EndedByUserId]', N'ClosedByUserId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] ADD [BillingMode] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] ADD [OperatingCityId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] ADD [RegisteredEmployeeId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] ADD [RegistrationType] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] ADD [SponsorId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[HousingSupervisorPeriods] ADD [ClosedAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[HousingSupervisorPeriods] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[HousingResidencePeriods] ADD [ClosedAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[HousingResidencePeriods] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeStatusPeriods] ADD [ClosedAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeStatusPeriods] ADD [ClosedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeStatusPeriods] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[Employees]') AND [c].[name] = N'PrimaryPhone');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [app].[Employees] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [app].[Employees] ALTER COLUMN [PrimaryPhone] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[Employees]') AND [c].[name] = N'NormalizedNameEn');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [app].[Employees] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [app].[Employees] ALTER COLUMN [NormalizedNameEn] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[Employees]') AND [c].[name] = N'FullNameEn');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [app].[Employees] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [app].[Employees] ALTER COLUMN [FullNameEn] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [HireDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [NationalityCountryCode] nchar(2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeRelationshipPeriods] ADD [ClosedAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeRelationshipPeriods] ADD [ClosedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeRelationshipPeriods] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeJobTitlePeriods] ADD [ClosedAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeJobTitlePeriods] ADD [ClosedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeJobTitlePeriods] ADD [OperatingCityId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeJobTitlePeriods] ADD [OperationalWorkTypeId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeJobTitlePeriods] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [platform].[DocumentTypes] ADD [MaxFileSizeBytes] bigint NOT NULL DEFAULT CAST(10485760 AS bigint);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[RiderProfiles] ADD CONSTRAINT [AK_RiderProfiles_Id_EmployeeId] UNIQUE ([Id], [EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] ADD CONSTRAINT [AK_PlatformRiderAccounts_Id_ClientContractId] UNIQUE ([Id], [ClientContractId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[ClientContracts] ADD CONSTRAINT [AK_ClientContracts_Id_ClientPlatformId] UNIQUE ([Id], [ClientPlatformId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[DriverLicenseCategories] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(100) NOT NULL,
        [NameEn] nvarchar(100) NOT NULL,
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
        CONSTRAINT [PK_DriverLicenseCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[InsuranceCompanies] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NULL,
        [ProviderRegistrationNumber] nvarchar(100) NULL,
        [ContactName] nvarchar(200) NULL,
        [ContactPhone] nvarchar(32) NULL,
        [ContactEmail] nvarchar(320) NULL,
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
        CONSTRAINT [PK_InsuranceCompanies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[OperationalWorkTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(100) NOT NULL,
        [NameEn] nvarchar(100) NOT NULL,
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
        CONSTRAINT [PK_OperationalWorkTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[ResidencyProfessions] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NULL,
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
        CONSTRAINT [PK_ResidencyProfessions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[RiderCards] (
        [Id] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [CardNumber] nvarchar(150) NOT NULL,
        [NormalizedCardNumber] nvarchar(150) NOT NULL,
        [CardType] int NOT NULL,
        [ValidityCycle] int NOT NULL,
        [IssueDate] date NULL,
        [ExpiryDate] date NULL,
        [Status] int NOT NULL,
        [PreviousCardId] uniqueidentifier NULL,
        [IsCurrent] bit NOT NULL,
        [EmployeeDocumentId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_RiderCards] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RiderCards_DateRange] CHECK ([ExpiryDate] IS NULL OR [IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]),
        CONSTRAINT [FK_RiderCards_EmployeeDocuments_EmployeeDocumentId] FOREIGN KEY ([EmployeeDocumentId]) REFERENCES [app].[EmployeeDocuments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderCards_RiderCards_PreviousCardId] FOREIGN KEY ([PreviousCardId]) REFERENCES [app].[RiderCards] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderCards_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[RiderHealthCards] (
        [Id] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [CardNumberCiphertext] varbinary(max) NOT NULL,
        [CardNumberLookupHash] nchar(64) NOT NULL,
        [CardNumberLastFour] nchar(4) NOT NULL,
        [CardType] nvarchar(100) NULL,
        [IssuingAuthority] nvarchar(200) NULL,
        [IssueDate] date NULL,
        [ExpiryDate] date NULL,
        [Status] int NOT NULL,
        [PreviousCardId] uniqueidentifier NULL,
        [IsCurrent] bit NOT NULL,
        [EmployeeDocumentId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_RiderHealthCards] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RiderHealthCards_DateRange] CHECK ([ExpiryDate] IS NULL OR [IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]),
        CONSTRAINT [FK_RiderHealthCards_EmployeeDocuments_EmployeeDocumentId] FOREIGN KEY ([EmployeeDocumentId]) REFERENCES [app].[EmployeeDocuments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderHealthCards_RiderHealthCards_PreviousCardId] FOREIGN KEY ([PreviousCardId]) REFERENCES [app].[RiderHealthCards] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderHealthCards_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[Sponsors] (
        [Id] uniqueidentifier NOT NULL,
        [CompanyProfileId] uniqueidentifier NOT NULL,
        [EmployerIdentityNumber] nvarchar(100) NOT NULL,
        [RegistryNameAr] nvarchar(200) NOT NULL,
        [RegistryNameEn] nvarchar(200) NULL,
        [CommercialRegistrationNumber] nvarchar(100) NULL,
        [UnifiedNationalNumber] nvarchar(100) NULL,
        [SponsorType] int NOT NULL,
        [Status] int NOT NULL,
        [ActiveFrom] date NULL,
        [ActiveTo] date NULL,
        [ContactName] nvarchar(200) NULL,
        [ContactPhone] nvarchar(32) NULL,
        [ContactEmail] nvarchar(320) NULL,
        [AddressBuildingNumber] nvarchar(32) NULL,
        [AddressStreet] nvarchar(200) NULL,
        [AddressDistrict] nvarchar(200) NULL,
        [AddressCity] nvarchar(200) NULL,
        [AddressPostalCode] nvarchar(32) NULL,
        [AddressAdditionalNumber] nvarchar(32) NULL,
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
        CONSTRAINT [PK_Sponsors] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Sponsors_ActiveRange] CHECK ([ActiveTo] IS NULL OR [ActiveFrom] IS NULL OR [ActiveTo] >= [ActiveFrom]),
        CONSTRAINT [FK_Sponsors_CompanyProfile_CompanyProfileId] FOREIGN KEY ([CompanyProfileId]) REFERENCES [platform].[CompanyProfile] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[EmployeeDriverLicenses] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [DriverLicenseCategoryId] uniqueidentifier NOT NULL,
        [LicenseNumberCiphertext] varbinary(max) NULL,
        [LicenseNumberLookupHash] nchar(64) NULL,
        [LicenseNumberLastFour] nchar(4) NULL,
        [IssueDate] date NULL,
        [ExpiryDate] date NULL,
        [BookingStatus] int NOT NULL,
        [IssuanceStatus] int NOT NULL,
        [LicenseStatus] int NOT NULL,
        [PreviousLicenseId] uniqueidentifier NULL,
        [IsCurrent] bit NOT NULL,
        [EmployeeDocumentId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_EmployeeDriverLicenses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeDriverLicenses_DateRange] CHECK ([ExpiryDate] IS NULL OR [IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]),
        CONSTRAINT [FK_EmployeeDriverLicenses_DriverLicenseCategories_DriverLicenseCategoryId] FOREIGN KEY ([DriverLicenseCategoryId]) REFERENCES [app].[DriverLicenseCategories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeDriverLicenses_EmployeeDocuments_EmployeeDocumentId] FOREIGN KEY ([EmployeeDocumentId]) REFERENCES [app].[EmployeeDocuments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeDriverLicenses_EmployeeDriverLicenses_PreviousLicenseId] FOREIGN KEY ([PreviousLicenseId]) REFERENCES [app].[EmployeeDriverLicenses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeDriverLicenses_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[InsurancePlanLevels] (
        [Id] uniqueidentifier NOT NULL,
        [InsuranceCompanyId] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NULL,
        [Rank] int NOT NULL,
        [NetworkName] nvarchar(200) NULL,
        [CoverageClass] nvarchar(100) NULL,
        [AnnualCoverageLimit] decimal(18,2) NULL,
        [DeductiblePercentage] decimal(5,2) NULL,
        [EffectiveFrom] date NULL,
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
        CONSTRAINT [PK_InsurancePlanLevels] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_InsurancePlanLevels_Id_InsuranceCompanyId] UNIQUE ([Id], [InsuranceCompanyId]),
        CONSTRAINT [CK_InsurancePlanLevels_AnnualLimit] CHECK ([AnnualCoverageLimit] IS NULL OR [AnnualCoverageLimit] >= 0),
        CONSTRAINT [CK_InsurancePlanLevels_DateRange] CHECK ([EffectiveTo] IS NULL OR [EffectiveFrom] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [CK_InsurancePlanLevels_Deductible] CHECK ([DeductiblePercentage] IS NULL OR ([DeductiblePercentage] >= 0 AND [DeductiblePercentage] <= 100)),
        CONSTRAINT [CK_InsurancePlanLevels_Rank] CHECK ([Rank] >= 0),
        CONSTRAINT [FK_InsurancePlanLevels_InsuranceCompanies_InsuranceCompanyId] FOREIGN KEY ([InsuranceCompanyId]) REFERENCES [app].[InsuranceCompanies] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[JobTitleOperationalWorkTypes] (
        [Id] uniqueidentifier NOT NULL,
        [JobTitleId] uniqueidentifier NOT NULL,
        [OperationalWorkTypeId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_JobTitleOperationalWorkTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobTitleOperationalWorkTypes_JobTitles_JobTitleId] FOREIGN KEY ([JobTitleId]) REFERENCES [app].[JobTitles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JobTitleOperationalWorkTypes_OperationalWorkTypes_OperationalWorkTypeId] FOREIGN KEY ([OperationalWorkTypeId]) REFERENCES [app].[OperationalWorkTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[EmployeePromissoryNotes] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [SponsorId] uniqueidentifier NULL,
        [NoteNumber] nvarchar(150) NOT NULL,
        [NormalizedNoteNumber] nvarchar(150) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [CurrencyCode] nchar(3) NOT NULL,
        [IssueDate] date NOT NULL,
        [DueDate] date NULL,
        [SignedAtUtc] datetimeoffset NULL,
        [Status] int NOT NULL,
        [BeneficiaryCompanyProfileId] uniqueidentifier NOT NULL,
        [EmployeeDocumentId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_EmployeePromissoryNotes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeePromissoryNotes_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_EmployeePromissoryNotes_DateRange] CHECK ([DueDate] IS NULL OR [DueDate] >= [IssueDate]),
        CONSTRAINT [FK_EmployeePromissoryNotes_CompanyProfile_BeneficiaryCompanyProfileId] FOREIGN KEY ([BeneficiaryCompanyProfileId]) REFERENCES [platform].[CompanyProfile] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeePromissoryNotes_EmployeeDocuments_EmployeeDocumentId] FOREIGN KEY ([EmployeeDocumentId]) REFERENCES [app].[EmployeeDocuments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeePromissoryNotes_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeePromissoryNotes_Sponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[EmployeeResidencyPermits] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [SponsorId] uniqueidentifier NOT NULL,
        [ResidencyProfessionId] uniqueidentifier NOT NULL,
        [PermitNumberCiphertext] varbinary(max) NOT NULL,
        [PermitNumberLookupHash] nchar(64) NOT NULL,
        [PermitNumberLastFour] nchar(4) NOT NULL,
        [IssueDate] date NULL,
        [ExpiryDate] date NOT NULL,
        [Status] int NOT NULL,
        [PreviousPermitId] uniqueidentifier NULL,
        [IsCurrent] bit NOT NULL,
        [EmployeeDocumentId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_EmployeeResidencyPermits] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeResidencyPermits_DateRange] CHECK ([IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]),
        CONSTRAINT [FK_EmployeeResidencyPermits_EmployeeDocuments_EmployeeDocumentId] FOREIGN KEY ([EmployeeDocumentId]) REFERENCES [app].[EmployeeDocuments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeResidencyPermits_EmployeeResidencyPermits_PreviousPermitId] FOREIGN KEY ([PreviousPermitId]) REFERENCES [app].[EmployeeResidencyPermits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeResidencyPermits_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeResidencyPermits_ResidencyProfessions_ResidencyProfessionId] FOREIGN KEY ([ResidencyProfessionId]) REFERENCES [app].[ResidencyProfessions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeResidencyPermits_Sponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[EmployeeSponsorshipPeriods] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [SponsorId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [Reason] nvarchar(1000) NULL,
        [SourceReference] nvarchar(200) NULL,
        [ChangedByUserId] uniqueidentifier NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [ClosedAtUtc] datetimeoffset NULL,
        [ClosedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployeeSponsorshipPeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeSponsorshipPeriods_EffectiveRange] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]),
        CONSTRAINT [FK_EmployeeSponsorshipPeriods_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeSponsorshipPeriods_Sponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[PlatformAccountRegistrations] (
        [Id] uniqueidentifier NOT NULL,
        [RegisteredEmployeeId] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [ClientPlatformId] uniqueidentifier NOT NULL,
        [ClientContractId] uniqueidentifier NOT NULL,
        [SponsorId] uniqueidentifier NULL,
        [OperatingCityId] uniqueidentifier NOT NULL,
        [RegistrationType] int NOT NULL,
        [Status] int NOT NULL,
        [StatusReason] nvarchar(1000) NULL,
        [RequestedAtUtc] datetimeoffset NULL,
        [ActivatedAtUtc] datetimeoffset NULL,
        [PlatformRiderAccountId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_PlatformAccountRegistrations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PlatformAccountRegistrations_ActivationRange] CHECK ([ActivatedAtUtc] IS NULL OR [RequestedAtUtc] IS NULL OR [ActivatedAtUtc] >= [RequestedAtUtc]),
        CONSTRAINT [CK_PlatformAccountRegistrations_Registration] CHECK (([RegistrationType] = 1 AND [SponsorId] IS NOT NULL) OR ([RegistrationType] = 2 AND [SponsorId] IS NULL)),
        CONSTRAINT [FK_PlatformAccountRegistrations_ClientContracts_ClientContractId_ClientPlatformId] FOREIGN KEY ([ClientContractId], [ClientPlatformId]) REFERENCES [app].[ClientContracts] ([Id], [ClientPlatformId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlatformAccountRegistrations_ClientPlatforms_ClientPlatformId] FOREIGN KEY ([ClientPlatformId]) REFERENCES [platform].[ClientPlatforms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlatformAccountRegistrations_Employees_RegisteredEmployeeId] FOREIGN KEY ([RegisteredEmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlatformAccountRegistrations_OperatingCities_OperatingCityId] FOREIGN KEY ([OperatingCityId]) REFERENCES [app].[OperatingCities] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlatformAccountRegistrations_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlatformAccountRegistrations_RiderProfiles_RiderProfileId_RegisteredEmployeeId] FOREIGN KEY ([RiderProfileId], [RegisteredEmployeeId]) REFERENCES [app].[RiderProfiles] ([Id], [EmployeeId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlatformAccountRegistrations_Sponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE TABLE [app].[EmployeeMedicalInsurancePolicies] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [InsuranceCompanyId] uniqueidentifier NOT NULL,
        [InsurancePlanLevelId] uniqueidentifier NOT NULL,
        [PolicyNumberCiphertext] varbinary(max) NULL,
        [PolicyNumberLookupHash] nchar(64) NULL,
        [PolicyNumberLastFour] nchar(4) NULL,
        [MemberNumberCiphertext] varbinary(max) NULL,
        [MemberNumberLookupHash] nchar(64) NULL,
        [MemberNumberLastFour] nchar(4) NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [Status] int NOT NULL,
        [PreviousPolicyId] uniqueidentifier NULL,
        [IsCurrent] bit NOT NULL,
        [EmployeeDocumentId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_EmployeeMedicalInsurancePolicies] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeMedicalInsurancePolicies_DateRange] CHECK ([EndDate] >= [StartDate]),
        CONSTRAINT [FK_EmployeeMedicalInsurancePolicies_EmployeeDocuments_EmployeeDocumentId] FOREIGN KEY ([EmployeeDocumentId]) REFERENCES [app].[EmployeeDocuments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeMedicalInsurancePolicies_EmployeeMedicalInsurancePolicies_PreviousPolicyId] FOREIGN KEY ([PreviousPolicyId]) REFERENCES [app].[EmployeeMedicalInsurancePolicies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeMedicalInsurancePolicies_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeMedicalInsurancePolicies_InsuranceCompanies_InsuranceCompanyId] FOREIGN KEY ([InsuranceCompanyId]) REFERENCES [app].[InsuranceCompanies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeMedicalInsurancePolicies_InsurancePlanLevels_InsurancePlanLevelId_InsuranceCompanyId] FOREIGN KEY ([InsurancePlanLevelId], [InsuranceCompanyId]) REFERENCES [app].[InsurancePlanLevels] ([Id], [InsuranceCompanyId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AllowedMimeTypes', N'AppliesToOutsideRider', N'AppliesToRiderProfile', N'AppliesToSponsoredInternal', N'Code', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'IsDeleted', N'MaxFileSizeBytes', N'NameAr', N'NameEn', N'RequiresExpiryDate', N'RequiresFile', N'RequiresIssueDate', N'RequiresNumber', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[platform].[DocumentTypes]'))
        SET IDENTITY_INSERT [platform].[DocumentTypes] ON;
    EXEC(N'INSERT INTO [platform].[DocumentTypes] ([Id], [AllowedMimeTypes], [AppliesToOutsideRider], [AppliesToRiderProfile], [AppliesToSponsoredInternal], [Code], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [IsDeleted], [MaxFileSizeBytes], [NameAr], [NameEn], [RequiresExpiryDate], [RequiresFile], [RequiresIssueDate], [RequiresNumber], [Status], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-8000-000000000030'', N''application/pdf,image/jpeg,image/png'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''RESIDENCY_PERMIT'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, NULL, NULL, CAST(0 AS bit), CAST(10485760 AS bigint), N''الإقامة'', N''Residency Permit'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), 1, NULL, NULL),
    (''019c18d5-62e1-7000-8000-000000000031'', N''application/pdf,image/jpeg,image/png'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''DRIVER_LICENSE'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, NULL, NULL, CAST(0 AS bit), CAST(10485760 AS bigint), N''رخصة القيادة'', N''Driver License'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), 1, NULL, NULL),
    (''019c18d5-62e1-7000-8000-000000000032'', N''application/pdf,image/jpeg,image/png'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''RIDER_CARD'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, NULL, NULL, CAST(0 AS bit), CAST(10485760 AS bigint), N''بطاقة السائق'', N''Rider Card'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), 1, NULL, NULL),
    (''019c18d5-62e1-7000-8000-000000000033'', N''application/pdf,image/jpeg,image/png'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''HEALTH_CARD'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, NULL, NULL, CAST(0 AS bit), CAST(10485760 AS bigint), N''البطاقة الصحية'', N''Health Card'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), 1, NULL, NULL),
    (''019c18d5-62e1-7000-8000-000000000034'', N''application/pdf,image/jpeg,image/png'', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''PROMISSORY_NOTE'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, NULL, NULL, CAST(0 AS bit), CAST(10485760 AS bigint), N''سند الأمر'', N''Promissory Note'', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), 1, NULL, NULL),
    (''019c18d5-62e1-7000-8000-000000000035'', N''application/pdf,image/jpeg,image/png'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''MEDICAL_INSURANCE'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, NULL, NULL, CAST(0 AS bit), CAST(10485760 AS bigint), N''التأمين الطبي'', N''Medical Insurance'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), 1, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AllowedMimeTypes', N'AppliesToOutsideRider', N'AppliesToRiderProfile', N'AppliesToSponsoredInternal', N'Code', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'IsDeleted', N'MaxFileSizeBytes', N'NameAr', N'NameEn', N'RequiresExpiryDate', N'RequiresFile', N'RequiresIssueDate', N'RequiresNumber', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[platform].[DocumentTypes]'))
        SET IDENTITY_INSERT [platform].[DocumentTypes] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'NameAr', N'NameEn', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[app].[DriverLicenseCategories]'))
        SET IDENTITY_INSERT [app].[DriverLicenseCategories] ON;
    EXEC(N'INSERT INTO [app].[DriverLicenseCategories] ([Id], [Code], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [NameAr], [NameEn], [Status], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-8000-000000000020'', N''LIGHT_TRANSPORT'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''نقل خفيف'', N''Light Transport'', 1, NULL, NULL),
    (''019c18d5-62e1-7000-8000-000000000021'', N''MOTORCYCLE'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''دراجة نارية'', N''Motorcycle'', 1, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'NameAr', N'NameEn', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[app].[DriverLicenseCategories]'))
        SET IDENTITY_INSERT [app].[DriverLicenseCategories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CountryCode', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DisplayOrder', N'IsDeleted', N'Latitude', N'Longitude', N'NameAr', N'NameEn', N'RegionAr', N'RegionEn', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[platform].[GlobalCities]'))
        SET IDENTITY_INSERT [platform].[GlobalCities] ON;
    EXEC(N'INSERT INTO [platform].[GlobalCities] ([Id], [Code], [CountryCode], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DisplayOrder], [IsDeleted], [Latitude], [Longitude], [NameAr], [NameEn], [RegionAr], [RegionEn], [Status], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-8000-000000000004'', N''RIYADH'', N''SA'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, 2, CAST(0 AS bit), 24.7136, 46.6753, N''الرياض'', N''Riyadh'', N''منطقة الرياض'', N''Riyadh Region'', 1, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CountryCode', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DisplayOrder', N'IsDeleted', N'Latitude', N'Longitude', N'NameAr', N'NameEn', N'RegionAr', N'RegionEn', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[platform].[GlobalCities]'))
        SET IDENTITY_INSERT [platform].[GlobalCities] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'NameAr', N'NameEn', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[app].[OperationalWorkTypes]'))
        SET IDENTITY_INSERT [app].[OperationalWorkTypes] ON;
    EXEC(N'INSERT INTO [app].[OperationalWorkTypes] ([Id], [Code], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [NameAr], [NameEn], [Status], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-8000-000000000010'', N''ADMIN'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''إداري'', N''Administrative'', 1, NULL, NULL),
    (''019c18d5-62e1-7000-8000-000000000011'', N''CAR'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''سيارة'', N''Car'', 1, NULL, NULL),
    (''019c18d5-62e1-7000-8000-000000000012'', N''MOTORCYCLE'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''دراجة نارية'', N''Motorcycle'', 1, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'NameAr', N'NameEn', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[app].[OperationalWorkTypes]'))
        SET IDENTITY_INSERT [app].[OperationalWorkTypes] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DisabledAt', N'EnabledFrom', N'GlobalCityId', N'IsDeleted', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[app].[OperatingCities]'))
        SET IDENTITY_INSERT [app].[OperatingCities] ON;
    EXEC(N'INSERT INTO [app].[OperatingCities] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DisabledAt], [EnabledFrom], [GlobalCityId], [IsDeleted], [Status], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-8000-000000000005'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, NULL, ''2026-01-01'', ''019c18d5-62e1-7000-8000-000000000004'', CAST(0 AS bit), 1, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DisabledAt', N'EnabledFrom', N'GlobalCityId', N'IsDeleted', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[app].[OperatingCities]'))
        SET IDENTITY_INSERT [app].[OperatingCities] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'
    UPDATE employee
    SET employee.HireDate = sponsored.HireDate,
        employee.NationalityCountryCode = sponsored.NationalityCountryCode
    FROM [app].[Employees] AS employee
    INNER JOIN [app].[SponsoredInternalDetails] AS sponsored ON sponsored.EmployeeId = employee.Id;

    UPDATE employee
    SET employee.NationalityCountryCode = outsideRider.NationalityCountryCode
    FROM [app].[Employees] AS employee
    INNER JOIN [app].[OutsideRiderDetails] AS outsideRider ON outsideRider.EmployeeId = employee.Id
    WHERE employee.NationalityCountryCode IS NULL;

    UPDATE [app].[EmployeeJobTitlePeriods]
    SET OperatingCityId = ''019c18d5-62e1-7000-8000-000000000003'',
        OperationalWorkTypeId = ''019c18d5-62e1-7000-8000-000000000010'';

    INSERT INTO [app].[EmployeeJobTitlePeriods]
        (Id, EmployeeId, JobTitleId, OperationalWorkTypeId, OperatingCityId, EffectiveFrom,
         EffectiveTo, Reason, ChangedByUserId, CreatedAtUtc, CreatedByUserId, ClosedAtUtc, ClosedByUserId)
    SELECT NEWID(), sponsored.EmployeeId, sponsored.CurrentJobTitleId,
           ''019c18d5-62e1-7000-8000-000000000010'',
           ''019c18d5-62e1-7000-8000-000000000003'',
           COALESCE(sponsored.HireDate, CAST(sponsored.CreatedAtUtc AS date), CAST(SYSUTCDATETIME() AS date)),
           NULL, N''Migrated from the previous current job-title reference.'',
           ''00000000-0000-0000-0000-000000000000'', SYSUTCDATETIME(), NULL, NULL, NULL
    FROM [app].[SponsoredInternalDetails] AS sponsored
    WHERE sponsored.CurrentJobTitleId IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM [app].[EmployeeJobTitlePeriods] AS period
          WHERE period.EmployeeId = sponsored.EmployeeId AND period.EffectiveTo IS NULL
      );

    INSERT INTO [app].[EmployeeDriverLicenses]
        (Id, EmployeeId, DriverLicenseCategoryId, BookingStatus, IssuanceStatus, LicenseStatus,
         PreviousLicenseId, IsCurrent, EmployeeDocumentId, Notes, CreatedAtUtc, IsDeleted)
    SELECT NEWID(), rider.EmployeeId, ''019c18d5-62e1-7000-8000-000000000020'',
           6, 3, 2, NULL, 1, rider.LicenseDocumentId,
           N''Migrated from RiderProfile.LicenseDocumentId.'', SYSUTCDATETIME(), 0
    FROM [app].[RiderProfiles] AS rider
    WHERE rider.LicenseDocumentId IS NOT NULL;

    UPDATE [app].[PlatformRiderAccounts]
    SET BillingMode = 1,
        OperatingCityId = ''019c18d5-62e1-7000-8000-000000000003'',
        RegistrationType = 2,
        SponsorId = NULL;
    ');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    DROP INDEX [IX_SponsoredInternalDetails_CurrentJobTitleId] ON [app].[SponsoredInternalDetails];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[SponsoredInternalDetails]') AND [c].[name] = N'CurrentJobTitleId');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [app].[SponsoredInternalDetails] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [app].[SponsoredInternalDetails] DROP COLUMN [CurrentJobTitleId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[SponsoredInternalDetails]') AND [c].[name] = N'HireDate');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [app].[SponsoredInternalDetails] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [app].[SponsoredInternalDetails] DROP COLUMN [HireDate];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[SponsoredInternalDetails]') AND [c].[name] = N'NationalityCountryCode');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [app].[SponsoredInternalDetails] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [app].[SponsoredInternalDetails] DROP COLUMN [NationalityCountryCode];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[RiderProfiles]') AND [c].[name] = N'LicenseDocumentId');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [app].[RiderProfiles] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [app].[RiderProfiles] DROP COLUMN [LicenseDocumentId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[OutsideRiderDetails]') AND [c].[name] = N'NationalityCountryCode');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [app].[OutsideRiderDetails] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [app].[OutsideRiderDetails] DROP COLUMN [NationalityCountryCode];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_RiderClientAssignments_PlatformRiderAccountId_ClientContractId] ON [app].[RiderClientAssignments] ([PlatformRiderAccountId], [ClientContractId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_RiderClientAssignments_RiderProfileId_ActualEmployeeId] ON [app].[RiderClientAssignments] ([RiderProfileId], [ActualEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_PlatformRiderAccounts_ClientContractId_ClientPlatformId] ON [app].[PlatformRiderAccounts] ([ClientContractId], [ClientPlatformId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_PlatformRiderAccounts_OperatingCityId_SponsorId_RegistrationType] ON [app].[PlatformRiderAccounts] ([OperatingCityId], [SponsorId], [RegistrationType]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_PlatformRiderAccounts_RegisteredEmployeeId_ClientPlatformId_Status] ON [app].[PlatformRiderAccounts] ([RegisteredEmployeeId], [ClientPlatformId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_PlatformRiderAccounts_SponsorId] ON [app].[PlatformRiderAccounts] ([SponsorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'ALTER TABLE [app].[PlatformRiderAccounts] ADD CONSTRAINT [CK_PlatformRiderAccounts_Registration] CHECK (([RegistrationType] = 1 AND [SponsorId] IS NOT NULL) OR ([RegistrationType] = 2 AND [SponsorId] IS NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeJobTitlePeriods_OperatingCityId_OperationalWorkTypeId_JobTitleId_EffectiveTo] ON [app].[EmployeeJobTitlePeriods] ([OperatingCityId], [OperationalWorkTypeId], [JobTitleId], [EffectiveTo]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeJobTitlePeriods_OperationalWorkTypeId] ON [app].[EmployeeJobTitlePeriods] ([OperationalWorkTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'ALTER TABLE [platform].[DocumentTypes] ADD CONSTRAINT [CK_DocumentTypes_MaxFileSize] CHECK ([MaxFileSizeBytes] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DriverLicenseCategories_Code] ON [app].[DriverLicenseCategories] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_DriverLicenseCategories_IsDeleted] ON [app].[DriverLicenseCategories] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeDriverLicenses_DriverLicenseCategoryId] ON [app].[EmployeeDriverLicenses] ([DriverLicenseCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeDriverLicenses_EmployeeDocumentId] ON [app].[EmployeeDriverLicenses] ([EmployeeDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeeDriverLicenses_EmployeeId_DriverLicenseCategoryId] ON [app].[EmployeeDriverLicenses] ([EmployeeId], [DriverLicenseCategoryId]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeDriverLicenses_EmployeeId_DriverLicenseCategoryId_LicenseStatus] ON [app].[EmployeeDriverLicenses] ([EmployeeId], [DriverLicenseCategoryId], [LicenseStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeDriverLicenses_IsDeleted] ON [app].[EmployeeDriverLicenses] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_EmployeeDriverLicenses_LicenseNumberLookupHash] ON [app].[EmployeeDriverLicenses] ([LicenseNumberLookupHash]) WHERE [LicenseNumberLookupHash] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeDriverLicenses_PreviousLicenseId] ON [app].[EmployeeDriverLicenses] ([PreviousLicenseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeMedicalInsurancePolicies_EmployeeDocumentId] ON [app].[EmployeeMedicalInsurancePolicies] ([EmployeeDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeeMedicalInsurancePolicies_EmployeeId] ON [app].[EmployeeMedicalInsurancePolicies] ([EmployeeId]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeMedicalInsurancePolicies_InsuranceCompanyId_Status_EndDate] ON [app].[EmployeeMedicalInsurancePolicies] ([InsuranceCompanyId], [Status], [EndDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeMedicalInsurancePolicies_InsurancePlanLevelId_InsuranceCompanyId] ON [app].[EmployeeMedicalInsurancePolicies] ([InsurancePlanLevelId], [InsuranceCompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeMedicalInsurancePolicies_IsDeleted] ON [app].[EmployeeMedicalInsurancePolicies] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_EmployeeMedicalInsurancePolicies_MemberNumberLookupHash] ON [app].[EmployeeMedicalInsurancePolicies] ([MemberNumberLookupHash]) WHERE [MemberNumberLookupHash] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_EmployeeMedicalInsurancePolicies_PolicyNumberLookupHash] ON [app].[EmployeeMedicalInsurancePolicies] ([PolicyNumberLookupHash]) WHERE [PolicyNumberLookupHash] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeMedicalInsurancePolicies_PreviousPolicyId] ON [app].[EmployeeMedicalInsurancePolicies] ([PreviousPolicyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeePromissoryNotes_BeneficiaryCompanyProfileId_NormalizedNoteNumber] ON [app].[EmployeePromissoryNotes] ([BeneficiaryCompanyProfileId], [NormalizedNoteNumber]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeePromissoryNotes_EmployeeDocumentId] ON [app].[EmployeePromissoryNotes] ([EmployeeDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeePromissoryNotes_EmployeeId_Status] ON [app].[EmployeePromissoryNotes] ([EmployeeId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeePromissoryNotes_IsDeleted] ON [app].[EmployeePromissoryNotes] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeePromissoryNotes_SponsorId] ON [app].[EmployeePromissoryNotes] ([SponsorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeResidencyPermits_EmployeeDocumentId] ON [app].[EmployeeResidencyPermits] ([EmployeeDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeeResidencyPermits_EmployeeId] ON [app].[EmployeeResidencyPermits] ([EmployeeId]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeResidencyPermits_IsDeleted] ON [app].[EmployeeResidencyPermits] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeeResidencyPermits_PermitNumberLookupHash] ON [app].[EmployeeResidencyPermits] ([PermitNumberLookupHash]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeResidencyPermits_PreviousPermitId] ON [app].[EmployeeResidencyPermits] ([PreviousPermitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeResidencyPermits_ResidencyProfessionId] ON [app].[EmployeeResidencyPermits] ([ResidencyProfessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeResidencyPermits_SponsorId_Status_ExpiryDate] ON [app].[EmployeeResidencyPermits] ([SponsorId], [Status], [ExpiryDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeeSponsorshipPeriods_EmployeeId] ON [app].[EmployeeSponsorshipPeriods] ([EmployeeId]) WHERE [EffectiveTo] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeSponsorshipPeriods_EmployeeId_EffectiveFrom] ON [app].[EmployeeSponsorshipPeriods] ([EmployeeId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_EmployeeSponsorshipPeriods_SponsorId_Status_EffectiveTo] ON [app].[EmployeeSponsorshipPeriods] ([SponsorId], [Status], [EffectiveTo]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsuranceCompanies_Code] ON [app].[InsuranceCompanies] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_InsuranceCompanies_IsDeleted] ON [app].[InsuranceCompanies] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_InsuranceCompanies_ProviderRegistrationNumber] ON [app].[InsuranceCompanies] ([ProviderRegistrationNumber]) WHERE [ProviderRegistrationNumber] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_InsuranceCompanies_Status_NameAr] ON [app].[InsuranceCompanies] ([Status], [NameAr]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_InsurancePlanLevels_InsuranceCompanyId_Code] ON [app].[InsurancePlanLevels] ([InsuranceCompanyId], [Code]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_InsurancePlanLevels_InsuranceCompanyId_Status_Rank] ON [app].[InsurancePlanLevels] ([InsuranceCompanyId], [Status], [Rank]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_InsurancePlanLevels_IsDeleted] ON [app].[InsurancePlanLevels] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_JobTitleOperationalWorkTypes_IsDeleted] ON [app].[JobTitleOperationalWorkTypes] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_JobTitleOperationalWorkTypes_JobTitleId_OperationalWorkTypeId] ON [app].[JobTitleOperationalWorkTypes] ([JobTitleId], [OperationalWorkTypeId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_JobTitleOperationalWorkTypes_OperationalWorkTypeId] ON [app].[JobTitleOperationalWorkTypes] ([OperationalWorkTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OperationalWorkTypes_Code] ON [app].[OperationalWorkTypes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_OperationalWorkTypes_IsDeleted] ON [app].[OperationalWorkTypes] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountRegistrations_ClientContractId_ClientPlatformId] ON [app].[PlatformAccountRegistrations] ([ClientContractId], [ClientPlatformId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountRegistrations_ClientPlatformId] ON [app].[PlatformAccountRegistrations] ([ClientPlatformId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountRegistrations_IsDeleted] ON [app].[PlatformAccountRegistrations] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountRegistrations_OperatingCityId_SponsorId_RegistrationType_Status] ON [app].[PlatformAccountRegistrations] ([OperatingCityId], [SponsorId], [RegistrationType], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PlatformAccountRegistrations_PlatformRiderAccountId] ON [app].[PlatformAccountRegistrations] ([PlatformRiderAccountId]) WHERE [PlatformRiderAccountId] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountRegistrations_RegisteredEmployeeId_ClientPlatformId_Status] ON [app].[PlatformAccountRegistrations] ([RegisteredEmployeeId], [ClientPlatformId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountRegistrations_RiderProfileId_RegisteredEmployeeId] ON [app].[PlatformAccountRegistrations] ([RiderProfileId], [RegisteredEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountRegistrations_SponsorId] ON [app].[PlatformAccountRegistrations] ([SponsorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ResidencyProfessions_Code] ON [app].[ResidencyProfessions] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_ResidencyProfessions_IsDeleted] ON [app].[ResidencyProfessions] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_ResidencyProfessions_NameAr] ON [app].[ResidencyProfessions] ([NameAr]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_RiderCards_EmployeeDocumentId] ON [app].[RiderCards] ([EmployeeDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_RiderCards_IsDeleted] ON [app].[RiderCards] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RiderCards_NormalizedCardNumber] ON [app].[RiderCards] ([NormalizedCardNumber]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_RiderCards_PreviousCardId] ON [app].[RiderCards] ([PreviousCardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RiderCards_RiderProfileId_CardType] ON [app].[RiderCards] ([RiderProfileId], [CardType]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_RiderCards_RiderProfileId_CardType_Status] ON [app].[RiderCards] ([RiderProfileId], [CardType], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RiderHealthCards_CardNumberLookupHash] ON [app].[RiderHealthCards] ([CardNumberLookupHash]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_RiderHealthCards_EmployeeDocumentId] ON [app].[RiderHealthCards] ([EmployeeDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_RiderHealthCards_IsDeleted] ON [app].[RiderHealthCards] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_RiderHealthCards_PreviousCardId] ON [app].[RiderHealthCards] ([PreviousCardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RiderHealthCards_RiderProfileId_CardType] ON [app].[RiderHealthCards] ([RiderProfileId], [CardType]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_RiderHealthCards_RiderProfileId_CardType_Status] ON [app].[RiderHealthCards] ([RiderProfileId], [CardType], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Sponsors_CommercialRegistrationNumber] ON [app].[Sponsors] ([CommercialRegistrationNumber]) WHERE [CommercialRegistrationNumber] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_Sponsors_CompanyProfileId] ON [app].[Sponsors] ([CompanyProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sponsors_EmployerIdentityNumber] ON [app].[Sponsors] ([EmployerIdentityNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_Sponsors_IsDeleted] ON [app].[Sponsors] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    CREATE INDEX [IX_Sponsors_Status_RegistryNameAr] ON [app].[Sponsors] ([Status], [RegistryNameAr]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeJobTitlePeriods] ADD CONSTRAINT [FK_EmployeeJobTitlePeriods_OperatingCities_OperatingCityId] FOREIGN KEY ([OperatingCityId]) REFERENCES [app].[OperatingCities] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[EmployeeJobTitlePeriods] ADD CONSTRAINT [FK_EmployeeJobTitlePeriods_OperationalWorkTypes_OperationalWorkTypeId] FOREIGN KEY ([OperationalWorkTypeId]) REFERENCES [app].[OperationalWorkTypes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] ADD CONSTRAINT [FK_PlatformRiderAccounts_ClientContracts_ClientContractId_ClientPlatformId] FOREIGN KEY ([ClientContractId], [ClientPlatformId]) REFERENCES [app].[ClientContracts] ([Id], [ClientPlatformId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] ADD CONSTRAINT [FK_PlatformRiderAccounts_Employees_RegisteredEmployeeId] FOREIGN KEY ([RegisteredEmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] ADD CONSTRAINT [FK_PlatformRiderAccounts_OperatingCities_OperatingCityId] FOREIGN KEY ([OperatingCityId]) REFERENCES [app].[OperatingCities] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] ADD CONSTRAINT [FK_PlatformRiderAccounts_Sponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] ADD CONSTRAINT [FK_RiderClientAssignments_Employees_ActualEmployeeId] FOREIGN KEY ([ActualEmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] ADD CONSTRAINT [FK_RiderClientAssignments_PlatformRiderAccounts_PlatformRiderAccountId_ClientContractId] FOREIGN KEY ([PlatformRiderAccountId], [ClientContractId]) REFERENCES [app].[PlatformRiderAccounts] ([Id], [ClientContractId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] ADD CONSTRAINT [FK_RiderClientAssignments_RiderProfiles_RiderProfileId_ActualEmployeeId] FOREIGN KEY ([RiderProfileId], [ActualEmployeeId]) REFERENCES [app].[RiderProfiles] ([Id], [EmployeeId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    ALTER TABLE [app].[SponsoredInternalDetails] ADD CONSTRAINT [FK_SponsoredInternalDetails_Sponsors_CurrentSponsorId] FOREIGN KEY ([CurrentSponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822113817_AddEmployeeRiderComplianceModels'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822113817_AddEmployeeRiderComplianceModels', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822123741_SeedPermissionCatalog'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000001'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض حسابات المستخدمين وحالتها.'', N''View user accounts and their status.'', 1, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''users.read'', N''عرض المستخدمين'', N''Read users'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000002'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إنشاء حسابات مستخدمين جديدة.'', N''Create new user accounts.'', 2, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''users.create'', N''إنشاء المستخدمين'', N''Create users'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000003'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل حالة وبيانات حسابات المستخدمين.'', N''Update user account details and status.'', 3, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''users.update'', N''تعديل المستخدمين'', N''Update users'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000004'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''أرشفة حساب مستخدم وإبطال جلساته دون حذف بياناته.'', N''Archive a user and revoke sessions without deleting records.'', 4, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''users.archive'', N''أرشفة المستخدمين'', N''Archive users'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000005'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض الأدوار وقوالب الصلاحيات.'', N''View roles and permission templates.'', 5, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''roles.read'', N''عرض الأدوار'', N''Read roles'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000006'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة الأدوار غير المحمية وتعيينها للمستخدمين.'', N''Manage non-protected roles and user role assignments.'', 6, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''roles.manage'', N''إدارة الأدوار'', N''Manage roles'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000007'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض كتالوج الصلاحيات والمنح والمنع.'', N''View the permission catalog, grants, and denies.'', 7, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''permissions.read'', N''عرض الصلاحيات'', N''Read permissions'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000008'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة منح ومنع الصلاحيات ونطاقاتها.'', N''Manage permission grants, denies, and scopes.'', 8, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''permissions.manage'', N''إدارة الصلاحيات'', N''Manage permissions'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000009'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض سجل التدقيق الأمني والتشغيلي.'', N''View security and operational audit records.'', 9, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''audit.read'', N''عرض سجل التدقيق'', N''Read audit log'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000010'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة وصول الدعم المؤقت وحالات الطوارئ.'', N''Manage temporary and break-glass support access.'', 10, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''support_access.manage'', N''إدارة وصول الدعم'', N''Manage support access'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000011'', N''Catalog'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض المدن والفروع التشغيلية.'', N''View operating cities and branches.'', 11, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''operating_cities.read'', N''عرض المدن التشغيلية'', N''Read operating cities'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000012'', N''Catalog'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إضافة وتعديل وتعطيل المدن التشغيلية.'', N''Add, update, and disable operating cities.'', 12, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''operating_cities.manage'', N''إدارة المدن التشغيلية'', N''Manage operating cities'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000013'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض بيانات الموظفين غير الحساسة.'', N''View non-sensitive employee data.'', 13, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''employees.read'', N''عرض الموظفين'', N''Read employees'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000014'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إنشاء سجلات موظفين جديدة.'', N''Create new employee records.'', 14, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''employees.create'', N''إنشاء الموظفين'', N''Create employees'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000015'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل بيانات الموظفين التشغيلية.'', N''Update operational employee data.'', 15, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''employees.update'', N''تعديل الموظفين'', N''Update employees'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000016'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''أرشفة الموظفين دون حذف تاريخهم.'', N''Archive employees without deleting their history.'', 16, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''employees.archive'', N''أرشفة الموظفين'', N''Archive employees'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000017'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض الهوية والإقامة والبيانات الشخصية المقيدة.'', N''View restricted identity, residency, and personal data.'', 17, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''employees.sensitive.read'', N''عرض بيانات الموظفين الحساسة'', N''Read sensitive employee data'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000018'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض ملفات المناديب وبياناتهم التشغيلية.'', N''View rider profiles and operational data.'', 18, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''riders.read'', N''عرض المناديب'', N''Read riders'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000019'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إنشاء وتعديل حالات وملفات المناديب.'', N''Create and update rider profiles and status.'', 19, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''riders.manage'', N''إدارة المناديب'', N''Manage riders'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000020'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض جهات الكفالة وبيانات السجل.'', N''View sponsors and registry information.'', 20, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''sponsors.read'', N''عرض الكفلاء'', N''Read sponsors'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000021'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة جهات الكفالة وفترات كفالة الموظفين.'', N''Manage sponsors and employee sponsorship periods.'', 21, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''sponsors.manage'', N''إدارة الكفلاء'', N''Manage sponsors'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000022'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض بيانات الإقامة المقيدة.'', N''View restricted residency permit data.'', 22, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''residency.read'', N''عرض الإقامات'', N''Read residency permits'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000023'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إضافة وتجديد وتحديث حالات الإقامة.'', N''Add, renew, and update residency permit status.'', 23, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''residency.manage'', N''إدارة الإقامات'', N''Manage residency permits'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000024'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض رخص القيادة وإصداراتها.'', N''View driver licenses and their versions.'', 24, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''licenses.read'', N''عرض الرخص'', N''Read driver licenses'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000025'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة إصدار وتجديد وحالة رخص القيادة.'', N''Manage driver-license issuance, renewal, and status.'', 25, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''licenses.manage'', N''إدارة الرخص'', N''Manage driver licenses'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000026'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض بطاقات السائق وتجديداتها.'', N''View rider cards and renewals.'', 26, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''rider_cards.read'', N''عرض بطاقات السائق'', N''Read rider cards'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000027'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة إصدار وتجديد بطاقات السائق.'', N''Manage rider-card issuance and renewal.'', 27, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''rider_cards.manage'', N''إدارة بطاقات السائق'', N''Manage rider cards'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000028'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض البطاقات الصحية وتجديداتها.'', N''View health cards and renewals.'', 28, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''health_cards.read'', N''عرض البطاقات الصحية'', N''Read health cards'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000029'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة إصدار وتجديد البطاقات الصحية.'', N''Manage health-card issuance and renewal.'', 29, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''health_cards.manage'', N''إدارة البطاقات الصحية'', N''Manage health cards'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000030'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض وثائق ومستويات التأمين الطبي.'', N''View medical-insurance policies and plan levels.'', 30, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''insurance.read'', N''عرض التأمين الطبي'', N''Read medical insurance'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000031'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة وثائق وتجديدات ومستويات التأمين الطبي.'', N''Manage medical-insurance policies, renewals, and levels.'', 31, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''insurance.manage'', N''إدارة التأمين الطبي'', N''Manage medical insurance'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000032'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض بيانات سندات الأمر المالية.'', N''View financial promissory-note data.'', 32, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''promissory_notes.read'', N''عرض سندات الأمر'', N''Read promissory notes'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000033'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة حالات ونسخ سندات الأمر دون حذف.'', N''Manage promissory-note status and versions without deletion.'', 33, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''promissory_notes.manage'', N''إدارة سندات الأمر'', N''Manage promissory notes'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000034'', N''Documents'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض بيانات الوثائق ونسخها دون تنزيل المحتوى.'', N''View document metadata and versions without downloading content.'', 34, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''documents.read'', N''عرض بيانات الوثائق'', N''Read document metadata'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000035'', N''Documents'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''رفع نسخة وثيقة جديدة وفق سياسة الملفات.'', N''Upload a new document version under the file policy.'', 35, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''documents.upload'', N''رفع الوثائق'', N''Upload documents'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000036'', N''Documents'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تنزيل محتوى الوثائق غير المصنفة عالية الحساسية.'', N''Download document content not classified as highly sensitive.'', 36, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''documents.download'', N''تنزيل الوثائق'', N''Download documents'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000037'', N''Documents'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تنزيل محتوى وثائق الهوية والمالية عالية الحساسية.'', N''Download highly sensitive identity and financial documents.'', 37, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''documents.download_sensitive'', N''تنزيل الوثائق الحساسة'', N''Download sensitive documents'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000038'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض حسابات منصات العملاء ضمن النطاق المسموح.'', N''View client-platform accounts within the allowed scope.'', 38, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''platform_accounts.read'', N''عرض حسابات المنصات'', N''Read platform accounts'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000039'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة التسجيل والحالة والملكية الرسمية لحسابات المنصات ضمن النطاق.'', N''Manage registration, status, and official ownership of platform accounts within scope.'', 39, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''platform_accounts.manage'', N''إدارة حسابات المنصات'', N''Manage platform accounts'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000040'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض تاريخ الاستخدام الفعلي لحسابات المنصات ضمن النطاق.'', N''View actual platform-account usage history within scope.'', 40, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''platform_assignments.read'', N''عرض تكليفات المنصات'', N''Read platform assignments'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000041'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة تكليفات الاستخدام الفعلي مع حفظ التاريخ ضمن النطاق.'', N''Manage actual-use assignments while preserving history within scope.'', 41, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''platform_assignments.manage'', N''إدارة تكليفات المنصات'', N''Manage platform assignments'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000042'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض السكن وفترات الإقامة ضمن النطاق المسموح.'', N''View housing and residence periods within the allowed scope.'', 42, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''housing.read'', N''عرض السكن'', N''Read housing'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1);
    INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000043'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة السكن والمشرفين وفترات الإقامة ضمن النطاق.'', N''Manage housing, supervisors, and residence periods within scope.'', 43, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''housing.manage'', N''إدارة السكن'', N''Manage housing'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000044'', N''Reporting'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض التقارير التشغيلية المصرح بها.'', N''View authorized operational reports.'', 44, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''reports.read'', N''عرض التقارير'', N''Read reports'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000045'', N''Reporting'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إنشاء ملفات تصدير من البيانات المصرح بها فقط.'', N''Create export files from authorized data only.'', 45, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''exports.create'', N''إنشاء التصديرات'', N''Create exports'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000046'', N''Reporting'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض الإشعارات التشغيلية.'', N''View operational notifications.'', 46, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''notifications.read'', N''عرض الإشعارات'', N''Read notifications'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000047'', N''Reporting'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة حالة ومحتوى الإشعارات التشغيلية.'', N''Manage operational notification status and content.'', 47, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''notifications.manage'', N''إدارة الإشعارات'', N''Manage notifications'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000048'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض طلبات الإجازة وتاريخها.'', N''View leave requests and history.'', 48, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''leave_requests.read'', N''عرض طلبات الإجازة'', N''Read leave requests'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000049'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إنشاء وتعديل طلبات الإجازة وفق حالتها.'', N''Create and update leave requests according to their state.'', 49, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''leave_requests.manage'', N''إدارة طلبات الإجازة'', N''Manage leave requests'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000050'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''الموافقة أو الرفض الموثق لطلبات الإجازة.'', N''Record approval or rejection decisions for leave requests.'', 50, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''leave_requests.approve'', N''اعتماد طلبات الإجازة'', N''Approve leave requests'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000051'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض حالات الغياب والهروب وسجل أحداثها.'', N''View absence and escaped-employee cases and their events.'', 51, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''absence_cases.read'', N''عرض حالات الغياب'', N''Read absence cases'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000052'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة حالات الغياب والهروب مع حفظ سجل الأحداث.'', N''Manage absence and escaped-employee cases while preserving event history.'', 52, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''absence_cases.manage'', N''إدارة حالات الغياب'', N''Manage absence cases'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000053'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض طلبات تغيير حالة الموظف.'', N''View employee status-change requests.'', 53, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''employee_status_changes.read'', N''عرض طلبات تغيير الحالة'', N''Read employee status changes'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000054'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إنشاء وتحديث طلبات تغيير حالة الموظف.'', N''Create and update employee status-change requests.'', 54, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''employee_status_changes.manage'', N''إدارة طلبات تغيير الحالة'', N''Manage employee status changes'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000055'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''اعتماد تغيير حالة الموظف مع حفظ الأثر التاريخي.'', N''Approve employee status changes while preserving history.'', 55, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''employee_status_changes.approve'', N''اعتماد تغيير حالة الموظف'', N''Approve employee status changes'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822123741_SeedPermissionCatalog'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822123741_SeedPermissionCatalog', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822134838_SeedPrimarySponsors'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActiveFrom', N'ActiveTo', N'CommercialRegistrationNumber', N'CompanyProfileId', N'ContactEmail', N'ContactName', N'ContactPhone', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'EmployerIdentityNumber', N'IsDeleted', N'Notes', N'RegistryNameAr', N'RegistryNameEn', N'SponsorType', N'Status', N'UnifiedNationalNumber', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[app].[Sponsors]'))
        SET IDENTITY_INSERT [app].[Sponsors] ON;
    EXEC(N'INSERT INTO [app].[Sponsors] ([Id], [ActiveFrom], [ActiveTo], [CommercialRegistrationNumber], [CompanyProfileId], [ContactEmail], [ContactName], [ContactPhone], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [EmployerIdentityNumber], [IsDeleted], [Notes], [RegistryNameAr], [RegistryNameEn], [SponsorType], [Status], [UnifiedNationalNumber], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-8000-000000000040'', ''2026-01-01'', NULL, NULL, ''019c18d5-62e1-7000-8000-000000000001'', NULL, NULL, NULL, ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''7038745530'', CAST(0 AS bit), NULL, N''مؤسسة البوابة التجارية'', NULL, 1, 1, NULL, NULL, NULL),
    (''019c18d5-62e1-7000-8000-000000000041'', ''2026-01-01'', NULL, NULL, ''019c18d5-62e1-7000-8000-000000000001'', NULL, NULL, NULL, ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''7015658094'', CAST(0 AS bit), NULL, N''شركة البوابة المقبلة'', NULL, 2, 1, NULL, NULL, NULL),
    (''019c18d5-62e1-7000-8000-000000000042'', ''2026-01-01'', NULL, NULL, ''019c18d5-62e1-7000-8000-000000000001'', NULL, NULL, NULL, ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''7034861059'', CAST(0 AS bit), NULL, N''اكسبرس جايت'', NULL, 2, 1, NULL, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ActiveFrom', N'ActiveTo', N'CommercialRegistrationNumber', N'CompanyProfileId', N'ContactEmail', N'ContactName', N'ContactPhone', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'EmployerIdentityNumber', N'IsDeleted', N'Notes', N'RegistryNameAr', N'RegistryNameEn', N'SponsorType', N'Status', N'UnifiedNationalNumber', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[app].[Sponsors]'))
        SET IDENTITY_INSERT [app].[Sponsors] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822134838_SeedPrimarySponsors'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822134838_SeedPrimarySponsors', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[FleetCommandReceipts] (
        [Id] uniqueidentifier NOT NULL,
        [CommandName] nvarchar(100) NOT NULL,
        [IdempotencyKey] nvarchar(200) NOT NULL,
        [RequestHash] nchar(64) NOT NULL,
        [ResultEntityId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_FleetCommandReceipts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[FleetLocations] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [LocationType] int NOT NULL,
        [HousingId] uniqueidentifier NULL,
        [Address] nvarchar(1000) NULL,
        [Latitude] decimal(9,6) NULL,
        [Longitude] decimal(9,6) NULL,
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
        CONSTRAINT [PK_FleetLocations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_FleetLocations_Housing] CHECK (([LocationType] = 2 AND [HousingId] IS NOT NULL) OR ([LocationType] <> 2 AND [HousingId] IS NULL)),
        CONSTRAINT [FK_FleetLocations_Housing_HousingId] FOREIGN KEY ([HousingId]) REFERENCES [app].[Housing] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleManufacturers] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [Status] int NOT NULL,
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
        CONSTRAINT [PK_VehicleManufacturers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleModels] (
        [Id] uniqueidentifier NOT NULL,
        [VehicleManufacturerId] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [VehicleType] int NOT NULL,
        [DefaultFuelType] int NOT NULL,
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
        CONSTRAINT [PK_VehicleModels] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_VehicleModels_Id_VehicleManufacturerId] UNIQUE ([Id], [VehicleManufacturerId]),
        CONSTRAINT [FK_VehicleModels_VehicleManufacturers_VehicleManufacturerId] FOREIGN KEY ([VehicleManufacturerId]) REFERENCES [app].[VehicleManufacturers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[Vehicles] (
        [Id] uniqueidentifier NOT NULL,
        [AssetNumber] nvarchar(64) NOT NULL,
        [NormalizedAssetNumber] nvarchar(64) NOT NULL,
        [PlateNumberAr] nvarchar(32) NULL,
        [NormalizedPlateNumberAr] nvarchar(32) NULL,
        [PlateNumberEn] nvarchar(32) NULL,
        [NormalizedPlateNumberEn] nvarchar(32) NULL,
        [PlateLettersAr] nvarchar(8) NULL,
        [PlateLettersEn] nvarchar(8) NULL,
        [PlateDigits] nvarchar(8) NULL,
        [Vin] nvarchar(64) NULL,
        [ChassisNumber] nvarchar(100) NULL,
        [EngineNumber] nvarchar(100) NULL,
        [VehicleManufacturerId] uniqueidentifier NOT NULL,
        [VehicleModelId] uniqueidentifier NOT NULL,
        [ModelYear] int NULL,
        [VehicleType] int NOT NULL,
        [FuelType] int NOT NULL,
        [TransmissionType] int NOT NULL,
        [ColorAr] nvarchar(100) NULL,
        [ColorEn] nvarchar(100) NULL,
        [OwnershipType] int NOT NULL,
        [OwnerName] nvarchar(200) NULL,
        [AcquisitionDate] date NULL,
        [LeaseReference] nvarchar(200) NULL,
        [CurrentLocationId] uniqueidentifier NULL,
        [CurrentOdometer] bigint NOT NULL,
        [LastOdometerAtUtc] datetimeoffset NULL,
        [CurrentOperationalStatus] int NOT NULL,
        [CurrentAssignmentId] uniqueidentifier NULL,
        [DecommissionedAtUtc] datetimeoffset NULL,
        [DecommissionReason] nvarchar(1000) NULL,
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
        CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Vehicles_ModelYear] CHECK ([ModelYear] IS NULL OR ([ModelYear] >= 1950 AND [ModelYear] <= 2200)),
        CONSTRAINT [CK_Vehicles_Odometer] CHECK ([CurrentOdometer] >= 0),
        CONSTRAINT [FK_Vehicles_FleetLocations_CurrentLocationId] FOREIGN KEY ([CurrentLocationId]) REFERENCES [app].[FleetLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Vehicles_VehicleManufacturers_VehicleManufacturerId] FOREIGN KEY ([VehicleManufacturerId]) REFERENCES [app].[VehicleManufacturers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Vehicles_VehicleModels_VehicleModelId_VehicleManufacturerId] FOREIGN KEY ([VehicleModelId], [VehicleManufacturerId]) REFERENCES [app].[VehicleModels] ([Id], [VehicleManufacturerId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[RiderVehicleAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [VehicleId] uniqueidentifier NOT NULL,
        [OperationId] uniqueidentifier NOT NULL,
        [PreviousAssignmentId] uniqueidentifier NULL,
        [StartedAtUtc] datetimeoffset NOT NULL,
        [StartLocationId] uniqueidentifier NULL,
        [StartOdometer] bigint NOT NULL,
        [StartVehicleCondition] int NOT NULL,
        [StartFuelLevelPercentage] tinyint NULL,
        [EndedAtUtc] datetimeoffset NULL,
        [EndLocationId] uniqueidentifier NULL,
        [EndOdometer] bigint NULL,
        [EndVehicleCondition] int NULL,
        [EndFuelLevelPercentage] tinyint NULL,
        [PermissionReference] nvarchar(200) NULL,
        [PermissionStartsOn] date NULL,
        [PermissionEndsOn] date NULL,
        [Status] int NOT NULL,
        [AssignmentReason] nvarchar(1000) NOT NULL,
        [CompletionReason] nvarchar(1000) NULL,
        [AssignedByUserId] uniqueidentifier NOT NULL,
        [EndedByUserId] uniqueidentifier NULL,
        [WasBackdated] bit NOT NULL,
        [BackdatedReason] nvarchar(1000) NULL,
        [CorrectionOfAssignmentId] uniqueidentifier NULL,
        [CorrectionReason] nvarchar(1000) NULL,
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
        CONSTRAINT [PK_RiderVehicleAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RiderVehicleAssignments_Backdated] CHECK ([WasBackdated] = 0 OR [BackdatedReason] IS NOT NULL),
        CONSTRAINT [CK_RiderVehicleAssignments_EndFuel] CHECK ([EndFuelLevelPercentage] IS NULL OR [EndFuelLevelPercentage] BETWEEN 0 AND 100),
        CONSTRAINT [CK_RiderVehicleAssignments_Odometer] CHECK ([StartOdometer] >= 0 AND ([EndOdometer] IS NULL OR [EndOdometer] >= [StartOdometer] OR [CorrectionReason] IS NOT NULL)),
        CONSTRAINT [CK_RiderVehicleAssignments_Permission] CHECK ([PermissionEndsOn] IS NULL OR [PermissionStartsOn] IS NULL OR [PermissionEndsOn] >= [PermissionStartsOn]),
        CONSTRAINT [CK_RiderVehicleAssignments_StartFuel] CHECK ([StartFuelLevelPercentage] IS NULL OR [StartFuelLevelPercentage] BETWEEN 0 AND 100),
        CONSTRAINT [CK_RiderVehicleAssignments_TimeRange] CHECK ([EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]),
        CONSTRAINT [FK_RiderVehicleAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderVehicleAssignments_FleetLocations_EndLocationId] FOREIGN KEY ([EndLocationId]) REFERENCES [app].[FleetLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderVehicleAssignments_FleetLocations_StartLocationId] FOREIGN KEY ([StartLocationId]) REFERENCES [app].[FleetLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderVehicleAssignments_RiderProfiles_RiderProfileId_EmployeeId] FOREIGN KEY ([RiderProfileId], [EmployeeId]) REFERENCES [app].[RiderProfiles] ([Id], [EmployeeId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderVehicleAssignments_RiderVehicleAssignments_CorrectionOfAssignmentId] FOREIGN KEY ([CorrectionOfAssignmentId]) REFERENCES [app].[RiderVehicleAssignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderVehicleAssignments_RiderVehicleAssignments_PreviousAssignmentId] FOREIGN KEY ([PreviousAssignmentId]) REFERENCES [app].[RiderVehicleAssignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiderVehicleAssignments_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [app].[Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleInsurancePolicies] (
        [Id] uniqueidentifier NOT NULL,
        [VehicleId] uniqueidentifier NOT NULL,
        [ProviderName] nvarchar(200) NOT NULL,
        [PolicyNumber] nvarchar(150) NOT NULL,
        [CoverageType] nvarchar(200) NULL,
        [EffectiveFrom] date NOT NULL,
        [ExpiryDate] date NOT NULL,
        [ClaimReference] nvarchar(200) NULL,
        [ClaimContact] nvarchar(200) NULL,
        [Status] int NOT NULL,
        [IsCurrent] bit NOT NULL,
        [PreviousRecordId] uniqueidentifier NULL,
        [ProofAttachmentId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_VehicleInsurancePolicies] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleInsurancePolicies_DateRange] CHECK ([ExpiryDate] >= [EffectiveFrom]),
        CONSTRAINT [FK_VehicleInsurancePolicies_VehicleInsurancePolicies_PreviousRecordId] FOREIGN KEY ([PreviousRecordId]) REFERENCES [app].[VehicleInsurancePolicies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleInsurancePolicies_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [app].[Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleOdometerReadings] (
        [Id] uniqueidentifier NOT NULL,
        [VehicleId] uniqueidentifier NOT NULL,
        [Reading] bigint NOT NULL,
        [RecordedAtUtc] datetimeoffset NOT NULL,
        [SourceType] int NOT NULL,
        [SourceEntityId] uniqueidentifier NULL,
        [EvidenceAttachmentId] uniqueidentifier NULL,
        [Notes] nvarchar(1000) NULL,
        [IsCorrection] bit NOT NULL,
        [CorrectionReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_VehicleOdometerReadings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleOdometerReadings_Correction] CHECK ([IsCorrection] = 0 OR [CorrectionReason] IS NOT NULL),
        CONSTRAINT [CK_VehicleOdometerReadings_Value] CHECK ([Reading] >= 0),
        CONSTRAINT [FK_VehicleOdometerReadings_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [app].[Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleOperationalStatusPeriods] (
        [Id] uniqueidentifier NOT NULL,
        [VehicleId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [EffectiveFromUtc] datetimeoffset NOT NULL,
        [EffectiveToUtc] datetimeoffset NULL,
        [ReasonCode] nvarchar(100) NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [SourceType] int NOT NULL,
        [SourceEntityId] uniqueidentifier NULL,
        [ChangedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_VehicleOperationalStatusPeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleStatusPeriods_Range] CHECK ([EffectiveToUtc] IS NULL OR [EffectiveToUtc] >= [EffectiveFromUtc]),
        CONSTRAINT [FK_VehicleOperationalStatusPeriods_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [app].[Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehiclePeriodicInspections] (
        [Id] uniqueidentifier NOT NULL,
        [VehicleId] uniqueidentifier NOT NULL,
        [InspectionNumber] nvarchar(150) NOT NULL,
        [StationName] nvarchar(200) NOT NULL,
        [InspectionDate] date NOT NULL,
        [ExpiryDate] date NOT NULL,
        [Result] int NOT NULL,
        [Odometer] bigint NULL,
        [FailureNotes] nvarchar(2000) NULL,
        [Status] int NOT NULL,
        [IsCurrent] bit NOT NULL,
        [PreviousRecordId] uniqueidentifier NULL,
        [ProofAttachmentId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_VehiclePeriodicInspections] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehiclePeriodicInspections_DateRange] CHECK ([ExpiryDate] >= [InspectionDate]),
        CONSTRAINT [CK_VehiclePeriodicInspections_Odometer] CHECK ([Odometer] IS NULL OR [Odometer] >= 0),
        CONSTRAINT [FK_VehiclePeriodicInspections_VehiclePeriodicInspections_PreviousRecordId] FOREIGN KEY ([PreviousRecordId]) REFERENCES [app].[VehiclePeriodicInspections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehiclePeriodicInspections_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [app].[Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleRegistrations] (
        [Id] uniqueidentifier NOT NULL,
        [VehicleId] uniqueidentifier NOT NULL,
        [RegistrationNumber] nvarchar(150) NOT NULL,
        [IssuingAuthority] nvarchar(200) NOT NULL,
        [IssueDate] date NOT NULL,
        [ExpiryDate] date NOT NULL,
        [Status] int NOT NULL,
        [IsCurrent] bit NOT NULL,
        [PreviousRecordId] uniqueidentifier NULL,
        [ProofAttachmentId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_VehicleRegistrations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleRegistrations_DateRange] CHECK ([ExpiryDate] >= [IssueDate]),
        CONSTRAINT [FK_VehicleRegistrations_VehicleRegistrations_PreviousRecordId] FOREIGN KEY ([PreviousRecordId]) REFERENCES [app].[VehicleRegistrations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleRegistrations_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [app].[Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[RiderVehicleAssignmentEvents] (
        [Id] uniqueidentifier NOT NULL,
        [RiderVehicleAssignmentId] uniqueidentifier NOT NULL,
        [OperationId] uniqueidentifier NOT NULL,
        [EventType] int NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [ChangeSnapshotJson] nvarchar(max) NULL,
        [CorrelationId] nvarchar(100) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_RiderVehicleAssignmentEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RiderVehicleAssignmentEvents_RiderVehicleAssignments_RiderVehicleAssignmentId] FOREIGN KEY ([RiderVehicleAssignmentId]) REFERENCES [app].[RiderVehicleAssignments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleIssues] (
        [Id] uniqueidentifier NOT NULL,
        [IssueNumber] nvarchar(64) NOT NULL,
        [VehicleId] uniqueidentifier NOT NULL,
        [Category] int NOT NULL,
        [Severity] int NOT NULL,
        [Description] nvarchar(4000) NOT NULL,
        [ReportedAtUtc] datetimeoffset NOT NULL,
        [LocationId] uniqueidentifier NULL,
        [OdometerAtReport] bigint NULL,
        [RelatedAssignmentId] uniqueidentifier NULL,
        [BlocksOperation] bit NOT NULL,
        [Status] int NOT NULL,
        [ReportedByUserId] uniqueidentifier NOT NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [ResolvedByUserId] uniqueidentifier NULL,
        [ResolvedAtUtc] datetimeoffset NULL,
        [ResolutionSummary] nvarchar(4000) NULL,
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
        CONSTRAINT [PK_VehicleIssues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VehicleIssues_FleetLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [app].[FleetLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleIssues_RiderVehicleAssignments_RelatedAssignmentId] FOREIGN KEY ([RelatedAssignmentId]) REFERENCES [app].[RiderVehicleAssignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleIssues_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [app].[Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleIssueEvents] (
        [Id] uniqueidentifier NOT NULL,
        [VehicleIssueId] uniqueidentifier NOT NULL,
        [EventType] int NOT NULL,
        [FromStatus] int NULL,
        [ToStatus] int NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [SnapshotJson] nvarchar(max) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_VehicleIssueEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VehicleIssueEvents_VehicleIssues_VehicleIssueId] FOREIGN KEY ([VehicleIssueId]) REFERENCES [app].[VehicleIssues] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleAccidentAttachments] (
        [Id] uniqueidentifier NOT NULL,
        [VehicleAccidentId] uniqueidentifier NOT NULL,
        [EvidenceType] int NOT NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [StoredFileName] nvarchar(255) NOT NULL,
        [ContentType] nvarchar(150) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [Sha256Checksum] nchar(64) NOT NULL,
        [StoragePath] nvarchar(1000) NOT NULL,
        [UploadedByUserId] uniqueidentifier NOT NULL,
        [UploadedAtUtc] datetimeoffset NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_VehicleAccidentAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleAccidentAttachments_Size] CHECK ([FileSizeBytes] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleAccidentEvents] (
        [Id] uniqueidentifier NOT NULL,
        [VehicleAccidentId] uniqueidentifier NOT NULL,
        [EventType] int NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [SnapshotJson] nvarchar(max) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_VehicleAccidentEvents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleAccidentReportVersions] (
        [Id] uniqueidentifier NOT NULL,
        [VehicleAccidentId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [ReportNumber] nvarchar(100) NOT NULL,
        [SnapshotJson] nvarchar(max) NOT NULL,
        [StoredFileName] nvarchar(255) NOT NULL,
        [StoragePath] nvarchar(1000) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [Sha256Checksum] nchar(64) NOT NULL,
        [GeneratedAtUtc] datetimeoffset NOT NULL,
        [GeneratedByUserId] uniqueidentifier NOT NULL,
        [SupersedesReportVersionId] uniqueidentifier NULL,
        [CorrectionReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_VehicleAccidentReportVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleAccidentReportVersions_Size] CHECK ([FileSizeBytes] > 0),
        CONSTRAINT [CK_VehicleAccidentReportVersions_Version] CHECK ([VersionNumber] > 0),
        CONSTRAINT [FK_VehicleAccidentReportVersions_VehicleAccidentReportVersions_SupersedesReportVersionId] FOREIGN KEY ([SupersedesReportVersionId]) REFERENCES [app].[VehicleAccidentReportVersions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleAccidents] (
        [Id] uniqueidentifier NOT NULL,
        [AccidentNumber] nvarchar(64) NOT NULL,
        [VehicleId] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [RiderVehicleAssignmentId] uniqueidentifier NOT NULL,
        [VehicleIssueId] uniqueidentifier NOT NULL,
        [VehicleInsurancePolicyId] uniqueidentifier NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [ReportedAtUtc] datetimeoffset NOT NULL,
        [LocationId] uniqueidentifier NULL,
        [LocationDescription] nvarchar(1000) NOT NULL,
        [Latitude] decimal(9,6) NULL,
        [Longitude] decimal(9,6) NULL,
        [PoliceReportNumber] nvarchar(150) NULL,
        [InsuranceClaimNumber] nvarchar(150) NULL,
        [Severity] int NOT NULL,
        [IsDrivable] bit NOT NULL,
        [HasInjuries] bit NOT NULL,
        [InjuryDetails] nvarchar(4000) NULL,
        [ThirdPartyDetails] nvarchar(4000) NULL,
        [DamageDescription] nvarchar(4000) NOT NULL,
        [FaultAssessment] nvarchar(2000) NULL,
        [Narrative] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [ReportedByUserId] uniqueidentifier NOT NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [CurrentReportVersionId] uniqueidentifier NULL,
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
        CONSTRAINT [PK_VehicleAccidents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VehicleAccidents_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleAccidents_FleetLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [app].[FleetLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleAccidents_RiderProfiles_RiderProfileId_EmployeeId] FOREIGN KEY ([RiderProfileId], [EmployeeId]) REFERENCES [app].[RiderProfiles] ([Id], [EmployeeId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleAccidents_RiderVehicleAssignments_RiderVehicleAssignmentId] FOREIGN KEY ([RiderVehicleAssignmentId]) REFERENCES [app].[RiderVehicleAssignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleAccidents_VehicleAccidentReportVersions_CurrentReportVersionId] FOREIGN KEY ([CurrentReportVersionId]) REFERENCES [app].[VehicleAccidentReportVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleAccidents_VehicleInsurancePolicies_VehicleInsurancePolicyId] FOREIGN KEY ([VehicleInsurancePolicyId]) REFERENCES [app].[VehicleInsurancePolicies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleAccidents_VehicleIssues_VehicleIssueId] FOREIGN KEY ([VehicleIssueId]) REFERENCES [app].[VehicleIssues] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleAccidents_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [app].[Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleAttachments] (
        [Id] uniqueidentifier NOT NULL,
        [VehicleId] uniqueidentifier NOT NULL,
        [Category] int NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
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
        CONSTRAINT [PK_VehicleAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VehicleAttachments_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [app].[Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE TABLE [app].[VehicleAttachmentVersions] (
        [Id] uniqueidentifier NOT NULL,
        [VehicleAttachmentId] uniqueidentifier NOT NULL,
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
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_VehicleAttachmentVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VehicleAttachmentVersions_Size] CHECK ([FileSizeBytes] > 0),
        CONSTRAINT [CK_VehicleAttachmentVersions_Version] CHECK ([VersionNumber] > 0),
        CONSTRAINT [FK_VehicleAttachmentVersions_VehicleAttachmentVersions_SupersededVersionId] FOREIGN KEY ([SupersededVersionId]) REFERENCES [app].[VehicleAttachmentVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleAttachmentVersions_VehicleAttachments_VehicleAttachmentId] FOREIGN KEY ([VehicleAttachmentId]) REFERENCES [app].[VehicleAttachments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000056'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض المركبات ومواقعها وحالتها.'', N''View vehicles, locations, and operational status.'', 56, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.vehicles.read'', N''عرض المركبات'', N''Read vehicles'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000057'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إنشاء وتعديل المركبات وحالتها التشغيلية.'', N''Create and update vehicles and their operational status.'', 57, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.vehicles.manage'', N''إدارة المركبات'', N''Manage vehicles'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000058'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''أرشفة واستعادة المركبات غير المستخدمة.'', N''Archive and restore unused vehicles.'', 58, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''fleet.vehicles.archive'', N''أرشفة المركبات'', N''Archive vehicles'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000059'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إنهاء خدمة مركبة بشكل تشغيلي نهائي.'', N''Operationally decommission a vehicle.'', 59, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''fleet.vehicles.decommission'', N''إنهاء خدمة المركبات'', N''Decommission vehicles'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000060'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض العهد الحالية والتاريخية بين الرايدرز والمركبات.'', N''View current and historical rider-vehicle assignments.'', 60, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.assignments.read'', N''عرض عهد المركبات'', N''Read vehicle assignments'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000061'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تنفيذ الاستلام والإرجاع والتبديل وتجديد التصريح.'', N''Execute take, return, switch, and permission renewal.'', 61, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.assignments.manage'', N''إدارة عهد المركبات'', N''Manage vehicle assignments'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000062'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تصحيح العهد التاريخية مع سبب إلزامي.'', N''Correct historical assignments with a mandatory reason.'', 62, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''fleet.assignments.correct'', N''تصحيح عهد المركبات'', N''Correct vehicle assignments'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000063'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض بلاغات وأعطال المركبات.'', N''View vehicle issues and faults.'', 63, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.issues.read'', N''عرض بلاغات المركبات'', N''Read vehicle issues'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000064'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تسجيل ومراجعة وحل وإغلاق البلاغات.'', N''Report, review, resolve, and close vehicle issues.'', 64, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.issues.manage'', N''إدارة بلاغات المركبات'', N''Manage vehicle issues'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000065'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض التسجيل والتأمين والفحص الدوري.'', N''View vehicle registration, insurance, and inspection.'', 65, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.compliance.read'', N''عرض التزام المركبات'', N''Read vehicle compliance'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000066'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إضافة وتجديد وثائق التزام المركبات.'', N''Add and renew vehicle compliance records.'', 66, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.compliance.manage'', N''إدارة التزام المركبات'', N''Manage vehicle compliance'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000067'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض بيانات ونسخ ملفات المركبات.'', N''View vehicle file metadata and versions.'', 67, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''fleet.files.read'', N''عرض ملفات المركبات'', N''Read vehicle files'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000068'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''رفع وأرشفة نسخ ملفات المركبات.'', N''Upload and archive vehicle file versions.'', 68, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''fleet.files.upload'', N''رفع ملفات المركبات'', N''Upload vehicle files'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000069'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تنزيل محتوى ملفات المركبات الخاصة.'', N''Download private vehicle file content.'', 69, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''fleet.files.download'', N''تنزيل ملفات المركبات'', N''Download vehicle files'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000070'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض بيانات الحوادث والأدلة.'', N''View vehicle accidents and evidence.'', 70, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''fleet.accidents.read'', N''عرض حوادث المركبات'', N''Read vehicle accidents'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000071'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تسجيل حادث مرتبط برايدر وعهدة فعالة.'', N''Report an accident linked to a rider and active assignment.'', 71, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''fleet.accidents.report'', N''تسجيل حوادث المركبات'', N''Report vehicle accidents'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000072'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''اعتماد وتصحيح وإغلاق تقارير الحوادث.'', N''Finalize, correct, and close accident reports.'', 72, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''fleet.accidents.finalize'', N''اعتماد تقارير الحوادث'', N''Finalize accident reports'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000073'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تنزيل الأدلة وتقارير الحوادث الخاصة.'', N''Download private accident evidence and reports.'', 73, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''fleet.accidents.download'', N''تنزيل تقارير الحوادث'', N''Download accident reports'', NULL, CAST(0 AS bit), CAST(1 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000074'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تنفيذ تصحيحات العداد والحالة عالية الثقة.'', N''Perform high-trust odometer and status corrections.'', 74, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''fleet.corrections.manage'', N''تصحيح بيانات الأسطول'', N''Manage fleet corrections'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FleetCommandReceipts_CommandName_IdempotencyKey] ON [app].[FleetCommandReceipts] ([CommandName], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FleetLocations_Code] ON [app].[FleetLocations] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_FleetLocations_HousingId] ON [app].[FleetLocations] ([HousingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_FleetLocations_IsDeleted] ON [app].[FleetLocations] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_FleetLocations_LocationType_Status] ON [app].[FleetLocations] ([LocationType], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_RiderVehicleAssignmentEvents_OperationId] ON [app].[RiderVehicleAssignmentEvents] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_RiderVehicleAssignmentEvents_RiderVehicleAssignmentId_OccurredAtUtc] ON [app].[RiderVehicleAssignmentEvents] ([RiderVehicleAssignmentId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_RiderVehicleAssignments_CorrectionOfAssignmentId] ON [app].[RiderVehicleAssignments] ([CorrectionOfAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_RiderVehicleAssignments_EmployeeId] ON [app].[RiderVehicleAssignments] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_RiderVehicleAssignments_EndLocationId] ON [app].[RiderVehicleAssignments] ([EndLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_RiderVehicleAssignments_IsDeleted] ON [app].[RiderVehicleAssignments] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_RiderVehicleAssignments_PreviousAssignmentId] ON [app].[RiderVehicleAssignments] ([PreviousAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RiderVehicleAssignments_RiderProfileId] ON [app].[RiderVehicleAssignments] ([RiderProfileId]) WHERE [EndedAtUtc] IS NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_RiderVehicleAssignments_RiderProfileId_EmployeeId] ON [app].[RiderVehicleAssignments] ([RiderProfileId], [EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_RiderVehicleAssignments_RiderProfileId_StartedAtUtc] ON [app].[RiderVehicleAssignments] ([RiderProfileId], [StartedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_RiderVehicleAssignments_StartLocationId] ON [app].[RiderVehicleAssignments] ([StartLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RiderVehicleAssignments_VehicleId] ON [app].[RiderVehicleAssignments] ([VehicleId]) WHERE [EndedAtUtc] IS NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_RiderVehicleAssignments_VehicleId_StartedAtUtc] ON [app].[RiderVehicleAssignments] ([VehicleId], [StartedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidentAttachments_IsDeleted] ON [app].[VehicleAccidentAttachments] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidentAttachments_VehicleAccidentId_IsDeleted] ON [app].[VehicleAccidentAttachments] ([VehicleAccidentId], [IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidentEvents_VehicleAccidentId_OccurredAtUtc] ON [app].[VehicleAccidentEvents] ([VehicleAccidentId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleAccidentReportVersions_ReportNumber] ON [app].[VehicleAccidentReportVersions] ([ReportNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidentReportVersions_SupersedesReportVersionId] ON [app].[VehicleAccidentReportVersions] ([SupersedesReportVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleAccidentReportVersions_VehicleAccidentId_VersionNumber] ON [app].[VehicleAccidentReportVersions] ([VehicleAccidentId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleAccidents_AccidentNumber] ON [app].[VehicleAccidents] ([AccidentNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidents_CurrentReportVersionId] ON [app].[VehicleAccidents] ([CurrentReportVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidents_EmployeeId] ON [app].[VehicleAccidents] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidents_IsDeleted] ON [app].[VehicleAccidents] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidents_LocationId] ON [app].[VehicleAccidents] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidents_RiderProfileId_EmployeeId] ON [app].[VehicleAccidents] ([RiderProfileId], [EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidents_RiderProfileId_OccurredAtUtc] ON [app].[VehicleAccidents] ([RiderProfileId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidents_RiderVehicleAssignmentId] ON [app].[VehicleAccidents] ([RiderVehicleAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidents_VehicleId_OccurredAtUtc] ON [app].[VehicleAccidents] ([VehicleId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidents_VehicleInsurancePolicyId] ON [app].[VehicleAccidents] ([VehicleInsurancePolicyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAccidents_VehicleIssueId] ON [app].[VehicleAccidents] ([VehicleIssueId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAttachments_CurrentVersionId] ON [app].[VehicleAttachments] ([CurrentVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAttachments_IsDeleted] ON [app].[VehicleAttachments] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAttachments_VehicleId_IsDeleted] ON [app].[VehicleAttachments] ([VehicleId], [IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleAttachmentVersions_SupersededVersionId] ON [app].[VehicleAttachmentVersions] ([SupersededVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleAttachmentVersions_VehicleAttachmentId_VersionNumber] ON [app].[VehicleAttachmentVersions] ([VehicleAttachmentId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleInsurancePolicies_ExpiryDate_IsCurrent] ON [app].[VehicleInsurancePolicies] ([ExpiryDate], [IsCurrent]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleInsurancePolicies_IsDeleted] ON [app].[VehicleInsurancePolicies] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleInsurancePolicies_PreviousRecordId] ON [app].[VehicleInsurancePolicies] ([PreviousRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VehicleInsurancePolicies_VehicleId] ON [app].[VehicleInsurancePolicies] ([VehicleId]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleInsurancePolicies_VehicleId_PolicyNumber] ON [app].[VehicleInsurancePolicies] ([VehicleId], [PolicyNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleIssueEvents_VehicleIssueId_OccurredAtUtc] ON [app].[VehicleIssueEvents] ([VehicleIssueId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleIssues_IsDeleted] ON [app].[VehicleIssues] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleIssues_IssueNumber] ON [app].[VehicleIssues] ([IssueNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleIssues_LocationId] ON [app].[VehicleIssues] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleIssues_RelatedAssignmentId] ON [app].[VehicleIssues] ([RelatedAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleIssues_VehicleId_Status_BlocksOperation] ON [app].[VehicleIssues] ([VehicleId], [Status], [BlocksOperation]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleManufacturers_Code] ON [app].[VehicleManufacturers] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleManufacturers_IsDeleted] ON [app].[VehicleManufacturers] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleModels_IsDeleted] ON [app].[VehicleModels] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleModels_VehicleManufacturerId_Code] ON [app].[VehicleModels] ([VehicleManufacturerId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleOdometerReadings_VehicleId_RecordedAtUtc] ON [app].[VehicleOdometerReadings] ([VehicleId], [RecordedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleOperationalStatusPeriods_IsDeleted] ON [app].[VehicleOperationalStatusPeriods] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VehicleOperationalStatusPeriods_VehicleId] ON [app].[VehicleOperationalStatusPeriods] ([VehicleId]) WHERE [EffectiveToUtc] IS NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleOperationalStatusPeriods_VehicleId_EffectiveFromUtc] ON [app].[VehicleOperationalStatusPeriods] ([VehicleId], [EffectiveFromUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehiclePeriodicInspections_ExpiryDate_IsCurrent] ON [app].[VehiclePeriodicInspections] ([ExpiryDate], [IsCurrent]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehiclePeriodicInspections_IsDeleted] ON [app].[VehiclePeriodicInspections] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehiclePeriodicInspections_PreviousRecordId] ON [app].[VehiclePeriodicInspections] ([PreviousRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VehiclePeriodicInspections_VehicleId] ON [app].[VehiclePeriodicInspections] ([VehicleId]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehiclePeriodicInspections_VehicleId_InspectionNumber] ON [app].[VehiclePeriodicInspections] ([VehicleId], [InspectionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleRegistrations_ExpiryDate_IsCurrent] ON [app].[VehicleRegistrations] ([ExpiryDate], [IsCurrent]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleRegistrations_IsDeleted] ON [app].[VehicleRegistrations] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_VehicleRegistrations_PreviousRecordId] ON [app].[VehicleRegistrations] ([PreviousRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VehicleRegistrations_VehicleId] ON [app].[VehicleRegistrations] ([VehicleId]) WHERE [IsCurrent] = 1 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_Vehicles_CurrentLocationId] ON [app].[Vehicles] ([CurrentLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_Vehicles_CurrentOperationalStatus_CurrentLocationId] ON [app].[Vehicles] ([CurrentOperationalStatus], [CurrentLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_Vehicles_IsDeleted] ON [app].[Vehicles] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Vehicles_NormalizedAssetNumber] ON [app].[Vehicles] ([NormalizedAssetNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Vehicles_NormalizedPlateNumberAr] ON [app].[Vehicles] ([NormalizedPlateNumberAr]) WHERE [NormalizedPlateNumberAr] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Vehicles_NormalizedPlateNumberEn] ON [app].[Vehicles] ([NormalizedPlateNumberEn]) WHERE [NormalizedPlateNumberEn] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_Vehicles_VehicleManufacturerId] ON [app].[Vehicles] ([VehicleManufacturerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    CREATE INDEX [IX_Vehicles_VehicleModelId_VehicleManufacturerId] ON [app].[Vehicles] ([VehicleModelId], [VehicleManufacturerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Vehicles_Vin] ON [app].[Vehicles] ([Vin]) WHERE [Vin] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    ALTER TABLE [app].[VehicleAccidentAttachments] ADD CONSTRAINT [FK_VehicleAccidentAttachments_VehicleAccidents_VehicleAccidentId] FOREIGN KEY ([VehicleAccidentId]) REFERENCES [app].[VehicleAccidents] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    ALTER TABLE [app].[VehicleAccidentEvents] ADD CONSTRAINT [FK_VehicleAccidentEvents_VehicleAccidents_VehicleAccidentId] FOREIGN KEY ([VehicleAccidentId]) REFERENCES [app].[VehicleAccidents] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    ALTER TABLE [app].[VehicleAccidentReportVersions] ADD CONSTRAINT [FK_VehicleAccidentReportVersions_VehicleAccidents_VehicleAccidentId] FOREIGN KEY ([VehicleAccidentId]) REFERENCES [app].[VehicleAccidents] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    ALTER TABLE [app].[VehicleAttachments] ADD CONSTRAINT [FK_VehicleAttachments_VehicleAttachmentVersions_CurrentVersionId] FOREIGN KEY ([CurrentVersionId]) REFERENCES [app].[VehicleAttachmentVersions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822164817_AddFleetOperations'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822164817_AddFleetOperations', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822165526_ImplementMissingModelApis'
)
BEGIN
    ALTER TABLE [app].[PlatformAccountCredentialVersions] ADD [RotationReason] nvarchar(1000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822165526_ImplementMissingModelApis'
)
BEGIN
    ALTER TABLE [app].[LeaveCancellationRequests] ADD [PreviousLeaveStatus] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822165526_ImplementMissingModelApis'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000075'', N''Catalog'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض بيانات الشركة المالكة وإعداداتها العامة.'', N''View the owning company profile and general settings.'', 75, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''company_profile.read'', N''عرض ملف الشركة'', N''Read company profile'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000076'', N''Catalog'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل بيانات الشركة وإعداداتها دون تغيير التسلسل الداخلي.'', N''Update company settings without changing protected internal sequences.'', 76, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''company_profile.manage'', N''إدارة ملف الشركة'', N''Manage company profile'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000077'', N''Catalog'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض كتالوج الوسوم وروابطه التشغيلية.'', N''View the tag catalog and operational assignments.'', 77, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''tags.read'', N''عرض الوسوم'', N''Read tags'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000078'', N''Catalog'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة كتالوج الوسوم وتعيينها للكيانات المسموحة.'', N''Manage tags and assign them to supported entities.'', 78, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''tags.manage'', N''إدارة الوسوم'', N''Manage tags'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000079'', N''Documents'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة أنواع الوثائق ومتطلبات اكتمالها.'', N''Manage document types and completeness requirements.'', 79, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''documents.catalog.manage'', N''إدارة كتالوج الوثائق'', N''Manage document catalog'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000080'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض بيانات وصفية فقط عن تدوير بيانات اعتماد حسابات المنصات.'', N''View metadata only for platform-account credential rotations.'', 80, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''platform_credentials.read'', N''عرض سجل بيانات اعتماد المنصات'', N''Read platform credential history'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000081'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''استبدال بيانات اعتماد حساب منصة مع حفظ سجل مشفر غير قابل للتعديل.'', N''Replace a platform account credential while preserving encrypted immutable history.'', 81, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''platform_credentials.rotate'', N''تدوير بيانات اعتماد المنصات'', N''Rotate platform credentials'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260822165526_ImplementMissingModelApis'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822165526_ImplementMissingModelApis', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823071345_MakeResidencyPermitSponsorOptional'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[EmployeeResidencyPermits]') AND [c].[name] = N'SponsorId');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [app].[EmployeeResidencyPermits] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [app].[EmployeeResidencyPermits] ALTER COLUMN [SponsorId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823071345_MakeResidencyPermitSponsorOptional'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260823071345_MakeResidencyPermitSponsorOptional', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @Targets TABLE
    (
        SchemaName sysname NOT NULL,
        TableName sysname NOT NULL,
        PRIMARY KEY (SchemaName, TableName)
    );

    INSERT INTO @Targets (SchemaName, TableName)
    VALUES (N'app', N'Employees');

    WHILE 1 = 1
    BEGIN
        INSERT INTO @Targets (SchemaName, TableName)
        SELECT DISTINCT childSchema.name, childTable.name
        FROM sys.foreign_keys AS foreignKey
        INNER JOIN sys.tables AS childTable
            ON childTable.object_id = foreignKey.parent_object_id
        INNER JOIN sys.schemas AS childSchema
            ON childSchema.schema_id = childTable.schema_id
        INNER JOIN sys.tables AS parentTable
            ON parentTable.object_id = foreignKey.referenced_object_id
        INNER JOIN sys.schemas AS parentSchema
            ON parentSchema.schema_id = parentTable.schema_id
        INNER JOIN @Targets AS target
            ON target.SchemaName = parentSchema.name
            AND target.TableName = parentTable.name
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM @Targets AS existing
            WHERE existing.SchemaName = childSchema.name
              AND existing.TableName = childTable.name
        );

        IF @@ROWCOUNT = 0 BREAK;
    END;

    DECLARE @DisableConstraints nvarchar(max);
    SELECT @DisableConstraints = STRING_AGG(CAST(
        N'ALTER TABLE ' + QUOTENAME(SchemaName) + N'.' + QUOTENAME(TableName)
        + N' NOCHECK CONSTRAINT ALL;' AS nvarchar(max)), NCHAR(10))
    FROM @Targets;
    EXEC sys.sp_executesql @DisableConstraints;

    DECLARE @DeleteRows nvarchar(max);
    SELECT @DeleteRows = STRING_AGG(CAST(
        N'DELETE FROM ' + QUOTENAME(SchemaName) + N'.' + QUOTENAME(TableName) + N';' AS nvarchar(max)),
        NCHAR(10))
    FROM @Targets;
    EXEC sys.sp_executesql @DeleteRows;

    DECLARE @EnableConstraints nvarchar(max);
    SELECT @EnableConstraints = STRING_AGG(CAST(
        N'ALTER TABLE ' + QUOTENAME(SchemaName) + N'.' + QUOTENAME(TableName)
        + N' WITH CHECK CHECK CONSTRAINT ALL;' AS nvarchar(max)), NCHAR(10))
    FROM @Targets;
    EXEC sys.sp_executesql @EnableConstraints;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[EmployeeStatusChangeRequests] DROP CONSTRAINT [FK_EmployeeStatusChangeRequests_EmployeeStatusPeriods_ResultingStatusPeriodId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT [FK_PlatformRiderAccounts_ClientContracts_ClientContractId_ClientPlatformId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT [FK_PlatformRiderAccounts_Sponsors_SponsorId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] DROP CONSTRAINT [FK_RiderClientAssignments_Employees_ActualEmployeeId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] DROP CONSTRAINT [FK_RiderClientAssignments_PlatformRiderAccounts_PlatformRiderAccountId_ClientContractId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] DROP CONSTRAINT [FK_RiderClientAssignments_RiderProfiles_RiderProfileId_ActualEmployeeId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[RiderProfiles] DROP CONSTRAINT [FK_RiderProfiles_GlobalCities_PreferredCityId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[RiderVehicleAssignments] DROP CONSTRAINT [FK_RiderVehicleAssignments_Employees_EmployeeId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[RiderVehicleAssignments] DROP CONSTRAINT [FK_RiderVehicleAssignments_RiderProfiles_RiderProfileId_EmployeeId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP TABLE [app].[EmployeeJobTitlePeriods];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP TABLE [app].[EmployeeRelationshipPeriods];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP TABLE [app].[EmployeeResidencyPermits];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP TABLE [app].[EmployeeSponsorshipPeriods];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP TABLE [app].[EmployeeStatusPeriods];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP TABLE [app].[OutsideRiderDetails];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP TABLE [app].[SponsoredInternalDetails];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_RiderVehicleAssignments_EmployeeId] ON [app].[RiderVehicleAssignments];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_RiderVehicleAssignments_RiderProfileId_EmployeeId] ON [app].[RiderVehicleAssignments];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_RiderProfiles_PreferredCityId] ON [app].[RiderProfiles];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_RiderProfiles_Status] ON [app].[RiderProfiles];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[RiderProfiles] DROP CONSTRAINT [CK_RiderProfiles_DateRange];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_RiderClientAssignments_ActualEmployeeId] ON [app].[RiderClientAssignments];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_RiderClientAssignments_ActualEmployeeId_EffectiveFrom] ON [app].[RiderClientAssignments];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_RiderClientAssignments_PlatformRiderAccountId_ClientContractId] ON [app].[RiderClientAssignments];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_RiderClientAssignments_RiderProfileId_ActualEmployeeId] ON [app].[RiderClientAssignments];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT [AK_PlatformRiderAccounts_Id_ClientContractId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_PlatformRiderAccounts_ClientContractId_ClientPlatformId] ON [app].[PlatformRiderAccounts];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_PlatformRiderAccounts_ClientContractId_Status] ON [app].[PlatformRiderAccounts];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_PlatformRiderAccounts_ClientPlatformId_NormalizedExternalAccountId] ON [app].[PlatformRiderAccounts];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_PlatformRiderAccounts_OperatingCityId_SponsorId_RegistrationType] ON [app].[PlatformRiderAccounts];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_PlatformRiderAccounts_RegisteredEmployeeId_ClientPlatformId_Status] ON [app].[PlatformRiderAccounts];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_PlatformRiderAccounts_SponsorId] ON [app].[PlatformRiderAccounts];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT [CK_PlatformRiderAccounts_Registration];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_Employees_CurrentStatus] ON [app].[Employees];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_Employees_EmployeeNumber] ON [app].[Employees];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_Employees_NormalizedNameAr] ON [app].[Employees];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_Employees_NormalizedNameEn] ON [app].[Employees];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DROP INDEX [IX_EmployeeDocuments_DocumentTypeId_NormalizedDocumentNumber] ON [app].[EmployeeDocuments];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[RiderVehicleAssignments]') AND [c].[name] = N'EmployeeId');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [app].[RiderVehicleAssignments] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [app].[RiderVehicleAssignments] DROP COLUMN [EmployeeId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[RiderProfiles]') AND [c].[name] = N'PreferredCityId');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [app].[RiderProfiles] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [app].[RiderProfiles] DROP COLUMN [PreferredCityId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[RiderProfiles]') AND [c].[name] = N'RiderEndDate');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [app].[RiderProfiles] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [app].[RiderProfiles] DROP COLUMN [RiderEndDate];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[RiderProfiles]') AND [c].[name] = N'RiderStartDate');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [app].[RiderProfiles] DROP CONSTRAINT ' + @var12 + ';');
    ALTER TABLE [app].[RiderProfiles] DROP COLUMN [RiderStartDate];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[RiderProfiles]') AND [c].[name] = N'Status');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [app].[RiderProfiles] DROP CONSTRAINT ' + @var13 + ';');
    ALTER TABLE [app].[RiderProfiles] DROP COLUMN [Status];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var14 nvarchar(max);
    SELECT @var14 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[RiderClientAssignments]') AND [c].[name] = N'ActualEmployeeId');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [app].[RiderClientAssignments] DROP CONSTRAINT ' + @var14 + ';');
    ALTER TABLE [app].[RiderClientAssignments] DROP COLUMN [ActualEmployeeId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var15 nvarchar(max);
    SELECT @var15 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[PlatformRiderAccounts]') AND [c].[name] = N'BillingMode');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT ' + @var15 + ';');
    ALTER TABLE [app].[PlatformRiderAccounts] DROP COLUMN [BillingMode];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var16 nvarchar(max);
    SELECT @var16 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[PlatformRiderAccounts]') AND [c].[name] = N'ClientContractId');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT ' + @var16 + ';');
    ALTER TABLE [app].[PlatformRiderAccounts] DROP COLUMN [ClientContractId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var17 nvarchar(max);
    SELECT @var17 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[PlatformRiderAccounts]') AND [c].[name] = N'LabelAr');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT ' + @var17 + ';');
    ALTER TABLE [app].[PlatformRiderAccounts] DROP COLUMN [LabelAr];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var18 nvarchar(max);
    SELECT @var18 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[PlatformRiderAccounts]') AND [c].[name] = N'LabelEn');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT ' + @var18 + ';');
    ALTER TABLE [app].[PlatformRiderAccounts] DROP COLUMN [LabelEn];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var19 nvarchar(max);
    SELECT @var19 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[PlatformRiderAccounts]') AND [c].[name] = N'NormalizedExternalAccountId');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT ' + @var19 + ';');
    ALTER TABLE [app].[PlatformRiderAccounts] DROP COLUMN [NormalizedExternalAccountId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var20 nvarchar(max);
    SELECT @var20 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[PlatformRiderAccounts]') AND [c].[name] = N'RegistrationType');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT ' + @var20 + ';');
    ALTER TABLE [app].[PlatformRiderAccounts] DROP COLUMN [RegistrationType];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var21 nvarchar(max);
    SELECT @var21 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[PlatformRiderAccounts]') AND [c].[name] = N'SponsorId');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT ' + @var21 + ';');
    ALTER TABLE [app].[PlatformRiderAccounts] DROP COLUMN [SponsorId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var22 nvarchar(max);
    SELECT @var22 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[Employees]') AND [c].[name] = N'EmployeeNumber');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [app].[Employees] DROP CONSTRAINT ' + @var22 + ';');
    ALTER TABLE [app].[Employees] DROP COLUMN [EmployeeNumber];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var23 nvarchar(max);
    SELECT @var23 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[Employees]') AND [c].[name] = N'NationalityCountryCode');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [app].[Employees] DROP CONSTRAINT ' + @var23 + ';');
    ALTER TABLE [app].[Employees] DROP COLUMN [NationalityCountryCode];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var24 nvarchar(max);
    SELECT @var24 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[Employees]') AND [c].[name] = N'NormalizedNameAr');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [app].[Employees] DROP CONSTRAINT ' + @var24 + ';');
    ALTER TABLE [app].[Employees] DROP COLUMN [NormalizedNameAr];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var25 nvarchar(max);
    SELECT @var25 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[EmployeeDocuments]') AND [c].[name] = N'IssuingAuthority');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [app].[EmployeeDocuments] DROP CONSTRAINT ' + @var25 + ';');
    ALTER TABLE [app].[EmployeeDocuments] DROP COLUMN [IssuingAuthority];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var26 nvarchar(max);
    SELECT @var26 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[EmployeeDocuments]') AND [c].[name] = N'IssuingCountryCode');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [app].[EmployeeDocuments] DROP CONSTRAINT ' + @var26 + ';');
    ALTER TABLE [app].[EmployeeDocuments] DROP COLUMN [IssuingCountryCode];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    DECLARE @var27 nvarchar(max);
    SELECT @var27 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[EmployeeDocuments]') AND [c].[name] = N'NormalizedDocumentNumber');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [app].[EmployeeDocuments] DROP CONSTRAINT ' + @var27 + ';');
    ALTER TABLE [app].[EmployeeDocuments] DROP COLUMN [NormalizedDocumentNumber];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC sp_rename N'[app].[EmployeeStatusChangeRequests].[ResultingStatusPeriodId]', N'ResultingWorkHistoryId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC sp_rename N'[app].[EmployeeStatusChangeRequests].[IX_EmployeeStatusChangeRequests_ResultingStatusPeriodId]', N'IX_EmployeeStatusChangeRequests_ResultingWorkHistoryId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC sp_rename N'[app].[Employees].[NormalizedNameEn]', N'WorkingForMeAs', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC sp_rename N'[app].[Employees].[CurrentStatus]', N'Status', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC sp_rename N'[app].[Employees].[CurrentRelationshipType]', N'MaritalStatus', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[RiderProfiles] ADD [TShirtSize] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [AlternateContactName] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [AlternateContactPhone] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [BirthDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [ContractEndDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [ContractStartDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [Email] nvarchar(320) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [EmergencyContactName] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [EmergencyContactPhone] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [EmergencyContactRelationship] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [EngagementType] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [Gender] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [IqamaNo] varchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [IsEmployee] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [Nationality] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [OperatingCityId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [OperationalWorkTypeId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [ProbationEndDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [ProfilePhotoDocumentId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [ResidencyProfession] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [SecondaryPhone] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [SponsorId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [StatusReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD [TerminationDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE TABLE [app].[EmployeeWorkHistory] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [ChangeType] int NOT NULL,
        [OldValue] nvarchar(1000) NULL,
        [NewValue] nvarchar(1000) NULL,
        [EffectiveDate] date NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [ChangedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_EmployeeWorkHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeWorkHistory_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [app].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RiderClientAssignments_RiderProfileId] ON [app].[RiderClientAssignments] ([RiderProfileId]) WHERE [EffectiveTo] IS NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE INDEX [IX_RiderClientAssignments_RiderProfileId_EffectiveFrom] ON [app].[RiderClientAssignments] ([RiderProfileId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlatformRiderAccounts_ClientPlatformId_ExternalAccountId] ON [app].[PlatformRiderAccounts] ([ClientPlatformId], [ExternalAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE INDEX [IX_PlatformRiderAccounts_ClientPlatformId_Status] ON [app].[PlatformRiderAccounts] ([ClientPlatformId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE INDEX [IX_PlatformRiderAccounts_OperatingCityId] ON [app].[PlatformRiderAccounts] ([OperatingCityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PlatformRiderAccounts_RegisteredEmployeeId_ClientPlatformId] ON [app].[PlatformRiderAccounts] ([RegisteredEmployeeId], [ClientPlatformId]) WHERE [RegisteredEmployeeId] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE INDEX [IX_Employees_FullNameAr] ON [app].[Employees] ([FullNameAr]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE INDEX [IX_Employees_FullNameEn] ON [app].[Employees] ([FullNameEn]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Employees_IqamaNo] ON [app].[Employees] ([IqamaNo]) WHERE [IqamaNo] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE INDEX [IX_Employees_IsEmployee_EngagementType_Status] ON [app].[Employees] ([IsEmployee], [EngagementType], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE INDEX [IX_Employees_OperatingCityId] ON [app].[Employees] ([OperatingCityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE INDEX [IX_Employees_OperationalWorkTypeId] ON [app].[Employees] ([OperationalWorkTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE INDEX [IX_Employees_ProfilePhotoDocumentId] ON [app].[Employees] ([ProfilePhotoDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE INDEX [IX_Employees_SponsorId] ON [app].[Employees] ([SponsorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC(N'ALTER TABLE [app].[Employees] ADD CONSTRAINT [CK_Employees_ActiveInternalSponsor] CHECK ([Status] <> 3 OR [EngagementType] <> 1 OR [SponsorId] IS NOT NULL)');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC(N'ALTER TABLE [app].[Employees] ADD CONSTRAINT [CK_Employees_ActiveIqama] CHECK ([Status] <> 3 OR [IqamaNo] IS NOT NULL)');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC(N'ALTER TABLE [app].[Employees] ADD CONSTRAINT [CK_Employees_ContractRange] CHECK ([ContractEndDate] IS NULL OR [ContractStartDate] IS NULL OR [ContractEndDate] >= [ContractStartDate])');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC(N'ALTER TABLE [app].[Employees] ADD CONSTRAINT [CK_Employees_IqamaNo] CHECK ([IqamaNo] IS NULL OR (LEN([IqamaNo]) = 10 AND [IqamaNo] NOT LIKE ''%[^0-9]%''))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC(N'ALTER TABLE [app].[Employees] ADD CONSTRAINT [CK_Employees_OutsideIsRider] CHECK ([EngagementType] <> 2 OR [IsEmployee] = 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployeeDocuments_DocumentTypeId_DocumentNumber] ON [app].[EmployeeDocuments] ([DocumentTypeId], [DocumentNumber]) WHERE [DocumentNumber] IS NOT NULL AND [IsDeleted] = 0 AND [Status] <> 3');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    CREATE INDEX [IX_EmployeeWorkHistory_EmployeeId_EffectiveDate_ChangeType] ON [app].[EmployeeWorkHistory] ([EmployeeId], [EffectiveDate], [ChangeType]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD CONSTRAINT [FK_Employees_EmployeeDocuments_ProfilePhotoDocumentId] FOREIGN KEY ([ProfilePhotoDocumentId]) REFERENCES [app].[EmployeeDocuments] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD CONSTRAINT [FK_Employees_OperatingCities_OperatingCityId] FOREIGN KEY ([OperatingCityId]) REFERENCES [app].[OperatingCities] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD CONSTRAINT [FK_Employees_OperationalWorkTypes_OperationalWorkTypeId] FOREIGN KEY ([OperationalWorkTypeId]) REFERENCES [app].[OperationalWorkTypes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[Employees] ADD CONSTRAINT [FK_Employees_Sponsors_SponsorId] FOREIGN KEY ([SponsorId]) REFERENCES [app].[Sponsors] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[EmployeeStatusChangeRequests] ADD CONSTRAINT [FK_EmployeeStatusChangeRequests_EmployeeWorkHistory_ResultingWorkHistoryId] FOREIGN KEY ([ResultingWorkHistoryId]) REFERENCES [app].[EmployeeWorkHistory] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] ADD CONSTRAINT [FK_RiderClientAssignments_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] ADD CONSTRAINT [FK_RiderClientAssignments_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    ALTER TABLE [app].[RiderVehicleAssignments] ADD CONSTRAINT [FK_RiderVehicleAssignments_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823120810_SimplifyEmployeeRiderModel'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260823120810_SimplifyEmployeeRiderModel', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823122317_DetachUsersFromResetEmployees'
)
BEGIN
    IF OBJECT_ID(N'[identity].[Users]', N'U') IS NOT NULL
    BEGIN
        UPDATE [identity].[Users]
        SET [EmployeeId] = NULL
        WHERE [EmployeeId] IS NOT NULL
          AND NOT EXISTS
          (
              SELECT 1
              FROM [app].[Employees]
              WHERE [Employees].[Id] = [Users].[EmployeeId]
          );
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260823122317_DetachUsersFromResetEmployees'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260823122317_DetachUsersFromResetEmployees', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824120551_SeedAjeerContractDocumentType'
)
BEGIN
    EXEC(N'UPDATE [platform].[DocumentTypes] SET [AllowedMimeTypes] = N''application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp''
    WHERE [Id] = ''019c18d5-62e1-7000-8000-000000000030'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824120551_SeedAjeerContractDocumentType'
)
BEGIN
    EXEC(N'UPDATE [platform].[DocumentTypes] SET [AllowedMimeTypes] = N''application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp''
    WHERE [Id] = ''019c18d5-62e1-7000-8000-000000000031'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824120551_SeedAjeerContractDocumentType'
)
BEGIN
    EXEC(N'UPDATE [platform].[DocumentTypes] SET [AllowedMimeTypes] = N''application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp''
    WHERE [Id] = ''019c18d5-62e1-7000-8000-000000000032'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824120551_SeedAjeerContractDocumentType'
)
BEGIN
    EXEC(N'UPDATE [platform].[DocumentTypes] SET [AllowedMimeTypes] = N''application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp''
    WHERE [Id] = ''019c18d5-62e1-7000-8000-000000000033'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824120551_SeedAjeerContractDocumentType'
)
BEGIN
    EXEC(N'UPDATE [platform].[DocumentTypes] SET [AllowedMimeTypes] = N''application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp''
    WHERE [Id] = ''019c18d5-62e1-7000-8000-000000000034'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824120551_SeedAjeerContractDocumentType'
)
BEGIN
    EXEC(N'UPDATE [platform].[DocumentTypes] SET [AllowedMimeTypes] = N''application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp''
    WHERE [Id] = ''019c18d5-62e1-7000-8000-000000000035'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824120551_SeedAjeerContractDocumentType'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AllowedMimeTypes', N'AppliesToOutsideRider', N'AppliesToRiderProfile', N'AppliesToSponsoredInternal', N'Code', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'IsDeleted', N'MaxFileSizeBytes', N'NameAr', N'NameEn', N'RequiresExpiryDate', N'RequiresFile', N'RequiresIssueDate', N'RequiresNumber', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[platform].[DocumentTypes]'))
        SET IDENTITY_INSERT [platform].[DocumentTypes] ON;
    EXEC(N'INSERT INTO [platform].[DocumentTypes] ([Id], [AllowedMimeTypes], [AppliesToOutsideRider], [AppliesToRiderProfile], [AppliesToSponsoredInternal], [Code], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [IsDeleted], [MaxFileSizeBytes], [NameAr], [NameEn], [RequiresExpiryDate], [RequiresFile], [RequiresIssueDate], [RequiresNumber], [Status], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-8000-000000000036'', N''application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''AJEER_CONTRACT'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, NULL, NULL, CAST(0 AS bit), CAST(10485760 AS bigint), N''عقود اجير'', N''Ajeer Contracts'', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), 1, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AllowedMimeTypes', N'AppliesToOutsideRider', N'AppliesToRiderProfile', N'AppliesToSponsoredInternal', N'Code', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'IsDeleted', N'MaxFileSizeBytes', N'NameAr', N'NameEn', N'RequiresExpiryDate', N'RequiresFile', N'RequiresIssueDate', N'RequiresNumber', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[platform].[DocumentTypes]'))
        SET IDENTITY_INSERT [platform].[DocumentTypes] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824120551_SeedAjeerContractDocumentType'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824120551_SeedAjeerContractDocumentType', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824132801_OptimizePlatformOperationsIndexes'
)
BEGIN
    DROP INDEX [IX_RiderClientAssignments_RiderProfileId_EffectiveFrom] ON [app].[RiderClientAssignments];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824132801_OptimizePlatformOperationsIndexes'
)
BEGIN
    DROP INDEX [IX_PlatformRiderAccounts_OperatingCityId] ON [app].[PlatformRiderAccounts];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824132801_OptimizePlatformOperationsIndexes'
)
BEGIN
    CREATE INDEX [IX_RiderClientAssignments_PlatformRiderAccountId_EffectiveFrom] ON [app].[RiderClientAssignments] ([PlatformRiderAccountId], [EffectiveFrom]) INCLUDE ([RiderProfileId], [EffectiveTo], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824132801_OptimizePlatformOperationsIndexes'
)
BEGIN
    CREATE INDEX [IX_RiderClientAssignments_RiderProfileId_EffectiveFrom] ON [app].[RiderClientAssignments] ([RiderProfileId], [EffectiveFrom]) INCLUDE ([PlatformRiderAccountId], [EffectiveTo], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824132801_OptimizePlatformOperationsIndexes'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_PlatformRiderAccounts_OperatingCityId_Status_ClientPlatformId] ON [app].[PlatformRiderAccounts] ([OperatingCityId], [Status], [ClientPlatformId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20260824132801_OptimizePlatformOperationsIndexes'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824132801_OptimizePlatformOperationsIndexes', N'10.0.11');
END;

COMMIT;
GO

