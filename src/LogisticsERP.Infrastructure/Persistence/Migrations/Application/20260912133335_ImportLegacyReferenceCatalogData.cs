using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class ImportLegacyReferenceCatalogData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM [platform].[ClientPlatforms] WHERE [Id] = '01A02A0D-845D-7535-AC25-1E1F0C8E5808' OR [Code] = N'SHIFTZ')
                    INSERT INTO [platform].[ClientPlatforms] ([Id], [Code], [NameAr], [NameEn], [Status], [Notes], [CreatedAtUtc], [UpdatedAtUtc], [IsDeleted], [SupportedPaymentModels])
                    VALUES ('01A02A0D-845D-7535-AC25-1E1F0C8E5808', N'SHIFTZ', N'شفز', N'Shiftz', 1, N'Created by the HR Excel import.', '2026-08-22T15:18:49.3852253+00:00', '2026-08-25T11:47:59.2735725+00:00', 0, 1);

                IF NOT EXISTS (SELECT 1 FROM [platform].[ClientPlatforms] WHERE [Id] = '01A02A0D-845C-7E0B-AA0C-3CD6246785D2' OR [Code] = N'JAHEZ')
                    INSERT INTO [platform].[ClientPlatforms] ([Id], [Code], [NameAr], [NameEn], [Status], [Notes], [CreatedAtUtc], [IsDeleted], [SupportedPaymentModels])
                    VALUES ('01A02A0D-845C-7E0B-AA0C-3CD6246785D2', N'JAHEZ', N'جاهز', N'Jahez', 1, N'Created by the HR Excel import.', '2026-08-22T15:18:49.3852253+00:00', 0, 1);

                IF NOT EXISTS (SELECT 1 FROM [platform].[ClientPlatforms] WHERE [Id] = '01A02A0D-845C-7C77-881F-5286903B7311' OR [Code] = N'HUNGER')
                    INSERT INTO [platform].[ClientPlatforms] ([Id], [Code], [NameAr], [NameEn], [Status], [Notes], [CreatedAtUtc], [IsDeleted], [SupportedPaymentModels])
                    VALUES ('01A02A0D-845C-7C77-881F-5286903B7311', N'HUNGER', N'هنقرستيشن', N'HungerStation', 1, N'Created by the HR Excel import.', '2026-08-22T15:18:49.3852253+00:00', 0, 3);

                IF NOT EXISTS (SELECT 1 FROM [platform].[ClientPlatforms] WHERE [Id] = '01A02A0D-845D-7BD6-B829-84CAF8BADC7F' OR [Code] = N'NINJA')
                    INSERT INTO [platform].[ClientPlatforms] ([Id], [Code], [NameAr], [NameEn], [Status], [Notes], [CreatedAtUtc], [IsDeleted], [SupportedPaymentModels])
                    VALUES ('01A02A0D-845D-7BD6-B829-84CAF8BADC7F', N'NINJA', N'نينجا', N'Ninja', 1, N'Created by the HR Excel import.', '2026-08-22T15:18:49.3852253+00:00', 0, 3);

                IF NOT EXISTS (SELECT 1 FROM [platform].[ClientPlatforms] WHERE [Id] = '01A02A0D-845C-7C44-BB8E-9FDF4D1AEC2B' OR [Code] = N'AMAZON')
                    INSERT INTO [platform].[ClientPlatforms] ([Id], [Code], [NameAr], [NameEn], [Status], [Notes], [CreatedAtUtc], [UpdatedAtUtc], [IsDeleted], [SupportedPaymentModels])
                    VALUES ('01A02A0D-845C-7C44-BB8E-9FDF4D1AEC2B', N'AMAZON', N'أمازون', N'Amazon', 1, N'Created by the HR Excel import.', '2026-08-22T15:18:49.3852253+00:00', '2026-08-25T11:47:52.6413745+00:00', 0, 2);

                IF NOT EXISTS (SELECT 1 FROM [platform].[ClientPlatforms] WHERE [Id] = '01A02A0D-83F6-77A0-9165-E430B588044D' OR [Code] = N'KEETA')
                    INSERT INTO [platform].[ClientPlatforms] ([Id], [Code], [NameAr], [NameEn], [Status], [Notes], [CreatedAtUtc], [IsDeleted], [SupportedPaymentModels])
                    VALUES ('01A02A0D-83F6-77A0-9165-E430B588044D', N'KEETA', N'كيتا', N'Keeta', 1, N'Created by the HR Excel import.', '2026-08-22T15:18:49.3852253+00:00', 0, 3);

                IF NOT EXISTS (SELECT 1 FROM [app].[JobTitles] WHERE [Id] = '01A02A0D-888A-7E06-A2F5-216AE238C339' OR [Code] = N'JOB_BA433520879CF9196E54')
                    INSERT INTO [app].[JobTitles] ([Id], [Code], [NameAr], [NameEn], [DescriptionAr], [Status], [CreatedAtUtc], [IsDeleted])
                    VALUES ('01A02A0D-888A-7E06-A2F5-216AE238C339', N'JOB_BA433520879CF9196E54', N'إداري', N'إداري', N'تم إنشاؤه من ملف الموارد البشرية.', 1, '2026-08-22T15:18:49.3852253+00:00', 0);

                IF NOT EXISTS (SELECT 1 FROM [app].[JobTitles] WHERE [Id] = '01A02A0D-8983-7B7D-BE63-5C3060D542AA' OR [Code] = N'JOB_C3AB5217E58EB8B77DE9')
                    INSERT INTO [app].[JobTitles] ([Id], [Code], [NameAr], [NameEn], [DescriptionAr], [Status], [CreatedAtUtc], [IsDeleted])
                    VALUES ('01A02A0D-8983-7B7D-BE63-5C3060D542AA', N'JOB_C3AB5217E58EB8B77DE9', N'مندوب توصيل', N'مندوب توصيل', N'تم إنشاؤه من ملف الموارد البشرية.', 1, '2026-08-22T15:18:49.3852253+00:00', 0);

                IF NOT EXISTS (SELECT 1 FROM [app].[JobTitles] WHERE [Id] = '01A02A0D-863A-735A-BD7F-D6F7873D8BE9' OR [Code] = N'JOB_9D811768B30D7E09EC45')
                    INSERT INTO [app].[JobTitles] ([Id], [Code], [NameAr], [NameEn], [DescriptionAr], [Status], [CreatedAtUtc], [IsDeleted])
                    VALUES ('01A02A0D-863A-735A-BD7F-D6F7873D8BE9', N'JOB_9D811768B30D7E09EC45', N'ميكانيكي', N'ميكانيكي', N'تم إنشاؤه من ملف الموارد البشرية.', 1, '2026-08-22T15:18:49.3852253+00:00', 0);

                IF NOT EXISTS (SELECT 1 FROM [app].[ResidencyProfessions] WHERE [Id] = '01A02A0D-8ED2-721D-AE7E-634458011594' OR [Code] = N'PROF_B1834C1F4570CD367BD5')
                    INSERT INTO [app].[ResidencyProfessions] ([Id], [Code], [NameAr], [NameEn], [Status], [CreatedAtUtc], [IsDeleted]) VALUES ('01A02A0D-8ED2-721D-AE7E-634458011594', N'PROF_B1834C1F4570CD367BD5', N'المهن', N'المهن', 1, '2026-08-22T15:18:49.3852253+00:00', 0);
                IF NOT EXISTS (SELECT 1 FROM [app].[ResidencyProfessions] WHERE [Id] = '01A02A0D-8EC1-7580-BA0B-7210C2373102' OR [Code] = N'PROF_73B9E11CE09EDBA373D8')
                    INSERT INTO [app].[ResidencyProfessions] ([Id], [Code], [NameAr], [NameEn], [Status], [CreatedAtUtc], [IsDeleted]) VALUES ('01A02A0D-8EC1-7580-BA0B-7210C2373102', N'PROF_73B9E11CE09EDBA373D8', N'عامل تحميل و تنزيل', N'عامل تحميل و تنزيل', 1, '2026-08-22T15:18:49.3852253+00:00', 0);
                IF NOT EXISTS (SELECT 1 FROM [app].[ResidencyProfessions] WHERE [Id] = '01A02A0D-9600-7567-BC93-7D447BC5367E' OR [Code] = N'PROF_16E323F5D2EE30B31307')
                    INSERT INTO [app].[ResidencyProfessions] ([Id], [Code], [NameAr], [NameEn], [Status], [CreatedAtUtc], [IsDeleted]) VALUES ('01A02A0D-9600-7567-BC93-7D447BC5367E', N'PROF_16E323F5D2EE30B31307', N'محاسب', N'محاسب', 1, '2026-08-22T15:18:49.3852253+00:00', 0);
                IF NOT EXISTS (SELECT 1 FROM [app].[ResidencyProfessions] WHERE [Id] = '01A02A0D-9370-7C10-B15A-A48E7331FDAD' OR [Code] = N'PROF_79FF5E40B8407138DAB7')
                    INSERT INTO [app].[ResidencyProfessions] ([Id], [Code], [NameAr], [NameEn], [Status], [CreatedAtUtc], [IsDeleted]) VALUES ('01A02A0D-9370-7C10-B15A-A48E7331FDAD', N'PROF_79FF5E40B8407138DAB7', N'سائق سيارة', N'سائق سيارة', 1, '2026-08-22T15:18:49.3852253+00:00', 0);
                IF NOT EXISTS (SELECT 1 FROM [app].[ResidencyProfessions] WHERE [Id] = '01A02A0D-94DD-7D99-AF14-E0707CB50270' OR [Code] = N'PROF_E3C6BB6C8F78713507EE')
                    INSERT INTO [app].[ResidencyProfessions] ([Id], [Code], [NameAr], [NameEn], [Status], [CreatedAtUtc], [IsDeleted]) VALUES ('01A02A0D-94DD-7D99-AF14-E0707CB50270', N'PROF_E3C6BB6C8F78713507EE', N'عامل تعبئة و تغليف', N'عامل تعبئة و تغليف', 1, '2026-08-22T15:18:49.3852253+00:00', 0);
                IF NOT EXISTS (SELECT 1 FROM [app].[ResidencyProfessions] WHERE [Id] = '01A02A0D-8651-79D7-9572-E793753D465C' OR [Code] = N'PROF_0FDC58004D87A557DC25')
                    INSERT INTO [app].[ResidencyProfessions] ([Id], [Code], [NameAr], [NameEn], [Status], [CreatedAtUtc], [IsDeleted]) VALUES ('01A02A0D-8651-79D7-9572-E793753D465C', N'PROF_0FDC58004D87A557DC25', N'سائق شاحنة صغيرة', N'سائق شاحنة صغيرة', 1, '2026-08-22T15:18:49.3852253+00:00', 0);
                IF NOT EXISTS (SELECT 1 FROM [app].[ResidencyProfessions] WHERE [Id] = '01A02A0D-966E-7774-A996-ED71D4EFBBA4' OR [Code] = N'PROF_888C45534C849455214B')
                    INSERT INTO [app].[ResidencyProfessions] ([Id], [Code], [NameAr], [NameEn], [Status], [CreatedAtUtc], [IsDeleted]) VALUES ('01A02A0D-966E-7774-A996-ED71D4EFBBA4', N'PROF_888C45534C849455214B', N'عامل تعبئة رفوف', N'عامل تعبئة رفوف', 1, '2026-08-22T15:18:49.3852253+00:00', 0);
                IF NOT EXISTS (SELECT 1 FROM [app].[ResidencyProfessions] WHERE [Id] = '01A02A0D-8E3C-7120-B94C-FC28187020F4' OR [Code] = N'PROF_58945EEED17B18BE5ACB')
                    INSERT INTO [app].[ResidencyProfessions] ([Id], [Code], [NameAr], [NameEn], [Status], [CreatedAtUtc], [IsDeleted]) VALUES ('01A02A0D-8E3C-7120-B94C-FC28187020F4', N'PROF_58945EEED17B18BE5ACB', N'سائق دراجة نارية', N'سائق دراجة نارية', 1, '2026-08-22T15:18:49.3852253+00:00', 0);
                IF NOT EXISTS (SELECT 1 FROM [app].[ResidencyProfessions] WHERE [Id] = '01A02A0D-94D5-719C-AB81-FD7228C56C96' OR [Code] = N'PROF_CE34B17AB5FA4E19C208')
                    INSERT INTO [app].[ResidencyProfessions] ([Id], [Code], [NameAr], [NameEn], [Status], [CreatedAtUtc], [IsDeleted]) VALUES ('01A02A0D-94D5-719C-AB81-FD7228C56C96', N'PROF_CE34B17AB5FA4E19C208', N'منظف واجهات مباني', N'منظف واجهات مباني', 1, '2026-08-22T15:18:49.3852253+00:00', 0);

                INSERT INTO [app].[JobTitleOperationalWorkTypes] ([Id], [JobTitleId], [OperationalWorkTypeId], [CreatedAtUtc], [IsDeleted])
                SELECT '01A02A0D-8A68-78A9-80D6-3712036FDF13', job.[Id], workType.[Id], '2026-08-22T15:18:49.3852253+00:00', 0
                FROM [app].[JobTitles] job CROSS JOIN [app].[OperationalWorkTypes] workType
                WHERE job.[Code] = N'JOB_C3AB5217E58EB8B77DE9' AND workType.[Code] = N'MOTORCYCLE'
                  AND NOT EXISTS (SELECT 1 FROM [app].[JobTitleOperationalWorkTypes] x WHERE x.[JobTitleId] = job.[Id] AND x.[OperationalWorkTypeId] = workType.[Id] AND x.[IsDeleted] = 0);
                INSERT INTO [app].[JobTitleOperationalWorkTypes] ([Id], [JobTitleId], [OperationalWorkTypeId], [CreatedAtUtc], [IsDeleted])
                SELECT '01A02A0D-8B07-7569-A1E5-3C294A9012C4', job.[Id], workType.[Id], '2026-08-22T15:18:49.3852253+00:00', 0
                FROM [app].[JobTitles] job CROSS JOIN [app].[OperationalWorkTypes] workType
                WHERE job.[Code] = N'JOB_BA433520879CF9196E54' AND workType.[Code] = N'CAR'
                  AND NOT EXISTS (SELECT 1 FROM [app].[JobTitleOperationalWorkTypes] x WHERE x.[JobTitleId] = job.[Id] AND x.[OperationalWorkTypeId] = workType.[Id] AND x.[IsDeleted] = 0);
                INSERT INTO [app].[JobTitleOperationalWorkTypes] ([Id], [JobTitleId], [OperationalWorkTypeId], [CreatedAtUtc], [IsDeleted])
                SELECT '01A02A0D-89F1-7976-B9A8-4020229CFB60', job.[Id], workType.[Id], '2026-08-22T15:18:49.3852253+00:00', 0
                FROM [app].[JobTitles] job CROSS JOIN [app].[OperationalWorkTypes] workType
                WHERE job.[Code] = N'JOB_C3AB5217E58EB8B77DE9' AND workType.[Code] = N'CAR'
                  AND NOT EXISTS (SELECT 1 FROM [app].[JobTitleOperationalWorkTypes] x WHERE x.[JobTitleId] = job.[Id] AND x.[OperationalWorkTypeId] = workType.[Id] AND x.[IsDeleted] = 0);
                INSERT INTO [app].[JobTitleOperationalWorkTypes] ([Id], [JobTitleId], [OperationalWorkTypeId], [CreatedAtUtc], [IsDeleted])
                SELECT '01A02A0D-896D-73C0-9F05-F419E274ED73', job.[Id], workType.[Id], '2026-08-22T15:18:49.3852253+00:00', 0
                FROM [app].[JobTitles] job CROSS JOIN [app].[OperationalWorkTypes] workType
                WHERE job.[Code] = N'JOB_BA433520879CF9196E54' AND workType.[Code] = N'ADMIN'
                  AND NOT EXISTS (SELECT 1 FROM [app].[JobTitleOperationalWorkTypes] x WHERE x.[JobTitleId] = job.[Id] AND x.[OperationalWorkTypeId] = workType.[Id] AND x.[IsDeleted] = 0);
                INSERT INTO [app].[JobTitleOperationalWorkTypes] ([Id], [JobTitleId], [OperationalWorkTypeId], [CreatedAtUtc], [IsDeleted])
                SELECT '01A02A0D-8791-79C7-90CC-F8D5697C5DBE', job.[Id], workType.[Id], '2026-08-22T15:18:49.3852253+00:00', 0
                FROM [app].[JobTitles] job CROSS JOIN [app].[OperationalWorkTypes] workType
                WHERE job.[Code] = N'JOB_9D811768B30D7E09EC45' AND workType.[Code] = N'ADMIN'
                  AND NOT EXISTS (SELECT 1 FROM [app].[JobTitleOperationalWorkTypes] x WHERE x.[JobTitleId] = job.[Id] AND x.[OperationalWorkTypeId] = workType.[Id] AND x.[IsDeleted] = 0);

                IF NOT EXISTS (SELECT 1 FROM [app].[InsuranceCompanies] WHERE [Id] = '01A032F7-BD2E-7245-A094-880C3B67DFE3' OR [Code] = N'MYTU')
                    INSERT INTO [app].[InsuranceCompanies] ([Id], [Code], [NameAr], [Status], [CreatedAtUtc], [IsDeleted])
                    VALUES ('01A032F7-BD2E-7245-A094-880C3B67DFE3', N'MYTU', N'taminin', 2, '2026-08-24T08:51:31.7896543+00:00', 0);

                IF NOT EXISTS (SELECT 1 FROM [app].[InsurancePlanLevels] WHERE [Id] = '01A032F8-20F8-7D71-867A-846C76F9DA31' OR [Code] = N'STANDER')
                    INSERT INTO [app].[InsurancePlanLevels] ([Id], [InsuranceCompanyId], [Code], [NameAr], [Rank], [EffectiveFrom], [Status], [CreatedAtUtc], [IsDeleted])
                    SELECT '01A032F8-20F8-7D71-867A-846C76F9DA31', company.[Id], N'STANDER', N'standers', 1, '2026-08-11', 2, '2026-08-24T08:51:57.3398561+00:00', 0
                    FROM [app].[InsuranceCompanies] company WHERE company.[Code] = N'MYTU';

                IF NOT EXISTS (SELECT 1 FROM [app].[VehicleManufacturers] WHERE [Id] = '01A0421B-D4B9-788F-B73E-DAABF2650C25' OR [Code] = N'KIA')
                    INSERT INTO [app].[VehicleManufacturers] ([Id], [Code], [NameAr], [NameEn], [Status], [DisplayOrder], [CreatedAtUtc], [IsDeleted])
                    VALUES ('01A0421B-D4B9-788F-B73E-DAABF2650C25', N'KIA', N'KIA', N'KIA', 1, 1, '2026-08-27T07:25:15.4049220+00:00', 0);

                IF NOT EXISTS (SELECT 1 FROM [app].[VehicleModels] WHERE [Id] = '01A0421C-5885-7648-A758-205624D28FBB' OR [Code] = N'K10')
                    INSERT INTO [app].[VehicleModels] ([Id], [VehicleManufacturerId], [Code], [NameAr], [NameEn], [VehicleType], [DefaultFuelType], [Status], [CreatedAtUtc], [IsDeleted])
                    SELECT '01A0421C-5885-7648-A758-205624D28FBB', manufacturer.[Id], N'K10', N'KI10', N'K10', 2, 1, 1, '2026-08-27T07:25:49.0872285+00:00', 0
                    FROM [app].[VehicleManufacturers] manufacturer WHERE manufacturer.[Code] = N'KIA';

                IF NOT EXISTS (SELECT 1 FROM [app].[VehicleSuppliers] WHERE [Id] = '01A0421C-BA0C-7C04-9D51-95FD16FCD2D8' OR [Code] = N'SIB')
                    INSERT INTO [app].[VehicleSuppliers] ([Id], [Code], [NameAr], [NameEn], [Status], [CreatedAtUtc], [IsDeleted])
                    VALUES ('01A0421C-BA0C-7C04-9D51-95FD16FCD2D8', N'SIB', N'cib bank', N'cib bank', 1, '2026-08-27T07:26:14.0599531+00:00', 0);

                IF NOT EXISTS (SELECT 1 FROM [app].[DriverLicenseCategories] WHERE [Id] = '01A0334C-C508-79D6-8585-5B90DE0DE3EE' OR [Code] = N'PRIVATE')
                    INSERT INTO [app].[DriverLicenseCategories] ([Id], [Code], [NameAr], [NameEn], [Status], [CreatedAtUtc], [IsDeleted])
                    VALUES ('01A0334C-C508-79D6-8585-5B90DE0DE3EE', N'PRIVATE', N'خصوصي', N'خصوصي', 1, '2026-08-24T10:24:24.3554319+00:00', 0);

                IF NOT EXISTS (SELECT 1 FROM [app].[DocumentRequirements] WHERE [Id] = '01A02EDB-BB48-7D82-AD02-0EAD9A1E3D7F' OR ([DocumentTypeId] = '019C18D5-62E1-7000-8000-000000000030' AND [RelationshipType] = 1 AND [EffectiveFrom] = '2026-08-19'))
                    INSERT INTO [app].[DocumentRequirements] ([Id], [DocumentTypeId], [RelationshipType], [AppliesToRiderProfile], [IsRequired], [ReminderOffsetsDays], [EffectiveFrom], [Status], [CreatedAtUtc], [UpdatedAtUtc], [IsDeleted])
                    VALUES ('01A02EDB-BB48-7D82-AD02-0EAD9A1E3D7F', '019C18D5-62E1-7000-8000-000000000030', 1, 1, 1, N'30', '2026-08-19', 1, '2026-08-23T13:42:27.4618033+00:00', '2026-08-31T08:36:05.2581391+00:00', 0);

                IF NOT EXISTS (SELECT 1 FROM [app].[DocumentRequirements] WHERE [Id] = '01A02EDC-155B-731E-9F40-2BE822BE840F' OR ([DocumentTypeId] = '019C18D5-62E1-7000-8000-000000000033' AND [RelationshipType] = 1 AND [EffectiveFrom] = '2026-08-23'))
                    INSERT INTO [app].[DocumentRequirements] ([Id], [DocumentTypeId], [RelationshipType], [AppliesToRiderProfile], [IsRequired], [ReminderOffsetsDays], [EffectiveFrom], [Status], [CreatedAtUtc], [IsDeleted])
                    VALUES ('01A02EDC-155B-731E-9F40-2BE822BE840F', '019C18D5-62E1-7000-8000-000000000033', 1, 1, 1, N'30', '2026-08-23', 1, '2026-08-23T13:42:50.4726889+00:00', 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [app].[DocumentRequirements] WHERE [Id] IN ('01A02EDB-BB48-7D82-AD02-0EAD9A1E3D7F', '01A02EDC-155B-731E-9F40-2BE822BE840F');
                DELETE FROM [app].[JobTitleOperationalWorkTypes] WHERE [Id] IN ('01A02A0D-8A68-78A9-80D6-3712036FDF13', '01A02A0D-8B07-7569-A1E5-3C294A9012C4', '01A02A0D-89F1-7976-B9A8-4020229CFB60', '01A02A0D-896D-73C0-9F05-F419E274ED73', '01A02A0D-8791-79C7-90CC-F8D5697C5DBE');
                DELETE FROM [app].[InsurancePlanLevels] WHERE [Id] = '01A032F8-20F8-7D71-867A-846C76F9DA31';
                DELETE FROM [app].[VehicleModels] WHERE [Id] = '01A0421C-5885-7648-A758-205624D28FBB';
                DELETE FROM [platform].[ClientPlatforms] WHERE [Id] IN ('01A02A0D-845D-7535-AC25-1E1F0C8E5808', '01A02A0D-845C-7E0B-AA0C-3CD6246785D2', '01A02A0D-845C-7C77-881F-5286903B7311', '01A02A0D-845D-7BD6-B829-84CAF8BADC7F', '01A02A0D-845C-7C44-BB8E-9FDF4D1AEC2B', '01A02A0D-83F6-77A0-9165-E430B588044D');
                DELETE FROM [app].[JobTitles] WHERE [Id] IN ('01A02A0D-888A-7E06-A2F5-216AE238C339', '01A02A0D-8983-7B7D-BE63-5C3060D542AA', '01A02A0D-863A-735A-BD7F-D6F7873D8BE9');
                DELETE FROM [app].[ResidencyProfessions] WHERE [Id] IN ('01A02A0D-8ED2-721D-AE7E-634458011594', '01A02A0D-8EC1-7580-BA0B-7210C2373102', '01A02A0D-9600-7567-BC93-7D447BC5367E', '01A02A0D-9370-7C10-B15A-A48E7331FDAD', '01A02A0D-94DD-7D99-AF14-E0707CB50270', '01A02A0D-8651-79D7-9572-E793753D465C', '01A02A0D-966E-7774-A996-ED71D4EFBBA4', '01A02A0D-8E3C-7120-B94C-FC28187020F4', '01A02A0D-94D5-719C-AB81-FD7228C56C96');
                DELETE FROM [app].[InsuranceCompanies] WHERE [Id] = '01A032F7-BD2E-7245-A094-880C3B67DFE3';
                DELETE FROM [app].[VehicleManufacturers] WHERE [Id] = '01A0421B-D4B9-788F-B73E-DAABF2650C25';
                DELETE FROM [app].[VehicleSuppliers] WHERE [Id] = '01A0421C-BA0C-7C04-9D51-95FD16FCD2D8';
                DELETE FROM [app].[DriverLicenseCategories] WHERE [Id] = '01A0334C-C508-79D6-8585-5B90DE0DE3EE';
                """);
        }
    }
}
