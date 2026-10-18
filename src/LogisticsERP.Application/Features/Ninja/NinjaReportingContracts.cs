namespace LogisticsERP.Application.Features.Ninja;

public sealed record NinjaReportMetrics(int PerformanceRecords, int DeductionRecords, long TotalOrders,
    decimal TotalWorkingHours, decimal? OrdersPerWorkingHour, decimal TotalCollectedAmountAtDelivery, decimal TotalDeductions);
public sealed record NinjaPerformanceReport(DateOnly From, DateOnly To, Guid? AccountId, Guid? RiderId,
    int DaysWithPerformance, int DaysWithDeductions, int AccountsWithData, int RidersWithData, NinjaReportMetrics Metrics);
public sealed record NinjaDailyReport(DateOnly Date, NinjaReportMetrics Metrics);
public sealed record NinjaRiderReport(Guid RiderProfileId, NinjaPersonResponse? Rider, int AccountsWithData,
    int DaysWithPerformance, int DaysWithDeductions, NinjaReportMetrics Metrics);
