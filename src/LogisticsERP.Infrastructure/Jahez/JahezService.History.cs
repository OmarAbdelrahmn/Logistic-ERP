using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezService
{
    public Task<Result<JahezAccountFee>> GetFeeAsync(Guid handoverId, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            await Handover(handoverId, ct);
            return await db.Set<JahezAccountFee>().AsNoTracking().SingleAsync(x => x.HandoverId == handoverId, ct);
        }, ct);

    public Task<Result<JahezPage<JahezRiderSettlement>>> GetSettlementsAsync(Guid handoverId, int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize); await Handover(handoverId, ct);
            var rows = await db.Set<JahezRiderSettlement>().AsNoTracking().Where(x => x.HandoverId == handoverId)
                .OrderByDescending(x => x.RecordedAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            return new JahezPage<JahezRiderSettlement>(rows, page, pageSize);
        }, ct);

    public Task<Result<JahezPage<JahezEarningsStatement>>> GetEarningsAsync(Guid handoverId, int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize); await Handover(handoverId, ct);
            var rows = await db.Set<JahezEarningsStatement>().AsNoTracking().Where(x => x.HandoverId == handoverId)
                .OrderByDescending(x => x.FromDate).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            return new JahezPage<JahezEarningsStatement>(rows, page, pageSize);
        }, ct);

    public Task<Result<JahezPage<JahezCommissionPolicyPeriod>>> GetPoliciesAsync(Guid handoverId, int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize); await Handover(handoverId, ct);
            var rows = await db.Set<JahezCommissionPolicyPeriod>().AsNoTracking().Where(x => x.HandoverId == handoverId)
                .OrderByDescending(x => x.FromDate).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            return new JahezPage<JahezCommissionPolicyPeriod>(rows, page, pageSize);
        }, ct);

    public Task<Result<JahezPage<JahezCashboxEntry>>> GetCashboxEntriesAsync(Guid? cashboxHandoverId, int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.CashboxRead, async () =>
        {
            Page(page, pageSize);
            var rows = await db.Set<JahezCashboxEntry>().AsNoTracking().Where(x => !cashboxHandoverId.HasValue || x.CashboxHandoverId == cashboxHandoverId)
                .OrderByDescending(x => x.ReceivedAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            return new JahezPage<JahezCashboxEntry>(rows, page, pageSize);
        }, ct);

    public Task<Result<JahezPage<JahezImportBatch>>> GetImportBatchesAsync(int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.ImportsRead, async () =>
        {
            Page(page, pageSize);
            var rows = await db.Set<JahezImportBatch>().AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            return new JahezPage<JahezImportBatch>(rows, page, pageSize);
        }, ct);
}
