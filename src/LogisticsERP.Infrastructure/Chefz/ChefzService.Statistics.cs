using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Chefz;

internal sealed partial class ChefzService
{
    private static ChefzStatisticsResponse StatisticsResponse(ChefzSponsorStatistics s, string name) => new(s.Id,
        s.SponsorId, name, new(s.SponsorshipCount, s.Working, s.NotWorking, s.Blocked, s.PermanentlyBlocked,
            s.UpdateBlocked, s.Uploaded, s.PrivateLicense, s.PendingUpload, s.NoLicense, s.ManagementSuspended,
            s.Motorcycle, s.WorkingOutsideSponsorship, s.WorkingWithoutPerformance, Convert.ToBase64String(s.RowVersion)),
        Convert.ToBase64String(s.RowVersion));

    public async Task<Result<ChefzPage<ChefzStatisticsResponse>>> GetStatisticsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct)) return Result.Failure<ChefzPage<ChefzStatisticsResponse>>(ChefzErrors.Forbidden);
        if (!ValidPage(page, pageSize)) return Result.Failure<ChefzPage<ChefzStatisticsResponse>>(ChefzErrors.Invalid("حجم الصفحة من 1 إلى 100."));
        var rows = await (from s in db.ChefzSponsorStatistics.AsNoTracking() join sponsor in db.Sponsors.AsNoTracking() on s.SponsorId equals sponsor.Id
            orderby sponsor.RegistryNameAr, s.Id select new { Statistics = s, Name = sponsor.RegistryNameAr })
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success(new ChefzPage<ChefzStatisticsResponse>(rows.Select(r => StatisticsResponse(r.Statistics, r.Name)).ToArray(), page, pageSize));
    }

    public async Task<Result<ChefzStatisticsResponse>> GetStatisticsAsync(Guid sponsorId, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct)) return Result.Failure<ChefzStatisticsResponse>(ChefzErrors.Forbidden);
        var row = await (from s in db.ChefzSponsorStatistics.AsNoTracking() join sponsor in db.Sponsors.AsNoTracking() on s.SponsorId equals sponsor.Id
            where s.SponsorId == sponsorId select new { Statistics = s, Name = sponsor.RegistryNameAr }).SingleOrDefaultAsync(ct);
        return row is null ? Result.Failure<ChefzStatisticsResponse>(ChefzErrors.NotFound) : Result.Success(StatisticsResponse(row.Statistics, row.Name));
    }

    public Task<Result<ChefzStatisticsResponse>> CreateStatisticsAsync(Guid sponsorId, ChefzStatisticsRequest request, CancellationToken ct = default) => SaveStatistics(sponsorId, request, false, ct);
    public Task<Result<ChefzStatisticsResponse>> UpdateStatisticsAsync(Guid sponsorId, ChefzStatisticsRequest request, CancellationToken ct = default) => SaveStatistics(sponsorId, request, true, ct);

    private async Task<Result<ChefzStatisticsResponse>> SaveStatistics(Guid sponsorId, ChefzStatisticsRequest r, bool update, CancellationToken ct)
    {
        if (!await Allowed(PermissionKeys.Chefz.AccountsUpdate, ct)) return Result.Failure<ChefzStatisticsResponse>(ChefzErrors.Forbidden);
        if (new[] { r.SponsorshipCount, r.Working, r.NotWorking, r.Blocked, r.PermanentlyBlocked, r.UpdateBlocked,
            r.Uploaded, r.PrivateLicense, r.PendingUpload, r.NoLicense, r.ManagementSuspended, r.Motorcycle,
            r.WorkingOutsideSponsorship, r.WorkingWithoutPerformance }.Any(v => v < 0))
            return Result.Failure<ChefzStatisticsResponse>(ChefzErrors.Invalid("جميع الإحصائيات يجب أن تكون أعداداً غير سالبة."));
        var name = await db.Sponsors.AsNoTracking().Where(s => s.Id == sponsorId).Select(s => s.RegistryNameAr).SingleOrDefaultAsync(ct);
        if (name is null) return Result.Failure<ChefzStatisticsResponse>(ChefzErrors.NotFound);
        var entity = await db.ChefzSponsorStatistics.SingleOrDefaultAsync(s => s.SponsorId == sponsorId, ct);
        if (update && entity is null) return Result.Failure<ChefzStatisticsResponse>(ChefzErrors.NotFound);
        if (!update && entity is not null) return Result.Failure<ChefzStatisticsResponse>(ChefzErrors.Conflict("يوجد سجل إحصائيات لهذا الكفيل."));
        if (entity is not null && !HrServiceSupport.MatchesRowVersion(entity.RowVersion, r.RowVersion))
            return Result.Failure<ChefzStatisticsResponse>(ChefzErrors.Conflict("أعد تحميل الإحصائيات."));
        entity ??= new() { SponsorId = sponsorId };
        if (!update) db.ChefzSponsorStatistics.Add(entity);
        entity.SponsorshipCount = r.SponsorshipCount; entity.Working = r.Working; entity.NotWorking = r.NotWorking;
        entity.Blocked = r.Blocked; entity.PermanentlyBlocked = r.PermanentlyBlocked; entity.UpdateBlocked = r.UpdateBlocked;
        entity.Uploaded = r.Uploaded; entity.PrivateLicense = r.PrivateLicense; entity.PendingUpload = r.PendingUpload;
        entity.NoLicense = r.NoLicense; entity.ManagementSuspended = r.ManagementSuspended; entity.Motorcycle = r.Motorcycle;
        entity.WorkingOutsideSponsorship = r.WorkingOutsideSponsorship; entity.WorkingWithoutPerformance = r.WorkingWithoutPerformance;
        var result = await Persist(true, ct);
        return result.IsSuccess ? Result.Success(StatisticsResponse(entity, name)) : Result.Failure<ChefzStatisticsResponse>(result.Error);
    }

    public async Task<Result> DeleteStatisticsAsync(Guid sponsorId, string rowVersion, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.AccountsUpdate, ct)) return Result.Failure(ChefzErrors.Forbidden);
        var entity = await db.ChefzSponsorStatistics.SingleOrDefaultAsync(s => s.SponsorId == sponsorId, ct);
        if (entity is null) return Result.Failure(ChefzErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, rowVersion)) return Result.Failure(ChefzErrors.Conflict("أعد تحميل الإحصائيات."));
        entity.IsDeleted = true;
        entity.DeletionReason = "Deleted through Chefz sponsor statistics CRUD.";
        return await Persist(true, ct);
    }
}
