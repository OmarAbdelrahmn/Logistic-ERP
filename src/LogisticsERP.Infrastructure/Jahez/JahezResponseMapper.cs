using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed class JahezResponseMapper(ApplicationDbContext db, IdentityDbContext identity) : IJahezResponseMapper
{
    public async Task<object?> MapAsync(object? value, CancellationToken ct = default)
    {
        var normalized = Normalize(value);
        var candidates = new List<(JahezNamedResponse Value, References Refs)>();
        Collect(normalized, null, candidates);
        if (candidates.Count == 0) return normalized;

        var handoverIds = candidates.Select(x => x.Refs.HandoverId).OfType<Guid>().Distinct().ToArray();
        var handovers = handoverIds.Length == 0 ? [] : await (
            from h in db.Set<JahezAccountHandover>().AsNoTracking()
            join a in db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking() on h.PlatformRiderAccountId equals a.Id
            join p in db.ClientPlatforms.IgnoreQueryFilters().AsNoTracking() on a.ClientPlatformId equals p.Id
            where handoverIds.Contains(h.Id) && p.Code == "JAHEZ"
            select new HandoverLink(h.Id, a.Id, h.RiderProfileId)).ToArrayAsync(ct);
        var handoverMap = handovers.ToDictionary(x => x.Id);
        var accountIds = candidates.SelectMany(x => new[] { x.Refs.AccountId, x.Refs.TargetAccountId })
            .OfType<Guid>().Concat(handovers.Select(x => x.AccountId)).Distinct().ToArray();
        var accounts = accountIds.Length == 0 ? [] : await (
            from a in db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking()
            join p in db.ClientPlatforms.IgnoreQueryFilters().AsNoTracking() on a.ClientPlatformId equals p.Id
            where accountIds.Contains(a.Id) && p.Code == "JAHEZ"
            select new AccountLink(a.Id, a.Code, a.ExternalAccountId, a.RegisteredEmployeeId)).ToArrayAsync(ct);
        var accountMap = accounts.ToDictionary(x => x.Id);
        var riderIds = candidates.Select(x => x.Refs.RiderId).OfType<Guid>()
            .Concat(handovers.Select(x => x.RiderId)).Distinct().ToArray();
        var ownerEmployeeIds = accounts.Select(x => x.OwnerEmployeeId).OfType<Guid>().Distinct().ToArray();
        var people = riderIds.Length == 0 && ownerEmployeeIds.Length == 0 ? [] : await (
            from e in db.Employees.IgnoreQueryFilters().AsNoTracking()
            join r in db.RiderProfiles.IgnoreQueryFilters().AsNoTracking() on e.Id equals r.EmployeeId into riders
            from r in riders.DefaultIfEmpty()
            where ownerEmployeeIds.Contains(e.Id) || r != null && riderIds.Contains(r.Id)
            select new PersonLink(r == null ? null : (Guid?)r.Id, e.Id, e.FullNameAr, e.FullNameEn)).ToArrayAsync(ct);
        var riderMap = people.Where(x => x.RiderId.HasValue).ToDictionary(x => x.RiderId!.Value);
        var employeeMap = people.GroupBy(x => x.EmployeeId).ToDictionary(x => x.Key, x => x.First());
        var userIds = candidates.SelectMany(x => x.Refs.Users.Values).OfType<Guid>().Distinct().ToArray();
        var users = userIds.Length == 0 ? [] : await identity.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new UserLink(x.Id, x.DisplayNameAr, x.DisplayNameEn)).ToArrayAsync(ct);
        var userMap = users.ToDictionary(x => x.Id);
        var enriched = new Dictionary<JahezNamedResponse, JahezNamedResponse>(ReferenceEqualityComparer.Instance);
        foreach (var (response, refs) in candidates)
        {
            var handover = refs.HandoverId.HasValue ? handoverMap.GetValueOrDefault(refs.HandoverId.Value) : null;
            var accountId = refs.AccountId ?? handover?.AccountId;
            var account = accountId.HasValue ? accountMap.GetValueOrDefault(accountId.Value) : null;
            var owner = account?.OwnerEmployeeId is Guid employeeId ? employeeMap.GetValueOrDefault(employeeId) : null;
            var riderId = refs.RiderId ?? handover?.RiderId;
            var rider = riderId.HasValue ? riderMap.GetValueOrDefault(riderId.Value) : null;
            var target = refs.TargetAccountId.HasValue ? accountMap.GetValueOrDefault(refs.TargetAccountId.Value) : null;
            enriched[response] = response with
            {
                Account = Display(account),
                OwnerRiderProfileId = owner?.RiderId,
                OwnerEmployeeId = account?.OwnerEmployeeId,
                OwnerRiderNameAr = owner?.NameAr,
                OwnerRiderNameEn = owner?.NameEn,
                ActualRiderProfileId = riderId,
                ActualEmployeeId = rider?.EmployeeId,
                ActualRiderNameAr = rider?.NameAr,
                ActualRiderNameEn = rider?.NameEn,
                TargetAccount = Display(target),
            CreatedByUserNameAr = UserName(refs, "CreatedByUserId")?.NameAr,
            CreatedByUserNameEn = UserName(refs, "CreatedByUserId")?.NameEn,
            UpdatedByUserNameAr = UserName(refs, "UpdatedByUserId")?.NameAr,
            UpdatedByUserNameEn = UserName(refs, "UpdatedByUserId")?.NameEn,
            DeletedByUserNameAr = UserName(refs, "DeletedByUserId")?.NameAr,
            DeletedByUserNameEn = UserName(refs, "DeletedByUserId")?.NameEn,
            RequestedByUserNameAr = UserName(refs, "RequestedByUserId")?.NameAr,
            RequestedByUserNameEn = UserName(refs, "RequestedByUserId")?.NameEn,
            CollectedByUserNameAr = UserName(refs, "CollectedByUserId")?.NameAr,
            CollectedByUserNameEn = UserName(refs, "CollectedByUserId")?.NameEn,
            ActorUserNameAr = UserName(refs, "ActorUserId")?.NameAr,
            ActorUserNameEn = UserName(refs, "ActorUserId")?.NameEn,
            UploadedByUserNameAr = UserName(refs, "UploadedByUserId")?.NameAr,
            UploadedByUserNameEn = UserName(refs, "UploadedByUserId")?.NameEn,
            AccountantUserNameAr = UserName(refs, "AccountantUserId")?.NameAr,
            AccountantUserNameEn = UserName(refs, "AccountantUserId")?.NameEn,
            ApprovedByUserNameAr = UserName(refs, "ApprovedByUserId")?.NameAr,
            ApprovedByUserNameEn = UserName(refs, "ApprovedByUserId")?.NameEn,
            };
        }
        return Rewrite(normalized, enriched);

        UserLink? UserName(References refs, string key) =>
            refs.Users.GetValueOrDefault(key) is Guid id ? userMap.GetValueOrDefault(id) : null;
    }

    private static JahezAccountDisplay? Display(AccountLink? account) =>
        account is null ? null : new(account.Id, account.Code, account.ExternalAccountId);

    private static object? Normalize(object? value) => value switch
    {
        JahezAccountFee x => new JahezAccountFeeResponse(x),
        JahezApprovalRequest x => new JahezApprovalRequestResponse(x),
        JahezApprovalDecision x => new JahezApprovalDecisionResponse(x),
        JahezCommissionPolicyPeriod x => new JahezCommissionPolicyPeriodResponse(x),
        JahezEarningsStatement x => new JahezEarningsStatementResponse(x),
        JahezImportBatch x => new JahezImportBatchResponse(x),
        JahezRiderSettlement x => new JahezRiderSettlementResponse(x),
        JahezLedgerEntry x => new JahezLedgerEntryResponse(x),
        JahezCashboxEntry x => new JahezCashboxEntryResponse(x),
        JahezCashboxHandover x => new JahezCashboxHandoverResponse(x),
        JahezApprovalResponse x => new JahezApprovalDetailsResponse(
            new(x.Request), x.Decisions.Select(d => new JahezApprovalDecisionResponse(d)).ToArray()),
        IJahezPage x => new JahezPage<object>(x.Values.Select(v => Normalize(v)!).ToArray(), x.Page, x.PageSize),
        _ => value
    };

    private static void Collect(object? value, Guid? parentHandoverId,
        List<(JahezNamedResponse Value, References Refs)> candidates)
    {
        switch (value)
        {
            case JahezNamedResponse response:
                candidates.Add((response, GetReferences(response, parentHandoverId)));
                break;
            case JahezApprovalDetailsResponse approval:
                Collect(approval.Request, null, candidates);
                foreach (var decision in approval.Decisions) Collect(decision, approval.Request.HandoverId, candidates);
                break;
            case JahezImportPreview preview:
                foreach (var row in preview.Rows) Collect(row, null, candidates);
                foreach (var account in preview.Accounts ?? []) Collect(account, null, candidates);
                break;
            case IJahezPage page:
                foreach (var item in page.Values) Collect(item, null, candidates);
                break;
        }
    }

    private static object? Rewrite(object? value, Dictionary<JahezNamedResponse, JahezNamedResponse> enriched) => value switch
    {
        JahezNamedResponse x => enriched[x],
        JahezApprovalDetailsResponse x => new JahezApprovalDetailsResponse(
            (JahezApprovalRequestResponse)enriched[x.Request],
            x.Decisions.Select(d => (JahezApprovalDecisionResponse)enriched[d]).ToArray()),
        JahezImportPreview x => x with
        {
            Rows = x.Rows.Select(r => (JahezImportRowPreview)enriched[r]).ToArray(),
            Accounts = x.Accounts?.Select(a => (JahezImportAccountSummary)enriched[a]).ToArray()
        },
        IJahezPage x => new JahezPage<object>(x.Values.Select(v => Rewrite(v, enriched)!).ToArray(), x.Page, x.PageSize),
        _ => value
    };

    private static References GetReferences(JahezNamedResponse response, Guid? parentHandoverId) => response switch
    {
        JahezHandoverResponse x => new(x.Id, x.AccountId, x.RiderProfileId, null, EmptyUsers),
        JahezBalanceResponse x => new(x.HandoverId, x.AccountId, x.RiderProfileId, null, EmptyUsers),
        JahezDispatchReportRow x => new(x.HandoverId, x.AccountId, x.RiderProfileId, null, EmptyUsers),
        JahezImportRowPreview x => new(x.HandoverId, x.AccountId, x.RiderProfileId, null, EmptyUsers),
        JahezImportAccountSummary x => new(null, x.AccountId, null, null, EmptyUsers),
        JahezAccountFeeResponse x => new(x.HandoverId, null, null, null, new Dictionary<string, Guid?> { ["CreatedByUserId"] = x.CreatedByUserId, ["UpdatedByUserId"] = x.UpdatedByUserId, ["DeletedByUserId"] = x.DeletedByUserId }),
        JahezApprovalRequestResponse x => new(x.HandoverId, null, null, x.TargetAccountId, new Dictionary<string, Guid?> { ["CreatedByUserId"] = x.CreatedByUserId, ["UpdatedByUserId"] = x.UpdatedByUserId, ["DeletedByUserId"] = x.DeletedByUserId, ["RequestedByUserId"] = x.RequestedByUserId }),
        JahezApprovalDecisionResponse x => new(parentHandoverId, null, null, null, new Dictionary<string, Guid?> { ["CreatedByUserId"] = x.CreatedByUserId, ["ActorUserId"] = x.ActorUserId }),
        JahezCommissionPolicyPeriodResponse x => new(x.HandoverId, null, null, null, new Dictionary<string, Guid?> { ["CreatedByUserId"] = x.CreatedByUserId }),
        JahezEarningsStatementResponse x => new(x.HandoverId, null, null, null, new Dictionary<string, Guid?> { ["CreatedByUserId"] = x.CreatedByUserId }),
        JahezImportBatchResponse x => new(parentHandoverId, null, null, null, new Dictionary<string, Guid?> { ["CreatedByUserId"] = x.CreatedByUserId, ["UpdatedByUserId"] = x.UpdatedByUserId, ["DeletedByUserId"] = x.DeletedByUserId, ["UploadedByUserId"] = x.UploadedByUserId }),
        JahezRiderSettlementResponse x => new(x.HandoverId, null, null, null, new Dictionary<string, Guid?> { ["CreatedByUserId"] = x.CreatedByUserId, ["CollectedByUserId"] = x.CollectedByUserId }),
        JahezLedgerEntryResponse x => new(x.HandoverId, null, null, null, new Dictionary<string, Guid?> { ["CreatedByUserId"] = x.CreatedByUserId }),
        JahezCashboxEntryResponse x => new(x.HandoverId, null, null, null, new Dictionary<string, Guid?> { ["CreatedByUserId"] = x.CreatedByUserId, ["UpdatedByUserId"] = x.UpdatedByUserId, ["DeletedByUserId"] = x.DeletedByUserId, ["CollectedByUserId"] = x.CollectedByUserId }),
        JahezCashboxHandoverResponse x => new(parentHandoverId, null, null, null, new Dictionary<string, Guid?> { ["CreatedByUserId"] = x.CreatedByUserId, ["UpdatedByUserId"] = x.UpdatedByUserId, ["DeletedByUserId"] = x.DeletedByUserId, ["RequestedByUserId"] = x.RequestedByUserId, ["AccountantUserId"] = x.AccountantUserId, ["ApprovedByUserId"] = x.ApprovedByUserId }),
        _ => throw new InvalidOperationException($"Unsupported Jahez response: {response.GetType().Name}.")
    };

    private static readonly IReadOnlyDictionary<string, Guid?> EmptyUsers = new Dictionary<string, Guid?>();
    private sealed record References(Guid? HandoverId, Guid? AccountId, Guid? RiderId, Guid? TargetAccountId,
        IReadOnlyDictionary<string, Guid?> Users);
    private sealed record HandoverLink(Guid Id, Guid AccountId, Guid RiderId);
    private sealed record AccountLink(Guid Id, string Code, string ExternalAccountId, Guid? OwnerEmployeeId);
    private sealed record PersonLink(Guid? RiderId, Guid EmployeeId, string NameAr, string? NameEn);
    private sealed record UserLink(Guid Id, string NameAr, string? NameEn);
}
