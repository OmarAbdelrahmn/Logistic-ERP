using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService : IHungerFinanceService
{
    private static HungerMoneyResult MoneyResult(HungerFinancialEvent e) => new(e.Id, e.Kind, e.Amount, e.RiderBalanceChange, e.Status, e.ApprovalStatus);
    private DateOnly BusinessToday => DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
    private static bool Money(decimal value) => value > 0 && value <= 1000000000m && decimal.Round(value, 2) == value;
    private bool ValidEvent(DateOnly date, string key, string evidence) => date != default && date <= BusinessToday
        && !string.IsNullOrWhiteSpace(key) && key.Length <= 150 && !string.IsNullOrWhiteSpace(evidence) && evidence.Length <= 1000;
    private async Task<bool> FinanceAccess(Guid platform, string permission, CancellationToken ct) =>
        (await Platforms(permission, ct)).Contains(platform) && configuration.GetValue<bool>("Features:HungerFinance");
    private async Task<Result<T>> FinancialWrite<T>(Guid[] lockIds, Func<Task<Result<T>>> body, CancellationToken ct)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear(); await using var locks = await PlatformAccountWriteScope.AcquireAsync(db, lockIds, ct);
                var result = await body();
                if (result.IsFailure) { db.ChangeTracker.Clear(); return result; }
                await db.SaveChangesAsync(ct); return result;
            });
        }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure<T>(HungerErrors.Conflict("تعارض في العملية المالية؛ أعد المحاولة بنفس المفتاح.")); }
    }
    private Task<HungerFinanceConfiguration?> FinanceConfig(Guid platform, Guid supervisor, CancellationToken ct) =>
        db.Set<HungerFinanceConfiguration>().SingleOrDefaultAsync(c => c.ClientPlatformId == platform && c.SupervisorEmployeeId == supervisor && c.IsActive, ct);
    private Task<decimal> CustodyBalance(HungerFinanceConfiguration c, CancellationToken ct) => db.Set<TreasuryPosting>().IgnoreQueryFilters()
        .Where(p => p.ClientPlatformId == c.ClientPlatformId && p.SupervisorEmployeeId == c.SupervisorEmployeeId && p.SubAccountId == c.CustodySubAccountId).SumAsync(p => p.Amount, ct);
    private async Task<bool> PostingAccounts(HungerFinanceConfiguration c, Guid from, Guid to, CancellationToken ct)
    {
        if (from == to) return false;
        var accounts = await db.Set<TreasurySubAccount>().AsNoTracking().Where(a => (a.Id == from || a.Id == to) && a.IsActive && a.IsPosting && (a.SponsorId == null || a.SponsorId == c.SponsorId)).ToArrayAsync(ct);
        return accounts.Length == 2 && accounts.Single(a => a.Id == from).AllowedDirections != TreasuryDirections.InOnly
            && accounts.Single(a => a.Id == to).AllowedDirections != TreasuryDirections.OutOnly;
    }
    private void StageTransfer(HungerFinanceConfiguration c, HungerFinancialEvent source, Guid from, Guid to, decimal amount)
    {
        foreach (var item in new[] { (Id: from, Amount: -amount), (Id: to, Amount: amount) })
            db.Add(new TreasuryPosting { ClientPlatformId = c.ClientPlatformId, SupervisorEmployeeId = c.SupervisorEmployeeId,
                SourceEventId = source.Id, SubAccountId = item.Id, SponsorId = c.SponsorId, BatchId = source.Id, Revision = 0,
                Kind = TreasuryEntryKind.Business, Amount = item.Amount, OccurredAtUtc = clock.GetUtcNow(), ActorUserId = user.UserId!.Value,
                ActorNameAr = "Hunger authorized posting", Reference = $"Hunger:{source.Id:D}", Reason = source.Kind });
    }
    public async Task<Result> ConfigureFinanceAsync(HungerFinanceSetup request, CancellationToken ct = default)
    {
        if (!await FinanceAccess(request.PlatformId, PermissionKeys.Hunger.FinanceConfigure, ct)) return Result.Failure(HungerErrors.Forbidden);
        var ids = new[] { request.CustodySubAccountId, request.CollectionsSubAccountId, request.WalletSubAccountId, request.RiderClearingSubAccountId };
        if (ids.Distinct().Count() != 4 || string.IsNullOrWhiteSpace(request.EvidenceReference) || request.EvidenceReference.Length > 1000
            || !await db.Employees.AnyAsync(e => e.Id == request.SupervisorEmployeeId, ct) || !await db.Sponsors.AnyAsync(s => s.Id == request.SponsorId, ct)) return Result.Failure(HungerErrors.Invalid("يلزم ربط محاسبي صريح ومختلف ودليل اعتماد ومشرف وجهة صحيحة."));
        var accounts = await db.Set<TreasurySubAccount>().AsNoTracking().Where(a => ids.Contains(a.Id) && a.IsPosting && a.IsActive && (a.SponsorId == null || a.SponsorId == request.SponsorId)).ToArrayAsync(ct);
        if (accounts.Length != 4 || accounts.Any(a => a.AllowedDirections != TreasuryDirections.Dual)) return Result.Failure(HungerErrors.Invalid("الحسابات الأربعة يجب أن تكون نشطة وترحيلية وتقبل الاتجاهين."));
        var result = await FinancialWrite([request.PlatformId, request.SupervisorEmployeeId], async () =>
        {
            var existing = await db.Set<HungerFinanceConfiguration>().SingleOrDefaultAsync(c => c.ClientPlatformId == request.PlatformId && c.SupervisorEmployeeId == request.SupervisorEmployeeId, ct);
            if (existing is not null && Convert.ToBase64String(existing.RowVersion) != request.RowVersion) return Result.Failure<Guid>(HungerErrors.Conflict("تغير الربط المحاسبي."));
            if (existing is not null && await db.Set<HungerFinancialEvent>().AnyAsync(e => e.ClientPlatformId == request.PlatformId && e.SupervisorEmployeeId == request.SupervisorEmployeeId, ct)
                && (existing.SponsorId != request.SponsorId || existing.CustodySubAccountId != request.CustodySubAccountId || existing.CollectionsSubAccountId != request.CollectionsSubAccountId
                    || existing.WalletSubAccountId != request.WalletSubAccountId || existing.RiderClearingSubAccountId != request.RiderClearingSubAccountId))
                return Result.Failure<Guid>(HungerErrors.Conflict("لا يمكن تغيير ربط استخدم ماليًا؛ يلزم مسار ترحيل معتمد."));
            var item = existing ?? new HungerFinanceConfiguration { ClientPlatformId = request.PlatformId, SupervisorEmployeeId = request.SupervisorEmployeeId };
            item.SponsorId = request.SponsorId; item.CustodySubAccountId = request.CustodySubAccountId; item.CollectionsSubAccountId = request.CollectionsSubAccountId;
            item.WalletSubAccountId = request.WalletSubAccountId; item.RiderClearingSubAccountId = request.RiderClearingSubAccountId;
            item.EvidenceReference = request.EvidenceReference; item.IsActive = request.IsActive;
            if (existing is null) db.Add(item); return Result.Success(item.Id);
        }, ct);
        return result.IsFailure ? Result.Failure(result.Error) : Result.Success();
    }
    public Task<Result<HungerMoneyResult>> PostMoneyAsync(HungerMoneyRequest request, CancellationToken ct = default)
        => PostMoneyCore(request, null, ct);
    private async Task<Result<HungerMoneyResult>> PostMoneyCore(HungerMoneyRequest request, Guid? requestedEventId, CancellationToken ct)
    {
        if (!await FinanceAccess(request.PlatformId, MoneyPermission(request.Kind), ct)) return Result.Failure<HungerMoneyResult>(HungerErrors.Forbidden);
        if (!Money(request.Amount) || !ValidEvent(request.BusinessDate, request.IdempotencyKey, request.EvidenceReference)
            || request.Kind is not ("Funding" or "TopUpAdvance" or "TopUpCollected" or "Collection" or "WithdrawalCompany" or "WithdrawalSupervisor" or "RiderPayment")) return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("حدد نوع العملية والمبلغ والتاريخ ومصدر الإثبات."));
        // CR-005 T1: custody is funded only through request -> accountant approval -> teller delivery.
        if (Cr005 && request.Kind == "Funding" && !requestedEventId.HasValue)
            return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("تمويل العهدة يمر بطلب المشرف ثم اعتماد المحاسب ثم تأكيد التسليم."));
        var withdrawal = request.Kind is "WithdrawalCompany" or "WithdrawalSupervisor";
        if (withdrawal && (!request.SourceWalletBalance.HasValue || !ValidBalance(request.SourceWalletBalance.Value)
            || request.SourceWalletBalance < request.Amount || request.DestinationType is not ("Company" or "Supervisor")
            || !request.DestinationOwnerId.HasValue || string.IsNullOrWhiteSpace(request.TransferReference) || request.TransferReference.Length > 200
            || !request.TransferDate.HasValue || request.TransferDate != request.BusinessDate
            || request.AgainstReservedRights && !request.RelatedEventId.HasValue))
            return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("السحب يحتاج لقطة رصيد المصدر ومالك ونوع الوجهة ومرجع وتاريخ التحويل ومصدر الحق المحجوز إن وجد."));
        var channel = withdrawal ? request.DestinationChannel : null;
        if (withdrawal && (channel is null ? Cr005 : channel is not ("Stc" or "Cashbox" or "Bank") || channel == "Stc" != (request.Kind == "WithdrawalSupervisor")))
            return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("حدد وجهة السحب: STC للمشرف، أو خزنة أو بنك للشركة."));
        var hash = Hash(Serialize(request));
        return await FinancialWrite([request.PlatformId, request.SupervisorEmployeeId, request.AccountId ?? request.PlatformId], async () =>
        {
            var old = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.ClientPlatformId == request.PlatformId && e.IdempotencyKey == request.IdempotencyKey, ct);
            if (old is not null) return old.RequestHash == hash ? Result.Success(MoneyResult(old)) : Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("مفتاح العملية مستخدم لطلب مختلف."));
            if (requestedEventId.HasValue)
            {
                var pendingRequest = await db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == requestedEventId, ct);
                if (pendingRequest.Status != (Cr005 && request.Kind == "Funding" ? "Approved" : "Requested"))
                    return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("الطلب منفذ مسبقًا أو لم يعتمد بعد."));
                pendingRequest.Status = "Executed";
                // The shortfall that held this request back is covered once it executes.
                foreach (var proposal in await db.Set<HungerFinancialEvent>().Where(e => e.Kind == "CustodyProposal" && e.Status == "Open" && e.RelatedEventId == requestedEventId).ToArrayAsync(ct))
                    proposal.Status = "Resolved";
            }
            var c = await FinanceConfig(request.PlatformId, request.SupervisorEmployeeId, ct);
            if (c is null) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("لم يعتمد ربط حسابات المشرف المالية."));
            var supersededId = request.SupersedesEventId ?? Guid.Empty;
            var topUp = request.Kind is "TopUpAdvance" or "TopUpCollected";
            HungerEvidenceFile[] screenshots = [];
            if (Cr005 && topUp)
            {
                screenshots = await db.Set<HungerEvidenceFile>().Where(x => x.Id == request.EvidenceBeforeId || x.Id == request.EvidenceAfterId).ToArrayAsync(ct);
                if (!request.EvidenceBeforeId.HasValue || !request.EvidenceAfterId.HasValue || screenshots.Length != 2
                    || screenshots.Any(x => x.ClientPlatformId != request.PlatformId || x.PlatformRiderAccountId != request.AccountId || x.FinancialEventId.HasValue)
                    || screenshots.Single(x => x.Id == request.EvidenceBeforeId).Purpose != "WalletBefore"
                    || screenshots.Single(x => x.Id == request.EvidenceAfterId).Purpose != "WalletAfter")
                    return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("الشحن يحتاج لقطتي المحفظة قبل وبعد لنفس الحساب وغير مستخدمتين."));
            }
            else if (Cr005 && withdrawal)
            {
                screenshots = await db.Set<HungerEvidenceFile>().Where(x => x.Id == request.WalletEvidenceId).ToArrayAsync(ct);
                if (!request.WalletEvidenceId.HasValue || screenshots.Length != 1 || screenshots[0].Purpose != "Wallet" || screenshots[0].FinancialEventId.HasValue
                    || screenshots[0].ClientPlatformId != request.PlatformId || screenshots[0].PlatformRiderAccountId != request.AccountId)
                    return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("السحب يحتاج لقطة محفظة غير مستخدمة لنفس الحساب."));
            }
            HungerFinancialEvent? superseded = null;
            if (request.SupersedesEventId.HasValue)
            {
                superseded = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == supersededId && e.ClientPlatformId == request.PlatformId, ct);
                // Only the row that still carries the money effect can be replaced, and only within its own family.
                if (superseded is null || superseded.ApprovalStatus != "Rejected" || superseded.SupersedesEventId.HasValue
                    || SupersedeFamily(superseded.Kind) is null || SupersedeFamily(superseded.Kind) != SupersedeFamily(request.Kind)
                    || superseded.PlatformRiderAccountId != request.AccountId || superseded.SupervisorEmployeeId != request.SupervisorEmployeeId)
                    return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("البديل يجب أن يخص عملية شحن أو سحب مرفوضة لنفس الحساب والمشرف."));
                if (await db.Set<HungerFinancialEvent>().AnyAsync(e => e.SupersedesEventId == supersededId && e.ApprovalStatus != "Rejected" && e.ApprovalStatus != "Cancelled", ct))
                    return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("للعملية المرفوضة بديل قيد الاعتماد."));
            }
            if (request.Kind != "Funding" && (!request.AccountId.HasValue || !await db.PlatformRiderAccounts.AnyAsync(a => a.Id == request.AccountId && a.ClientPlatformId == request.PlatformId, ct))) return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("حساب هنقر مطلوب."));
            if (request.RiderProfileId.HasValue && !await db.RiderProfiles.AnyAsync(r => r.Id == request.RiderProfileId, ct)) return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("المندوب غير معروف."));
            var from = request.Kind switch { "Funding" or "RiderPayment" => request.CounterSubAccountId ?? Guid.Empty, "Collection" => c.RiderClearingSubAccountId,
                "TopUpAdvance" or "TopUpCollected" => c.CustodySubAccountId, _ => c.WalletSubAccountId };
            var to = request.Kind switch { "Funding" => c.CustodySubAccountId, "TopUpAdvance" or "TopUpCollected" => c.WalletSubAccountId,
                "Collection" => c.CollectionsSubAccountId, "RiderPayment" => c.RiderClearingSubAccountId, _ => request.CounterSubAccountId ?? Guid.Empty };
            if (!await PostingAccounts(c, from, to, ct) || request.Kind == "Collection" && to == c.CustodySubAccountId
                || request.Kind.StartsWith("Withdrawal", StringComparison.Ordinal) && to == c.CustodySubAccountId) return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("وجهة العملية غير صالحة؛ التحصيل والسحب لا يعيدان تمويل العهدة."));
            if (request.Kind is "TopUpAdvance" or "TopUpCollected" && await CustodyBalance(c, ct) + (superseded?.Amount ?? 0) < request.Amount) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("عهدة المشرف لا تكفي للشحن."));
            // T1-4: a custody ledger that disagrees with its events blocks further top-ups unless that exact difference was approved.
            if (Cr005 && topUp && await CustodySettlement(c, ct) is { Balanced: false, ExceptionApproved: false })
                return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("تسوية عهدة المشرف غير متطابقة؛ عالج الفرق أو اعتمد استثناءً بنفس المبلغ قبل الشحن."));
            var related = request.RelatedEventId.HasValue ? await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == request.RelatedEventId && e.ClientPlatformId == request.PlatformId, ct) : null;
            if (request.Kind is "Collection" or "RiderPayment")
            {
                if (related is null || related.PlatformRiderAccountId != request.AccountId || related.RiderProfileId != request.RiderProfileId
                    || request.Kind == "Collection" && related.RiderBalanceChange >= 0 || request.Kind == "RiderPayment" && related.RiderBalanceChange <= 0) return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("اربط التحصيل أو الصرف بواقعة ذمة الطرف الصحيحة."));
                var paid = await db.Set<HungerFinancialEvent>().Where(e => e.RelatedEventId == related.Id && e.Kind == request.Kind).SumAsync(e => e.Amount, ct);
                if (paid + request.Amount > Math.Abs(related.RiderBalanceChange)) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("المبلغ يتجاوز المتبقي من ذمة الطرف."));
            }
            if (request.Kind is "TopUpAdvance" or "TopUpCollected" && (!request.RiderProfileId.HasValue
                || !await db.RiderClientAssignments.AnyAsync(a => a.PlatformRiderAccountId == request.AccountId && a.RiderProfileId == request.RiderProfileId
                    && a.Status != LogisticsERP.Domain.Enums.RiderAssignmentStatus.Cancelled && a.EffectiveFrom <= request.BusinessDate
                    && (a.EffectiveTo == null || a.EffectiveTo >= request.BusinessDate), ct))) return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("الشحن يجب أن يخص المندوب المستخدم للحساب بتاريخ العملية."));
            if (withdrawal)
            {
                var pending = await db.Set<HungerFinancialEvent>().Where(e => e.PlatformRiderAccountId == request.AccountId
                    && (e.Kind == "WithdrawalCompany" || e.Kind == "WithdrawalSupervisor") && e.Status == "PendingArrival" && e.Id != supersededId).SumAsync(e => e.Amount, ct);
                if (pending + request.Amount > request.SourceWalletBalance!.Value)
                    return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("رصيد المصدر لا يكفي بعد حجز السحوبات المنتظرة."));
                var rights = await ReadWalletState(request.AccountId!.Value, ct);
                if (related is null && request.Amount > request.SourceWalletBalance.Value - rights.Restricted)
                    return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("السحب يمس حقوقًا محجوزة؛ حدد مصدر الحق المحجوز."));
            }
            if (withdrawal && !await ValidWithdrawalDestination(c, request.Kind, request.DestinationType, request.DestinationOwnerId, to, ct))
                return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("مالك وجهة السحب ليس الشركة أو مشرفًا معتمدًا لهذه الوجهة."));
            if (withdrawal && !await ChannelMatches(channel, to, ct))
                return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("الحساب المختار لا يطابق وجهة السحب (خزنة أو بنك)."));
            if (withdrawal && request.RelatedEventId.HasValue && related is null)
                return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("مصدر الحق المحجوز غير موجود."));
            if (withdrawal && related is not null)
            {
                if (related.Kind != "WalletReturn" || related.PlatformRiderAccountId != request.AccountId || related.RiderBalanceChange <= 0)
                    return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("مصدر الحق المحجوز غير صالح."));
                var withdrawn = await db.Set<HungerFinancialEvent>().Where(e => e.RelatedEventId == related.Id && (e.Kind == "WithdrawalCompany" || e.Kind == "WithdrawalSupervisor") && e.Status != "Cancelled" && e.Id != supersededId).SumAsync(e => e.Amount, ct);
                if (withdrawn + request.Amount > related.RiderBalanceChange) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("السحب يتجاوز الحق المحجوز."));
            }
            var item = new HungerFinancialEvent { ClientPlatformId = request.PlatformId, SupervisorEmployeeId = request.SupervisorEmployeeId,
                PlatformRiderAccountId = request.AccountId, RiderProfileId = request.RiderProfileId, RelatedEventId = request.RelatedEventId,
                FromSubAccountId = from, ToSubAccountId = to, Kind = request.Kind, Amount = request.Amount,
                RiderBalanceChange = request.Kind == "TopUpAdvance" || request.Kind == "RiderPayment" ? -request.Amount : request.Kind == "Collection" ? request.Amount : 0,
                BusinessDate = request.BusinessDate, IdempotencyKey = request.IdempotencyKey, RequestHash = hash, EvidenceReference = request.EvidenceReference, DetailsJson = Serialize(request),
                ApprovalStatus = Sealable(request.Kind) ? "PendingSeal" : "NotRequired", Role = RoleOf(request.Kind), SupersedesEventId = request.SupersedesEventId };
            if (withdrawal)
            {
                item.Status = "PendingArrival"; item.SourceWalletBalance = request.SourceWalletBalance; item.DestinationChannel = channel;
                item.DestinationType = request.DestinationType; item.DestinationOwnerId = request.DestinationOwnerId;
                item.TransferReference = request.TransferReference; item.TransferDate = request.TransferDate;
            }
            db.Add(item);
            foreach (var screenshot in screenshots) screenshot.FinancialEventId = item.Id;
            // CR-005: cash collected for a top-up goes to the owner's collections and never enters company treasury or remittances.
            if (Cr005 && request.Kind == "TopUpCollected") item.DestinationChannel = "OwnerVault";
            // A replacement top-up waits for the seal: the rejected row is reversed and this one posted in one transaction.
            if (superseded is not null && !withdrawal) { item.Status = "AwaitingSeal"; return Result.Success(MoneyResult(item)); }
            if (!withdrawal) StageTransfer(c, item, from, to, request.Amount);
            if (request.Kind == "TopUpCollected" && await StageCollectedCash(c, item, ct) is { } cashError) return Result.Failure<HungerMoneyResult>(cashError);
            return Result.Success(MoneyResult(item));
        }, ct);
    }
    // Cashbox is the company's central treasury, Bank a bank account. Stc is the supervisor's own receive account, already checked by owner.
    private async Task<bool> ChannelMatches(string? channel, Guid destination, CancellationToken ct)
    {
        if (channel is null or "Stc") return true;
        var location = await db.Set<TreasurySubAccount>().AsNoTracking().Where(a => a.Id == destination).Select(a => (TreasuryLocation?)a.Location).SingleOrDefaultAsync(ct);
        return location == (channel == "Cashbox" ? TreasuryLocation.Central : TreasuryLocation.Bank);
    }
    // Legacy rows post the rider's cash into supervisor collections. Owner-vault rows only record an expected receipt.
    private async Task<OperationError?> StageCollectedCash(HungerFinanceConfiguration c, HungerFinancialEvent item, CancellationToken ct)
    {
        if (item.DestinationChannel == "OwnerVault")
        {
            db.Add(new HungerFinancialEvent { ClientPlatformId = item.ClientPlatformId, SupervisorEmployeeId = item.SupervisorEmployeeId,
                PlatformRiderAccountId = item.PlatformRiderAccountId, RiderProfileId = item.RiderProfileId, RelatedEventId = item.Id,
                Kind = "OwnerCollectionReceipt", Status = "PendingReceipt", Amount = item.Amount, BusinessDate = item.BusinessDate,
                IdempotencyKey = $"OwnerReceipt:{item.Id:D}", RequestHash = item.RequestHash, EvidenceReference = item.EvidenceReference,
                Role = "OwnerCollections", DestinationChannel = "OwnerVault" });
            return null;
        }
        if (!await PostingAccounts(c, c.RiderClearingSubAccountId, c.CollectionsSubAccountId, ct)) return HungerErrors.Invalid("ربط التحصيل غير صالح.");
        StageTransfer(c, item, c.RiderClearingSubAccountId, c.CollectionsSubAccountId, item.Amount);
        return null;
    }
    // What a supervisor can remit to the company: one rule for submission, pending lists, reports and the wallets tab.
    private static readonly System.Linq.Expressions.Expression<Func<HungerFinancialEvent, bool>> RemittableSource = e => e.Kind == "Collection"
        || e.Kind == "TopUpCollected" && e.DestinationChannel == null && e.Status == "Posted" || e.Kind == "WithdrawalSupervisor" && e.Status == "Arrived";
    private static readonly Func<HungerFinancialEvent, bool> IsRemittableSource = RemittableSource.Compile();
    private async Task<bool> ValidWithdrawalDestination(HungerFinanceConfiguration c, string kind, string? type, Guid? owner, Guid destination, CancellationToken ct)
    {
        var account = await db.Set<TreasurySubAccount>().AsNoTracking().SingleOrDefaultAsync(a => a.Id == destination, ct);
        if (account is null) return false;
        if (kind == "WithdrawalCompany") return type == "Company" && owner == c.SponsorId && account.SponsorId == c.SponsorId
            && account.Location is TreasuryLocation.Bank or TreasuryLocation.Central;
        // The approved finance configuration authorizes this employee and their collections destination.
        return type == "Supervisor" && owner == c.SupervisorEmployeeId && destination == c.CollectionsSubAccountId
            && account.Location == TreasuryLocation.Supervisor && await db.Employees.AnyAsync(e => e.Id == owner, ct);
    }
    public Task<Result<HungerMoneyResult>> ConfirmWithdrawalArrivalAsync(Guid withdrawalId, HungerWithdrawalAction request, CancellationToken ct = default)
        => CompleteWithdrawal(withdrawalId, request, false, ct);
    public Task<Result<HungerMoneyResult>> CancelWithdrawalAsync(Guid withdrawalId, HungerWithdrawalAction request, CancellationToken ct = default)
        => CompleteWithdrawal(withdrawalId, request, true, ct);
    private async Task<Result<HungerMoneyResult>> CompleteWithdrawal(Guid id, HungerWithdrawalAction request, bool cancel, CancellationToken ct)
    {
        var source = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == id && (e.Kind == "WithdrawalCompany" || e.Kind == "WithdrawalSupervisor"), ct);
        if (source is null) return Result.Failure<HungerMoneyResult>(HungerErrors.NotFound);
        if (!await FinanceAccess(source.ClientPlatformId, cancel ? PermissionKeys.Hunger.WithdrawalRequest : PermissionKeys.Hunger.WithdrawalArrivalConfirm, ct)) return Result.Failure<HungerMoneyResult>(HungerErrors.Forbidden);
        if (!ValidEvent(request.BusinessDate, request.IdempotencyKey, request.EvidenceReference) || request.BusinessDate < source.BusinessDate)
            return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("تاريخ ودليل وصول أو إلغاء السحب مطلوبان."));
        var hash = Hash(Serialize(new { id, request, cancel }));
        return await FinancialWrite([source.ClientPlatformId, source.SupervisorEmployeeId, source.PlatformRiderAccountId!.Value], async () =>
        {
            var item = await db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == id, ct);
            if (item.CompletionKey == request.IdempotencyKey && item.CompletionHash == hash) return Result.Success(MoneyResult(item));
            if (item.Status != "PendingArrival") return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("السحب ليس في انتظار الوصول."));
            if (!cancel)
            {
                var c = await FinanceConfig(item.ClientPlatformId, item.SupervisorEmployeeId, ct);
                if (c is null || !await PostingAccounts(c, item.FromSubAccountId!.Value, item.ToSubAccountId!.Value, ct)
                    || !await ValidWithdrawalDestination(c, item.Kind, item.DestinationType, item.DestinationOwnerId, item.ToSubAccountId.Value, ct))
                    return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("ربط وجهة السحب لم يعد صالحًا."));
                StageTransfer(c, item, item.FromSubAccountId.Value, item.ToSubAccountId.Value, item.Amount);
            }
            item.Status = cancel ? "Cancelled" : "Arrived"; item.ArrivalEvidence = request.EvidenceReference;
            item.CompletionDate = request.BusinessDate; item.CompletionKey = request.IdempotencyKey; item.CompletionHash = hash;
            return Result.Success(MoneyResult(item));
        }, ct);
    }
    public Task<Result<HungerMoneyResult>> SubmitRemittanceAsync(HungerRemittanceRequest request, CancellationToken ct = default)
        => CreateRemittance(request, false, ct);
    public Task<Result<HungerMoneyResult>> CreateRemittanceDraftAsync(HungerRemittanceRequest request, CancellationToken ct = default)
        => CreateRemittance(request, true, ct);
    private async Task<Result<HungerMoneyResult>> CreateRemittance(HungerRemittanceRequest request, bool draft, CancellationToken ct)
    {
        if (!await FinanceAccess(request.PlatformId, PermissionKeys.Hunger.RemittancesSubmit, ct)) return Result.Failure<HungerMoneyResult>(HungerErrors.Forbidden);
        if (!ValidEvent(request.BusinessDate, request.IdempotencyKey, request.EvidenceReference) || request.Sources is null || request.Sources.Count is 0 or > 1000
            || request.Sources.Any(s => s is null || !Money(s.Amount)) || request.Sources.Select(s => s.SourceEventId).Distinct().Count() != request.Sources.Count) return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("حدد مصادر توريد صحيحة دون تكرار."));
        var hash = draft ? Hash(Serialize(new { request, draft })) : Hash(Serialize(request));
        return await FinancialWrite([request.PlatformId, request.SupervisorEmployeeId], async () =>
        {
            var old = await db.Set<HungerFinancialEvent>().SingleOrDefaultAsync(e => e.ClientPlatformId == request.PlatformId && e.IdempotencyKey == request.IdempotencyKey, ct);
            if (old is not null) return old.RequestHash == hash ? Result.Success(MoneyResult(old)) : Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("مفتاح مكرر لطلب مختلف."));
            if (await FinanceConfig(request.PlatformId, request.SupervisorEmployeeId, ct) is null) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("الربط المالي غير معتمد."));
            var item = new HungerFinancialEvent { ClientPlatformId = request.PlatformId, SupervisorEmployeeId = request.SupervisorEmployeeId, Kind = "Remittance", Status = draft ? "Draft" : "Submitted",
                Amount = request.Sources.Sum(s => s.Amount), BusinessDate = request.BusinessDate, IdempotencyKey = request.IdempotencyKey, RequestHash = hash, EvidenceReference = request.EvidenceReference, DetailsJson = Serialize(request) };
            foreach (var line in request.Sources)
            {
                var source = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == line.SourceEventId && e.ClientPlatformId == request.PlatformId && e.SupervisorEmployeeId == request.SupervisorEmployeeId, ct);
                if (source is null || source.BusinessDate > request.BusinessDate || !IsRemittableSource(source)) return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("مصدر التوريد لا يخص المشرف أو غير قابل للتوريد."));
                var reserved = await (from l in db.Set<HungerRemittanceLine>()
                    join batch in db.Set<HungerFinancialEvent>() on l.RemittanceEventId equals batch.Id
                    where l.SourceEventId == source.Id && batch.Status != "Cancelled"
                    select l.Amount).SumAsync(ct);
                if (reserved + line.Amount > source.Amount) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("المصدر مخصص لتوريد آخر أو المبلغ يتجاوز المتبقي."));
                db.Add(new HungerRemittanceLine { RemittanceEventId = item.Id, SourceEventId = source.Id, Amount = line.Amount });
            }
            db.Add(item); return Result.Success(MoneyResult(item));
        }, ct);
    }
    public async Task<Result<HungerMoneyResult>> ConfirmRemittanceAsync(Guid remittanceId, HungerRemittanceConfirmation request, CancellationToken ct = default)
    {
        var remit = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == remittanceId && e.Kind == "Remittance", ct);
        if (remit is null || !await FinanceAccess(remit.ClientPlatformId, PermissionKeys.Hunger.RemittancesConfirm, ct)) return Result.Failure<HungerMoneyResult>(HungerErrors.Forbidden);
        if (!ValidEvent(request.BusinessDate, request.IdempotencyKey, request.EvidenceReference) || request.Sources?.Any(s => s is null || !Money(s.Amount)) == true
            || request.Sources is { Count: 0 } || request.Sources is { Count: > 1000 } || request.Sources?.Select(s => s.SourceEventId).Distinct().Count() != request.Sources?.Count
            || request.SelectedSourceIds is { Count: 0 } or { Count: > 1000 }
            || request.SelectedSourceIds?.Distinct().Count() != request.SelectedSourceIds?.Count
            || request.SelectedSourceIds != null && request.Sources != null || request.BusinessDate < remit.BusinessDate) return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("بيانات التأكيد غير صالحة."));
        // CR-005: the receiving side is stated explicitly; nothing is assumed to be the company bank.
        if (request.DestinationChannel is not (null or "Cashbox" or "Bank") || Cr005 && (request.DestinationChannel is null
            || request.DestinationType != "Company" || !request.DestinationOwnerId.HasValue))
            return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("حدد وجهة الاستلام: خزنة أو بنك الشركة ومالكها."));
        var hash = Hash(Serialize(new { remittanceId, request }));
        var legacyHash = request.DestinationType is null && request.DestinationOwnerId is null && request.SelectedSourceIds is null
            ? Hash(Serialize(new { remittanceId, request = new { request.DestinationSubAccountId, request.Sources,
                request.IdempotencyKey, request.BusinessDate, request.EvidenceReference } })) : null;
        return await FinancialWrite([remit.ClientPlatformId, remit.SupervisorEmployeeId], async () =>
        {
            var old = await db.Set<HungerFinancialEvent>().SingleOrDefaultAsync(e => e.ClientPlatformId == remit.ClientPlatformId && e.IdempotencyKey == request.IdempotencyKey, ct);
            if (old is not null) return old.RequestHash == hash || old.RequestHash == legacyHash ? Result.Success(MoneyResult(old)) : Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("مفتاح مكرر لطلب مختلف."));
            var c = await FinanceConfig(remit.ClientPlatformId, remit.SupervisorEmployeeId, ct);
            if (c is null) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("الربط المالي غير نشط."));
            var batch = await db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == remittanceId, ct);
            if (batch.Status is not ("Submitted" or "PartiallyConfirmed")) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("الدفعة ليست مقدمة أو لا يوجد متبق."));
            if (!await ValidWithdrawalDestination(c, "WithdrawalCompany", request.DestinationType ?? "Company",
                request.DestinationOwnerId ?? c.SponsorId, request.DestinationSubAccountId, ct))
                return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("وجهة الاستلام يجب أن تكون خزنة أو بنك الشركة المعتمدة."));
            if (!await ChannelMatches(request.DestinationChannel, request.DestinationSubAccountId, ct))
                return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("الحساب المختار لا يطابق وجهة الاستلام (خزنة أو بنك)."));
            var lines = await db.Set<HungerRemittanceLine>().Where(l => l.RemittanceEventId == remittanceId).ToArrayAsync(ct);
            if (request.SelectedSourceIds != null && request.SelectedSourceIds.Any(id => !lines.Any(l => l.SourceEventId == id && l.Amount > l.ConfirmedAmount)))
                return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("مصدر غير متبق ضمن الدفعة."));
            var selected = request.Sources ?? lines.Where(l => l.ConfirmedAmount < l.Amount && (request.SelectedSourceIds == null || request.SelectedSourceIds.Contains(l.SourceEventId))).Select(l => new HungerRemittanceSource(l.SourceEventId, l.Amount - l.ConfirmedAmount)).ToArray();
            if (selected.Count == 0) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("لا يوجد مبلغ متبق للتأكيد."));
            var item = new HungerFinancialEvent { ClientPlatformId = c.ClientPlatformId, SupervisorEmployeeId = c.SupervisorEmployeeId, Kind = "RemittanceConfirmation",
                ToSubAccountId = request.DestinationSubAccountId, DestinationType = "Company", DestinationOwnerId = c.SponsorId, DestinationChannel = request.DestinationChannel,
                RelatedEventId = remittanceId, Amount = selected.Sum(s => s.Amount), BusinessDate = request.BusinessDate, IdempotencyKey = request.IdempotencyKey,
                RequestHash = hash, EvidenceReference = request.EvidenceReference, DetailsJson = Serialize(request) };
            foreach (var confirm in selected)
            {
                var line = lines.SingleOrDefault(l => l.SourceEventId == confirm.SourceEventId);
                if (line is null || confirm.Amount > line.Amount - line.ConfirmedAmount) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("التأكيد يتجاوز المتبقي أو المصدر لا يخص الدفعة."));
                var source = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleAsync(e => e.Id == confirm.SourceEventId, ct);
                var from = source.Kind == "WithdrawalSupervisor" ? source.ToSubAccountId!.Value : c.CollectionsSubAccountId;
                if (!await PostingAccounts(c, from, request.DestinationSubAccountId, ct) || request.DestinationSubAccountId == c.CustodySubAccountId)
                    return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("وجهة التوريد غير صالحة؛ لا تمويل تلقائي للعهدة."));
                // One confirmation may aggregate several sources against the same posting accounts.
                line.ConfirmedAmount += confirm.Amount;
                db.Add(new HungerReceiptAllocation { ConfirmationEventId = item.Id, RemittanceLineId = line.Id, Amount = confirm.Amount });
            }
            var transfers = new Dictionary<Guid, decimal>();
            foreach (var confirm in selected)
            {
                var source = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleAsync(e => e.Id == confirm.SourceEventId, ct);
                var from = source.Kind == "WithdrawalSupervisor" ? source.ToSubAccountId!.Value : c.CollectionsSubAccountId;
                transfers[from] = transfers.GetValueOrDefault(from) + confirm.Amount;
            }
            foreach (var (from, amount) in transfers)
                db.Add(new TreasuryPosting { ClientPlatformId = c.ClientPlatformId, SupervisorEmployeeId = c.SupervisorEmployeeId, SourceEventId = item.Id,
                    SubAccountId = from, SponsorId = c.SponsorId, BatchId = item.Id, Amount = -amount, Kind = TreasuryEntryKind.Business,
                    OccurredAtUtc = clock.GetUtcNow(), ActorUserId = user.UserId!.Value, ActorNameAr = "Hunger accountant receipt", Reference = $"Hunger:{item.Id:D}", Reason = item.Kind });
            db.Add(new TreasuryPosting { ClientPlatformId = c.ClientPlatformId, SupervisorEmployeeId = c.SupervisorEmployeeId, SourceEventId = item.Id,
                SubAccountId = request.DestinationSubAccountId, SponsorId = c.SponsorId, BatchId = item.Id, Amount = item.Amount, Kind = TreasuryEntryKind.Business,
                OccurredAtUtc = clock.GetUtcNow(), ActorUserId = user.UserId!.Value, ActorNameAr = "Hunger accountant receipt", Reference = $"Hunger:{item.Id:D}", Reason = item.Kind });
            batch.Status = lines.All(l => l.ConfirmedAmount == l.Amount) ? "Confirmed" : "PartiallyConfirmed";
            db.Add(item); return Result.Success(MoneyResult(item));
        }, ct);
    }
    private async Task<HungerCustodySettlement> CustodySettlement(HungerFinanceConfiguration c, CancellationToken ct)
    {
        var events = db.Set<HungerFinancialEvent>().AsNoTracking().Where(e => e.ClientPlatformId == c.ClientPlatformId && e.SupervisorEmployeeId == c.SupervisorEmployeeId);
        var funding = await events.Where(e => e.Kind == "Funding").SumAsync(e => e.Amount, ct);
        // A replacement awaiting its seal and a cancelled row carry no custody postings.
        var advance = await events.Where(e => e.Kind == "TopUpAdvance" && e.Status == "Posted").SumAsync(e => e.Amount, ct);
        var collected = await events.Where(e => e.Kind == "TopUpCollected" && e.Status == "Posted").SumAsync(e => e.Amount, ct);
        var cancelled = await events.Where(e => (e.Kind == "TopUpAdvance" || e.Kind == "TopUpCollected") && e.Status == "Cancelled").SumAsync(e => e.Amount, ct);
        var posting = await CustodyBalance(c, ct); var expected = funding - advance - collected; var difference = posting - expected;
        var approved = difference != 0 && await db.Set<HungerExceptionApproval>().AnyAsync(e => e.ClientPlatformId == c.ClientPlatformId
            && e.SupervisorEmployeeId == c.SupervisorEmployeeId && e.Kind == "CustodyIdentityMismatch" && e.Status == "Approved" && e.Amount == difference, ct);
        return new(funding, advance, collected, cancelled, expected, posting, difference, difference == 0, approved);
    }
    public async Task<Result<HungerCustodySettlement>> CustodySettlementAsync(Guid platformId, Guid supervisorEmployeeId, CancellationToken ct = default)
    {
        if (!await FinanceAccess(platformId, PermissionKeys.Hunger.FinanceRead, ct)) return Result.Failure<HungerCustodySettlement>(HungerErrors.Forbidden);
        var c = await FinanceConfig(platformId, supervisorEmployeeId, ct);
        return c is null ? Result.Failure<HungerCustodySettlement>(HungerErrors.NotFound) : Result.Success(await CustodySettlement(c, ct));
    }
    public async Task<Result<HungerMoneyResult>> ConfirmOwnerCollectionAsync(Guid receiptId, HungerOwnerCollectionConfirmation request, CancellationToken ct = default)
    {
        var source = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == receiptId && e.Kind == "OwnerCollectionReceipt", ct);
        if (source is null) return Result.Failure<HungerMoneyResult>(HungerErrors.NotFound);
        if (!await FinanceAccess(source.ClientPlatformId, PermissionKeys.Hunger.OwnerCollectionsConfirm, ct)) return Result.Failure<HungerMoneyResult>(HungerErrors.Forbidden);
        if (request.Capacity is not ("Accountant" or "Owner") || !ValidEvent(request.BusinessDate, request.IdempotencyKey, request.EvidenceReference)
            || request.BusinessDate < source.BusinessDate) return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("حدد صفة المستلم وتاريخ ودليل الاستلام."));
        var hash = Hash(Serialize(new { receiptId, request }));
        return await FinancialWrite([source.ClientPlatformId, source.SupervisorEmployeeId, source.PlatformRiderAccountId ?? source.ClientPlatformId], async () =>
        {
            var old = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.ClientPlatformId == source.ClientPlatformId && e.IdempotencyKey == request.IdempotencyKey, ct);
            var item = await db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == receiptId, ct);
            if (old is not null) return old.RequestHash == hash ? Result.Success(MoneyResult(item)) : Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("مفتاح مستخدم لطلب مختلف."));
            if (item.Status != "PendingReceipt") return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("الاستلام مؤكد أو ملغى مسبقًا."));
            // Who confirmed, and in which capacity, lives on the audit event; the receipt itself only changes state.
            item.Status = "Received";
            db.Add(new HungerFinancialEvent { ClientPlatformId = item.ClientPlatformId, SupervisorEmployeeId = item.SupervisorEmployeeId,
                PlatformRiderAccountId = item.PlatformRiderAccountId, RelatedEventId = item.Id, Kind = "OwnerCollectionConfirmation", Status = "Received",
                Amount = item.Amount, BusinessDate = request.BusinessDate, IdempotencyKey = request.IdempotencyKey, RequestHash = hash,
                EvidenceReference = request.EvidenceReference, Role = "OwnerCollections", DestinationChannel = "OwnerVault",
                DetailsJson = Serialize(new { request.Capacity, ConfirmedByUserId = user.UserId }) });
            return Result.Success(MoneyResult(item));
        }, ct);
    }
    public async Task<Result<HungerFinanceReport>> FinanceReportAsync(Guid platformId, Guid supervisorEmployeeId, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await FinanceAccess(platformId, PermissionKeys.Hunger.FinanceRead, ct)) return Result.Failure<HungerFinanceReport>(HungerErrors.Forbidden);
        if (page < 1 || pageSize is < 1 or > 100) return Result.Failure<HungerFinanceReport>(HungerErrors.Invalid("صفحة غير صالحة."));
        var query = db.Set<HungerFinancialEvent>().AsNoTracking().Where(e => e.ClientPlatformId == platformId && e.SupervisorEmployeeId == supervisorEmployeeId);
        var count = await query.CountAsync(ct);
        var events = await query.OrderByDescending(e => e.BusinessDate).ThenByDescending(e => e.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new HungerFinancialEventItem(e.Id, e.Kind, e.PlatformRiderAccountId, e.RiderProfileId, e.SupervisorEmployeeId, e.Amount, e.RiderBalanceChange, e.BusinessDate, e.EvidenceReference, e.Status,
                e.ApprovalStatus, e.ApprovedAtUtc, e.SupersedesEventId, e.RejectionReason)).ToArrayAsync(ct);
        var c = await FinanceConfig(platformId, supervisorEmployeeId, ct);
        var availableSources = await query.Where(RemittableSource).SumAsync(e => e.Amount, ct);
        var confirmed = await query.Where(e => e.Kind == "RemittanceConfirmation").SumAsync(e => e.Amount, ct);
        var reversed = await query.Where(e => e.Kind == "RemittanceReversal").SumAsync(e => e.Amount, ct);
        var pending = availableSources - confirmed + reversed;
        return Result.Success(new HungerFinanceReport(events, count, c is null ? 0 : await CustodyBalance(c, ct), pending, page, pageSize));
    }
}
