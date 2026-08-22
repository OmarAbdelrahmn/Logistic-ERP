namespace LogisticsERP.Application.Authorization;

public static class SystemRoles
{
    public const string SystemAdmin = "SYSTEM_ADMIN";
    public const string Manager = "MANAGER";
    public const string User = "USER";

    public static readonly Guid SystemAdminId = Guid.Parse("019c18d5-62e1-7000-9000-000000000001");
    public static readonly Guid ManagerId = Guid.Parse("019c18d5-62e1-7000-9000-000000000002");
    public static readonly Guid UserId = Guid.Parse("019c18d5-62e1-7000-9000-000000000003");

    public static bool IsProtected(Guid roleId) =>
        roleId == SystemAdminId || roleId == ManagerId || roleId == UserId;
}
