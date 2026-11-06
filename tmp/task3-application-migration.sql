BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
VALUES ('019c18d5-62e1-7000-a000-000000000383', N'Fleet', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'تعديل هوية المركبة الأساسية.', N'Change base vehicle identity.', 383, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'fleet.vehicles.fields.identity', N'هوية المركبة', N'Vehicle identity', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106093923_GrantFleetVehicleIdentityFieldPermissions', N'10.0.11');

COMMIT;
GO

