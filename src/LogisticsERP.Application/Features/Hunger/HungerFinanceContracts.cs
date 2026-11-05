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
    string IdempotencyKey, DateOnly BusinessDate, string EvidenceReference);
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
public sealed record HungerFinancialEventItem(Guid Id, string Kind, Guid? AccountId, Guid? RiderProfileId,
    Guid SupervisorEmployeeId, decimal Amount, decimal RiderBalanceChange, DateOnly BusinessDate, string EvidenceReference, string Status = "Posted");
public sealed record HungerFinanceReport(IReadOnlyList<HungerFinancialEventItem> Events, int TotalCount,
    decimal CustodyBalance, decimal PendingRemittances, int Page, int PageSize);
public interface IHungerFinanceService
{
    Task<Result<HungerMoneyResult>> ConfirmWithdrawalArrivalAsync(Guid withdrawalId, HungerWithdrawalAction request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> CancelWithdrawalAsync(Guid withdrawalId, HungerWithdrawalAction request, CancellationToken ct = default);
    Task<Result> ConfigureFinanceAsync(HungerFinanceSetup request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> PostMoneyAsync(HungerMoneyRequest request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> SubmitRemittanceAsync(HungerRemittanceRequest request, CancellationToken ct = default);
    Task<Result<HungerMoneyResult>> ConfirmRemittanceAsync(Guid remittanceId, HungerRemittanceConfirmation request, CancellationToken ct = default);
    Task<Result<HungerWalletReviewResponse>> ReviewWalletAsync(Guid accountId, HungerWalletReviewRequest request, CancellationToken ct = default);
    Task<Result<HungerWalletConfirmation>> ConfirmWalletAsync(Guid reviewId, CancellationToken ct = default);
    Task<Result<HungerFinanceReport>> FinanceReportAsync(Guid platformId, Guid supervisorEmployeeId, int page, int pageSize, CancellationToken ct = default);
}
