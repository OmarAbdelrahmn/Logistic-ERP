using System.Security.Cryptography;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Chefz;

internal sealed partial class ChefzService
{
    private static ChefzPerformanceResponse PerformanceResponse(ChefzDailyPerformance p) => new(p.Id,
        p.PlatformRiderAccountId, p.RiderClientAssignmentId, p.RiderProfileId, p.ReportDate, p.ReportIdNumber,
        p.TotalValidOffers, p.AcceptedOffers, p.DeclinedOffers, p.MissedOffers, p.AcceptanceRate);

    private async Task<ChefzPerformanceResponse[]> PerformanceResponses(ChefzDailyPerformance[] rows, CancellationToken ct)
    {
        if (rows.Length == 0) return [];
        var accountIds = rows.Select(p => p.PlatformRiderAccountId).Distinct().ToArray();
        var accounts = await db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking().Where(a => accountIds.Contains(a.Id))
            .Select(a => new { a.Id, a.ExternalAccountId }).ToDictionaryAsync(a => a.Id, ct);
        var riders = await RiderIdentities(rows.Select(p => p.RiderProfileId), ct);
        return rows.Select(p => PerformanceResponse(p) with
        {
            ExternalAccountId = accounts.GetValueOrDefault(p.PlatformRiderAccountId)?.ExternalAccountId ?? p.PlatformRiderAccountId.ToString(),
            RiderName = riders.GetValueOrDefault(p.RiderProfileId)?.RiderName ?? "مندوب سابق",
            RiderIqamaNo = riders.GetValueOrDefault(p.RiderProfileId)?.RiderIqamaNo
        }).ToArray();
    }

