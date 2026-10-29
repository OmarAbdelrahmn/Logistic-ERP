BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
VALUES ('019c18d5-62e1-7000-a000-000000000319', N'Ninja', '2026-01-01T00:00:00.0000000+00:00', NULL, NULL, NULL, NULL, N'تصحيح تاريخ بداية التكليف الحالي وتاريخي بداية ونهاية التكليف المنتهي دون تغيير الأداء المسجل.', N'Correct the current assignment start date or an ended assignment''s start and end dates without changing recorded performance.', 319, N'SENSITIVE_DATA', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'ninja.assignments.dates.update', N'تعديل تواريخ تكليفات نينجا', N'Edit Ninja assignment dates', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
    SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261028111233_AddNinjaAssignmentDatePermissionCatalog', N'10.0.11');

COMMIT;
GO

