using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Ninja;

public static class NinjaAccountStatusRules
{
    public static bool IsManual(PlatformRiderAccountStatus status) =>
        status is PlatformRiderAccountStatus.Suspended or PlatformRiderAccountStatus.Retired or PlatformRiderAccountStatus.Archived;

    public static PlatformRiderAccountStatus AfterRelease(PlatformRiderAccountStatus status) =>
        IsManual(status) ? status : PlatformRiderAccountStatus.Available;
}
