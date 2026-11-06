using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Hunger;

public sealed class HungerDailyOperation : AuditableEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public Guid ImportId { get; set; }
    public DateOnly ReportDate { get; set; }
    public int SourceRowNumber { get; set; }
    public int CompletedOrders { get; set; }
    public decimal? ActualWorkingHours { get; set; }
    public string HoursUnit { get; set; } = "DecimalHours";
    public string? CaptainClassification { get; set; }
    public int? AcceptedOrders { get; set; }
    public int? StackedOrders { get; set; }
    public int? RejectedOrders { get; set; }
    public int? CancelledOrders { get; set; }
    public int? NoShowCount { get; set; }
    public int? ManualOrders { get; set; }
    public decimal? ManualHours { get; set; }
    public string? ManualReason { get; set; }
    public string AllocationsJson { get; set; } = "[]";
    public string AllocationStatus { get; set; } = "UnresolvedAllocation";
}
