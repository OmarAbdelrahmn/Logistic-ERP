SET NOCOUNT ON;
DECLARE @map TABLE (LegacyKey nvarchar(150), Prefix nvarchar(150));
INSERT INTO @map VALUES
('roles.manage','roles'),
('permissions.manage','permissions'),
('support_access.manage','support_access'),
('company_profile.manage','company_profile'),
('operating_cities.manage','operating_cities'),
('tags.manage','tags'),
('riders.manage','riders'),
('sponsors.manage','sponsors'),
('residency.manage','residency'),
('licenses.manage','licenses'),
('rider_cards.manage','rider_cards'),
('health_cards.manage','health_cards'),
('insurance.manage','insurance'),
('promissory_notes.manage','promissory_notes'),
('documents.catalog.manage','documents.catalog'),
('platform_accounts.manage','platform_accounts'),
('platform_assignments.manage','platform_assignments'),
('housing.manage','housing'),
('phone_sims.manage','phone_sims'),
('notifications.manage','notifications'),
('fleet.vehicles.manage','fleet.vehicles'),
('fleet.assignments.manage','fleet.assignments'),
('fleet.issues.manage','fleet.issues'),
('fleet.compliance.manage','fleet.compliance'),
('fleet.corrections.manage','fleet.corrections'),
('fleet.registration_transitions.manage','fleet.registration_transitions'),
('fleet.daily_distances.manage','fleet.daily_distances'),
('jahez.handovers.manage','jahez.handovers'),
('jahez.collections.manage','jahez.collections'),
('jahez.earnings.manage','jahez.earnings'),
('jahez.imports.manage','jahez.imports'),
('jahez.adjustments.manage','jahez.adjustments'),
('fuel.manage','fuel'),
('maintenance.locations.manage','maintenance.locations'),
('maintenance.external_jobs.manage','maintenance.external_jobs'),
('maintenance.part_sales.manage','maintenance.part_sales'),
('maintenance.customer_labor_charges.manage','maintenance.customer_labor_charges'),
('maintenance.mechanic_labor_payments.manage','maintenance.mechanic_labor_payments'),
('inventory.items.manage','inventory.items'),
('inventory.receipts.manage','inventory.receipts'),
('inventory.returns.manage','inventory.returns'),
('leave_requests.manage','leave_requests'),
('absence_cases.manage','absence_cases'),
('employee_status_changes.manage','employee_status_changes'),
('legal_cases.manage','legal_cases'),
('hr_forms.templates.manage','hr_forms.templates');
IF EXISTS (SELECT 1 FROM @map mapping CROSS APPLY (VALUES('read'),('create'),('update'),('delete')) action(Suffix)
    WHERE NOT EXISTS (SELECT 1 FROM platform.PermissionDefinitions definition
        WHERE definition.[Key] = mapping.Prefix + '.' + action.Suffix AND definition.IsDeleted = 0 AND definition.IsDeprecated = 0))
    THROW 51000, 'One of the four action definitions is missing.', 1;
IF EXISTS (SELECT 1 FROM [identity].RolePermissions legacy JOIN @map mapping ON mapping.LegacyKey = legacy.PermissionKey
    CROSS APPLY (VALUES('read'),('create'),('update'),('delete')) action(Suffix)
    WHERE legacy.IsDeleted = 0 AND NOT EXISTS (SELECT 1 FROM [identity].RolePermissions target
        WHERE target.RoleId = legacy.RoleId AND target.PermissionKey = mapping.Prefix + '.' + action.Suffix AND target.IsDeleted = 0))
    THROW 51000, 'A legacy role holder did not receive all four action permissions.', 1;
