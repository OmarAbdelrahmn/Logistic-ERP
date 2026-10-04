using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Entities.Jahez;

namespace LogisticsERP.Application.Features.Jahez;

public sealed record JahezHandoverRequest(Guid AccountId, Guid RiderProfileId, DateTimeOffset EffectiveAtUtc, string Reason,
    decimal InitialFeePayment, Guid? FeeApprovalRequestId = null);
public sealed record JahezLegacyAdoptionRequest(Guid AssignmentId, DateOnly FinancialStartOn, decimal OpeningDebt,
    decimal OpeningFees, decimal OpeningCommission, string Reason);
public sealed record JahezCloseRequest(DateTimeOffset EffectiveAtUtc, string Reason);
public sealed record JahezApprovalCreateRequest(Guid HandoverId, JahezApprovalKind Kind, string Reason,
    Guid? TargetAccountId = null, decimal WaiverAmount = 0m, DateOnly? FromDate = null, DateOnly? ToDate = null,
    DateTimeOffset? EffectiveAtUtc = null, string? ExternalResetReference = null);
public sealed record JahezDecisionRequest(bool Approve, string Reason);
public sealed record JahezEarningsRequest(Guid HandoverId, DateOnly FromDate, DateOnly ToDate,
    decimal? TotalDeliveryPrice, decimal? TotalPenalties, decimal? TotalCashAmount, decimal? TotalDriverDebit,
    decimal? TotalServiceDeduction, decimal? TotalDriverCredit, decimal? TotalBonuses, decimal? TotalTips,
    decimal? TotalFreeOrders, string Reason, Guid? SupersedesId = null);
public sealed record JahezPaymentRequest(Guid HandoverId, DateOnly ThroughDate, decimal FeePayment,
    decimal DebtPayment, decimal CommissionPayment, bool CountsAsSettlement, string Reason);
public sealed record JahezLedgerAdjustmentRequest(Guid HandoverId, JahezLedgerBucket Bucket, decimal Amount,
    string Reason, Guid? ReversesEntryId = null);
public sealed record JahezCashboxCreateRequest(DateOnly BusinessDate, string Reason);
public sealed record JahezAccountantConfirmRequest(decimal FeeAmount, decimal SettlementAmount, string Reason);
public sealed record JahezUploadFile(string FileName, byte[] Content);
public sealed record JahezImportCreateRequest(JahezImportKind Kind, IReadOnlyList<JahezUploadFile> Files,
    Guid? ReplacesBatchId = null, string? CorrectionReason = null);
public sealed record JahezDispatchAllocation(Guid RowId, Guid HandoverId, int Count, string Reason);
public sealed record JahezImportCommitRequest(IReadOnlyList<JahezDispatchAllocation>? Allocations = null);
public sealed record JahezPage<T>(IReadOnlyList<T> Items, int Page, int PageSize);
public sealed record JahezHandoverResponse(Guid Id, Guid AccountId, string ExternalAccountId, Guid RiderProfileId,
    Guid AssignmentId, DateTimeOffset StartedAtUtc, DateTimeOffset? EndedAtUtc, DateOnly CommissionStartsOn,
    DateOnly? CommissionPostedThrough, DateTimeOffset? LastSettlementPaymentAtUtc, bool IsLegacy, bool DebtTransferred);
public sealed record JahezBalanceResponse(Guid HandoverId, Guid AccountId, string ExternalAccountId, Guid RiderProfileId,
    DateOnly ThroughDate, decimal Fees, decimal PlatformDebt, decimal PostedCommission, decimal UnpostedCommission,
    decimal TotalReceivable, bool CommissionComplete, IReadOnlyList<string> Problems,
    DateTimeOffset ReminderAnchorAtUtc, int DaysSinceSettlementPayment, bool IsOverdue, bool DebtTransferred,
    DateTimeOffset? LatestTransactionAtUtc);
public sealed record JahezApprovalResponse(JahezApprovalRequest Request, IReadOnlyList<JahezApprovalDecision> Decisions);
public sealed record JahezImportIssue(Guid RowId, string FileName, int RowNumber, string Code, string Description);
public sealed record JahezImportRowPreview(Guid RowId, string FileName, int RowNumber, string DriverId,
    DateTimeOffset OccurredAtUtc, Guid? AccountId, Guid? HandoverId, Guid? RiderProfileId, decimal NetAmount, int? Dispatches);
public sealed record JahezImportPreview(Guid BatchId, JahezImportKind Kind, bool Committed,
    IReadOnlyList<JahezImportRowPreview> Rows, IReadOnlyList<JahezImportIssue> Issues, IReadOnlyList<JahezImportFileMetadata>? Files = null,
    IReadOnlyList<JahezImportAccountSummary>? Accounts = null);
public sealed record JahezImportAccountSummary(Guid? AccountId, string DriverId, DateOnly FromDate, DateOnly ToDate,
    int ValidRowCount, decimal NetAmount, decimal PlatformDebtChange, long? Dispatches, bool HasIssues);
