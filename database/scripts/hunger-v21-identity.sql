BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
    SET IDENTITY_INSERT [identity].[RolePermissions] ON;
INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
VALUES ('019c18d5-62e1-7000-b000-000000000304', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'hunger.finance.collection', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL),
('019c18d5-62e1-7000-b000-000000000305', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'hunger.finance.funding.execute', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL),
('019c18d5-62e1-7000-b000-000000000306', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'hunger.finance.funding.request', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL),
('019c18d5-62e1-7000-b000-000000000307', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'hunger.finance.read', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL),
('019c18d5-62e1-7000-b000-000000000308', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'hunger.finance.rider.payment', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL),
('019c18d5-62e1-7000-b000-000000000309', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'hunger.finance.topup.execute', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL),
('019c18d5-62e1-7000-b000-000000000310', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'hunger.finance.topup.request', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL),
('019c18d5-62e1-7000-b000-000000000311', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'hunger.finance.wallet.review', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL),
('019c18d5-62e1-7000-b000-000000000312', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'hunger.finance.withdrawal.arrival.confirm', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL),
('019c18d5-62e1-7000-b000-000000000313', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'hunger.finance.withdrawal.request', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL),
('019c18d5-62e1-7000-b000-000000000314', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'hunger.remittances.cancel_reverse', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL),
('019c18d5-62e1-7000-b000-000000000315', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'hunger.remittances.submit', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
    SET IDENTITY_INSERT [identity].[RolePermissions] OFF;

INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106083738_GrantHungerFinanceActionPermissions', N'10.0.11');

COMMIT;
GO

BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
    SET IDENTITY_INSERT [identity].[RolePermissions] ON;
INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
VALUES ('019c18d5-62e1-7000-b000-000000000316', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'fleet.vehicles.fields.identity', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
    SET IDENTITY_INSERT [identity].[RolePermissions] OFF;

INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106094011_GrantFleetVehicleIdentityFieldPermissions', N'10.0.11');

COMMIT;
GO

