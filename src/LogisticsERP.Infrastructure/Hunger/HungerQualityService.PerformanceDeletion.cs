using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService
{
    public async Task<Result<HungerPerformanceDeletionResponse>> DeletePerformanceAsync(DateOnly reportDate,
        Guid? sponsorId, string reason, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.PerformanceDelete, ct);
        if (platforms.Length == 0) return Result.Failure<HungerPerformanceDeletionResponse>(HungerErrors.Forbidden);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        if (reportDate == default || reportDate > today || sponsorId == Guid.Empty || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            return Result.Failure<HungerPerformanceDeletionResponse>(HungerErrors.Invalid("حدد تاريخاً صحيحاً وسبب الحذف بحد أقصى 1000 حرف والكفيل إن وجد."));
        if (sponsorId.HasValue && !await db.Sponsors.IgnoreQueryFilters().AnyAsync(s => s.Id == sponsorId, ct))
            return Result.Failure<HungerPerformanceDeletionResponse>(HungerErrors.Invalid("الكفيل غير موجود."));
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                var accounts = db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking().Where(a => platforms.Contains(a.ClientPlatformId));
                var lockIds = await accounts.Select(a => a.Id).ToArrayAsync(ct);
                await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, lockIds, ct);
                var ids = await accounts.Where(a => lockIds.Contains(a.Id) && (sponsorId == null || a.SponsorId == sponsorId)).Select(a => a.Id).ToArrayAsync(ct);
                var rows = await db.Set<HungerDailyOperation>().Where(o => o.ReportDate == reportDate && ids.Contains(o.PlatformRiderAccountId)).ToArrayAsync(ct);
                var operationIds = rows.Select(o => o.Id).ToArray();
                var fines = await db.Set<HungerPotentialFine>().Where(f => operationIds.Contains(f.DailyOperationId)).ToArrayAsync(ct);
                if (fines.Any(f => f.Status != "Potential" || f.InvoiceDocumentId.HasValue || f.ResolutionEvidence != null))
                    return Result.Failure<HungerPerformanceDeletionResponse>(HungerErrors.Conflict("يوجد غياب تمت مراجعته أو ربطه بفاتورة؛ صحح المراجعة المالية أولاً."));
                // Soft deletion preserves the original observation, allocation and manual comparison.
                foreach (var row in rows) { row.IsDeleted = true; row.DeletionReason = reason.Trim(); }
                foreach (var fine in fines) { fine.IsDeleted = true; fine.DeletionReason = reason.Trim(); }
                var response = new HungerPerformanceDeletionResponse(reportDate, sponsorId, rows.Length, fines.Length);
                db.AuditEntries.Add(new AuditEntry
                {
                    ActorUserId = user.UserId, ActorType = "User", Action = "Delete", Category = "Hunger", EntityType = "HungerPerformanceDay",
                    OccurredAtUtc = clock.GetUtcNow(), CorrelationId = user.CorrelationId ?? Guid.NewGuid().ToString(), Source = "API",
                    Reason = reason.Trim(), BeforeJson = Serialize(response)
                });
                await db.SaveChangesAsync(ct);
                return Result.Success(response);
            });
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return Result.Failure<HungerPerformanceDeletionResponse>(HungerErrors.Conflict("تعارض أثناء حذف الأداء؛ أعد المحاولة."));
        }
    }
}
