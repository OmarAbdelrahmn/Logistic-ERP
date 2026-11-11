using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Hunger;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LogisticsERP.Application.Abstractions.Files;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService(ApplicationDbContext db, ICurrentUser user, IPermissionChecker permissions,
    TimeProvider clock, IConfiguration configuration, SimplePlatformService? lifecycle = null, IPrivateFileStorage? files = null) : IHungerQualityService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);
    private static T Read<T>(string value) => JsonSerializer.Deserialize<T>(value, Json)!;
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private sealed record Envelope(IReadOnlyList<HungerQualityInput> Inputs, IReadOnlyList<HungerImportRow> Errors);
    private sealed record Change(HungerQualityInput Input, Guid AccountId, HungerQualityCounters Daily,
        DateOnly CoverageFrom, Guid? NextId, HungerQualityCounters? NextDaily, Guid? SoleRiderId, IReadOnlyList<PlatformDayShare> Allocations);
    private sealed record Plan(string Revision, IReadOnlyList<HungerImportRow> Rows, IReadOnlyList<Change> Changes);

    private async Task<Guid[]> Platforms(string permission, CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("Features:Hunger") || user.UserId is not { } uid || user.AuthorizationVersion is not { } version) return [];
        var ids = await db.ClientPlatforms.AsNoTracking().Where(p => p.Code == "HUNGER").Select(p => p.Id).ToArrayAsync(ct);
        var allowed = new List<Guid>();
        foreach (var id in ids)
            if (await permissions.HasPermissionAsync(uid, version, permission, new(AccessScopeType.ClientPlatform, id), ct)) allowed.Add(id);
        return allowed.ToArray();
    }
    public async Task<Result<HungerFile>> TemplateAsync(CancellationToken ct = default)
    {
        if ((await Platforms(PermissionKeys.Hunger.QualityImport, ct)).Length == 0) return Result.Failure<HungerFile>(HungerErrors.Forbidden);
        using var book = new XLWorkbook(); var sheet = book.AddWorksheet("Quality");
        for (var i = 0; i < HungerQualityParser.Headers.Length; i++) sheet.Cell(1, i + 1).Value = HungerQualityParser.Headers[i];
        sheet.Column(1).Style.NumberFormat.Format = "@"; sheet.Row(1).Style.Font.Bold = true; sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream(); book.SaveAs(stream);
        return Result.Success(new HungerFile(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Hunger-Quality-MTD.xlsx"));
    }
    public async Task<Result<HungerQualityPreview>> PreviewAsync(string fileName, byte[] content, DateOnly reportDate, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.QualityImport, ct);
        if (platforms.Length == 0) return Result.Failure<HungerQualityPreview>(HungerErrors.Forbidden);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        if (reportDate == default || reportDate > today) return Result.Failure<HungerQualityPreview>(HungerErrors.Invalid("حدد تاريخ تقرير لا يتجاوز اليوم."));
        var parsed = HungerQualityParser.Parse(fileName, content);
        if (parsed.FileError is not null) return Result.Failure<HungerQualityPreview>(HungerErrors.Invalid(parsed.FileError));
        var envelope = new Envelope(parsed.Inputs, parsed.Errors);
        var plan = await Prepare(envelope, platforms, reportDate, ct);
        var preview = new HungerImportPreview { UserId = user.UserId!.Value, ReportDate = reportDate,
            FileName = Path.GetFileName(fileName), ContentHash = Convert.ToHexString(SHA256.HashData(content)),
            InputRevision = plan.Revision, RowsJson = Serialize(envelope), ExpiresAtUtc = clock.GetUtcNow().AddMinutes(30) };
        db.Add(preview); await db.SaveChangesAsync(ct);
        return Result.Success(new HungerQualityPreview(preview.Id, reportDate, preview.ExpiresAtUtc, plan.Changes.Count > 0, plan.Rows));
    }
    private async Task<Plan> Prepare(Envelope envelope, Guid[] platforms, DateOnly date, CancellationToken ct)
    {
        var codes = envelope.Inputs.Select(r => r.AccountId).Distinct().ToArray();
        var accounts = await db.PlatformRiderAccounts.AsNoTracking().Where(a => platforms.Contains(a.ClientPlatformId) && codes.Contains(a.ExternalAccountId)).OrderBy(a => a.Id).ToArrayAsync(ct);
        var ids = accounts.Select(a => a.Id).ToArray();
        var start = new DateOnly(date.Year, date.Month, 1); var end = start.AddMonths(1);
        var snapshots = await db.Set<HungerQualitySnapshot>().AsNoTracking().Where(s => ids.Contains(s.PlatformRiderAccountId) && s.ReportDate >= start && s.ReportDate < end).OrderBy(s => s.ReportDate).ThenBy(s => s.Id).ToArrayAsync(ct);
        var assignments = await db.RiderClientAssignments.AsNoTracking().Where(a => ids.Contains(a.PlatformRiderAccountId)
            && a.Status != RiderAssignmentStatus.Cancelled && a.EffectiveFrom < end && (a.EffectiveTo == null || a.EffectiveTo >= start)).OrderBy(a => a.Id).ToArrayAsync(ct);
        var usages = await db.PlatformAccountDayUsages.AsNoTracking().Where(u => ids.Contains(u.PlatformRiderAccountId) && u.BusinessDate >= start && u.BusinessDate < end).OrderBy(u => u.Id).ToArrayAsync(ct);
        var contributions = await db.Set<LogisticsERP.Domain.Entities.Clients.PlatformAccountDayContribution>().AsNoTracking().Where(u => ids.Contains(u.PlatformRiderAccountId) && u.BusinessDate >= start && u.BusinessDate < end).OrderBy(u => u.Id).ToArrayAsync(ct);
        var revision = Hash(Serialize(new { accounts, snapshots, assignments, usages, contributions }));
        var rows = envelope.Errors.ToList(); var changes = new List<Change>(); var seen = new HashSet<Guid>();
        foreach (var input in envelope.Inputs.OrderBy(r => r.RowNumber))
        {
            ct.ThrowIfCancellationRequested(); var matches = accounts.Where(a => a.ExternalAccountId == input.AccountId).ToArray();
            if (matches.Length != 1) { rows.Add(new(input.RowNumber, input.AccountId, "Rejected", "المعرف غير موجود أو غير فريد ضمن نطاق هنقر المسموح.")); continue; }
            var account = matches[0];
            if (!seen.Add(account.Id)) { rows.Add(new(input.RowNumber, input.AccountId, "Rejected", "المعرف مكرر داخل الملف.")); continue; }
            var saved = snapshots.Where(s => s.PlatformRiderAccountId == account.Id).ToArray();
            if (saved.Any(s => s.ReportDate == date)) { rows.Add(new(input.RowNumber, input.AccountId, "Skipped", "يوجد تقرير لهذا المعرف والتاريخ؛ لم يستبدل.")); continue; }
            var prior = saved.LastOrDefault(s => s.ReportDate < date); var next = saved.FirstOrDefault(s => s.ReportDate > date);
            var split = HungerQualityRules.Split(input.Counters, prior is null ? new() : Read<HungerQualityCounters>(prior.CountersJson), next is null ? null : Read<HungerQualityCounters>(next.CountersJson));
            if (split.Error is not null) { rows.Add(new(input.RowNumber, input.AccountId, "Rejected", split.Error)); continue; }
            var from = prior?.ReportDate.AddDays(1) ?? start;
            var participants = assignments.Where(a => a.PlatformRiderAccountId == account.Id && a.EffectiveFrom <= date && (a.EffectiveTo == null || a.EffectiveTo >= from)).ToArray();
            var riderIds = participants.Select(a => a.RiderProfileId).Distinct().ToArray();
            // A complete single-rider interval is safe. Shared days are resolved through the common contribution workflow.
            var sole = riderIds.Length == 1 && participants.Any(a => a.EffectiveFrom <= from && (a.EffectiveTo == null || a.EffectiveTo >= date)) ? (Guid?)riderIds[0] : null;
            var allocations = await Allocate(account.Id, from, date, split.Daily, sole, ct);
            changes.Add(new(input, account.Id, split.Daily, from, next?.Id, split.NextDaily, sole, allocations));
            rows.Add(new(input.RowNumber, input.AccountId, "New", HungerQualityRules.Rank(input.Segment) is null ? "تصنيف غير معروف؛ حفظ النص دون ترتيب رقمي." : null,
                split.Daily, next is not null, allocations.Count > 0 ? "Resolved" : "UnresolvedAllocation"));
        }
        return new(revision, rows.OrderBy(r => r.RowNumber).ToArray(), changes);
    }
    public async Task<Result<HungerImportResult>> ConfirmAsync(Guid previewId, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.QualityImport, ct);
        if (platforms.Length == 0) return Result.Failure<HungerImportResult>(HungerErrors.Forbidden);
        var preview = await db.Set<HungerImportPreview>().AsNoTracking().SingleOrDefaultAsync(p => p.Id == previewId && p.UserId == user.UserId && p.Kind == "Quality", ct);
        if (preview is null) return Result.Failure<HungerImportResult>(HungerErrors.NotFound);
        var envelope = Read<Envelope>(preview.RowsJson);
        var codes = envelope.Inputs.Select(r => r.AccountId).Distinct().ToArray();
        var ids = await db.PlatformRiderAccounts.AsNoTracking().Where(a => platforms.Contains(a.ClientPlatformId) && codes.Contains(a.ExternalAccountId)).Select(a => a.Id).ToArrayAsync(ct);
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear(); await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, ids, ct);
                var current = await db.Set<HungerImportPreview>().SingleAsync(p => p.Id == previewId, ct);
                var plan = await Prepare(envelope, platforms, current.ReportDate, ct);
                if (current.ResultJson is not null)
                {
                    // Do not replay a result after the caller loses access to an imported account.
                    var savedIds = await db.Set<HungerQualitySnapshot>().AsNoTracking().Where(s => s.ImportId == previewId).Select(s => s.PlatformRiderAccountId).ToArrayAsync(ct);
                    if (savedIds.Except(ids).Any()) return Result.Failure<HungerImportResult>(HungerErrors.Forbidden);
                    return Result.Success(Read<HungerImportResult>(current.ResultJson));
                }
                if (current.ExpiresAtUtc <= clock.GetUtcNow() || current.InputRevision != plan.Revision)
                    return Result.Failure<HungerImportResult>(HungerErrors.Conflict("انتهت المعاينة أو تغيرت البيانات؛ أنشئ معاينة جديدة."));
                foreach (var change in plan.Changes)
                {
                    var input = change.Input;
                    db.Add(new HungerQualitySnapshot { PlatformRiderAccountId = change.AccountId, ImportId = current.Id,
                        ReportDate = current.ReportDate, CoverageFrom = change.CoverageFrom, SourceRowNumber = input.RowNumber,
                        CountersJson = Serialize(input.Counters), DailyCountersJson = Serialize(change.Daily), Segment = input.Segment,
                        VerificationSuccessRate = input.VerificationSuccessRate, OnTimeDeliveryScore = input.OnTimeDeliveryScore,
                        FinalDeliveryQualityScore = input.FinalDeliveryQualityScore, SoleRiderProfileId = change.SoleRiderId,
                        AllocationStatus = change.Allocations.Count > 0 ? "Resolved" : "UnresolvedAllocation", AllocationsJson = Serialize(change.Allocations) });
                    if (change.NextId is { } nextId)
                    {
                        var next = await db.Set<HungerQualitySnapshot>().SingleAsync(s => s.Id == nextId, ct);
                        var before = Serialize(next); next.DailyCountersJson = Serialize(change.NextDaily!);
                        next.CoverageFrom = current.ReportDate.AddDays(1);
                        // Re-resolve the shorter interval, never retain attribution from the old wider interval.
                        var riders = await db.RiderClientAssignments.AsNoTracking().Where(a => a.PlatformRiderAccountId == next.PlatformRiderAccountId
                            && a.Status != RiderAssignmentStatus.Cancelled && a.EffectiveFrom <= next.ReportDate && (a.EffectiveTo == null || a.EffectiveTo >= next.CoverageFrom)).ToArrayAsync(ct);
                        var soleIds = riders.Select(a => a.RiderProfileId).Distinct().ToArray();
                        next.SoleRiderProfileId = soleIds.Length == 1 && riders.Any(a => a.EffectiveFrom <= next.CoverageFrom && (a.EffectiveTo == null || a.EffectiveTo >= next.ReportDate)) ? soleIds[0] : null;
                        var nextAllocations = await Allocate(next.PlatformRiderAccountId, next.CoverageFrom, next.ReportDate, change.NextDaily!, next.SoleRiderProfileId, ct);
                        next.AllocationsJson = Serialize(nextAllocations);
                        next.AllocationStatus = nextAllocations.Count > 0 ? "Resolved" : "UnresolvedAllocation";
                        db.Add(new HungerQualityRevision { SnapshotId = next.Id, TriggerImportId = current.Id, BeforeJson = before, AfterJson = Serialize(next) });
                    }
                }
                var result = new HungerImportResult(current.Id, plan.Rows.Count, plan.Changes.Count, plan.Rows.Count(r => r.Status == "Skipped"),
                    plan.Rows.Count(r => r.Status == "Rejected"), plan.Changes.Count(c => c.NextId.HasValue), plan.Changes.Count(c => c.Allocations.Count == 0), plan.Rows);
                current.ResultJson = Serialize(result); await db.SaveChangesAsync(ct); return Result.Success(result);
            });
        }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure<HungerImportResult>(HungerErrors.Conflict("تعارض متزامن؛ أعد المعاينة.")); }
    }
    public async Task<Result<HungerImportResult>> ImportResultAsync(Guid importId, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.QualityImport, ct);
        if (platforms.Length == 0) return Result.Failure<HungerImportResult>(HungerErrors.Forbidden);
        var item = await db.Set<HungerImportPreview>().AsNoTracking().SingleOrDefaultAsync(i => i.Id == importId && i.UserId == user.UserId && i.Kind == "Quality", ct);
        if (item?.ResultJson is null) return Result.Failure<HungerImportResult>(HungerErrors.NotFound);
        var accountPlatforms = await (from s in db.Set<HungerQualitySnapshot>().AsNoTracking() join a in db.PlatformRiderAccounts on s.PlatformRiderAccountId equals a.Id where s.ImportId == importId select a.ClientPlatformId).Distinct().ToArrayAsync(ct);
        return accountPlatforms.Except(platforms).Any() ? Result.Failure<HungerImportResult>(HungerErrors.Forbidden) : Result.Success(Read<HungerImportResult>(item.ResultJson));
    }
    public async Task<Result<HungerQualityReport>> ReportAsync(DateOnly month, Guid? accountId, Guid? riderId, int page, int pageSize, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.Read, ct);
        if (platforms.Length == 0) return Result.Failure<HungerQualityReport>(HungerErrors.Forbidden);
        if (month == default || month.Year >= 9999 || page < 1 || pageSize is < 1 or > 100) return Result.Failure<HungerQualityReport>(HungerErrors.Invalid("حدد الشهر وصفحة صحيحة وحجمًا من 1 إلى 100."));
        var start = new DateOnly(month.Year, month.Month, 1); var end = start.AddMonths(1);
        var query = from s in db.Set<HungerQualitySnapshot>().AsNoTracking() join a in db.PlatformRiderAccounts.AsNoTracking() on s.PlatformRiderAccountId equals a.Id
                    where platforms.Contains(a.ClientPlatformId) && s.ReportDate >= start && s.ReportDate < end && (accountId == null || a.Id == accountId)
                    select new { Snapshot = s, a.ExternalAccountId };
        var saved = await query.OrderBy(x => x.Snapshot.ReportDate).ThenBy(x => x.Snapshot.PlatformRiderAccountId).ToArrayAsync(ct);
        var items = saved.Select(x => new HungerQualityItem(x.Snapshot.Id, x.Snapshot.PlatformRiderAccountId, x.ExternalAccountId, x.Snapshot.ReportDate,
            x.Snapshot.CoverageFrom, Read<HungerQualityCounters>(x.Snapshot.CountersJson), Read<HungerQualityCounters>(x.Snapshot.DailyCountersJson),
            x.Snapshot.Segment, HungerQualityRules.Rank(x.Snapshot.Segment), x.Snapshot.VerificationSuccessRate, x.Snapshot.OnTimeDeliveryScore,
            x.Snapshot.FinalDeliveryQualityScore, x.Snapshot.AllocationStatus, x.Snapshot.SoleRiderProfileId)
            { Allocations = Read<PlatformDayShare[]>(x.Snapshot.AllocationsJson), RowVersion = Convert.ToBase64String(x.Snapshot.RowVersion) }).Where(i => !riderId.HasValue || i.Allocations.Any(a => a.RiderProfileId == riderId)).ToArray();
        var pageItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        var visibleAccounts = pageItems.Select(i => i.AccountId).ToHashSet();
        var latest = items.Where(i => visibleAccounts.Contains(i.AccountId)).GroupBy(i => i.AccountId).Select(g => g.MaxBy(i => i.ReportDate)!).ToArray();
        return Result.Success(new HungerQualityReport(start.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture), pageItems,
            latest, items.Aggregate(new HungerQualityCounters(), (sum, i) => sum + (riderId.HasValue
                ? i.Allocations.Where(a => a.RiderProfileId == riderId).Aggregate(new HungerQualityCounters(), (value, a) => value + FromMetrics(a.Values)) : i.Daily)), items.Length, page, pageSize));
    }

    private static Dictionary<PlatformDayMetric, decimal> Metrics(HungerQualityCounters c) => new()
    {
        [PlatformDayMetric.QualityGrossOrders] = c.GrossOrders, [PlatformDayMetric.QualityCompletedOrders] = c.CompletedOrders,
        [PlatformDayMetric.QualityCompletedOrdersInTime] = c.CompletedOrdersInTime, [PlatformDayMetric.QualityVerificationRequests] = c.TotalVerificationRequests,
        [PlatformDayMetric.QualitySuccessfulVerifications] = c.SuccessfulVerificationRequests, [PlatformDayMetric.QualityFailedOrders] = c.FailedOrdersByRider
    };
    private static HungerQualityCounters FromMetrics(IReadOnlyDictionary<PlatformDayMetric, decimal> c) => new(
        (long)c[PlatformDayMetric.QualityGrossOrders], (long)c[PlatformDayMetric.QualityCompletedOrders], (long)c[PlatformDayMetric.QualityCompletedOrdersInTime],
        (long)c[PlatformDayMetric.QualityVerificationRequests], (long)c[PlatformDayMetric.QualitySuccessfulVerifications], (long)c[PlatformDayMetric.QualityFailedOrders]);
    private async Task<IReadOnlyList<PlatformDayShare>> Allocate(Guid account, DateOnly from, DateOnly through, HungerQualityCounters values, Guid? sole, CancellationToken ct)
    {
        if (from == through)
        {
            var allocated = await new PlatformAccountDayAllocationService(db).AllocateAsync(account, through, Metrics(values), ct);
            return allocated.IsSuccess ? allocated.Value! : [];
        }
        if (sole is null) return [];
        var assignments = await db.RiderClientAssignments.AsNoTracking().Where(a => a.PlatformRiderAccountId == account && a.RiderProfileId == sole
            && a.Status != RiderAssignmentStatus.Cancelled && a.EffectiveFrom <= through && (a.EffectiveTo == null || a.EffectiveTo >= from)).OrderBy(a => a.EffectiveFrom).ThenBy(a => a.Id).ToArrayAsync(ct);
        if (assignments.Length == 0) return [];
        return [new PlatformDayShare(assignments[^1].Id, sole.Value, 1, Metrics(values)) { CoveredAssignmentIds = assignments.Select(a => a.Id).ToArray() }];
    }
}
