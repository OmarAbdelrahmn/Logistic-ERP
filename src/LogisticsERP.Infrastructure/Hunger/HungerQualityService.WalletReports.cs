using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Hunger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService
{
    public Task<Result<HungerWalletPage>> WalletsAsync(HungerWalletFilter filter, int page = 1, int pageSize = 50, CancellationToken ct = default)
        => QueryWallets(filter, page, pageSize, false, ct);

    private async Task<Result<HungerWalletPage>> QueryWallets(HungerWalletFilter filter, int page, int pageSize, bool all, CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("Features:HungerFinance")) return Result.Failure<HungerWalletPage>(HungerErrors.Forbidden);
        if (page < 1 || pageSize is < 1 or > 100 || filter.BalanceFrom > filter.BalanceThrough
            || filter.ReceiptState is not (null or "None" or "Pending" or "PartiallyConfirmed" or "Confirmed"))
            return Result.Failure<HungerWalletPage>(HungerErrors.Invalid("Invalid wallet filters or page."));
        var accounts = await QueryAccounts(filter.Accounts, 1, 100, true, PermissionKeys.Hunger.FinanceRead, ct);
        if (accounts.IsFailure) return Result.Failure<HungerWalletPage>(accounts.Error);
        var ids = accounts.Value!.Items.Select(a => a.AccountId).ToArray();
        var events = await db.Set<HungerFinancialEvent>().AsNoTracking().Where(e => ids.Contains(e.PlatformRiderAccountId ?? Guid.Empty)).ToArrayAsync(ct);
        var reviews = await db.Set<HungerWalletReview>().AsNoTracking().Where(r => ids.Contains(r.PlatformRiderAccountId))
            .OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id).ToArrayAsync(ct);
        var eventIds = events.Select(e => e.Id).ToArray();
        var lines = await db.Set<HungerRemittanceLine>().AsNoTracking().Where(l => eventIds.Contains(l.SourceEventId)).ToArrayAsync(ct);
        var result = new List<HungerWalletItem>();
        foreach (var account in accounts.Value.Items)
        {
            ct.ThrowIfCancellationRequested();
            var history = events.Where(e => e.PlatformRiderAccountId == account.AccountId).ToArray();
            var observation = reviews.Where(r => r.PlatformRiderAccountId == account.AccountId)
                .Select(r => Read<HungerWalletReviewRequest>(r.RequestJson)).OrderByDescending(r => r.BusinessDate).FirstOrDefault();
            decimal? period = null;
            if (observation is not null)
            {
                var closing = await WalletClosing(account.AccountId, observation.BusinessDate, ct);
                var previous = new DateOnly(observation.BusinessDate.Year, observation.BusinessDate.Month, 1).AddMonths(-1);
                var closeId = await db.Set<HungerSettlementDocument>().AsNoTracking().Where(d => d.PlatformRiderAccountId == account.AccountId
                    && d.Kind == "MonthClose" && d.ChargeFrom == previous && d.Status == "Matched").Select(d => (Guid?)d.Id).SingleOrDefaultAsync(ct);
                var adjustments = await ClosingAdjustments(account.AccountId, closeId, observation.BusinessDate, ct);
                if (closing.HasValue) period = HungerWalletRules.CurrentPeriod(observation.MobileBalance, closing.Value, adjustments);
            }
            var restricted = history.Where(e => e.Kind == "WalletReturn" && e.RiderBalanceChange > 0).Sum(e => Math.Max(0,
                e.RiderBalanceChange - history.Where(w => w.RelatedEventId == e.Id && w.Kind is "WithdrawalCompany" or "WithdrawalSupervisor" && w.Status == "Arrived").Sum(w => w.Amount)));
            restricted += history.Where(e => e.Kind is "WithdrawalCompany" or "WithdrawalSupervisor" && e.Status == "PendingArrival" && e.RelatedEventId == null).Sum(e => e.Amount);
            var sources = history.Where(e => e.Kind is "Collection" or "TopUpCollected" || e.Kind == "WithdrawalSupervisor" && e.Status == "Arrived").ToArray();
            var sourceIds = sources.Select(e => e.Id).ToHashSet();
            var confirmed = lines.Where(l => sourceIds.Contains(l.SourceEventId)).Sum(l => l.ConfirmedAmount)
                + history.Where(e => e.Kind == "WithdrawalCompany" && e.Status == "Arrived").Sum(e => e.Amount);
            var pending = sources.Sum(e => e.Amount) - lines.Where(l => sourceIds.Contains(l.SourceEventId)).Sum(l => l.ConfirmedAmount);
            var arrival = history.Where(e => e.Kind is "WithdrawalCompany" or "WithdrawalSupervisor" && e.Status == "PendingArrival").Sum(e => e.Amount);
            var state = pending + arrival > 0 ? confirmed > 0 ? "PartiallyConfirmed" : "Pending" : confirmed > 0 ? "Confirmed" : "None";
            var item = new HungerWalletItem(account.AccountId, account.ExternalAccountId, observation?.MobileBalance, observation?.BusinessDate,
                observation?.WalletEvidence, period, restricted, period.HasValue ? HungerWalletRules.Usable(period.Value, restricted) : null,
                state, arrival, confirmed, pending);
            if ((filter.BalanceFrom == null || item.MobileBalance >= filter.BalanceFrom)
                && (filter.BalanceThrough == null || item.MobileBalance <= filter.BalanceThrough)
                && (filter.BalanceEqual == null || item.MobileBalance == filter.BalanceEqual)
                && (filter.ReceiptState == null || state == filter.ReceiptState)) result.Add(item);
        }
        return Result.Success(new HungerWalletPage(all ? result : result.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), result.Count, page, pageSize));
    }

    public async Task<Result<HungerWalletHistory>> WalletHistoryAsync(Guid accountId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var platform = await db.PlatformRiderAccounts.AsNoTracking().Where(a => a.Id == accountId).Select(a => (Guid?)a.ClientPlatformId).SingleOrDefaultAsync(ct);
        if (platform == null || !await FinanceAccess(platform.Value, PermissionKeys.Hunger.FinanceRead, ct)) return Result.Failure<HungerWalletHistory>(HungerErrors.Forbidden);
        if (page < 1 || pageSize is < 1 or > 100) return Result.Failure<HungerWalletHistory>(HungerErrors.Invalid("Invalid page."));
        var direct = db.Set<HungerFinancialEvent>().AsNoTracking().Where(e => e.PlatformRiderAccountId == accountId);
        var sourceIds = direct.Select(e => e.Id);
        var batchIds = db.Set<HungerRemittanceLine>().Where(l => sourceIds.Contains(l.SourceEventId)).Select(l => l.RemittanceEventId);
        var confirmations = from allocation in db.Set<HungerReceiptAllocation>() join line in db.Set<HungerRemittanceLine>() on allocation.RemittanceLineId equals line.Id
            where sourceIds.Contains(line.SourceEventId) select allocation.ConfirmationEventId;
        var events = db.Set<HungerFinancialEvent>().AsNoTracking().Where(e => e.ClientPlatformId == platform.Value
            && (e.PlatformRiderAccountId == accountId || batchIds.Contains(e.Id) || confirmations.Contains(e.Id) || confirmations.Contains(e.RelatedEventId ?? Guid.Empty)));
        var observations = db.Set<HungerWalletReview>().AsNoTracking().Where(r => r.PlatformRiderAccountId == accountId);
        var eventRows = await events.OrderByDescending(e => e.BusinessDate).ThenByDescending(e => e.CreatedAtUtc).ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(e => new HungerFinancialEventItem(e.Id, e.Kind, e.PlatformRiderAccountId, e.RiderProfileId,
                e.SupervisorEmployeeId, e.Amount, e.RiderBalanceChange, e.BusinessDate, e.EvidenceReference, e.Status)).ToArrayAsync(ct);
        var reviewRows = await observations.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success(new HungerWalletHistory(eventRows, reviewRows.Select(r => { var request = Read<HungerWalletReviewRequest>(r.RequestJson);
            return new HungerWalletObservation(r.Id, request.BusinessDate, request.MobileBalance, request.WalletEvidence, r.Status); }).ToArray(),
            await events.CountAsync(ct), await observations.CountAsync(ct), page, pageSize));
    }

    public async Task<Result<HungerFile>> ExportWalletsAsync(HungerWalletFilter filter, string format = "xlsx", CancellationToken ct = default)
    {
        // Financial read is the existing permission for financial report exports.
        if (format.ToLowerInvariant() is not ("xlsx" or "csv")) return Result.Failure<HungerFile>(HungerErrors.Invalid("Use csv or xlsx."));
        var result = await QueryWallets(filter, 1, 100, true, ct);
        if (result.IsFailure) return Result.Failure<HungerFile>(result.Error);
        return Result.Success(TabularFile("Hunger-Wallets", format,
            ["AccountId", "ExternalAccountId", "MobileBalance", "ObservedOn", "Evidence", "CurrentPeriodBalance", "RestrictedRights", "AttributedBalance", "ReceiptState", "PendingArrival", "ConfirmedReceipts", "PendingReceipts"],
            result.Value!.Items.Select(w => new object?[] { w.AccountId.ToString(), w.ExternalAccountId, w.MobileBalance, w.ObservedOn?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                w.Evidence, w.CurrentPeriodBalance, w.RestrictedRights, w.AttributedBalance, w.ReceiptState, w.PendingArrival, w.ConfirmedReceipts, w.PendingReceipts })));
    }
}
