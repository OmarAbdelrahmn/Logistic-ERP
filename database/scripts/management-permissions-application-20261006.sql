BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    SELECT * INTO #LegacyManagementDefinitions FROM [platform].[PermissionDefinitions]
    WHERE [Key] LIKE '%.manage' AND IsDeleted = 0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء الأدوار ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create roles within the authorized scope.'', [Key] = N''roles.create'', [NameAr] = N''إنشاء الأدوار'', [NameEn] = N''Create roles''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000006'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء الصلاحيات ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create permissions within the authorized scope.'', [Key] = N''permissions.create'', [NameAr] = N''إنشاء الصلاحيات'', [NameEn] = N''Create permissions''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000008'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء وصول الدعم ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create support access within the authorized scope.'', [Key] = N''support_access.create'', [NameAr] = N''إنشاء وصول الدعم'', [NameEn] = N''Create support access''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000010'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء المدن التشغيلية ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create operating cities within the authorized scope.'', [Key] = N''operating_cities.create'', [NameAr] = N''إنشاء المدن التشغيلية'', [NameEn] = N''Create operating cities''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000012'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء المناديب ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create riders within the authorized scope.'', [Key] = N''riders.create'', [NameAr] = N''إنشاء المناديب'', [NameEn] = N''Create riders''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000019'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء الكفلاء ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create sponsors within the authorized scope.'', [Key] = N''sponsors.create'', [NameAr] = N''إنشاء الكفلاء'', [NameEn] = N''Create sponsors''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000021'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء الإقامات ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create residency permits within the authorized scope.'', [Key] = N''residency.create'', [NameAr] = N''إنشاء الإقامات'', [NameEn] = N''Create residency permits''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000023'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء الرخص ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create driver licenses within the authorized scope.'', [Key] = N''licenses.create'', [NameAr] = N''إنشاء الرخص'', [NameEn] = N''Create driver licenses''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000025'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء بطاقات السائق ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create rider cards within the authorized scope.'', [Key] = N''rider_cards.create'', [NameAr] = N''إنشاء بطاقات السائق'', [NameEn] = N''Create rider cards''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000027'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء البطاقات الصحية ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create health cards within the authorized scope.'', [Key] = N''health_cards.create'', [NameAr] = N''إنشاء البطاقات الصحية'', [NameEn] = N''Create health cards''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000029'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء التأمين الطبي ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create medical insurance within the authorized scope.'', [Key] = N''insurance.create'', [NameAr] = N''إنشاء التأمين الطبي'', [NameEn] = N''Create medical insurance''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000031'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء سندات الأمر ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create promissory notes within the authorized scope.'', [Key] = N''promissory_notes.create'', [NameAr] = N''إنشاء سندات الأمر'', [NameEn] = N''Create promissory notes''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000033'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء حسابات المنصات ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create platform accounts within the authorized scope.'', [Key] = N''platform_accounts.create'', [NameAr] = N''إنشاء حسابات المنصات'', [NameEn] = N''Create platform accounts''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000039'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء تكليفات المنصات ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create platform assignments within the authorized scope.'', [Key] = N''platform_assignments.create'', [NameAr] = N''إنشاء تكليفات المنصات'', [NameEn] = N''Create platform assignments''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000041'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء السكن ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create housing within the authorized scope.'', [Key] = N''housing.create'', [NameAr] = N''إنشاء السكن'', [NameEn] = N''Create housing''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000043'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء الإشعارات ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create notifications within the authorized scope.'', [Key] = N''notifications.create'', [NameAr] = N''إنشاء الإشعارات'', [NameEn] = N''Create notifications''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000047'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء طلبات الإجازة ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create leave requests within the authorized scope.'', [Key] = N''leave_requests.create'', [NameAr] = N''إنشاء طلبات الإجازة'', [NameEn] = N''Create leave requests''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000049'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء حالات الغياب ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create absence cases within the authorized scope.'', [Key] = N''absence_cases.create'', [NameAr] = N''إنشاء حالات الغياب'', [NameEn] = N''Create absence cases''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000052'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء طلبات تغيير الحالة ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create employee status changes within the authorized scope.'', [Key] = N''employee_status_changes.create'', [NameAr] = N''إنشاء طلبات تغيير الحالة'', [NameEn] = N''Create employee status changes''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000054'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء المركبات ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create vehicles within the authorized scope.'', [Key] = N''fleet.vehicles.create'', [NameAr] = N''إنشاء المركبات'', [NameEn] = N''Create vehicles''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000057'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء عهد المركبات ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create vehicle assignments within the authorized scope.'', [Key] = N''fleet.assignments.create'', [NameAr] = N''إنشاء عهد المركبات'', [NameEn] = N''Create vehicle assignments''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000061'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء بلاغات المركبات ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create vehicle issues within the authorized scope.'', [Key] = N''fleet.issues.create'', [NameAr] = N''إنشاء بلاغات المركبات'', [NameEn] = N''Create vehicle issues''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000064'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء التزام المركبات ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create vehicle compliance within the authorized scope.'', [Key] = N''fleet.compliance.create'', [NameAr] = N''إنشاء التزام المركبات'', [NameEn] = N''Create vehicle compliance''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000066'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء تصحيح بيانات الأسطول ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create fleet corrections within the authorized scope.'', [Key] = N''fleet.corrections.create'', [NameAr] = N''إنشاء تصحيح بيانات الأسطول'', [NameEn] = N''Create fleet corrections''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000074'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء ملف الشركة ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create company profile within the authorized scope.'', [Key] = N''company_profile.create'', [NameAr] = N''إنشاء ملف الشركة'', [NameEn] = N''Create company profile''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000076'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء الوسوم ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create tags within the authorized scope.'', [Key] = N''tags.create'', [NameAr] = N''إنشاء الوسوم'', [NameEn] = N''Create tags''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000078'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء كتالوج الوثائق ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create document catalog within the authorized scope.'', [Key] = N''documents.catalog.create'', [NameAr] = N''إنشاء كتالوج الوثائق'', [NameEn] = N''Create document catalog''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000079'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء تحويل تسجيل المركبة ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create vehicle registration transitions within the authorized scope.'', [Key] = N''fleet.registration_transitions.create'', [NameAr] = N''إنشاء تحويل تسجيل المركبة'', [NameEn] = N''Create vehicle registration transitions''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000082'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء قوالب نماذج الموارد البشرية ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create HR form templates within the authorized scope.'', [Key] = N''hr_forms.templates.create'', [NameAr] = N''إنشاء قوالب نماذج الموارد البشرية'', [NameEn] = N''Create HR form templates''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000084'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء شرائح الاتصال ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create phone SIMs within the authorized scope.'', [Key] = N''phone_sims.create'', [NameAr] = N''إنشاء شرائح الاتصال'', [NameEn] = N''Create phone SIMs''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000086'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء المسافات اليومية للمركبات ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create vehicle daily distances within the authorized scope.'', [Key] = N''fleet.daily_distances.create'', [NameAr] = N''إنشاء المسافات اليومية للمركبات'', [NameEn] = N''Create vehicle daily distances''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000088'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء بطاقات الوقود ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create fuel cards within the authorized scope.'', [Key] = N''fuel.create'', [NameAr] = N''إنشاء بطاقات الوقود'', [NameEn] = N''Create fuel cards''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000091'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء مواقع الصيانة ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create maintenance locations within the authorized scope.'', [Key] = N''maintenance.locations.create'', [NameAr] = N''إنشاء مواقع الصيانة'', [NameEn] = N''Create maintenance locations''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000094'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء صيانة المركبات الخارجية ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create external maintenance jobs within the authorized scope.'', [Key] = N''maintenance.external_jobs.create'', [NameAr] = N''إنشاء صيانة المركبات الخارجية'', [NameEn] = N''Create external maintenance jobs''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000100'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء بيع قطع الغيار ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create spare-part sales within the authorized scope.'', [Key] = N''maintenance.part_sales.create'', [NameAr] = N''إنشاء بيع قطع الغيار'', [NameEn] = N''Create spare-part sales''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000101'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء مصنعية العميل ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create customer labor charges within the authorized scope.'', [Key] = N''maintenance.customer_labor_charges.create'', [NameAr] = N''إنشاء مصنعية العميل'', [NameEn] = N''Create customer labor charges''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000102'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء أجرة الميكانيكي ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create mechanic labor payments within the authorized scope.'', [Key] = N''maintenance.mechanic_labor_payments.create'', [NameAr] = N''إنشاء أجرة الميكانيكي'', [NameEn] = N''Create mechanic labor payments''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000103'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء أصناف المخزون ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create inventory items within the authorized scope.'', [Key] = N''inventory.items.create'', [NameAr] = N''إنشاء أصناف المخزون'', [NameEn] = N''Create inventory items''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000107'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء فواتير الشراء ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create purchase receipts within the authorized scope.'', [Key] = N''inventory.receipts.create'', [NameAr] = N''إنشاء فواتير الشراء'', [NameEn] = N''Create purchase receipts''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000112'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء مرتجعات المورد ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create supplier returns within the authorized scope.'', [Key] = N''inventory.returns.create'', [NameAr] = N''إنشاء مرتجعات المورد'', [NameEn] = N''Create supplier returns''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000113'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء القضايا القانونية ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create legal cases within the authorized scope.'', [Key] = N''legal_cases.create'', [NameAr] = N''إنشاء القضايا القانونية'', [NameEn] = N''Create legal cases''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000118'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء تسليم وإغلاق حسابات جاهز ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create Jahez handovers within the authorized scope.'', [Key] = N''jahez.handovers.create'', [NameAr] = N''إنشاء تسليم وإغلاق حسابات جاهز'', [NameEn] = N''Create Jahez handovers''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000131'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء تحصيل مبالغ جاهز ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create Collect Jahez payments within the authorized scope.'', [Key] = N''jahez.collections.create'', [NameAr] = N''إنشاء تحصيل مبالغ جاهز'', [NameEn] = N''Create Collect Jahez payments''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000132'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء تسجيل أرباح جاهز ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create Jahez earnings within the authorized scope.'', [Key] = N''jahez.earnings.create'', [NameAr] = N''إنشاء تسجيل أرباح جاهز'', [NameEn] = N''Create Jahez earnings''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000136'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء استيراد تقارير جاهز ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create Import Jahez reports within the authorized scope.'', [Key] = N''jahez.imports.create'', [NameAr] = N''إنشاء استيراد تقارير جاهز'', [NameEn] = N''Create Import Jahez reports''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000137'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    EXEC(N'UPDATE [platform].[PermissionDefinitions] SET [DescriptionAr] = N''إنشاء تسويات وأرصدة جاهز ضمن النطاق المصرح به.'', [DescriptionEn] = N''Create Adjust Jahez balances within the authorized scope.'', [Key] = N''jahez.adjustments.create'', [NameAr] = N''إنشاء تسويات وأرصدة جاهز'', [NameEn] = N''Create Adjust Jahez balances''
    WHERE [Id] = ''019c18d5-62e1-7000-a000-000000000138'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] ON;
    EXEC(N'INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000200'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل الأدوار ضمن النطاق المصرح به.'', N''Update roles within the authorized scope.'', 200, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''roles.update'', N''تعديل الأدوار'', N''Edit roles'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000201'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف الأدوار ضمن النطاق المصرح به.'', N''Delete roles within the authorized scope.'', 201, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''roles.delete'', N''حذف الأدوار'', N''Delete roles'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000202'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل الصلاحيات ضمن النطاق المصرح به.'', N''Update permissions within the authorized scope.'', 202, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''permissions.update'', N''تعديل الصلاحيات'', N''Edit permissions'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000203'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف الصلاحيات ضمن النطاق المصرح به.'', N''Delete permissions within the authorized scope.'', 203, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''permissions.delete'', N''حذف الصلاحيات'', N''Delete permissions'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000204'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل وصول الدعم ضمن النطاق المصرح به.'', N''Update support access within the authorized scope.'', 204, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''support_access.update'', N''تعديل وصول الدعم'', N''Edit support access'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000205'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف وصول الدعم ضمن النطاق المصرح به.'', N''Delete support access within the authorized scope.'', 205, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''support_access.delete'', N''حذف وصول الدعم'', N''Delete support access'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000206'', N''Security'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض وصول الدعم ضمن النطاق المصرح به.'', N''Read support access within the authorized scope.'', 206, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''support_access.read'', N''عرض وصول الدعم'', N''Read support access'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000207'', N''Catalog'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل ملف الشركة ضمن النطاق المصرح به.'', N''Update company profile within the authorized scope.'', 207, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''company_profile.update'', N''تعديل ملف الشركة'', N''Edit company profile'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000208'', N''Catalog'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف ملف الشركة ضمن النطاق المصرح به.'', N''Delete company profile within the authorized scope.'', 208, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''company_profile.delete'', N''حذف ملف الشركة'', N''Delete company profile'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000209'', N''Catalog'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل المدن التشغيلية ضمن النطاق المصرح به.'', N''Update operating cities within the authorized scope.'', 209, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''operating_cities.update'', N''تعديل المدن التشغيلية'', N''Edit operating cities'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000210'', N''Catalog'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف المدن التشغيلية ضمن النطاق المصرح به.'', N''Delete operating cities within the authorized scope.'', 210, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''operating_cities.delete'', N''حذف المدن التشغيلية'', N''Delete operating cities'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000211'', N''Catalog'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل الوسوم ضمن النطاق المصرح به.'', N''Update tags within the authorized scope.'', 211, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''tags.update'', N''تعديل الوسوم'', N''Edit tags'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000212'', N''Catalog'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف الوسوم ضمن النطاق المصرح به.'', N''Delete tags within the authorized scope.'', 212, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''tags.delete'', N''حذف الوسوم'', N''Delete tags'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000213'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل المناديب ضمن النطاق المصرح به.'', N''Update riders within the authorized scope.'', 213, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''riders.update'', N''تعديل المناديب'', N''Edit riders'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000214'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف المناديب ضمن النطاق المصرح به.'', N''Delete riders within the authorized scope.'', 214, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''riders.delete'', N''حذف المناديب'', N''Delete riders'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000215'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل الكفلاء ضمن النطاق المصرح به.'', N''Update sponsors within the authorized scope.'', 215, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''sponsors.update'', N''تعديل الكفلاء'', N''Edit sponsors'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000216'', N''Workforce'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف الكفلاء ضمن النطاق المصرح به.'', N''Delete sponsors within the authorized scope.'', 216, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''sponsors.delete'', N''حذف الكفلاء'', N''Delete sponsors'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000217'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل الإقامات ضمن النطاق المصرح به.'', N''Update residency permits within the authorized scope.'', 217, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''residency.update'', N''تعديل الإقامات'', N''Edit residency permits'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000218'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف الإقامات ضمن النطاق المصرح به.'', N''Delete residency permits within the authorized scope.'', 218, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''residency.delete'', N''حذف الإقامات'', N''Delete residency permits'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000219'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل الرخص ضمن النطاق المصرح به.'', N''Update driver licenses within the authorized scope.'', 219, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''licenses.update'', N''تعديل الرخص'', N''Edit driver licenses'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000220'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف الرخص ضمن النطاق المصرح به.'', N''Delete driver licenses within the authorized scope.'', 220, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''licenses.delete'', N''حذف الرخص'', N''Delete driver licenses'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000221'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل بطاقات السائق ضمن النطاق المصرح به.'', N''Update rider cards within the authorized scope.'', 221, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''rider_cards.update'', N''تعديل بطاقات السائق'', N''Edit rider cards'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000222'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف بطاقات السائق ضمن النطاق المصرح به.'', N''Delete rider cards within the authorized scope.'', 222, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''rider_cards.delete'', N''حذف بطاقات السائق'', N''Delete rider cards'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000223'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل البطاقات الصحية ضمن النطاق المصرح به.'', N''Update health cards within the authorized scope.'', 223, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''health_cards.update'', N''تعديل البطاقات الصحية'', N''Edit health cards'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000224'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف البطاقات الصحية ضمن النطاق المصرح به.'', N''Delete health cards within the authorized scope.'', 224, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''health_cards.delete'', N''حذف البطاقات الصحية'', N''Delete health cards'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000225'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل التأمين الطبي ضمن النطاق المصرح به.'', N''Update medical insurance within the authorized scope.'', 225, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''insurance.update'', N''تعديل التأمين الطبي'', N''Edit medical insurance'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000226'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف التأمين الطبي ضمن النطاق المصرح به.'', N''Delete medical insurance within the authorized scope.'', 226, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''insurance.delete'', N''حذف التأمين الطبي'', N''Delete medical insurance'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000227'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل سندات الأمر ضمن النطاق المصرح به.'', N''Update promissory notes within the authorized scope.'', 227, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''promissory_notes.update'', N''تعديل سندات الأمر'', N''Edit promissory notes'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000228'', N''Compliance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف سندات الأمر ضمن النطاق المصرح به.'', N''Delete promissory notes within the authorized scope.'', 228, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''promissory_notes.delete'', N''حذف سندات الأمر'', N''Delete promissory notes'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000229'', N''Documents'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل كتالوج الوثائق ضمن النطاق المصرح به.'', N''Update document catalog within the authorized scope.'', 229, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''documents.catalog.update'', N''تعديل كتالوج الوثائق'', N''Edit document catalog'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000230'', N''Documents'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف كتالوج الوثائق ضمن النطاق المصرح به.'', N''Delete document catalog within the authorized scope.'', 230, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''documents.catalog.delete'', N''حذف كتالوج الوثائق'', N''Delete document catalog'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000231'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل حسابات المنصات ضمن النطاق المصرح به.'', N''Update platform accounts within the authorized scope.'', 231, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''platform_accounts.update'', N''تعديل حسابات المنصات'', N''Edit platform accounts'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000232'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف حسابات المنصات ضمن النطاق المصرح به.'', N''Delete platform accounts within the authorized scope.'', 232, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''platform_accounts.delete'', N''حذف حسابات المنصات'', N''Delete platform accounts'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000233'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل تكليفات المنصات ضمن النطاق المصرح به.'', N''Update platform assignments within the authorized scope.'', 233, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''platform_assignments.update'', N''تعديل تكليفات المنصات'', N''Edit platform assignments'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000234'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف تكليفات المنصات ضمن النطاق المصرح به.'', N''Delete platform assignments within the authorized scope.'', 234, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''platform_assignments.delete'', N''حذف تكليفات المنصات'', N''Delete platform assignments'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000235'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل السكن ضمن النطاق المصرح به.'', N''Update housing within the authorized scope.'', 235, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''housing.update'', N''تعديل السكن'', N''Edit housing'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000236'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف السكن ضمن النطاق المصرح به.'', N''Delete housing within the authorized scope.'', 236, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''housing.delete'', N''حذف السكن'', N''Delete housing'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000237'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل شرائح الاتصال ضمن النطاق المصرح به.'', N''Update phone SIMs within the authorized scope.'', 237, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''phone_sims.update'', N''تعديل شرائح الاتصال'', N''Edit phone SIMs'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000238'', N''Operations'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف شرائح الاتصال ضمن النطاق المصرح به.'', N''Delete phone SIMs within the authorized scope.'', 238, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''phone_sims.delete'', N''حذف شرائح الاتصال'', N''Delete phone SIMs'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000239'', N''Reporting'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل الإشعارات ضمن النطاق المصرح به.'', N''Update notifications within the authorized scope.'', 239, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''notifications.update'', N''تعديل الإشعارات'', N''Edit notifications'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000240'', N''Reporting'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف الإشعارات ضمن النطاق المصرح به.'', N''Delete notifications within the authorized scope.'', 240, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''notifications.delete'', N''حذف الإشعارات'', N''Delete notifications'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000241'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل المركبات ضمن النطاق المصرح به.'', N''Update vehicles within the authorized scope.'', 241, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.vehicles.update'', N''تعديل المركبات'', N''Edit vehicles'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1);
    INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000242'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف المركبات ضمن النطاق المصرح به.'', N''Delete vehicles within the authorized scope.'', 242, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.vehicles.delete'', N''حذف المركبات'', N''Delete vehicles'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000243'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل عهد المركبات ضمن النطاق المصرح به.'', N''Update vehicle assignments within the authorized scope.'', 243, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.assignments.update'', N''تعديل عهد المركبات'', N''Edit vehicle assignments'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000244'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف عهد المركبات ضمن النطاق المصرح به.'', N''Delete vehicle assignments within the authorized scope.'', 244, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.assignments.delete'', N''حذف عهد المركبات'', N''Delete vehicle assignments'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000245'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل بلاغات المركبات ضمن النطاق المصرح به.'', N''Update vehicle issues within the authorized scope.'', 245, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.issues.update'', N''تعديل بلاغات المركبات'', N''Edit vehicle issues'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000246'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف بلاغات المركبات ضمن النطاق المصرح به.'', N''Delete vehicle issues within the authorized scope.'', 246, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.issues.delete'', N''حذف بلاغات المركبات'', N''Delete vehicle issues'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000247'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل التزام المركبات ضمن النطاق المصرح به.'', N''Update vehicle compliance within the authorized scope.'', 247, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.compliance.update'', N''تعديل التزام المركبات'', N''Edit vehicle compliance'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000248'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف التزام المركبات ضمن النطاق المصرح به.'', N''Delete vehicle compliance within the authorized scope.'', 248, NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), N''fleet.compliance.delete'', N''حذف التزام المركبات'', N''Delete vehicle compliance'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000249'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل تصحيح بيانات الأسطول ضمن النطاق المصرح به.'', N''Update fleet corrections within the authorized scope.'', 249, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''fleet.corrections.update'', N''تعديل تصحيح بيانات الأسطول'', N''Edit fleet corrections'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000250'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف تصحيح بيانات الأسطول ضمن النطاق المصرح به.'', N''Delete fleet corrections within the authorized scope.'', 250, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''fleet.corrections.delete'', N''حذف تصحيح بيانات الأسطول'', N''Delete fleet corrections'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000251'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل تحويل تسجيل المركبة ضمن النطاق المصرح به.'', N''Update vehicle registration transitions within the authorized scope.'', 251, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''fleet.registration_transitions.update'', N''تعديل تحويل تسجيل المركبة'', N''Edit vehicle registration transitions'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000252'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف تحويل تسجيل المركبة ضمن النطاق المصرح به.'', N''Delete vehicle registration transitions within the authorized scope.'', 252, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''fleet.registration_transitions.delete'', N''حذف تحويل تسجيل المركبة'', N''Delete vehicle registration transitions'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000253'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل المسافات اليومية للمركبات ضمن النطاق المصرح به.'', N''Update vehicle daily distances within the authorized scope.'', 253, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''fleet.daily_distances.update'', N''تعديل المسافات اليومية للمركبات'', N''Edit vehicle daily distances'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000254'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف المسافات اليومية للمركبات ضمن النطاق المصرح به.'', N''Delete vehicle daily distances within the authorized scope.'', 254, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''fleet.daily_distances.delete'', N''حذف المسافات اليومية للمركبات'', N''Delete vehicle daily distances'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000255'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل تسليم وإغلاق حسابات جاهز ضمن النطاق المصرح به.'', N''Update Jahez handovers within the authorized scope.'', 255, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.handovers.update'', N''تعديل تسليم وإغلاق حسابات جاهز'', N''Edit Jahez handovers'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000256'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف تسليم وإغلاق حسابات جاهز ضمن النطاق المصرح به.'', N''Delete Jahez handovers within the authorized scope.'', 256, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.handovers.delete'', N''حذف تسليم وإغلاق حسابات جاهز'', N''Delete Jahez handovers'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000257'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل تحصيل مبالغ جاهز ضمن النطاق المصرح به.'', N''Update Collect Jahez payments within the authorized scope.'', 257, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.collections.update'', N''تعديل تحصيل مبالغ جاهز'', N''Edit Collect Jahez payments'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000258'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف تحصيل مبالغ جاهز ضمن النطاق المصرح به.'', N''Delete Collect Jahez payments within the authorized scope.'', 258, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.collections.delete'', N''حذف تحصيل مبالغ جاهز'', N''Delete Collect Jahez payments'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000259'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل تسجيل أرباح جاهز ضمن النطاق المصرح به.'', N''Update Jahez earnings within the authorized scope.'', 259, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.earnings.update'', N''تعديل تسجيل أرباح جاهز'', N''Edit Jahez earnings'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000260'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف تسجيل أرباح جاهز ضمن النطاق المصرح به.'', N''Delete Jahez earnings within the authorized scope.'', 260, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.earnings.delete'', N''حذف تسجيل أرباح جاهز'', N''Delete Jahez earnings'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000261'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل استيراد تقارير جاهز ضمن النطاق المصرح به.'', N''Update Import Jahez reports within the authorized scope.'', 261, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.imports.update'', N''تعديل استيراد تقارير جاهز'', N''Edit Import Jahez reports'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000262'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف استيراد تقارير جاهز ضمن النطاق المصرح به.'', N''Delete Import Jahez reports within the authorized scope.'', 262, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.imports.delete'', N''حذف استيراد تقارير جاهز'', N''Delete Import Jahez reports'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000263'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض استيراد تقارير جاهز ضمن النطاق المصرح به.'', N''Read Import Jahez reports within the authorized scope.'', 263, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.imports.read'', N''عرض استيراد تقارير جاهز'', N''Read Import Jahez reports'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000264'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل تسويات وأرصدة جاهز ضمن النطاق المصرح به.'', N''Update Adjust Jahez balances within the authorized scope.'', 264, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.adjustments.update'', N''تعديل تسويات وأرصدة جاهز'', N''Edit Adjust Jahez balances'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000265'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف تسويات وأرصدة جاهز ضمن النطاق المصرح به.'', N''Delete Adjust Jahez balances within the authorized scope.'', 265, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.adjustments.delete'', N''حذف تسويات وأرصدة جاهز'', N''Delete Adjust Jahez balances'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000266'', N''Fuel'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل بطاقات الوقود ضمن النطاق المصرح به.'', N''Update fuel cards within the authorized scope.'', 266, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''fuel.update'', N''تعديل بطاقات الوقود'', N''Edit fuel cards'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000267'', N''Fuel'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف بطاقات الوقود ضمن النطاق المصرح به.'', N''Delete fuel cards within the authorized scope.'', 267, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''fuel.delete'', N''حذف بطاقات الوقود'', N''Delete fuel cards'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000268'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل مواقع الصيانة ضمن النطاق المصرح به.'', N''Update maintenance locations within the authorized scope.'', 268, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''maintenance.locations.update'', N''تعديل مواقع الصيانة'', N''Edit maintenance locations'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000269'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف مواقع الصيانة ضمن النطاق المصرح به.'', N''Delete maintenance locations within the authorized scope.'', 269, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), N''maintenance.locations.delete'', N''حذف مواقع الصيانة'', N''Delete maintenance locations'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000270'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل صيانة المركبات الخارجية ضمن النطاق المصرح به.'', N''Update external maintenance jobs within the authorized scope.'', 270, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.external_jobs.update'', N''تعديل صيانة المركبات الخارجية'', N''Edit external maintenance jobs'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000271'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف صيانة المركبات الخارجية ضمن النطاق المصرح به.'', N''Delete external maintenance jobs within the authorized scope.'', 271, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.external_jobs.delete'', N''حذف صيانة المركبات الخارجية'', N''Delete external maintenance jobs'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000272'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل بيع قطع الغيار ضمن النطاق المصرح به.'', N''Update spare-part sales within the authorized scope.'', 272, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.part_sales.update'', N''تعديل بيع قطع الغيار'', N''Edit spare-part sales'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000273'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف بيع قطع الغيار ضمن النطاق المصرح به.'', N''Delete spare-part sales within the authorized scope.'', 273, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.part_sales.delete'', N''حذف بيع قطع الغيار'', N''Delete spare-part sales'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000274'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل مصنعية العميل ضمن النطاق المصرح به.'', N''Update customer labor charges within the authorized scope.'', 274, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.customer_labor_charges.update'', N''تعديل مصنعية العميل'', N''Edit customer labor charges'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000275'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف مصنعية العميل ضمن النطاق المصرح به.'', N''Delete customer labor charges within the authorized scope.'', 275, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.customer_labor_charges.delete'', N''حذف مصنعية العميل'', N''Delete customer labor charges'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000276'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل أجرة الميكانيكي ضمن النطاق المصرح به.'', N''Update mechanic labor payments within the authorized scope.'', 276, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.mechanic_labor_payments.update'', N''تعديل أجرة الميكانيكي'', N''Edit mechanic labor payments'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000277'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف أجرة الميكانيكي ضمن النطاق المصرح به.'', N''Delete mechanic labor payments within the authorized scope.'', 277, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.mechanic_labor_payments.delete'', N''حذف أجرة الميكانيكي'', N''Delete mechanic labor payments'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000278'', N''Inventory'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل أصناف المخزون ضمن النطاق المصرح به.'', N''Update inventory items within the authorized scope.'', 278, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''inventory.items.update'', N''تعديل أصناف المخزون'', N''Edit inventory items'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000279'', N''Inventory'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف أصناف المخزون ضمن النطاق المصرح به.'', N''Delete inventory items within the authorized scope.'', 279, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''inventory.items.delete'', N''حذف أصناف المخزون'', N''Delete inventory items'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000280'', N''Inventory'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل فواتير الشراء ضمن النطاق المصرح به.'', N''Update purchase receipts within the authorized scope.'', 280, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''inventory.receipts.update'', N''تعديل فواتير الشراء'', N''Edit purchase receipts'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000281'', N''Inventory'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف فواتير الشراء ضمن النطاق المصرح به.'', N''Delete purchase receipts within the authorized scope.'', 281, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''inventory.receipts.delete'', N''حذف فواتير الشراء'', N''Delete purchase receipts'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000282'', N''Inventory'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض فواتير الشراء ضمن النطاق المصرح به.'', N''Read purchase receipts within the authorized scope.'', 282, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''inventory.receipts.read'', N''عرض فواتير الشراء'', N''Read purchase receipts'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000283'', N''Inventory'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل مرتجعات المورد ضمن النطاق المصرح به.'', N''Update supplier returns within the authorized scope.'', 283, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''inventory.returns.update'', N''تعديل مرتجعات المورد'', N''Edit supplier returns'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1);
    INSERT INTO [platform].[PermissionDefinitions] ([Id], [Category], [CreatedAtUtc], [CreatedByUserId], [DeletedAtUtc], [DeletedByUserId], [DeletionReason], [DescriptionAr], [DescriptionEn], [DisplayOrder], [GrantabilityRule], [IsDeleted], [IsDeprecated], [IsHighTrust], [IsSensitive], [Key], [NameAr], [NameEn], [ReplacementKey], [RequiresClientScope], [RequiresHousingScope], [UpdatedAtUtc], [UpdatedByUserId], [Version])
    VALUES (''019c18d5-62e1-7000-a000-000000000284'', N''Inventory'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف مرتجعات المورد ضمن النطاق المصرح به.'', N''Delete supplier returns within the authorized scope.'', 284, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''inventory.returns.delete'', N''حذف مرتجعات المورد'', N''Delete supplier returns'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000285'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل طلبات الإجازة ضمن النطاق المصرح به.'', N''Update leave requests within the authorized scope.'', 285, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''leave_requests.update'', N''تعديل طلبات الإجازة'', N''Edit leave requests'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000286'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف طلبات الإجازة ضمن النطاق المصرح به.'', N''Delete leave requests within the authorized scope.'', 286, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''leave_requests.delete'', N''حذف طلبات الإجازة'', N''Delete leave requests'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000287'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل حالات الغياب ضمن النطاق المصرح به.'', N''Update absence cases within the authorized scope.'', 287, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''absence_cases.update'', N''تعديل حالات الغياب'', N''Edit absence cases'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000288'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف حالات الغياب ضمن النطاق المصرح به.'', N''Delete absence cases within the authorized scope.'', 288, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''absence_cases.delete'', N''حذف حالات الغياب'', N''Delete absence cases'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000289'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل طلبات تغيير الحالة ضمن النطاق المصرح به.'', N''Update employee status changes within the authorized scope.'', 289, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''employee_status_changes.update'', N''تعديل طلبات تغيير الحالة'', N''Edit employee status changes'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000290'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف طلبات تغيير الحالة ضمن النطاق المصرح به.'', N''Delete employee status changes within the authorized scope.'', 290, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''employee_status_changes.delete'', N''حذف طلبات تغيير الحالة'', N''Delete employee status changes'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000291'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل القضايا القانونية ضمن النطاق المصرح به.'', N''Update legal cases within the authorized scope.'', 291, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''legal_cases.update'', N''تعديل القضايا القانونية'', N''Edit legal cases'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000292'', N''Workflows'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف القضايا القانونية ضمن النطاق المصرح به.'', N''Delete legal cases within the authorized scope.'', 292, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''legal_cases.delete'', N''حذف القضايا القانونية'', N''Delete legal cases'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000293'', N''HrForms'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''تعديل قوالب نماذج الموارد البشرية ضمن النطاق المصرح به.'', N''Update HR form templates within the authorized scope.'', 293, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''hr_forms.templates.update'', N''تعديل قوالب نماذج الموارد البشرية'', N''Edit HR form templates'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000294'', N''HrForms'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''حذف قوالب نماذج الموارد البشرية ضمن النطاق المصرح به.'', N''Delete HR form templates within the authorized scope.'', 294, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''hr_forms.templates.delete'', N''حذف قوالب نماذج الموارد البشرية'', N''Delete HR form templates'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000295'', N''Documents'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض كتالوج الوثائق ضمن النطاق المصرح به.'', N''Read document catalog within the authorized scope.'', 295, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''documents.catalog.read'', N''عرض كتالوج الوثائق'', N''Read document catalog'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000296'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض تصحيح بيانات الأسطول ضمن النطاق المصرح به.'', N''Read fleet corrections within the authorized scope.'', 296, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''fleet.corrections.read'', N''عرض تصحيح بيانات الأسطول'', N''Read fleet corrections'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000297'', N''Fleet'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض تحويل تسجيل المركبة ضمن النطاق المصرح به.'', N''Read vehicle registration transitions within the authorized scope.'', 297, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''fleet.registration_transitions.read'', N''عرض تحويل تسجيل المركبة'', N''Read vehicle registration transitions'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000298'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض تسليم وإغلاق حسابات جاهز ضمن النطاق المصرح به.'', N''Read Jahez handovers within the authorized scope.'', 298, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.handovers.read'', N''عرض تسليم وإغلاق حسابات جاهز'', N''Read Jahez handovers'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000299'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض تحصيل مبالغ جاهز ضمن النطاق المصرح به.'', N''Read Collect Jahez payments within the authorized scope.'', 299, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.collections.read'', N''عرض تحصيل مبالغ جاهز'', N''Read Collect Jahez payments'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000300'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض تسجيل أرباح جاهز ضمن النطاق المصرح به.'', N''Read Jahez earnings within the authorized scope.'', 300, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''jahez.earnings.read'', N''عرض تسجيل أرباح جاهز'', N''Read Jahez earnings'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000301'', N''Jahez'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض تسويات وأرصدة جاهز ضمن النطاق المصرح به.'', N''Read Adjust Jahez balances within the authorized scope.'', 301, N''HIGH_TRUST_ONLY'', CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N''jahez.adjustments.read'', N''عرض تسويات وأرصدة جاهز'', N''Read Adjust Jahez balances'', NULL, CAST(1 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000302'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض بيع قطع الغيار ضمن النطاق المصرح به.'', N''Read spare-part sales within the authorized scope.'', 302, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.part_sales.read'', N''عرض بيع قطع الغيار'', N''Read spare-part sales'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000303'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض مصنعية العميل ضمن النطاق المصرح به.'', N''Read customer labor charges within the authorized scope.'', 303, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.customer_labor_charges.read'', N''عرض مصنعية العميل'', N''Read customer labor charges'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000304'', N''Maintenance'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض أجرة الميكانيكي ضمن النطاق المصرح به.'', N''Read mechanic labor payments within the authorized scope.'', 304, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''maintenance.mechanic_labor_payments.read'', N''عرض أجرة الميكانيكي'', N''Read mechanic labor payments'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1),
    (''019c18d5-62e1-7000-a000-000000000305'', N''Inventory'', ''2026-01-01T00:00:00.0000000+00:00'', NULL, NULL, NULL, NULL, N''عرض مرتجعات المورد ضمن النطاق المصرح به.'', N''Read supplier returns within the authorized scope.'', 305, N''SENSITIVE_DATA'', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), N''inventory.returns.read'', N''عرض مرتجعات المورد'', N''Read supplier returns'', NULL, CAST(0 AS bit), CAST(0 AS bit), NULL, NULL, 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'CreatedByUserId', N'DeletedAtUtc', N'DeletedByUserId', N'DeletionReason', N'DescriptionAr', N'DescriptionEn', N'DisplayOrder', N'GrantabilityRule', N'IsDeleted', N'IsDeprecated', N'IsHighTrust', N'IsSensitive', N'Key', N'NameAr', N'NameEn', N'ReplacementKey', N'RequiresClientScope', N'RequiresHousingScope', N'UpdatedAtUtc', N'UpdatedByUserId', N'Version') AND [object_id] = OBJECT_ID(N'[platform].[PermissionDefinitions]'))
        SET IDENTITY_INSERT [platform].[PermissionDefinitions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    INSERT INTO [platform].[PermissionDefinitions]
        (Id, [Key], Category, NameAr, NameEn, DescriptionAr, DescriptionEn,
        RequiresHousingScope, RequiresClientScope, IsSensitive, IsHighTrust, GrantabilityRule,
        Version, IsDeprecated, ReplacementKey, DisplayOrder, CreatedAtUtc, IsDeleted)
    SELECT NEWID(), legacy.[Key], legacy.Category, legacy.NameAr, legacy.NameEn,
        legacy.DescriptionAr, legacy.DescriptionEn, legacy.RequiresHousingScope, legacy.RequiresClientScope,
        legacy.IsSensitive, legacy.IsHighTrust, legacy.GrantabilityRule, legacy.Version,
        0, REPLACE(legacy.[Key], '.manage', '.create'), legacy.DisplayOrder, legacy.CreatedAtUtc, 0
    FROM #LegacyManagementDefinitions legacy
    WHERE NOT EXISTS (SELECT 1 FROM [platform].[PermissionDefinitions] currentRow WHERE currentRow.[Key] = legacy.[Key]);
    DROP TABLE #LegacyManagementDefinitions;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006074723_SplitManagementPermissionCatalog'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006074723_SplitManagementPermissionCatalog', N'10.0.11');
END;

COMMIT;
GO

