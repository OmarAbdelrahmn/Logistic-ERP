IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261015072948_GrantJahezWorkflowPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] ON;
    EXEC(N'INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
    VALUES (''019c18d5-62e1-7000-b000-000000000247'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.withdraw'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000248'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.settle'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000249'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.fees.record'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000250'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.accounts.status.update'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000251'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.money.receive'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000252'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.money.disburse'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000253'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.administrative-debt.adjust'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000254'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.transferred-debt.repay'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000255'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.requests.fee.create'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000256'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.requests.fee.approve'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000257'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.requests.switch.create'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000258'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.requests.switch.approve'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000259'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.requests.transfer.create'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000260'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.requests.transfer.approve'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000261'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.requests.commission.create'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000262'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.requests.commission.approve'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000263'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.requests.adjustment.create'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL),
    (''019c18d5-62e1-7000-b000-000000000264'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, CAST(0 AS bit), N''jahez.requests.adjustment.approve'', ''019c18d5-62e1-7000-9000-000000000001'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
        SET IDENTITY_INSERT [identity].[RolePermissions] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261015072948_GrantJahezWorkflowPermissions'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015072948_GrantJahezWorkflowPermissions', N'10.0.11');
END;
GO

