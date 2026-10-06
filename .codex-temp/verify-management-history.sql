SET NOCOUNT ON;
SELECT MigrationId FROM migration.__ApplicationMigrationsHistory WHERE MigrationId LIKE '%SplitManagementPermissionCatalog';
SELECT MigrationId FROM migration.__IdentityMigrationsHistory WHERE MigrationId LIKE '%SplitManagementPermissionGrants';
SELECT COUNT(*) AS PermissionDefinitions FROM platform.PermissionDefinitions WHERE IsDeleted = 0;
SELECT COUNT(*) AS HolderUsers FROM (
    SELECT assignment.UserId FROM [identity].UserRoleAssignments assignment
    JOIN [identity].RolePermissions grantRow ON grantRow.RoleId = assignment.RoleId
    WHERE assignment.IsDeleted = 0 AND grantRow.IsDeleted = 0 AND grantRow.PermissionKey LIKE '%.manage'
    UNION
    SELECT UserId FROM [identity].UserDirectPermissionAssignments
    WHERE IsDeleted = 0 AND Effect = 1 AND PermissionKey LIKE '%.manage'
) holders;
