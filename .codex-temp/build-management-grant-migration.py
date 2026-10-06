from pathlib import Path
import re,json
maps=json.loads(Path('.codex-temp/manage-map.json').read_text());reads={m['prefix'] for m in maps.values()}
p=next(Path('src/LogisticsERP.Infrastructure/Identity/Migrations').glob('*_SplitManagementPermissionGrants.cs'));generated=p.read_text()
seeds=re.findall(r'\{ new Guid\("([^"]+)"\).*?false, "([^"]+)", new Guid\("([^"]+)"\), null, null \}',generated)
assert len(seeds)>70,len(seeds)
prior=Path('src/LogisticsERP.Infrastructure/Identity/Migrations/20261003095953_SplitExternalRiderAndWorkOrderGrants.cs').read_text()
sql=prior.split('migrationBuilder.Sql("""',1)[1].split('""");',1)[0]
sql='\n'.join(line[12:] if line.startswith('            ') else line for line in sql.splitlines()).strip()
start=sql.index('DECLARE @map TABLE');end=sql.index('DECLARE @roles TABLE')
map_sql='DECLARE @map TABLE (LegacyKey nvarchar(150), CreateKey nvarchar(150), UpdateKey nvarchar(150), DeleteKey nvarchar(150), ReadKey nvarchar(150));\nINSERT INTO @map VALUES\n'
map_sql+=',\n'.join("    ('%s', '%s.create', '%s.update', '%s.delete', %s)"%(m['old'],m['prefix'],m['prefix'],m['prefix'],("'%s.read'"%m['prefix']) if m['prefix'] in reads else 'NULL') for m in maps.values())+';\n\n'
map_sql+='DECLARE @seeds TABLE (Id uniqueidentifier, RoleId uniqueidentifier, PermissionKey nvarchar(150));\nINSERT INTO @seeds VALUES\n'+',\n'.join(f"    ('{i}', '{role}', '{key}')" for i,key,role in seeds)+';\n\n'
sql=sql[:start]+map_sql+sql[end:]
sql=sql.replace('CROSS APPLY (VALUES (mapping.UpdateKey), (mapping.DeleteKey)) target(PermissionKey)','CROSS APPLY (VALUES (mapping.UpdateKey), (mapping.DeleteKey), (mapping.ReadKey)) target(PermissionKey)')
sql=sql.replace('SELECT NEWID(), source.RoleId, target.PermissionKey', 'SELECT COALESCE(seed.Id, NEWID()), source.RoleId, target.PermissionKey')
sql=sql.replace('WHERE NOT EXISTS (SELECT 1 FROM [identity].[RolePermissions] existing\n    WHERE existing.RoleId = source.RoleId', 'LEFT JOIN @seeds seed ON seed.RoleId = source.RoleId AND seed.PermissionKey = target.PermissionKey\nWHERE target.PermissionKey IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [identity].[RolePermissions] existing\n    WHERE existing.RoleId = source.RoleId',1)
sql=sql.replace(') AS source ON 1 = 0','    WHERE target.PermissionKey IS NOT NULL\n) AS source ON 1 = 0')
# Remove old default baseline restoration; only split active grants, preserving intentional role revocations.
start=sql.index('-- Default grants');end=sql.index('UPDATE userRow SET AuthorizationVersion')
support='''-- Expand and deduplicate requested temporary access while keeping scopes and validity unchanged.
DECLARE @supportUsers TABLE (UserId uniqueidentifier);
UPDATE grantRow
SET RequestedPermissionsJson = expanded.Json, UpdatedAtUtc = @now
OUTPUT inserted.PlatformOperatorUserId INTO @supportUsers
FROM [identity].[SupportAccessGrants] grantRow
CROSS APPLY (
    SELECT '[' + STRING_AGG(CONVERT(nvarchar(max), '"' + STRING_ESCAPE(keys.PermissionKey, 'json') + '"'), ',') + ']' AS Json
    FROM (
        SELECT DISTINCT COALESCE(target.PermissionKey, original.[value]) AS PermissionKey
        FROM OPENJSON(grantRow.RequestedPermissionsJson) original
        LEFT JOIN @map mapping ON mapping.LegacyKey = original.[value]
        OUTER APPLY (SELECT value AS PermissionKey FROM
            (VALUES (mapping.CreateKey), (mapping.UpdateKey), (mapping.DeleteKey), (mapping.ReadKey)) actions(value)
            WHERE value IS NOT NULL) target
    ) keys
) expanded
WHERE ISJSON(grantRow.RequestedPermissionsJson) = 1 AND EXISTS (
    SELECT 1 FROM OPENJSON(grantRow.RequestedPermissionsJson) original JOIN @map mapping ON mapping.LegacyKey = original.[value]);

'''
sql=sql[:start]+support+sql[end:]
sql=sql.replace("OR assignment.RoleId IN ('019c18d5-62e1-7000-9000-000000000001', '019c18d5-62e1-7000-9000-000000000002')",'OR 1 = 0')
sql=sql.replace(');\n\nIF EXISTS', '    OR EXISTS (SELECT 1 FROM @supportUsers supportUser WHERE supportUser.UserId = userRow.Id)\n);\n\nIF EXISTS',1)
sql=sql.replace("PermissionKey IN ('external_riders.manage', 'maintenance.work_orders.manage')", 'PermissionKey IN (SELECT LegacyKey FROM @map)')
sql=sql.replace('2 * (SELECT COUNT(*) FROM @directIds)', '(SELECT SUM(CASE WHEN mapping.ReadKey IS NULL THEN 2 ELSE 3 END) FROM @directIds original JOIN @map mapping ON mapping.LegacyKey = original.LegacyKey)')
sql=sql.replace('OR 1 = 0','OR assignment.RoleId IN (SELECT RoleId FROM @seeds)') # cache affected baseline roles included
# Check every role retained all split actions.
sql+='''\nIF EXISTS (SELECT 1 FROM @roles source JOIN @map mapping ON mapping.LegacyKey = source.LegacyKey
    CROSS APPLY (VALUES(mapping.CreateKey),(mapping.UpdateKey),(mapping.DeleteKey),(mapping.ReadKey)) target(PermissionKey)
    WHERE target.PermissionKey IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [identity].[RolePermissions] grantRow
        WHERE grantRow.RoleId = source.RoleId AND grantRow.PermissionKey = target.PermissionKey AND grantRow.IsDeleted = 0))
    THROW 51000, 'Role permission split is incomplete.', 1;
'''
Path('.codex-temp/split-management-grants.sql').write_text(sql)
p.write_text('''using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Identity.Migrations;

public partial class SplitManagementPermissionGrants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
'''+''.join('            '+line+'\n' for line in sql.splitlines())+'''            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Independent action grants cannot safely be collapsed. Restore the pre-migration authorization backup instead.");
}
''')
# Prevent a rollback that collapses grants after they have been independently edited.
p=next(Path('src/LogisticsERP.Infrastructure/Persistence/Migrations/Application').glob('*_SplitManagementPermissionCatalog.cs'));s=p.read_text();start=s.index('        protected override void Down(');s=s[:start]+'''        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("Independent action permissions cannot safely be collapsed. Restore the pre-migration authorization backup instead.");
    }
}
''';p.write_text(s)
print('Grant migration prepared')
