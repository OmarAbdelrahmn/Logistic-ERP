using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaService
{
    public async Task<Result<NinjaPerformanceDeletionResponse>> DeletePerformanceAsync(DateOnly reportDate,
        Guid? sponsorId, string reason, CancellationToken ct = default)
    {
        var platforms = await AllowedPlatforms(PermissionKeys.Ninja.PerformanceDelete, ct);
        if (platforms.Length == 0) return Result.Failure<NinjaPerformanceDeletionResponse>(NinjaErrors.Forbidden);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        if (reportDate == default || reportDate > today || sponsorId == Guid.Empty
            || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            return Result.Failure<NinjaPerformanceDeletionResponse>(NinjaErrors.Invalid("حدد يوم التقرير وسبب الحذف بما لا يتجاوز 1000 حرف والكفيل إن وجد."));
        if (sponsorId.HasValue && !await db.Sponsors.IgnoreQueryFilters().AnyAsync(s => s.Id == sponsorId, ct))
            return Result.Failure<NinjaPerformanceDeletionResponse>(NinjaErrors.Invalid("الكفيل غير موجود."));

        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                // Include archived accounts: their historical performance remains reportable.
                var accounts = db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking()
                    .Where(a => platforms.Contains(a.ClientPlatformId));
                var lockIds = await accounts.Select(a => a.Id).ToArrayAsync(ct);
                await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, lockIds, ct);
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                var ids = await accounts.Where(a => lockIds.Contains(a.Id) && (sponsorId == null || a.SponsorId == sponsorId))
                    .Select(a => a.Id).ToArrayAsync(ct);
                var performance = db.NinjaDailyPerformance.Where(p => p.ReportDate == reportDate && ids.Contains(p.PlatformRiderAccountId));
                var sources = db.NinjaDailyPerformanceSources.Where(p => p.ReportDate == reportDate && ids.Contains(p.PlatformRiderAccountId));
                var sourceIds = sources.Select(p => p.Id).Union(performance.Select(p => p.SourceId));
                var provenanceCount = await db.Set<NinjaAllocationProvenance>().Where(p => sourceIds.Contains(p.SourceId)).ExecuteDeleteAsync(ct);
                var performanceCount = await performance.ExecuteDeleteAsync(ct);
                var sourceCount = await sources.ExecuteDeleteAsync(ct);
                var response = new NinjaPerformanceDeletionResponse(reportDate, sponsorId, performanceCount, sourceCount, provenanceCount);
                // Keep imports and uploaded artifacts as evidence. The importer can rebuild missing
                // account/days from the same file without changing surviving sponsor records.
                var audit = new AuditEntry
                {
                    ActorUserId = user.UserId, ActorType = "User", Action = "Delete", Category = "Ninja",
                    EntityType = "NinjaPerformanceDay", OccurredAtUtc = clock.GetUtcNow(),
                    CorrelationId = user.CorrelationId ?? Guid.NewGuid().ToString(), Source = "API",
                    Reason = reason.Trim(), BeforeJson = JsonSerializer.Serialize(response)
                };
                db.AuditEntries.Add(audit);
                try
                {
                    await db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    db.Entry(audit).State = EntityState.Detached;
                    throw;
                }
                return Result.Success(response);
            });
        }
        catch (DbUpdateException)
        {
            return Result.Failure<NinjaPerformanceDeletionResponse>(NinjaErrors.Conflict);
        }
    }
}
