using LogisticsERP.Application.Authorization;

namespace LogisticsERP.Infrastructure.Identity.SeedData;

internal static class AuthorizationSeedCatalog
{
    public static IReadOnlyList<string> OriginalSystemAdminPermissions { get; } =
    [
        PermissionKeys.Security.UsersRead,
        PermissionKeys.Security.UsersCreate,
        PermissionKeys.Security.UsersUpdate,
        PermissionKeys.Security.UsersArchive,
        PermissionKeys.Security.RolesRead,
        PermissionKeys.Security.RolesCreate,
        PermissionKeys.Security.PermissionsRead,
        PermissionKeys.Security.PermissionsCreate,
        PermissionKeys.Security.AuditRead,
        PermissionKeys.Security.SupportAccessCreate,
        PermissionKeys.Catalog.CompanyProfileRead,
        PermissionKeys.Catalog.CompanyProfileCreate,
        PermissionKeys.Catalog.OperatingCitiesRead,
        PermissionKeys.Catalog.OperatingCitiesCreate,
        PermissionKeys.Catalog.TagsRead,
        PermissionKeys.Catalog.TagsCreate,
        PermissionKeys.Documents.CatalogCreate,
        PermissionKeys.Operations.PlatformCredentialsRead,
        PermissionKeys.Operations.PlatformCredentialsRotate,
        PermissionKeys.Reporting.ReportsRead,
        PermissionKeys.Fleet.VehiclesRead,
        PermissionKeys.Fleet.VehiclesCreate,
        PermissionKeys.Fleet.VehiclesArchive,
        PermissionKeys.Fleet.VehiclesDecommission,
        PermissionKeys.Fleet.AssignmentsRead,
        PermissionKeys.Fleet.AssignmentsCreate,
        PermissionKeys.Fleet.AssignmentsCorrect,
        PermissionKeys.Fleet.IssuesRead,
        PermissionKeys.Fleet.IssuesCreate,
        PermissionKeys.Fleet.ComplianceRead,
        PermissionKeys.Fleet.ComplianceCreate,
        PermissionKeys.Fleet.FilesRead,
        PermissionKeys.Fleet.FilesUpload,
        PermissionKeys.Fleet.FilesDownload,
        PermissionKeys.Fleet.AccidentsRead,
        PermissionKeys.Fleet.AccidentsReport,
        PermissionKeys.Fleet.AccidentsFinalize,
        PermissionKeys.Fleet.AccidentsDownload,
        PermissionKeys.Fleet.CorrectionsCreate,
        PermissionKeys.Fleet.RegistrationTransitionsCreate,
        PermissionKeys.Operations.PhoneSimsRead,
        PermissionKeys.Operations.PhoneSimsCreate,
        PermissionKeys.Fuel.Read,
        PermissionKeys.Fuel.Create,
        PermissionKeys.Fuel.Import,
        PermissionKeys.Maintenance.LocationsRead,
        PermissionKeys.Maintenance.LocationsCreate,
        PermissionKeys.Maintenance.WorkOrdersRead,
        PermissionKeys.Maintenance.WorkOrdersCreate,
        PermissionKeys.Maintenance.WorkOrdersUpdate,
        PermissionKeys.Maintenance.WorkOrdersDelete,
        PermissionKeys.Maintenance.OilRead,
        PermissionKeys.Maintenance.OilComplete,
        PermissionKeys.Maintenance.ExternalJobsRead,
        PermissionKeys.Maintenance.ExternalJobsCreate,
        PermissionKeys.Maintenance.PartSalesCreate,
        PermissionKeys.Maintenance.CustomerLaborChargesCreate,
        PermissionKeys.Maintenance.MechanicLaborPaymentsCreate,
        PermissionKeys.Maintenance.ProfitReportsRead,
        PermissionKeys.Maintenance.ProfitReportsExport,
        PermissionKeys.Inventory.ItemsRead,
        PermissionKeys.Inventory.ItemsCreate,
        PermissionKeys.Inventory.StockRead,
        PermissionKeys.Inventory.StockMove,
        PermissionKeys.Inventory.StockAdjust,
        PermissionKeys.Inventory.CostLayersRead,
        PermissionKeys.Inventory.ReceiptsCreate,
        PermissionKeys.Inventory.ReturnsCreate,
        PermissionKeys.Workflows.LegalCasesRead,
        PermissionKeys.Workflows.LegalCasesCreate,
        PermissionKeys.Workflows.LegalCaseFilesDownload,
        PermissionKeys.Workforce.ExternalRidersRead,
        PermissionKeys.Workforce.ExternalRidersCreate,
        PermissionKeys.Workforce.ExternalRidersUpdate,
        PermissionKeys.Workforce.ExternalRidersDelete,
        PermissionKeys.Jahez.Read,
        PermissionKeys.Jahez.HandoversCreate,
        PermissionKeys.Jahez.CollectionsCreate,
        PermissionKeys.Jahez.RequestsCreate,
        PermissionKeys.Jahez.RequestsApprove,
        PermissionKeys.Jahez.ResetsApprove,
        PermissionKeys.Jahez.EarningsCreate,
        PermissionKeys.Jahez.ImportsCreate,
        PermissionKeys.Jahez.AdjustmentsCreate,
        PermissionKeys.Jahez.CashboxRead,
        PermissionKeys.Jahez.CashboxSubmit,
        PermissionKeys.Jahez.CashboxConfirm,
        PermissionKeys.Jahez.CashboxApprove,
    ];

