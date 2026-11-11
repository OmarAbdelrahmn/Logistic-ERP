using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hunger;

// CR-005 foundations: accountant seal, server-issued evidence files and exception approvals.
internal sealed partial class HungerQualityService
{
    // Funding has its own approve-then-deliver chain, so it is not sealed after the fact.
    private static bool Sealable(string kind) => kind is "TopUpAdvance" or "TopUpCollected" or "Collection"
        or "WithdrawalCompany" or "WithdrawalSupervisor" or "RiderPayment" or "WalletDelivery" or "WalletReturn"
        or "CloseAdjustmentAdvance" or "CloseAdjustmentWash";
    private static string? RoleOf(string kind) => kind switch
    {
        "Funding" or "TopUpAdvance" or "TopUpCollected" => "Custody",
        "Collection" => "Collections",
        "WithdrawalCompany" or "WithdrawalSupervisor" or "WalletDelivery" or "WalletReturn" => "Wallet",
        "RiderPayment" or "CloseAdjustmentAdvance" or "CloseAdjustmentWash" => "RiderClearing",
        _ => null
    };
    private static string? SupersedeFamily(string kind) => kind switch
    {
        "TopUpAdvance" or "TopUpCollected" => "TopUp",
        "WithdrawalCompany" or "WithdrawalSupervisor" => "Withdrawal",
        _ => null
    };
    private static HungerFinancialEventItem EventItem(HungerFinancialEvent e) => new(e.Id, e.Kind, e.PlatformRiderAccountId, e.RiderProfileId,
        e.SupervisorEmployeeId, e.Amount, e.RiderBalanceChange, e.BusinessDate, e.EvidenceReference, e.Status,
        e.ApprovalStatus, e.ApprovedAtUtc, e.SupersedesEventId, e.RejectionReason);
    private HungerFinancialEvent ApprovalAudit(HungerFinancialEvent item, string kind, string key, string hash, string? note, object details) => new()
    {
        ClientPlatformId = item.ClientPlatformId, SupervisorEmployeeId = item.SupervisorEmployeeId, PlatformRiderAccountId = item.PlatformRiderAccountId,
        RelatedEventId = item.Id, Kind = kind, BusinessDate = BusinessToday, IdempotencyKey = key, RequestHash = hash,
        EvidenceReference = note ?? string.Empty, DetailsJson = Serialize(details)
    };

