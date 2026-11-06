using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hunger;

public sealed record HungerAlertRuleRequest(Guid PlatformId, string Code, decimal Threshold, string Severity,
    DateOnly EffectiveFrom, bool Enabled, string EvidenceReference)
{
    public string Metric { get; init; } = "WorkingHours";
    public string Operator { get; init; } = "LessThan";
    public string Kind { get; init; } = "Alert";
    public string Period { get; init; } = "Day";
    public Guid? OperatingCityId { get; init; }
    public DateOnly? EffectiveThrough { get; init; }
}
public sealed record HungerAlertRuleItem(Guid Id, Guid PlatformId, string Code, decimal Threshold, string Severity,
    DateOnly EffectiveFrom, bool Enabled, string EvidenceReference)
{
    public string Metric { get; init; } = "WorkingHours";
    public string Operator { get; init; } = "LessThan";
    public string Kind { get; init; } = "Alert";
    public string Period { get; init; } = "Day";
    public Guid? OperatingCityId { get; init; }
    public DateOnly? EffectiveThrough { get; init; }
}
public sealed record HungerSummary(int InventoryAccounts, int AssignedNormalAccounts, int AvailableNormalAccounts,
    IReadOnlyDictionary<string, int> Dispositions, int ActualRiders, IReadOnlyDictionary<string, int> RegisteredVehicleTypes);
public interface IHungerReportingService
{
    Task<Result<HungerFile>> ExportPerformanceAsync(HungerPerformanceQuery query, CancellationToken ct = default);
    Task<Result<HungerPerformanceReport>> PerformanceAsync(HungerPerformanceQuery query, CancellationToken ct = default);
    Task<Result<IReadOnlyList<HungerPotentialFineItem>>> PotentialFinesAsync(DateOnly from, DateOnly through, CancellationToken ct = default);
    Task<Result> ResolvePotentialFineAsync(Guid id, HungerPotentialFineResolution request, CancellationToken ct = default);
    Task<Result<HungerFile>> ExportQualityAsync(DateOnly month, Guid? accountId, CancellationToken ct = default);
    Task<Result<HungerFile>> ExportQualityAsync(DateOnly month, Guid? accountId, HungerReportExportFilter filter, CancellationToken ct = default);
    Task<Result<HungerFile>> ExportOperationsAsync(DateOnly from, DateOnly through, Guid? accountId, CancellationToken ct = default);
    Task<Result<HungerFile>> ExportOperationsAsync(DateOnly from, DateOnly through, Guid? accountId, HungerReportExportFilter filter, CancellationToken ct = default);
    Task<Result<HungerSummary>> SummaryAsync(DateOnly month, CancellationToken ct = default);
    Task<Result<IReadOnlyList<HungerAlertRuleItem>>> AlertRulesAsync(CancellationToken ct = default);
    Task<Result<Guid>> AddAlertRuleAsync(HungerAlertRuleRequest request, CancellationToken ct = default);
}

public sealed record HungerPerformanceQuery(DateOnly From, DateOnly Through, string GroupBy = "account", string Period = "month",
    string Layout = "vertical", Guid? AccountId = null, Guid? RiderId = null, Guid? CityId = null,
    Guid? SponsorId = null, string? ActualVehicle = null, bool? TargetAchieved = null, int Page = 1, int PageSize = 50)
{
    public HungerAccountFilter? Accounts { get; init; }
    public string? ActualVehicleType { get; init; }
}

public sealed record HungerReportExportFilter
{
    public HungerAccountFilter? Accounts { get; init; }
    public Guid? RiderId { get; init; }
    public string? CaptainClassification { get; init; }
}
public sealed record HungerPerformanceRow(Guid EntityId, DateOnly Period, decimal CompletedOrders, decimal WorkingHours,
    int WorkedDays, int QualifiedDays, int UnknownDays, int ScheduledDays, int ElapsedDays, int RemainingDays,
    long? AcceptedOrders, long? StackedOrders, string Sponsorship, string ActualVehicle, decimal? Target,
    decimal? Deficit, bool? TargetAchieved, decimal? Forecast, decimal? RequiredPace, IReadOnlyList<string> Alerts)
{
    public long? RejectedOrders { get; init; }
    public long? CancelledOrders { get; init; }
    public long? NoShowCount { get; init; }
    public string ActualVehicleType { get; init; } = "Unknown";
    public DateOnly CoverageThrough { get; init; }
    public decimal ForecastObservedOrders { get; init; }
    public IReadOnlyList<HungerCatalogEvaluation> Catalog { get; init; } = [];
}
public sealed record HungerPerformanceReport(IReadOnlyList<HungerPerformanceRow> Items, int TotalRecords,
    decimal CompletedOrders, decimal WorkingHours, int DistinctAccounts, int DistinctRiders, string GroupBy, string Period, string Layout);
public sealed record HungerPotentialFineItem(Guid Id, Guid DailyOperationId, Guid AccountId, DateOnly Date, int NoShowCount,
    string Status, Guid? InvoiceDocumentId, string? InvoiceSourceKey, string? Evidence, string RowVersion);
public sealed record HungerPotentialFineResolution(string Status, Guid? InvoiceDocumentId, string? InvoiceSourceKey, string Evidence, string RowVersion);

public sealed record HungerCatalogEvaluation(Guid RuleId, string Code, string Kind, string Metric, DateOnly Period, decimal? Actual, decimal Threshold, bool Matches, string Severity, string Evidence)
{
    public decimal? Deficit { get; init; }
}
