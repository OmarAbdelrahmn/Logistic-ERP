SET NOCOUNT ON;
DECLARE @user uniqueidentifier='bade1c20-d7ea-4ae4-d3fc-08df14b0e9ba';
SELECT PermissionKey,Effect,StartsAtUtc,ExpiresAtUtc,LEN(GrantReason) AS ReasonLength,IsAllHousingScope,IsAllClientScope,IsDeleted FROM [identity].UserDirectPermissionAssignments WHERE UserId=@user AND IsDeleted=0 ORDER BY PermissionKey;
SELECT d.PermissionKey,s.ScopeType,s.TargetId FROM [identity].AccessScopes s JOIN [identity].UserDirectPermissionAssignments d ON d.Id=s.DirectPermissionAssignmentId WHERE d.UserId=@user AND d.IsDeleted=0 AND s.IsDeleted=0;
SELECT COUNT(*) AS ActiveDefinitions FROM platform.PermissionDefinitions WHERE IsDeleted=0;
