using System.Data;
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
    TimeProvider clock, JahezService finance) : IJahezReminderService
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var today = JahezRules.RiyadhDate(now);
        var platformId = await db.ClientPlatforms.Where(x => x.Code == "JAHEZ").Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (!platformId.HasValue) return;
        var users = await identity.Users.AsNoTracking().Where(x => x.Status == UserAccountStatus.Active && !x.IsDevelopmentOnly)
            .Select(x => new { x.Id, x.AuthorizationVersion }).ToArrayAsync(ct);
        List<Guid> recipients = [];
        foreach (var u in users)
            if (await permissions.HasPermissionAsync(u.Id, u.AuthorizationVersion, PermissionKeys.Jahez.Read,
                    new PermissionScope(AccessScopeType.ClientPlatform, platformId.Value), ct)) recipients.Add(u.Id);

        // Resolve old episodes even when no recipients currently hold permission.
        var states = await db.Set<JahezReminderState>().Where(x => x.ResolvedAtUtc == null).ToArrayAsync(ct);
        foreach (var state in states)
        {
            var h = await db.Set<JahezAccountHandover>().AsNoTracking().SingleAsync(x => x.Id == state.HandoverId, ct);
            var balance = await finance.BalanceForReminderAsync(h, ct);
            if ((h.LastSettlementPaymentAtUtc ?? h.StartedAtUtc) == state.AnchorAtUtc && balance.IsOverdue) continue;
            state.ResolvedAtUtc = now;
            var key = $"jahez:overdue:{state.Id:N}";
            var notifications = await db.Notifications.Where(x => x.DeduplicationKey == key && x.ArchivedAtUtc == null).ToArrayAsync(ct);
            foreach (var n in notifications) { n.ArchivedAtUtc = now; n.ExpiresAtUtc = now; }
        }
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        if (recipients.Count == 0) return;
        var cutoff = JahezRules.StartOfDay(today.AddDays(-10)).ToUniversalTime();
        Guid? after = null;
        while (true)
        {
            var query = db.Set<JahezAccountHandover>().AsNoTracking().Where(x => (x.LastSettlementPaymentAtUtc ?? x.StartedAtUtc) < cutoff
                && (x.EndedAtUtc == null || db.Set<JahezLedgerEntry>().Where(l => l.HandoverId == x.Id).Sum(l => l.Amount) > 0));
            if (after.HasValue) query = query.Where(x => x.Id.CompareTo(after.Value) > 0);
            var ids = await query.OrderBy(x => x.Id).Take(200).Select(x => x.Id).ToArrayAsync(ct);
            if (ids.Length == 0) break;
            foreach (var id in ids)
            {
                await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    db.ChangeTracker.Clear();
                    await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
                    var h = await db.Set<JahezAccountHandover>().AsNoTracking().SingleAsync(x => x.Id == id, ct);
                    var balance = await finance.BalanceForReminderAsync(h, ct);
                    if (!balance.IsOverdue) return;
                    var anchor = h.LastSettlementPaymentAtUtc ?? h.StartedAtUtc;
                    var state = await db.Set<JahezReminderState>().SingleOrDefaultAsync(x => x.HandoverId == id && x.AnchorAtUtc == anchor, ct);
                    if (state is null) { state = new JahezReminderState { HandoverId = id, AnchorAtUtc = anchor }; db.Add(state); }
                    var key = $"jahez:overdue:{state.Id:N}";
                    var riderName = await (from r in db.RiderProfiles.IgnoreQueryFilters()
                        join e in db.Employees.IgnoreQueryFilters() on r.EmployeeId equals e.Id
                        where r.Id == h.RiderProfileId select e.FullNameAr).SingleAsync(ct);
                    var latestData = balance.LatestTransactionAtUtc.HasValue
                        ? JahezRules.RiyadhDate(balance.LatestTransactionAtUtc.Value).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                        : "لا توجد حركات مستوردة";
                    var bodyAr = $"الحساب {balance.ExternalAccountId}، المندوب {riderName}: مضى {balance.DaysSinceSettlementPayment} يومًا. الرصيد {balance.TotalReceivable:0.00} ريال. بيانات العمولة {(balance.CommissionComplete ? "مكتملة" : "غير مكتملة")}. آخر حركة مستوردة: {latestData}.";
                    var bodyEn = $"Account {balance.ExternalAccountId}: {balance.DaysSinceSettlementPayment} days since payment; receivable SAR {balance.TotalReceivable:0.00}.";
                    // A late transaction can revive a closed rider's debt without changing its payment anchor.
                    if (state.ResolvedAtUtc.HasValue)
                    {
                        state.ResolvedAtUtc = null;
                        var previous = await db.Notifications.Where(x => x.DeduplicationKey == key).ToArrayAsync(ct);
                        foreach (var notification in previous)
                        {
                            notification.ArchivedAtUtc = null; notification.ArchivedByUserId = null;
                            notification.ExpiresAtUtc = null; notification.ReadAtUtc = null; notification.VisibleAtUtc = now;
                            notification.BodyAr = bodyAr; notification.BodyEn = bodyEn;
                        }
                    }
                    var notified = await db.Notifications.Where(x => x.DeduplicationKey == key).Select(x => x.RecipientUserId).ToArrayAsync(ct);
                    foreach (var recipient in recipients.Except(notified)) db.Add(new Notification
                    {
                        RecipientUserId = recipient, EventType = "jahez.settlement.overdue", Severity = NotificationSeverity.Warning,
                        TitleAr = "تأخر تصفية جاهز", TitleEn = "Jahez settlement overdue",
                        BodyAr = bodyAr, BodyEn = bodyEn,
                        SourceEntityType = "JahezAccountHandover", SourceEntityId = id, DeepLink = $"/jahez/handovers/{id}",
                        AudiencePermissionKeysJson = JsonSerializer.Serialize(new[] { PermissionKeys.Jahez.Read }),
                        ScopeSnapshotJson = JsonSerializer.Serialize(new { ClientPlatformId = platformId }), DeduplicationKey = key, VisibleAtUtc = now
                    });
                    await db.SaveChangesAsync(ct);
                    if (tx is not null) await tx.CommitAsync(ct);
                });
            }
            after = ids[^1];
        }
    }
}