    public static IReadOnlyList<string> OriginalManagerPermissions { get; } =
    [
        PermissionKeys.Catalog.OperatingCitiesRead,
        PermissionKeys.Catalog.TagsRead,
        PermissionKeys.Workforce.EmployeesRead,
        PermissionKeys.Workforce.RidersRead,
        PermissionKeys.Operations.PlatformAccountsRead,
        PermissionKeys.Operations.PlatformAssignmentsRead,
        PermissionKeys.Operations.HousingRead,
        PermissionKeys.Reporting.ReportsRead,
        PermissionKeys.Reporting.NotificationsRead,
        PermissionKeys.Fleet.VehiclesRead,
        PermissionKeys.Fleet.VehiclesCreate,
        PermissionKeys.Fleet.AssignmentsRead,
        PermissionKeys.Fleet.AssignmentsCreate,
        PermissionKeys.Fleet.IssuesRead,
        PermissionKeys.Fleet.IssuesCreate,
        PermissionKeys.Fleet.ComplianceRead,
        PermissionKeys.Fleet.ComplianceCreate,
        PermissionKeys.Fleet.FilesRead,
        PermissionKeys.Fleet.FilesUpload,
        PermissionKeys.Fleet.FilesDownload,
        PermissionKeys.Fleet.AccidentsRead,
        PermissionKeys.Fleet.AccidentsReport,
        PermissionKeys.Fleet.AccidentsDownload,
        PermissionKeys.Operations.PhoneSimsRead,
        PermissionKeys.Operations.PhoneSimsCreate,
        PermissionKeys.Fuel.Read,
        PermissionKeys.Fuel.Create,
        PermissionKeys.Fuel.Import,
        PermissionKeys.Maintenance.LocationsRead,
        PermissionKeys.Maintenance.WorkOrdersRead,
        PermissionKeys.Maintenance.WorkOrdersCreate,
        PermissionKeys.Maintenance.WorkOrdersUpdate,
        PermissionKeys.Maintenance.WorkOrdersDelete,
        PermissionKeys.Maintenance.OilRead,
        PermissionKeys.Maintenance.OilComplete,
        PermissionKeys.Maintenance.ExternalJobsRead,
        PermissionKeys.Maintenance.ExternalJobsCreate,
        PermissionKeys.Maintenance.PartSalesCreate,
        PermissionKeys.Maintenance.CustomerLaborChargesCreate,
        PermissionKeys.Maintenance.MechanicLaborPaymentsCreate,
        PermissionKeys.Inventory.ItemsRead,
        PermissionKeys.Inventory.ItemsCreate,
        PermissionKeys.Inventory.StockRead,
        PermissionKeys.Inventory.StockMove,
        PermissionKeys.Inventory.ReceiptsCreate,
        PermissionKeys.Inventory.ReturnsCreate,
        PermissionKeys.Workflows.LegalCasesRead,
        PermissionKeys.Workflows.LegalCasesCreate,
        PermissionKeys.Workflows.LegalCaseFilesDownload
    ];

