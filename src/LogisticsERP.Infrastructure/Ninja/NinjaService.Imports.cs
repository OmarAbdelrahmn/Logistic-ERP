using System.Security.Cryptography;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaService
{
    public async Task<Result<NinjaImportResponse>> ImportAsync(bool deductions, DateOnly reportDate, string fileName, byte[] content, CancellationToken ct = default)
    {
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
        var assignments = await db.RiderClientAssignments.AsNoTracking().Where(a => accountIds.Contains(a.PlatformRiderAccountId)
            && (a.Status == RiderAssignmentStatus.Active || a.Status == RiderAssignmentStatus.Ended)
            && a.EffectiveFrom <= reportDate && (a.EffectiveTo == null || a.EffectiveTo >= reportDate)).ToArrayAsync(ct);
        var byAccount = assignments.ToLookup(a => a.PlatformRiderAccountId);
        var hash = Convert.ToHexString(SHA256.HashData(content));
        var previous = await db.NinjaDailyImports.AsNoTracking().SingleOrDefaultAsync(i => i.IsDeductions == deductions
            && i.ReportDate == reportDate && i.ContentHash == hash, ct);
        var oldPerformance = deductions ? new Dictionary<Guid, NinjaDailyPerformance>() : await db.NinjaDailyPerformance.AsNoTracking()
            .Where(p => p.ReportDate == reportDate && accountIds.Contains(p.PlatformRiderAccountId)).ToDictionaryAsync(p => p.PlatformRiderAccountId, ct);
        var oldDeductions = !deductions ? new Dictionary<Guid, NinjaDailyDeduction>() : await db.NinjaDailyDeductions.AsNoTracking()
            .Where(p => p.ReportDate == reportDate && accountIds.Contains(p.PlatformRiderAccountId)).ToDictionaryAsync(p => p.PlatformRiderAccountId, ct);
        var batch = new NinjaDailyImport { ReportDate = reportDate, IsDeductions = deductions, FileName = Path.GetFileName(fileName), ContentHash = hash };
        var performance = new List<NinjaDailyPerformance>(); var amounts = new List<NinjaDailyDeduction>();
        var issues = new List<NinjaImportIssue>(); var skipped = 0;
        foreach (var row in parsed.Rows)
        {
            ct.ThrowIfCancellationRequested();
            var matches = byCaptain[row.CaptainId].ToArray();
            if (matches.Length != 1) { issues.Add(new(row.RowNumber, row.CaptainId, "Captain ID لا يرتبط بحساب نينجا واحد ضمن النطاق المسموح.")); continue; }
            var accountId = matches[0].Id; var riders = byAccount[accountId].ToArray();
            if (riders.Length != 1) { issues.Add(new(row.RowNumber, row.CaptainId, "يجب وجود تكليف واحد للمندوب بتاريخ التقرير؛ لا يتم استخدام صاحب الحساب.")); continue; }
            var a = riders[0];
            if (!deductions && oldPerformance.TryGetValue(accountId, out var old))
            {
                if (old.RiderClientAssignmentId == a.Id && old.RiderProfileId == a.RiderProfileId
                    && old.TotalOrders == row.TotalOrders && old.TotalWorkingHours == row.WorkingHours) skipped++;
                else issues.Add(new(row.RowNumber, row.CaptainId, "يوجد أداء محفوظ لهذا الحساب واليوم بقيم أو تكليف مختلف؛ لم يتم استبداله."));
                continue;
            }
            if (deductions && oldDeductions.TryGetValue(accountId, out var oldAmount))
            {
                if (oldAmount.RiderClientAssignmentId == a.Id && oldAmount.RiderProfileId == a.RiderProfileId
                    && oldAmount.TotalCollectedAmountAtDelivery == row.CollectedAmount && oldAmount.TotalDeductions == row.Deductions) skipped++;
                else issues.Add(new(row.RowNumber, row.CaptainId, "توجد خصومات محفوظة لهذا الحساب واليوم بقيم أو تكليف مختلف؛ لم يتم استبدالها."));
                continue;
            }
            NinjaDailyRecord record;
            if (deductions) { var d = new NinjaDailyDeduction { TotalCollectedAmountAtDelivery = row.CollectedAmount, TotalDeductions = row.Deductions }; amounts.Add(d); record = d; }
            else { var p = new NinjaDailyPerformance { TotalOrders = row.TotalOrders, TotalWorkingHours = row.WorkingHours }; performance.Add(p); record = p; }
            record.PlatformRiderAccountId = accountId; record.RiderClientAssignmentId = a.Id; record.RiderProfileId = a.RiderProfileId;
            record.ImportId = batch.Id; record.ReportDate = reportDate; record.CaptainId = row.CaptainId; record.SourceRowNumber = row.RowNumber;
        }
        if (issues.Count > 0) return Result.Success(new NinjaImportResponse(null, reportDate, 0, 0, issues));
        if (previous is not null) return Result.Success(new NinjaImportResponse(previous.Id, reportDate, 0, skipped, []));
        batch.ImportedRows = performance.Count + amounts.Count; batch.SkippedRows = skipped;
        db.NinjaDailyImports.Add(batch); db.NinjaDailyPerformance.AddRange(performance); db.NinjaDailyDeductions.AddRange(amounts);
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
