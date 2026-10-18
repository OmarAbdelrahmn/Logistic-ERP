using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Entities.Jahez;

namespace LogisticsERP.Application.Features.Jahez;

public sealed record JahezOperationCreate(Guid HandoverId, JahezOperationMode Mode, DateOnly ThroughDate,
    DateTimeOffset EffectiveAtUtc, decimal Net, string Reason, JahezMoneyFund? Fund = null,
    JahezMoneyPurpose Purpose = JahezMoneyPurpose.Settlement, bool TransferToRider = false, string? PaymentReference = null);
public sealed record JahezOperationPreview(decimal Net, decimal Commission, decimal Remaining,
    JahezMoneyDirection Direction, IReadOnlyList<JahezMoneyFund> AllowedFunds, bool WithdrawsAccount,
    bool CommissionComplete, IReadOnlyList<string> Problems, string CalculationJson);
public sealed record JahezOperationUpdate(JahezOperationCreate Values, string RowVersion);
public sealed record JahezOperationPaymentMethodUpdate(JahezMoneyFund Fund, string PaymentReference,
    string Reason, string RowVersion);
public sealed record JahezWorkflowRequestCreate(Guid HandoverId, JahezApprovalKind Kind, string Reason,
    Guid? OperationId = null, Guid? TargetAccountId = null, decimal WaiverAmount = 0,
    DateOnly? FromDate = null, DateOnly? ToDate = null, DateTimeOffset? EffectiveAtUtc = null,
    JahezAdjustmentTarget? AdjustmentTarget = null, decimal? ProposedNet = null,
    decimal? ProposedCommission = null, decimal? DebtAdjustment = null);
public sealed record JahezWorkflowDecision(bool Approve, string Reason, decimal? ApprovedNet = null,
    decimal? ApprovedCommission = null, decimal? ApprovedDebtAdjustment = null, bool UseOriginalValues = false);
public sealed record JahezWorkflowApproval(JahezApprovalRequest Request, IReadOnlyList<JahezApprovalDecision> Decisions);
public sealed record JahezMoneyRecord(JahezMoneyFund Fund, string Reference, string Reason,
    JahezMoneyPurpose Purpose = JahezMoneyPurpose.TransferredDebtRepayment);
public sealed record JahezFeeRecord(Guid HandoverId, decimal Amount, JahezMoneyFund Fund, string Reference, string Reason);
public sealed record JahezAccountStatusUpdate(string Status, string Reason, string RowVersion);
public sealed record JahezAccountPerson(Guid EmployeeId, Guid? RiderProfileId, string NameAr, string? NameEn,
    string? IqamaNo, string? Mobile, Guid? SponsorId);
public sealed record JahezMoneyMovementResponse(Guid Id, Guid HandoverId, Guid RiderProfileId, Guid? OperationId,
    Guid? TransferredDebtId, JahezMoneyFund Fund, JahezMoneyDirection Direction, JahezMoneyPurpose Purpose,
    decimal Amount, Guid ActorUserId, DateTimeOffset OccurredAtUtc, string Reference, string Reason,
    DateTimeOffset CreatedAtUtc, Guid? CreatedByUserId, JahezAccountPerson? UserRider, JahezAccountPerson? OwnerRider,
    string? SponsorNameAr, Guid? CashboxHandoverId, bool IsHandedToAccountant,
    Guid? AccountId, string? ExternalAccountId, decimal? Net, decimal? Commission, decimal? CalculatedCommission)
{
    public string? PlatformAccountId => ExternalAccountId;
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
}
public sealed record JahezAccountRow(Guid AccountId, string Code, string ExternalAccountId, string? UserName,
    Guid OperatingCityId, string CityNameAr, Guid? SponsorId, string? SponsorNameAr, string Status,
    string? StatusReason, bool IsAssigned, bool HasPassword, JahezAccountPerson? Owner,
    JahezAccountPerson? ActualRider, Guid? CurrentAssignmentId, Guid? CurrentHandoverId,
    DateOnly? AcquisitionDate, DateOnly? StartDate, DateOnly? EndDate, string? OwnershipNotes,
    string? OperationalNotes, int? YesterdayOrders, DateOnly? LastOrderDate, int? LastOrderCount, string RowVersion);
