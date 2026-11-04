using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Hunger;

namespace LogisticsERP.Application.Features.Hunger;

public sealed record HungerQualityInput(int RowNumber, string AccountId, HungerQualityCounters Counters,
    string Segment, decimal VerificationSuccessRate, decimal OnTimeDeliveryScore, decimal FinalDeliveryQualityScore);
public sealed record HungerImportRow(int RowNumber, string AccountId, string Status, string? Error,
    HungerQualityCounters? DailyCounters = null, bool AdjustsNextRecord = false, string? AllocationStatus = null);
public sealed record HungerQualityPreview(Guid PreviewId, DateOnly ReportDate, DateTimeOffset ExpiresAtUtc,
    bool CanConfirm, IReadOnlyList<HungerImportRow> Rows);
public sealed record HungerImportResult(Guid ImportId, int ProcessedRows, int ImportedRows, int SkippedRows,
    int RejectedRows, int AdjustedNextRecords, int UnresolvedAllocationRows, IReadOnlyList<HungerImportRow> Rows);
public sealed record HungerQualityItem(Guid Id, Guid AccountId, string ExternalAccountId, DateOnly ReportDate,
    DateOnly CoverageFrom, HungerQualityCounters Cumulative, HungerQualityCounters Daily, string Segment, int? Rank,
    decimal VerificationSuccessRate, decimal OnTimeDeliveryScore, decimal FinalDeliveryQualityScore,
    string AllocationStatus, Guid? SoleRiderProfileId)
{
    public IReadOnlyList<LogisticsERP.Application.Features.Hr.PlatformDayShare> Allocations { get; init; } = [];
    public string RowVersion { get; init; } = string.Empty;
}
public sealed record HungerQualityReport(string Month, IReadOnlyList<HungerQualityItem> Records,
    IReadOnlyList<HungerQualityItem> LatestSnapshots, HungerQualityCounters Totals, int TotalRecords, int Page, int PageSize);
public sealed record HungerFile(byte[] Content, string ContentType, string FileName);
public sealed record HungerReallocationRequest(string RowVersion, string Reason);
public interface IHungerQualityService
{
    Task<Result<HungerFile>> TemplateAsync(CancellationToken ct = default);
    Task<Result<HungerQualityPreview>> PreviewAsync(string fileName, byte[] content, DateOnly reportDate, CancellationToken ct = default);
    Task<Result<HungerImportResult>> ConfirmAsync(Guid previewId, CancellationToken ct = default);
    Task<Result<HungerQualityReport>> ReportAsync(DateOnly month, Guid? accountId, Guid? riderId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<HungerImportResult>> ImportResultAsync(Guid importId, CancellationToken ct = default);
    Task<Result> ReallocateQualityAsync(Guid sourceId, HungerReallocationRequest request, CancellationToken ct = default);
}

public static class HungerErrors
{
    public static readonly OperationError Forbidden = new("hunger.forbidden", "لا تملك صلاحية هنقر المطلوبة.", ErrorType.Forbidden);
    public static readonly OperationError NotFound = new("hunger.not_found", "السجل غير موجود.", ErrorType.NotFound);
    public static OperationError Invalid(string message) => new("hunger.invalid", message, ErrorType.Validation);
    public static OperationError Conflict(string message) => new("hunger.conflict", message, ErrorType.Conflict);
}
