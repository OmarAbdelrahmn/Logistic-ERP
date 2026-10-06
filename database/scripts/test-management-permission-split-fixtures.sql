SET NOCOUNT ON;
DECLARE @fixtureUser uniqueidentifier = (SELECT TOP 1 Id FROM [identity].Users WHERE IsDeleted = 0 ORDER BY Id);
IF @fixtureUser IS NULL THROW 51000, 'A user is required for rolled-back migration fixtures.', 1;
DECLARE @fixtureNow datetimeoffset = SYSUTCDATETIME();
DECLARE @fixtureDirect TABLE (Id uniqueidentifier, Effect int, StartsAtUtc datetimeoffset, ExpiresAtUtc datetimeoffset, AllClients bit);
INSERT INTO @fixtureDirect VALUES
    (NEWID(), 1, DATEADD(day, -1, @fixtureNow), DATEADD(day, 1, @fixtureNow), 0),
    (NEWID(), 2, DATEADD(day, 1, @fixtureNow), DATEADD(day, 2, @fixtureNow), 0),
    (NEWID(), 1, DATEADD(day, -2, @fixtureNow), DATEADD(day, -1, @fixtureNow), 1),
    (NEWID(), 2, DATEADD(day, -1, @fixtureNow), NULL, 1);
INSERT INTO [identity].UserDirectPermissionAssignments
    (Id, UserId, PermissionKey, Effect, StartsAtUtc, ExpiresAtUtc, GrantedByUserId, GrantReason,
    IsAllHousingScope, IsAllClientScope, IncludesFuturePlatformContracts, CreatedAtUtc, IsDeleted)
SELECT Id, @fixtureUser, 'jahez.imports.manage', Effect, StartsAtUtc, ExpiresAtUtc, @fixtureUser,
    N'Permission migration rollback fixture', 0, AllClients, 1, @fixtureNow, 0 FROM @fixtureDirect;
INSERT INTO [identity].AccessScopes
    (Id, DirectPermissionAssignmentId, ScopeType, TargetId, CreatedAtUtc, IsDeleted)
SELECT NEWID(), Id, 2, NEWID(), @fixtureNow, 0 FROM @fixtureDirect;
INSERT INTO [identity].AccessScopes
    (Id, DirectPermissionAssignmentId, ScopeType, TargetId, CreatedAtUtc, IsDeleted, DeletedAtUtc, DeletionReason)
SELECT NEWID(), Id, 3, NEWID(), @fixtureNow, 1, @fixtureNow, N'Fixture historical scope' FROM @fixtureDirect;
CREATE TABLE #PermissionSplitFixtureSupport (Id uniqueidentifier);
DECLARE @fixtureSupport uniqueidentifier = NEWID();
INSERT INTO #PermissionSplitFixtureSupport VALUES(@fixtureSupport);
INSERT INTO [identity].SupportAccessGrants
    (Id, PlatformOperatorUserId, RequestedPermissionsJson, RequestedScopesJson, Reason, Status,
    RequestedStartAtUtc, RequestedEndAtUtc, IsBreakGlass, CreatedAtUtc, IsDeleted)
VALUES(@fixtureSupport, @fixtureUser, '["fuel.manage","fuel.create","jahez.imports.manage"]', '[]',
    N'Permission migration rollback fixture', 1, @fixtureNow, DATEADD(hour, 1, @fixtureNow), 0, @fixtureNow, 0);
