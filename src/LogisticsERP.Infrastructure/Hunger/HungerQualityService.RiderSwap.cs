using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Hunger;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService
{
    private async Task SupersedePendingWalletReviews(Guid accountId, CancellationToken ct)
    {
        var pending = await db.Set<HungerWalletReview>().Where(r => r.PlatformRiderAccountId == accountId
            && r.ResultJson == null && r.Status == "Pending").ToArrayAsync(ct);
        foreach (var review in pending) review.Status = "Superseded";
    }

    private static HungerWalletReviewResponse WalletPreview(HungerWalletReview review, HungerWalletReviewRequest request,
        decimal? closing, decimal restricted)
    {
        decimal? period = closing.HasValue ? HungerWalletRules.CurrentPeriod(request.MobileBalance, closing.Value, request.ProvenAdjustments) : null;
        decimal? attributed = period.HasValue ? HungerWalletRules.Usable(period.Value, restricted) : null;
        return new(review.Id, period, restricted, attributed,
            attributed.HasValue ? HungerWalletRules.RiderBalanceChange(request.Action, attributed.Value) : null,
            review.ExpiresAtUtc, closing, closing.HasValue);
    }

    public async Task<Result<HungerRiderSwapReview>> ReviewRiderSwapAsync(Guid accountId, HungerRiderSwapRequest request, CancellationToken ct = default)
    {
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null || !await FinanceAccess(account.ClientPlatformId, PermissionKeys.Hunger.WalletReview, ct))
            return Result.Failure<HungerRiderSwapReview>(HungerErrors.Forbidden);
        if (request.Return is null || request.Handover is null || request.Return.Action != "Return"
            || request.Return.RiderProfileId == request.Handover.ActualRiderProfileId)
            return Result.Failure<HungerRiderSwapReview>(HungerErrors.Invalid("يلزم سحب من السابق وتسليم إلى مندوب مختلف."));
        await using var locks = await PlatformAccountWriteScope.AcquireAsync(db, [accountId, account.ClientPlatformId], ct);
        var handover = request.Return with { Action = "Deliver", RiderProfileId = request.Handover.ActualRiderProfileId,
            Assignment = request.Handover, Return = null };
        if (await ValidateWallet(accountId, request.Return, ct) is { } returnError) return Result.Failure<HungerRiderSwapReview>(returnError);
        if (await ValidateWallet(accountId, handover, ct) is { } handoverError) return Result.Failure<HungerRiderSwapReview>(handoverError);
        var state = await ReadWalletState(accountId, ct);
        var closing = await WalletClosing(accountId, request.Return.BusinessDate, ct);
        var swapId = Guid.CreateVersion7();
        HungerWalletReview Review(HungerWalletReviewRequest input) => new() { PlatformRiderAccountId = accountId,
            UserId = user.UserId!.Value, SwapId = swapId, InputRevision = state.Revision,
            RequestJson = Serialize(input), ExpiresAtUtc = clock.GetUtcNow().AddMinutes(20) };
        var oldReview = Review(request.Return); var newReview = Review(handover);
        var oldPreview = WalletPreview(oldReview, request.Return, closing, state.Restricted);
        var newPreview = WalletPreview(newReview, handover, closing, state.Restricted + Math.Max(0, oldPreview.AttributedBalance ?? 0));
        await SupersedePendingWalletReviews(accountId, ct);
        db.AddRange(oldReview, newReview); await db.SaveChangesAsync(ct);
        return Result.Success(new HungerRiderSwapReview(swapId, oldPreview, newPreview));
    }

    public async Task<Result<HungerRiderSwapConfirmation>> ConfirmRiderSwapAsync(Guid swapId, CancellationToken ct = default)
    {
        var preview = await db.Set<HungerWalletReview>().AsNoTracking().FirstOrDefaultAsync(r => r.SwapId == swapId && r.UserId == user.UserId, ct);
        if (preview is null) return Result.Failure<HungerRiderSwapConfirmation>(HungerErrors.NotFound);
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == preview.PlatformRiderAccountId, ct);
        if (account is null || !await FinanceAccess(account.ClientPlatformId, PermissionKeys.Hunger.WalletReview, ct))
            return Result.Failure<HungerRiderSwapConfirmation>(HungerErrors.Forbidden);
        // The lifecycle service saves each assignment. Both saves must be enclosed by a real transaction.
        if (lifecycle is null || !db.Database.IsRelational())
            return Result.Failure<HungerRiderSwapConfirmation>(HungerErrors.Conflict("التبديل يحتاج معاملة قاعدة بيانات ذرية."));
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var locks = await PlatformAccountWriteScope.AcquireAsync(db, [account.Id, account.ClientPlatformId], ct);
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
                try
                {
                    var reviews = await db.Set<HungerWalletReview>().Where(r => r.SwapId == swapId && r.UserId == user.UserId).ToArrayAsync(ct);
                    if (reviews.Length != 2 || reviews.Any(r => r.PlatformRiderAccountId != account.Id))
                        return Result.Failure<HungerRiderSwapConfirmation>(HungerErrors.NotFound);
                    var pairs = reviews.Select(r => (Review: r, Request: Read<HungerWalletReviewRequest>(r.RequestJson))).ToArray();
                    var old = pairs.Single(p => p.Request.Action == "Return"); var next = pairs.Single(p => p.Request.Action == "Deliver");
                    if (reviews.All(r => r.Status == "Confirmed" && r.ResultJson != null))
                        return Result.Success(new HungerRiderSwapConfirmation(swapId, Read<HungerWalletConfirmation>(old.Review.ResultJson!), Read<HungerWalletConfirmation>(next.Review.ResultJson!)));
                    if (reviews.Any(r => r.Status != "Pending" || r.ResultJson != null || r.ExpiresAtUtc <= clock.GetUtcNow()))
                        return Result.Failure<HungerRiderSwapConfirmation>(HungerErrors.Conflict("انتهت مراجعة التبديل أو تم استبدالها؛ أعد إثبات الرصيد."));
                    var state = await ReadWalletState(account.Id, ct);
                    if (reviews.Any(r => r.InputRevision != state.Revision))
                        return Result.Failure<HungerRiderSwapConfirmation>(HungerErrors.Conflict("تغير الرصيد أو مصادره؛ أعد إثبات الرصيد ومراجعتَي التبديل."));
                    if (await ValidateWallet(account.Id, old.Request, ct) is { } oldError) return Result.Failure<HungerRiderSwapConfirmation>(oldError);
                    if (await ValidateWallet(account.Id, next.Request, ct) is { } nextError) return Result.Failure<HungerRiderSwapConfirmation>(nextError);
                    var closing = await WalletClosing(account.Id, old.Request.BusinessDate, ct);
                    if (!closing.HasValue) return Result.Failure<HungerRiderSwapConfirmation>(HungerErrors.Invalid("رصيد الإغلاق غير معروف؛ التأكيد المالي محظور."));
                    var attributed = WalletPreview(old.Review, old.Request, closing, state.Restricted).AttributedBalance!.Value;
                    HungerFinancialEvent Event(HungerWalletReview review, HungerWalletReviewRequest input, decimal amount) => new() {
                        ClientPlatformId = account.ClientPlatformId, SupervisorEmployeeId = input.SupervisorEmployeeId,
                        PlatformRiderAccountId = account.Id, RiderProfileId = input.RiderProfileId,
                        Kind = input.Action == "Return" ? "WalletReturn" : "WalletDelivery", Amount = Math.Abs(amount),
                        RiderBalanceChange = HungerWalletRules.RiderBalanceChange(input.Action, amount), BusinessDate = input.BusinessDate,
                        IdempotencyKey = $"WalletReview:{review.Id:D}", RequestHash = Hash(review.RequestJson), EvidenceReference = input.WalletEvidence,
                        DetailsJson = Serialize(new { SwapId = swapId, Request = input, AttributedBalance = amount }) };
                    var returned = Event(old.Review, old.Request, attributed);
                    var delivered = Event(next.Review, next.Request, attributed - Math.Max(0, attributed));
                    // Exclude these reviews from invalidation by their own atomic lifecycle writes.
                    foreach (var review in reviews) review.Status = "Confirmed";
                    var release = await lifecycle.ReturnHungerAccountAsync(account.Id, old.Request.Return!, () => db.Add(returned), ct);
                    if (release.IsFailure) return Result.Failure<HungerRiderSwapConfirmation>(release.Error);
                    var assign = await lifecycle.AssignHungerAccountAsync(account.Id, next.Request.Assignment!, () => db.Add(delivered), ct);
                    if (assign.IsFailure) return Result.Failure<HungerRiderSwapConfirmation>(assign.Error);
                    var oldResult = new HungerWalletConfirmation(returned.Id, release.Value!.Id, "Return", returned.RiderBalanceChange);
                    var newResult = new HungerWalletConfirmation(delivered.Id, assign.Value!.Id, "Deliver", delivered.RiderBalanceChange);
                    old.Review.ResultJson = Serialize(oldResult); next.Review.ResultJson = Serialize(newResult);
                    await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
                    return Result.Success(new HungerRiderSwapConfirmation(swapId, oldResult, newResult));
                }
                finally { db.ChangeTracker.Clear(); }
            });
        }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure<HungerRiderSwapConfirmation>(HungerErrors.Conflict("تعارض في التبديل؛ أعد المراجعة.")); }
    }
}
