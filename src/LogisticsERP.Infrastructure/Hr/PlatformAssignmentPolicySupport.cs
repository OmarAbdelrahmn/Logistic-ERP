using LogisticsERP.Domain.Chefz;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Platform;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal static class PlatformAssignmentPolicySupport
{
    public static IQueryable<CurrentAssignment> CurrentAssignments(ApplicationDbContext db, Guid riderId, Guid? excludedAssignmentId = null) =>
        from assignment in db.RiderClientAssignments.AsNoTracking()
        join account in db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking() on assignment.PlatformRiderAccountId equals account.Id
        join platform in db.ClientPlatforms.IgnoreQueryFilters().AsNoTracking() on account.ClientPlatformId equals platform.Id
        where assignment.RiderProfileId == riderId && assignment.EffectiveTo == null
            && (excludedAssignmentId == null || assignment.Id != excludedAssignmentId)
        select new CurrentAssignment(assignment.Id, assignment.PaymentModel, assignment.RiderAccountSlot, platform.Code);

    public static Task<string?> PlatformCodeAsync(ApplicationDbContext db, Guid accountId, CancellationToken ct) =>
        (from account in db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking()
         join platform in db.ClientPlatforms.IgnoreQueryFilters().AsNoTracking() on account.ClientPlatformId equals platform.Id
         where account.Id == accountId select platform.Code).SingleOrDefaultAsync(ct);

    public static int NextSlot(CurrentAssignment[] active) => Enumerable.Range(1, ChefzAccountAssignmentRules.MaximumCombinedActiveAccountsPerRider)
        .First(slot => active.All(a => a.RiderAccountSlot != slot));

    public static PlatformAccountAssignmentDecision Evaluate(string? requestedPlatformCode, CurrentAssignment[] active,
        PlatformAccountPaymentModel requestedPaymentModel) => ChefzAccountAssignmentRules.Evaluate(requestedPlatformCode,
            active.Select(a => ((string?)a.PlatformCode, a.PaymentModel)), requestedPaymentModel);

    internal sealed record CurrentAssignment(Guid Id, PlatformAccountPaymentModel PaymentModel, int RiderAccountSlot, string PlatformCode);
}
