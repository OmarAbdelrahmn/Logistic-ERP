using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hunger;

public sealed record HungerInvoiceSourceLine(string SourceKey, string Level, Guid? AccountId, Guid? RiderProfileId,
    decimal SignedNetAmount, decimal TaxAmount, Guid? ExistingFinancialEventId, string EvidenceReference);
public sealed record HungerSettlementRequest(Guid PlatformId, Guid? AccountId, string Kind, DateOnly InvoicePeriod,
    DateOnly ChargeFrom, DateOnly ChargeThrough, DateOnly ObservedOn, decimal? WalletBalance, decimal? CompanyControlTotal,
    string? CanonicalLevel, IReadOnlyList<HungerInvoiceSourceLine>? Lines, Guid? InvoiceDocumentId, string IdempotencyKey,
    string EvidenceReference, string? SourceDocumentType = null, string? SourceReference = null, Guid? ClosingSnapshotId = null);
public sealed record HungerSettlementResponse(Guid DocumentId, string Kind, string Status, DateOnly InvoicePeriod,
    DateOnly ChargeFrom, DateOnly ChargeThrough, decimal? WalletBalance, decimal? CanonicalGrossTotal, decimal? CompanyControlTotal, Guid? ClosingSnapshotId = null);
public sealed record HungerAdjustmentReconciliation(Guid ClosingSnapshotId, string EvidenceReference);
public interface IHungerSettlementService
{
    Task<Result<HungerSettlementResponse>> ReconcileAdjustmentAsync(Guid documentId, HungerAdjustmentReconciliation request, CancellationToken ct = default);
    Task<Result<HungerSettlementResponse>> RecordSettlementAsync(HungerSettlementRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<HungerSettlementResponse>>> SettlementsAsync(Guid platformId, DateOnly month, Guid? accountId, int page, int pageSize, CancellationToken ct = default);
}
