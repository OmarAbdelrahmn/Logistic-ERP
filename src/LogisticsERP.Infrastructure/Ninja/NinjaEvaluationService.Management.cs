using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaEvaluationService
{
    public async Task<Result<NinjaEvaluationDayDetail>> DayAsync(Guid accountId, DateOnly businessDate, CancellationToken ct = default)
    {
        var date = businessDate;
        var result = await Report(new() { Year = date.Year, Month = date.Month, AccountId = accountId, View = "accounts" }, true, ct);
        if (result.IsFailure) return Result.Failure<NinjaEvaluationDayDetail>(result.Error);
        var riders = await Report(new() { Year = date.Year, Month = date.Month, AccountId = accountId }, true, ct);
        if (riders.IsFailure) return Result.Failure<NinjaEvaluationDayDetail>(riders.Error);
        return Result.Success(new NinjaEvaluationDayDetail(result.Value!.Items.SelectMany(r => r.Days).SingleOrDefault(d => d.Date == date),
            riders.Value!.Items.SelectMany(r => r.Days).Where(d => d.Date == date).ToArray()));
    }
    public async Task<Result<NinjaEvaluationContextResponse>> ContextAsync(Guid accountId, CancellationToken ct = default)
    {
        if (await Access(accountId, PermissionKeys.Ninja.Read, ct) is { } error) return Result.Failure<NinjaEvaluationContextResponse>(error);
        return Result.Success(ContextResponse(accountId, await db.Set<NinjaEvaluationContextRevision>().AsNoTracking()
            .Where(c => c.PlatformRiderAccountId == accountId).OrderByDescending(c => c.Revision).FirstOrDefaultAsync(ct)));
    }
    private static NinjaEvaluationContextResponse ContextResponse(Guid account, NinjaEvaluationContextRevision? revision) =>
        new(account, revision?.Id, revision?.Revision ?? 0, revision?.ConfirmedFirstDeliveryDate,
            revision is null ? [] : JsonSerializer.Deserialize<NinjaPricingPeriod[]>(revision.PricingPeriodsJson, Json)!);

    public async Task<Result<NinjaEvaluationContextResponse>> SetContextAsync(Guid accountId, NinjaEvaluationContextRequest request, CancellationToken ct = default)
    {
        if (await Access(accountId, PermissionKeys.Ninja.EvaluationManage, ct) is { } error) return Result.Failure<NinjaEvaluationContextResponse>(error);
        if (!ReasonValid(request.Reason) || request.ConfirmedFirstDeliveryDate == default(DateOnly) || request.ConfirmedFirstDeliveryDate > Today || request.PricingPeriods is null || request.PricingPeriods.Count > 100
            || request.PricingPeriods.Any(p => p.From == default || p.To < p.From || p.PricingType is not ("CAR" or "BIKE")))
            return Result.Failure<NinjaEvaluationContextResponse>(Invalid("حدد التسعير والفترات وسبب المراجعة."));
        var ordered = request.PricingPeriods.OrderBy(p => p.From).ToArray();
        if (ordered.Zip(ordered.Skip(1)).Any(p => p.First.To >= p.Second.From)) return Result.Failure<NinjaEvaluationContextResponse>(Invalid("فترات التسعير لا تتداخل."));
        return await Write([accountId], async () =>
        {
            var previous = await db.Set<NinjaEvaluationContextRevision>().Where(c => c.PlatformRiderAccountId == accountId).OrderByDescending(c => c.Revision).FirstOrDefaultAsync(ct);
            if (previous?.Id != request.ExpectedRevisionId) return Result.Failure<NinjaEvaluationContextResponse>(NinjaErrors.Conflict);
            if (await FrozenReview(accountId, null, request.ReviewedSnapshotId, ct) is { } frozen) return Result.Failure<NinjaEvaluationContextResponse>(frozen);
            if (request.ConfirmedFirstDeliveryDate is { } first && await db.Set<NinjaDailyPerformanceSource>().AnyAsync(s =>
                s.PlatformRiderAccountId == accountId && s.ReportDate < first && s.TotalOrders > 0, ct))
                return Result.Failure<NinjaEvaluationContextResponse>(Invalid("تاريخ أول توصيلة يتعارض مع أداء موجب محفوظ قبله."));
            var revision = new NinjaEvaluationContextRevision { PlatformRiderAccountId = accountId, Revision = (previous?.Revision ?? 0) + 1,
                SupersedesId = previous?.Id, ConfirmedFirstDeliveryDate = request.ConfirmedFirstDeliveryDate,
                PricingPeriodsJson = JsonSerializer.Serialize(ordered, Json), Reason = request.Reason.Trim(), ReviewedSnapshotId = request.ReviewedSnapshotId };
            db.Add(revision); await db.SaveChangesAsync(ct);
            return Result.Success(ContextResponse(accountId, revision));
        }, ct);
    }
    public async Task<Result<NinjaResponsibilityResponse>> ResponsibilityAsync(Guid accountId, int year, int month, CancellationToken ct = default)
    {
        if (await Access(accountId, PermissionKeys.Ninja.Read, ct) is { } error) return Result.Failure<NinjaResponsibilityResponse>(error);
        if (!MonthValid(year, month)) return Result.Failure<NinjaResponsibilityResponse>(Invalid("حدد شهراً صحيحاً."));
        return Result.Success(await ResponsibilityResponse(accountId, new(year, month, 1), ct));
    }
    private async Task<NinjaResponsibilityResponse> ResponsibilityResponse(Guid account, DateOnly month, CancellationToken ct)
    {
        var revision = await db.Set<NinjaResponsibilityRevision>().AsNoTracking().Where(r => r.PlatformRiderAccountId == account && r.Month == month)
            .OrderByDescending(r => r.Revision).FirstOrDefaultAsync(ct);
        var lines = revision is null ? [] : await db.Set<NinjaResponsibilityLine>().AsNoTracking().Where(l => l.RevisionId == revision.Id)
            .OrderBy(l => l.BusinessDate).ThenBy(l => l.RiderClientAssignmentId).Select(l => new NinjaResponsibilityInput(l.RiderClientAssignmentId, l.BusinessDate, l.Weight)).ToArrayAsync(ct);
        return new(account, month.Year, month.Month, revision?.Id, revision?.Revision ?? 0, lines);
    }
    public async Task<Result<NinjaResponsibilityResponse>> SetResponsibilityAsync(Guid accountId, NinjaResponsibilityRequest request, CancellationToken ct = default)
    {
        if (await Access(accountId, PermissionKeys.Ninja.EvaluationManage, ct) is { } error) return Result.Failure<NinjaResponsibilityResponse>(error);
        if (!MonthValid(request.Year, request.Month) || !ReasonValid(request.Reason) || request.Lines is null || request.Lines.Count > 3100
            || request.Lines.Any(l => l.Weight is < 0 or > 1 || decimal.Round(l.Weight, 8) != l.Weight || l.Date.Year != request.Year || l.Date.Month != request.Month)
            || request.Lines.Select(l => (l.Date, l.AssignmentId)).Distinct().Count() != request.Lines.Count)
            return Result.Failure<NinjaResponsibilityResponse>(Invalid("حدد أوزان المسؤولية اليومية وسبب المراجعة."));
        var month = new DateOnly(request.Year, request.Month, 1);
        return await Write([accountId], async () =>
        {
            var previous = await db.Set<NinjaResponsibilityRevision>().Where(r => r.PlatformRiderAccountId == accountId && r.Month == month).OrderByDescending(r => r.Revision).FirstOrDefaultAsync(ct);
            if (previous?.Id != request.ExpectedRevisionId) return Result.Failure<NinjaResponsibilityResponse>(NinjaErrors.Conflict);
            if (await FrozenReview(accountId, month, request.ReviewedSnapshotId, ct) is { } frozen) return Result.Failure<NinjaResponsibilityResponse>(frozen);
            var assignments = await db.RiderClientAssignments.IgnoreQueryFilters().AsNoTracking().Where(a => a.PlatformRiderAccountId == accountId
                && (a.Status == RiderAssignmentStatus.Active || a.Status == RiderAssignmentStatus.Ended)
                && a.EffectiveFrom < month.AddMonths(1) && (a.EffectiveTo == null || a.EffectiveTo >= month)).ToArrayAsync(ct);
            foreach (var day in request.Lines.GroupBy(l => l.Date))
            {
                var holders = assignments.Where(a => a.EffectiveFrom <= day.Key && (a.EffectiveTo == null || a.EffectiveTo >= day.Key)).Select(a => a.Id).Order().ToArray();
                if (day.Sum(l => l.Weight) != 1 || !holders.SequenceEqual(day.Select(l => l.AssignmentId).Order()))
                    return Result.Failure<NinjaResponsibilityResponse>(Invalid("كل يوم محدد يتضمن جميع التكليفات المؤهلة مرة واحدة ومجموع الأوزان يساوي 1."));
            }
            var revision = new NinjaResponsibilityRevision { PlatformRiderAccountId = accountId, Month = month, Revision = (previous?.Revision ?? 0) + 1,
                SupersedesId = previous?.Id, Reason = request.Reason.Trim(), ReviewedSnapshotId = request.ReviewedSnapshotId };
            db.Add(revision); db.AddRange(request.Lines.Select(l => new NinjaResponsibilityLine { RevisionId = revision.Id,
                RiderClientAssignmentId = l.AssignmentId, BusinessDate = l.Date, Weight = l.Weight }));
            await db.SaveChangesAsync(ct);
            return Result.Success(await ResponsibilityResponse(accountId, month, ct));
        }, ct);
    }

    private async Task<OperationError?> FrozenReview(Guid account, DateOnly? month, Guid? reviewed, CancellationToken ct)
    {
        var snapshots = await db.Set<NinjaEvaluationSnapshot>().AsNoTracking().Where(s => s.Frozen).OrderByDescending(s => s.CreatedAtUtc).ThenByDescending(s => s.Id).ToArrayAsync(ct);
        var latest = snapshots.FirstOrDefault(s => JsonSerializer.Deserialize<Guid[]>(s.AccountIdsJson, Json)!.Contains(account)
            && (month is null || JsonSerializer.Deserialize<NinjaEvaluationQuery>(s.QueryJson, Json) is { } q && q.Year == month.Value.Year && q.Month == month.Value.Month));
        return latest is not null && reviewed != latest.Id ? Conflict("ninja.frozen_review_required", "حدد آخر نسخة مجمدة وسبب مراجعة لإنشاء إصدار جديد.") : null;
    }

    private async Task<Result<T>> Write<T>(Guid[] accounts, Func<Task<Result<T>>> operation, CancellationToken ct)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, accounts, ct);
                return await operation();
            });
        }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure<T>(NinjaErrors.Conflict); }
    }

    public async Task<Result<NinjaSnapshotResponse>> SnapshotAsync(NinjaSnapshotRequest request, CancellationToken ct = default)
    {
        if ((await Platforms(PermissionKeys.Ninja.EvaluationManage, ct)).Length == 0) return Result.Failure<NinjaSnapshotResponse>(NinjaErrors.Forbidden);
        if (request.Query is null || !ReasonValid(request.Reason)) return Result.Failure<NinjaSnapshotResponse>(Invalid("حدد نطاق التقييم وسبب حفظ النسخة."));
        var first = await Report(request.Query, true, ct);
        if (first.IsFailure) return Result.Failure<NinjaSnapshotResponse>(first.Error);
        // Lock the entire authorized roster, including empty accounts, so assignment additions cannot change snapshot scope.
        var platformIds = await Platforms(PermissionKeys.Ninja.Read, ct);
        var accounts = await db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking().Where(a => platformIds.Contains(a.ClientPlatformId)).Select(a => a.Id).ToArrayAsync(ct);
        return await Write(accounts, async () =>
        {
            var result = await Report(request.Query, true, ct);
            if (result.IsFailure) return Result.Failure<NinjaSnapshotResponse>(result.Error);
            var report = result.Value!;
            if (request.ExpectedInputRevision is not null && request.ExpectedInputRevision != report.InputRevision) return Result.Failure<NinjaSnapshotResponse>(NinjaErrors.Conflict);
            if (request.SupersedesId is { } oldId)
            {
                var prior = await GetSnapshotAsync(oldId, ct);
                if (prior.IsFailure) return Result.Failure<NinjaSnapshotResponse>(prior.Error);
                if (prior.Value!.Report.Year != report.Year || prior.Value.Report.Month != report.Month
                    || Hash(JsonSerializer.Serialize(request.Query with { Page = 1, PageSize = 100 }, Json)) !=
                       Hash(JsonSerializer.Serialize(JsonSerializer.Deserialize<NinjaEvaluationQuery>((await db.Set<NinjaEvaluationSnapshot>().AsNoTracking().SingleAsync(s => s.Id == oldId, ct)).QueryJson, Json)! with { Page = 1, PageSize = 100 }, Json)))
                    return Result.Failure<NinjaSnapshotResponse>(Invalid("النسخة المصححة يجب أن تحتفظ بنطاق النسخة السابقة."));
            }
            var snapshot = new NinjaEvaluationSnapshot { PolicyVersion = report.PolicyVersion, InputRevision = report.InputRevision,
                QueryJson = JsonSerializer.Serialize(request.Query, Json), AccountIdsJson = JsonSerializer.Serialize(report.Items.SelectMany(r => r.AccountIds).Distinct().Order(), Json),
                ResultJson = JsonSerializer.Serialize(report, Json), Reason = request.Reason.Trim(), Frozen = request.Freeze, SupersedesId = request.SupersedesId };
            db.Add(snapshot); await db.SaveChangesAsync(ct);
            return Result.Success(new NinjaSnapshotResponse(snapshot.Id, snapshot.Frozen, snapshot.SupersedesId, snapshot.CreatedAtUtc, snapshot.Reason, report));
        }, ct);
    }
    public async Task<Result<NinjaSnapshotResponse>> GetSnapshotAsync(Guid id, CancellationToken ct = default)
    {
        if ((await Platforms(PermissionKeys.Ninja.Read, ct)).Length == 0) return Result.Failure<NinjaSnapshotResponse>(NinjaErrors.Forbidden);
        var snapshot = await db.Set<NinjaEvaluationSnapshot>().AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, ct);
        if (snapshot is null) return Result.Failure<NinjaSnapshotResponse>(NinjaErrors.NotFound);
        foreach (var account in JsonSerializer.Deserialize<Guid[]>(snapshot.AccountIdsJson, Json)!)
            if (await Access(account, PermissionKeys.Ninja.Read, ct) is { } error) return Result.Failure<NinjaSnapshotResponse>(error);
        return Result.Success(new NinjaSnapshotResponse(snapshot.Id, snapshot.Frozen, snapshot.SupersedesId, snapshot.CreatedAtUtc,
            snapshot.Reason, JsonSerializer.Deserialize<NinjaEvaluationReport>(snapshot.ResultJson, Json)!));
    }
}