public sealed record JahezAccountsPage(IReadOnlyList<JahezAccountRow> Items, int Page, int PageSize, int TotalCount);
public sealed record JahezSponsorStatistics(Guid SponsorId, string NameAr, int Accounts, int AssignedAccounts,
    int ActualRiders, int SponsoredRiders, int SponsoredRidersUsingOtherSponsorsAccounts);
public sealed record JahezAccountStatistics(DateOnly Yesterday, int TotalAccounts, int ActiveAccounts,
    int UnassignedAccounts, int BlockedAccounts, int MissingPasswordAccounts, int? YesterdayOrders,
    int AccountsWithYesterdayData, IReadOnlyList<JahezSponsorStatistics> Sponsors);
public sealed record JahezAccountDetails(JahezAccountRow Account, JahezPage<JahezHandoverResponse> Handovers,
    JahezPage<JahezOperation> Operations, JahezPage<JahezApprovalRequest> Requests,
    JahezPage<JahezAccountFee> Fees, JahezPage<JahezDispatchReportRow> Orders,
    JahezPage<JahezLedgerEntry> Ledger);
public sealed record JahezFundBalance(JahezMoneyFund Fund, decimal Receipts, decimal Disbursements, decimal Balance);
public sealed record JahezCashboxHandoverCreate(DateOnly BusinessDate, string Reason);
public sealed record JahezCashboxHandoverConfirm(decimal CashboxAmount, decimal PetroAppAmount, decimal BankAmount,
    string Reason, string RowVersion);
public sealed record JahezCashboxHandoverCancel(string Reason, string RowVersion);