public sealed record JahezImportFileMetadata(Guid Id, string FileName);
public sealed record JahezImportFileResponse(Guid Id, string FileName, byte[] Content);
public sealed record JahezCashboxBalance(decimal Fees, decimal Settlements, decimal ReservedFees,
    decimal ReservedSettlements, decimal AvailableFees, decimal AvailableSettlements);
public sealed record JahezDispatchReportRow(Guid AccountId, string ExternalAccountId, Guid RiderProfileId,
    Guid HandoverId, DateOnly Date, int Count);

public interface IJahezService
{
    Task<Result<JahezHandoverResponse>> HandoverAsync(string key, JahezHandoverRequest request, CancellationToken ct = default);
    Task<Result<JahezHandoverResponse>> AdoptLegacyAsync(string key, JahezLegacyAdoptionRequest request, CancellationToken ct = default);
    Task<Result<JahezHandoverResponse>> CloseAsync(string key, Guid id, JahezCloseRequest request, CancellationToken ct = default);
    Task<Result<JahezPage<JahezHandoverResponse>>> GetHandoversAsync(Guid? accountId, Guid? riderId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezBalanceResponse>> GetBalanceAsync(Guid id, DateOnly through, CancellationToken ct = default);
    Task<Result<JahezPage<JahezBalanceResponse>>> GetDebtsAsync(Guid? riderId, bool overdueOnly, int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezPage<JahezLedgerEntry>>> GetLedgerAsync(Guid? riderId, Guid? handoverId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezAccountFee>> GetFeeAsync(Guid handoverId, CancellationToken ct = default);
    Task<Result<JahezPage<JahezRiderSettlement>>> GetSettlementsAsync(Guid handoverId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezPage<JahezEarningsStatement>>> GetEarningsAsync(Guid handoverId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezPage<JahezCommissionPolicyPeriod>>> GetPoliciesAsync(Guid handoverId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezApprovalResponse>> RequestApprovalAsync(string key, JahezApprovalCreateRequest request, CancellationToken ct = default);
    Task<Result<JahezApprovalResponse>> DecideApprovalAsync(string key, Guid id, JahezDecisionRequest request, CancellationToken ct = default);
    Task<Result<JahezApprovalResponse>> CancelApprovalAsync(string key, Guid id, string reason, CancellationToken ct = default);
    Task<Result<JahezPage<JahezApprovalRequest>>> GetApprovalsAsync(JahezApprovalStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezApprovalResponse>> GetApprovalAsync(Guid id, CancellationToken ct = default);
    Task<Result<JahezEarningsStatement>> RecordEarningsAsync(string key, JahezEarningsRequest request, CancellationToken ct = default);
    Task<Result<JahezRiderSettlement>> PayAsync(string key, JahezPaymentRequest request, CancellationToken ct = default);
    Task<Result<JahezLedgerEntry>> AdjustAsync(string key, JahezLedgerAdjustmentRequest request, CancellationToken ct = default);
    Task<Result<JahezImportPreview>> UploadAsync(string key, JahezImportCreateRequest request, CancellationToken ct = default);
    Task<Result<JahezImportPreview>> PreviewImportAsync(Guid id, CancellationToken ct = default);
    Task<Result<JahezImportPreview>> CommitImportAsync(string key, Guid id, JahezImportCommitRequest request, CancellationToken ct = default);
    Task<Result<JahezImportFileResponse>> GetImportFileAsync(Guid id, CancellationToken ct = default);
    Task<Result<JahezPage<JahezDispatchReportRow>>> GetDispatchesAsync(DateOnly fromDate, DateOnly toDate, Guid? riderId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezCashboxBalance>> GetCashboxAsync(CancellationToken ct = default);
    Task<Result<JahezPage<JahezCashboxEntry>>> GetCashboxEntriesAsync(Guid? cashboxHandoverId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezPage<JahezImportBatch>>> GetImportBatchesAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezCashboxHandover>> SubmitCashboxAsync(string key, JahezCashboxCreateRequest request, CancellationToken ct = default);
    Task<Result<JahezCashboxHandover>> ConfirmCashboxAsync(string key, Guid id, JahezAccountantConfirmRequest request, CancellationToken ct = default);
    Task<Result<JahezCashboxHandover>> DecideCashboxAsync(string key, Guid id, JahezDecisionRequest request, CancellationToken ct = default);
    Task<Result<JahezPage<JahezCashboxHandover>>> GetCashboxHandoversAsync(int page, int pageSize, CancellationToken ct = default);
}

public interface IJahezReminderService
{
    Task RunAsync(CancellationToken ct = default);
}

public static class JahezErrors
{
    public static OperationError Invalid(string description) => new("jahez.invalid_request", description, ErrorType.Validation);
    public static OperationError Conflict(string description) => new("jahez.conflict", description, ErrorType.Conflict);
    public static readonly OperationError NotFound = new("jahez.not_found", "السجل غير موجود.", ErrorType.NotFound);
    public static readonly OperationError Forbidden = new("jahez.forbidden", "لا تملك صلاحية العملية ضمن نطاق جاهز.", ErrorType.Forbidden);
    public static readonly OperationError UseJahezWorkflow = Conflict("استخدم إجراءات جاهز لتسليم أو إغلاق هذا الحساب مع حفظ الالتزامات المالية.");
}
