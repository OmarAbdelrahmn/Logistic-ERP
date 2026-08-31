using LogisticsERP.Application.Authorization;

namespace LogisticsERP.Infrastructure.Identity.SeedData;

internal static class AuthorizationSeedCatalog
{
    public static IReadOnlyList<string> SystemAdminPermissions { get; } =
    [
        PermissionKeys.Security.UsersRead,
        PermissionKeys.Security.UsersCreate,
        PermissionKeys.Security.UsersUpdate,
        PermissionKeys.Security.UsersArchive,
        PermissionKeys.Security.RolesRead,
        PermissionKeys.Security.RolesManage,
        PermissionKeys.Security.PermissionsRead,
        PermissionKeys.Security.PermissionsManage,
        PermissionKeys.Security.AuditRead,
        PermissionKeys.Security.SupportAccessManage,
        PermissionKeys.Catalog.CompanyProfileRead,
        PermissionKeys.Catalog.CompanyProfileManage,
        PermissionKeys.Catalog.OperatingCitiesRead,
        PermissionKeys.Catalog.OperatingCitiesManage,
        PermissionKeys.Catalog.TagsRead,
        PermissionKeys.Catalog.TagsManage,
        PermissionKeys.Documents.CatalogManage,
        PermissionKeys.Operations.PlatformCredentialsRead,
        PermissionKeys.Operations.PlatformCredentialsRotate,
        PermissionKeys.Reporting.ReportsRead,
        PermissionKeys.Fleet.VehiclesRead,
        PermissionKeys.Fleet.VehiclesManage,
        PermissionKeys.Fleet.VehiclesArchive,
        PermissionKeys.Fleet.VehiclesDecommission,
        PermissionKeys.Fleet.AssignmentsRead,
        PermissionKeys.Fleet.AssignmentsManage,
        PermissionKeys.Fleet.AssignmentsCorrect,
        PermissionKeys.Fleet.IssuesRead,
        PermissionKeys.Fleet.IssuesManage,
        PermissionKeys.Fleet.ComplianceRead,
        PermissionKeys.Fleet.ComplianceManage,
        PermissionKeys.Fleet.FilesRead,
        PermissionKeys.Fleet.FilesUpload,
        PermissionKeys.Fleet.FilesDownload,
        PermissionKeys.Fleet.AccidentsRead,
        PermissionKeys.Fleet.AccidentsReport,
        PermissionKeys.Fleet.AccidentsFinalize,
        PermissionKeys.Fleet.AccidentsDownload,
        PermissionKeys.Fleet.CorrectionsManage,
        PermissionKeys.Fleet.RegistrationTransitionsManage,
        PermissionKeys.Operations.PhoneSimsRead,
        PermissionKeys.Operations.PhoneSimsManage
    ];

    public static IReadOnlyList<string> ManagerPermissions { get; } =
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
        PermissionKeys.Fleet.VehiclesManage,
        PermissionKeys.Fleet.AssignmentsRead,
        PermissionKeys.Fleet.AssignmentsManage,
        PermissionKeys.Fleet.IssuesRead,
        PermissionKeys.Fleet.IssuesManage,
        PermissionKeys.Fleet.ComplianceRead,
        PermissionKeys.Fleet.ComplianceManage,
        PermissionKeys.Fleet.FilesRead,
        PermissionKeys.Fleet.FilesUpload,
        PermissionKeys.Fleet.FilesDownload,
        PermissionKeys.Fleet.AccidentsRead,
        PermissionKeys.Fleet.AccidentsReport,
        PermissionKeys.Fleet.AccidentsDownload,
        PermissionKeys.Operations.PhoneSimsRead,
        PermissionKeys.Operations.PhoneSimsManage
    ];

    public static IReadOnlyList<RolePermissionSeed> RolePermissions { get; } =
        CreateRolePermissions();

    private static List<RolePermissionSeed> CreateRolePermissions()
    {
        var seeds = new List<RolePermissionSeed>(SystemAdminPermissions.Count + ManagerPermissions.Count);
        var sequence = 1;

        string[] legacySystemAdminPermissions =
        [
            PermissionKeys.Security.UsersRead,
            PermissionKeys.Security.UsersCreate,
            PermissionKeys.Security.UsersUpdate,
            PermissionKeys.Security.UsersArchive,
            PermissionKeys.Security.RolesRead,
            PermissionKeys.Security.RolesManage,
            PermissionKeys.Security.PermissionsRead,
            PermissionKeys.Security.PermissionsManage,
            PermissionKeys.Security.AuditRead,
            PermissionKeys.Security.SupportAccessManage,
            PermissionKeys.Catalog.OperatingCitiesRead,
            PermissionKeys.Catalog.OperatingCitiesManage,
            PermissionKeys.Reporting.ReportsRead
        ];
        string[] legacyManagerPermissions =
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

        AddRolePermissions(seeds, SystemRoles.SystemAdminId, legacySystemAdminPermissions, ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId, legacyManagerPermissions, ref sequence);
        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            SystemAdminPermissions.Except(legacySystemAdminPermissions).Where(key =>
                !key.StartsWith("fleet.", StringComparison.Ordinal)
                && !key.StartsWith("phone_sims.", StringComparison.Ordinal)), ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId,
            ManagerPermissions.Except(legacyManagerPermissions).Where(key =>
                !key.StartsWith("fleet.", StringComparison.Ordinal)
                && !key.StartsWith("phone_sims.", StringComparison.Ordinal)), ref sequence);
        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            SystemAdminPermissions.Where(key => key.StartsWith("fleet.", StringComparison.Ordinal) && key != PermissionKeys.Fleet.RegistrationTransitionsManage), ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId,
            ManagerPermissions.Where(key => key.StartsWith("fleet.", StringComparison.Ordinal)), ref sequence);
        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            [PermissionKeys.Fleet.RegistrationTransitionsManage], ref sequence);
        // New grants are appended explicitly so existing sequence-derived seed IDs remain stable.
        AddRolePermissions(seeds, SystemRoles.SystemAdminId,
            [PermissionKeys.Operations.PhoneSimsRead, PermissionKeys.Operations.PhoneSimsManage], ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId,
            [PermissionKeys.Operations.PhoneSimsRead, PermissionKeys.Operations.PhoneSimsManage], ref sequence);

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
