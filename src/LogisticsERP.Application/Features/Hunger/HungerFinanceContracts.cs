using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using System.Text.Json.Serialization;

namespace LogisticsERP.Application.Features.Hunger;

public sealed record HungerFinanceSetup(Guid PlatformId, Guid SupervisorEmployeeId, Guid SponsorId,
    Guid CustodySubAccountId, Guid CollectionsSubAccountId, Guid WalletSubAccountId, Guid RiderClearingSubAccountId,
    string EvidenceReference, bool IsActive, string? RowVersion = null);
public sealed record HungerMoneyRequest(Guid PlatformId, Guid SupervisorEmployeeId, string Kind, decimal Amount,
    Guid? AccountId, Guid? RiderProfileId, Guid? CounterSubAccountId, Guid? RelatedEventId, DateOnly BusinessDate,
    string IdempotencyKey, string EvidenceReference, decimal? SourceWalletBalance = null,
    string? DestinationType = null, Guid? DestinationOwnerId = null, string? TransferReference = null,
    DateOnly? TransferDate = null, bool AgainstReservedRights = false);
public sealed record HungerWithdrawalAction(string IdempotencyKey, DateOnly BusinessDate, string EvidenceReference);
public sealed record HungerMoneyResult(Guid EventId, string Kind, decimal Amount, decimal RiderBalanceChange, string Status = "Posted");
public sealed record HungerRemittanceSource(Guid SourceEventId, decimal Amount);
public sealed record HungerRemittanceRequest(Guid PlatformId, Guid SupervisorEmployeeId, IReadOnlyList<HungerRemittanceSource> Sources,
    string IdempotencyKey, DateOnly BusinessDate, string EvidenceReference);
public sealed record HungerRemittanceConfirmation(Guid DestinationSubAccountId, IReadOnlyList<HungerRemittanceSource>? Sources,
    string IdempotencyKey, DateOnly BusinessDate, string EvidenceReference, string? DestinationType = null, Guid? DestinationOwnerId = null,
    IReadOnlyList<Guid>? SelectedSourceIds = null);
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
    ReleaseSimplePlatformAccountRequest? Return = null);
public sealed record HungerWalletReviewResponse(Guid ReviewId, decimal? CurrentPeriodBalance, decimal RestrictedRights,
    decimal? AttributedBalance, decimal? RiderBalanceChange, DateTimeOffset ExpiresAtUtc, decimal? ClosingBalance, bool CanConfirm);
public sealed record HungerWalletConfirmation(Guid EventId, Guid AssignmentId, string Action, decimal RiderBalanceChange);
public sealed record HungerRiderSwapRequest(HungerWalletReviewRequest Return, AssignSimplePlatformAccountRequest Handover);
public sealed record HungerRiderSwapReview(Guid SwapId, HungerWalletReviewResponse Return, HungerWalletReviewResponse Handover);
public sealed record HungerRiderSwapConfirmation(Guid SwapId, HungerWalletConfirmation Return, HungerWalletConfirmation Handover);
public sealed record HungerFinancialEventItem(Guid Id, string Kind, Guid? AccountId, Guid? RiderProfileId,
    Guid SupervisorEmployeeId, decimal Amount, decimal RiderBalanceChange, DateOnly BusinessDate, string EvidenceReference, string Status = "Posted");
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
    Task<Result<HungerMoneyResult>> CreateRemittanceDraftAsync(HungerRemittanceRequest request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> SubmitRemittanceDraftAsync(Guid remittanceId, HungerRemittanceAction request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> CancelRemittanceAsync(Guid remittanceId, HungerRemittanceAction request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> ReverseRemittanceConfirmationAsync(Guid confirmationId, HungerRemittanceAction request, CancellationToken ct = default);
    Task<Result<HungerPendingCollections>> PendingCollectionsAsync(Guid platformId, Guid supervisorEmployeeId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> SubmitRemittanceAsync(HungerRemittanceRequest request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> ConfirmRemittanceAsync(Guid remittanceId, HungerRemittanceConfirmation request, CancellationToken ct = default);
    Task<Result<HungerWalletReviewResponse>> ReviewWalletAsync(Guid accountId, HungerWalletReviewRequest request, CancellationToken ct = default);
    Task<Result<HungerWalletConfirmation>> ConfirmWalletAsync(Guid reviewId, CancellationToken ct = default);
    Task<Result<HungerRiderSwapReview>> ReviewRiderSwapAsync(Guid accountId, HungerRiderSwapRequest request, CancellationToken ct = default);
    Task<Result<HungerRiderSwapConfirmation>> ConfirmRiderSwapAsync(Guid swapId, CancellationToken ct = default);
    Task<Result<HungerFinanceReport>> FinanceReportAsync(Guid platformId, Guid supervisorEmployeeId, int page, int pageSize, CancellationToken ct = default);
}

public sealed record HungerWalletFilter
{
    public HungerAccountFilter Accounts { get; init; } = new();
    public decimal? BalanceFrom { get; init; }
    public decimal? BalanceThrough { get; init; }
    public decimal? BalanceEqual { get; init; }
    public string? ReceiptState { get; init; }
}
public sealed record HungerWalletItem(Guid AccountId, string ExternalAccountId, decimal? MobileBalance,
    DateOnly? ObservedOn, string? Evidence, decimal? CurrentPeriodBalance, decimal RestrictedRights,
    decimal? AttributedBalance, string ReceiptState, decimal PendingArrival, decimal ConfirmedReceipts, decimal PendingReceipts);
public sealed record HungerWalletPage(IReadOnlyList<HungerWalletItem> Items, int TotalCount, int Page, int PageSize);
public sealed record HungerWalletHistory(IReadOnlyList<HungerFinancialEventItem> Events,
    IReadOnlyList<HungerWalletObservation> Observations, int TotalEvents, int TotalObservations, int Page, int PageSize);
public sealed record HungerWalletObservation(Guid ReviewId, DateOnly ObservedOn, decimal MobileBalance, string Evidence, string Status);
