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
    public async Task<Result<ChefzPerformanceDeletionResponse>> DeletePerformanceAsync(DateOnly reportDate, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.PerformanceDelete, ct))
            return Result.Failure<ChefzPerformanceDeletionResponse>(ChefzErrors.Forbidden);
        if (reportDate == default)
            return Result.Failure<ChefzPerformanceDeletionResponse>(ChefzErrors.Invalid("حدد تاريخ التقرير."));
        var accounts = await Accounts.Select(a => a.Id).ToArrayAsync(ct);
        await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, accounts, ct);
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var rowCount = await db.ChefzDailyPerformance.Where(p => p.ReportDate == reportDate).ExecuteDeleteAsync(ct);
            var sourceCount = await db.Set<ChefzDailyPerformanceSource>().IgnoreQueryFilters().Where(p => p.ReportDate == reportDate).ExecuteDeleteAsync(ct);
            var importCount = await db.ChefzPerformanceImports.Where(p => p.ReportDate == reportDate).ExecuteDeleteAsync(ct);
            var response = new ChefzPerformanceDeletionResponse(reportDate, rowCount, sourceCount, importCount);
            db.AuditEntries.Add(new AuditEntry { ActorUserId = user.UserId, ActorType = "User", Action = "Delete",
                Category = "Chefz", EntityType = "ChefzPerformanceDay", OccurredAtUtc = clock.GetUtcNow(),
                CorrelationId = Guid.NewGuid().ToString(), Source = "API", BeforeJson = JsonSerializer.Serialize(response) });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Result.Success(response);
        });
    }
}
