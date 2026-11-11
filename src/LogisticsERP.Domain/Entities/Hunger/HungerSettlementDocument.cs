using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Hunger;

/// <summary>Reviewed source evidence. Capturing an invoice never posts an advance a second time.</summary>
public sealed class HungerSettlementDocument : HistoryEntity
{
    public Guid ClientPlatformId { get; set; }
    public Guid? PlatformRiderAccountId { get; set; }
    public Guid? InvoiceDocumentId { get; set; }
    public Guid? ClosingSnapshotId { get; set; }
    public string? SourceDocumentType { get; set; }
    public string? SourceReference { get; set; }
    public string? ReconciliationEvidence { get; set; }
    public string Kind { get; set; } = string.Empty;
    public DateOnly InvoicePeriod { get; set; }
    public DateOnly ChargeFrom { get; set; }
    public DateOnly ChargeThrough { get; set; }
    public DateOnly ObservedOn { get; set; }
    // Adjustment only: the day the platform approved it. Decides which rider the adjustment belongs to.
    public DateOnly? ApprovalDate { get; set; }
    public decimal? WalletBalance { get; set; }
    public decimal? CompanyControlTotal { get; set; }
    public decimal? CanonicalGrossTotal { get; set; }
    // Set on an invoice whose rider balances matched their control total; such an invoice is the source of month closes.
    public decimal? RiderBalanceTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string EvidenceReference { get; set; } = string.Empty;
    public string DataJson { get; set; } = "{}";
}
