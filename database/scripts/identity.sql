IF OBJECT_ID(N'[migration].[__IdentityMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'migration') IS NULL EXEC(N'CREATE SCHEMA [migration];');
    CREATE TABLE [migration].[__IdentityMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___IdentityMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    IF SCHEMA_ID(N'identity') IS NULL EXEC(N'CREATE SCHEMA [identity];');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[Roles] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [NameAr] nvarchar(200) NOT NULL,
        [NameEn] nvarchar(200) NOT NULL,
        [DescriptionAr] nvarchar(500) NULL,
        [DescriptionEn] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [IsProtected] bit NOT NULL,
        [IsTemplate] bit NOT NULL,
        [SourceTemplateId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Roles_Roles_SourceTemplateId] FOREIGN KEY ([SourceTemplateId]) REFERENCES [identity].[Roles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[Users] (
        [Id] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NULL,
        [DisplayNameAr] nvarchar(200) NOT NULL,
        [DisplayNameEn] nvarchar(200) NOT NULL,
        [Status] int NOT NULL,
        [PreferredLocale] nvarchar(10) NOT NULL,
        [PreferredTheme] nvarchar(32) NOT NULL,
        [PreferredDensity] nvarchar(32) NOT NULL,
        [RequiresPasswordChange] bit NOT NULL,
        [AuthorizationVersion] bigint NOT NULL,
        [LastLoginAtUtc] datetimeoffset NULL,
        [LastActivityAtUtc] datetimeoffset NULL,
        [PasswordChangedAtUtc] datetimeoffset NULL,
        [SessionsRevokedAtUtc] datetimeoffset NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[RoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[RolePermissions] (
        [Id] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        [PermissionKey] nvarchar(150) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Roles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[SupportAccessGrants] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformOperatorUserId] uniqueidentifier NOT NULL,
        [RequestedPermissionsJson] nvarchar(max) NOT NULL,
        [RequestedScopesJson] nvarchar(max) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [Status] int NOT NULL,
        [RequestedStartAtUtc] datetimeoffset NOT NULL,
        [RequestedEndAtUtc] datetimeoffset NOT NULL,
        [ApprovedByUserId] uniqueidentifier NULL,
        [ApprovedAtUtc] datetimeoffset NULL,
        [IsBreakGlass] bit NOT NULL,
        [BreakGlassJustification] nvarchar(2000) NULL,
        [RevokedAtUtc] datetimeoffset NULL,
        [RevokedByUserId] uniqueidentifier NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SupportAccessGrants] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SupportAccessGrants_BreakGlassReason] CHECK ([IsBreakGlass] = 0 OR [BreakGlassJustification] IS NOT NULL),
        CONSTRAINT [CK_SupportAccessGrants_MaxDuration] CHECK (DATEDIFF(HOUR, [RequestedStartAtUtc], [RequestedEndAtUtc]) <= 24),
        CONSTRAINT [CK_SupportAccessGrants_TimeRange] CHECK ([RequestedEndAtUtc] > [RequestedStartAtUtc]),
        CONSTRAINT [FK_SupportAccessGrants_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupportAccessGrants_Users_PlatformOperatorUserId] FOREIGN KEY ([PlatformOperatorUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[TemporaryCredentials] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Purpose] int NOT NULL,
        [CredentialHash] nvarchar(128) NOT NULL,
        [ExpiresAtUtc] datetimeoffset NOT NULL,
        [ConsumedAtUtc] datetimeoffset NULL,
        [RevokedAtUtc] datetimeoffset NULL,
        [IssuedByUserId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TemporaryCredentials] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TemporaryCredentials_Users_IssuedByUserId] FOREIGN KEY ([IssuedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TemporaryCredentials_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[UserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[UserDirectPermissionAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [PermissionKey] nvarchar(150) NOT NULL,
        [Effect] int NOT NULL,
        [StartsAtUtc] datetimeoffset NOT NULL,
        [ExpiresAtUtc] datetimeoffset NULL,
        [GrantedByUserId] uniqueidentifier NOT NULL,
        [GrantReason] nvarchar(1000) NOT NULL,
        [IsAllHousingScope] bit NOT NULL,
        [IsAllClientScope] bit NOT NULL,
        [IncludesFuturePlatformContracts] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserDirectPermissionAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UserDirectPermissionAssignments_TimeRange] CHECK ([ExpiresAtUtc] IS NULL OR [ExpiresAtUtc] > [StartsAtUtc]),
        CONSTRAINT [FK_UserDirectPermissionAssignments_Users_GrantedByUserId] FOREIGN KEY ([GrantedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserDirectPermissionAssignments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[UserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[UserRoleAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        [StartsAtUtc] datetimeoffset NOT NULL,
        [ExpiresAtUtc] datetimeoffset NULL,
        [GrantedByUserId] uniqueidentifier NOT NULL,
        [GrantReason] nvarchar(1000) NOT NULL,
        [IsAllHousingScope] bit NOT NULL,
        [IsAllClientScope] bit NOT NULL,
        [IncludesFuturePlatformContracts] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserRoleAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UserRoleAssignments_TimeRange] CHECK ([ExpiresAtUtc] IS NULL OR [ExpiresAtUtc] > [StartsAtUtc]),
        CONSTRAINT [FK_UserRoleAssignments_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Roles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRoleAssignments_Users_GrantedByUserId] FOREIGN KEY ([GrantedByUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRoleAssignments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[UserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[UserSessions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RefreshTokenFamilyId] uniqueidentifier NOT NULL,
        [RefreshTokenHash] nvarchar(128) NOT NULL,
        [DeviceLabel] nvarchar(200) NULL,
        [UserAgentHash] nvarchar(128) NULL,
        [LastIpAddress] nvarchar(64) NULL,
        [LastUsedAtUtc] datetimeoffset NOT NULL,
        [IdleExpiresAtUtc] datetimeoffset NOT NULL,
        [AbsoluteExpiresAtUtc] datetimeoffset NOT NULL,
        [RevokedAtUtc] datetimeoffset NULL,
        [RevokedByUserId] uniqueidentifier NULL,
        [RevocationReason] nvarchar(1000) NULL,
        [AuthorizationVersion] bigint NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UserSessions_AbsoluteExpiry] CHECK ([AbsoluteExpiresAtUtc] > [CreatedAtUtc]),
        CONSTRAINT [CK_UserSessions_IdleExpiry] CHECK ([IdleExpiresAtUtc] > [CreatedAtUtc]),
        CONSTRAINT [FK_UserSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[UserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE TABLE [identity].[AccessScopes] (
        [Id] uniqueidentifier NOT NULL,
        [UserRoleAssignmentId] uniqueidentifier NULL,
        [DirectPermissionAssignmentId] uniqueidentifier NULL,
        [ScopeType] int NOT NULL,
        [TargetId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AccessScopes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AccessScopes_ExactlyOneParent] CHECK (CASE WHEN [UserRoleAssignmentId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [DirectPermissionAssignmentId] IS NULL THEN 0 ELSE 1 END = 1),
        CONSTRAINT [FK_AccessScopes_UserDirectPermissionAssignments_DirectPermissionAssignmentId] FOREIGN KEY ([DirectPermissionAssignmentId]) REFERENCES [identity].[UserDirectPermissionAssignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AccessScopes_UserRoleAssignments_UserRoleAssignmentId] FOREIGN KEY ([UserRoleAssignmentId]) REFERENCES [identity].[UserRoleAssignments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AccessScopes_DirectPermissionAssignmentId] ON [identity].[AccessScopes] ([DirectPermissionAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AccessScopes_IsDeleted] ON [identity].[AccessScopes] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AccessScopes_UserRoleAssignmentId_DirectPermissionAssignmentId_ScopeType_TargetId] ON [identity].[AccessScopes] ([UserRoleAssignmentId], [DirectPermissionAssignmentId], [ScopeType], [TargetId]) WHERE [UserRoleAssignmentId] IS NOT NULL AND [DirectPermissionAssignmentId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_RoleClaims_RoleId] ON [identity].[RoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_IsDeleted] ON [identity].[RolePermissions] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionKey] ON [identity].[RolePermissions] ([PermissionKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RolePermissions_RoleId_PermissionKey] ON [identity].[RolePermissions] ([RoleId], [PermissionKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Code] ON [identity].[Roles] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_Roles_SourceTemplateId] ON [identity].[Roles] ([SourceTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Roles_NormalizedName] ON [identity].[Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_SupportAccessGrants_ApprovedByUserId] ON [identity].[SupportAccessGrants] ([ApprovedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_SupportAccessGrants_IsDeleted] ON [identity].[SupportAccessGrants] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_SupportAccessGrants_PlatformOperatorUserId_RequestedEndAtUtc] ON [identity].[SupportAccessGrants] ([PlatformOperatorUserId], [RequestedEndAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_SupportAccessGrants_Status_RequestedStartAtUtc] ON [identity].[SupportAccessGrants] ([Status], [RequestedStartAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TemporaryCredentials_CredentialHash] ON [identity].[TemporaryCredentials] ([CredentialHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_TemporaryCredentials_IsDeleted] ON [identity].[TemporaryCredentials] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_TemporaryCredentials_IssuedByUserId] ON [identity].[TemporaryCredentials] ([IssuedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_TemporaryCredentials_UserId_Purpose_ExpiresAtUtc] ON [identity].[TemporaryCredentials] ([UserId], [Purpose], [ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserClaims_UserId] ON [identity].[UserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserDirectPermissionAssignments_GrantedByUserId] ON [identity].[UserDirectPermissionAssignments] ([GrantedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserDirectPermissionAssignments_IsDeleted] ON [identity].[UserDirectPermissionAssignments] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserDirectPermissionAssignments_UserId_ExpiresAtUtc] ON [identity].[UserDirectPermissionAssignments] ([UserId], [ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserDirectPermissionAssignments_UserId_PermissionKey_StartsAtUtc] ON [identity].[UserDirectPermissionAssignments] ([UserId], [PermissionKey], [StartsAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserLogins_UserId] ON [identity].[UserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_GrantedByUserId] ON [identity].[UserRoleAssignments] ([GrantedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_IsDeleted] ON [identity].[UserRoleAssignments] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_RoleId] ON [identity].[UserRoleAssignments] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_UserId_ExpiresAtUtc] ON [identity].[UserRoleAssignments] ([UserId], [ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_UserId_RoleId_StartsAtUtc] ON [identity].[UserRoleAssignments] ([UserId], [RoleId], [StartsAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [identity].[UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [identity].[Users] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_EmployeeId] ON [identity].[Users] ([EmployeeId]) WHERE [EmployeeId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_Users_Status] ON [identity].[Users] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Users_NormalizedUserName] ON [identity].[Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserSessions_IsDeleted] ON [identity].[UserSessions] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserSessions_RefreshTokenHash] ON [identity].[UserSessions] ([RefreshTokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserSessions_UserId_RevokedAtUtc_AbsoluteExpiresAtUtc] ON [identity].[UserSessions] ([UserId], [RevokedAtUtc], [AbsoluteExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822091647_InitialIdentity'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822091647_InitialIdentity', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822123746_SeedProtectedAuthorizationRoles'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'ConcurrencyStamp', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'IsDeleted', N'IsProtected', N'IsTemplate', N'Name', N'NameAr', N'NameEn', N'NormalizedName', N'SourceTemplateId', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[Roles]'))
        SET IDENTITY_INSERT [identity].[Roles] ON;
    EXEC(N'INSERT INTO [identity].[Roles] ([Id], [Code], [ConcurrencyStamp], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [IsDeleted], [IsProtected], [IsTemplate], [Name], [NameAr], [NameEn], [NormalizedName], [SourceTemplateId], [Status], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-9000-000000000001'', N''SYSTEM_ADMIN'', N''protected-system_admin-v1'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إدارة المستخدمين والأدوار والصلاحيات والأمن دون منح تلقائي لكل البيانات التشغيلية الحساسة.'', N''Manages users, roles, permissions, and security without automatic access to all sensitive operational data.'', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''SYSTEM_ADMIN'', N''مسؤول النظام'', N''System Administrator'', N''SYSTEM_ADMIN'', NULL, 2, NULL, NULL),
    (''019c18d5-62e1-7000-9000-000000000002'', N''MANAGER'', N''protected-manager-v1'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''قراءة تشغيلية أساسية، وتضاف صلاحيات الإدارة والنطاقات حسب مسؤوليات الشخص.'', N''Minimal operational read access; management permissions and scopes are assigned per responsibility.'', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''MANAGER'', N''مدير'', N''Manager'', N''MANAGER'', NULL, 2, NULL, NULL),
    (''019c18d5-62e1-7000-9000-000000000003'', N''USER'', N''protected-user-v1'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''الوصول إلى الملف الشخصي والجلسات فقط حتى تمنح صلاحيات إضافية.'', N''Access to the user''''s own profile and sessions until additional permissions are granted.'', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''USER'', N''مستخدم'', N''User'', N''USER'', NULL, 2, NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'ConcurrencyStamp', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'IsDeleted', N'IsProtected', N'IsTemplate', N'Name', N'NameAr', N'NameEn', N'NormalizedName', N'SourceTemplateId', N'Status', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[Roles]'))
        SET IDENTITY_INSERT [identity].[Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822123746_SeedProtectedAuthorizationRoles'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] ON;
    EXEC(N'INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-b000-000000000001'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''users.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000002'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''users.create'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000003'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''users.update'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000004'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''users.archive'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000005'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''roles.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000006'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''roles.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000007'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''permissions.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000008'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''permissions.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000009'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''audit.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000010'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''support_access.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000011'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''operating_cities.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000012'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''operating_cities.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000013'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''reports.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000014'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''operating_cities.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000015'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''employees.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000016'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''riders.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000017'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''platform_accounts.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000018'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''platform_assignments.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000019'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''housing.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000020'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''reports.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000021'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''notifications.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822123746_SeedProtectedAuthorizationRoles'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822123746_SeedProtectedAuthorizationRoles', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822124255_ProtectDevelopmentAccounts'
)
BEGIN
    ALTER TABLE [identity].[Users] ADD [IsDevelopmentOnly] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822124255_ProtectDevelopmentAccounts'
)
BEGIN
    CREATE INDEX [IX_Users_IsDevelopmentOnly] ON [identity].[Users] ([IsDevelopmentOnly]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822124255_ProtectDevelopmentAccounts'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822124255_ProtectDevelopmentAccounts', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822165334_AddFleetPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] ON;
    EXEC(N'INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-b000-000000000030'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.vehicles.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000031'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.vehicles.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000032'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.vehicles.archive'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000033'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.vehicles.decommission'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000034'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.assignments.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000035'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.assignments.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000036'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.assignments.correct'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000037'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.issues.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000038'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.issues.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000039'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.compliance.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000040'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.compliance.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000041'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.files.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000042'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.files.upload'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000043'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.files.download'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000044'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.accidents.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000045'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.accidents.report'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000046'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.accidents.finalize'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000047'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.accidents.download'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000048'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.corrections.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000049'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.vehicles.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000050'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.vehicles.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000051'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.assignments.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000052'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.assignments.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000053'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.issues.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000054'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.issues.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000055'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.compliance.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000056'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.compliance.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000057'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.files.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000058'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.files.upload'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000059'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.files.download'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000060'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.accidents.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000061'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.accidents.report'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000062'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.accidents.download'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822165334_AddFleetPermissions'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822165334_AddFleetPermissions', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822165718_AddMissingModelApiPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] ON;
    EXEC(N'INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-b000-000000000022'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''company_profile.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000023'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''company_profile.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000024'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''tags.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000025'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''tags.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000026'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''documents.catalog.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000027'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''platform_credentials.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000028'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''platform_credentials.rotate'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000029'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''tags.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260822165718_AddMissingModelApiPermissions'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822165718_AddMissingModelApiPermissions', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260826090732_GrantVehicleRegistrationTransitionAccess'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] ON;
    EXEC(N'INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-b000-000000000063'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fleet.registration_transitions.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260826090732_GrantVehicleRegistrationTransitionAccess'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260826090732_GrantVehicleRegistrationTransitionAccess', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260829090518_EnforceSingleActiveUserSession'
)
BEGIN
    ;WITH RankedOpenSessions AS
    (
        SELECT
            [Id],
            ROW_NUMBER() OVER
            (
                PARTITION BY [UserId]
                ORDER BY [LastUsedAtUtc] DESC, [CreatedAtUtc] DESC, [Id] DESC
            ) AS [SessionRank]
        FROM [identity].[UserSessions]
        WHERE [RevokedAtUtc] IS NULL AND [IsDeleted] = 0
    )
    UPDATE [session]
    SET
        [RevokedAtUtc] = SYSUTCDATETIME(),
        [RevocationReason] = N'Revoked while enabling the single-active-session policy.',
        [UpdatedAtUtc] = SYSUTCDATETIME()
    FROM [identity].[UserSessions] AS [session]
    INNER JOIN RankedOpenSessions AS [ranked] ON [ranked].[Id] = [session].[Id]
    WHERE [ranked].[SessionRank] > 1;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260829090518_EnforceSingleActiveUserSession'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_UserSessions_OneOpenSessionPerUser] ON [identity].[UserSessions] ([UserId]) WHERE [RevokedAtUtc] IS NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260829090518_EnforceSingleActiveUserSession'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260829090518_EnforceSingleActiveUserSession', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260829095458_AllowRepeatedTemporaryCredentialHashes'
)
BEGIN
    DROP INDEX [IX_TemporaryCredentials_CredentialHash] ON [identity].[TemporaryCredentials];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260829095458_AllowRepeatedTemporaryCredentialHashes'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260829095458_AllowRepeatedTemporaryCredentialHashes', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260831061917_GrantPhoneSimPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] ON;
    EXEC(N'INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-b000-000000000064'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''phone_sims.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000065'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''phone_sims.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000066'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''phone_sims.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000067'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''phone_sims.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260831061917_GrantPhoneSimPermissions'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831061917_GrantPhoneSimPermissions', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260831080424_AddUserProfileImage'
)
BEGIN
    ALTER TABLE [identity].[Users] ADD [ProfileImageUrl] nvarchar(512) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260831080424_AddUserProfileImage'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831080424_AddUserProfileImage', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260903153722_GrantFuelPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] ON;
    EXEC(N'INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-b000-000000000068'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fuel.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000069'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fuel.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000070'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fuel.import'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000071'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fuel.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000072'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fuel.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000073'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''fuel.import'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260903153722_GrantFuelPermissions'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260903153722_GrantFuelPermissions', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260903155037_GrantFuelPermissionsToDefaultUser'
)
BEGIN
    DECLARE @OmarUserId uniqueidentifier;
    DECLARE @NowUtc datetimeoffset = SYSUTCDATETIME();
    DECLARE @InsertedCount int = 0;

    SELECT @OmarUserId = [Id]
    FROM [identity].[Users]
    WHERE [NormalizedUserName] = N'OMAR'
        OR UPPER([UserName]) = N'OMAR';

    IF @OmarUserId IS NOT NULL
    BEGIN
        INSERT INTO [identity].[UserDirectPermissionAssignments]
        (
            [Id], [UserId], [PermissionKey], [Effect], [StartsAtUtc], [ExpiresAtUtc],
            [GrantedByUserId], [GrantReason], [IsAllHousingScope], [IsAllClientScope],
            [IncludesFuturePlatformContracts], [CreatedAtUtc], [CreatedByUserId],
            [UpdatedAtUtc], [UpdatedByUserId], [IsDeleted], [DeletedAtUtc],
            [DeletedByUserId], [DeletionReason]
        )
        SELECT
            source.[Id], @OmarUserId, source.[PermissionKey], 1, @NowUtc, NULL,
            @OmarUserId, N'Direct fuel access grant for user omar.', 1, 1,
            1, @NowUtc, @OmarUserId, NULL, NULL, 0, NULL, NULL, NULL
        FROM (VALUES
            (CAST('019c18d5-62e1-7000-e000-000000000001' AS uniqueidentifier), N'fuel.read'),
            (CAST('019c18d5-62e1-7000-e000-000000000002' AS uniqueidentifier), N'fuel.manage'),
            (CAST('019c18d5-62e1-7000-e000-000000000003' AS uniqueidentifier), N'fuel.import')
        ) source([Id], [PermissionKey])
        WHERE NOT EXISTS (
            SELECT 1
            FROM [identity].[UserDirectPermissionAssignments] existing
            WHERE existing.[UserId] = @OmarUserId
                AND existing.[PermissionKey] = source.[PermissionKey]
                AND existing.[Effect] = 1
                AND existing.[ExpiresAtUtc] IS NULL
                AND existing.[IsDeleted] = 0
                AND existing.[IsAllHousingScope] = 1
                AND existing.[IsAllClientScope] = 1
                AND existing.[IncludesFuturePlatformContracts] = 1)
            AND NOT EXISTS (
                SELECT 1
                FROM [identity].[UserDirectPermissionAssignments] existing
                WHERE existing.[Id] = source.[Id]);

        SET @InsertedCount = @@ROWCOUNT;

        IF @InsertedCount > 0
        BEGIN
            UPDATE [identity].[Users]
            SET [AuthorizationVersion] = [AuthorizationVersion] + 1
            WHERE [Id] = @OmarUserId;
        END
    END
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260903155037_GrantFuelPermissionsToDefaultUser'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260903155037_GrantFuelPermissionsToDefaultUser', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260905093829_GrantMaintenancePermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] ON;
    EXEC(N'INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-b000-000000000074'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.locations.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000075'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.locations.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000076'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.work_orders.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000077'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.work_orders.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000078'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.oil.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000079'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.oil.complete'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000080'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.external_jobs.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000081'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.external_jobs.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000082'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.part_sales.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000083'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.customer_labor_charges.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000084'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.mechanic_labor_payments.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000085'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.profit_reports.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000086'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.profit_reports.export'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000087'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.items.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000088'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.items.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000089'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.stock.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000090'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.stock.move'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000091'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.stock.adjust'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000092'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.cost_layers.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000093'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.receipts.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000094'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.returns.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000095'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.locations.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000096'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.work_orders.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000097'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.work_orders.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000098'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.oil.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000099'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.oil.complete'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000100'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.external_jobs.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000101'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.external_jobs.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000102'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.part_sales.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000103'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.customer_labor_charges.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000104'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''maintenance.mechanic_labor_payments.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000105'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.items.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000106'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.items.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000107'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.stock.read'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000108'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.stock.move'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000109'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.receipts.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000110'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''inventory.returns.manage'', ''019c18d5-62e1-7000-9000-000000000002'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20260905093829_GrantMaintenancePermissions'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260905093829_GrantMaintenancePermissions', N'10.0.11');
END;

COMMIT;
GO

