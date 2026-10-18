BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261017095007_AddNinjaPermissionCatalog'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000310'', N''Ninja'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض حسابات وتقارير وأداء وخصومات ومراجعات ملفات نينجا.'', N''Read Ninja accounts, reports, performance, deductions and sheet reviews.'', 310, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''ninja.read'', N''عرض نينجا'', N''Read Ninja'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000311'', N''Ninja'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل حالة ورقم هاتف الحساب ورفع مراجعات الملفات وتعديل ملاحظاتها.'', N''Update Ninja account status and phone, upload sheet reviews and edit their notes.'', 311, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''ninja.accounts.update'', N''تعديل حسابات نينجا'', N''Edit Ninja accounts'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000312'', N''Ninja'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تسليم حساب نينجا للمندوب الفعلي.'', N''Assign a Ninja account to its actual rider.'', 312, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''ninja.assignments.create'', N''تسليم حسابات نينجا'', N''Assign Ninja accounts'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000313'', N''Ninja'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''استلام حساب نينجا وإنهاء تكليفه.'', N''Return a Ninja account and close its assignment.'', 313, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''ninja.assignments.delete'', N''استلام حسابات نينجا'', N''Return Ninja accounts'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000314'', N''Ninja'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تغيير كلمة مرور حساب نينجا ضمن سياسة مهلة التغيير.'', N''Change Ninja account passwords subject to the cooldown policy.'', 314, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''ninja.credentials.rotate'', N''تغيير كلمات مرور نينجا'', N''Change Ninja passwords'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000315'', N''Ninja'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''استيراد الأداء اليومي لمندوبي نينجا.'', N''Import Ninja daily rider performance.'', 315, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''ninja.performance.import'', N''استيراد أداء نينجا'', N''Import Ninja performance'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000316'', N''Ninja'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''استيراد الخصومات اليومية لمندوبي نينجا.'', N''Import Ninja daily rider deductions.'', 316, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''ninja.deductions.import'', N''استيراد خصومات نينجا'', N''Import Ninja deductions'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261017095007_AddNinjaPermissionCatalog'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261017095007_AddNinjaPermissionCatalog', N'10.0.11');
END;

COMMIT;
GO

