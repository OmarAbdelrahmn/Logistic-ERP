using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Hunger;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService : IHungerSettlementService
{
    private static HungerSettlementResponse SettlementResponse(HungerSettlementDocument d) => new(d.Id, d.Kind, d.Status, d.InvoicePeriod,
        d.ChargeFrom, d.ChargeThrough, d.WalletBalance, d.CanonicalGrossTotal, d.CompanyControlTotal, d.ClosingSnapshotId);
    private static HungerInvoiceSourceLine[] BalanceLines(HungerSettlementRequest request) => request.Lines?.Where(l => l.RiderBalance.HasValue).ToArray() ?? [];
    // Month closes fed by the invoice. An existing close is never overwritten: a different value is reported as a conflict.
    private async Task<(int Generated, Guid[] Conflicts)> InvoiceCloses(HungerSettlementDocument invoice, HungerSettlementRequest request, bool create, CancellationToken ct)
    {
        var lines = BalanceLines(request);
        if (lines.Length == 0 || invoice.Status != "Matched" || invoice.RiderBalanceTotal is null) return (0, []);
        var ids = lines.Select(l => l.AccountId!.Value).ToArray();
        var existing = await db.Set<HungerSettlementDocument>().AsNoTracking().Where(d => d.Kind == "MonthClose" && d.ChargeFrom == invoice.InvoicePeriod
            && ids.Contains(d.PlatformRiderAccountId!.Value)).ToArrayAsync(ct);
        var generated = 0; var conflicts = new List<Guid>();
        foreach (var line in lines)
        {
            var account = line.AccountId!.Value; var close = -line.RiderBalance!.Value;
            if (existing.SingleOrDefault(d => d.PlatformRiderAccountId == account) is { } current)
            {
                if (current.WalletBalance != close || current.Status != "Matched") conflicts.Add(account);
                continue;
            }
            if (!create) continue;
            db.Add(new HungerSettlementDocument { ClientPlatformId = invoice.ClientPlatformId, PlatformRiderAccountId = account, InvoiceDocumentId = invoice.Id,
                Kind = "MonthClose", InvoicePeriod = invoice.InvoicePeriod, ChargeFrom = invoice.InvoicePeriod, ChargeThrough = invoice.InvoicePeriod.AddMonths(1).AddDays(-1),
                ObservedOn = invoice.ObservedOn, WalletBalance = close, Status = "Matched", IdempotencyKey = $"InvoiceClose:{invoice.Id:D}:{account:D}",
                RequestHash = Hash(Serialize(line)), EvidenceReference = line.EvidenceReference, DataJson = Serialize(new { line.SourceKey, line.RiderBalance }) });
            generated++;
        }
        return (generated, conflicts.ToArray());
    }
    public async Task<Result<HungerSettlementResponse>> RecordSettlementAsync(HungerSettlementRequest request, CancellationToken ct = default)
    {
        if (!await FinanceAccess(request.PlatformId, PermissionKeys.Hunger.FinanceManage, ct)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Forbidden);
        if (request.Kind is not ("Invoice" or "MonthClose" or "Adjustment") || request.InvoicePeriod == default || request.InvoicePeriod.Day != 1
            || request.ChargeFrom == default || request.ChargeThrough < request.ChargeFrom || request.ObservedOn < request.ChargeThrough
            || !ValidEvent(request.ObservedOn, request.IdempotencyKey, request.EvidenceReference) || request.WalletBalance.HasValue && !ValidBalance(request.WalletBalance.Value)
            || request.CompanyControlTotal.HasValue && !ValidBalance(request.CompanyControlTotal.Value)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("الفترات أو أدلة المستند غير صالحة."));
        if (request.Kind == "Invoice" && (request.CanonicalLevel is not ("RiderLVL" or "Breakdown") || request.Lines is null || request.Lines.Count is 0 or > 10000
            || !request.CompanyControlTotal.HasValue || request.Lines.Any(l => l.Level is not ("CoLVL" or "RiderLVL" or "Breakdown" or "Vehicle")
                || string.IsNullOrWhiteSpace(l.SourceKey) || l.SourceKey.Length > 200 || string.IsNullOrWhiteSpace(l.EvidenceReference) || l.EvidenceReference.Length > 1000
                || !ValidBalance(l.SignedNetAmount) || !ValidBalance(l.TaxAmount) || l.NoShowCount < 0)
            || !request.Lines.Any(l => l.Level == request.CanonicalLevel)
            || request.Lines.Select(l => (l.Level, l.SourceKey)).Distinct().Count() != request.Lines.Count)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("حدد مصدرًا محاسبيًا واحدًا للفواتير وخطوطًا موثقة دون تكرار."));
        var balanceLines = BalanceLines(request);
        if (request.Kind != "Invoice" && request.RiderBalanceControlTotal.HasValue || balanceLines.Length > 0 && (request.Kind != "Invoice"
            || !request.RiderBalanceControlTotal.HasValue || !ValidBalance(request.RiderBalanceControlTotal.Value)
            || request.ChargeFrom != request.InvoicePeriod || request.ChargeThrough != request.InvoicePeriod.AddMonths(1).AddDays(-1)
            || balanceLines.Any(l => l.Level != "RiderLVL" || !l.AccountId.HasValue || l.RiderBalance < 0 || !ValidBalance(l.RiderBalance!.Value))
            || balanceLines.Select(l => l.AccountId).Distinct().Count() != balanceLines.Length))
            return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("أرصدة المناديب تحتاج فاتورة شهر كامل وإجمالي رقابة وسطر RiderLVL واحد غير سالب لكل معرف."));
        if (request.Kind != "Invoice" && (!request.AccountId.HasValue || !request.WalletBalance.HasValue || request.Lines is { Count: > 0 })) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("إغلاق المحفظة أو تسويتها يحتاج المعرف والرصيد الصريح."));
        if (request.Kind == "MonthClose" && (request.ChargeFrom.Day != 1 || request.ChargeThrough != request.ChargeFrom.AddMonths(1).AddDays(-1))) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("إغلاق الشهر يجب أن يغطي شهرًا كاملًا."));
        if (request.Kind == "Adjustment" && (string.IsNullOrWhiteSpace(request.SourceDocumentType) || request.SourceDocumentType.Length > 64
            || string.IsNullOrWhiteSpace(request.SourceReference) || request.SourceReference.Length > 200 || !request.ClosingSnapshotId.HasValue))
            return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("التسوية تحتاج نوع ومرجع المصدر وإغلاق الشهر المرتبط."));
        request = request with { SourceDocumentType = request.SourceDocumentType?.Trim(), SourceReference = request.SourceReference?.Trim() };
        var hash = Hash(Serialize(request));
        return await FinancialWrite([request.PlatformId, request.AccountId ?? request.PlatformId], async () =>
        {
            var old = await db.Set<HungerSettlementDocument>().SingleOrDefaultAsync(d => d.ClientPlatformId == request.PlatformId && d.IdempotencyKey == request.IdempotencyKey, ct);
            if (old is not null && old.RequestHash != hash) return Result.Failure<HungerSettlementResponse>(HungerErrors.Conflict("مفتاح المستند مستخدم لبيانات مختلفة."));
            if (old is not null)
            {
                var replayed = await InvoiceCloses(old, request, false, ct);
                return Result.Success(SettlementResponse(old) with { CloseConflictAccountIds = replayed.Conflicts.Length > 0 ? replayed.Conflicts : null });
            }
            if (request.AccountId.HasValue && !await db.PlatformRiderAccounts.AnyAsync(a => a.Id == request.AccountId && a.ClientPlatformId == request.PlatformId, ct)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("المعرف لا يخص هنقر المسموح."));
            if (request.InvoiceDocumentId.HasValue && !await db.Set<HungerSettlementDocument>().AnyAsync(d => d.Id == request.InvoiceDocumentId && d.ClientPlatformId == request.PlatformId && d.Kind == "Invoice" && d.Status == "Matched", ct)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Conflict("الفاتورة المرتبطة غير مطابقة أو خارج النطاق."));
            if (request.Kind == "Adjustment")
            {
                if (!await ValidAdjustmentClose(request.PlatformId, request.AccountId, request.InvoicePeriod, request.ClosingSnapshotId!.Value, ct))
                    return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("إغلاق التسوية لا يطابق الحساب والفترة."));
                if (await db.Set<HungerSettlementDocument>().AnyAsync(d => d.Kind == "Adjustment" && d.PlatformRiderAccountId == request.AccountId
                    && d.InvoicePeriod == request.InvoicePeriod && d.SourceDocumentType == request.SourceDocumentType && d.SourceReference == request.SourceReference, ct))
                    return Result.Failure<HungerSettlementResponse>(HungerErrors.Conflict("مصدر التسوية مسجل بالفعل ولو بمفتاح مختلف."));
            }
            if (request.Lines is not null)
                foreach (var line in request.Lines)
                {
                    if (line.AccountId.HasValue && !await db.PlatformRiderAccounts.AnyAsync(a => a.Id == line.AccountId && a.ClientPlatformId == request.PlatformId, ct)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("معرف داخل الفاتورة خارج النطاق."));
                    if (line.ExistingFinancialEventId.HasValue && !await db.Set<HungerFinancialEvent>().AnyAsync(e => e.Id == line.ExistingFinancialEventId && e.ClientPlatformId == request.PlatformId
                        && (!line.AccountId.HasValue || e.PlatformRiderAccountId == line.AccountId) && (!line.RiderProfileId.HasValue || e.RiderProfileId == line.RiderProfileId), ct)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("مرجع الواقعة المالية لا يطابق الطرف أو المنصة."));
                }
            decimal? riderBalances = balanceLines.Length > 0 ? balanceLines.Sum(l => l.RiderBalance!.Value) : null;
            var balancesMatch = riderBalances == request.RiderBalanceControlTotal;
            var gross = request.Kind == "Invoice" ? request.Lines!.Where(l => l.Level == request.CanonicalLevel).Sum(l => l.SignedNetAmount + l.TaxAmount) : (decimal?)null;
            var item = new HungerSettlementDocument { ClientPlatformId = request.PlatformId, PlatformRiderAccountId = request.AccountId, InvoiceDocumentId = request.InvoiceDocumentId,
                ClosingSnapshotId = request.ClosingSnapshotId, SourceDocumentType = request.SourceDocumentType, SourceReference = request.SourceReference,
                Kind = request.Kind, InvoicePeriod = request.InvoicePeriod, ChargeFrom = request.ChargeFrom, ChargeThrough = request.ChargeThrough, ObservedOn = request.ObservedOn,
                WalletBalance = request.WalletBalance, CompanyControlTotal = request.CompanyControlTotal, CanonicalGrossTotal = gross,
                RiderBalanceTotal = balancesMatch ? riderBalances : null,
                Status = request.Kind == "Adjustment" ? "Unmatched" : request.Kind == "MonthClose" || gross == request.CompanyControlTotal && balancesMatch ? "Matched" : "NeedsReview", IdempotencyKey = request.IdempotencyKey,
                RequestHash = hash, EvidenceReference = request.EvidenceReference, DataJson = Serialize(request) };
            db.Add(item);
            var closes = await InvoiceCloses(item, request, true, ct);
            return Result.Success(SettlementResponse(item) with { GeneratedCloses = closes.Generated,
                CloseConflictAccountIds = closes.Conflicts.Length > 0 ? closes.Conflicts : null });
        }, ct);
    }
    private Task<bool> ValidAdjustmentClose(Guid platform, Guid? account, DateOnly period, Guid close, CancellationToken ct) =>
        db.Set<HungerSettlementDocument>().AnyAsync(d => d.Id == close && d.Kind == "MonthClose" && d.Status == "Matched"
            && d.ClientPlatformId == platform && d.PlatformRiderAccountId == account && d.ChargeFrom == period, ct);
    public async Task<Result<HungerSettlementResponse>> ReconcileAdjustmentAsync(Guid documentId, HungerAdjustmentReconciliation request, CancellationToken ct = default)
    {
        var document = await db.Set<HungerSettlementDocument>().AsNoTracking().SingleOrDefaultAsync(d => d.Id == documentId && d.Kind == "Adjustment", ct);
        if (document is null) return Result.Failure<HungerSettlementResponse>(HungerErrors.NotFound);
        if (!await FinanceAccess(document.ClientPlatformId, PermissionKeys.Hunger.FinanceManage, ct)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(request.EvidenceReference) || request.EvidenceReference.Length > 1000)
            return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("دليل المطابقة مطلوب."));
        var posts = Cr005;
        if (posts && (request.ApprovalDate is not { } day || day > BusinessToday || !request.SupervisorEmployeeId.HasValue))
            return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("حدد تاريخ اعتماد التسوية والمشرف المسؤول."));
        return await FinancialWrite([document.ClientPlatformId, document.PlatformRiderAccountId!.Value], async () =>
        {
            var item = await db.Set<HungerSettlementDocument>().SingleAsync(d => d.Id == documentId, ct);
            if (item.ClosingSnapshotId != request.ClosingSnapshotId || !await ValidAdjustmentClose(item.ClientPlatformId, item.PlatformRiderAccountId, item.InvoicePeriod, request.ClosingSnapshotId, ct))
                return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("الإغلاق لا يطابق مصدر التسوية."));
            if (item.Status == "Matched" && item.ReconciliationEvidence != request.EvidenceReference)
                return Result.Failure<HungerSettlementResponse>(HungerErrors.Conflict("تمت مطابقة التسوية بدليل مختلف."));
            if (posts) return await ReconcileWithRiderEntries(item, request, ct);
            item.Status = "Matched"; item.ReconciliationEvidence = request.EvidenceReference;
            return Result.Success(SettlementResponse(item));
        }, ct);
    }
    // Latest assignment covering the day; on a swap day that is the rider who ended the day with the account.
    private Task<Guid?> RiderOn(Guid accountId, DateOnly date, CancellationToken ct) => db.RiderClientAssignments.AsNoTracking()
        .Where(a => a.PlatformRiderAccountId == accountId && a.Status != LogisticsERP.Domain.Enums.RiderAssignmentStatus.Cancelled
            && a.EffectiveFrom <= date && (a.EffectiveTo == null || a.EffectiveTo >= date))
        .OrderByDescending(a => a.EffectiveFrom).ThenByDescending(a => a.Id).Select(a => (Guid?)a.RiderProfileId).FirstOrDefaultAsync(ct);

    // CR-005 T4: no custody or treasury movement. The adjustment becomes an advance on the holder and a credit to the closer.
    private async Task<Result<HungerSettlementResponse>> ReconcileWithRiderEntries(HungerSettlementDocument item, HungerAdjustmentReconciliation request, CancellationToken ct)
    {
        var approval = request.ApprovalDate!.Value; var supervisor = request.SupervisorEmployeeId!.Value; var account = item.PlatformRiderAccountId!.Value;
        if (item.Status == "Matched")
            return item.ApprovalDate is null || item.ApprovalDate == approval ? Result.Success(SettlementResponse(item))
                : Result.Failure<HungerSettlementResponse>(HungerErrors.Conflict("تمت مطابقة التسوية بتاريخ اعتماد مختلف."));
        var close = await db.Set<HungerSettlementDocument>().AsNoTracking().SingleAsync(d => d.Id == request.ClosingSnapshotId, ct);
        if (approval <= close.ChargeThrough) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("تاريخ اعتماد التسوية يجب أن يلي الشهر المغلق."));
        if (await FinanceConfig(item.ClientPlatformId, supervisor, ct) is null) return Result.Failure<HungerSettlementResponse>(HungerErrors.Conflict("لم يعتمد ربط حسابات المشرف المالية."));
        var closer = await RiderOn(account, close.ChargeThrough, ct); var holder = await RiderOn(account, approval, ct);
        item.ReconciliationEvidence = request.EvidenceReference; item.ApprovalDate = approval;
        // Without both riders the adjustment cannot be attributed; it stays out of the wallet formula until corrected.
        if (closer is null || holder is null) { item.Status = "NeedsReview"; return Result.Success(SettlementResponse(item)); }
        item.Status = "Matched";
        var adjustment = item.WalletBalance ?? 0;
        if (adjustment == 0) return Result.Success(SettlementResponse(item));
        var (holderChange, closerChange) = HungerWalletRules.CloseAdjustment(adjustment);
        // Link the holder's entry to the funded top-up it offsets, so the remaining top-up can be derived without editing that row.
        var topUp = await db.Set<HungerFinancialEvent>().AsNoTracking().Where(e => e.PlatformRiderAccountId == account && e.Kind == "TopUpAdvance"
            && e.RiderProfileId == holder && e.Status == "Posted" && e.BusinessDate > close.ChargeThrough && e.BusinessDate <= approval)
            .OrderByDescending(e => e.BusinessDate).ThenByDescending(e => e.Id).Select(e => (Guid?)e.Id).FirstOrDefaultAsync(ct);
        var hash = Hash(Serialize(new { item.Id, request }));
        HungerFinancialEvent Entry(string kind, Guid rider, decimal change, Guid? related) => new() { ClientPlatformId = item.ClientPlatformId,
            SupervisorEmployeeId = supervisor, PlatformRiderAccountId = account, RiderProfileId = rider, RelatedEventId = related, Kind = kind,
            Amount = Math.Abs(adjustment), RiderBalanceChange = change, BusinessDate = approval, IdempotencyKey = $"CloseAdj:{item.Id:D}:{kind}",
            RequestHash = hash, EvidenceReference = request.EvidenceReference, ApprovalStatus = "PendingSeal", Role = RoleOf(kind),
            DetailsJson = Serialize(new { DocumentId = item.Id, request.ClosingSnapshotId, ApprovalDate = approval, CloserRiderProfileId = closer,
                HolderRiderProfileId = holder, Adjustment = adjustment }) };
        db.Add(Entry("CloseAdjustmentAdvance", holder.Value, holderChange, topUp));
        db.Add(Entry("CloseAdjustmentWash", closer.Value, closerChange, null));
        return Result.Success(SettlementResponse(item));
    }

    public async Task<Result<IReadOnlyList<HungerSettlementResponse>>> SettlementsAsync(Guid platformId, DateOnly month, Guid? accountId, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await FinanceAccess(platformId, PermissionKeys.Hunger.FinanceRead, ct)) return Result.Failure<IReadOnlyList<HungerSettlementResponse>>(HungerErrors.Forbidden);
        if (month == default || page < 1 || pageSize is < 1 or > 100) return Result.Failure<IReadOnlyList<HungerSettlementResponse>>(HungerErrors.Invalid("حدد الشهر والصفحة."));
        var start = new DateOnly(month.Year, month.Month, 1);
        var source = await db.Set<HungerSettlementDocument>().AsNoTracking().Where(d => d.ClientPlatformId == platformId && d.InvoicePeriod == start
            && (accountId == null || d.PlatformRiderAccountId == accountId)).OrderBy(d => d.ObservedOn).ThenBy(d => d.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success<IReadOnlyList<HungerSettlementResponse>>(source.Select(SettlementResponse).ToArray());
    }
}
