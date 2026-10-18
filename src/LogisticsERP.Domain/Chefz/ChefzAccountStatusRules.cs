using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Chefz;

public static class ChefzAccountStatusRules
{
    public static bool IsManual(PlatformRiderAccountStatus status) =>
        status is PlatformRiderAccountStatus.Suspended or PlatformRiderAccountStatus.Retired or PlatformRiderAccountStatus.Archived;

    public static PlatformRiderAccountStatus Active(bool hasAssignment) =>
        hasAssignment ? PlatformRiderAccountStatus.Assigned : PlatformRiderAccountStatus.Available;

    public static PlatformRiderAccountStatus AfterRelease(PlatformRiderAccountStatus status) =>
        IsManual(status) ? status : PlatformRiderAccountStatus.Available;
}
