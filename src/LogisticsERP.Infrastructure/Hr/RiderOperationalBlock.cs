using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal static class RiderOperationalBlock
{
    public const string Reason = "المندوب موقوف بسبب مستحق جاهز مرحل؛ يلزم سداد كامل المبلغ قبل التشغيل.";
    public static readonly OperationError Error = new("rider.jahez_debt_block", Reason, ErrorType.Conflict);
    public static readonly string[] PlatformCodes = ["KEETA", "CHEFZ", "THECHEFZ", "CHEFSZ", "SHIFTZ", "NINJA"];

    public static async Task<bool> IsBlocked(ApplicationDbContext db, Guid riderId, Guid platformId, CancellationToken ct) =>
        await db.ClientPlatforms.AnyAsync(p => p.Id == platformId && PlatformCodes.Contains(p.Code), ct)
        && await db.Set<JahezTransferredDebt>().AnyAsync(x => x.RiderProfileId == riderId && x.PaidAtUtc == null, ct);

    public static async Task<HashSet<Guid>> BlockedAssignmentIds(ApplicationDbContext db, Guid[] assignmentIds, CancellationToken ct) =>
        (await (from a in db.RiderClientAssignments.AsNoTracking()
            join account in db.PlatformRiderAccounts on a.PlatformRiderAccountId equals account.Id
            join p in db.ClientPlatforms on account.ClientPlatformId equals p.Id
            where assignmentIds.Contains(a.Id) && a.EffectiveTo == null && PlatformCodes.Contains(p.Code)
                && db.Set<JahezTransferredDebt>().Any(x => x.RiderProfileId == a.RiderProfileId && x.PaidAtUtc == null)
            select a.Id).ToArrayAsync(ct)).ToHashSet();

    public static Task<bool> IsAccountBlocked(ApplicationDbContext db, Guid accountId, CancellationToken ct) =>
        (from a in db.RiderClientAssignments join account in db.PlatformRiderAccounts on a.PlatformRiderAccountId equals account.Id
            join p in db.ClientPlatforms on account.ClientPlatformId equals p.Id
            where a.PlatformRiderAccountId == accountId && a.EffectiveTo == null && PlatformCodes.Contains(p.Code)
                && db.Set<JahezTransferredDebt>().Any(x => x.RiderProfileId == a.RiderProfileId && x.PaidAtUtc == null)
            select a.Id).AnyAsync(ct);
}
