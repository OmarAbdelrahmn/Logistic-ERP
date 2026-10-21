using System.Security.Cryptography;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Chefz;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Chefz;

internal sealed partial class ChefzService
{
    private static ChefzOrderSettlementResponse OrderSettlementResponse(ChefzWalletSettlement s) => new(
        s.Id, s.PlatformRiderAccountId, s.DriverId, s.RiderClientAssignmentId, s.RiderProfileId,
        s.RiderName, s.AccountType.ToString(), s.PeriodFrom, s.PeriodTo, s.ChargedFrom, s.ChargedTo,
        s.AssignedDays, s.SourceRows, s.DeliveryFees, s.VatAmount, s.DeliveryFees - s.VatAmount,
        s.Tips, s.Compensation, s.Bonuses, s.Deductions, s.GrossEarnings, s.CompanyAmount, s.RiderAmount);

    private async Task<ChefzOrderSettlementImportResponse> OrderImportResponse(Guid? id, bool preview,
        bool alreadyImported, int sourceRows, IReadOnlyList<ChefzWalletSettlement> settlements,
        List<ChefzImportIssue> issues, CancellationToken ct)
    {
        var identities = await RiderIdentities(settlements.Select(s => s.RiderProfileId), ct);
        var riders = settlements.GroupBy(s => new { s.RiderProfileId, s.PeriodFrom, s.PeriodTo })
            .OrderBy(g => g.Key.PeriodFrom).ThenBy(g => g.First().RiderName, StringComparer.Ordinal).ThenBy(g => g.Key.RiderProfileId)
            .Select(g => new ChefzRiderCycleSettlement(g.Key.RiderProfileId, g.First().RiderName,
                g.Key.PeriodFrom, g.Key.PeriodTo, g.Sum(s => s.SourceRows), g.Sum(s => s.DeliveryFees),
                g.Sum(s => s.VatAmount), g.Sum(s => s.DeliveryFees - s.VatAmount), g.Sum(s => s.Tips),
                g.Sum(s => s.Compensation), g.Sum(s => s.Bonuses), g.Sum(s => s.Deductions),
                g.Sum(s => s.GrossEarnings), g.Sum(s => s.CompanyAmount), g.Sum(s => s.RiderAmount),
                g.OrderBy(s => s.DriverId, StringComparer.Ordinal).ThenBy(s => s.RiderClientAssignmentId)
                    .Select(s => OrderSettlementResponse(s) with
                    { RiderIqamaNo = identities.GetValueOrDefault(s.RiderProfileId)?.RiderIqamaNo }).ToArray())
                { RiderIqamaNo = identities.GetValueOrDefault(g.Key.RiderProfileId)?.RiderIqamaNo }).ToArray();
        return new(id, preview, alreadyImported, preview && !alreadyImported && issues.Count == 0,
            sourceRows, riders, riders.Sum(r => r.NetEarnings), riders.Sum(r => r.CompanyAmount),
            riders.Sum(r => r.RiderAmount), issues);
    }

    private async Task<ChefzOrderSettlementImportResponse> ReadOrderImport(ChefzOrderSettlementImport batch, bool preview, bool alreadyImported, CancellationToken ct)
    {
        var rows = await db.ChefzWalletSettlements.AsNoTracking().Where(s => s.ImportId == batch.Id).ToArrayAsync(ct);
        return await OrderImportResponse(batch.Id, preview, alreadyImported, batch.SourceRows, rows, [], ct);
    }

