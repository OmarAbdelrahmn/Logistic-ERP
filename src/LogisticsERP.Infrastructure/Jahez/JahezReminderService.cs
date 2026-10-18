using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Jahez;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed class JahezReminderService(ApplicationDbContext db, IdentityDbContext identity, IPermissionChecker permissions,
    TimeProvider clock) : IJahezReminderService
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var platformId = await db.ClientPlatforms.Where(x => x.Code == "JAHEZ").Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (platformId is null) return;
        var users = await identity.Users.AsNoTracking().Where(x => x.Status == UserAccountStatus.Active && !x.IsDevelopmentOnly)
            .Select(x => new { x.Id, x.AuthorizationVersion }).ToArrayAsync(ct);
        List<Guid> recipients = [];
        foreach (var u in users)
            if (await permissions.HasPermissionAsync(u.Id, u.AuthorizationVersion, PermissionKeys.Jahez.Read,
                new PermissionScope(AccessScopeType.ClientPlatform, platformId.Value), ct)) recipients.Add(u.Id);
        var cutoff = JahezRules.StartOfDay(JahezRules.RiyadhDate(now).AddDays(-10)).ToUniversalTime();
        Guid? after = null;
        while (true)
        {
            var query = db.Set<JahezAccountSession>().AsNoTracking().Where(x => (x.LastSettlementPaymentAtUtc ?? x.StartedAtUtc) < cutoff);
            if (after.HasValue) query = query.Where(x => x.Id.CompareTo(after.Value) > 0);
            var sessions = await query.OrderBy(x => x.Id).Take(200).ToArrayAsync(ct);
            if (sessions.Length == 0) break;
            foreach (var h in sessions)
            {
                var operations = db.Set<JahezOperation>().Where(x => x.HandoverId == h.Id && x.ExecutedAtUtc != null);
                var settled = await operations.Where(x => x.Mode != JahezOperationMode.Withdraw).Select(x => (DateOnly?)x.ThroughDate).MaxAsync(ct);
                var deferred = h.EndedAtUtc.HasValue && (!settled.HasValue || settled < JahezRules.RiyadhDate(h.EndedAtUtc.Value));
                var debt = await db.Set<JahezTransferredDebt>().AnyAsync(x => x.PaidAtUtc == null && operations.Any(o => o.Id == x.OperationId), ct);
                var due = h.EndedAtUtc == null || deferred || debt;
                var anchor = h.LastSettlementPaymentAtUtc ?? h.StartedAtUtc;
                var states = await db.Set<JahezReminderState>().Where(x => x.HandoverId == h.Id && x.ResolvedAtUtc == null).ToArrayAsync(ct);
                foreach (var state in states.Where(x => !due || x.AnchorAtUtc != anchor))
                {
                    state.ResolvedAtUtc = now;
                    var key = $"jahez:overdue:{state.Id:N}";
                    foreach (var notification in await db.Notifications.Where(x => x.DeduplicationKey == key && x.ArchivedAtUtc == null).ToArrayAsync(ct))
                    { notification.ArchivedAtUtc = now; notification.ExpiresAtUtc = now; }
                }
                if (due && recipients.Count > 0)
                {
                    var state = await db.Set<JahezReminderState>().SingleOrDefaultAsync(x => x.HandoverId == h.Id && x.AnchorAtUtc == anchor, ct);
                    if (state is null) { state = new() { HandoverId = h.Id, AnchorAtUtc = anchor }; db.Add(state); }
                    state.ResolvedAtUtc = null;
                    var key = $"jahez:overdue:{state.Id:N}";
                    var notified = await db.Notifications.Where(x => x.DeduplicationKey == key).Select(x => x.RecipientUserId).ToArrayAsync(ct);
                    foreach (var recipient in recipients.Except(notified)) db.Add(new Notification
                    {
                        RecipientUserId = recipient, EventType = "jahez.settlement.overdue", Severity = NotificationSeverity.Warning,
                        TitleAr = "تأخر تصفية جاهز", TitleEn = "Jahez settlement overdue",
                        BodyAr = "توجد فترة جاهز تحتاج تصفية أو سداد مستحق مرحل. المديونية الإدارية لا تحدد مبلغ التصفية.",
                        BodyEn = "A Jahez settlement or transferred repayment is due. Administrative debt does not determine settlement.",
                        SourceEntityType = nameof(JahezAccountSession), SourceEntityId = h.Id, DeepLink = $"/jahez/accounts/{h.PlatformRiderAccountId}",
                        AudiencePermissionKeysJson = JsonSerializer.Serialize(new[] { PermissionKeys.Jahez.Read }),
                        ScopeSnapshotJson = JsonSerializer.Serialize(new { ClientPlatformId = platformId }), DeduplicationKey = key, VisibleAtUtc = now
                    });
                }
            }
            await db.SaveChangesAsync(ct); db.ChangeTracker.Clear();
            after = sessions[^1].Id;
        }
    }
}
