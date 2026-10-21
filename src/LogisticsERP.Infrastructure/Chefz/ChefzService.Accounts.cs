using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Chefz;

internal sealed partial class ChefzService
{
    public async Task<Result<ChefzAccountsResponse>> GetAccountsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct)) return Result.Failure<ChefzAccountsResponse>(ChefzErrors.Forbidden);
        if (!ValidPage(page, pageSize)) return Result.Failure<ChefzAccountsResponse>(ChefzErrors.Invalid("حجم الصفحة من 1 إلى 100."));
        var rows = await (from a in Accounts.AsNoTracking() join detail in db.ChefzAccountDetails.AsNoTracking() on a.Id equals detail.PlatformRiderAccountId into detailRows
            from d in detailRows.DefaultIfEmpty()
            orderby a.ExternalAccountId, a.Id select new { Account = a, Details = d,
                Password = db.PlatformAccountCredentialVersions.Any(v => v.PlatformRiderAccountId == a.Id) })
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);

        // Aggregate the complete account set in SQL, independently of the requested page.
        var counts = await AccountCountsQuery().ToArrayAsync(ct);
        var sponsorIds = counts.Where(c => c.SponsorId.HasValue).Select(c => c.SponsorId!.Value).Distinct().ToArray();
        var names = await db.Sponsors.AsNoTracking().Where(s => sponsorIds.Contains(s.Id))
            .Select(s => new { s.Id, s.RegistryNameAr, s.RegistryNameEn }).ToDictionaryAsync(s => s.Id, ct);
        var sponsors = counts.GroupBy(c => c.SponsorId).Select(group =>
        {
            var sponsor = group.Key.HasValue ? names.GetValueOrDefault(group.Key.Value) : null;
            return new ChefzSponsorAccountCounts(group.Key,
                sponsor?.RegistryNameAr ?? (group.Key.HasValue ? "كفيل غير متاح" : "خارج الكفالة"),
                sponsor?.RegistryNameEn, SummarizeAccounts(group));
        }).OrderBy(s => s.SponsorNameAr).ThenBy(s => s.SponsorId).ToArray();
        var summary = SummarizeAccounts(counts);
        var accountIds = rows.Select(x => x.Account.Id).ToArray();
        var assignmentIds = await db.RiderClientAssignments.AsNoTracking()
            .Where(x => accountIds.Contains(x.PlatformRiderAccountId) && x.EffectiveTo == null)
            .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Id)
            .Select(x => new { x.Id, x.PlatformRiderAccountId, x.RiderProfileId }).ToArrayAsync(ct);
        var riders = await RiderIdentities(assignmentIds.Select(x => x.RiderProfileId), ct);
        var riderIqamas = assignmentIds.GroupBy(x => x.PlatformRiderAccountId)
            .ToDictionary(g => g.Key, g => riders.GetValueOrDefault(g.First().RiderProfileId)?.RiderIqamaNo);
        var blocked = await LogisticsERP.Infrastructure.Hr.RiderOperationalBlock.BlockedAssignmentIds(db, assignmentIds.Select(x => x.Id).ToArray(), ct);
        var blockedAccounts = assignmentIds.Where(x => blocked.Contains(x.Id)).Select(x => x.PlatformRiderAccountId).ToHashSet();
        return Result.Success(new ChefzAccountsResponse(rows.Select(r => AccountResponse(r.Account, r.Details, r.Password) with {
            RiderIqamaNo = riderIqamas.GetValueOrDefault(r.Account.Id),
            IsOperationallyBlocked = blockedAccounts.Contains(r.Account.Id),
            OperationalBlockReason = blockedAccounts.Contains(r.Account.Id) ? LogisticsERP.Infrastructure.Hr.RiderOperationalBlock.Reason : null }).ToArray(),
            page, pageSize, summary.Total, summary, sponsors));
    }

    private IQueryable<AccountCountRow> AccountCountsQuery()
    {
        var takenAccounts = db.RiderClientAssignments.AsNoTracking().Where(a => a.EffectiveTo == null)
            .Select(a => (Guid?)a.PlatformRiderAccountId).Distinct();
        return from account in Accounts.AsNoTracking()
            join takenId in takenAccounts on (Guid?)account.Id equals takenId into takenRows
            from takenId in takenRows.DefaultIfEmpty()
            group account by new { account.SponsorId, account.Status, Taken = takenId != null } into accounts
            select new AccountCountRow(accounts.Key.SponsorId, accounts.Key.Status, accounts.Key.Taken, accounts.Count());
    }

    private static ChefzAccountCounts SummarizeAccounts(IEnumerable<AccountCountRow> rows)
    {
        var counts = rows.ToArray();
        var total = counts.Sum(c => c.Count);
        var taken = counts.Where(c => c.Taken).Sum(c => c.Count);
        int InStatus(PlatformRiderAccountStatus status) => counts.Where(c => c.Status == status).Sum(c => c.Count);
        return new(total, taken, total - taken, InStatus(PlatformRiderAccountStatus.Available),
            InStatus(PlatformRiderAccountStatus.Assigned), InStatus(PlatformRiderAccountStatus.Suspended),
            InStatus(PlatformRiderAccountStatus.Retired), InStatus(PlatformRiderAccountStatus.Archived));
    }

    private sealed record AccountCountRow(Guid? SponsorId, PlatformRiderAccountStatus Status, bool Taken, int Count);
}
