using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService
{
    private static string MoneyPermission(string kind) => kind switch
    {
        "Funding" => PermissionKeys.Hunger.FundingExecute,
        "TopUpAdvance" or "TopUpCollected" => PermissionKeys.Hunger.TopUpExecute,
        "Collection" => PermissionKeys.Hunger.Collection,
        "WithdrawalCompany" or "WithdrawalSupervisor" => PermissionKeys.Hunger.WithdrawalRequest,
        "RiderPayment" => PermissionKeys.Hunger.RiderPayment,
        _ => PermissionKeys.Hunger.FinanceManage
    };

    public async Task<Result<HungerMoneyResult>> RequestMoneyAsync(HungerMoneyRequest request, CancellationToken ct = default)
    {
        if (request.Kind is not ("Funding" or "TopUpAdvance" or "TopUpCollected") || !Money(request.Amount)
            || !ValidEvent(request.BusinessDate, request.IdempotencyKey, request.EvidenceReference))
            return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("حدد طلب تمويل أو شحن صحيحًا."));
        var permission = request.Kind == "Funding" ? PermissionKeys.Hunger.FundingRequest : PermissionKeys.Hunger.TopUpRequest;
        if (!await FinanceAccess(request.PlatformId, permission, ct)) return Result.Failure<HungerMoneyResult>(HungerErrors.Forbidden);
        var hash = Hash(Serialize(new { request, action = "Request" }));
        return await FinancialWrite([request.PlatformId, request.SupervisorEmployeeId], async () =>
        {
            var old = await db.Set<HungerFinancialEvent>().SingleOrDefaultAsync(e => e.ClientPlatformId == request.PlatformId && e.IdempotencyKey == request.IdempotencyKey, ct);
            if (old != null) return old.RequestHash == hash ? Result.Success(MoneyResult(old)) : Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("مفتاح مستخدم لطلب مختلف."));
            var config = await FinanceConfig(request.PlatformId, request.SupervisorEmployeeId, ct);
            if (config == null)
                return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("الربط المالي غير معتمد."));
            if (request.Kind != "Funding" && (!request.AccountId.HasValue || !await db.PlatformRiderAccounts.AnyAsync(a => a.Id == request.AccountId && a.ClientPlatformId == request.PlatformId, ct)))
                return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("حساب هنقر مطلوب."));
            var item = new HungerFinancialEvent { ClientPlatformId = request.PlatformId, SupervisorEmployeeId = request.SupervisorEmployeeId,
                PlatformRiderAccountId = request.AccountId, RiderProfileId = request.RiderProfileId, Kind = "MoneyRequest", Status = "Requested", Amount = request.Amount,
                BusinessDate = request.BusinessDate, IdempotencyKey = request.IdempotencyKey, RequestHash = hash, EvidenceReference = request.EvidenceReference, DetailsJson = Serialize(request) };
            db.Add(item);
            // CR-005: a top-up the custody cannot cover stays a draft and raises a custody proposal for the shortfall.
            if (Cr005 && request.Kind != "Funding" && request.Amount - await CustodyBalance(config, ct) is > 0 and var shortfall)
                db.Add(new HungerFinancialEvent { ClientPlatformId = request.PlatformId, SupervisorEmployeeId = request.SupervisorEmployeeId,
                    PlatformRiderAccountId = request.AccountId, RiderProfileId = request.RiderProfileId, RelatedEventId = item.Id, Kind = "CustodyProposal",
                    Status = "Open", Amount = shortfall, BusinessDate = request.BusinessDate, IdempotencyKey = $"CustodyProposal:{item.Id:D}",
                    RequestHash = hash, EvidenceReference = request.EvidenceReference, Role = "Custody" });
            return Result.Success(MoneyResult(item));
        }, ct);
    }

    public Task<Result<HungerMoneyResult>> ExecuteMoneyRequestAsync(Guid requestId, CancellationToken ct = default)
        => ExecuteMoneyRequestAsync(requestId, null, ct);
    public async Task<Result<HungerMoneyResult>> ExecuteMoneyRequestAsync(Guid requestId, HungerMoneyExecution? execution, CancellationToken ct = default)
    {
        var item = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == requestId && e.Kind == "MoneyRequest", ct);
        if (item == null) return Result.Failure<HungerMoneyResult>(HungerErrors.NotFound);
        var original = Read<HungerMoneyRequest>(item.DetailsJson);
        if (Cr005 && original.Kind == "Funding")
        {
            // The teller delivers what the accountant approved, from the account the accountant chose.
            var approval = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.RelatedEventId == requestId && e.Kind == "FundingApproval" && e.Status == "Approved", ct);
            if (approval is null) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("طلب التمويل لم يعتمده المحاسب."));
            original = original with { Amount = approval.Amount, CounterSubAccountId = approval.FromSubAccountId };
        }
        // Execution has its own deterministic idempotency key; all business validation runs again under the write lock.
        // Screenshots exist only once the top-up was performed, so they arrive with the execution.
        return await PostMoneyCore(original with { IdempotencyKey = $"execute:{requestId:D}", RelatedEventId = requestId,
            EvidenceBeforeId = execution?.EvidenceBeforeId ?? original.EvidenceBeforeId, EvidenceAfterId = execution?.EvidenceAfterId ?? original.EvidenceAfterId }, requestId, ct);
    }

    public async Task<Result<HungerMoneyResult>> DecideFundingAsync(Guid requestId, bool approve, HungerFundingDecision request, CancellationToken ct = default)
    {
        var source = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == requestId && e.Kind == "MoneyRequest", ct);
        if (source is null) return Result.Failure<HungerMoneyResult>(HungerErrors.NotFound);
        if (!await FinanceAccess(source.ClientPlatformId, PermissionKeys.Hunger.FundingApprove, ct)) return Result.Failure<HungerMoneyResult>(HungerErrors.Forbidden);
        var amount = request.Amount ?? source.Amount;
        if (Read<HungerMoneyRequest>(source.DetailsJson).Kind != "Funding" || request.Note is { Length: > 1000 }
            || (approve ? !Money(amount) || !request.SourceSubAccountId.HasValue : string.IsNullOrWhiteSpace(request.Note)))
            return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("الاعتماد يحتاج مبلغًا وحساب مصدر، والرفض يحتاج سببًا، لطلب تمويل عهدة."));
        var hash = Hash(Serialize(new { requestId, approve, request }));
        return await FinancialWrite([source.ClientPlatformId, source.SupervisorEmployeeId], async () =>
        {
            var item = await db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == requestId, ct);
            var old = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.ClientPlatformId == item.ClientPlatformId && e.IdempotencyKey == $"FundingDecision:{requestId:D}", ct);
            if (old is not null) return old.RequestHash == hash ? Result.Success(MoneyResult(item) with { Amount = old.Amount })
                : Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("طلب التمويل حُسم مسبقًا بقرار مختلف."));
            if (item.Status != "Requested") return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("طلب التمويل ليس بانتظار الاعتماد."));
            if (item.CreatedByUserId is { } maker && maker == user.UserId)
                return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("لا يعتمد طالب التمويل طلبه بنفسه."));
            var c = await FinanceConfig(item.ClientPlatformId, item.SupervisorEmployeeId, ct);
            if (c is null) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("الربط المالي غير معتمد."));
            // Any active company cashbox or bank account of the sponsor may fund custody; none is hard-wired.
            if (approve && (request.SourceSubAccountId == c.CustodySubAccountId || !await db.Set<TreasurySubAccount>().AnyAsync(a => a.Id == request.SourceSubAccountId
                && a.IsActive && a.IsPosting && (a.SponsorId == null || a.SponsorId == c.SponsorId) && a.AllowedDirections != TreasuryDirections.InOnly
                && (a.Location == TreasuryLocation.Central || a.Location == TreasuryLocation.Bank), ct)))
                return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("حساب المصدر يجب أن يكون خزنة أو بنكًا نشطًا للشركة."));
            item.Status = approve ? "Approved" : "Rejected";
            db.Add(new HungerFinancialEvent { ClientPlatformId = item.ClientPlatformId, SupervisorEmployeeId = item.SupervisorEmployeeId, RelatedEventId = requestId,
                Kind = "FundingApproval", Status = item.Status, Amount = approve ? amount : 0, FromSubAccountId = approve ? request.SourceSubAccountId : null,
                ToSubAccountId = approve ? c.CustodySubAccountId : null, BusinessDate = BusinessToday, IdempotencyKey = $"FundingDecision:{requestId:D}",
                RequestHash = hash, EvidenceReference = request.Note ?? string.Empty, Role = "Custody", DetailsJson = Serialize(new { approve, request, RequestedAmount = item.Amount }) });
            return Result.Success(MoneyResult(item) with { Amount = approve ? amount : item.Amount });
        }, ct);
    }

    public Task<Result<HungerMoneyResult>> SubmitRemittanceDraftAsync(Guid remittanceId, HungerRemittanceAction request, CancellationToken ct = default)
        => ChangeRemittance(remittanceId, request, false, ct);
    public Task<Result<HungerMoneyResult>> CancelRemittanceAsync(Guid remittanceId, HungerRemittanceAction request, CancellationToken ct = default)
        => ChangeRemittance(remittanceId, request, true, ct);

    private async Task<Result<HungerMoneyResult>> ChangeRemittance(Guid id, HungerRemittanceAction request, bool cancel, CancellationToken ct)
    {
        var batch = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == id && e.Kind == "Remittance", ct);
        if (batch == null) return Result.Failure<HungerMoneyResult>(HungerErrors.NotFound);
        if (!await FinanceAccess(batch.ClientPlatformId, cancel ? PermissionKeys.Hunger.ReceiptsCancelReverse : PermissionKeys.Hunger.RemittancesSubmit, ct))
            return Result.Failure<HungerMoneyResult>(HungerErrors.Forbidden);
        if (!ValidEvent(request.BusinessDate, request.IdempotencyKey, request.EvidenceReference) || request.BusinessDate < batch.BusinessDate
            || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
            return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("يلزم تاريخ ودليل وسبب الإجراء."));
        var hash = Hash(Serialize(new { id, request, cancel }));
        return await FinancialWrite([batch.ClientPlatformId, batch.SupervisorEmployeeId], async () =>
        {
            var old = await db.Set<HungerFinancialEvent>().SingleOrDefaultAsync(e => e.ClientPlatformId == batch.ClientPlatformId && e.IdempotencyKey == request.IdempotencyKey, ct);
            if (old != null) return old.RequestHash == hash ? Result.Success(MoneyResult(old) with { EventId = id }) : Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("مفتاح مستخدم لطلب مختلف."));
            var current = await db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == id, ct);
            if (cancel ? current.Status is not ("Draft" or "Submitted") : current.Status != "Draft")
                return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("انتقال حالة غير مسموح."));
            // Even a reversed historical confirmation requires an audited correction path rather than cancellation.
            if (cancel && await db.Set<HungerFinancialEvent>().AnyAsync(e => e.RelatedEventId == id && e.Kind == "RemittanceConfirmation", ct))
                return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("دفعة لها تأكيد سابق لا تلغى."));
            current.Status = cancel ? "Cancelled" : "Submitted";
            var audit = new HungerFinancialEvent { ClientPlatformId = current.ClientPlatformId, SupervisorEmployeeId = current.SupervisorEmployeeId,
                RelatedEventId = id, Kind = cancel ? "RemittanceCancellation" : "RemittanceSubmission", Status = current.Status,
                BusinessDate = request.BusinessDate, IdempotencyKey = request.IdempotencyKey, RequestHash = hash,
                EvidenceReference = request.EvidenceReference, DetailsJson = Serialize(request) };
            db.Add(audit); return Result.Success(MoneyResult(audit) with { EventId = id });
        }, ct);
    }

    public async Task<Result<HungerMoneyResult>> ReverseRemittanceConfirmationAsync(Guid confirmationId, HungerRemittanceAction request, CancellationToken ct = default)
    {
        var confirmation = await db.Set<HungerFinancialEvent>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == confirmationId && e.Kind == "RemittanceConfirmation", ct);
        if (confirmation == null) return Result.Failure<HungerMoneyResult>(HungerErrors.NotFound);
        if (!await FinanceAccess(confirmation.ClientPlatformId, PermissionKeys.Hunger.ReceiptsCancelReverse, ct)) return Result.Failure<HungerMoneyResult>(HungerErrors.Forbidden);
        if (!ValidEvent(request.BusinessDate, request.IdempotencyKey, request.EvidenceReference) || request.BusinessDate < confirmation.BusinessDate
            || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
            return Result.Failure<HungerMoneyResult>(HungerErrors.Invalid("يلزم سبب ودليل وتاريخ العكس."));
        var hash = Hash(Serialize(new { confirmationId, request }));
        return await FinancialWrite([confirmation.ClientPlatformId, confirmation.SupervisorEmployeeId], async () =>
        {
            var old = await db.Set<HungerFinancialEvent>().SingleOrDefaultAsync(e => e.ClientPlatformId == confirmation.ClientPlatformId && e.IdempotencyKey == request.IdempotencyKey, ct);
            if (old != null) return old.RequestHash == hash ? Result.Success(MoneyResult(old)) : Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("مفتاح مستخدم لطلب مختلف."));
            if (await db.Set<HungerFinancialEvent>().AnyAsync(e => e.RelatedEventId == confirmationId && e.Kind == "RemittanceReversal", ct))
                return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("التأكيد معكوس سابقًا."));
            // Retain the original confirmation in the unit of work so persistence can verify the audited lifecycle transition.
            await db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == confirmationId, ct);
            var allocations = await db.Set<HungerReceiptAllocation>().Where(a => a.ConfirmationEventId == confirmationId).ToArrayAsync(ct);
            if (allocations.Length == 0 || allocations.Sum(a => a.Amount) != confirmation.Amount)
                return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("تخصيص التأكيد غير مكتمل؛ يلزم ترحيل البيانات القديمة."));
            var batch = await db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == confirmation.RelatedEventId, ct);
            var lines = await db.Set<HungerRemittanceLine>().Where(l => l.RemittanceEventId == batch.Id).ToArrayAsync(ct);
            foreach (var allocation in allocations)
            {
                var line = lines.Single(l => l.Id == allocation.RemittanceLineId);
                if (line.ConfirmedAmount < allocation.Amount) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("تخصيص غير متسق."));
                line.ConfirmedAmount -= allocation.Amount;
            }
            var postings = await db.Set<TreasuryPosting>().IgnoreQueryFilters().AsNoTracking().Where(p => p.ClientPlatformId == confirmation.ClientPlatformId && p.SourceEventId == confirmationId).ToArrayAsync(ct);
            if (postings.Length < 2 || postings.Sum(p => p.Amount) != 0) return Result.Failure<HungerMoneyResult>(HungerErrors.Conflict("قيود التأكيد غير مكتملة."));
            var reversal = new HungerFinancialEvent { ClientPlatformId = confirmation.ClientPlatformId, SupervisorEmployeeId = confirmation.SupervisorEmployeeId,
                RelatedEventId = confirmationId, Kind = "RemittanceReversal", Amount = confirmation.Amount, BusinessDate = request.BusinessDate,
                IdempotencyKey = request.IdempotencyKey, RequestHash = hash, EvidenceReference = request.EvidenceReference, DetailsJson = Serialize(request) };
            foreach (var posting in postings)
                db.Add(new TreasuryPosting { ClientPlatformId = posting.ClientPlatformId, SupervisorEmployeeId = posting.SupervisorEmployeeId,
                    SourceEventId = reversal.Id, ReversesPostingId = posting.Id, SubAccountId = posting.SubAccountId, SponsorId = posting.SponsorId, BatchId = reversal.Id,
                    Amount = -posting.Amount, Kind = TreasuryEntryKind.Reversal, OccurredAtUtc = clock.GetUtcNow(), ActorUserId = user.UserId!.Value,
                    ActorNameAr = "Hunger accountant receipt reversal", Reference = $"Hunger:{confirmationId:D}", Reason = request.Reason });
            batch.Status = lines.Any(l => l.ConfirmedAmount > 0) ? "PartiallyConfirmed" : "Submitted";
            db.Add(reversal); return Result.Success(MoneyResult(reversal));
        }, ct);
    }

    public async Task<Result<HungerPendingCollections>> PendingCollectionsAsync(Guid platformId, Guid supervisorEmployeeId, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await FinanceAccess(platformId, PermissionKeys.Hunger.FinanceRead, ct)) return Result.Failure<HungerPendingCollections>(HungerErrors.Forbidden);
        if (page < 1 || pageSize is < 1 or > 100) return Result.Failure<HungerPendingCollections>(HungerErrors.Invalid("صفحة غير صالحة."));
        var events = db.Set<HungerFinancialEvent>().AsNoTracking().Where(e => e.ClientPlatformId == platformId && e.SupervisorEmployeeId == supervisorEmployeeId);
        var batches = events.Where(e => e.Kind == "Remittance" && e.Status != "Cancelled" && e.Status != "Confirmed");
        var batchRows = await batches.OrderBy(e => e.BusinessDate).ThenBy(e => e.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        var ids = batchRows.Select(b => b.Id).ToArray();
        var lines = await db.Set<HungerRemittanceLine>().AsNoTracking().Where(l => ids.Contains(l.RemittanceEventId)).ToArrayAsync(ct);
        var sources = events.Where(RemittableSource);
        var sourceTotals = sources.Select(e => new { SourceEventId = e.Id, e.Amount,
            ConfirmedAmount = (from line in db.Set<HungerRemittanceLine>() where line.SourceEventId == e.Id select line.ConfirmedAmount).Sum(),
            RemainingAmount = e.Amount - (from line in db.Set<HungerRemittanceLine>() where line.SourceEventId == e.Id select line.ConfirmedAmount).Sum() });
        var pending = sourceTotals.Where(s => s.RemainingAmount > 0);
        return Result.Success(new HungerPendingCollections(batchRows.Select(b => new HungerPendingBatch(b.Id, b.Status, b.Amount,
            lines.Where(l => l.RemittanceEventId == b.Id).Sum(l => l.ConfirmedAmount),
            lines.Where(l => l.RemittanceEventId == b.Id).Sum(l => l.Amount - l.ConfirmedAmount),
            lines.Where(l => l.RemittanceEventId == b.Id).Select(l => new HungerPendingSource(l.SourceEventId, l.Amount, l.ConfirmedAmount, l.Amount - l.ConfirmedAmount)).ToArray())).ToArray(),
            await pending.OrderBy(s => s.SourceEventId).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(s => new HungerPendingSource(s.SourceEventId, s.Amount, s.ConfirmedAmount, s.RemainingAmount)).ToArrayAsync(ct),
            await batches.CountAsync(ct), await pending.CountAsync(ct), page, pageSize));
    }
}
