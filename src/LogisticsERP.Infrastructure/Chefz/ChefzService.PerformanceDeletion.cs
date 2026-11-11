using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Chefz;

internal sealed partial class ChefzService
{
    public Task<Result<ChefzPerformanceDeletionResponse>> DeletePerformanceAsync(DateOnly reportDate, CancellationToken ct = default) =>
        DeletePerformanceAsync(reportDate, null, "Performance day correction", ct);

    public async Task<Result<ChefzPerformanceDeletionResponse>> DeletePerformanceAsync(DateOnly reportDate, Guid? sponsorId, string reason, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.PerformanceDelete, ct))
            return Result.Failure<ChefzPerformanceDeletionResponse>(ChefzErrors.Forbidden);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        if (reportDate == default || reportDate > today || sponsorId == Guid.Empty || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            return Result.Failure<ChefzPerformanceDeletionResponse>(ChefzErrors.Invalid("حدد تاريخاً صحيحاً وسبب الحذف بحد أقصى 1000 حرف والكفيل إن وجد."));
        if (sponsorId.HasValue && !await db.Sponsors.IgnoreQueryFilters().AnyAsync(s => s.Id == sponsorId, ct))
            return Result.Failure<ChefzPerformanceDeletionResponse>(ChefzErrors.Invalid("الكفيل غير موجود."));
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                var platformIds = await db.ClientPlatforms.AsNoTracking().Where(p => p.Code == "CHEFZ" || p.Code == "THECHEFZ" || p.Code == "CHEFSZ" || p.Code == "SHIFTZ")
                    .Select(p => p.Id).ToArrayAsync(ct);
                var accounts = db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking().Where(a => platformIds.Contains(a.ClientPlatformId));
                var lockIds = await accounts.Select(a => a.Id).ToArrayAsync(ct);
                await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, lockIds, ct);
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                var ids = await accounts.Where(a => lockIds.Contains(a.Id) && (sponsorId == null || a.SponsorId == sponsorId)).Select(a => a.Id).ToArrayAsync(ct);
                var rowCount = await db.ChefzDailyPerformance.Where(p => p.ReportDate == reportDate && ids.Contains(p.PlatformRiderAccountId)).ExecuteDeleteAsync(ct);
                var sourceCount = await db.Set<ChefzDailyPerformanceSource>().Where(p => p.ReportDate == reportDate && ids.Contains(p.PlatformRiderAccountId)).ExecuteDeleteAsync(ct);
                // Keep import history, which may also cover another sponsor. The importer rebuilds missing account/days.
                var response = new ChefzPerformanceDeletionResponse(reportDate, rowCount, sourceCount, 0) { SponsorId = sponsorId };
                var audit = new AuditEntry { ActorUserId = user.UserId, ActorType = "User", Action = "Delete",
                    Category = "Chefz", EntityType = "ChefzPerformanceDay", OccurredAtUtc = clock.GetUtcNow(),
                    CorrelationId = user.CorrelationId ?? Guid.NewGuid().ToString(), Source = "API", Reason = reason.Trim(), BeforeJson = JsonSerializer.Serialize(response) };
                db.AuditEntries.Add(audit);
                try { await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); }
                catch { db.Entry(audit).State = EntityState.Detached; throw; }
                return Result.Success(response);
            });
        }
        catch (DbUpdateException) { return Result.Failure<ChefzPerformanceDeletionResponse>(ChefzErrors.Conflict("تعارض أثناء حذف الأداء؛ أعد المحاولة.")); }
    }
}
