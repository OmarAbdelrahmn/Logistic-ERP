SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS ServerName;
SELECT [Key], IsDeprecated, IsDeleted FROM platform.PermissionDefinitions WHERE [Key] LIKE '%.manage' ORDER BY [Key];
SELECT TOP 5 MigrationId FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId DESC;
SELECT TOP 5 MigrationId FROM migration.__IdentityMigrationsHistory ORDER BY MigrationId DESC;
SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'identity' AND (TABLE_NAME LIKE '%Permission%' OR TABLE_NAME IN ('AccessScopes','SupportAccessGrants')) ORDER BY TABLE_NAME,ORDINAL_POSITION;
