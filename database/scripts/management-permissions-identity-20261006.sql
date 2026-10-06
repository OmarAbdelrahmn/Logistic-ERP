BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261006074727_SplitManagementPermissionGrants'
)
BEGIN
    DECLARE @now datetimeoffset = SYSUTCDATETIME();
    DECLARE @map TABLE (LegacyKey nvarchar(150), CreateKey nvarchar(150), UpdateKey nvarchar(150), DeleteKey nvarchar(150), ReadKey nvarchar(150));
    INSERT INTO @map VALUES
        ('roles.manage', 'roles.create', 'roles.update', 'roles.delete', 'roles.read'),
        ('permissions.manage', 'permissions.create', 'permissions.update', 'permissions.delete', 'permissions.read'),
        ('support_access.manage', 'support_access.create', 'support_access.update', 'support_access.delete', 'support_access.read'),
        ('company_profile.manage', 'company_profile.create', 'company_profile.update', 'company_profile.delete', 'company_profile.read'),
        ('operating_cities.manage', 'operating_cities.create', 'operating_cities.update', 'operating_cities.delete', 'operating_cities.read'),
        ('tags.manage', 'tags.create', 'tags.update', 'tags.delete', 'tags.read'),
        ('riders.manage', 'riders.create', 'riders.update', 'riders.delete', 'riders.read'),
        ('sponsors.manage', 'sponsors.create', 'sponsors.update', 'sponsors.delete', 'sponsors.read'),
        ('residency.manage', 'residency.create', 'residency.update', 'residency.delete', 'residency.read'),
        ('licenses.manage', 'licenses.create', 'licenses.update', 'licenses.delete', 'licenses.read'),
        ('rider_cards.manage', 'rider_cards.create', 'rider_cards.update', 'rider_cards.delete', 'rider_cards.read'),
        ('health_cards.manage', 'health_cards.create', 'health_cards.update', 'health_cards.delete', 'health_cards.read'),
        ('insurance.manage', 'insurance.create', 'insurance.update', 'insurance.delete', 'insurance.read'),
        ('promissory_notes.manage', 'promissory_notes.create', 'promissory_notes.update', 'promissory_notes.delete', 'promissory_notes.read'),
        ('documents.catalog.manage', 'documents.catalog.create', 'documents.catalog.update', 'documents.catalog.delete', 'documents.catalog.read'),
        ('platform_accounts.manage', 'platform_accounts.create', 'platform_accounts.update', 'platform_accounts.delete', 'platform_accounts.read'),
        ('platform_assignments.manage', 'platform_assignments.create', 'platform_assignments.update', 'platform_assignments.delete', 'platform_assignments.read'),
        ('housing.manage', 'housing.create', 'housing.update', 'housing.delete', 'housing.read'),
        ('phone_sims.manage', 'phone_sims.create', 'phone_sims.update', 'phone_sims.delete', 'phone_sims.read'),
        ('notifications.manage', 'notifications.create', 'notifications.update', 'notifications.delete', 'notifications.read'),
        ('fleet.vehicles.manage', 'fleet.vehicles.create', 'fleet.vehicles.update', 'fleet.vehicles.delete', 'fleet.vehicles.read'),
        ('fleet.assignments.manage', 'fleet.assignments.create', 'fleet.assignments.update', 'fleet.assignments.delete', 'fleet.assignments.read'),
        ('fleet.issues.manage', 'fleet.issues.create', 'fleet.issues.update', 'fleet.issues.delete', 'fleet.issues.read'),
        ('fleet.compliance.manage', 'fleet.compliance.create', 'fleet.compliance.update', 'fleet.compliance.delete', 'fleet.compliance.read'),
        ('fleet.corrections.manage', 'fleet.corrections.create', 'fleet.corrections.update', 'fleet.corrections.delete', 'fleet.corrections.read'),
        ('fleet.registration_transitions.manage', 'fleet.registration_transitions.create', 'fleet.registration_transitions.update', 'fleet.registration_transitions.delete', 'fleet.registration_transitions.read'),
        ('fleet.daily_distances.manage', 'fleet.daily_distances.create', 'fleet.daily_distances.update', 'fleet.daily_distances.delete', 'fleet.daily_distances.read'),
        ('jahez.handovers.manage', 'jahez.handovers.create', 'jahez.handovers.update', 'jahez.handovers.delete', 'jahez.handovers.read'),
        ('jahez.collections.manage', 'jahez.collections.create', 'jahez.collections.update', 'jahez.collections.delete', 'jahez.collections.read'),
        ('jahez.earnings.manage', 'jahez.earnings.create', 'jahez.earnings.update', 'jahez.earnings.delete', 'jahez.earnings.read'),
        ('jahez.imports.manage', 'jahez.imports.create', 'jahez.imports.update', 'jahez.imports.delete', 'jahez.imports.read'),
        ('jahez.adjustments.manage', 'jahez.adjustments.create', 'jahez.adjustments.update', 'jahez.adjustments.delete', 'jahez.adjustments.read'),
        ('fuel.manage', 'fuel.create', 'fuel.update', 'fuel.delete', 'fuel.read'),
        ('maintenance.locations.manage', 'maintenance.locations.create', 'maintenance.locations.update', 'maintenance.locations.delete', 'maintenance.locations.read'),
        ('maintenance.external_jobs.manage', 'maintenance.external_jobs.create', 'maintenance.external_jobs.update', 'maintenance.external_jobs.delete', 'maintenance.external_jobs.read'),
        ('maintenance.part_sales.manage', 'maintenance.part_sales.create', 'maintenance.part_sales.update', 'maintenance.part_sales.delete', 'maintenance.part_sales.read'),
        ('maintenance.customer_labor_charges.manage', 'maintenance.customer_labor_charges.create', 'maintenance.customer_labor_charges.update', 'maintenance.customer_labor_charges.delete', 'maintenance.customer_labor_charges.read'),
        ('maintenance.mechanic_labor_payments.manage', 'maintenance.mechanic_labor_payments.create', 'maintenance.mechanic_labor_payments.update', 'maintenance.mechanic_labor_payments.delete', 'maintenance.mechanic_labor_payments.read'),
        ('inventory.items.manage', 'inventory.items.create', 'inventory.items.update', 'inventory.items.delete', 'inventory.items.read'),
        ('inventory.receipts.manage', 'inventory.receipts.create', 'inventory.receipts.update', 'inventory.receipts.delete', 'inventory.receipts.read'),
        ('inventory.returns.manage', 'inventory.returns.create', 'inventory.returns.update', 'inventory.returns.delete', 'inventory.returns.read'),
        ('leave_requests.manage', 'leave_requests.create', 'leave_requests.update', 'leave_requests.delete', 'leave_requests.read'),
        ('absence_cases.manage', 'absence_cases.create', 'absence_cases.update', 'absence_cases.delete', 'absence_cases.read'),
        ('employee_status_changes.manage', 'employee_status_changes.create', 'employee_status_changes.update', 'employee_status_changes.delete', 'employee_status_changes.read'),
        ('legal_cases.manage', 'legal_cases.create', 'legal_cases.update', 'legal_cases.delete', 'legal_cases.read'),
        ('hr_forms.templates.manage', 'hr_forms.templates.create', 'hr_forms.templates.update', 'hr_forms.templates.delete', 'hr_forms.templates.read');

    DECLARE @seeds TABLE (Id uniqueidentifier, RoleId uniqueidentifier, PermissionKey nvarchar(150));
    INSERT INTO @seeds VALUES
        ('019c18d5-62e1-7000-b000-000000000138', '019c18d5-62e1-7000-9000-000000000001', 'roles.update'),
        ('019c18d5-62e1-7000-b000-000000000139', '019c18d5-62e1-7000-9000-000000000001', 'roles.delete'),
        ('019c18d5-62e1-7000-b000-000000000140', '019c18d5-62e1-7000-9000-000000000001', 'permissions.update'),
        ('019c18d5-62e1-7000-b000-000000000141', '019c18d5-62e1-7000-9000-000000000001', 'permissions.delete'),
        ('019c18d5-62e1-7000-b000-000000000142', '019c18d5-62e1-7000-9000-000000000001', 'support_access.update'),
        ('019c18d5-62e1-7000-b000-000000000143', '019c18d5-62e1-7000-9000-000000000001', 'support_access.delete'),
        ('019c18d5-62e1-7000-b000-000000000144', '019c18d5-62e1-7000-9000-000000000001', 'support_access.read'),
        ('019c18d5-62e1-7000-b000-000000000145', '019c18d5-62e1-7000-9000-000000000001', 'operating_cities.update'),
        ('019c18d5-62e1-7000-b000-000000000146', '019c18d5-62e1-7000-9000-000000000001', 'operating_cities.delete'),
        ('019c18d5-62e1-7000-b000-000000000147', '019c18d5-62e1-7000-9000-000000000001', 'company_profile.update'),
        ('019c18d5-62e1-7000-b000-000000000148', '019c18d5-62e1-7000-9000-000000000001', 'company_profile.delete'),
        ('019c18d5-62e1-7000-b000-000000000149', '019c18d5-62e1-7000-9000-000000000001', 'tags.update'),
        ('019c18d5-62e1-7000-b000-000000000150', '019c18d5-62e1-7000-9000-000000000001', 'tags.delete'),
        ('019c18d5-62e1-7000-b000-000000000151', '019c18d5-62e1-7000-9000-000000000001', 'documents.catalog.update'),
        ('019c18d5-62e1-7000-b000-000000000152', '019c18d5-62e1-7000-9000-000000000001', 'documents.catalog.delete'),
        ('019c18d5-62e1-7000-b000-000000000153', '019c18d5-62e1-7000-9000-000000000001', 'documents.catalog.read'),
        ('019c18d5-62e1-7000-b000-000000000154', '019c18d5-62e1-7000-9000-000000000001', 'fleet.vehicles.update'),
        ('019c18d5-62e1-7000-b000-000000000155', '019c18d5-62e1-7000-9000-000000000001', 'fleet.vehicles.delete'),
        ('019c18d5-62e1-7000-b000-000000000156', '019c18d5-62e1-7000-9000-000000000001', 'fleet.assignments.update'),
        ('019c18d5-62e1-7000-b000-000000000157', '019c18d5-62e1-7000-9000-000000000001', 'fleet.assignments.delete'),
        ('019c18d5-62e1-7000-b000-000000000158', '019c18d5-62e1-7000-9000-000000000001', 'fleet.issues.update'),
        ('019c18d5-62e1-7000-b000-000000000159', '019c18d5-62e1-7000-9000-000000000001', 'fleet.issues.delete'),
        ('019c18d5-62e1-7000-b000-000000000160', '019c18d5-62e1-7000-9000-000000000001', 'fleet.compliance.update'),
        ('019c18d5-62e1-7000-b000-000000000161', '019c18d5-62e1-7000-9000-000000000001', 'fleet.compliance.delete'),
        ('019c18d5-62e1-7000-b000-000000000162', '019c18d5-62e1-7000-9000-000000000001', 'fleet.corrections.update'),
        ('019c18d5-62e1-7000-b000-000000000163', '019c18d5-62e1-7000-9000-000000000001', 'fleet.corrections.delete'),
        ('019c18d5-62e1-7000-b000-000000000164', '019c18d5-62e1-7000-9000-000000000001', 'fleet.corrections.read'),
        ('019c18d5-62e1-7000-b000-000000000165', '019c18d5-62e1-7000-9000-000000000002', 'fleet.vehicles.update'),
        ('019c18d5-62e1-7000-b000-000000000166', '019c18d5-62e1-7000-9000-000000000002', 'fleet.vehicles.delete'),
        ('019c18d5-62e1-7000-b000-000000000167', '019c18d5-62e1-7000-9000-000000000002', 'fleet.assignments.update'),
        ('019c18d5-62e1-7000-b000-000000000168', '019c18d5-62e1-7000-9000-000000000002', 'fleet.assignments.delete'),
        ('019c18d5-62e1-7000-b000-000000000169', '019c18d5-62e1-7000-9000-000000000002', 'fleet.issues.update'),
        ('019c18d5-62e1-7000-b000-000000000170', '019c18d5-62e1-7000-9000-000000000002', 'fleet.issues.delete'),
        ('019c18d5-62e1-7000-b000-000000000171', '019c18d5-62e1-7000-9000-000000000002', 'fleet.compliance.update'),
        ('019c18d5-62e1-7000-b000-000000000172', '019c18d5-62e1-7000-9000-000000000002', 'fleet.compliance.delete'),
        ('019c18d5-62e1-7000-b000-000000000173', '019c18d5-62e1-7000-9000-000000000001', 'fleet.registration_transitions.update'),
        ('019c18d5-62e1-7000-b000-000000000174', '019c18d5-62e1-7000-9000-000000000001', 'fleet.registration_transitions.delete'),
        ('019c18d5-62e1-7000-b000-000000000175', '019c18d5-62e1-7000-9000-000000000001', 'fleet.registration_transitions.read'),
        ('019c18d5-62e1-7000-b000-000000000176', '019c18d5-62e1-7000-9000-000000000001', 'phone_sims.update'),
        ('019c18d5-62e1-7000-b000-000000000177', '019c18d5-62e1-7000-9000-000000000001', 'phone_sims.delete'),
        ('019c18d5-62e1-7000-b000-000000000178', '019c18d5-62e1-7000-9000-000000000002', 'phone_sims.update'),
        ('019c18d5-62e1-7000-b000-000000000179', '019c18d5-62e1-7000-9000-000000000002', 'phone_sims.delete'),
        ('019c18d5-62e1-7000-b000-000000000180', '019c18d5-62e1-7000-9000-000000000001', 'fuel.update'),
        ('019c18d5-62e1-7000-b000-000000000181', '019c18d5-62e1-7000-9000-000000000001', 'fuel.delete'),
        ('019c18d5-62e1-7000-b000-000000000182', '019c18d5-62e1-7000-9000-000000000002', 'fuel.update'),
        ('019c18d5-62e1-7000-b000-000000000183', '019c18d5-62e1-7000-9000-000000000002', 'fuel.delete'),
        ('019c18d5-62e1-7000-b000-000000000184', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.locations.update'),
        ('019c18d5-62e1-7000-b000-000000000185', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.locations.delete'),
        ('019c18d5-62e1-7000-b000-000000000186', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.external_jobs.update'),
        ('019c18d5-62e1-7000-b000-000000000187', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.external_jobs.delete'),
        ('019c18d5-62e1-7000-b000-000000000188', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.part_sales.update'),
        ('019c18d5-62e1-7000-b000-000000000189', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.part_sales.delete'),
        ('019c18d5-62e1-7000-b000-000000000190', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.part_sales.read'),
        ('019c18d5-62e1-7000-b000-000000000191', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.customer_labor_charges.update'),
        ('019c18d5-62e1-7000-b000-000000000192', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.customer_labor_charges.delete'),
        ('019c18d5-62e1-7000-b000-000000000193', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.customer_labor_charges.read'),
        ('019c18d5-62e1-7000-b000-000000000194', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.mechanic_labor_payments.update'),
        ('019c18d5-62e1-7000-b000-000000000195', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.mechanic_labor_payments.delete'),
        ('019c18d5-62e1-7000-b000-000000000196', '019c18d5-62e1-7000-9000-000000000001', 'maintenance.mechanic_labor_payments.read'),
        ('019c18d5-62e1-7000-b000-000000000197', '019c18d5-62e1-7000-9000-000000000001', 'inventory.items.update'),
        ('019c18d5-62e1-7000-b000-000000000198', '019c18d5-62e1-7000-9000-000000000001', 'inventory.items.delete'),
        ('019c18d5-62e1-7000-b000-000000000199', '019c18d5-62e1-7000-9000-000000000001', 'inventory.receipts.update'),
        ('019c18d5-62e1-7000-b000-000000000200', '019c18d5-62e1-7000-9000-000000000001', 'inventory.receipts.delete'),
        ('019c18d5-62e1-7000-b000-000000000201', '019c18d5-62e1-7000-9000-000000000001', 'inventory.receipts.read'),
        ('019c18d5-62e1-7000-b000-000000000202', '019c18d5-62e1-7000-9000-000000000001', 'inventory.returns.update'),
        ('019c18d5-62e1-7000-b000-000000000203', '019c18d5-62e1-7000-9000-000000000001', 'inventory.returns.delete'),
        ('019c18d5-62e1-7000-b000-000000000204', '019c18d5-62e1-7000-9000-000000000001', 'inventory.returns.read'),
        ('019c18d5-62e1-7000-b000-000000000205', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.external_jobs.update'),
        ('019c18d5-62e1-7000-b000-000000000206', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.external_jobs.delete'),
        ('019c18d5-62e1-7000-b000-000000000207', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.part_sales.update'),
        ('019c18d5-62e1-7000-b000-000000000208', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.part_sales.delete'),
        ('019c18d5-62e1-7000-b000-000000000209', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.part_sales.read'),
        ('019c18d5-62e1-7000-b000-000000000210', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.customer_labor_charges.update'),
        ('019c18d5-62e1-7000-b000-000000000211', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.customer_labor_charges.delete'),
        ('019c18d5-62e1-7000-b000-000000000212', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.customer_labor_charges.read'),
        ('019c18d5-62e1-7000-b000-000000000213', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.mechanic_labor_payments.update'),
        ('019c18d5-62e1-7000-b000-000000000214', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.mechanic_labor_payments.delete'),
        ('019c18d5-62e1-7000-b000-000000000215', '019c18d5-62e1-7000-9000-000000000002', 'maintenance.mechanic_labor_payments.read'),
        ('019c18d5-62e1-7000-b000-000000000216', '019c18d5-62e1-7000-9000-000000000002', 'inventory.items.update'),
        ('019c18d5-62e1-7000-b000-000000000217', '019c18d5-62e1-7000-9000-000000000002', 'inventory.items.delete'),
        ('019c18d5-62e1-7000-b000-000000000218', '019c18d5-62e1-7000-9000-000000000002', 'inventory.receipts.update'),
        ('019c18d5-62e1-7000-b000-000000000219', '019c18d5-62e1-7000-9000-000000000002', 'inventory.receipts.delete'),
        ('019c18d5-62e1-7000-b000-000000000220', '019c18d5-62e1-7000-9000-000000000002', 'inventory.receipts.read'),
        ('019c18d5-62e1-7000-b000-000000000221', '019c18d5-62e1-7000-9000-000000000002', 'inventory.returns.update'),
        ('019c18d5-62e1-7000-b000-000000000222', '019c18d5-62e1-7000-9000-000000000002', 'inventory.returns.delete'),
        ('019c18d5-62e1-7000-b000-000000000223', '019c18d5-62e1-7000-9000-000000000002', 'inventory.returns.read'),
        ('019c18d5-62e1-7000-b000-000000000224', '019c18d5-62e1-7000-9000-000000000001', 'legal_cases.update'),
        ('019c18d5-62e1-7000-b000-000000000225', '019c18d5-62e1-7000-9000-000000000001', 'legal_cases.delete'),
        ('019c18d5-62e1-7000-b000-000000000226', '019c18d5-62e1-7000-9000-000000000002', 'legal_cases.update'),
        ('019c18d5-62e1-7000-b000-000000000227', '019c18d5-62e1-7000-9000-000000000002', 'legal_cases.delete'),
        ('019c18d5-62e1-7000-b000-000000000228', '019c18d5-62e1-7000-9000-000000000001', 'jahez.handovers.update'),
        ('019c18d5-62e1-7000-b000-000000000229', '019c18d5-62e1-7000-9000-000000000001', 'jahez.handovers.delete'),
        ('019c18d5-62e1-7000-b000-000000000230', '019c18d5-62e1-7000-9000-000000000001', 'jahez.handovers.read'),
        ('019c18d5-62e1-7000-b000-000000000231', '019c18d5-62e1-7000-9000-000000000001', 'jahez.collections.update'),
        ('019c18d5-62e1-7000-b000-000000000232', '019c18d5-62e1-7000-9000-000000000001', 'jahez.collections.delete'),
        ('019c18d5-62e1-7000-b000-000000000233', '019c18d5-62e1-7000-9000-000000000001', 'jahez.collections.read'),
        ('019c18d5-62e1-7000-b000-000000000234', '019c18d5-62e1-7000-9000-000000000001', 'jahez.earnings.update'),
        ('019c18d5-62e1-7000-b000-000000000235', '019c18d5-62e1-7000-9000-000000000001', 'jahez.earnings.delete'),
        ('019c18d5-62e1-7000-b000-000000000236', '019c18d5-62e1-7000-9000-000000000001', 'jahez.earnings.read'),
        ('019c18d5-62e1-7000-b000-000000000237', '019c18d5-62e1-7000-9000-000000000001', 'jahez.imports.update'),
        ('019c18d5-62e1-7000-b000-000000000238', '019c18d5-62e1-7000-9000-000000000001', 'jahez.imports.delete'),
        ('019c18d5-62e1-7000-b000-000000000239', '019c18d5-62e1-7000-9000-000000000001', 'jahez.imports.read'),
        ('019c18d5-62e1-7000-b000-000000000240', '019c18d5-62e1-7000-9000-000000000001', 'jahez.adjustments.update'),
        ('019c18d5-62e1-7000-b000-000000000241', '019c18d5-62e1-7000-9000-000000000001', 'jahez.adjustments.delete'),
        ('019c18d5-62e1-7000-b000-000000000242', '019c18d5-62e1-7000-9000-000000000001', 'jahez.adjustments.read');

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
    SELECT COALESCE(seed.Id, NEWID()), source.RoleId, target.PermissionKey, source.CreatedAtUtc, source.CreatedByUserId, @now, 0
    FROM @roles source JOIN @map mapping ON mapping.LegacyKey = source.LegacyKey
    CROSS APPLY (VALUES (mapping.UpdateKey), (mapping.DeleteKey), (mapping.ReadKey)) target(PermissionKey)
    LEFT JOIN @seeds seed ON seed.RoleId = source.RoleId AND seed.PermissionKey = target.PermissionKey
    WHERE target.PermissionKey IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [identity].[RolePermissions] existing
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
        CROSS APPLY (VALUES (mapping.UpdateKey), (mapping.DeleteKey), (mapping.ReadKey), (mapping.LegacyKey)) target(PermissionKey)
        WHERE target.PermissionKey IS NOT NULL
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

    -- Compatibility grants are ignored by the new runtime but keep older API instances working.
    INSERT INTO [identity].[RolePermissions] (Id, RoleId, PermissionKey, CreatedAtUtc, CreatedByUserId, UpdatedAtUtc, IsDeleted)
    SELECT NEWID(), source.RoleId, source.LegacyKey, source.CreatedAtUtc, source.CreatedByUserId, @now, 0
    FROM @roles source
    WHERE NOT EXISTS (SELECT 1 FROM [identity].[RolePermissions] existing
        WHERE existing.RoleId = source.RoleId AND existing.PermissionKey = source.LegacyKey AND existing.IsDeleted = 0);

    -- Expand and deduplicate requested temporary access while keeping scopes and validity unchanged.
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
                (VALUES (mapping.CreateKey), (mapping.UpdateKey), (mapping.DeleteKey), (mapping.ReadKey), (mapping.LegacyKey)) actions(value)
                WHERE value IS NOT NULL) target
        ) keys
    ) expanded
    WHERE ISJSON(grantRow.RequestedPermissionsJson) = 1 AND EXISTS (
        SELECT 1 FROM OPENJSON(grantRow.RequestedPermissionsJson) original JOIN @map mapping ON mapping.LegacyKey = original.[value]);

    UPDATE userRow SET AuthorizationVersion = AuthorizationVersion + 1
    FROM [identity].[Users] userRow
    WHERE userRow.IsDeleted = 0 AND (
        EXISTS (SELECT 1 FROM [identity].[UserDirectPermissionAssignments] assignment JOIN @directIds original ON original.Id = assignment.Id
            WHERE assignment.UserId = userRow.Id)
        OR EXISTS (SELECT 1 FROM [identity].[UserRoleAssignments] assignment WHERE assignment.UserId = userRow.Id AND assignment.IsDeleted = 0
            AND (assignment.RoleId IN (SELECT RoleId FROM @roles)
                OR assignment.RoleId IN (SELECT RoleId FROM @seeds)))
        OR EXISTS (SELECT 1 FROM @supportUsers supportUser WHERE supportUser.UserId = userRow.Id)
    );

    IF (SELECT COUNT(*) FROM @copies) <> (SELECT SUM(CASE WHEN mapping.ReadKey IS NULL THEN 3 ELSE 4 END) FROM @directIds original JOIN @map mapping ON mapping.LegacyKey = original.LegacyKey)
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
    IF EXISTS (SELECT 1 FROM @roles source JOIN @map mapping ON mapping.LegacyKey = source.LegacyKey
        CROSS APPLY (VALUES(mapping.CreateKey),(mapping.UpdateKey),(mapping.DeleteKey),(mapping.ReadKey)) target(PermissionKey)
        WHERE target.PermissionKey IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [identity].[RolePermissions] grantRow
            WHERE grantRow.RoleId = source.RoleId AND grantRow.PermissionKey = target.PermissionKey AND grantRow.IsDeleted = 0))
        THROW 51000, 'Role permission split is incomplete.', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__IdentityMigrationsHistory]
    WHERE [MigrationId] = N'20261006074727_SplitManagementPermissionGrants'
)
BEGIN
    INSERT INTO [migration].[__IdentityMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006074727_SplitManagementPermissionGrants', N'10.0.11');
END;

COMMIT;
GO