IF EXISTS (SELECT 1 FROM [identity].UserDirectPermissionAssignments legacy JOIN @map mapping ON mapping.LegacyKey = legacy.PermissionKey
    CROSS APPLY (VALUES('read'),('create'),('update'),('delete')) action(Suffix)
    WHERE legacy.IsDeleted = 0 AND NOT EXISTS (SELECT 1 FROM [identity].UserDirectPermissionAssignments target
        WHERE target.UserId = legacy.UserId AND target.PermissionKey = mapping.Prefix + '.' + action.Suffix AND target.IsDeleted = 0
        AND NOT EXISTS (SELECT legacy.Effect, legacy.StartsAtUtc, legacy.ExpiresAtUtc, legacy.GrantedByUserId,
                legacy.GrantReason, legacy.IsAllHousingScope, legacy.IsAllClientScope, legacy.IncludesFuturePlatformContracts
            EXCEPT SELECT target.Effect, target.StartsAtUtc, target.ExpiresAtUtc, target.GrantedByUserId,
                target.GrantReason, target.IsAllHousingScope, target.IsAllClientScope, target.IncludesFuturePlatformContracts)
        AND NOT EXISTS (SELECT ScopeType, TargetId, IsDeleted FROM [identity].AccessScopes WHERE DirectPermissionAssignmentId = legacy.Id
            EXCEPT SELECT ScopeType, TargetId, IsDeleted FROM [identity].AccessScopes WHERE DirectPermissionAssignmentId = target.Id)
        AND NOT EXISTS (SELECT ScopeType, TargetId, IsDeleted FROM [identity].AccessScopes WHERE DirectPermissionAssignmentId = target.Id
            EXCEPT SELECT ScopeType, TargetId, IsDeleted FROM [identity].AccessScopes WHERE DirectPermissionAssignmentId = legacy.Id)))
    THROW 51000, 'A direct grant or deny lost an action, validity restriction, or exact scope.', 1;
IF EXISTS (SELECT 1 FROM [identity].SupportAccessGrants grantRow
    CROSS APPLY OPENJSON(grantRow.RequestedPermissionsJson) legacy
    JOIN @map mapping ON mapping.LegacyKey = legacy.[value]
    CROSS APPLY (VALUES('read'),('create'),('update'),('delete')) action(Suffix)
    WHERE grantRow.IsDeleted = 0 AND NOT EXISTS (
        SELECT 1 FROM OPENJSON(grantRow.RequestedPermissionsJson) target WHERE target.[value] = mapping.Prefix + '.' + action.Suffix))
    THROW 51000, 'Temporary support access did not receive all four action permissions.', 1;
SELECT DB_NAME() AS DatabaseName,
    (SELECT COUNT(*) FROM @map) AS SplitFamilies,
    (SELECT COUNT(*) FROM platform.PermissionDefinitions definition JOIN @map mapping ON definition.[Key] IN
        (mapping.Prefix + '.read', mapping.Prefix + '.create', mapping.Prefix + '.update', mapping.Prefix + '.delete')
        WHERE definition.IsDeleted = 0 AND definition.IsDeprecated = 0) AS ActionDefinitions,
    (SELECT COUNT(*) FROM [identity].RolePermissions legacy JOIN @map mapping ON mapping.LegacyKey = legacy.PermissionKey WHERE legacy.IsDeleted = 0) AS LegacyRoleGrantsCovered,
    (SELECT COUNT(*) FROM [identity].UserDirectPermissionAssignments legacy JOIN @map mapping ON mapping.LegacyKey = legacy.PermissionKey WHERE legacy.IsDeleted = 0) AS LegacyDirectAssignmentsCovered,
    (SELECT COUNT(DISTINCT assignment.UserId) FROM [identity].UserRoleAssignments assignment JOIN [identity].RolePermissions grantRow ON grantRow.RoleId = assignment.RoleId
        JOIN @map mapping ON mapping.LegacyKey = grantRow.PermissionKey WHERE assignment.IsDeleted = 0 AND grantRow.IsDeleted = 0) AS UsersThroughRoles,
    (SELECT COUNT(DISTINCT legacy.UserId) FROM [identity].UserDirectPermissionAssignments legacy JOIN @map mapping ON mapping.LegacyKey = legacy.PermissionKey WHERE legacy.IsDeleted = 0) AS UsersThroughDirectAssignments;
IF OBJECT_ID('tempdb..#PermissionSplitFixtureSupport') IS NOT NULL
BEGIN
    IF (SELECT COUNT(*) FROM [identity].SupportAccessGrants grantRow
        JOIN #PermissionSplitFixtureSupport fixture ON fixture.Id = grantRow.Id
        CROSS APPLY OPENJSON(grantRow.RequestedPermissionsJson)) <> 10
        THROW 51000, 'Support fixture did not expand and deduplicate the permission array.', 1;
    PRINT 'Scoped grants/denies, future/expired validity, historical scopes and support JSON fixtures verified.';
END;