    private static string[] ExpandPermissions(IEnumerable<string> permissions) =>
        permissions.SelectMany(key => SplitActions.TryGetValue(key, out var actions) ? actions : new[] { key }).Distinct(StringComparer.Ordinal).ToArray();

    private static readonly Dictionary<string, string[]> SplitActions = new Dictionary<string, string[]>
    {
        [PermissionKeys.Security.RolesCreate] = [PermissionKeys.Security.RolesCreate, PermissionKeys.Security.RolesUpdate, PermissionKeys.Security.RolesDelete, PermissionKeys.Security.RolesRead],
        [PermissionKeys.Security.PermissionsCreate] = [PermissionKeys.Security.PermissionsCreate, PermissionKeys.Security.PermissionsUpdate, PermissionKeys.Security.PermissionsDelete, PermissionKeys.Security.PermissionsRead],
        [PermissionKeys.Security.SupportAccessCreate] = [PermissionKeys.Security.SupportAccessCreate, PermissionKeys.Security.SupportAccessUpdate, PermissionKeys.Security.SupportAccessDelete, PermissionKeys.Security.SupportAccessRead],
        [PermissionKeys.Catalog.CompanyProfileCreate] = [PermissionKeys.Catalog.CompanyProfileCreate, PermissionKeys.Catalog.CompanyProfileUpdate, PermissionKeys.Catalog.CompanyProfileDelete, PermissionKeys.Catalog.CompanyProfileRead],
        [PermissionKeys.Catalog.OperatingCitiesCreate] = [PermissionKeys.Catalog.OperatingCitiesCreate, PermissionKeys.Catalog.OperatingCitiesUpdate, PermissionKeys.Catalog.OperatingCitiesDelete, PermissionKeys.Catalog.OperatingCitiesRead],
        [PermissionKeys.Catalog.TagsCreate] = [PermissionKeys.Catalog.TagsCreate, PermissionKeys.Catalog.TagsUpdate, PermissionKeys.Catalog.TagsDelete, PermissionKeys.Catalog.TagsRead],
        [PermissionKeys.Workforce.RidersCreate] = [PermissionKeys.Workforce.RidersCreate, PermissionKeys.Workforce.RidersUpdate, PermissionKeys.Workforce.RidersDelete, PermissionKeys.Workforce.RidersRead],
        [PermissionKeys.Workforce.SponsorsCreate] = [PermissionKeys.Workforce.SponsorsCreate, PermissionKeys.Workforce.SponsorsUpdate, PermissionKeys.Workforce.SponsorsDelete, PermissionKeys.Workforce.SponsorsRead],
        [PermissionKeys.Compliance.ResidencyCreate] = [PermissionKeys.Compliance.ResidencyCreate, PermissionKeys.Compliance.ResidencyUpdate, PermissionKeys.Compliance.ResidencyDelete, PermissionKeys.Compliance.ResidencyRead],
        [PermissionKeys.Compliance.LicensesCreate] = [PermissionKeys.Compliance.LicensesCreate, PermissionKeys.Compliance.LicensesUpdate, PermissionKeys.Compliance.LicensesDelete, PermissionKeys.Compliance.LicensesRead],
        [PermissionKeys.Compliance.RiderCardsCreate] = [PermissionKeys.Compliance.RiderCardsCreate, PermissionKeys.Compliance.RiderCardsUpdate, PermissionKeys.Compliance.RiderCardsDelete, PermissionKeys.Compliance.RiderCardsRead],
        [PermissionKeys.Compliance.HealthCardsCreate] = [PermissionKeys.Compliance.HealthCardsCreate, PermissionKeys.Compliance.HealthCardsUpdate, PermissionKeys.Compliance.HealthCardsDelete, PermissionKeys.Compliance.HealthCardsRead],
        [PermissionKeys.Compliance.InsuranceCreate] = [PermissionKeys.Compliance.InsuranceCreate, PermissionKeys.Compliance.InsuranceUpdate, PermissionKeys.Compliance.InsuranceDelete, PermissionKeys.Compliance.InsuranceRead],
        [PermissionKeys.Compliance.PromissoryNotesCreate] = [PermissionKeys.Compliance.PromissoryNotesCreate, PermissionKeys.Compliance.PromissoryNotesUpdate, PermissionKeys.Compliance.PromissoryNotesDelete, PermissionKeys.Compliance.PromissoryNotesRead],
        [PermissionKeys.Documents.CatalogCreate] = [PermissionKeys.Documents.CatalogCreate, PermissionKeys.Documents.CatalogUpdate, PermissionKeys.Documents.CatalogDelete, PermissionKeys.Documents.CatalogRead],
        [PermissionKeys.Operations.PlatformAccountsCreate] = [PermissionKeys.Operations.PlatformAccountsCreate, PermissionKeys.Operations.PlatformAccountsUpdate, PermissionKeys.Operations.PlatformAccountsDelete, PermissionKeys.Operations.PlatformAccountsRead],
        [PermissionKeys.Operations.PlatformAssignmentsCreate] = [PermissionKeys.Operations.PlatformAssignmentsCreate, PermissionKeys.Operations.PlatformAssignmentsUpdate, PermissionKeys.Operations.PlatformAssignmentsDelete, PermissionKeys.Operations.PlatformAssignmentsRead],
        [PermissionKeys.Operations.HousingCreate] = [PermissionKeys.Operations.HousingCreate, PermissionKeys.Operations.HousingUpdate, PermissionKeys.Operations.HousingDelete, PermissionKeys.Operations.HousingRead],
        [PermissionKeys.Operations.PhoneSimsCreate] = [PermissionKeys.Operations.PhoneSimsCreate, PermissionKeys.Operations.PhoneSimsUpdate, PermissionKeys.Operations.PhoneSimsDelete, PermissionKeys.Operations.PhoneSimsRead],
        [PermissionKeys.Reporting.NotificationsCreate] = [PermissionKeys.Reporting.NotificationsCreate, PermissionKeys.Reporting.NotificationsUpdate, PermissionKeys.Reporting.NotificationsDelete, PermissionKeys.Reporting.NotificationsRead],
        [PermissionKeys.Fleet.VehiclesCreate] = [PermissionKeys.Fleet.VehiclesCreate, PermissionKeys.Fleet.VehiclesUpdate, PermissionKeys.Fleet.VehiclesDelete, PermissionKeys.Fleet.VehiclesRead],
        [PermissionKeys.Fleet.AssignmentsCreate] = [PermissionKeys.Fleet.AssignmentsCreate, PermissionKeys.Fleet.AssignmentsUpdate, PermissionKeys.Fleet.AssignmentsDelete, PermissionKeys.Fleet.AssignmentsRead],
        [PermissionKeys.Fleet.IssuesCreate] = [PermissionKeys.Fleet.IssuesCreate, PermissionKeys.Fleet.IssuesUpdate, PermissionKeys.Fleet.IssuesDelete, PermissionKeys.Fleet.IssuesRead],
        [PermissionKeys.Fleet.ComplianceCreate] = [PermissionKeys.Fleet.ComplianceCreate, PermissionKeys.Fleet.ComplianceUpdate, PermissionKeys.Fleet.ComplianceDelete, PermissionKeys.Fleet.ComplianceRead],
        [PermissionKeys.Fleet.CorrectionsCreate] = [PermissionKeys.Fleet.CorrectionsCreate, PermissionKeys.Fleet.CorrectionsUpdate, PermissionKeys.Fleet.CorrectionsDelete, PermissionKeys.Fleet.CorrectionsRead],
        [PermissionKeys.Fleet.RegistrationTransitionsCreate] = [PermissionKeys.Fleet.RegistrationTransitionsCreate, PermissionKeys.Fleet.RegistrationTransitionsUpdate, PermissionKeys.Fleet.RegistrationTransitionsDelete, PermissionKeys.Fleet.RegistrationTransitionsRead],
        [PermissionKeys.Fleet.DailyDistancesCreate] = [PermissionKeys.Fleet.DailyDistancesCreate, PermissionKeys.Fleet.DailyDistancesUpdate, PermissionKeys.Fleet.DailyDistancesDelete, PermissionKeys.Fleet.DailyDistancesRead],
        [PermissionKeys.Jahez.HandoversCreate] = [PermissionKeys.Jahez.HandoversCreate, PermissionKeys.Jahez.HandoversUpdate, PermissionKeys.Jahez.HandoversDelete, PermissionKeys.Jahez.HandoversRead],
        [PermissionKeys.Jahez.CollectionsCreate] = [PermissionKeys.Jahez.CollectionsCreate, PermissionKeys.Jahez.CollectionsUpdate, PermissionKeys.Jahez.CollectionsDelete, PermissionKeys.Jahez.CollectionsRead],
        [PermissionKeys.Jahez.EarningsCreate] = [PermissionKeys.Jahez.EarningsCreate, PermissionKeys.Jahez.EarningsUpdate, PermissionKeys.Jahez.EarningsDelete, PermissionKeys.Jahez.EarningsRead],
        [PermissionKeys.Jahez.ImportsCreate] = [PermissionKeys.Jahez.ImportsCreate, PermissionKeys.Jahez.ImportsUpdate, PermissionKeys.Jahez.ImportsDelete, PermissionKeys.Jahez.ImportsRead],
        [PermissionKeys.Jahez.AdjustmentsCreate] = [PermissionKeys.Jahez.AdjustmentsCreate, PermissionKeys.Jahez.AdjustmentsUpdate, PermissionKeys.Jahez.AdjustmentsDelete, PermissionKeys.Jahez.AdjustmentsRead],
        [PermissionKeys.Fuel.Create] = [PermissionKeys.Fuel.Create, PermissionKeys.Fuel.Update, PermissionKeys.Fuel.Delete, PermissionKeys.Fuel.Read],
        [PermissionKeys.Maintenance.LocationsCreate] = [PermissionKeys.Maintenance.LocationsCreate, PermissionKeys.Maintenance.LocationsUpdate, PermissionKeys.Maintenance.LocationsDelete, PermissionKeys.Maintenance.LocationsRead],
        [PermissionKeys.Maintenance.ExternalJobsCreate] = [PermissionKeys.Maintenance.ExternalJobsCreate, PermissionKeys.Maintenance.ExternalJobsUpdate, PermissionKeys.Maintenance.ExternalJobsDelete, PermissionKeys.Maintenance.ExternalJobsRead],
        [PermissionKeys.Maintenance.PartSalesCreate] = [PermissionKeys.Maintenance.PartSalesCreate, PermissionKeys.Maintenance.PartSalesUpdate, PermissionKeys.Maintenance.PartSalesDelete, PermissionKeys.Maintenance.PartSalesRead],
        [PermissionKeys.Maintenance.CustomerLaborChargesCreate] = [PermissionKeys.Maintenance.CustomerLaborChargesCreate, PermissionKeys.Maintenance.CustomerLaborChargesUpdate, PermissionKeys.Maintenance.CustomerLaborChargesDelete, PermissionKeys.Maintenance.CustomerLaborChargesRead],
        [PermissionKeys.Maintenance.MechanicLaborPaymentsCreate] = [PermissionKeys.Maintenance.MechanicLaborPaymentsCreate, PermissionKeys.Maintenance.MechanicLaborPaymentsUpdate, PermissionKeys.Maintenance.MechanicLaborPaymentsDelete, PermissionKeys.Maintenance.MechanicLaborPaymentsRead],
        [PermissionKeys.Inventory.ItemsCreate] = [PermissionKeys.Inventory.ItemsCreate, PermissionKeys.Inventory.ItemsUpdate, PermissionKeys.Inventory.ItemsDelete, PermissionKeys.Inventory.ItemsRead],
        [PermissionKeys.Inventory.ReceiptsCreate] = [PermissionKeys.Inventory.ReceiptsCreate, PermissionKeys.Inventory.ReceiptsUpdate, PermissionKeys.Inventory.ReceiptsDelete, PermissionKeys.Inventory.ReceiptsRead],
        [PermissionKeys.Inventory.ReturnsCreate] = [PermissionKeys.Inventory.ReturnsCreate, PermissionKeys.Inventory.ReturnsUpdate, PermissionKeys.Inventory.ReturnsDelete, PermissionKeys.Inventory.ReturnsRead],
        [PermissionKeys.Workflows.LeaveRequestsCreate] = [PermissionKeys.Workflows.LeaveRequestsCreate, PermissionKeys.Workflows.LeaveRequestsUpdate, PermissionKeys.Workflows.LeaveRequestsDelete, PermissionKeys.Workflows.LeaveRequestsRead],
        [PermissionKeys.Workflows.AbsenceCasesCreate] = [PermissionKeys.Workflows.AbsenceCasesCreate, PermissionKeys.Workflows.AbsenceCasesUpdate, PermissionKeys.Workflows.AbsenceCasesDelete, PermissionKeys.Workflows.AbsenceCasesRead],
        [PermissionKeys.Workflows.EmployeeStatusChangesCreate] = [PermissionKeys.Workflows.EmployeeStatusChangesCreate, PermissionKeys.Workflows.EmployeeStatusChangesUpdate, PermissionKeys.Workflows.EmployeeStatusChangesDelete, PermissionKeys.Workflows.EmployeeStatusChangesRead],
        [PermissionKeys.Workflows.LegalCasesCreate] = [PermissionKeys.Workflows.LegalCasesCreate, PermissionKeys.Workflows.LegalCasesUpdate, PermissionKeys.Workflows.LegalCasesDelete, PermissionKeys.Workflows.LegalCasesRead],
        [PermissionKeys.HrForms.TemplatesCreate] = [PermissionKeys.HrForms.TemplatesCreate, PermissionKeys.HrForms.TemplatesUpdate, PermissionKeys.HrForms.TemplatesDelete, PermissionKeys.HrForms.TemplatesRead],
    };