    public async Task<Result<HungerMoneyResult>> SealEventAsync(Guid eventId, HungerSealRequest request, CancellationToken ct = default)
    {
        var source = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == eventId, ct);
        if (source is null) return Result.Failure<HungerMoneyResult>(HungerErrors.NotFound);
        if (!await FinanceAccess(source.ClientPlatformId, PermissionKeys.Hunger.Seal, ct)) return Result.Failure<HungerMoneyResult>(HungerErrors.Forbidden);
        if (request.Note is { Length: > 1000 } || request.Accounts is { Count: > 20 }
            || request.Accounts?.Any(a => string.IsNullOrWhiteSpace(a.Key) || a.Key.Length > 64 || string.IsNullOrWhiteSpace(a.Value) || a.Value.Length > 64) == true)
            return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("بيانات الاعتماد غير صالحة."));
        return await FinancialWrite([source.ClientPlatformId, source.SupervisorEmployeeId, source.PlatformRiderAccountId ?? source.ClientPlatformId], async () =>
        {
            var item = await db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == eventId, ct);
            if (item.ApprovalStatus == "Sealed") return Result.Success(MoneyResult(item));
            if (item.ApprovalStatus != "PendingSeal") return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("العملية ليست بانتظار اعتماد المحاسب."));
            if (item.CreatedByUserId is { } maker && maker == user.UserId)
                return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("لا يعتمد المحاسب عملية رحّلها بنفسه."));
            var audit = ApprovalAudit(item, "Seal", $"Seal:{eventId:D}", Hash(Serialize(new { eventId, request })), request.Note, request);
            if (item.SupersedesEventId is { } oldId && await ApplySupersession(item, oldId, audit, ct) is { } error)
                return Result.Failure<HungerMoneyResult>(error);
            item.ApprovalStatus = "Sealed"; item.ApprovedAtUtc = clock.GetUtcNow(); item.ApprovedByUserId = user.UserId;
            item.SealAccountsJson = request.Accounts is { Count: > 0 } ? Serialize(request.Accounts) : null;
            db.Add(audit); return Result.Success(MoneyResult(item));
        }, ct);
    }

    // Reverses the rejected row and gives the replacement its money effect, inside the caller's unit of work.
    private async Task<OperationError?> ApplySupersession(HungerFinancialEvent item, Guid oldId, HungerFinancialEvent audit, CancellationToken ct)
    {
        var old = await db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == oldId, ct);
        if (old.ApprovalStatus != "Rejected") return HungerErrors.Conflict("العملية المستبدلة لم تعد مرفوضة.");
        var postings = await db.Set<TreasuryPosting>().IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.ClientPlatformId == old.ClientPlatformId && p.SourceEventId == oldId).ToArrayAsync(ct);
        if (postings.Sum(p => p.Amount) != 0) return HungerErrors.Conflict("قيود العملية المرفوضة غير متوازنة.");
        var postingIds = postings.Select(p => p.Id).ToArray();
        if (await db.Set<TreasuryPosting>().IgnoreQueryFilters().AnyAsync(p => p.ReversesPostingId != null && postingIds.Contains(p.ReversesPostingId.Value), ct))
            return HungerErrors.Conflict("قيود العملية المرفوضة معكوسة مسبقًا.");
        // The reversal belongs to the seal audit event; the replacement keeps its own id for its own postings.
        foreach (var posting in postings)
            db.Add(new TreasuryPosting { ClientPlatformId = posting.ClientPlatformId, SupervisorEmployeeId = posting.SupervisorEmployeeId,
                SourceEventId = audit.Id, ReversesPostingId = posting.Id, SubAccountId = posting.SubAccountId, SponsorId = posting.SponsorId, BatchId = audit.Id,
                Amount = -posting.Amount, Kind = TreasuryEntryKind.Reversal, OccurredAtUtc = clock.GetUtcNow(), ActorUserId = user.UserId!.Value,
                ActorNameAr = "Hunger accountant seal", Reference = $"Hunger:{oldId:D}", Reason = "Superseded" });
        old.ApprovalStatus = "Cancelled"; old.Status = "Cancelled";
        var receipt = await db.Set<HungerFinancialEvent>().SingleOrDefaultAsync(e => e.RelatedEventId == oldId && e.Kind == "OwnerCollectionReceipt", ct);
        // Cash the owner already confirmed cannot be unwound by a replacement; that needs an explicit correction.
        if (receipt is { Status: "Received" }) return HungerErrors.Conflict("تحصيل المالك للعملية المرفوضة مؤكد الاستلام؛ يلزم مسار تصحيح.");
        if (receipt is not null) receipt.Status = "Cancelled";
        if (SupersedeFamily(item.Kind) != "TopUp") return null;
        var c = await FinanceConfig(item.ClientPlatformId, item.SupervisorEmployeeId, ct);
        if (c is null || !item.FromSubAccountId.HasValue || !item.ToSubAccountId.HasValue
            || !await PostingAccounts(c, item.FromSubAccountId.Value, item.ToSubAccountId.Value, ct)) return HungerErrors.Conflict("الربط المالي لم يعد صالحًا للشحن البديل.");
        // The reversal above is staged, not saved, so add back what it returns to custody.
        var returned = -postings.Where(p => p.SubAccountId == c.CustodySubAccountId).Sum(p => p.Amount);
        if (await CustodyBalance(c, ct) + returned < item.Amount) return HungerErrors.Conflict("عهدة المشرف لا تكفي للشحن البديل.");
        StageTransfer(c, item, item.FromSubAccountId.Value, item.ToSubAccountId.Value, item.Amount);
        if (item.Kind == "TopUpCollected" && await StageCollectedCash(c, item, ct) is { } cashError) return cashError;
        item.Status = "Posted";
        return null;
    }

    public async Task<Result<HungerMoneyResult>> RejectEventAsync(Guid eventId, HungerSealRejection request, CancellationToken ct = default)
    {
        var source = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == eventId, ct);
        if (source is null) return Result.Failure<HungerMoneyResult>(HungerErrors.NotFound);
        if (!await FinanceAccess(source.ClientPlatformId, PermissionKeys.Hunger.Seal, ct)) return Result.Failure<HungerMoneyResult>(HungerErrors.Forbidden);
        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > 1000) return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("سبب الرفض مطلوب."));
        return await FinancialWrite([source.ClientPlatformId, source.SupervisorEmployeeId, source.PlatformRiderAccountId ?? source.ClientPlatformId], async () =>
        {
            var item = await db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == eventId, ct);
            if (item.ApprovalStatus == "Rejected" && item.RejectionReason == reason) return Result.Success(MoneyResult(item));
            if (item.ApprovalStatus != "PendingSeal") return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("العملية ليست بانتظار اعتماد المحاسب."));
            // The money effect stays until a replacement is sealed; rejection alone never moves custody or wallet.
            item.ApprovalStatus = "Rejected"; item.RejectionReason = reason;
            db.Add(ApprovalAudit(item, "SealRejection", $"SealReject:{eventId:D}", Hash(Serialize(new { eventId, reason })), reason, new { reason }));
            return Result.Success(MoneyResult(item));
        }, ct);
    }

    public async Task<Result<HungerSealPage>> SealQueueAsync(Guid platformId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await FinanceAccess(platformId, PermissionKeys.Hunger.Seal, ct) && !await FinanceAccess(platformId, PermissionKeys.Hunger.FinanceRead, ct))
            return Result.Failure<HungerSealPage>(HungerErrors.Forbidden);
        status ??= "PendingSeal";
        if (status is not ("PendingSeal" or "Rejected") || page < 1 || pageSize is < 1 or > 100) return Result.Failure<HungerSealPage>(HungerErrors.Invalid("حالة أو صفحة غير صالحة."));
        var query = db.Set<HungerFinancialEvent>().AsNoTracking().Where(e => e.ClientPlatformId == platformId && e.ApprovalStatus == status);
        var rows = await query.OrderBy(e => e.BusinessDate).ThenBy(e => e.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success(new HungerSealPage(rows.Select(EventItem).ToArray(), await query.CountAsync(ct), page, pageSize));
    }

    public async Task<Result<HungerEvidenceItem>> UploadEvidenceAsync(HungerEvidenceUpload request, PrivateFileUpload? image, CancellationToken ct = default)
    {
        var allowed = false;
        foreach (var permission in new[] { PermissionKeys.Hunger.WalletReview, PermissionKeys.Hunger.TopUpRequest, PermissionKeys.Hunger.TopUpExecute, PermissionKeys.Hunger.WithdrawalRequest })
            if (await FinanceAccess(request.PlatformId, permission, ct)) { allowed = true; break; }
        if (!allowed) return Result.Failure<HungerEvidenceItem>(HungerErrors.Forbidden);
        if (request.Purpose is not ("WalletBefore" or "WalletAfter" or "Wallet" or "Arrival") || request.ObservedOn == default || request.ObservedOn > BusinessToday
            || !await db.PlatformRiderAccounts.AnyAsync(a => a.Id == request.AccountId && a.ClientPlatformId == request.PlatformId, ct))
            return Result.Failure<HungerEvidenceItem>(HungerErrors.Invalid("حدد حساب هنقر وغرض اللقطة وتاريخها."));
        if (files is null || image is null || image.ContentType.ToLowerInvariant() is not ("image/jpeg" or "image/png" or "image/webp" or "image/gif" or "image/bmp"))
            return Result.Failure<HungerEvidenceItem>(PrivateFileErrors.InvalidFile);
        var stored = await files.StoreAsync($"hunger/evidence/{request.AccountId:D}", image, 5 * 1024 * 1024, ct);
        if (stored.IsFailure) return Result.Failure<HungerEvidenceItem>(stored.Error);
        var keepFile = false;
        try
        {
            var item = new HungerEvidenceFile { ClientPlatformId = request.PlatformId, PlatformRiderAccountId = request.AccountId, UploadedByUserId = user.UserId!.Value,
                StoragePath = stored.Value!.StoragePath, ContentType = stored.Value.ContentType, Purpose = request.Purpose, ObservedOn = request.ObservedOn };
            db.Add(item); await db.SaveChangesAsync(ct); keepFile = true;
            return Result.Success(new HungerEvidenceItem(item.Id, item.Purpose, item.ObservedOn, "/" + item.StoragePath["wwwroot/".Length..]));
        }
        finally
        {
            if (!keepFile) files.DeleteBestEffort(stored.Value!.StoragePath);
        }
    }

    private static HungerExceptionItem ExceptionItem(HungerExceptionApproval e) => new(e.Id, e.Kind, e.Status, e.PlatformRiderAccountId, e.SupervisorEmployeeId,
        e.Amount, e.Reason, e.RequestedByUserId, e.DecidedByUserId, e.DecidedAtUtc, e.ConsumedByEventId);

    public async Task<Result<HungerExceptionItem>> RequestExceptionAsync(HungerExceptionRequest request, CancellationToken ct = default)
    {
        if (!await FinanceAccess(request.PlatformId, PermissionKeys.Hunger.ExceptionRequest, ct)) return Result.Failure<HungerExceptionItem>(HungerErrors.Forbidden);
        if (request.Kind is not ("NonZeroHandover" or "CustodyIdentityMismatch") || request.Amount == 0 || !ValidBalance(request.Amount)
            || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000 || request.EvidenceReference is { Length: > 1000 }
            || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 150
            || request.Kind == "NonZeroHandover" && !request.AccountId.HasValue
            || request.Kind == "CustodyIdentityMismatch" && !request.SupervisorEmployeeId.HasValue)
            return Result.Failure<HungerExceptionItem>(HungerErrors.Invalid("حدد نوع الاستثناء ومبلغه وسببه والحساب أو المشرف المعني."));
        var hash = Hash(Serialize(request));
        return await FinancialWrite([request.PlatformId, request.AccountId ?? request.SupervisorEmployeeId ?? request.PlatformId], async () =>
        {
            var old = await db.Set<HungerExceptionApproval>().AsNoTracking().SingleOrDefaultAsync(e => e.ClientPlatformId == request.PlatformId && e.IdempotencyKey == request.IdempotencyKey, ct);
            if (old is not null) return old.RequestHash == hash ? Result.Success(ExceptionItem(old)) : Result.Failure<HungerExceptionItem>(HungerErrors.Conflict("مفتاح الطلب مستخدم لبيانات مختلفة."));
            if (request.AccountId.HasValue && !await db.PlatformRiderAccounts.AnyAsync(a => a.Id == request.AccountId && a.ClientPlatformId == request.PlatformId, ct))
                return Result.Failure<HungerExceptionItem>(HungerErrors.Invalid("حساب هنقر غير معروف."));
            if (request.SupervisorEmployeeId.HasValue && !await db.Employees.AnyAsync(e => e.Id == request.SupervisorEmployeeId, ct))
                return Result.Failure<HungerExceptionItem>(HungerErrors.Invalid("المشرف غير معروف."));
            var item = new HungerExceptionApproval { ClientPlatformId = request.PlatformId, PlatformRiderAccountId = request.AccountId,
                SupervisorEmployeeId = request.SupervisorEmployeeId, Kind = request.Kind, Amount = request.Amount, Reason = request.Reason.Trim(),
                EvidenceReference = request.EvidenceReference, RequestedByUserId = user.UserId!.Value, IdempotencyKey = request.IdempotencyKey, RequestHash = hash };
            db.Add(item); return Result.Success(ExceptionItem(item));
        }, ct);
    }

    public async Task<Result<HungerExceptionItem>> DecideExceptionAsync(Guid exceptionId, bool approve, HungerExceptionDecision request, CancellationToken ct = default)
    {
        var source = await db.Set<HungerExceptionApproval>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == exceptionId, ct);
        if (source is null) return Result.Failure<HungerExceptionItem>(HungerErrors.NotFound);
        if (!await FinanceAccess(source.ClientPlatformId, PermissionKeys.Hunger.ExceptionApprove, ct)) return Result.Failure<HungerExceptionItem>(HungerErrors.Forbidden);
        if (request.Note is { Length: > 1000 } || !approve && string.IsNullOrWhiteSpace(request.Note))
            return Result.Failure<HungerExceptionItem>(HungerErrors.Invalid("سبب الرفض مطلوب."));
        var target = approve ? "Approved" : "Rejected";
        return await FinancialWrite([source.ClientPlatformId, source.PlatformRiderAccountId ?? source.SupervisorEmployeeId ?? source.ClientPlatformId], async () =>
        {
            var item = await db.Set<HungerExceptionApproval>().SingleAsync(e => e.Id == exceptionId, ct);
            if (item.Status == target) return Result.Success(ExceptionItem(item));
            if (item.Status != "Requested") return Result.Failure<HungerExceptionItem>(HungerErrors.Conflict("طلب الاستثناء حُسم مسبقًا."));
            if (item.RequestedByUserId == user.UserId) return Result.Failure<HungerExceptionItem>(HungerErrors.Conflict("لا يحسم طالب الاستثناء طلبه بنفسه."));
            item.Status = target; item.DecidedByUserId = user.UserId; item.DecidedAtUtc = clock.GetUtcNow(); item.DecisionNote = request.Note?.Trim();
            return Result.Success(ExceptionItem(item));
        }, ct);
    }

    public async Task<Result<HungerExceptionPage>> ExceptionsAsync(Guid platformId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await FinanceAccess(platformId, PermissionKeys.Hunger.FinanceRead, ct) && !await FinanceAccess(platformId, PermissionKeys.Hunger.ExceptionApprove, ct))
            return Result.Failure<HungerExceptionPage>(HungerErrors.Forbidden);
        if (status is not (null or "Requested" or "Approved" or "Rejected" or "Consumed") || page < 1 || pageSize is < 1 or > 100)
            return Result.Failure<HungerExceptionPage>(HungerErrors.Invalid("حالة أو صفحة غير صالحة."));
        var query = db.Set<HungerExceptionApproval>().AsNoTracking().Where(e => e.ClientPlatformId == platformId && (status == null || e.Status == status));
        var rows = await query.OrderByDescending(e => e.CreatedAtUtc).ThenByDescending(e => e.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success(new HungerExceptionPage(rows.Select(ExceptionItem).ToArray(), await query.CountAsync(ct), page, pageSize));
    }
}
