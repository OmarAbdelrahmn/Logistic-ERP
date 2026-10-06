from pathlib import Path
# Preserve active compatibility definitions for the currently hosted API. They are hidden by the new API's catalog filter.
p=next(Path('src/LogisticsERP.Infrastructure/Persistence/Migrations/Application').glob('*_SplitManagementPermissionCatalog.cs'));s=p.read_text();needle='        {\n            migrationBuilder.UpdateData('
s=s.replace(needle,'''        {
            migrationBuilder.Sql("""
                SELECT * INTO #LegacyManagementDefinitions FROM [platform].[PermissionDefinitions]
                WHERE [Key] LIKE '%.manage' AND IsDeleted = 0;
                """);
            migrationBuilder.UpdateData(''',1)
marker='        /// <inheritdoc />\n        protected override void Down';pos=s.index(marker);end=s.rfind('        }',0,pos)
addition='''            // Keep older deployed API instances working until the new authorization code is published.
            migrationBuilder.Sql("""
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
                """);
'''
s=s[:end]+addition+s[end:];p.write_text(s)
# Preserve legacy role/direct assignments too; the new runtime rejects old keys.
p=next(Path('src/LogisticsERP.Infrastructure/Identity/Migrations').glob('*_SplitManagementPermissionGrants.cs'));s=p.read_text();sql=s.split('migrationBuilder.Sql("""',1)[1].split('""");',1)[0];sql='\n'.join(line[12:] if line.startswith('            ') else line for line in sql.splitlines()).strip()
# Direct copies: read, update, delete plus a legacy copy for the old deployment.
needle='CROSS APPLY (VALUES (mapping.UpdateKey), (mapping.DeleteKey), (mapping.ReadKey)) target(PermissionKey)'
first=sql.index(needle);second=sql.index(needle,first+1);sql=sql[:second]+sql[second:].replace(needle,'CROSS APPLY (VALUES (mapping.UpdateKey), (mapping.DeleteKey), (mapping.ReadKey), (mapping.LegacyKey)) target(PermissionKey)',1)
marker='-- Expand and deduplicate requested temporary access';sql=sql.replace(marker,'''-- Compatibility grants are ignored by the new runtime but keep older API instances working.
INSERT INTO [identity].[RolePermissions] (Id, RoleId, PermissionKey, CreatedAtUtc, CreatedByUserId, UpdatedAtUtc, IsDeleted)
SELECT NEWID(), source.RoleId, source.LegacyKey, source.CreatedAtUtc, source.CreatedByUserId, @now, 0
FROM @roles source
WHERE NOT EXISTS (SELECT 1 FROM [identity].[RolePermissions] existing
    WHERE existing.RoleId = source.RoleId AND existing.PermissionKey = source.LegacyKey AND existing.IsDeleted = 0);

'''+marker)
sql=sql.replace('SELECT DISTINCT COALESCE(target.PermissionKey, original.[value]) AS PermissionKey','SELECT DISTINCT COALESCE(target.PermissionKey, original.[value]) AS PermissionKey')
sql=sql.replace('(VALUES (mapping.CreateKey), (mapping.UpdateKey), (mapping.DeleteKey), (mapping.ReadKey)) actions(value)','(VALUES (mapping.CreateKey), (mapping.UpdateKey), (mapping.DeleteKey), (mapping.ReadKey), (mapping.LegacyKey)) actions(value)')
start=sql.index('IF EXISTS (SELECT 1 FROM [identity].[RolePermissions] WHERE');end=sql.index('IF (SELECT COUNT(*) FROM @copies)',start);sql=sql[:start]+sql[end:]
sql=sql.replace('CASE WHEN mapping.ReadKey IS NULL THEN 2 ELSE 3 END','CASE WHEN mapping.ReadKey IS NULL THEN 3 ELSE 4 END')
p.write_text(s.split('migrationBuilder.Sql("""',1)[0]+'migrationBuilder.Sql("""\n'+''.join('            '+line+'\n' for line in sql.splitlines())+'            """);'+s.split('""");',1)[1])
Path('.codex-temp/split-management-grants.sql').write_text(sql)
# Hide compatibility keys from catalog and user/role editing responses on the new backend.
p=Path('src/LogisticsERP.Infrastructure/Authentication/UserManagementService.cs');s=p.read_text();s=s.replace('.Where(permission => !permission.IsDeprecated)', '.Where(permission => !permission.IsDeprecated && PermissionKeys.All.Contains(permission.Key))');p.write_text(s)
# IDs/validity remain available in the DB; UI responses should contain only supported keys.
print('Added rollout compatibility without runtime aliases')
