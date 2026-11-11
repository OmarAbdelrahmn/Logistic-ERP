using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Hunger;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LogisticsERP.Application.Abstractions.Files;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService
{
    private async Task SupersedePendingWalletReviews(Guid accountId, CancellationToken ct)
    {
        var pending = await db.Set<HungerWalletReview>().Where(r => r.PlatformRiderAccountId == accountId
            && r.ResultJson == null && r.Status == "Pending").ToArrayAsync(ct);
        foreach (var review in pending) review.Status = "Superseded";
    }

    private HungerWalletReviewResponse WalletPreview(HungerWalletReview review, HungerWalletReviewRequest request,
        WalletBasis basis, decimal restricted)
    {
        decimal? period = basis.Closing.HasValue ? HungerWalletRules.CurrentPeriod(request.MobileBalance, basis.Closing.Value, basis.Adjustments) : null;
        var attributed = Attributed(request, basis, restricted);
        return new(review.Id, period, restricted, attributed,
            attributed.HasValue ? HungerWalletRules.RiderBalanceChange(request.Action, attributed.Value) : null,
            review.ExpiresAtUtc, basis.Closing, basis.Closing.HasValue, null, basis.Adjustments, basis.Source,
            Cr005 && request.Action == "Deliver" && attributed is not (null or 0));
    }

    public Task<Result<HungerRiderSwapReview>> ReviewRiderSwapAsync(Guid accountId, HungerRiderSwapRequest request, CancellationToken ct = default)
        => Cr005 ? Task.FromResult(Result.Failure<HungerRiderSwapReview>(WalletImageRequired)) : ReviewRiderSwapCore(accountId, request, ct);

    // One screenshot proves the balance for both the return and the hand-over review.
    public async Task<Result<HungerRiderSwapReview>> ReviewRiderSwapWithImageAsync(Guid accountId, HungerRiderSwapRequest request, PrivateFileUpload? image, CancellationToken ct = default)
    {
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null || !await FinanceAccess(account.ClientPlatformId, PermissionKeys.Hunger.WalletReview, ct))
            return Result.Failure<HungerRiderSwapReview>(HungerErrors.Forbidden);
        if (request.Return is null || request.Handover is null) return Result.Failure<HungerRiderSwapReview>(HungerErrors.Invalid("يلزم سحب من السابق وتسليم إلى مندوب مختلف."));
        if (image is null) return Result.Failure<HungerRiderSwapReview>(WalletImageRequired);
        if (files is null || image.ContentType.ToLowerInvariant() is not ("image/jpeg" or "image/png" or "image/webp" or "image/gif" or "image/bmp"))
            return Result.Failure<HungerRiderSwapReview>(PrivateFileErrors.InvalidFile);
        var stored = await files.StoreAsync($"hunger/wallets/{accountId:D}", image, 5 * 1024 * 1024, ct);
        if (stored.IsFailure) return Result.Failure<HungerRiderSwapReview>(stored.Error);
        var keepFile = false;
        try
        {
            var link = "/" + stored.Value!.StoragePath["wwwroot/".Length..];
            var result = await ReviewRiderSwapCore(accountId, request with { Return = request.Return with { WalletEvidence = link } }, ct);
            keepFile = result.IsSuccess;
            return result.IsFailure ? result : Result.Success(result.Value! with { Return = result.Value.Return with { WalletEvidence = link },
                Handover = result.Value.Handover with { WalletEvidence = link } });
        }
        finally
        {
            if (!keepFile) files.DeleteBestEffort(stored.Value!.StoragePath);
        }
    }

    private async Task<Result<HungerRiderSwapReview>> ReviewRiderSwapCore(Guid accountId, HungerRiderSwapRequest request, CancellationToken ct)
    {
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null || !await FinanceAccess(account.ClientPlatformId, PermissionKeys.Hunger.WalletReview, ct))
            return Result.Failure<HungerRiderSwapReview>(HungerErrors.Forbidden);
        if (request.Return is null || request.Handover is null || request.Return.Action != "Return"
            || request.Return.RiderProfileId == request.Handover.ActualRiderProfileId)
            return Result.Failure<HungerRiderSwapReview>(HungerErrors.Invalid("يلزم سحب من السابق وتسليم إلى مندوب مختلف."));
        await using var locks = await PlatformAccountWriteScope.AcquireAsync(db, [accountId, account.ClientPlatformId], ct);
        var handover = request.Return with { Action = "Deliver", RiderProfileId = request.Handover.ActualRiderProfileId,
            Assignment = request.Handover, Return = null, ExceptionApprovalId = request.HandoverExceptionApprovalId };
        if (await ValidateWallet(accountId, request.Return, ct) is { } returnError) return Result.Failure<HungerRiderSwapReview>(returnError);
        if (await ValidateWallet(accountId, handover, ct) is { } handoverError) return Result.Failure<HungerRiderSwapReview>(handoverError);
        var state = await ReadWalletState(accountId, ct);
        var basis = await ReadWalletBasis(accountId, request.Return.BusinessDate, ct);
        var swapId = Guid.CreateVersion7();
        HungerWalletReview Review(HungerWalletReviewRequest input) => new() { PlatformRiderAccountId = accountId,
            UserId = user.UserId!.Value, SwapId = swapId, InputRevision = state.Revision,
            RequestJson = Serialize(input), ExpiresAtUtc = clock.GetUtcNow().AddMinutes(20) };
        var oldReview = Review(request.Return); var newReview = Review(handover);
        var oldPreview = WalletPreview(oldReview, request.Return, basis, state.Restricted);
        var newPreview = WalletPreview(newReview, handover, basis, state.Restricted + Math.Max(0, oldPreview.AttributedBalance ?? 0));
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
                    var basis = await ReadWalletBasis(account.Id, old.Request.BusinessDate, ct);
                    if (Attributed(old.Request, basis, state.Restricted) is not { } attributed)
                        return Result.Failure<HungerRiderSwapConfirmation>(HungerErrors.Invalid("رصيد الإغلاق غير معروف؛ التأكيد المالي محظور."));
                    // No custody proposal here: in a swap a negative wallet passes to the receiver under an approved exception.
                    HungerFinancialEvent Event(HungerWalletReview review, HungerWalletReviewRequest input, decimal amount) => new() {
                        ClientPlatformId = account.ClientPlatformId, SupervisorEmployeeId = input.SupervisorEmployeeId,
                        PlatformRiderAccountId = account.Id, RiderProfileId = input.RiderProfileId,
                        Kind = input.Action == "Return" ? "WalletReturn" : "WalletDelivery", Amount = Math.Abs(amount),
                        RiderBalanceChange = HungerWalletRules.RiderBalanceChange(input.Action, amount), BusinessDate = input.BusinessDate,
                        IdempotencyKey = $"WalletReview:{review.Id:D}", RequestHash = Hash(review.RequestJson), EvidenceReference = input.WalletEvidence,
                        DetailsJson = Serialize(new { SwapId = swapId, Request = input, AttributedBalance = amount, ClosingBalance = basis.Closing,
                            ProvenAdjustments = basis.Adjustments, ClosingSource = basis.Source }),
                        ApprovalStatus = "PendingSeal", Role = "Wallet" };
                    var returned = Event(old.Review, old.Request, attributed);
                    var delivered = Event(next.Review, next.Request, attributed - Math.Max(0, attributed));
                    if (await ConsumeHandoverException(account.Id, next.Request.ExceptionApprovalId, attributed - Math.Max(0, attributed), delivered, ct) is { } exceptionError)
                        return Result.Failure<HungerRiderSwapConfirmation>(exceptionError);
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
