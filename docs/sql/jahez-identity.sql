BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261003152452_GrantJahezPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] ON;
    EXEC(N'INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-b000-000000000125'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000126'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.handovers.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000127'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.collections.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000128'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.requests.create'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000129'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.requests.approve'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000130'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.resets.approve'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000131'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.earnings.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000132'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.imports.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000133'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.adjustments.manage'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000134'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.cashbox.read'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000135'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.cashbox.submit'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000136'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.cashbox.confirm'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000137'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.cashbox.approve'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261003152452_GrantJahezPermissions'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003152452_GrantJahezPermissions', N'10.0.11');
END;

COMMIT;
GO

