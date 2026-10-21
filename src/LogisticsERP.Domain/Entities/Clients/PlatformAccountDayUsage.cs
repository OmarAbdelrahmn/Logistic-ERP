using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Clients;

public sealed class PlatformAccountDayUsage : AuditableEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public Guid RiderClientAssignmentId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public int? Sequence { get; set; }
    public int? OrdersCount { get; set; }
    public decimal? WorkingHours { get; set; }
    public decimal? CollectedAmount { get; set; }
    public decimal? DeductionsAmount { get; set; }
    public int? TotalValidOffers { get; set; }
    public int? DeclinedOffers { get; set; }
    public int? MissedOffers { get; set; }
    public string Reason { get; set; } = string.Empty;
}
