using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Ninja;

public abstract class NinjaDailySource : HistoryEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public Guid ImportId { get; set; }
    public DateOnly ReportDate { get; set; }
    public string CaptainId { get; set; } = string.Empty;
    public int SourceRowNumber { get; set; }
}
public sealed class NinjaDailyPerformanceSource : NinjaDailySource
{
    public int TotalOrders { get; set; }
    public decimal TotalWorkingHours { get; set; }
}
public sealed class NinjaDailyDeductionSource : NinjaDailySource
{
    public decimal TotalCollectedAmountAtDelivery { get; set; }
    public decimal TotalDeductions { get; set; }
}
