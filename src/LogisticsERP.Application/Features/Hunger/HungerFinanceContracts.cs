using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using System.Text.Json.Serialization;
using LogisticsERP.Application.Abstractions.Files;

namespace LogisticsERP.Application.Features.Hunger;

public sealed record HungerFinanceSetup(Guid PlatformId, Guid SupervisorEmployeeId, Guid SponsorId,
    Guid CustodySubAccountId, Guid CollectionsSubAccountId, Guid WalletSubAccountId, Guid RiderClearingSubAccountId,
    string EvidenceReference, bool IsActive, string? RowVersion = null);
public sealed record HungerMoneyRequest(Guid PlatformId, Guid SupervisorEmployeeId, string Kind, decimal Amount,
    Guid? AccountId, Guid? RiderProfileId, Guid? CounterSubAccountId, Guid? RelatedEventId, DateOnly BusinessDate,
    string IdempotencyKey, string EvidenceReference, decimal? SourceWalletBalance = null,
    string? DestinationType = null, Guid? DestinationOwnerId = null, string? TransferReference = null,
    DateOnly? TransferDate = null, bool AgainstReservedRights = false,
    // Omitted from JSON when null so request hashes of keys issued before CR-005 still match on replay.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? SupersedesEventId = null,
    // Server-issued screenshots of the wallet before and after a top-up (finance/evidence).
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? EvidenceBeforeId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? EvidenceAfterId = null,
    // Withdrawals: Stc | Cashbox | Bank, and one server-issued wallet screenshot.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DestinationChannel = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? WalletEvidenceId = null);
// Accountant decision on a custody funding request. Amount defaults to the requested amount; the source is chosen per request.
public sealed record HungerFundingDecision(decimal? Amount = null, Guid? SourceSubAccountId = null, string? Note = null);
// Difference = PostingBalance - ExpectedBalance. Anything but zero means the custody ledger and the events disagree.
public sealed record HungerCustodySettlement(decimal FundingDelivered, decimal TopUpAdvance, decimal TopUpCollected, decimal CancelledTopUps,
    decimal ExpectedBalance, decimal PostingBalance, decimal Difference, bool Balanced, bool ExceptionApproved);
public sealed record HungerMoneyExecution(Guid? EvidenceBeforeId = null, Guid? EvidenceAfterId = null);
// Capacity: Accountant | Owner. Recorded with the confirming user on the audit event.
public sealed record HungerOwnerCollectionConfirmation(string IdempotencyKey, DateOnly BusinessDate, string EvidenceReference, string Capacity);
public sealed record HungerWithdrawalAction(string IdempotencyKey, DateOnly BusinessDate, string EvidenceReference);
public sealed record HungerMoneyResult(Guid EventId, string Kind, decimal Amount, decimal RiderBalanceChange, string Status = "Posted",
    string ApprovalStatus = "NotRequired");
public sealed record HungerSealRequest(IReadOnlyDictionary<string, string>? Accounts = null, string? Note = null);
public sealed record HungerSealRejection(string Reason);
public sealed record HungerSealPage(IReadOnlyList<HungerFinancialEventItem> Items, int TotalCount, int Page, int PageSize);
public sealed record HungerEvidenceUpload(Guid PlatformId, Guid AccountId, string Purpose, DateOnly ObservedOn);
public sealed record HungerEvidenceItem(Guid EvidenceFileId, string Purpose, DateOnly ObservedOn, string Link);
public sealed record HungerExceptionRequest(Guid PlatformId, string Kind, Guid? AccountId, Guid? SupervisorEmployeeId,
    decimal Amount, string Reason, string IdempotencyKey, string? EvidenceReference = null);
public sealed record HungerExceptionDecision(string? Note = null);
public sealed record HungerExceptionItem(Guid ExceptionId, string Kind, string Status, Guid? AccountId, Guid? SupervisorEmployeeId,
    decimal Amount, string Reason, Guid RequestedByUserId, Guid? DecidedByUserId, DateTimeOffset? DecidedAtUtc, Guid? ConsumedByEventId);
public sealed record HungerExceptionPage(IReadOnlyList<HungerExceptionItem> Items, int TotalCount, int Page, int PageSize);
public sealed record HungerRemittanceSource(Guid SourceEventId, decimal Amount);
public sealed record HungerRemittanceRequest(Guid PlatformId, Guid SupervisorEmployeeId, IReadOnlyList<HungerRemittanceSource> Sources,
    string IdempotencyKey, DateOnly BusinessDate, string EvidenceReference);
public sealed record HungerRemittanceConfirmation(Guid DestinationSubAccountId, IReadOnlyList<HungerRemittanceSource>? Sources,
    string IdempotencyKey, DateOnly BusinessDate, string EvidenceReference, string? DestinationType = null, Guid? DestinationOwnerId = null,
    IReadOnlyList<Guid>? SelectedSourceIds = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DestinationChannel = null);
public sealed record HungerRemittanceAction(string IdempotencyKey, DateOnly BusinessDate, string EvidenceReference, string Reason);
public sealed record HungerPendingSource(Guid SourceEventId, decimal Amount, decimal ConfirmedAmount, decimal RemainingAmount);
public sealed record HungerPendingBatch(Guid BatchId, string Status, decimal Amount, decimal ConfirmedAmount, decimal RemainingAmount, IReadOnlyList<HungerPendingSource> Sources);
public sealed record HungerPendingCollections(IReadOnlyList<HungerPendingBatch> Batches, IReadOnlyList<HungerPendingSource> Sources, int TotalBatches, int TotalSources, int Page, int PageSize);
public sealed record HungerWalletReviewRequest(string Action, Guid RiderProfileId, Guid SupervisorEmployeeId,
    DateOnly BusinessDate,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal MobileBalance,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal? ClosingBalance,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal ProvenAdjustments,
    string WalletEvidence, string? ClosingEvidence, string? AdjustmentEvidence,
    AssignSimplePlatformAccountRequest? Assignment = null,
    ReleaseSimplePlatformAccountRequest? Return = null,
    // ClosingBalance, ProvenAdjustments and their evidence are ignored since CR-005: the server derives both from settlement documents.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? ExceptionApprovalId = null);
public sealed record HungerWalletReviewResponse(Guid ReviewId, decimal? CurrentPeriodBalance, decimal RestrictedRights,
    decimal? AttributedBalance, decimal? RiderBalanceChange, DateTimeOffset ExpiresAtUtc, decimal? ClosingBalance, bool CanConfirm,
    string? WalletEvidence = null, decimal ProvenAdjustments = 0, string? ClosingSource = null, bool RequiresException = false);
public sealed record HungerWalletConfirmation(Guid EventId, Guid AssignmentId, string Action, decimal RiderBalanceChange);
public sealed record HungerRiderSwapRequest(HungerWalletReviewRequest Return, AssignSimplePlatformAccountRequest Handover,
    Guid? HandoverExceptionApprovalId = null);
public sealed record HungerRiderSwapReview(Guid SwapId, HungerWalletReviewResponse Return, HungerWalletReviewResponse Handover);
public sealed record HungerRiderSwapConfirmation(Guid SwapId, HungerWalletConfirmation Return, HungerWalletConfirmation Handover);
public sealed record HungerFinancialEventItem(Guid Id, string Kind, Guid? AccountId, Guid? RiderProfileId,
    Guid SupervisorEmployeeId, decimal Amount, decimal RiderBalanceChange, DateOnly BusinessDate, string EvidenceReference, string Status = "Posted",
    string ApprovalStatus = "NotRequired", DateTimeOffset? ApprovedAtUtc = null, Guid? SupersedesEventId = null, string? RejectionReason = null);
public sealed record HungerFinanceReport(IReadOnlyList<HungerFinancialEventItem> Events, int TotalCount,
    decimal CustodyBalance, decimal PendingRemittances, int Page, int PageSize);
public interface IHungerFinanceService
{
    Task<Result<HungerWalletPage>> WalletsAsync(HungerWalletFilter filter, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<Result<HungerWalletHistory>> WalletHistoryAsync(Guid accountId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<Result<HungerFile>> ExportWalletsAsync(HungerWalletFilter filter, string format = "xlsx", CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> ConfirmWithdrawalArrivalAsync(Guid withdrawalId, HungerWithdrawalAction request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> CancelWithdrawalAsync(Guid withdrawalId, HungerWithdrawalAction request, CancellationToken ct = default);
    Task<Result> ConfigureFinanceAsync(HungerFinanceSetup request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> PostMoneyAsync(HungerMoneyRequest request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> RequestMoneyAsync(HungerMoneyRequest request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> ExecuteMoneyRequestAsync(Guid requestId, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> ExecuteMoneyRequestAsync(Guid requestId, HungerMoneyExecution? execution, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> DecideFundingAsync(Guid requestId, bool approve, HungerFundingDecision request, CancellationToken ct = default);
    Task<Result<HungerCustodySettlement>> CustodySettlementAsync(Guid platformId, Guid supervisorEmployeeId, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> ConfirmOwnerCollectionAsync(Guid receiptId, HungerOwnerCollectionConfirmation request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> CreateRemittanceDraftAsync(HungerRemittanceRequest request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> SubmitRemittanceDraftAsync(Guid remittanceId, HungerRemittanceAction request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> CancelRemittanceAsync(Guid remittanceId, HungerRemittanceAction request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> ReverseRemittanceConfirmationAsync(Guid confirmationId, HungerRemittanceAction request, CancellationToken ct = default);
    Task<Result<HungerPendingCollections>> PendingCollectionsAsync(Guid platformId, Guid supervisorEmployeeId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> SubmitRemittanceAsync(HungerRemittanceRequest request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> ConfirmRemittanceAsync(Guid remittanceId, HungerRemittanceConfirmation request, CancellationToken ct = default);
    Task<Result<HungerWalletReviewResponse>> ReviewWalletAsync(Guid accountId, HungerWalletReviewRequest request, CancellationToken ct = default);
    Task<Result<HungerWalletReviewResponse>> ReviewWalletWithImageAsync(Guid accountId, HungerWalletReviewRequest request, PrivateFileUpload? image, CancellationToken ct = default);
    Task<Result<HungerWalletConfirmation>> ConfirmWalletAsync(Guid reviewId, CancellationToken ct = default);
    Task<Result<HungerRiderSwapReview>> ReviewRiderSwapAsync(Guid accountId, HungerRiderSwapRequest request, CancellationToken ct = default);
    Task<Result<HungerRiderSwapReview>> ReviewRiderSwapWithImageAsync(Guid accountId, HungerRiderSwapRequest request, PrivateFileUpload? image, CancellationToken ct = default);
    Task<Result<HungerRiderSwapConfirmation>> ConfirmRiderSwapAsync(Guid swapId, CancellationToken ct = default);
    Task<Result<HungerFinanceReport>> FinanceReportAsync(Guid platformId, Guid supervisorEmployeeId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> SealEventAsync(Guid eventId, HungerSealRequest request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> RejectEventAsync(Guid eventId, HungerSealRejection request, CancellationToken ct = default);
    Task<Result<HungerSealPage>> SealQueueAsync(Guid platformId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<Result<HungerEvidenceItem>> UploadEvidenceAsync(HungerEvidenceUpload request, PrivateFileUpload? image, CancellationToken ct = default);
    Task<Result<HungerExceptionItem>> RequestExceptionAsync(HungerExceptionRequest request, CancellationToken ct = default);
    Task<Result<HungerExceptionItem>> DecideExceptionAsync(Guid exceptionId, bool approve, HungerExceptionDecision request, CancellationToken ct = default);
    Task<Result<HungerExceptionPage>> ExceptionsAsync(Guid platformId, string? status, int page, int pageSize, CancellationToken ct = default);
}

public sealed record HungerWalletFilter
{
    public HungerAccountFilter Accounts { get; init; } = new();
    public decimal? BalanceFrom { get; init; }
    public decimal? BalanceThrough { get; init; }
    public decimal? BalanceEqual { get; init; }
    public string? ReceiptState { get; init; }
    public bool? AlertOnly { get; init; }
}
public sealed record HungerWalletItem(Guid AccountId, string ExternalAccountId, decimal? MobileBalance,
    DateOnly? ObservedOn, string? Evidence, decimal? CurrentPeriodBalance, decimal RestrictedRights,
    decimal? AttributedBalance, string ReceiptState, decimal PendingArrival, decimal ConfirmedReceipts, decimal PendingReceipts,
    // None | WalletNotZero | CustodyProposalOpen. Clears by itself; there is nothing to dismiss.
    string Alert = "None");
public sealed record HungerWalletPage(IReadOnlyList<HungerWalletItem> Items, int TotalCount, int Page, int PageSize);
public sealed record HungerWalletHistory(IReadOnlyList<HungerFinancialEventItem> Events,
    IReadOnlyList<HungerWalletObservation> Observations, int TotalEvents, int TotalObservations, int Page, int PageSize);
public sealed record HungerWalletObservation(Guid ReviewId, DateOnly ObservedOn, decimal MobileBalance, string Evidence, string Status);
