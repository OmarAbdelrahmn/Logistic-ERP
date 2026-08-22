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

