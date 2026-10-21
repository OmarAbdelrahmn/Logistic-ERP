using System.Security.Cryptography;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Infrastructure.Hr;
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
        // Recheck current rows: a later upload may have corrected data from this file.
        var parsed = ChefzSpreadsheetParser.Parse(content);
        var issues = parsed.Issues.ToList();
        if (issues.Count != 0) return Result.Success(new ChefzImportResponse(null, reportDate, 0, 0, issues));
        var reportIds = parsed.Rows.Select(r => r.IdNumber).ToArray();
        var details = await (from a in Accounts.AsNoTracking() join d in db.ChefzAccountDetails.AsNoTracking()
            on a.Id equals d.PlatformRiderAccountId where reportIds.Contains(d.ReportIdNumber) select d).ToArrayAsync(ct);
        var byId = details.ToLookup(d => d.ReportIdNumber, StringComparer.Ordinal);
        var accountIds = details.Select(d => d.PlatformRiderAccountId).ToArray();
        await using var accountScope = await PlatformAccountWriteScope.AcquireAsync(db, accountIds, ct);
        var previous = await db.ChefzPerformanceImports.AsNoTracking().SingleOrDefaultAsync(b => b.ReportDate == reportDate && b.ContentHash == hash, ct);
        var existingRows = await db.ChefzDailyPerformance.Where(p => p.ReportDate == reportDate && accountIds.Contains(p.PlatformRiderAccountId)).ToArrayAsync(ct);
        var sources = await db.Set<ChefzDailyPerformanceSource>().Where(p => p.ReportDate == reportDate && accountIds.Contains(p.PlatformRiderAccountId))
            .ToDictionaryAsync(p => p.PlatformRiderAccountId, ct);
        var batch = previous ?? new ChefzPerformanceImport { ReportDate = reportDate, FileName = Path.GetFileName(fileName), ContentHash = hash };
        var pending = new List<(ChefzDailyPerformanceSource Source, ChefzParsedRow Row, IReadOnlyList<PlatformDayShare> Shares, bool IsNew)>();
        var allocator = new PlatformAccountDayAllocationService(db);
        await allocator.Preload(accountIds, reportDate, reportDate, ct);
        var skipped = 0;
        foreach (var row in parsed.Rows)
        {
            ct.ThrowIfCancellationRequested();
            var matches = byId[row.IdNumber].ToArray();
            if (matches.Length != 1)
            { issues.Add(new(row.RowNumber, row.IdNumber, "Report ID must identify exactly one Chefz account.")); continue; }
            var accountId = matches[0].PlatformRiderAccountId;
            var allocation = await allocator.AllocateAsync(accountId, reportDate, new Dictionary<PlatformDayMetric, decimal>
            {
                [PlatformDayMetric.OrdersCount] = row.AcceptedOffers, [PlatformDayMetric.TotalValidOffers] = row.TotalValidOffers,
                [PlatformDayMetric.DeclinedOffers] = row.DeclinedOffers, [PlatformDayMetric.MissedOffers] = row.MissedOffers
            }, ct);
            if (allocation.IsFailure) { issues.Add(new(row.RowNumber, row.IdNumber, allocation.Error.Description)); continue; }
            var shares = allocation.Value!;
            if (shares.Any(a => a.Values[PlatformDayMetric.OrdersCount] > a.Values[PlatformDayMetric.TotalValidOffers]))
            { issues.Add(new(row.RowNumber, row.IdNumber, "Allocated accepted offers exceed valid offers.")); continue; }
            var isNew = !sources.TryGetValue(accountId, out var source);
            var oldRows = existingRows.Where(p => p.PlatformRiderAccountId == accountId).ToArray();
            if (!isNew && !oldRows.Select(p => p.RiderClientAssignmentId).Order().SequenceEqual(shares.Select(a => a.AssignmentId).Order()))
            { issues.Add(new(row.RowNumber, row.IdNumber, "Saved attribution differs from the assignment history; correct history before importing.")); continue; }
            if (source is not null && source.ReportIdNumber == row.IdNumber && source.TotalValidOffers == row.TotalValidOffers
                && source.AcceptedOffers == row.AcceptedOffers && source.DeclinedOffers == row.DeclinedOffers
                && source.MissedOffers == row.MissedOffers && source.AcceptanceRate == row.AcceptanceRate)
            { skipped++; continue; }
            source ??= new() { PlatformRiderAccountId = accountId, ReportDate = reportDate };
            pending.Add((source, row, shares, isNew));
        }
        if (issues.Count > 0) return Result.Success(new ChefzImportResponse(null, reportDate, 0, 0, issues));
        // Do not stage any corrections until all source rows and rider shares validate.
        var savedRows = existingRows.ToList();
        foreach (var (source, row, shares, isNew) in pending)
        {
            source.ImportId = batch.Id; source.ReportIdNumber = row.IdNumber; source.SourceRowNumber = row.RowNumber;
            source.TotalValidOffers = row.TotalValidOffers; source.AcceptedOffers = row.AcceptedOffers;
            source.DeclinedOffers = row.DeclinedOffers; source.MissedOffers = row.MissedOffers; source.AcceptanceRate = row.AcceptanceRate;
            if (isNew) db.Add(source);
            foreach (var share in shares)
            {
                var performance = savedRows.SingleOrDefault(p => p.PlatformRiderAccountId == source.PlatformRiderAccountId && p.RiderClientAssignmentId == share.AssignmentId);
                if (performance is null)
                {
                    performance = new() { PlatformRiderAccountId = source.PlatformRiderAccountId, ReportDate = reportDate,
                        RiderClientAssignmentId = share.AssignmentId, RiderProfileId = share.RiderProfileId, SourceId = source.Id };
                    db.Add(performance); savedRows.Add(performance);
                }
                performance.ImportId = batch.Id; performance.ReportIdNumber = row.IdNumber; performance.SourceRowNumber = row.RowNumber;
                performance.TotalValidOffers = checked((int)share.Values[PlatformDayMetric.TotalValidOffers]);
                performance.AcceptedOffers = checked((int)share.Values[PlatformDayMetric.OrdersCount]);
                performance.DeclinedOffers = checked((int)share.Values[PlatformDayMetric.DeclinedOffers]);
                performance.MissedOffers = checked((int)share.Values[PlatformDayMetric.MissedOffers]);
                // The original rate remains on the source; rider rates use allocated counts.
                performance.AcceptanceRate = performance.TotalValidOffers == 0 ? 0
                    : decimal.Round(100m * performance.AcceptedOffers / performance.TotalValidOffers, 2, MidpointRounding.AwayFromZero);
            }
        }
        if (previous is null)
        {
            batch.ImportedRows = pending.Count; batch.SkippedRows = skipped; db.ChefzPerformanceImports.Add(batch);
        }
        var requestedAccounts = parsed.Rows.Select(row => byId[row.IdNumber].Single().PlatformRiderAccountId).ToHashSet();
        var responses = await PerformanceResponses(savedRows.Where(p => requestedAccounts.Contains(p.PlatformRiderAccountId)).ToArray(), ct);
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
