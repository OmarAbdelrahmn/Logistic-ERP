using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Ninja;

public sealed class NinjaAccountDetails : AuditableEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public string? PhoneNumber { get; set; }
}

public sealed class NinjaDailyImport : HistoryEntity
{
    public DateOnly ReportDate { get; set; }
    public bool IsDeductions { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }
}

public abstract class NinjaDailyRecord : HistoryEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public Guid RiderClientAssignmentId { get; set; }
    public Guid RiderProfileId { get; set; }
    public Guid ImportId { get; set; }
    public DateOnly ReportDate { get; set; }
    public string CaptainId { get; set; } = string.Empty;
    public int SourceRowNumber { get; set; }
}

public sealed class NinjaDailyPerformance : NinjaDailyRecord
{
    public int TotalOrders { get; set; }
    public decimal TotalWorkingHours { get; set; }
}

public sealed class NinjaDailyDeduction : NinjaDailyRecord
{
    public decimal TotalCollectedAmountAtDelivery { get; set; }
    public decimal TotalDeductions { get; set; }
}
