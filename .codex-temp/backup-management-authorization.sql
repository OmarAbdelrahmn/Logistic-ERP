SET NOCOUNT ON;
SELECT * FROM platform.PermissionDefinitions;
SELECT * FROM [identity].RolePermissions;
SELECT * FROM [identity].UserDirectPermissionAssignments;
SELECT * FROM [identity].AccessScopes;
SELECT * FROM [identity].SupportAccessGrants;
SELECT Id, AuthorizationVersion FROM [identity].Users;
SELECT * FROM migration.__ApplicationMigrationsHistory;
SELECT * FROM migration.__IdentityMigrationsHistory;