public interface IJahezWorkflowService
{
    Task<Result<JahezHandoverResponse>> HandoverAsync(string key, JahezHandoverRequest request, CancellationToken ct = default);
    Task<Result<JahezEarningsStatement>> RecordEarningsAsync(string key, JahezEarningsRequest request, CancellationToken ct = default);
    Task<Result<JahezImportPreview>> UploadAsync(string key, JahezImportCreateRequest request, CancellationToken ct = default);
    Task<Result<JahezImportPreview>> PreviewImportAsync(Guid id, CancellationToken ct = default);
    Task<Result<JahezImportPreview>> CommitImportAsync(string key, Guid id, JahezImportCommitRequest request, CancellationToken ct = default);
    Task<Result<JahezImportFileResponse>> GetImportFileAsync(Guid id, CancellationToken ct = default);
    Task<Result<JahezPage<JahezImportBatch>>> ImportBatchesAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezPage<JahezAdministrativeDebt>>> AdministrativeDebtsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezAccountsPage>> AccountsV2Async(int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezAccountDetails>> AccountDetailsV2Async(Guid id, int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezAccountStatistics>> StatisticsV2Async(CancellationToken ct = default);
    Task<Result<JahezOperationPreview>> PreviewOperationAsync(JahezOperationCreate request, CancellationToken ct = default);
    Task<Result<JahezOperation>> CreateOperationAsync(string key, JahezOperationCreate request, CancellationToken ct = default);
    Task<Result<JahezOperation>> ExecuteOperationAsync(string key, Guid id, CancellationToken ct = default);
    Task<Result<JahezOperation>> UpdateOperationAsync(string key, Guid id, JahezOperationUpdate request, CancellationToken ct = default);
    Task<Result<JahezOperation>> UpdateOperationPaymentMethodAsync(string key, Guid id, JahezOperationPaymentMethodUpdate request, CancellationToken ct = default);
    Task<Result<JahezPage<JahezOperation>>> OperationsV2Async(int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezWorkflowApproval>> CreateWorkflowRequestAsync(string key, JahezWorkflowRequestCreate request, CancellationToken ct = default);
    Task<Result<JahezWorkflowApproval>> DecideWorkflowRequestAsync(string key, Guid id, JahezWorkflowDecision request, CancellationToken ct = default);
    Task<Result<JahezPage<JahezApprovalRequest>>> RequestsV2Async(int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezWorkflowApproval>> RequestV2Async(Guid id, CancellationToken ct = default);
    Task<Result<JahezTransferredDebt>> RepayTransferredDebtAsync(string key, Guid id, JahezMoneyRecord request, CancellationToken ct = default);
    Task<Result<JahezPage<JahezTransferredDebt>>> TransferredDebtsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezMoneyMovement>> RecordFeeV2Async(string key, JahezFeeRecord request, CancellationToken ct = default);
    Task<Result<JahezPage<JahezMoneyMovementResponse>>> MovementsV2Async(int page, int pageSize,
        DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<JahezFundBalance>>> FundsV2Async(CancellationToken ct = default);
    Task<Result<JahezPage<JahezCashboxHandover>>> CashboxHandoversV2Async(int page, int pageSize, CancellationToken ct = default);
    Task<Result<JahezCashboxHandover>> CashboxHandoverV2Async(Guid id, CancellationToken ct = default);
    Task<Result<JahezCashboxHandover>> SubmitCashboxHandoverV2Async(string key, JahezCashboxHandoverCreate request, CancellationToken ct = default);
    Task<Result<JahezCashboxHandover>> ConfirmCashboxHandoverV2Async(string key, Guid id, JahezCashboxHandoverConfirm request, CancellationToken ct = default);
    Task<Result<JahezCashboxHandover>> CancelCashboxHandoverV2Async(string key, Guid id, JahezCashboxHandoverCancel request, CancellationToken ct = default);
    Task<Result<JahezAccountRow>> UpdateAccountStatusV2Async(string key, Guid id, JahezAccountStatusUpdate request, CancellationToken ct = default);
}

public static class JahezWorkflowPermissions
{
    public static IReadOnlyList<string> All { get; } = [Withdraw, Settle, FeesRecord, StatusUpdate, Receive, Disburse,
        DebtAdjust, Repay, FeeRequest, FeeApprove, SwitchRequest, SwitchApprove, TransferRequest, TransferApprove,
        CommissionRequest, CommissionApprove, AdjustmentRequest, AdjustmentApprove];
    public const string Withdraw = "jahez.withdraw";
    public const string Settle = "jahez.settle";
    public const string FeesRecord = "jahez.fees.record";
    public const string StatusUpdate = "jahez.accounts.status.update";
    public const string Receive = "jahez.money.receive";
    public const string Disburse = "jahez.money.disburse";
    public const string DebtAdjust = "jahez.administrative-debt.adjust";
    public const string Repay = "jahez.transferred-debt.repay";
    public const string FeeRequest = "jahez.requests.fee.create";
    public const string FeeApprove = "jahez.requests.fee.approve";
    public const string SwitchRequest = "jahez.requests.switch.create";
    public const string SwitchApprove = "jahez.requests.switch.approve";
    public const string TransferRequest = "jahez.requests.transfer.create";
    public const string TransferApprove = "jahez.requests.transfer.approve";
    public const string CommissionRequest = "jahez.requests.commission.create";
    public const string CommissionApprove = "jahez.requests.commission.approve";
    public const string AdjustmentRequest = "jahez.requests.adjustment.create";
    public const string AdjustmentApprove = "jahez.requests.adjustment.approve";
    public static string Request(JahezApprovalKind kind) => kind switch
    {
        JahezApprovalKind.FeeException => FeeRequest, JahezApprovalKind.FreeSwitch => SwitchRequest,
        JahezApprovalKind.ResetAccount => TransferRequest, JahezApprovalKind.PercentageCommission => CommissionRequest,
        JahezApprovalKind.Adjustment => AdjustmentRequest, _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public static string Approve(JahezApprovalKind kind) => kind switch
    {
        JahezApprovalKind.FeeException => FeeApprove, JahezApprovalKind.FreeSwitch => SwitchApprove,
        JahezApprovalKind.ResetAccount => TransferApprove, JahezApprovalKind.PercentageCommission => CommissionApprove,
        JahezApprovalKind.Adjustment => AdjustmentApprove, _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
