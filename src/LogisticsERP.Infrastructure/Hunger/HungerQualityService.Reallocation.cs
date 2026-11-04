using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Hunger;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService
{
    public async Task<Result> ReallocateQualityAsync(Guid sourceId, HungerReallocationRequest request, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.QualityImport, ct);
        var accountId = await (from s in db.Set<HungerQualitySnapshot>() join a in db.PlatformRiderAccounts on s.PlatformRiderAccountId equals a.Id
            where s.Id == sourceId && platforms.Contains(a.ClientPlatformId) select (Guid?)a.Id).SingleOrDefaultAsync(ct);
        if (accountId is null) return Result.Failure(HungerErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000) return Result.Failure(HungerErrors.Invalid("سبب إعادة التخصيص مطلوب."));
        await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, [accountId.Value], ct);
        var source = await db.Set<HungerQualitySnapshot>().SingleAsync(s => s.Id == sourceId, ct);
        if (Convert.ToBase64String(source.RowVersion) != request.RowVersion) return Result.Failure(HungerErrors.Conflict("تغير التقرير."));
        var participants = await db.RiderClientAssignments.AsNoTracking().Where(a => a.PlatformRiderAccountId == accountId && a.Status != RiderAssignmentStatus.Cancelled
            && a.EffectiveFrom <= source.ReportDate && (a.EffectiveTo == null || a.EffectiveTo >= source.CoverageFrom)).ToArrayAsync(ct);
        var identities = participants.Select(a => a.RiderProfileId).Distinct().ToArray();
        var sole = identities.Length == 1 && participants.Any(a => a.EffectiveFrom <= source.CoverageFrom && (a.EffectiveTo == null || a.EffectiveTo >= source.ReportDate)) ? (Guid?)identities[0] : null;
        var shares = await Allocate(accountId.Value, source.CoverageFrom, source.ReportDate, Read<HungerQualityCounters>(source.DailyCountersJson), sole, ct);
        if (shares.Count == 0) return Result.Failure(HungerErrors.Conflict("ما زالت حصص المستخدمين غير مثبتة؛ لم يغير إجمالي المعرف."));
        var before = Serialize(new { request.Reason, Snapshot = source }); source.AllocationsJson = Serialize(shares); source.SoleRiderProfileId = sole; source.AllocationStatus = "Resolved";
        db.Add(new HungerQualityRevision { SnapshotId = source.Id, TriggerImportId = source.ImportId, BeforeJson = before, AfterJson = Serialize(source) });
        try { await db.SaveChangesAsync(ct); return Result.Success(); }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure(HungerErrors.Conflict("تعارض أثناء إعادة التخصيص.")); }
    }
}