    public async Task<Result<ChefzImportResponse>> ImportPerformanceAsync(DateOnly reportDate, string fileName, byte[] content, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.PerformanceImport, ct)) return Result.Failure<ChefzImportResponse>(ChefzErrors.Forbidden);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        if (reportDate == default || reportDate > today || string.IsNullOrWhiteSpace(fileName)
            || fileName.Length > 255 || !fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            || content.Length is 0 or > ChefzSpreadsheetParser.MaxBytes)
            return Result.Failure<ChefzImportResponse>(ChefzErrors.Invalid("حدد تاريخ التقرير وارفع ملف xlsx بحد أقصى 10 ميجابايت."));
        var hash = Convert.ToHexString(SHA256.HashData(content));
        var previous = await db.ChefzPerformanceImports.AsNoTracking().SingleOrDefaultAsync(b => b.ReportDate == reportDate && b.ContentHash == hash, ct);
        // Recheck current rows: a later upload may have corrected data from this file.
        var parsed = ChefzSpreadsheetParser.Parse(content);
        var issues = parsed.Issues.ToList();
        if (issues.Count != 0) return Result.Success(new ChefzImportResponse(null, reportDate, 0, 0, issues));
        var details = await (from a in Accounts.AsNoTracking() join d in db.ChefzAccountDetails.AsNoTracking()
            on a.Id equals d.PlatformRiderAccountId select d).ToArrayAsync(ct);
        var byId = details.ToLookup(d => d.ReportIdNumber, StringComparer.Ordinal);
        var assignments = await (from a in db.RiderClientAssignments.AsNoTracking() join account in Accounts on a.PlatformRiderAccountId equals account.Id
            where (a.Status == RiderAssignmentStatus.Active || a.Status == RiderAssignmentStatus.Ended)
                && a.EffectiveFrom <= reportDate && (a.EffectiveTo == null || a.EffectiveTo >= reportDate) select a).ToArrayAsync(ct);
        var byAccount = assignments.ToLookup(a => a.PlatformRiderAccountId);
        var existingRows = await db.ChefzDailyPerformance.Where(p => p.ReportDate == reportDate).ToDictionaryAsync(p => p.PlatformRiderAccountId, ct);
        var batch = previous ?? new ChefzPerformanceImport { ReportDate = reportDate, FileName = Path.GetFileName(fileName), ContentHash = hash };
        var pending = new List<(ChefzDailyPerformance Performance, ChefzParsedRow Row, RiderClientAssignment Assignment, bool IsNew)>();
        var skipped = 0;
        foreach (var row in parsed.Rows)
        {
            ct.ThrowIfCancellationRequested();
            var matches = byId[row.IdNumber].ToArray();
            if (matches.Length != 1)
            { issues.Add(new(row.RowNumber, row.IdNumber, "رقم الهوية غير مرتبط بحساب شيفز واحد؛ أضف تفاصيل الحساب أولاً.")); continue; }
            var accountId = matches[0].PlatformRiderAccountId;
            var riders = byAccount[accountId].ToArray();
            if (riders.Length != 1)
            { issues.Add(new(row.RowNumber, row.IdNumber, "يجب وجود تكليف واحد للمندوب بتاريخ التقرير.")); continue; }
            var assignment = riders[0];
            if (existingRows.TryGetValue(accountId, out var old))
            {
                if (old.RiderClientAssignmentId == assignment.Id && old.RiderProfileId == assignment.RiderProfileId
                    && old.ReportIdNumber == row.IdNumber && old.TotalValidOffers == row.TotalValidOffers
                    && old.AcceptedOffers == row.AcceptedOffers && old.DeclinedOffers == row.DeclinedOffers
                    && old.MissedOffers == row.MissedOffers && old.AcceptanceRate == row.AcceptanceRate)
                    skipped++;
                else pending.Add((old, row, assignment, false));
                continue;
            }
            pending.Add((new ChefzDailyPerformance
            {
                PlatformRiderAccountId = accountId, ReportDate = reportDate
            }, row, assignment, true));
        }
        if (issues.Count > 0) return Result.Success(new ChefzImportResponse(null, reportDate, 0, 0, issues));
        // Stage changes only after every row validates, so invalid files leave no tracked corrections.
        foreach (var (performance, row, assignment, isNew) in pending)
        {
            performance.RiderClientAssignmentId = assignment.Id;
            performance.RiderProfileId = assignment.RiderProfileId;
            performance.ImportId = batch.Id;
            performance.ReportIdNumber = row.IdNumber;
            performance.SourceRowNumber = row.RowNumber;
            performance.TotalValidOffers = row.TotalValidOffers;
            performance.AcceptedOffers = row.AcceptedOffers;
            performance.DeclinedOffers = row.DeclinedOffers;
            performance.MissedOffers = row.MissedOffers;
            performance.AcceptanceRate = row.AcceptanceRate;
            if (isNew) db.ChefzDailyPerformance.Add(performance);
        }
        if (previous is null)
        {
            batch.ImportedRows = pending.Count;
            batch.SkippedRows = skipped;
            db.ChefzPerformanceImports.Add(batch);
        }
        foreach (var change in pending) existingRows[change.Performance.PlatformRiderAccountId] = change.Performance;
        var reportRows = parsed.Rows.Select(row => existingRows[byId[row.IdNumber].Single().PlatformRiderAccountId]).ToArray();
        var responses = await PerformanceResponses(reportRows, ct);
        var riderGroups = responses.GroupBy(p => p.RiderProfileId).Select(g => new ChefzPerformanceRider(g.Key, g.First().RiderName,
            g.OrderBy(p => p.ExternalAccountId, StringComparer.Ordinal).ThenBy(p => p.AccountId).ToArray())
            { RiderIqamaNo = g.First().RiderIqamaNo })
            .OrderBy(r => r.RiderName, StringComparer.Ordinal).ThenBy(r => r.RiderProfileId).ToArray();
        return await Persist(new ChefzImportResponse(batch.Id, reportDate, pending.Count, skipped, []) { Riders = riderGroups }, ct);
    }

    public async Task<Result<ChefzPage<ChefzPerformanceResponse>>> GetPerformanceAsync(Guid? accountId, Guid? riderId,
        DateOnly from, DateOnly to, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct)) return Result.Failure<ChefzPage<ChefzPerformanceResponse>>(ChefzErrors.Forbidden);
        if (!ValidPage(page, pageSize) || from == default || to < from)
            return Result.Failure<ChefzPage<ChefzPerformanceResponse>>(ChefzErrors.Invalid("حدد فترة صحيحة وحجم صفحة من 1 إلى 100."));
        var rows = await db.ChefzDailyPerformance.AsNoTracking()
            .Where(p => p.ReportDate >= from && p.ReportDate <= to && (accountId == null || p.PlatformRiderAccountId == accountId)
                && (riderId == null || p.RiderProfileId == riderId)).OrderByDescending(p => p.ReportDate).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success(new ChefzPage<ChefzPerformanceResponse>(await PerformanceResponses(rows, ct), page, pageSize));
    }
}
