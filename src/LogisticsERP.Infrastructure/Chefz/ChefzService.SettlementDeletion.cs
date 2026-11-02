using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Chefz;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Chefz;

internal sealed partial class ChefzService
{
    public async Task<Result<ChefzSettlementDeletionResponse>> DeleteSettlementsAsync(int year, int month, int half, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.SettlementsDelete, ct))
            return Result.Failure<ChefzSettlementDeletionResponse>(ChefzErrors.Forbidden);
        if (year is < 1 or > 9999 || month is < 1 or > 12 || half is not (1 or 2))
            return Result.Failure<ChefzSettlementDeletionResponse>(ChefzErrors.Invalid("تحقق من السنة والشهر ونصف الشهر."));
        var (from, to) = ChefzRules.Period(year, month, half);
        var accounts = await Accounts.Select(a => a.Id).ToArrayAsync(ct);
        await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, accounts, ct);
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var settlements = db.ChefzWalletSettlements.Where(s => s.PeriodFrom == from && s.PeriodTo == to);
            var importIds = await settlements.Where(s => s.ImportId != null).Select(s => s.ImportId!.Value).Distinct().ToArrayAsync(ct);
            // Removing a whole-file hash for a partial batch would break replay or overwrite another period.
            if (await db.ChefzWalletSettlements.AnyAsync(s => s.ImportId != null && importIds.Contains(s.ImportId.Value)
                    && (s.PeriodFrom != from || s.PeriodTo != to), ct))
                return Result.Failure<ChefzSettlementDeletionResponse>(ChefzErrors.Conflict("يتضمن ملف الاستيراد فترات أخرى؛ لا يمكن حذف فترة واحدة من ملف متعدد الفترات. استخدم ملفاً مستقلاً لكل فترة."));
            var rows = db.ChefzImportedOrderRows.Where(r => importIds.Contains(r.ImportId));
            var rowCount = await rows.ExecuteDeleteAsync(ct);
            var settlementCount = await settlements.ExecuteDeleteAsync(ct);
            var importCount = await db.ChefzOrderSettlementImports.Where(i => importIds.Contains(i.Id)).ExecuteDeleteAsync(ct);
            var response = new ChefzSettlementDeletionResponse(from, to, settlementCount, importCount, rowCount);
            db.AuditEntries.Add(new AuditEntry { ActorUserId = user.UserId, ActorType = "User", Action = "Delete",
                Category = "Chefz", EntityType = "ChefzSettlementPeriod", OccurredAtUtc = clock.GetUtcNow(),
                CorrelationId = Guid.NewGuid().ToString(), Source = "API", BeforeJson = JsonSerializer.Serialize(response) });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Result.Success(response);
        });
    }
}
