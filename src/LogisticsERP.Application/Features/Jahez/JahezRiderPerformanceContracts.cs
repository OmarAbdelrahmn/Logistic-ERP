namespace LogisticsERP.Application.Features.Jahez;

public sealed record JahezPerformanceSummary(int PeriodDays, int DaysWithData, int DaysWithOrders,
    int ZeroOrderDays, int MissingDays, long Orders, decimal? AverageOrdersPerDayWithData, long? MaxOrdersPerDay);
public sealed record JahezPerformanceDay(DateOnly Date, long? Orders);
public sealed record JahezAccountPerformanceReport(Guid AccountId, string ExternalAccountId,
    JahezPerformanceSummary Summary, IReadOnlyList<JahezPerformanceDay> Daily);
public sealed record JahezRiderPerformanceReport(Guid RiderProfileId, string RiderName, string? RiderIqamaNo,
    JahezPerformanceSummary SelectedSummary, IReadOnlyList<JahezPerformanceDay> Daily,
    IReadOnlyList<JahezAccountPerformanceReport> Accounts);
public sealed record JahezRidersPerformanceReport(DateOnly StartDate, DateOnly EndDate, Guid? RiderId, Guid? AccountId,
    int Page, int PageSize, int TotalCount, IReadOnlyList<JahezRiderPerformanceReport> Items);
