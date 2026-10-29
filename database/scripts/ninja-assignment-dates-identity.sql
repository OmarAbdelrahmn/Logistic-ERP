BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
    SET IDENTITY_INSERT [identity].[RolePermissions] ON;
INSERT INTO [identity].[RolePermissions] ([Id], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [IsDeleted], [PermissionKey], [RoleId], [UpdatedAtUtc], [UpdatedByUserId])
VALUES ('019c18d5-62e1-7000-b000-000000000283', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, CAST(0 AS bit), N'ninja.assignments.dates.update', '019c18d5-62e1-7000-9000-000000000001', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'IsDeleted', N'PermissionKey', N'RoleId', N'UpdatedAtUtc', N'UpdatedByUserId') AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]'))
    SET IDENTITY_INSERT [identity].[RolePermissions] OFF;

INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261028111320_GrantNinjaAssignmentDateCorrectionAccess', N'10.0.11');

COMMIT;
GO

