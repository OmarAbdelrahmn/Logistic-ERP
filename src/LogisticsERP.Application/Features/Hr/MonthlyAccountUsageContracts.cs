namespace LogisticsERP.Application.Features.Hr;
public interface IMonthlyAccountUsageResponse
{
    Guid UsageAccountId { get; }
    string MonthlyUsageStatus { get; set; }
    string UsageMonth { get; set; }
}
public sealed record MonthlyAccountUsage(string UsageMonth, IReadOnlySet<Guid> UsedAccountIds);
public interface IMonthlyAccountUsageService
{
    Task<MonthlyAccountUsage> GetAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken = default);
}
