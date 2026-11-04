using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Hunger;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService
{
    private sealed record WalletState(string Revision, decimal Restricted);
    private async Task<WalletState> ReadWalletState(Guid accountId, CancellationToken ct)
    {
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleAsync(a => a.Id == accountId, ct);
        var assignments = await db.RiderClientAssignments.AsNoTracking().Where(a => a.PlatformRiderAccountId == accountId).OrderBy(a => a.Id).ToArrayAsync(ct);
        var details = await db.Set<PlatformAccountBusinessDetails>().AsNoTracking().SingleOrDefaultAsync(d => d.PlatformRiderAccountId == accountId, ct);
        var events = await db.Set<HungerFinancialEvent>().AsNoTracking().Where(e => e.PlatformRiderAccountId == accountId).OrderBy(e => e.Id).ToArrayAsync(ct);
        var usages = await db.PlatformAccountDayUsages.AsNoTracking().Where(u => u.PlatformRiderAccountId == accountId).OrderBy(u => u.Id).ToArrayAsync(ct);
        var contributions = await db.Set<PlatformAccountDayContribution>().AsNoTracking().Where(u => u.PlatformRiderAccountId == accountId).OrderBy(u => u.Id).ToArrayAsync(ct);
        var settlements = await db.Set<HungerSettlementDocument>().AsNoTracking().Where(d => d.PlatformRiderAccountId == accountId).OrderBy(d => d.Id).ToArrayAsync(ct);
        decimal restricted = 0;
        foreach (var credit in events.Where(e => e.Kind == "WalletReturn" && e.RiderBalanceChange > 0))
        {
            // Paying the rider transfers the claim to the company; only an actual wallet withdrawal releases it.
            var removed = events.Where(e => e.RelatedEventId == credit.Id && e.Kind is "WithdrawalCompany" or "WithdrawalSupervisor").Sum(e => e.Amount);
            restricted += Math.Max(0, credit.RiderBalanceChange - removed);
        }
        return new(Hash(Serialize(new { account, assignments, details, events, usages, contributions, settlements })), restricted);
    }
    private static bool ValidBalance(decimal value) => Math.Abs(value) <= 1000000000m && decimal.Round(value, 2) == value;
    private async Task<OperationError?> ValidateWallet(Guid accountId, HungerWalletReviewRequest request, CancellationToken ct)
    {
        if (request.Action is not ("Deliver" or "Return") || request.BusinessDate == default || request.BusinessDate > BusinessToday
            || !ValidBalance(request.MobileBalance) || !ValidBalance(request.ClosingBalance) || !ValidBalance(request.ProvenAdjustments)
            || string.IsNullOrWhiteSpace(request.WalletEvidence) || request.WalletEvidence.Length > 1000
            || request.ClosingBalance != 0 && string.IsNullOrWhiteSpace(request.ClosingEvidence)
            || request.ProvenAdjustments != 0 && string.IsNullOrWhiteSpace(request.AdjustmentEvidence)) return HungerErrors.Invalid("أدخل رصيدًا صريحًا وتاريخه وأدلة الإغلاق والتسويات إن وجدت.");
        if (!await db.Employees.AnyAsync(e => e.Id == request.SupervisorEmployeeId, ct)) return HungerErrors.Invalid("المشرف غير معروف.");
        var month = new DateOnly(request.BusinessDate.Year, request.BusinessDate.Month, 1);
        var previousMonth = month.AddMonths(-1);
        var closing = await db.Set<HungerSettlementDocument>().AsNoTracking().SingleOrDefaultAsync(d => d.PlatformRiderAccountId == accountId && d.Kind == "MonthClose" && d.ChargeFrom == previousMonth && d.Status == "Matched", ct);
        var adjustments = await db.Set<HungerSettlementDocument>().AsNoTracking().Where(d => d.PlatformRiderAccountId == accountId && d.Kind == "Adjustment" && d.ChargeFrom == month && d.ObservedOn <= request.BusinessDate && d.Status == "Matched").SumAsync(d => d.WalletBalance ?? 0, ct);
        if (request.ClosingBalance != (closing?.WalletBalance ?? 0) || request.ProvenAdjustments != adjustments)
            return HungerErrors.Conflict("أثر الإغلاق أو التسويات لا يطابق مستندات المحفظة المعتمدة؛ سجل الأدلة أولًا.");
        if (request.Action == "Deliver")
        {
            if (request.Assignment is null || request.Assignment.ActualRiderProfileId != request.RiderProfileId || request.Assignment.EffectiveFrom != request.BusinessDate || request.Return is not null) return HungerErrors.Invalid("بيانات مراجعة المحفظة لا تطابق طلب التسليم.");
            var details = await db.Set<PlatformAccountBusinessDetails>().AsNoTracking().SingleOrDefaultAsync(d => d.PlatformRiderAccountId == accountId, ct);
            if (details is { OperationalDisposition: not "Normal" }) return HungerErrors.Conflict("يلزم تفعيل الحساب يدويًا ومعالجة استثنائه قبل التسليم.");
        }
        else
        {
            if (request.Return is null || request.Return.EffectiveTo != request.BusinessDate || request.Assignment is not null || request.Return.Status != "Ended") return HungerErrors.Invalid("بيانات المراجعة لا تطابق سحبًا نهائيًا؛ الإلغاء يحتاج مسار تصحيح.");
            if (!await db.RiderClientAssignments.AnyAsync(a => a.PlatformRiderAccountId == accountId && a.RiderProfileId == request.RiderProfileId && a.Status == RiderAssignmentStatus.Active && a.EffectiveTo == null, ct)) return HungerErrors.Conflict("المندوب لا يحمل الحساب حاليًا.");
        }
        return null;
    }
    public async Task<Result<HungerWalletReviewResponse>> ReviewWalletAsync(Guid accountId, HungerWalletReviewRequest request, CancellationToken ct = default)
    {
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null || !await FinanceAccess(account.ClientPlatformId, PermissionKeys.Hunger.FinanceManage, ct)) return Result.Failure<HungerWalletReviewResponse>(HungerErrors.Forbidden);
        if (await ValidateWallet(accountId, request, ct) is { } error) return Result.Failure<HungerWalletReviewResponse>(error);
        var state = await ReadWalletState(accountId, ct);
        var period = HungerWalletRules.CurrentPeriod(request.MobileBalance, request.ClosingBalance, request.ProvenAdjustments);
        var attributed = HungerWalletRules.Usable(period, state.Restricted);
        var preview = new HungerWalletReview { PlatformRiderAccountId = accountId, UserId = user.UserId!.Value,
            RequestJson = Serialize(request), InputRevision = state.Revision, ExpiresAtUtc = clock.GetUtcNow().AddMinutes(20) };
        db.Add(preview); await db.SaveChangesAsync(ct);
        return Result.Success(new HungerWalletReviewResponse(preview.Id, period, state.Restricted, attributed,
            HungerWalletRules.RiderBalanceChange(request.Action, attributed), preview.ExpiresAtUtc));
    }
    public async Task<Result<HungerWalletConfirmation>> ConfirmWalletAsync(Guid reviewId, CancellationToken ct = default)
    {
        var preview = await db.Set<HungerWalletReview>().AsNoTracking().SingleOrDefaultAsync(p => p.Id == reviewId && p.UserId == user.UserId, ct);
        if (preview is null || lifecycle is null) return Result.Failure<HungerWalletConfirmation>(HungerErrors.NotFound);
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == preview.PlatformRiderAccountId, ct);
        if (account is null || !await FinanceAccess(account.ClientPlatformId, PermissionKeys.Hunger.FinanceManage, ct)) return Result.Failure<HungerWalletConfirmation>(HungerErrors.Forbidden);
        var request = Read<HungerWalletReviewRequest>(preview.RequestJson);
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear(); await using var locks = await PlatformAccountWriteScope.AcquireAsync(db, [account.Id, request.SupervisorEmployeeId, account.ClientPlatformId], ct);
                await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
                var current = await db.Set<HungerWalletReview>().SingleAsync(p => p.Id == reviewId, ct);
                if (current.ResultJson is not null) return Result.Success(Read<HungerWalletConfirmation>(current.ResultJson));
                if (current.ExpiresAtUtc <= clock.GetUtcNow()) return Result.Failure<HungerWalletConfirmation>(HungerErrors.Conflict("انتهت مراجعة المحفظة."));
                if (await ValidateWallet(account.Id, request, ct) is { } error) return Result.Failure<HungerWalletConfirmation>(error);
                var state = await ReadWalletState(account.Id, ct);
                if (current.InputRevision != state.Revision) return Result.Failure<HungerWalletConfirmation>(HungerErrors.Conflict("تغير الحساب أو الرصيد أو الاستخدام؛ أعد مراجعة المحفظة."));
                var attributed = HungerWalletRules.Usable(HungerWalletRules.CurrentPeriod(request.MobileBalance, request.ClosingBalance, request.ProvenAdjustments), state.Restricted);
                var item = new HungerFinancialEvent { ClientPlatformId = account.ClientPlatformId, SupervisorEmployeeId = request.SupervisorEmployeeId,
                    PlatformRiderAccountId = account.Id, RiderProfileId = request.RiderProfileId, Kind = request.Action == "Deliver" ? "WalletDelivery" : "WalletReturn",
                    Amount = Math.Abs(attributed), RiderBalanceChange = HungerWalletRules.RiderBalanceChange(request.Action, attributed), BusinessDate = request.BusinessDate,
                    IdempotencyKey = $"WalletReview:{reviewId:D}", RequestHash = Hash(current.RequestJson), EvidenceReference = request.WalletEvidence,
                    DetailsJson = Serialize(new { request, state.Restricted, AttributedBalance = attributed }) };
                Result<SimplePlatformAssignmentResponse> result;
                if (request.Action == "Deliver") result = await lifecycle.AssignHungerAccountAsync(account.Id, request.Assignment!, () => db.Add(item), ct);
                else result = await lifecycle.ReturnHungerAccountAsync(account.Id, request.Return!, () => db.Add(item), ct);
                if (result.IsFailure) { db.ChangeTracker.Clear(); return Result.Failure<HungerWalletConfirmation>(result.Error); }
                var response = new HungerWalletConfirmation(item.Id, result.Value!.Id, request.Action, item.RiderBalanceChange);
                current.ResultJson = Serialize(response); await db.SaveChangesAsync(ct);
                if (transaction is not null) await transaction.CommitAsync(ct);
                return Result.Success(response);
            });
        }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure<HungerWalletConfirmation>(HungerErrors.Conflict("تعارض في تأكيد التسليم؛ أعد المراجعة.")); }
    }
}
