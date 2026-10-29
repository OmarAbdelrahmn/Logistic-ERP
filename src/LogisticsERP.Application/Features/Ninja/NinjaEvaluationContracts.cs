using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Ninja;

namespace LogisticsERP.Application.Features.Ninja;

public sealed record NinjaEvaluationQuery
{
    public int Year { get; init; }
    public int Month { get; init; }
    public DateOnly? AsOf { get; init; }
    public string View { get; init; } = "riders";
    public Guid? AccountId { get; init; }
    public Guid? RiderId { get; init; }
    public Guid? CityId { get; init; }
    public Guid? SponsorId { get; init; }
    public string? PricingType { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed record NinjaPricingPeriod(DateOnly From, DateOnly To, string PricingType);
public sealed record NinjaEvaluationContextRequest(Guid? ExpectedRevisionId, DateOnly? ConfirmedFirstDeliveryDate,
    IReadOnlyList<NinjaPricingPeriod> PricingPeriods, string Reason, Guid? ReviewedSnapshotId = null);
public sealed record NinjaEvaluationContextResponse(Guid AccountId, Guid? RevisionId, int Revision,
    DateOnly? ConfirmedFirstDeliveryDate, IReadOnlyList<NinjaPricingPeriod> PricingPeriods);
public sealed record NinjaResponsibilityInput(Guid AssignmentId, DateOnly Date, decimal Weight);
public sealed record NinjaResponsibilityRequest(int Year, int Month, Guid? ExpectedRevisionId,
    IReadOnlyList<NinjaResponsibilityInput> Lines, string Reason, Guid? ReviewedSnapshotId = null);
public sealed record NinjaResponsibilityResponse(Guid AccountId, int Year, int Month, Guid? RevisionId, int Revision,
    IReadOnlyList<NinjaResponsibilityInput> Lines);
public sealed record NinjaEvaluationDay(DateOnly Date, Guid AccountId, string CaptainId, Guid? AssignmentId,
    Guid? RiderProfileId, string? PricingType, decimal? Responsibility, int? Orders, decimal? Hours,
    decimal? Target, decimal? Gap, string HoursStatus, string Coverage, Guid? SourceId,
    Guid? ImportId, int? SourceRowNumber, string AllocationOrigin, DateTimeOffset? ImportedAtUtc);
public sealed record NinjaEvaluationTotals(long Orders, decimal Hours, decimal Responsibility,
    decimal? Target, decimal? Gap, decimal? CompletionRate, decimal? OrdersPerHour);
public sealed record NinjaAttendanceEvaluation(decimal Scheduled, decimal Qualified, decimal Unknown,
    int ShortHoursDays, decimal RequiredQualifyingUnits, int? RequiredQualifyingDays, string Status);
public sealed record NinjaMoneySegment(string PricingType, decimal Responsibility, NinjaOrderSuggestion Calculation);
public sealed record NinjaSuggestedMoney(decimal OrderAmount, decimal SuggestedAmount, bool AttendanceReduction,
    string AttendanceBasis, IReadOnlyList<NinjaMoneySegment> Segments)
{
    public bool Provisional { get; } = true;
    public string Basis { get; } = "CompletedOrdersAndWorkingHoursOnly";
    public string InvoiceStatus { get; } = "PendingExternalInvoice";
    public decimal? ActualInvoiceAmount { get; }
}
public sealed record NinjaEvaluationForecast(decimal ProjectedOrders, decimal ProjectedGap,
    decimal DailyRunRate, decimal RemainingResponsibility, decimal RemainingOrders,
    decimal? RequiredDailyRate, decimal ProjectedQualifiedUnits, string ProjectedAttendanceStatus,
    bool AttendanceRecoverable, NinjaSuggestedMoney? SuggestedMoney, bool Reliable,
    string Assumption);
public sealed record NinjaEvaluationRow(string Key, Guid? AccountId, Guid? RiderProfileId, string? RiderName,
    IReadOnlyList<Guid> AccountIds, IReadOnlyList<string> CaptainIds, IReadOnlyList<Guid> AssignmentIds,
    Guid? CityId, string? CityName, Guid? SponsorId, string? EstablishmentName,
    DateOnly? LatestImportedDate, DateOnly? LatestCompleteDate, DateOnly RequestedAsOf, int? DataDelayDays,
    NinjaEvaluationTotals Observed, NinjaEvaluationTotals Evaluated, decimal? PeriodTarget,
    decimal? TargetDueToday, NinjaAttendanceEvaluation Attendance, string TargetStatus,
    NinjaSuggestedMoney? SuggestedMoney, NinjaEvaluationForecast? Forecast,
    NinjaEvaluationForecast? ContinuationScenario, IReadOnlyList<NinjaEvaluationDay> Days,
    IReadOnlyList<string> Reasons)
{
    public IReadOnlyDictionary<string, string> OtherEligibility { get; init; } =
        new Dictionary<string, string> { ["Acceptance"] = "Unknown", ["Timing"] = "Unknown", ["Uniform"] = "Unknown", ["LeaveCompliance"] = "Unknown" };
}
public sealed record NinjaAccountSuggestionComparison(Guid AccountId, decimal? AccountSuggestedAmount,
    decimal? RiderSuggestedAmounts, decimal? Difference, DateOnly? AccountCutoff, bool RiderContextComplete);
public sealed record NinjaEvaluationSummary(int TotalRows, int Accounts, int Riders, long ObservedOrders,
    decimal ObservedHours, decimal? SuggestedAmount, bool MixedCutoffs, int RowsMissingSuggestion,
    IReadOnlyList<NinjaAccountSuggestionComparison> AccountComparisons);
public sealed record NinjaEvaluationReport(int Year, int Month, DateOnly Today, DateOnly RequestedAsOf,
    string View, string PolicyVersion, string InputRevision, DateTimeOffset ComputedAtUtc,
    NinjaEvaluationSummary Summary, IReadOnlyList<NinjaEvaluationRow> Items, int Page, int PageSize);
public sealed record NinjaEvaluationDayDetail(NinjaEvaluationDay? Source, IReadOnlyList<NinjaEvaluationDay> Riders);
public sealed record NinjaEvaluationFile(byte[] Content, string ContentType, string FileName);
public sealed record NinjaSnapshotRequest(NinjaEvaluationQuery Query, string Reason, bool Freeze = false,
    Guid? SupersedesId = null, string? ExpectedInputRevision = null);
public sealed record NinjaSnapshotResponse(Guid Id, bool Frozen, Guid? SupersedesId, DateTimeOffset CreatedAtUtc,
    string Reason, NinjaEvaluationReport Report);
public sealed record NinjaEvaluationPolicyResponse(string Version, string MonthBasis, decimal AttendanceThreshold,
    decimal HoursPerFullDay, decimal AttendanceReduction, string Rounding,
    NinjaSuggestionRates Bike, NinjaSuggestionRates Car, IReadOnlyList<string> UnavailableConditions);
public sealed record NinjaPerformanceImportOptions(string Mode = "Daily", DateOnly? ReportDate = null,
    string Encoding = "utf-8", string Delimiter = ",", string HoursUnit = "DecimalHours", string DateFormat = "yyyy-MM-dd");
public sealed record NinjaPerformancePreviewRow(int RowNumber, string CaptainId, DateOnly Date,
    Guid? AccountId, int Orders, decimal Hours, string Status, IReadOnlyList<string> Issues);
public sealed record NinjaPerformancePreviewResponse(Guid PreviewId, DateTimeOffset ExpiresAtUtc,
    string ContentHash, string InputRevision, bool CanConfirm, IReadOnlyList<NinjaPerformancePreviewRow> Rows);
public sealed record NinjaPerformanceConfirmation(Guid? ArtifactId, int ImportedRows, int SkippedRows,
    IReadOnlyList<NinjaImportResponse> Days);

public interface INinjaEvaluationService
{
    Task<Result<NinjaEvaluationPolicyResponse>> PolicyAsync(CancellationToken ct = default);
    Task<Result<NinjaEvaluationReport>> EvaluateAsync(NinjaEvaluationQuery query, CancellationToken ct = default);
    Task<Result<NinjaEvaluationDayDetail>> DayAsync(Guid accountId, DateOnly businessDate, CancellationToken ct = default);
    Task<Result<NinjaEvaluationContextResponse>> ContextAsync(Guid accountId, CancellationToken ct = default);
    Task<Result<NinjaEvaluationContextResponse>> SetContextAsync(Guid accountId, NinjaEvaluationContextRequest request, CancellationToken ct = default);
    Task<Result<NinjaResponsibilityResponse>> ResponsibilityAsync(Guid accountId, int year, int month, CancellationToken ct = default);
    Task<Result<NinjaResponsibilityResponse>> SetResponsibilityAsync(Guid accountId, NinjaResponsibilityRequest request, CancellationToken ct = default);
    Task<Result<NinjaEvaluationFile>> ExportAsync(NinjaEvaluationQuery query, CancellationToken ct = default);
    Task<Result<NinjaSnapshotResponse>> SnapshotAsync(NinjaSnapshotRequest request, CancellationToken ct = default);
    Task<Result<NinjaSnapshotResponse>> GetSnapshotAsync(Guid id, CancellationToken ct = default);
}

public interface INinjaPerformanceImportService
{
    Task<Result<NinjaEvaluationFile>> TemplateAsync(string mode, CancellationToken ct = default);
    Task<Result<NinjaPerformancePreviewResponse>> PreviewAsync(string fileName, byte[] content, NinjaPerformanceImportOptions options, CancellationToken ct = default);
    Task<Result<NinjaPerformanceConfirmation>> ConfirmAsync(Guid previewId, string fileName, byte[] content, CancellationToken ct = default);
}
