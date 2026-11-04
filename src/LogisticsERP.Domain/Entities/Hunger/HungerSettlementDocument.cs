using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Hunger;

/// <summary>Reviewed source evidence. Capturing an invoice never posts an advance a second time.</summary>
public sealed class HungerSettlementDocument : HistoryEntity
{
    public Guid ClientPlatformId { get; set; }
    public Guid? PlatformRiderAccountId { get; set; }
    public Guid? InvoiceDocumentId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public DateOnly InvoicePeriod { get; set; }
    public DateOnly ChargeFrom { get; set; }
    public DateOnly ChargeThrough { get; set; }
    public DateOnly ObservedOn { get; set; }
    public decimal? WalletBalance { get; set; }
    public decimal? CompanyControlTotal { get; set; }
    public decimal? CanonicalGrossTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string EvidenceReference { get; set; } = string.Empty;
    public string DataJson { get; set; } = "{}";
}
