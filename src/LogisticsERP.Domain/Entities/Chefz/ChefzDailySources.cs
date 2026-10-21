using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Chefz;

public sealed class ChefzDailyPerformanceSource : AuditableEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public Guid ImportId { get; set; }
    public DateOnly ReportDate { get; set; }
    public string ReportIdNumber { get; set; } = string.Empty;
    public int SourceRowNumber { get; set; }
    public int TotalValidOffers { get; set; }
    public int AcceptedOffers { get; set; }
    public int DeclinedOffers { get; set; }
    public int MissedOffers { get; set; }
    public decimal AcceptanceRate { get; set; }
}

public sealed class ChefzImportedOrderRow : HistoryEntity
{
    public Guid ImportId { get; set; }
    public int SourceRowNumber { get; set; }
    public Guid PlatformRiderAccountId { get; set; }
    public Guid RiderClientAssignmentId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public int UsageSequence { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public decimal DeliveryFees { get; set; }
    public decimal Tips { get; set; }
    public decimal Compensation { get; set; }
    public decimal Bonuses { get; set; }
    public decimal Deductions { get; set; }
    public decimal CashCollected { get; set; }
}
