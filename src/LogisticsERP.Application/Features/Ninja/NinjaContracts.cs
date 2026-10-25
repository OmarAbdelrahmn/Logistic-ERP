using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;

namespace LogisticsERP.Application.Features.Ninja;

public sealed record NinjaAssignRequest(Guid ActualRiderProfileId, DateOnly EffectiveFrom, string PhoneNumber,
    string? NewPassword = null, string? Reason = null, bool WasBackdated = false, string? BackdatedReason = null,
    string? RiderType = null, string? PaymentModel = null);
public sealed record NinjaReturnRequest(DateOnly EffectiveTo, string PhoneNumber, string? NewPassword,
    string Reason, string RowVersion, string Status = "Ended", PlatformAccountDayUsageInput? DayUsage = null);
public sealed record NinjaCredentialRequest(string? NewPassword, string? PhoneNumber, string Reason, string RowVersion);
public sealed record NinjaPasswordNotification(string Code, string MessageAr, string MessageEn, DateTimeOffset ChangeAllowedAtUtc);
public sealed record NinjaCredentialResponse(Guid AccountId, string? PhoneNumber, bool HasPassword,
    bool PasswordChanged, DateTimeOffset? PasswordChangeAllowedAtUtc, NinjaPasswordNotification? Notification, string RowVersion);
public sealed record NinjaLifecycleResponse(SimplePlatformAssignmentResponse Assignment, NinjaCredentialResponse Credentials);
public sealed record NinjaPersonResponse(Guid EmployeeId, string NameAr, string? NameEn, string? PhoneNumber, string? IqamaNo);
public sealed record NinjaAccountStatusRequest(string Status, string RowVersion, string? Reason = null);
public sealed record NinjaAccountResponse(Guid AccountId, string CaptainId, string Status, Guid? OwnerEmployeeId,
    Guid? ActualRiderProfileId, string? PhoneNumber, bool HasPassword, DateTimeOffset? PasswordChangeAllowedAtUtc, string RowVersion,
    Guid? CurrentAssignmentId = null, string? AssignmentRowVersion = null)
{
    public NinjaPersonResponse? Owner { get; init; }
    public NinjaPersonResponse? ActualRider { get; init; }
    public string? Password { get; init; }
    public string? RiderType { get; init; }
    public string PaymentModel { get; init; } = string.Empty;
    public string? StatusReason { get; init; }
    public string Code { get; init; } = string.Empty;
    public Guid OperatingCityId { get; init; }
    public Guid? SponsorId { get; init; }
    public string? LatestSheetStatus { get; init; }
    public bool? LatestSheetSuspended { get; init; }
    public Guid? LatestSheetUploadId { get; init; }
    public DateTimeOffset? SheetUpdatedAtUtc { get; init; }
    public bool IsOperationallyBlocked { get; init; }
    public string? OperationalBlockReason { get; init; }
}
public sealed record NinjaPage<T>(IReadOnlyList<T> Items, int Page, int PageSize);
public sealed record NinjaImportIssue(int RowNumber, string? CaptainId, string Message);
public sealed record NinjaImportResponse(Guid? ImportId, DateOnly ReportDate, int ImportedRows, int SkippedRows, IReadOnlyList<NinjaImportIssue> Issues);
public sealed record NinjaPerformanceResponse(Guid Id, Guid AccountId, Guid AssignmentId, Guid RiderProfileId,
    DateOnly ReportDate, string CaptainId, int TotalOrders, decimal TotalWorkingHours);
public sealed record NinjaDeductionResponse(Guid Id, Guid AccountId, Guid AssignmentId, Guid RiderProfileId,
    DateOnly ReportDate, string CaptainId, decimal TotalCollectedAmountAtDelivery, decimal TotalDeductions);

public interface INinjaService
{
    Task<Result<NinjaAccountUpdateUploadResponse>> UploadAccountSheetAsync(string fileName, byte[] content, string? note, CancellationToken ct = default);
    Task<Result<NinjaAccountUpdatePage<NinjaAccountUpdateUploadResponse>>> GetAccountSheetUploadsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<NinjaAccountUpdateUploadResponse>> GetAccountSheetUploadAsync(Guid uploadId, CancellationToken ct = default);
    Task<Result<NinjaAccountUpdatePage<NinjaAccountUpdateRowResponse>>> GetAccountSheetRowsAsync(Guid uploadId, string? matchStatus,
        string? sheetStatus, bool problemsOnly, int page, int pageSize, CancellationToken ct = default);
    Task<Result<NinjaAccountUpdateUploadResponse>> UpdateAccountSheetNoteAsync(Guid uploadId, NinjaAccountUpdateNoteRequest request, CancellationToken ct = default);
    Task<Result<NinjaAccountComparisonResponse>> CompareAccountsAsync(string fileName, byte[] content, CancellationToken ct = default);
    Task<Result<NinjaPage<NinjaAccountResponse>>> GetAccountsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<NinjaAccountResponse>> GetAccountAsync(Guid accountId, CancellationToken ct = default);
    Task<Result<NinjaAccountResponse>> SetAccountStatusAsync(Guid accountId, NinjaAccountStatusRequest request, CancellationToken ct = default);
    Task<Result<NinjaPerformanceReport>> GetPerformanceReportAsync(DateOnly dateFrom, DateOnly dateTo, Guid? accountId, Guid? riderId, CancellationToken ct = default);
    Task<Result<NinjaPage<NinjaDailyReport>>> GetDailyReportAsync(DateOnly dateFrom, DateOnly dateTo, Guid? accountId, Guid? riderId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<NinjaPage<NinjaRiderReport>>> GetRiderReportAsync(DateOnly dateFrom, DateOnly dateTo, Guid? accountId, Guid? riderId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<NinjaLifecycleResponse>> AssignAsync(Guid accountId, NinjaAssignRequest request, CancellationToken ct = default);
    Task<Result<NinjaLifecycleResponse>> ReturnAsync(Guid accountId, NinjaReturnRequest request, CancellationToken ct = default);
    Task<Result<NinjaCredentialResponse>> UpdateCredentialsAsync(Guid accountId, NinjaCredentialRequest request, CancellationToken ct = default);
    Task<Result<NinjaImportResponse>> ImportAsync(bool deductions, DateOnly reportDate, string fileName, byte[] content, CancellationToken ct = default);
    Task<Result<NinjaPage<NinjaPerformanceResponse>>> GetPerformanceAsync(DateOnly dateFrom, DateOnly dateTo, Guid? accountId, Guid? riderId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<NinjaPage<NinjaDeductionResponse>>> GetDeductionsAsync(DateOnly dateFrom, DateOnly dateTo, Guid? accountId, Guid? riderId, int page, int pageSize, CancellationToken ct = default);
}

public static class NinjaErrors
{
    public static readonly OperationError NotFound = new("ninja.not_found", "حساب نينجا غير موجود.", ErrorType.NotFound);
    public static readonly OperationError Forbidden = new("ninja.forbidden", "لا تملك صلاحية لهذا الحساب أو العملية.", ErrorType.Forbidden);
    public static readonly OperationError UseNinjaWorkflow = new("ninja.use_workflow", "استخدم مسارات نينجا لإدارة الحالة والتكليف والهاتف ومهلة كلمة المرور.", ErrorType.Conflict);
    public static OperationError Invalid(string message) => new("ninja.invalid", message, ErrorType.Validation);
    public static readonly OperationError Conflict = new("ninja.conflict", "تعارض في البيانات؛ أعد تحميل السجل وحاول مجدداً.", ErrorType.Conflict);
}