    public static IReadOnlyList<string> SystemAdminPermissions { get; } = ExpandPermissions(OriginalSystemAdminPermissions);
    public static IReadOnlyList<string> ManagerPermissions { get; } = ExpandPermissions(OriginalManagerPermissions);

    public static IReadOnlyList<RolePermissionSeed> RolePermissions { get; } =
        CreateRolePermissions();

    private static List<RolePermissionSeed> CreateRolePermissions()
    {
        var seeds = new List<RolePermissionSeed>(OriginalSystemAdminPermissions.Count + OriginalManagerPermissions.Count);
        var sequence = 1;

        string[] legacyOriginalSystemAdminPermissions =
        [
            PermissionKeys.Security.UsersRead,
            PermissionKeys.Security.UsersCreate,
            PermissionKeys.Security.UsersUpdate,
            PermissionKeys.Security.UsersArchive,
            PermissionKeys.Security.RolesRead,
            PermissionKeys.Security.RolesCreate,
            PermissionKeys.Security.PermissionsRead,
            PermissionKeys.Security.PermissionsCreate,
            PermissionKeys.Security.AuditRead,
            PermissionKeys.Security.SupportAccessCreate,
            PermissionKeys.Catalog.OperatingCitiesRead,
            PermissionKeys.Catalog.OperatingCitiesCreate,
            PermissionKeys.Reporting.ReportsRead
        ];
        string[] legacyOriginalManagerPermissions =
        [
            PermissionKeys.Catalog.OperatingCitiesRead,
            PermissionKeys.Workforce.EmployeesRead,
            PermissionKeys.Workforce.RidersRead,
            PermissionKeys.Operations.PlatformAccountsRead,
            PermissionKeys.Operations.PlatformAssignmentsRead,
            PermissionKeys.Operations.HousingRead,
            PermissionKeys.Reporting.ReportsRead,
            PermissionKeys.Reporting.NotificationsRead
        ];

        AddRolePermissions(seeds, SystemRoles.SystemAdminId, legacyOriginalSystemAdminPermissions, ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId, legacyOriginalManagerPermissions, ref sequence);
        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            OriginalSystemAdminPermissions.Except(legacyOriginalSystemAdminPermissions).Where(key =>
                !key.StartsWith("fleet.", StringComparison.Ordinal)
                && !key.StartsWith("phone_sims.", StringComparison.Ordinal)
                && !key.StartsWith("fuel.", StringComparison.Ordinal)
                && !key.StartsWith("maintenance.", StringComparison.Ordinal)
                && !key.StartsWith("inventory.", StringComparison.Ordinal)
                && !key.StartsWith("legal_cases.", StringComparison.Ordinal)
                && !key.StartsWith("external_riders.", StringComparison.Ordinal)
                && !key.StartsWith("jahez.", StringComparison.Ordinal)), ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId,
            OriginalManagerPermissions.Except(legacyOriginalManagerPermissions).Where(key =>
                !key.StartsWith("fleet.", StringComparison.Ordinal)
                && !key.StartsWith("phone_sims.", StringComparison.Ordinal)
                && !key.StartsWith("fuel.", StringComparison.Ordinal)
                && !key.StartsWith("maintenance.", StringComparison.Ordinal)
                && !key.StartsWith("inventory.", StringComparison.Ordinal)
                && !key.StartsWith("legal_cases.", StringComparison.Ordinal)), ref sequence);
        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            OriginalSystemAdminPermissions.Where(key => key.StartsWith("fleet.", StringComparison.Ordinal) && key != PermissionKeys.Fleet.RegistrationTransitionsCreate), ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId,
            OriginalManagerPermissions.Where(key => key.StartsWith("fleet.", StringComparison.Ordinal)), ref sequence);
        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            [PermissionKeys.Fleet.RegistrationTransitionsCreate], ref sequence);
        // New grants are appended explicitly so existing sequence-derived seed IDs remain stable.
        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            [PermissionKeys.Operations.PhoneSimsRead, PermissionKeys.Operations.PhoneSimsCreate], ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId,
            [PermissionKeys.Operations.PhoneSimsRead, PermissionKeys.Operations.PhoneSimsCreate], ref sequence);
        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            [PermissionKeys.Fuel.Read, PermissionKeys.Fuel.Create, PermissionKeys.Fuel.Import], ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId,
            [PermissionKeys.Fuel.Read, PermissionKeys.Fuel.Create, PermissionKeys.Fuel.Import], ref sequence);
        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            OriginalSystemAdminPermissions.Where(key => (key.StartsWith("maintenance.", StringComparison.Ordinal) || key.StartsWith("inventory.", StringComparison.Ordinal))
                && key != PermissionKeys.Maintenance.WorkOrdersUpdate && key != PermissionKeys.Maintenance.WorkOrdersDelete), ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId,
            OriginalManagerPermissions.Where(key => (key.StartsWith("maintenance.", StringComparison.Ordinal) || key.StartsWith("inventory.", StringComparison.Ordinal))
                && key != PermissionKeys.Maintenance.WorkOrdersUpdate && key != PermissionKeys.Maintenance.WorkOrdersDelete), ref sequence);
        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            [PermissionKeys.Workflows.LegalCasesRead, PermissionKeys.Workflows.LegalCasesCreate, PermissionKeys.Workflows.LegalCaseFilesDownload], ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId,
            [PermissionKeys.Workflows.LegalCasesRead, PermissionKeys.Workflows.LegalCasesCreate, PermissionKeys.Workflows.LegalCaseFilesDownload], ref sequence);

        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            [PermissionKeys.Maintenance.WorkOrdersUpdate, PermissionKeys.Maintenance.WorkOrdersDelete,
                PermissionKeys.Workforce.ExternalRidersRead, PermissionKeys.Workforce.ExternalRidersCreate,
                PermissionKeys.Workforce.ExternalRidersUpdate, PermissionKeys.Workforce.ExternalRidersDelete], ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId,
            [PermissionKeys.Maintenance.WorkOrdersUpdate, PermissionKeys.Maintenance.WorkOrdersDelete], ref sequence);

        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            OriginalSystemAdminPermissions.Where(key => key.StartsWith("jahez.", StringComparison.Ordinal)), ref sequence);

        foreach (var grant in seeds.ToArray())
        {
            if (SplitActions.TryGetValue(grant.PermissionKey, out var actions))
                AddRolePermissions(seeds, grant.RoleId, actions.Skip(1).Where(key => !seeds.Any(existing => existing.RoleId == grant.RoleId && existing.PermissionKey == key)), ref sequence);
        }
        return seeds;
    }

    private static void AddRolePermissions(
        List<RolePermissionSeed> seeds,
        Guid roleId,
        IEnumerable<string> permissionKeys,
        ref int sequence)
    {
        foreach (var permissionKey in permissionKeys)
        {
            seeds.Add(new RolePermissionSeed(CreateGrantId(sequence++), roleId, permissionKey));
        }
    }

    private static Guid CreateGrantId(int sequence) =>
        Guid.Parse($"019c18d5-62e1-7000-b000-{sequence:D12}");
}

internal sealed record RolePermissionSeed(Guid Id, Guid RoleId, string PermissionKey);
