using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Entities.Jahez;

namespace LogisticsERP.Application.Features.Jahez;

public sealed record JahezHandoverRequest(Guid AccountId, Guid RiderProfileId, DateTimeOffset EffectiveAtUtc, string Reason,
    decimal InitialFeePayment = 0, string? PaymentReference = null, IReadOnlyList<LogisticsERP.Application.Features.Hr.PreviousDayUsageInput>? PreviousDayUsages = null);
public sealed record JahezHandoverResponse(Guid Id, Guid AccountId, string ExternalAccountId, Guid RiderProfileId,
    Guid AssignmentId, DateTimeOffset StartedAtUtc, DateTimeOffset? EndedAtUtc, DateOnly CommissionStartsOn,
    DateOnly? CommissionPostedThrough, DateTimeOffset? LastSettlementPaymentAtUtc);
public sealed record JahezEarningsRequest(Guid HandoverId, DateOnly FromDate, DateOnly ToDate,
    decimal? TotalDeliveryPrice, decimal? TotalPenalties, decimal? TotalCashAmount, decimal? TotalDriverDebit,
    decimal? TotalServiceDeduction, decimal? TotalDriverCredit, decimal? TotalBonuses, decimal? TotalTips,
    decimal? TotalFreeOrders, string Reason, Guid? SupersedesId = null);
public sealed record JahezUploadFile(string FileName, byte[] Content);
public sealed record JahezImportCreateRequest(JahezImportKind Kind, IReadOnlyList<JahezUploadFile> Files,
    Guid? ReplacesBatchId = null, string? CorrectionReason = null);
public sealed record JahezDispatchAllocation(Guid RowId, Guid HandoverId, int Count, string Reason);
public sealed record JahezImportCommitRequest(IReadOnlyList<JahezDispatchAllocation>? Allocations = null);
public sealed record JahezPage<T>(IReadOnlyList<T> Items, int Page, int PageSize);
public sealed record JahezImportIssue(Guid RowId, string FileName, int RowNumber, string Code, string Description);
public sealed record JahezImportRowPreview(Guid RowId, string FileName, int RowNumber, string DriverId,
    DateTimeOffset OccurredAtUtc, Guid? AccountId, Guid? HandoverId, Guid? RiderProfileId, decimal NetAmount, int? Dispatches);
public sealed record JahezImportPreview(Guid BatchId, JahezImportKind Kind, bool Committed,
    IReadOnlyList<JahezImportRowPreview> Rows, IReadOnlyList<JahezImportIssue> Issues, IReadOnlyList<JahezImportFileMetadata>? Files = null,
    IReadOnlyList<JahezImportAccountSummary>? Accounts = null)
{
    public IReadOnlyList<JahezDispatchAllocation> ResolvedAllocations { get; init; } = [];
}
public sealed record JahezImportAccountSummary(Guid? AccountId, string DriverId, DateOnly FromDate, DateOnly ToDate,
    int ValidRowCount, decimal NetAmount, decimal PlatformDebtChange, long? Dispatches, bool HasIssues);
public sealed record JahezImportFileMetadata(Guid Id, string FileName);
public sealed record JahezImportFileResponse(Guid Id, string FileName, byte[] Content);

public sealed record JahezDispatchReportRow(Guid AccountId, string ExternalAccountId, Guid RiderProfileId,
    Guid HandoverId, DateOnly Date, int Count);
public sealed record JahezAdministrativeDebt(Guid? HandoverId, Guid AccountId, string ExternalAccountId,
    Guid? RiderProfileId, string? RiderNameAr, decimal PlatformDebt);
public interface IJahezReminderService { Task RunAsync(CancellationToken ct = default); }

public static class JahezErrors
{
    public static OperationError Invalid(string description) => new("jahez.invalid_request", description, ErrorType.Validation);
    public static OperationError Conflict(string description) => new("jahez.conflict", description, ErrorType.Conflict);
    public static readonly OperationError NotFound = new("jahez.not_found", "السجل غير موجود.", ErrorType.NotFound);
    public static readonly OperationError Forbidden = new("jahez.forbidden", "لا تملك صلاحية العملية ضمن نطاق جاهز.", ErrorType.Forbidden);
    public static readonly OperationError UseJahezWorkflow = Conflict("استخدم إجراءات جاهز لتسليم أو إغلاق هذا الحساب مع حفظ الالتزامات المالية.");
}