    public async Task<Result<ChefzOrderSettlementImportResponse>> GetOrderSettlementImportAsync(Guid importId, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct)) return Result.Failure<ChefzOrderSettlementImportResponse>(ChefzErrors.Forbidden);
        var batch = await db.ChefzOrderSettlementImports.AsNoTracking().SingleOrDefaultAsync(i => i.Id == importId, ct);
        return batch is null ? Result.Failure<ChefzOrderSettlementImportResponse>(ChefzErrors.NotFound)
            : Result.Success(await ReadOrderImport(batch, false, false, ct));
    }

    public async Task<Result<ChefzOrderSettlementImportResponse>> ProcessOrderSettlementsAsync(
        string fileName, byte[] content, bool preview, CancellationToken ct = default)
    {
        if (!await Allowed(preview ? PermissionKeys.Chefz.Read : PermissionKeys.Chefz.SettlementsCreate, ct))
            return Result.Failure<ChefzOrderSettlementImportResponse>(ChefzErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255
            || !fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            || content.Length is 0 or > ChefzSpreadsheetParser.MaxBytes)
            return Result.Failure<ChefzOrderSettlementImportResponse>(ChefzErrors.Invalid("ارفع ملف xlsx بحد أقصى 10 ميجابايت."));
        var hash = Convert.ToHexString(SHA256.HashData(content));
        var oldImport = await db.ChefzOrderSettlementImports.AsNoTracking().SingleOrDefaultAsync(i => i.ContentHash == hash, ct);
        if (oldImport is not null) return Result.Success(await ReadOrderImport(oldImport, preview, true, ct));
        var parsed = ChefzOrderSpreadsheetParser.Parse(content, ct);
        var issues = parsed.Issues.ToList();
        if (issues.Count != 0) return Result.Success(await OrderImportResponse(null, preview, false, parsed.Rows.Count, [], issues, ct));

        var driverIds = parsed.Rows.Select(r => r.DriverId).Distinct().ToArray();
        // Driver ID identifies the platform account, never the payable employee.
        var details = await (from d in db.ChefzAccountDetails.AsNoTracking() join a in Accounts.AsNoTracking()
            on d.PlatformRiderAccountId equals a.Id where driverIds.Contains(d.ReportIdNumber) select d).ToArrayAsync(ct);
        var byDriver = details.ToLookup(d => d.ReportIdNumber, StringComparer.Ordinal);
        var accountIds = details.Select(d => d.PlatformRiderAccountId).ToArray();
        await using var accountScope = await PlatformAccountWriteScope.AcquireAsync(db, accountIds, ct);
        oldImport = await db.ChefzOrderSettlementImports.AsNoTracking().SingleOrDefaultAsync(i => i.ContentHash == hash, ct);
        if (oldImport is not null) return Result.Success(await ReadOrderImport(oldImport, preview, true, ct));
        var from = parsed.Rows.Min(r => ChefzRules.Period(r.Date.Year, r.Date.Month, r.Date.Day <= 15 ? 1 : 2).From);
        var to = parsed.Rows.Max(r => ChefzRules.Period(r.Date.Year, r.Date.Month, r.Date.Day <= 15 ? 1 : 2).To);
        var assignments = await db.RiderClientAssignments.AsNoTracking().Where(a => accountIds.Contains(a.PlatformRiderAccountId)
            && (a.Status == RiderAssignmentStatus.Active || a.Status == RiderAssignmentStatus.Ended)
            && a.EffectiveFrom <= to && (a.EffectiveTo == null || a.EffectiveTo >= from)).ToArrayAsync(ct);
        var byAccount = assignments.ToLookup(a => a.PlatformRiderAccountId);
        var riderIds = assignments.Select(a => a.RiderProfileId).Distinct().ToArray();
        var names = await (from r in db.RiderProfiles.AsNoTracking() join e in db.Employees.AsNoTracking()
            on r.EmployeeId equals e.Id where riderIds.Contains(r.Id) select new { r.Id, e.FullNameAr }).ToDictionaryAsync(r => r.Id, r => r.FullNameAr, ct);
        var resolved = new List<(ChefzOrderRow Row, Guid AssignmentId, int Sequence)>();
        var allocator = new PlatformAccountDayAllocationService(db);
        await allocator.Preload(accountIds, from, to, ct);
        foreach (var day in parsed.Rows.GroupBy(r => new { r.DriverId, r.Date }))
        {
            ct.ThrowIfCancellationRequested();
            var matches = byDriver[day.Key.DriverId].ToArray();
            if (matches.Length != 1 || !Enum.IsDefined(matches[0].AccountType))
            { issues.Add(new(day.First().RowNumber, day.Key.DriverId, "Driver ID must identify one Chefz account with a valid account type.")); continue; }
            var allocation = await allocator.AllocateAsync(matches[0].PlatformRiderAccountId, day.Key.Date,
                new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = day.Count() }, ct);
            if (allocation.IsFailure) { issues.Add(new(day.First().RowNumber, day.Key.DriverId, allocation.Error.Description)); continue; }
            var orderedRows = day.OrderBy(r => r.RowNumber).ToArray(); var offset = 0;
            foreach (var share in allocation.Value!)
            {
                if (!names.ContainsKey(share.RiderProfileId))
                { issues.Add(new(day.First().RowNumber, day.Key.DriverId, $"Actual rider {share.RiderProfileId} does not exist.")); break; }
                var count = checked((int)share.Values[PlatformDayMetric.OrdersCount]);
                foreach (var row in orderedRows.Skip(offset).Take(count)) resolved.Add((row, share.AssignmentId, share.Sequence));
                offset += count;
            }
        }
        if (issues.Count != 0) return Result.Success(await OrderImportResponse(null, preview, false, parsed.Rows.Count, [], issues, ct));
        var batch = new ChefzOrderSettlementImport { FileName = Path.GetFileName(fileName), ContentHash = hash, SourceRows = parsed.Rows.Count };
        var byAssignment = assignments.ToDictionary(a => a.Id);
        var accountDetails = details.ToDictionary(d => d.PlatformRiderAccountId);
        var pending = new List<ChefzWalletSettlement>();
        foreach (var group in resolved.GroupBy(r => new { r.AssignmentId, r.Row.Date.Year, r.Row.Date.Month, Half = r.Row.Date.Day <= 15 ? 1 : 2 }))
        {
            ct.ThrowIfCancellationRequested();
            var assignment = byAssignment[group.Key.AssignmentId];
            var detail = accountDetails[assignment.PlatformRiderAccountId];
            var settlement = ChefzOrderSettlementRules.Calculate(detail.AccountType, group.Key.Year, group.Key.Month, group.Key.Half,
                assignment.EffectiveFrom, assignment.EffectiveTo, group.Count(), group.Sum(r => r.Row.DeliveryFees),
                group.Sum(r => r.Row.Tips), group.Sum(r => r.Row.Compensation), group.Sum(r => r.Row.Bonuses), group.Sum(r => r.Row.Deductions));
            settlement.PlatformRiderAccountId = assignment.PlatformRiderAccountId;
            settlement.RiderClientAssignmentId = assignment.Id;
            settlement.RiderProfileId = assignment.RiderProfileId;
            settlement.RiderName = names[assignment.RiderProfileId];
            settlement.DriverId = detail.ReportIdNumber;
            settlement.ImportId = batch.Id;
            if (preview) settlement.Id = Guid.Empty;
            pending.Add(settlement);
        }
        ChefzOrderSettlementRules.ReconcileRiderCycles(pending);
        var existing = await db.ChefzWalletSettlements.AsNoTracking().Where(s => accountIds.Contains(s.PlatformRiderAccountId)
            && s.PeriodFrom >= from && s.PeriodFrom <= to).Select(s => new { s.RiderClientAssignmentId, s.PeriodFrom }).ToArrayAsync(ct);
        var existingKeys = existing.Select(s => (s.RiderClientAssignmentId, s.PeriodFrom)).ToHashSet();
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        foreach (var s in pending)
        {
            if (s.AccountType == ChefzAccountType.FullTime && byAccount[s.PlatformRiderAccountId].Any(a =>
                a.Id != s.RiderClientAssignmentId && a.EffectiveFrom <= s.ChargedTo
                && (a.EffectiveTo == null || a.EffectiveTo >= s.ChargedFrom)))
                issues.Add(new(0, s.DriverId, "توجد تكليفات متداخلة خلال أيام رسوم الحساب الكامل؛ صحح التواريخ لمنع احتساب رسوم اليوم مرتين."));
            if (existingKeys.Contains((s.RiderClientAssignmentId, s.PeriodFrom)))
                issues.Add(new(0, s.DriverId, $"توجد تصفية محفوظة للتكليف والفترة {s.PeriodFrom:yyyy-MM-dd}؛ لا يمكن تكرارها أو استبدالها."));
            if (s.ChargedTo > today)
                issues.Add(new(0, s.DriverId, $"يمكن المعاينة، لكن حفظ الفترة {s.PeriodFrom:yyyy-MM-dd} يتطلب انتهاء أيام التكليف حتى {s.ChargedTo:yyyy-MM-dd}."));
        }
        // One atomic save; invalid/conflicting batches never stage financial writes.
        if (preview || issues.Count != 0)
        {
            foreach (var s in pending) { s.Id = Guid.Empty; s.ImportId = null; }
            return Result.Success(await OrderImportResponse(null, preview, false, parsed.Rows.Count, pending, issues, ct));
        }
        db.AddRange(resolved.Select(r => new ChefzImportedOrderRow
        {
            ImportId = batch.Id, SourceRowNumber = r.Row.RowNumber,
            PlatformRiderAccountId = byAssignment[r.AssignmentId].PlatformRiderAccountId,
            RiderClientAssignmentId = r.AssignmentId, BusinessDate = r.Row.Date, UsageSequence = r.Sequence,
            OrderId = r.Row.OrderId, DeliveryFees = r.Row.DeliveryFees, Tips = r.Row.Tips,
            Compensation = r.Row.Compensation, Bonuses = r.Row.Bonuses, Deductions = r.Row.Deductions, CashCollected = r.Row.CashCollected
        }));
        db.ChefzOrderSettlementImports.Add(batch);
        db.ChefzWalletSettlements.AddRange(pending);
        var response = await OrderImportResponse(batch.Id, false, false, batch.SourceRows, pending, [], ct);
        return await Persist(response, ct);
    }
}
