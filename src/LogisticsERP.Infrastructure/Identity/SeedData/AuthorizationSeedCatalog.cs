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
        PermissionKeys.Catalog.OperatingCitiesRead,
        PermissionKeys.Catalog.OperatingCitiesManage,
        PermissionKeys.Reporting.ReportsRead
    ];

    public static IReadOnlyList<string> ManagerPermissions { get; } =
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

    public static IReadOnlyList<RolePermissionSeed> RolePermissions { get; } =
        CreateRolePermissions();

    private static List<RolePermissionSeed> CreateRolePermissions()
    {
        var seeds = new List<RolePermissionSeed>(SystemAdminPermissions.Count + ManagerPermissions.Count);
        var sequence = 1;

        AddRolePermissions(seeds, SystemRoles.SystemAdminId, SystemAdminPermissions, ref sequence);
        AddRolePermissions(seeds, SystemRoles.ManagerId, ManagerPermissions, ref sequence);

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
