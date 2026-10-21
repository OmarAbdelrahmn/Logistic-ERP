using LogisticsERP.Application.Common.Results;
using LogisticsERP.Infrastructure.Persistence;

namespace LogisticsERP.Infrastructure.Hr;

internal static class RiderOperationalBlock
{
    public const string Reason = "المندوب موقوف بسبب مستحق جاهز مرحل؛ يلزم سداد كامل المبلغ قبل التشغيل.";
    public static readonly OperationError Error = new("rider.jahez_debt_block", Reason, ErrorType.Conflict);
    public static readonly string[] PlatformCodes = ["KEETA", "CHEFZ", "THECHEFZ", "CHEFSZ", "SHIFTZ", "NINJA"];

    // Transferred Jahez debt is a financial obligation, not an operational suspension.
    // Keep the shared callers compatible; this also releases debts created before this change.
    public static Task<bool> IsBlocked(ApplicationDbContext db, Guid riderId, Guid platformId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }

    public static Task<HashSet<Guid>> BlockedAssignmentIds(ApplicationDbContext db, Guid[] assignmentIds, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new HashSet<Guid>());
    }

    public static Task<bool> IsAccountBlocked(ApplicationDbContext db, Guid accountId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }
}
