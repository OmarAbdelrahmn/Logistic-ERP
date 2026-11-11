using System.Text.Json.Serialization;
using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hunger;

public sealed record HungerInvoiceSourceLine(string SourceKey, string Level, Guid? AccountId, Guid? RiderProfileId,
    decimal SignedNetAmount, decimal TaxAmount, Guid? ExistingFinancialEventId, string EvidenceReference)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NoShowCount { get; init; }
    // Invoice "Rider Balance" (>= 0) on a RiderLVL line. The month close of that account is its negative.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? RiderBalance { get; init; }
}
public sealed record HungerSettlementRequest(Guid PlatformId, Guid? AccountId, string Kind, DateOnly InvoicePeriod,
    DateOnly ChargeFrom, DateOnly ChargeThrough, DateOnly ObservedOn, decimal? WalletBalance, decimal? CompanyControlTotal,
    string? CanonicalLevel, IReadOnlyList<HungerInvoiceSourceLine>? Lines, Guid? InvoiceDocumentId, string IdempotencyKey,
    string EvidenceReference, string? SourceDocumentType = null, string? SourceReference = null, Guid? ClosingSnapshotId = null,
    // Company-level total of the wallet-balance deduction; must equal the sum of the lines' RiderBalance.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? RiderBalanceControlTotal = null);
public sealed record HungerSettlementResponse(Guid DocumentId, string Kind, string Status, DateOnly InvoicePeriod,
    DateOnly ChargeFrom, DateOnly ChargeThrough, decimal? WalletBalance, decimal? CanonicalGrossTotal, decimal? CompanyControlTotal, Guid? ClosingSnapshotId = null,
    int GeneratedCloses = 0, IReadOnlyList<Guid>? CloseConflictAccountIds = null);
// ApprovalDate is the day the platform approved the adjustment; SupervisorEmployeeId owns the resulting rider entries.
public sealed record HungerAdjustmentReconciliation(Guid ClosingSnapshotId, string EvidenceReference,
    DateOnly? ApprovalDate = null, Guid? SupervisorEmployeeId = null);
public interface IHungerSettlementService
{
    Task<Result<HungerSettlementResponse>> ReconcileAdjustmentAsync(Guid documentId, HungerAdjustmentReconciliation request, CancellationToken ct = default);
    Task<Result<HungerSettlementResponse>> RecordSettlementAsync(HungerSettlementRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<HungerSettlementResponse>>> SettlementsAsync(Guid platformId, DateOnly month, Guid? accountId, int page, int pageSize, CancellationToken ct = default);
}
