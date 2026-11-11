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
using Microsoft.Extensions.Configuration;
using LogisticsERP.Application.Abstractions.Files;

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
        var settlements = await db.Set<HungerSettlementDocument>().AsNoTracking().Where(d => d.ClientPlatformId == account.ClientPlatformId).OrderBy(d => d.Id).ToArrayAsync(ct);
        var snapshots = await db.Set<HungerQualitySnapshot>().AsNoTracking().Where(s => s.PlatformRiderAccountId == accountId).OrderBy(s => s.Id).ToArrayAsync(ct);
        var operations = await db.Set<HungerDailyOperation>().AsNoTracking().Where(s => s.PlatformRiderAccountId == accountId).OrderBy(s => s.Id).ToArrayAsync(ct);
        var relatedEvents = await db.Set<HungerFinancialEvent>().AsNoTracking().Where(e => e.ClientPlatformId == account.ClientPlatformId && e.PlatformRiderAccountId == null).OrderBy(e => e.Id).ToArrayAsync(ct);
        decimal restricted = 0;
        foreach (var credit in events.Where(e => e.Kind == "WalletReturn" && e.RiderBalanceChange > 0))
        {
            // Paying the rider transfers the claim to the company; only an actual wallet withdrawal releases it.
            var removed = events.Where(e => e.RelatedEventId == credit.Id && (e.Kind is "WithdrawalCompany" or "WithdrawalSupervisor") && e.Status == "Arrived").Sum(e => e.Amount);
            restricted += Math.Max(0, credit.RiderBalanceChange - removed);
        }
        // Linked rights are already restricted above; reserve only additional, unrestricted withdrawals here.
        restricted += events.Where(e => (e.Kind is "WithdrawalCompany" or "WithdrawalSupervisor")
            && e.Status == "PendingArrival" && e.RelatedEventId == null).Sum(e => e.Amount);
        return new(Hash(Serialize(new { account, assignments, details, events, relatedEvents, usages, contributions, settlements, snapshots, operations })), restricted);
    }
    private static bool ValidBalance(decimal value) => Math.Abs(value) <= 1000000000m && decimal.Round(value, 2) == value;
    private bool Cr005 => configuration.GetValue<bool>("Features:HungerFinanceCr005");
    private static readonly OperationError WalletImageRequired = HungerErrors.Invalid("لقطة شاشة المحفظة مطلوبة للتسليم والسحب.");
    // Source: Invoice | Manual | NoPriorPeriod | InvoiceAbsent | Unknown.
    private sealed record WalletBasis(decimal? Closing, decimal Adjustments, string Source);
    private async Task<WalletBasis> ReadWalletBasis(Guid accountId, DateOnly date, CancellationToken ct)
    {
        var month = new DateOnly(date.Year, date.Month, 1);
        var previous = month.AddMonths(-1);
        var close = await db.Set<HungerSettlementDocument>().AsNoTracking().SingleOrDefaultAsync(d => d.PlatformRiderAccountId == accountId
            && d.Kind == "MonthClose" && d.ChargeFrom == previous && d.Status == "Matched", ct);
        if (close?.WalletBalance is { } balance)
            return new(balance, await ClosingAdjustments(accountId, close.Id, date, ct),
                close.IdempotencyKey.StartsWith("InvoiceClose:", StringComparison.Ordinal) ? "Invoice" : "Manual");
        if (!await HasPriorWalletPeriod(accountId, month, ct)) return new(0, 0, "NoPriorPeriod");
        // An account missing from a matched invoice that carried rider balances had no wallet deduction that month.
        var platform = await db.PlatformRiderAccounts.AsNoTracking().Where(a => a.Id == accountId).Select(a => a.ClientPlatformId).SingleAsync(ct);
        return await db.Set<HungerSettlementDocument>().AnyAsync(d => d.ClientPlatformId == platform && d.Kind == "Invoice"
            && d.InvoicePeriod == previous && d.Status == "Matched" && d.RiderBalanceTotal != null, ct)
            ? new(0, 0, "InvoiceAbsent") : new(null, 0, "Unknown");
    }
    private static decimal? Attributed(HungerWalletReviewRequest request, WalletBasis basis, decimal restricted) => basis.Closing.HasValue
        ? HungerWalletRules.Usable(HungerWalletRules.CurrentPeriod(request.MobileBalance, basis.Closing.Value, basis.Adjustments), restricted) : null;
    // What the supervisor's custody cannot cover of the amount needed to bring a negative wallet back to zero.
    private async Task<decimal> CustodyShortfall(Guid platform, Guid supervisor, decimal attributed, CancellationToken ct)
    {
        if (!Cr005 || attributed >= 0) return 0;
        var c = await FinanceConfig(platform, supervisor, ct);
        return Math.Max(0, -attributed - (c is null ? 0 : await CustodyBalance(c, ct)));
    }
    private async Task SetWalletDisposition(Guid accountId, string from, string to, string reason, CancellationToken ct)
    {
        var details = await db.Set<PlatformAccountBusinessDetails>().SingleOrDefaultAsync(d => d.PlatformRiderAccountId == accountId, ct);
        if (details is null || details.OperationalDisposition != from) return;
        var before = Serialize(details); details.OperationalDisposition = to; details.DispositionReason = reason;
        db.Add(new PlatformAccountBusinessRevision { PlatformRiderAccountId = accountId, EffectiveFrom = BusinessToday, Reason = reason, BeforeJson = before, AfterJson = Serialize(details) });
    }
    // T3-4: the account is unavailable until the wallet is zeroed; the proposal feeds the normal custody funding request.
    private async Task RaiseReturnProposal(HungerFinancialEvent returned, decimal shortfall, CancellationToken ct)
    {
        db.Add(new HungerFinancialEvent { ClientPlatformId = returned.ClientPlatformId, SupervisorEmployeeId = returned.SupervisorEmployeeId,
            PlatformRiderAccountId = returned.PlatformRiderAccountId, RiderProfileId = returned.RiderProfileId, RelatedEventId = returned.Id,
            Kind = "CustodyProposal", Status = "Open", Amount = shortfall, BusinessDate = returned.BusinessDate,
            IdempotencyKey = $"CustodyProposal:{returned.Id:D}", RequestHash = returned.RequestHash, EvidenceReference = returned.EvidenceReference, Role = "Custody" });
        await SetWalletDisposition(returned.PlatformRiderAccountId!.Value, "Normal", "WalletPending", "محفظة سالبة عند السحب والعهدة لا تكفي لتصفيرها.", ct);
    }
    private async Task ResolveReturnProposals(Guid accountId, CancellationToken ct)
    {
        var open = await (from p in db.Set<HungerFinancialEvent>() join r in db.Set<HungerFinancialEvent>() on p.RelatedEventId equals r.Id
            where p.PlatformRiderAccountId == accountId && p.Kind == "CustodyProposal" && p.Status == "Open" && r.Kind == "WalletReturn" select p).ToArrayAsync(ct);
        foreach (var proposal in open) proposal.Status = "Resolved";
        await SetWalletDisposition(accountId, "WalletPending", "Normal", "تم تصفير المحفظة وإثباته عند التسليم.", ct);
    }
    // Hand-over needs a zero attributed wallet or an approved, unused exception for exactly that amount.
    private async Task<OperationError?> ConsumeHandoverException(Guid accountId, Guid? approvalId, decimal amount, HungerFinancialEvent item, CancellationToken ct)
    {
        if (!Cr005 || amount == 0) return null;
        var approval = approvalId.HasValue ? await db.Set<HungerExceptionApproval>().SingleOrDefaultAsync(e => e.Id == approvalId, ct) : null;
        if (approval is null || approval.Kind != "NonZeroHandover" || approval.PlatformRiderAccountId != accountId || approval.Status != "Approved"
            || approval.ConsumedByEventId.HasValue || approval.Amount != amount)
            return HungerErrors.Conflict("التسليم برصيد منسوب غير صفري يحتاج استثناءً معتمدًا غير مستخدم بنفس المبلغ لهذا الحساب.");
        approval.Status = "Consumed"; approval.ConsumedByEventId = item.Id; item.ExceptionApprovalId = approval.Id;
        return null;
    }
    private async Task<OperationError?> ValidateWallet(Guid accountId, HungerWalletReviewRequest request, CancellationToken ct)
    {
        if (request.Action is not ("Deliver" or "Return") || request.BusinessDate == default || request.BusinessDate > BusinessToday
            || !ValidBalance(request.MobileBalance) || request.WalletEvidence is null || request.WalletEvidence.Length > 1000)
            return HungerErrors.Invalid("أدخل رصيدًا صريحًا وتاريخه.");
        if (!await db.Employees.AnyAsync(e => e.Id == request.SupervisorEmployeeId, ct)) return HungerErrors.Invalid("المشرف غير معروف.");
        if (request.Action == "Deliver")
        {
            if (request.Assignment is null || request.Assignment.ActualRiderProfileId != request.RiderProfileId || request.Assignment.EffectiveFrom != request.BusinessDate || request.Return is not null) return HungerErrors.Invalid("بيانات مراجعة المحفظة لا تطابق طلب التسليم.");
            var details = await db.Set<PlatformAccountBusinessDetails>().AsNoTracking().SingleOrDefaultAsync(d => d.PlatformRiderAccountId == accountId, ct);
            // An account parked for a negative wallet reopens only on a hand-over whose screenshot shows the wallet back at zero.
            if (details is { OperationalDisposition: not "Normal" } && !(Cr005 && details.OperationalDisposition == "WalletPending" && request.MobileBalance == 0)) return HungerErrors.Conflict("يلزم تفعيل الحساب يدويًا ومعالجة استثنائه قبل التسليم.");
        }
        else
        {
            if (request.Return is null || request.Return.EffectiveTo != request.BusinessDate || request.Assignment is not null || request.Return.Status != "Ended") return HungerErrors.Invalid("بيانات المراجعة لا تطابق سحبًا نهائيًا؛ الإلغاء يحتاج مسار تصحيح.");
            if (!await db.RiderClientAssignments.AnyAsync(a => a.PlatformRiderAccountId == accountId && a.RiderProfileId == request.RiderProfileId && a.Status == RiderAssignmentStatus.Active && a.EffectiveTo == null, ct)) return HungerErrors.Conflict("المندوب لا يحمل الحساب حاليًا.");
        }
        return null;
    }
    private async Task<bool> HasPriorWalletPeriod(Guid accountId, DateOnly month, CancellationToken ct) =>
        await db.RiderClientAssignments.AnyAsync(a => a.PlatformRiderAccountId == accountId && a.EffectiveFrom < month, ct)
        || await db.PlatformAccountDayUsages.AnyAsync(a => a.PlatformRiderAccountId == accountId && a.BusinessDate < month, ct)
        || await db.Set<PlatformAccountDayContribution>().AnyAsync(a => a.PlatformRiderAccountId == accountId && a.BusinessDate < month, ct)
        || await db.Set<HungerFinancialEvent>().AnyAsync(a => a.PlatformRiderAccountId == accountId && a.BusinessDate < month, ct)
        || await db.Set<HungerSettlementDocument>().AnyAsync(a => a.PlatformRiderAccountId == accountId && a.ChargeFrom < month, ct);
    private Task<decimal> ClosingAdjustments(Guid accountId, Guid? closeId, DateOnly date, CancellationToken ct) =>
        db.Set<HungerSettlementDocument>().Where(d => closeId != null && d.ClosingSnapshotId == closeId
            && d.PlatformRiderAccountId == accountId && d.Kind == "Adjustment" && d.ObservedOn <= date && d.Status == "Matched")
            .SumAsync(d => d.WalletBalance ?? 0, ct);
    private async Task<decimal?> WalletClosing(Guid accountId, DateOnly date, CancellationToken ct) => (await ReadWalletBasis(accountId, date, ct)).Closing;
    public async Task<Result<HungerWalletReviewResponse>> ReviewWalletWithImageAsync(Guid accountId, HungerWalletReviewRequest request, PrivateFileUpload? image, CancellationToken ct = default)
    {
        // Authorize and validate before writing any file. Only the server supplies the image link.
        request = request with { WalletEvidence = string.Empty };
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null || !await FinanceAccess(account.ClientPlatformId, PermissionKeys.Hunger.WalletReview, ct))
            return Result.Failure<HungerWalletReviewResponse>(HungerErrors.Forbidden);
        if (await ValidateWallet(accountId, request, ct) is { } error)
            return Result.Failure<HungerWalletReviewResponse>(error);
        if (image is null) return Cr005 ? Result.Failure<HungerWalletReviewResponse>(WalletImageRequired) : await ReviewWalletCore(accountId, request, ct);
        if (files is null || image.ContentType.ToLowerInvariant() is not ("image/jpeg" or "image/png" or "image/webp" or "image/gif" or "image/bmp"))
            return Result.Failure<HungerWalletReviewResponse>(PrivateFileErrors.InvalidFile);
        var stored = await files.StoreAsync($"hunger/wallets/{accountId:D}", image, 5 * 1024 * 1024, ct);
        if (stored.IsFailure) return Result.Failure<HungerWalletReviewResponse>(stored.Error);
        var keepFile = false;
        try
        {
            var link = "/" + stored.Value!.StoragePath["wwwroot/".Length..];
            var result = await ReviewWalletCore(accountId, request with { WalletEvidence = link }, ct);
            keepFile = result.IsSuccess;
            return result;
        }
        finally
        {
            if (!keepFile) files.DeleteBestEffort(stored.Value!.StoragePath);
        }
    }

    // Without an image the client would be supplying its own evidence text, which CR-005 no longer accepts.
    public Task<Result<HungerWalletReviewResponse>> ReviewWalletAsync(Guid accountId, HungerWalletReviewRequest request, CancellationToken ct = default)
        => Cr005 ? Task.FromResult(Result.Failure<HungerWalletReviewResponse>(WalletImageRequired)) : ReviewWalletCore(accountId, request, ct);
    private async Task<Result<HungerWalletReviewResponse>> ReviewWalletCore(Guid accountId, HungerWalletReviewRequest request, CancellationToken ct)
    {
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null || !await FinanceAccess(account.ClientPlatformId, PermissionKeys.Hunger.WalletReview, ct)) return Result.Failure<HungerWalletReviewResponse>(HungerErrors.Forbidden);
        if (await ValidateWallet(accountId, request, ct) is { } error) return Result.Failure<HungerWalletReviewResponse>(error);
        await using var locks = await PlatformAccountWriteScope.AcquireAsync(db, [accountId, account.ClientPlatformId], ct);
        var state = await ReadWalletState(accountId, ct);
        var basis = await ReadWalletBasis(accountId, request.BusinessDate, ct);
        var preview = new HungerWalletReview { PlatformRiderAccountId = accountId, UserId = user.UserId!.Value,
            RequestJson = Serialize(request), InputRevision = state.Revision, ExpiresAtUtc = clock.GetUtcNow().AddMinutes(20) };
        await SupersedePendingWalletReviews(accountId, ct);
        db.Add(preview); await db.SaveChangesAsync(ct);
        return Result.Success(WalletPreview(preview, request, basis, state.Restricted) with
            { WalletEvidence = string.IsNullOrEmpty(request.WalletEvidence) ? null : request.WalletEvidence });
    }
    public async Task<Result<HungerWalletConfirmation>> ConfirmWalletAsync(Guid reviewId, CancellationToken ct = default)
    {
        var preview = await db.Set<HungerWalletReview>().AsNoTracking().SingleOrDefaultAsync(p => p.Id == reviewId && p.UserId == user.UserId, ct);
        if (preview is null) return Result.Failure<HungerWalletConfirmation>(HungerErrors.NotFound);
        if (preview.SwapId.HasValue) return Result.Failure<HungerWalletConfirmation>(HungerErrors.Conflict("أكد مراجعتَي التبديل معًا من مسار التبديل."));
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == preview.PlatformRiderAccountId, ct);
        if (account is null || !await FinanceAccess(account.ClientPlatformId, PermissionKeys.Hunger.WalletReview, ct)) return Result.Failure<HungerWalletConfirmation>(HungerErrors.Forbidden);
        var request = Read<HungerWalletReviewRequest>(preview.RequestJson);
        if (! (await WalletClosing(account.Id, request.BusinessDate, ct)).HasValue)
            return Result.Failure<HungerWalletConfirmation>(HungerErrors.Invalid("رصيد إغلاق الفترة السابقة غير معروف؛ التأكيد المالي محظور حتى تسجيل الإغلاق."));
        if (lifecycle is null) return Result.Failure<HungerWalletConfirmation>(HungerErrors.NotFound);
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear(); await using var locks = await PlatformAccountWriteScope.AcquireAsync(db, [account.Id, request.SupervisorEmployeeId, account.ClientPlatformId], ct);
                await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
                var current = await db.Set<HungerWalletReview>().SingleAsync(p => p.Id == reviewId, ct);
                if (current.ResultJson is not null) return Result.Success(Read<HungerWalletConfirmation>(current.ResultJson));
                if (current.Status != "Pending") return Result.Failure<HungerWalletConfirmation>(HungerErrors.Conflict("تم استبدال مراجعة المحفظة؛ أعد المراجعة."));
                if (current.ExpiresAtUtc <= clock.GetUtcNow()) return Result.Failure<HungerWalletConfirmation>(HungerErrors.Conflict("انتهت مراجعة المحفظة."));
                if (await ValidateWallet(account.Id, request, ct) is { } error) return Result.Failure<HungerWalletConfirmation>(error);
                var state = await ReadWalletState(account.Id, ct);
                if (current.InputRevision != state.Revision) return Result.Failure<HungerWalletConfirmation>(HungerErrors.Conflict("تغير الحساب أو الرصيد أو الاستخدام؛ أعد مراجعة المحفظة."));
                var basis = await ReadWalletBasis(account.Id, request.BusinessDate, ct);
                if (Attributed(request, basis, state.Restricted) is not { } attributed)
                    return Result.Failure<HungerWalletConfirmation>(HungerErrors.Invalid("رصيد الإغلاق غير معروف؛ التأكيد المالي محظور."));
                var item = new HungerFinancialEvent { ClientPlatformId = account.ClientPlatformId, SupervisorEmployeeId = request.SupervisorEmployeeId,
                    PlatformRiderAccountId = account.Id, RiderProfileId = request.RiderProfileId, Kind = request.Action == "Deliver" ? "WalletDelivery" : "WalletReturn",
                    Amount = Math.Abs(attributed), RiderBalanceChange = HungerWalletRules.RiderBalanceChange(request.Action, attributed), BusinessDate = request.BusinessDate,
                    IdempotencyKey = $"WalletReview:{reviewId:D}", RequestHash = Hash(current.RequestJson), EvidenceReference = request.WalletEvidence,
                    DetailsJson = Serialize(new { request, state.Restricted, AttributedBalance = attributed, ClosingBalance = basis.Closing,
                        ProvenAdjustments = basis.Adjustments, ClosingSource = basis.Source }),
                    ApprovalStatus = "PendingSeal", Role = "Wallet" };
                if (request.Action == "Deliver" && await ConsumeHandoverException(account.Id, request.ExceptionApprovalId, attributed, item, ct) is { } exceptionError)
                { db.ChangeTracker.Clear(); return Result.Failure<HungerWalletConfirmation>(exceptionError); }
                Result<SimplePlatformAssignmentResponse> result;
                current.Status = "Confirmed";
                if (request.Action == "Return" && await CustodyShortfall(account.ClientPlatformId, request.SupervisorEmployeeId, attributed, ct) is > 0 and var shortfall)
                    await RaiseReturnProposal(item, shortfall, ct);
                if (Cr005 && request.Action == "Deliver" && request.MobileBalance == 0) await ResolveReturnProposals(account.Id, ct);
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
