using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed class AccidentNotificationService(ApplicationDbContext dbContext, IdentityDbContext identityDbContext,
    IPermissionChecker permissionChecker, TimeProvider timeProvider) : IAccidentNotificationService
{
    private static readonly string[] AudiencePermissions = [PermissionKeys.Fleet.AccidentsRead, PermissionKeys.Fleet.VehiclesRead,
        PermissionKeys.Operations.PlatformAssignmentsRead, PermissionKeys.Maintenance.WorkOrdersRead];
    public async Task QueueAsync(Guid accidentId, Guid vehicleId, string accidentNumber, string eventKey, string description, CancellationToken cancellationToken = default)
    {
        var users = await identityDbContext.Users.AsNoTracking().Where(x => x.Status == UserAccountStatus.Active)
            .Select(x => new { x.Id, x.AuthorizationVersion }).ToArrayAsync(cancellationToken);
        var key = $"accident:{accidentId:N}:{eventKey}";
        var existing = await dbContext.Notifications.AsNoTracking().Where(x => x.DeduplicationKey == key).Select(x => x.RecipientUserId).ToArrayAsync(cancellationToken);
        foreach (var user in users)
        {
            if (existing.Contains(user.Id) || dbContext.Notifications.Local.Any(x => x.RecipientUserId == user.Id && x.DeduplicationKey == key)) continue;
            var permitted = false;
            foreach (var permission in AudiencePermissions)
            {
                if (await permissionChecker.HasPermissionAsync(user.Id, user.AuthorizationVersion, permission, null, cancellationToken)) { permitted = true; break; }
            }
            if (!permitted) continue;
            dbContext.Notifications.Add(new Notification
            {
                RecipientUserId = user.Id, EventType = eventKey.StartsWith("due:", StringComparison.Ordinal) ? "fleet.accident.deadline" : "fleet.accident.updated",
                Severity = NotificationSeverity.Warning, TitleAr = $"متابعة الحادث {accidentNumber}", TitleEn = $"Accident {accidentNumber}",
                BodyAr = description, BodyEn = description, SourceEntityType = "vehicle-accident", SourceEntityId = accidentId,
                DeepLink = $"/fleet/vehicles/{vehicleId}", ScopeSnapshotJson = JsonSerializer.Serialize(new { VehicleId = vehicleId, AccidentId = accidentId }),
                AudiencePermissionKeysJson = JsonSerializer.Serialize(AudiencePermissions),
                DeduplicationKey = key, VisibleAtUtc = timeProvider.GetUtcNow()
            });
        }
    }

    public async Task RunDueNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var due = await (from item in dbContext.VehicleAccidentCases.AsNoTracking()
                         join accident in dbContext.VehicleAccidents.AsNoTracking() on item.VehicleAccidentId equals accident.Id
                         where accident.Status != VehicleAccidentStatus.Closed
                             && (item.Stage == AccidentCaseStage.AwaitingInsurance && item.InsuranceDueAtUtc <= now
                                 || item.Stage == AccidentCaseStage.AwaitingSupplierTransfer && item.SupplierTransferDueAtUtc <= now)
                         select new { accident.Id, accident.VehicleId, accident.AccidentNumber, item.Stage,
                             Deadline = item.Stage == AccidentCaseStage.AwaitingInsurance ? item.InsuranceDueAtUtc : item.SupplierTransferDueAtUtc }).ToArrayAsync(cancellationToken);
        foreach (var item in due)
        {
            await QueueAsync(item.Id, item.VehicleId, item.AccidentNumber, $"due:{(int)item.Stage}:{item.Deadline!.Value.UtcTicks}",
                item.Stage == AccidentCaseStage.AwaitingInsurance ? "انتهت مهلة متابعة التأمين (15 يومًا) / Insurance follow-up is due (15 days)."
                    : "انتهت مهلة متابعة تحويل الشركة (10 أيام) / Supplier transfer follow-up is due (10 days).", cancellationToken);
        }
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            // Another worker may have inserted the same recipient/deadline notifications.
            // The unique recipient/deduplication index prevents duplicates; retry remaining rows on the next scan.
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }
}
