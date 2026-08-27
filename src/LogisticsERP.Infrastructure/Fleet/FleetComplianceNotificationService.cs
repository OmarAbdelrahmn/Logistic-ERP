using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed class FleetComplianceNotificationService(
    ApplicationDbContext dbContext,
    IdentityDbContext identityDbContext,
    IPermissionChecker permissionChecker,
    TimeProvider timeProvider) : IFleetComplianceNotificationService
{
    private static readonly int[] ReminderDays = [30, 7, 1, 0];

    public async Task RunDueNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var checkDate = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(3)).DateTime);
        var users = await identityDbContext.Users.AsNoTracking()
            .Where(x => x.Status == UserAccountStatus.Active && !x.IsDevelopmentOnly)
            .Select(x => new NotificationUser(x.Id, x.AuthorizationVersion))
            .ToArrayAsync(cancellationToken);
        if (users.Length == 0) return;

        var compliance = await (
            from vehicle in dbContext.Vehicles.AsNoTracking()
            join registration in dbContext.VehicleRegistrations.AsNoTracking().Where(x => x.IsCurrent) on vehicle.Id equals registration.VehicleId into registrations
            from registration in registrations.DefaultIfEmpty()
            join insurance in dbContext.VehicleInsurancePolicies.AsNoTracking().Where(x => x.IsCurrent) on vehicle.Id equals insurance.VehicleId into policies
            from insurance in policies.DefaultIfEmpty()
            join inspection in dbContext.VehiclePeriodicInspections.AsNoTracking().Where(x => x.IsCurrent) on vehicle.Id equals inspection.VehicleId into inspections
            from inspection in inspections.DefaultIfEmpty()
            select new { vehicle.Id, vehicle.AssetNumber, RegistrationId = registration == null ? (Guid?)null : registration.Id, RegistrationExpiry = registration == null ? null : (DateOnly?)registration.ExpiryDate, InsuranceId = insurance == null ? (Guid?)null : insurance.Id, InsuranceExpiry = insurance == null ? null : (DateOnly?)insurance.ExpiryDate, InspectionId = inspection == null ? (Guid?)null : inspection.Id, InspectionExpiry = inspection == null ? null : (DateOnly?)inspection.ExpiryDate })
            .ToArrayAsync(cancellationToken);

        foreach (var item in compliance)
        {
            await NotifyAsync(users, PermissionKeys.Fleet.ComplianceRead, "registration", item.RegistrationId, item.Id, item.AssetNumber, item.RegistrationExpiry, checkDate, now, cancellationToken);
            await NotifyAsync(users, PermissionKeys.Fleet.ComplianceRead, "insurance", item.InsuranceId, item.Id, item.AssetNumber, item.InsuranceExpiry, checkDate, now, cancellationToken);
            await NotifyAsync(users, PermissionKeys.Fleet.ComplianceRead, "inspection", item.InspectionId, item.Id, item.AssetNumber, item.InspectionExpiry, checkDate, now, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyAsync(
        IReadOnlyList<NotificationUser> users,
        string permission,
        string type,
        Guid? recordId,
        Guid sourceId,
        string assetNumber,
        DateOnly? expiry,
        DateOnly checkDate,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (recordId is null || expiry is null) return;
        var days = expiry.Value.DayNumber - checkDate.DayNumber;
        var band = days < 0 ? "expired" : ReminderDays.Contains(days) ? days.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
        if (band is null) return;
        foreach (var user in users)
        {
            if (!await permissionChecker.HasPermissionAsync(user.Id, user.AuthorizationVersion, permission, null, cancellationToken)) continue;
            var key = $"fleet:{type}:{recordId:N}:{band}";
            if (await dbContext.Notifications.AnyAsync(x => x.RecipientUserId == user.Id && x.DeduplicationKey == key, cancellationToken)) continue;
            var expired = days < 0;
            dbContext.Notifications.Add(new Notification
            {
                RecipientUserId = user.Id,
                EventType = $"fleet.{type}.{(expired ? "expired" : "due")}",
                Severity = expired ? NotificationSeverity.Error : days <= 1 ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                TitleAr = expired ? $"انتهاء {ArabicType(type)}" : $"اقتراب انتهاء {ArabicType(type)}",
                TitleEn = expired ? $"{EnglishType(type)} expired" : $"{EnglishType(type)} expiring",
                BodyAr = expired ? $"انتهى {ArabicType(type)} للمركبة {assetNumber} بتاريخ {expiry:yyyy-MM-dd}." : $"ينتهي {ArabicType(type)} للمركبة {assetNumber} خلال {days} يوم/أيام.",
                BodyEn = expired ? $"{EnglishType(type)} for vehicle {assetNumber} expired on {expiry:yyyy-MM-dd}." : $"{EnglishType(type)} for vehicle {assetNumber} expires in {days} day(s).",
                SourceEntityType = type,
                SourceEntityId = sourceId,
                DeepLink = $"/fleet/vehicles/{sourceId}",
                ScopeSnapshotJson = "{}",
                DeduplicationKey = key,
                VisibleAtUtc = now
            });
        }
    }

    private static string EnglishType(string type) => type switch { "registration" => "Vehicle registration", "insurance" => "Vehicle insurance", "inspection" => "Periodic inspection", _ => "Vehicle permission" };
    private static string ArabicType(string type) => type switch { "registration" => "تسجيل المركبة", "insurance" => "تأمين المركبة", "inspection" => "الفحص الدوري", _ => "تصريح عهدة المركبة" };
    private sealed record NotificationUser(Guid Id, long AuthorizationVersion);
}
