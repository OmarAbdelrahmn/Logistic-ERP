IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003095839_SplitExternalRiderAndWorkOrderPermissions'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''فتح أوامر الصيانة.'', [DescriptionEn] = N''Create maintenance work orders.'', [Key] = N''maintenance.work_orders.create'', [NameAr] = N''إنشاء أوامر الصيانة'', [NameEn] = N''Create maintenance work orders''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000096'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003095839_SplitExternalRiderAndWorkOrderPermissions'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء ملفات المناديب الخارجيين.'', [DescriptionEn] = N''Create external rider profiles.'', [Key] = N''external_riders.create'', [NameAr] = N''إنشاء المناديب الخارجيين'', [NameEn] = N''Create external riders''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000121'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003095839_SplitExternalRiderAndWorkOrderPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000122'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل ملفات المناديب الخارجيين.'', N''Update external rider profiles.'', 122, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''external_riders.update'', N''تعديل المناديب الخارجيين'', N''Edit external riders'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000123'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''أرشفة ملفات المناديب الخارجيين مع حفظ التاريخ.'', N''Archive external rider profiles while retaining history.'', 123, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''external_riders.delete'', N''حذف المناديب الخارجيين'', N''Delete external riders'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000124'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''بدء وإكمال وإغلاق أوامر الصيانة وتحديث موادها وتكاليفها.'', N''Start, complete, close, and update materials and costs of maintenance work orders.'', 124, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.work_orders.update'', N''تعديل أوامر الصيانة'', N''Edit maintenance work orders'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000125'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''إلغاء أوامر الصيانة مع حفظ التاريخ.'', N''Cancel maintenance work orders while retaining history.'', 125, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.work_orders.delete'', N''إلغاء أوامر الصيانة'', N''Cancel maintenance work orders'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261003095839_SplitExternalRiderAndWorkOrderPermissions'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003095839_SplitExternalRiderAndWorkOrderPermissions', N'10.0.11');
END;
GO

