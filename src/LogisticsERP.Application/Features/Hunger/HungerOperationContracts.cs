using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;

namespace LogisticsERP.Application.Features.Hunger;

public sealed record HungerOperationInput(int RowNumber, string AccountId, int CompletedOrders, decimal? ActualWorkingHours,
    int? RejectedOrders, int? CancelledOrders, int? NoShowCount)
{
    public string? CaptainClassification { get; init; }
    public int? AcceptedOrders { get; init; }
    public int? StackedOrders { get; init; }
    public string HoursUnit { get; init; } = "DecimalHours";
}
public sealed record HungerOperationItem(Guid Id, Guid AccountId, string ExternalAccountId, DateOnly ReportDate,
    int CompletedOrders, decimal? ActualWorkingHours, int? RejectedOrders, int? CancelledOrders, int? NoShowCount,
    int? ManualOrders, decimal? ManualHours, int? OrdersDifference, string AllocationStatus,
    IReadOnlyList<PlatformDayShare> Allocations, IReadOnlyList<string> Alerts, string RowVersion)
{
    public string? CaptainClassification { get; init; }
    public int? AcceptedOrders { get; init; }
    public int? StackedOrders { get; init; }
    public IReadOnlyList<HungerOperationAlert> AlertDetails { get; init; } = [];
}
public sealed record HungerOperationAlert(string Code, decimal Actual, decimal Threshold, string Severity, Guid? RuleId);
public sealed record HungerOperationReport(IReadOnlyList<HungerOperationItem> Items, int TotalRecords, long CompletedOrders,
    decimal WorkingHours, long? RejectedOrders, long? CancelledOrders, long? NoShowCount, int Page, int PageSize)
{
    public long? AcceptedOrders { get; init; }
    public long? StackedOrders { get; init; }
}
public sealed record HungerManualPerformanceRequest(int? Orders, decimal? Hours, string Reason, string RowVersion);
public interface IHungerOperationsService
{
    Task<Result<HungerFile>> OperationTemplateAsync(CancellationToken ct = default);
    Task<Result<HungerQualityPreview>> PreviewOperationsAsync(string fileName, byte[] content, DateOnly reportDate, CancellationToken ct = default);
    Task<Result<HungerImportResult>> ConfirmOperationsAsync(Guid previewId, CancellationToken ct = default);
    Task<Result<HungerOperationReport>> OperationsAsync(DateOnly from, DateOnly through, Guid? accountId, Guid? riderId, int page, int pageSize, CancellationToken ct = default);
    Task<Result> ManualPerformanceAsync(Guid sourceId, HungerManualPerformanceRequest request, CancellationToken ct = default);
}
