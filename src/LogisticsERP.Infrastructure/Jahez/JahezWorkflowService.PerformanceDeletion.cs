using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    public async Task<Result<JahezPerformanceDeletionResponse>> DeletePerformanceAsync(DateOnly reportDate,
        Guid? sponsorId, string reason, CancellationToken ct = default)
    {
        try
        {
            var platform = await AuthorizeAsync(PermissionKeys.Jahez.PerformanceDelete, ct);
            Require(reportDate != default && reportDate <= DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime)
                && sponsorId != Guid.Empty && !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length <= 1000,
                JahezErrors.Invalid("حدد تاريخاً صحيحاً وسبب الحذف بحد أقصى 1000 حرف والكفيل إن وجد."));
            Require(!sponsorId.HasValue || await db.Sponsors.IgnoreQueryFilters().AnyAsync(s => s.Id == sponsorId, ct), JahezErrors.Invalid("الكفيل غير موجود."));
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                var accounts = db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking().Where(a => a.ClientPlatformId == platform);
                var lockIds = await accounts.Select(a => a.Id).ToArrayAsync(ct);
                await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, lockIds, ct);
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                var ids = await accounts.Where(a => lockIds.Contains(a.Id) && (sponsorId == null || a.SponsorId == sponsorId)).Select(a => a.Id).ToArrayAsync(ct);
                var sessions = db.JahezAccountSessions.IgnoreQueryFilters().Where(h => ids.Contains(h.PlatformRiderAccountId)).Select(h => h.Id);
                var rows = db.Set<JahezDailyDispatch>().Where(d => d.Date == reportDate && sessions.Contains(d.HandoverId));
                // Preserve original rows in audit as well as the uploaded files and import batch.
                var before = await rows.AsNoTracking().ToArrayAsync(ct);
                var count = await rows.ExecuteDeleteAsync(ct);
                var response = new JahezPerformanceDeletionResponse(reportDate, sponsorId, count);
                var audit = new AuditEntry { ActorUserId = user.UserId, ActorType = "User", Action = "Delete", Category = "Jahez", EntityType = "JahezPerformanceDay",
                    OccurredAtUtc = clock.GetUtcNow(), CorrelationId = user.CorrelationId ?? Guid.NewGuid().ToString(), Source = "API", Reason = reason.Trim(),
                    BeforeJson = JsonSerializer.Serialize(new { response, Rows = before }) };
                db.AuditEntries.Add(audit);
                try { await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); }
                catch { db.Entry(audit).State = EntityState.Detached; throw; }
                return Result.Success(response);
            });
        }
        catch (JahezBusinessException e) { return Result.Failure<JahezPerformanceDeletionResponse>(e.Error); }
        catch (DbUpdateException) { return Result.Failure<JahezPerformanceDeletionResponse>(JahezErrors.Conflict("تعارض أثناء حذف الطلبات؛ أعد المحاولة.")); }
    }
}
