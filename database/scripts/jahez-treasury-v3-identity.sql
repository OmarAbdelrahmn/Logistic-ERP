BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261019075332_GrantJahezTreasuryPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] ON;
    EXEC(N'INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-b000-000000000272'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.treasury.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000273'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.treasury.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000274'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.treasury.adjust'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000275'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.financial-reports.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000276'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.financial-reports.export'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261019075332_GrantJahezTreasuryPermissions'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019075332_GrantJahezTreasuryPermissions', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261019114451_UseGranularJahezTreasuryPermissions'
)
BEGIN
    EXEC(N'UPDATE [identity].[RolePermissions] SET [PermissionKey] = N''jahez.treasury.accounts.create''
    WHERE [Id] = ''019c18d5-62e1-7000-b000-000000000273'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261019114451_UseGranularJahezTreasuryPermissions'
)
BEGIN
    EXEC(N'UPDATE [identity].[RolePermissions] SET [PermissionKey] = N''jahez.treasury.accounts.update''
    WHERE [Id] = ''019c18d5-62e1-7000-b000-000000000274'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261019114451_UseGranularJahezTreasuryPermissions'
)
BEGIN
    EXEC(N'UPDATE [identity].[RolePermissions] SET [PermissionKey] = N''jahez.treasury.transfers.create''
    WHERE [Id] = ''019c18d5-62e1-7000-b000-000000000275'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261019114451_UseGranularJahezTreasuryPermissions'
)
BEGIN
    EXEC(N'UPDATE [identity].[RolePermissions] SET [PermissionKey] = N''jahez.treasury.entries.create''
    WHERE [Id] = ''019c18d5-62e1-7000-b000-000000000276'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261019114451_UseGranularJahezTreasuryPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] ON;
    EXEC(N'INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-b000-000000000277'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.treasury.petroapp-evidence.create'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000278'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.treasury.adjust'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000279'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.financial-reports.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000280'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.financial-reports.export'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261019114451_UseGranularJahezTreasuryPermissions'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019114451_UseGranularJahezTreasuryPermissions', N'10.0.11');
END;

COMMIT;
GO

