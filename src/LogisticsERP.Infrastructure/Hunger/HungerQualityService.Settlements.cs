using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService : IHungerSettlementService
{
    private static HungerSettlementResponse SettlementResponse(HungerSettlementDocument d) => new(d.Id, d.Kind, d.Status, d.InvoicePeriod,
        d.ChargeFrom, d.ChargeThrough, d.WalletBalance, d.CanonicalGrossTotal, d.CompanyControlTotal);
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
                || !ValidBalance(l.SignedNetAmount) || !ValidBalance(l.TaxAmount))
            || !request.Lines.Any(l => l.Level == request.CanonicalLevel)
            || request.Lines.Select(l => (l.Level, l.SourceKey)).Distinct().Count() != request.Lines.Count)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("حدد مصدرًا محاسبيًا واحدًا للفواتير وخطوطًا موثقة دون تكرار."));
        if (request.Kind != "Invoice" && (!request.AccountId.HasValue || !request.WalletBalance.HasValue || request.Lines is { Count: > 0 })) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("إغلاق المحفظة أو تسويتها يحتاج المعرف والرصيد الصريح."));
        if (request.Kind == "MonthClose" && (request.ChargeFrom.Day != 1 || request.ChargeThrough != request.ChargeFrom.AddMonths(1).AddDays(-1))) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("إغلاق الشهر يجب أن يغطي شهرًا كاملًا."));
        var hash = Hash(Serialize(request));
        return await FinancialWrite([request.PlatformId, request.AccountId ?? request.PlatformId], async () =>
        {
            var old = await db.Set<HungerSettlementDocument>().SingleOrDefaultAsync(d => d.ClientPlatformId == request.PlatformId && d.IdempotencyKey == request.IdempotencyKey, ct);
            if (old is not null) return old.RequestHash == hash ? Result.Success(SettlementResponse(old)) : Result.Failure<HungerSettlementResponse>(HungerErrors.Conflict("مفتاح المستند مستخدم لبيانات مختلفة."));
            if (request.AccountId.HasValue && !await db.PlatformRiderAccounts.AnyAsync(a => a.Id == request.AccountId && a.ClientPlatformId == request.PlatformId, ct)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("المعرف لا يخص هنقر المسموح."));
            if (request.InvoiceDocumentId.HasValue && !await db.Set<HungerSettlementDocument>().AnyAsync(d => d.Id == request.InvoiceDocumentId && d.ClientPlatformId == request.PlatformId && d.Kind == "Invoice" && d.Status == "Matched", ct)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Conflict("الفاتورة المرتبطة غير مطابقة أو خارج النطاق."));
            if (request.Lines is not null)
                foreach (var line in request.Lines)
                {
                    if (line.AccountId.HasValue && !await db.PlatformRiderAccounts.AnyAsync(a => a.Id == line.AccountId && a.ClientPlatformId == request.PlatformId, ct)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("معرف داخل الفاتورة خارج النطاق."));
                    if (line.ExistingFinancialEventId.HasValue && !await db.Set<HungerFinancialEvent>().AnyAsync(e => e.Id == line.ExistingFinancialEventId && e.ClientPlatformId == request.PlatformId
                        && (!line.AccountId.HasValue || e.PlatformRiderAccountId == line.AccountId) && (!line.RiderProfileId.HasValue || e.RiderProfileId == line.RiderProfileId), ct)) return Result.Failure<HungerSettlementResponse>(HungerErrors.Invalid("مرجع الواقعة المالية لا يطابق الطرف أو المنصة."));
                }
            var gross = request.Kind == "Invoice" ? request.Lines!.Where(l => l.Level == request.CanonicalLevel).Sum(l => l.SignedNetAmount + l.TaxAmount) : (decimal?)null;
            var item = new HungerSettlementDocument { ClientPlatformId = request.PlatformId, PlatformRiderAccountId = request.AccountId, InvoiceDocumentId = request.InvoiceDocumentId,
                Kind = request.Kind, InvoicePeriod = request.InvoicePeriod, ChargeFrom = request.ChargeFrom, ChargeThrough = request.ChargeThrough, ObservedOn = request.ObservedOn,
                WalletBalance = request.WalletBalance, CompanyControlTotal = request.CompanyControlTotal, CanonicalGrossTotal = gross,
                Status = request.Kind != "Invoice" || gross == request.CompanyControlTotal ? "Matched" : "NeedsReview", IdempotencyKey = request.IdempotencyKey,
                RequestHash = hash, EvidenceReference = request.EvidenceReference, DataJson = Serialize(request) };
            db.Add(item); return Result.Success(SettlementResponse(item));
        }, ct);
    }
    public async Task<Result<IReadOnlyList<HungerSettlementResponse>>> SettlementsAsync(Guid platformId, DateOnly month, Guid? accountId, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await FinanceAccess(platformId, PermissionKeys.Hunger.FinanceManage, ct)) return Result.Failure<IReadOnlyList<HungerSettlementResponse>>(HungerErrors.Forbidden);
        if (month == default || page < 1 || pageSize is < 1 or > 100) return Result.Failure<IReadOnlyList<HungerSettlementResponse>>(HungerErrors.Invalid("حدد الشهر والصفحة."));
        var start = new DateOnly(month.Year, month.Month, 1);
        var source = await db.Set<HungerSettlementDocument>().AsNoTracking().Where(d => d.ClientPlatformId == platformId && d.InvoicePeriod == start
            && (accountId == null || d.PlatformRiderAccountId == accountId)).OrderBy(d => d.ObservedOn).ThenBy(d => d.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success<IReadOnlyList<HungerSettlementResponse>>(source.Select(SettlementResponse).ToArray());
    }
}
