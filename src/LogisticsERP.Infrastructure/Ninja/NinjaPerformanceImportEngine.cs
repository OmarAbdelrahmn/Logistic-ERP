using System.Text.Json;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

/// <summary>Plans all dates without staging tracked entities or saving. The caller owns locks and the single commit.</summary>
internal static class NinjaPerformanceImportEngine
{
    internal sealed record Plan(string InputRevision, IReadOnlyList<NinjaPerformancePreviewRow> Rows,
        IReadOnlyList<NinjaDailyImport> Imports, IReadOnlyList<NinjaDailyPerformanceSource> Sources,
        IReadOnlyList<NinjaDailyPerformance> Shares, IReadOnlyList<NinjaAllocationProvenance> Provenance,
        IReadOnlyList<NinjaImportResponse> Results)
    {
        public bool Valid => Rows.All(r => r.Issues.Count == 0);
        public void Stage(ApplicationDbContext db) { db.AddRange(Imports); db.AddRange(Sources); db.AddRange(Shares); db.AddRange(Provenance); }
    }
    internal static async Task<Guid[]> AccountIds(ApplicationDbContext db, Guid[] platforms, IReadOnlyList<NinjaPerformanceInput> rows, CancellationToken ct)
    {
        var captains = rows.Select(r => r.CaptainId).Distinct().ToArray();
        return await db.PlatformRiderAccounts.AsNoTracking().Where(a => platforms.Contains(a.ClientPlatformId) && captains.Contains(a.ExternalAccountId)).Select(a => a.Id).ToArrayAsync(ct);
    }
    internal static async Task<Plan> Prepare(ApplicationDbContext db, Guid[] platforms, IReadOnlyList<NinjaPerformanceInput> input,
        string name, string hash, CancellationToken ct)
    {
        var captains = input.Select(r => r.CaptainId).Distinct().ToArray();
        var accounts = await db.PlatformRiderAccounts.AsNoTracking().Where(a => platforms.Contains(a.ClientPlatformId) && captains.Contains(a.ExternalAccountId)).OrderBy(a => a.Id).ToArrayAsync(ct);
        var byCaptain = accounts.ToLookup(a => a.ExternalAccountId, StringComparer.Ordinal); var ids = accounts.Select(a => a.Id).ToArray();
        var from = input.Min(r => r.Date); var to = input.Max(r => r.Date);
        var assignments = await db.RiderClientAssignments.IgnoreQueryFilters().AsNoTracking().Where(a => ids.Contains(a.PlatformRiderAccountId)
            && a.EffectiveFrom <= to && (a.EffectiveTo == null || a.EffectiveTo >= from)).OrderBy(a => a.Id).ToArrayAsync(ct);
        var usages = await db.PlatformAccountDayUsages.AsNoTracking().Where(u => ids.Contains(u.PlatformRiderAccountId) && u.BusinessDate >= from && u.BusinessDate <= to).OrderBy(u => u.Id).ToArrayAsync(ct);
        var sessions = await db.Set<LogisticsERP.Domain.Entities.Jahez.JahezAccountSession>().AsNoTracking()
            .Where(s => assignments.Select(a => a.Id).Contains(s.RiderClientAssignmentId)).OrderBy(s => s.Id).ToArrayAsync(ct);
        var responsibilities = await db.Set<NinjaResponsibilityRevision>().AsNoTracking().Where(r => ids.Contains(r.PlatformRiderAccountId)
            && r.Month >= new DateOnly(from.Year, from.Month, 1) && r.Month <= new DateOnly(to.Year, to.Month, 1)).OrderBy(r => r.Id).ToArrayAsync(ct);
        var contexts = await db.Set<NinjaEvaluationContextRevision>().AsNoTracking().Where(c => ids.Contains(c.PlatformRiderAccountId)).OrderBy(c => c.Id).ToArrayAsync(ct);
        var old = await db.Set<NinjaDailyPerformanceSource>().AsNoTracking().Where(p => ids.Contains(p.PlatformRiderAccountId) && p.ReportDate >= from && p.ReportDate <= to).ToArrayAsync(ct);
        var existingImports = await db.NinjaDailyImports.AsNoTracking().Where(i => !i.IsDeductions && i.ContentHash == hash && i.ReportDate >= from && i.ReportDate <= to).ToArrayAsync(ct);
        // Existing performance is deliberately excluded: an independent identical import must remain idempotent.
        var signature = NinjaEvaluationService.Hash(JsonSerializer.Serialize(new { accounts, assignments, usages, sessions, responsibilities, contexts,
            Policy = LogisticsERP.Domain.Ninja.NinjaSuggestionCalculator.PolicyVersion }, NinjaEvaluationService.Json));
        var allocator = new PlatformAccountDayAllocationService(db); await allocator.Preload(ids, from, to, ct);
        var rows = new List<NinjaPerformancePreviewRow>(); var batches = new List<NinjaDailyImport>();
        var sources = new List<NinjaDailyPerformanceSource>(); var shares = new List<NinjaDailyPerformance>();
        var origins = new List<NinjaAllocationProvenance>(); var results = new List<NinjaImportResponse>();
        foreach (var day in input.GroupBy(r => r.Date).OrderBy(g => g.Key))
        {
            var previousBatch = existingImports.SingleOrDefault(i => i.ReportDate == day.Key);
            var batch = previousBatch ?? new NinjaDailyImport { ReportDate = day.Key, FileName = Path.GetFileName(name), ContentHash = hash };
            var skipped = 0; var newCount = 0;
            foreach (var row in day)
            {
                ct.ThrowIfCancellationRequested(); var matches = byCaptain[row.CaptainId].ToArray();
                if (matches.Length != 1) { rows.Add(new(row.RowNumber, row.CaptainId, row.Date, null, row.Orders, row.Hours, "UnknownAccount", ["Captain ID must identify one permitted account."])); continue; }
                var account = matches[0].Id;
                var saved = old.SingleOrDefault(p => p.PlatformRiderAccountId == account && p.ReportDate == row.Date);
                if (saved is not null)
                {
                    var identical = saved.TotalOrders == row.Orders && saved.TotalWorkingHours == row.Hours;
                    rows.Add(new(row.RowNumber, row.CaptainId, row.Date, account, row.Orders, row.Hours, identical ? "Identical" : "Conflict",
                        identical ? [] : ["Different performance is already saved for this account/day; it was not replaced."]));
                    if (identical) skipped++;
                    continue;
                }
                var allocation = await allocator.AllocateAsync(account, row.Date,
                    new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = row.Orders, [PlatformDayMetric.WorkingHours] = row.Hours }, ct);
                if (allocation.IsFailure) { rows.Add(new(row.RowNumber, row.CaptainId, row.Date, account, row.Orders, row.Hours, "UnresolvedAllocation", [allocation.Error.Description])); continue; }
                if (previousBatch is not null) { rows.Add(new(row.RowNumber, row.CaptainId, row.Date, account, row.Orders, row.Hours, "Conflict", ["The saved batch no longer matches its original scope."])); continue; }
                rows.Add(new(row.RowNumber, row.CaptainId, row.Date, account, row.Orders, row.Hours, "New", []));
                var source = new NinjaDailyPerformanceSource { PlatformRiderAccountId = account, ImportId = batch.Id, ReportDate = row.Date,
                    CaptainId = row.CaptainId, SourceRowNumber = row.RowNumber, TotalOrders = row.Orders, TotalWorkingHours = row.Hours };
                sources.Add(source); newCount++;
                var allocated = allocation.Value!;
                foreach (var share in allocated)
                {
                    shares.Add(new NinjaDailyPerformance { SourceId = source.Id, PlatformRiderAccountId = account,
                        RiderClientAssignmentId = share.AssignmentId, RiderProfileId = share.RiderProfileId, ImportId = batch.Id,
                        ReportDate = row.Date, CaptainId = row.CaptainId, SourceRowNumber = row.RowNumber,
                        TotalOrders = checked((int)share.Values[PlatformDayMetric.OrdersCount]), TotalWorkingHours = share.Values[PlatformDayMetric.WorkingHours] });
                    origins.Add(new NinjaAllocationProvenance { SourceId = source.Id, RiderClientAssignmentId = share.AssignmentId,
                        Origin = allocated.Count == 1 ? "Reported" : share.AssignmentId == allocated[^1].AssignmentId ? "Remainder" : "Declared",
                        EvidenceReference = $"Import:{batch.Id:D}/Row:{row.RowNumber}" });
                }
            }
            if (previousBatch is null) { batch.ImportedRows = newCount; batch.SkippedRows = skipped; batches.Add(batch); }
            results.Add(new(batch.Id, day.Key, newCount, skipped, []));
        }
        return new(signature, rows, batches, sources, shares, origins, results);
    }
}
