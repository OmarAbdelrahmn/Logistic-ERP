BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
VALUES ('019c18d5-62e1-7000-a000-000000000323', N'Hunger', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'حذف تشغيل يوم محدد لجميع الحسابات أو لكفيل محدد لإعادة الاستيراد.', N'Delete a daily operations report for all accounts or one sponsor for reimport.', 323, N'HIGH_TRUST_ONLY', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N'hunger.performance.delete', N'حذف أداء هنقر اليومي', N'Delete Hunger daily performance', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
('019c18d5-62e1-7000-a000-000000000324', N'Jahez', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'حذف الطلبات اليومية لجميع الحسابات أو لكفيل محدد لإعادة الاستيراد.', N'Delete daily orders for all accounts or one sponsor for reimport.', 324, N'HIGH_TRUST_ONLY', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N'jahez.performance.delete', N'حذف طلبات جاهز اليومية', N'Delete Jahez daily orders', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261110105351_AddHungerJahezPerformanceDeletePermissions', N'10.0.11');

COMMIT;
GO

