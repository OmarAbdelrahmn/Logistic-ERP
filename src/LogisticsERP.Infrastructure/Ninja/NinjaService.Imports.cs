using System.Security.Cryptography;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaService
{
    public async Task<Result<NinjaImportResponse>> ImportAsync(bool deductions, DateOnly reportDate, string fileName, byte[] content, CancellationToken ct = default)
    {
        if (!deductions) return await ImportPerformanceLegacy(reportDate, fileName, content, ct);
        var platformIds = await AllowedPlatforms(deductions ? PermissionKeys.Ninja.DeductionsImport : PermissionKeys.Ninja.PerformanceImport, ct);
        if (platformIds.Length == 0) return Result.Failure<NinjaImportResponse>(NinjaErrors.Forbidden);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        if (reportDate == default || reportDate > today || string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255
            || !fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || content.Length is 0 or > NinjaSpreadsheetParser.MaxBytes)
            return Result.Failure<NinjaImportResponse>(NinjaErrors.Invalid("حدد تاريخ التقرير وارفع ملف xlsx بحد أقصى 10 ميجابايت."));
        var parsed = NinjaSpreadsheetParser.Parse(content, deductions, ct);
        if (parsed.Issues.Count > 0) return Result.Success(new NinjaImportResponse(null, reportDate, 0, 0, parsed.Issues));
        var captainIds = parsed.Rows.Select(r => r.CaptainId).ToArray();
        // The source Captain ID is the platform account's ExternalAccountId, never an employee/owner identity.
        var accounts = await Accounts.AsNoTracking().Where(a => platformIds.Contains(a.ClientPlatformId)
            && captainIds.Contains(a.ExternalAccountId)).Select(a => new { a.Id, a.ExternalAccountId }).ToArrayAsync(ct);
        var byCaptain = accounts.ToLookup(a => a.ExternalAccountId, StringComparer.Ordinal);
        var accountIds = accounts.Select(a => a.Id).ToArray();
        await using var accountScope = await PlatformAccountWriteScope.AcquireAsync(db, accountIds, ct);
        var hash = Convert.ToHexString(SHA256.HashData(content));
        var previous = await db.NinjaDailyImports.AsNoTracking().SingleOrDefaultAsync(i => i.IsDeductions == deductions
            && i.ReportDate == reportDate && i.ContentHash == hash, ct);
        if (previous is not null) return Result.Success(new NinjaImportResponse(previous.Id, reportDate, 0, parsed.Rows.Count, []));
        var oldPerformance = await db.Set<NinjaDailyPerformanceSource>().AsNoTracking()
            .Where(p => p.ReportDate == reportDate && accountIds.Contains(p.PlatformRiderAccountId)).ToDictionaryAsync(p => p.PlatformRiderAccountId, ct);
        var oldDeductions = await db.Set<NinjaDailyDeductionSource>().AsNoTracking()
            .Where(p => p.ReportDate == reportDate && accountIds.Contains(p.PlatformRiderAccountId)).ToDictionaryAsync(p => p.PlatformRiderAccountId, ct);
        var batch = new NinjaDailyImport { ReportDate = reportDate, IsDeductions = deductions, FileName = Path.GetFileName(fileName), ContentHash = hash };
        var performance = new List<NinjaDailyPerformance>(); var amounts = new List<NinjaDailyDeduction>();
        var performanceSources = new List<NinjaDailyPerformanceSource>(); var deductionSources = new List<NinjaDailyDeductionSource>();
        var allocator = new PlatformAccountDayAllocationService(db);
        await allocator.Preload(accountIds, reportDate, reportDate, ct);
        var issues = new List<NinjaImportIssue>(); var skipped = 0;
        foreach (var row in parsed.Rows)
        {
            ct.ThrowIfCancellationRequested();
            var matches = byCaptain[row.CaptainId].ToArray();
            if (matches.Length != 1) { issues.Add(new(row.RowNumber, row.CaptainId, "Captain ID must identify one permitted account.")); continue; }
            var accountId = matches[0].Id;
            if (!deductions && oldPerformance.TryGetValue(accountId, out var old))
            {
                if (old.TotalOrders == row.TotalOrders && old.TotalWorkingHours == row.WorkingHours) skipped++;
                else issues.Add(new(row.RowNumber, row.CaptainId, "Different performance is already saved for this account/day; it was not replaced."));
                continue;
            }
            if (deductions && oldDeductions.TryGetValue(accountId, out var oldAmount))
            {
                if (oldAmount.TotalCollectedAmountAtDelivery == row.CollectedAmount && oldAmount.TotalDeductions == row.Deductions) skipped++;
                else issues.Add(new(row.RowNumber, row.CaptainId, "Different deductions are already saved for this account/day; they were not replaced."));
                continue;
            }
            var totals = deductions
                ? new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.CollectedAmount] = row.CollectedAmount, [PlatformDayMetric.DeductionsAmount] = row.Deductions }
                : new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = row.TotalOrders, [PlatformDayMetric.WorkingHours] = row.WorkingHours };
            var allocation = await allocator.AllocateAsync(accountId, reportDate, totals, ct);
            if (allocation.IsFailure) { issues.Add(new(row.RowNumber, row.CaptainId, allocation.Error.Description)); continue; }
            NinjaDailySource source;
            if (deductions)
            {
                var d = new NinjaDailyDeductionSource { TotalCollectedAmountAtDelivery = row.CollectedAmount, TotalDeductions = row.Deductions };
                deductionSources.Add(d); source = d;
            }
            else
            {
                var p = new NinjaDailyPerformanceSource { TotalOrders = row.TotalOrders, TotalWorkingHours = row.WorkingHours };
                performanceSources.Add(p); source = p;
            }
            source.PlatformRiderAccountId = accountId; source.ImportId = batch.Id; source.ReportDate = reportDate;
            source.CaptainId = row.CaptainId; source.SourceRowNumber = row.RowNumber;
            foreach (var share in allocation.Value!)
            {
                NinjaDailyRecord record;
                if (deductions)
                {
                    var d = new NinjaDailyDeduction { TotalCollectedAmountAtDelivery = share.Values[PlatformDayMetric.CollectedAmount],
                        TotalDeductions = share.Values[PlatformDayMetric.DeductionsAmount] }; amounts.Add(d); record = d;
                }
                else
                {
                    var p = new NinjaDailyPerformance { TotalOrders = checked((int)share.Values[PlatformDayMetric.OrdersCount]),
                        TotalWorkingHours = share.Values[PlatformDayMetric.WorkingHours] }; performance.Add(p); record = p;
                }
                record.SourceId = source.Id; record.PlatformRiderAccountId = accountId; record.RiderClientAssignmentId = share.AssignmentId;
                record.RiderProfileId = share.RiderProfileId; record.ImportId = batch.Id; record.ReportDate = reportDate;
                record.CaptainId = row.CaptainId; record.SourceRowNumber = row.RowNumber;
            }
        }
        if (issues.Count > 0) return Result.Success(new NinjaImportResponse(null, reportDate, 0, 0, issues));
        batch.ImportedRows = performanceSources.Count + deductionSources.Count; batch.SkippedRows = skipped;
        db.NinjaDailyImports.Add(batch); db.AddRange(performanceSources); db.AddRange(deductionSources);
        db.NinjaDailyPerformance.AddRange(performance); db.NinjaDailyDeductions.AddRange(amounts);
        return await Persist(new NinjaImportResponse(batch.Id, reportDate, batch.ImportedRows, skipped, []), ct);
    }

    public async Task<Result<NinjaPage<NinjaPerformanceResponse>>> GetPerformanceAsync(DateOnly dateFrom, DateOnly dateTo, Guid? accountId, Guid? riderId,
        int page, int pageSize, CancellationToken ct = default)
    {
        var ids = await AllowedPlatforms(PermissionKeys.Ninja.Read, ct);
        if (ids.Length == 0) return Result.Failure<NinjaPage<NinjaPerformanceResponse>>(NinjaErrors.Forbidden);
        if (!ValidPage(page, pageSize) || dateFrom == default || dateTo < dateFrom)
            return Result.Failure<NinjaPage<NinjaPerformanceResponse>>(NinjaErrors.Invalid("حدد فترة صحيحة وحجم صفحة من 1 إلى 100."));
        var rows = await (from p in db.NinjaDailyPerformance.AsNoTracking() join a in db.PlatformRiderAccounts.IgnoreQueryFilters()
            on p.PlatformRiderAccountId equals a.Id where ids.Contains(a.ClientPlatformId) && p.ReportDate >= dateFrom && p.ReportDate <= dateTo
                && (accountId == null || p.PlatformRiderAccountId == accountId) && (riderId == null || p.RiderProfileId == riderId)
            orderby p.ReportDate descending, p.Id select new NinjaPerformanceResponse(p.Id, p.PlatformRiderAccountId, p.RiderClientAssignmentId,
                p.RiderProfileId, p.ReportDate, p.CaptainId, p.TotalOrders, p.TotalWorkingHours))
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success(new NinjaPage<NinjaPerformanceResponse>(rows, page, pageSize));
    }

    public async Task<Result<NinjaPage<NinjaDeductionResponse>>> GetDeductionsAsync(DateOnly dateFrom, DateOnly dateTo, Guid? accountId, Guid? riderId,
        int page, int pageSize, CancellationToken ct = default)
    {
        var ids = await AllowedPlatforms(PermissionKeys.Ninja.Read, ct);
        if (ids.Length == 0) return Result.Failure<NinjaPage<NinjaDeductionResponse>>(NinjaErrors.Forbidden);
        if (!ValidPage(page, pageSize) || dateFrom == default || dateTo < dateFrom)
            return Result.Failure<NinjaPage<NinjaDeductionResponse>>(NinjaErrors.Invalid("حدد فترة صحيحة وحجم صفحة من 1 إلى 100."));
        var rows = await (from p in db.NinjaDailyDeductions.AsNoTracking() join a in db.PlatformRiderAccounts.IgnoreQueryFilters()
            on p.PlatformRiderAccountId equals a.Id where ids.Contains(a.ClientPlatformId) && p.ReportDate >= dateFrom && p.ReportDate <= dateTo
                && (accountId == null || p.PlatformRiderAccountId == accountId) && (riderId == null || p.RiderProfileId == riderId)
            orderby p.ReportDate descending, p.Id select new NinjaDeductionResponse(p.Id, p.PlatformRiderAccountId, p.RiderClientAssignmentId,
                p.RiderProfileId, p.ReportDate, p.CaptainId, p.TotalCollectedAmountAtDelivery, p.TotalDeductions))
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success(new NinjaPage<NinjaDeductionResponse>(rows, page, pageSize));
    }
}
