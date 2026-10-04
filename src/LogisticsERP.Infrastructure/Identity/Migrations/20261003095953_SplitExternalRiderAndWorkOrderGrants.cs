using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Identity.Migrations;

public partial class SplitExternalRiderAndWorkOrderGrants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @now datetimeoffset = SYSUTCDATETIME();
            DECLARE @map TABLE (LegacyKey nvarchar(150), CreateKey nvarchar(150), UpdateKey nvarchar(150), DeleteKey nvarchar(150));
            INSERT INTO @map VALUES
                ('external_riders.manage', 'external_riders.create', 'external_riders.update', 'external_riders.delete'),
                ('maintenance.work_orders.manage', 'maintenance.work_orders.create', 'maintenance.work_orders.update', 'maintenance.work_orders.delete');

            DECLARE @roles TABLE (Id uniqueidentifier, RoleId uniqueidentifier, LegacyKey nvarchar(150), CreatedAtUtc datetimeoffset, CreatedByUserId uniqueidentifier);
            INSERT INTO @roles
            SELECT grantRow.Id, grantRow.RoleId, grantRow.PermissionKey, grantRow.CreatedAtUtc, grantRow.CreatedByUserId
            FROM [identity].[RolePermissions] grantRow JOIN @map mapping ON mapping.LegacyKey = grantRow.PermissionKey
            WHERE grantRow.IsDeleted = 0;

            -- Retain original grant IDs for create when possible, including the seeded IDs.
            UPDATE grantRow SET IsDeleted = 1, DeletedAtUtc = @now, UpdatedAtUtc = @now,
                DeletionReason = N'Replaced by existing create permission during the permission split.'
            FROM [identity].[RolePermissions] grantRow JOIN @roles source ON source.Id = grantRow.Id
            JOIN @map mapping ON mapping.LegacyKey = source.LegacyKey
            WHERE EXISTS (SELECT 1 FROM [identity].[RolePermissions] existing
                WHERE existing.RoleId = source.RoleId AND existing.PermissionKey = mapping.CreateKey AND existing.IsDeleted = 0);
            UPDATE grantRow SET PermissionKey = mapping.CreateKey, UpdatedAtUtc = @now
            FROM [identity].[RolePermissions] grantRow JOIN @roles source ON source.Id = grantRow.Id
            JOIN @map mapping ON mapping.LegacyKey = source.LegacyKey WHERE grantRow.IsDeleted = 0;

            INSERT INTO [identity].[RolePermissions] (Id, RoleId, PermissionKey, CreatedAtUtc, CreatedByUserId, UpdatedAtUtc, IsDeleted)
            SELECT NEWID(), source.RoleId, target.PermissionKey, source.CreatedAtUtc, source.CreatedByUserId, @now, 0
            FROM @roles source JOIN @map mapping ON mapping.LegacyKey = source.LegacyKey
            CROSS APPLY (VALUES (mapping.UpdateKey), (mapping.DeleteKey)) target(PermissionKey)
            WHERE NOT EXISTS (SELECT 1 FROM [identity].[RolePermissions] existing
                WHERE existing.RoleId = source.RoleId AND existing.PermissionKey = target.PermissionKey AND existing.IsDeleted = 0);

            DECLARE @directIds TABLE (Id uniqueidentifier, LegacyKey nvarchar(150));
            INSERT INTO @directIds
            SELECT assignment.Id, assignment.PermissionKey FROM [identity].[UserDirectPermissionAssignments] assignment
            JOIN @map mapping ON mapping.LegacyKey = assignment.PermissionKey WHERE assignment.IsDeleted = 0;
            DECLARE @copies TABLE (SourceId uniqueidentifier, CopyId uniqueidentifier);
            -- Copy grants and denials with their original validity windows and scope flags.
            MERGE [identity].[UserDirectPermissionAssignments] AS destination
            USING (
                SELECT assignment.*, target.PermissionKey AS TargetKey
                FROM [identity].[UserDirectPermissionAssignments] assignment JOIN @directIds original ON original.Id = assignment.Id
                JOIN @map mapping ON mapping.LegacyKey = original.LegacyKey
                CROSS APPLY (VALUES (mapping.UpdateKey), (mapping.DeleteKey)) target(PermissionKey)
            ) AS source ON 1 = 0
            WHEN NOT MATCHED THEN INSERT
                (Id, UserId, PermissionKey, Effect, StartsAtUtc, ExpiresAtUtc, GrantedByUserId, GrantReason,
                    IsAllHousingScope, IsAllClientScope, IncludesFuturePlatformContracts, CreatedAtUtc, CreatedByUserId,
                    UpdatedAtUtc, UpdatedByUserId, IsDeleted)
            VALUES (NEWID(), source.UserId, source.TargetKey, source.Effect, source.StartsAtUtc, source.ExpiresAtUtc,
                source.GrantedByUserId, source.GrantReason, source.IsAllHousingScope, source.IsAllClientScope,
                source.IncludesFuturePlatformContracts, source.CreatedAtUtc, source.CreatedByUserId, @now, source.UpdatedByUserId, 0)
            OUTPUT source.Id, inserted.Id INTO @copies;

            INSERT INTO [identity].[AccessScopes]
                (Id, UserRoleAssignmentId, DirectPermissionAssignmentId, ScopeType, TargetId,
                    CreatedAtUtc, CreatedByUserId, UpdatedAtUtc, UpdatedByUserId,
                    IsDeleted, DeletedAtUtc, DeletedByUserId, DeletionReason)
            SELECT NEWID(), NULL, copy.CopyId, scope.ScopeType, scope.TargetId,
                scope.CreatedAtUtc, scope.CreatedByUserId, @now, scope.UpdatedByUserId,
                scope.IsDeleted, scope.DeletedAtUtc, scope.DeletedByUserId, scope.DeletionReason
            FROM [identity].[AccessScopes] scope JOIN @copies copy ON copy.SourceId = scope.DirectPermissionAssignmentId;

            UPDATE assignment SET PermissionKey = mapping.CreateKey, UpdatedAtUtc = @now
            FROM [identity].[UserDirectPermissionAssignments] assignment JOIN @directIds original ON original.Id = assignment.Id
            JOIN @map mapping ON mapping.LegacyKey = original.LegacyKey;

            -- Default grants may already have been assigned through the admin UI.
            INSERT INTO [identity].[RolePermissions] (Id, RoleId, PermissionKey, CreatedAtUtc, IsDeleted)
            SELECT seed.Id, seed.RoleId, seed.PermissionKey, CONVERT(datetimeoffset, '2026-01-01T00:00:00+00:00'), 0
            FROM (VALUES
                (CONVERT(uniqueidentifier, '019c18d5-62e1-7000-b000-000000000117'), CONVERT(uniqueidentifier, '019c18d5-62e1-7000-9000-000000000001'), 'maintenance.work_orders.update'),
                ('019c18d5-62e1-7000-b000-000000000118', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.work_orders.delete'),
                ('019c18d5-62e1-7000-b000-000000000119', '019c18d5-62e1-7000-9000-000000000001', 'external_riders.read'),
                ('019c18d5-62e1-7000-b000-000000000120', '019c18d5-62e1-7000-9000-000000000001', 'external_riders.create'),
                ('019c18d5-62e1-7000-b000-000000000121', '019c18d5-62e1-7000-9000-000000000001', 'external_riders.update'),
                ('019c18d5-62e1-7000-b000-000000000122', '019c18d5-62e1-7000-9000-000000000001', 'external_riders.delete'),
                ('019c18d5-62e1-7000-b000-000000000123', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.work_orders.update'),
                ('019c18d5-62e1-7000-b000-000000000124', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.work_orders.delete')
            ) seed(Id, RoleId, PermissionKey)
            WHERE NOT EXISTS (SELECT 1 FROM [identity].[RolePermissions] existing
                WHERE existing.RoleId = seed.RoleId AND existing.PermissionKey = seed.PermissionKey AND existing.IsDeleted = 0)
                AND NOT EXISTS (SELECT 1 FROM [identity].[RolePermissions] existing WHERE existing.Id = seed.Id);

            UPDATE [identity].[SupportAccessGrants]
            SET RequestedPermissionsJson = REPLACE(REPLACE(RequestedPermissionsJson,
                    '"external_riders.manage"', '"external_riders.create","external_riders.update","external_riders.delete"'),
                    '"maintenance.work_orders.manage"', '"maintenance.work_orders.create","maintenance.work_orders.update","maintenance.work_orders.delete"'),
                UpdatedAtUtc = @now
            WHERE RequestedPermissionsJson LIKE '%"external_riders.manage"%' OR RequestedPermissionsJson LIKE '%"maintenance.work_orders.manage"%';

            UPDATE userRow SET AuthorizationVersion = AuthorizationVersion + 1
            FROM [identity].[Users] userRow
            WHERE userRow.IsDeleted = 0 AND (
                EXISTS (SELECT 1 FROM [identity].[UserDirectPermissionAssignments] assignment JOIN @directIds original ON original.Id = assignment.Id
                    WHERE assignment.UserId = userRow.Id)
                OR EXISTS (SELECT 1 FROM [identity].[UserRoleAssignments] assignment WHERE assignment.UserId = userRow.Id AND assignment.IsDeleted = 0
                    AND (assignment.RoleId IN (SELECT RoleId FROM @roles)
                        OR assignment.RoleId IN ('019c18d5-62e1-7000-9000-000000000001', '019c18d5-62e1-7000-9000-000000000002')))
            );

            IF EXISTS (SELECT 1 FROM [identity].[RolePermissions] WHERE IsDeleted = 0 AND PermissionKey IN ('external_riders.manage', 'maintenance.work_orders.manage'))
                OR EXISTS (SELECT 1 FROM [identity].[UserDirectPermissionAssignments] WHERE IsDeleted = 0 AND PermissionKey IN ('external_riders.manage', 'maintenance.work_orders.manage'))
                THROW 51000, 'Legacy manage grants remain after the permission split.', 1;
            IF (SELECT COUNT(*) FROM @copies) <> 2 * (SELECT COUNT(*) FROM @directIds)
                THROW 51000, 'Direct permission grants were not fully copied.', 1;
            IF EXISTS (SELECT 1 FROM @copies copy
                JOIN [identity].[UserDirectPermissionAssignments] original ON original.Id = copy.SourceId
                JOIN [identity].[UserDirectPermissionAssignments] replacement ON replacement.Id = copy.CopyId
                WHERE EXISTS (SELECT original.UserId, original.Effect, original.StartsAtUtc, original.ExpiresAtUtc,
                        original.GrantedByUserId, original.GrantReason, original.IsAllHousingScope,
                        original.IsAllClientScope, original.IncludesFuturePlatformContracts
                    EXCEPT SELECT replacement.UserId, replacement.Effect, replacement.StartsAtUtc, replacement.ExpiresAtUtc,
                        replacement.GrantedByUserId, replacement.GrantReason, replacement.IsAllHousingScope,
                        replacement.IsAllClientScope, replacement.IncludesFuturePlatformContracts))
                THROW 51000, 'The direct permission split changed assignment restrictions.', 1;
            IF EXISTS (SELECT 1 FROM @copies copy WHERE
                (SELECT COUNT(*) FROM [identity].[AccessScopes] scope WHERE scope.DirectPermissionAssignmentId = copy.SourceId)
                <> (SELECT COUNT(*) FROM [identity].[AccessScopes] scope WHERE scope.DirectPermissionAssignmentId = copy.CopyId))
                THROW 51000, 'The direct permission split changed scope counts.', 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Independent grants cannot safely be collapsed after administrators have edited them.
        throw new NotSupportedException("Restore the pre-migration database backup to undo this permission split without broadening or losing permissions.");
    }
}
