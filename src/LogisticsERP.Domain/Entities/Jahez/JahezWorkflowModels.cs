using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Jahez;

public enum JahezOperationMode { Settle = 1, SettleAndWithdraw = 2, Withdraw = 3 }
public enum JahezOperationStatus { Ready = 1, UnderReview = 2, Executed = 3 }
public enum JahezOperationPaymentStatus { None = 1, Paid = 2, Transferred = 3, Deferred = 4 }
public enum JahezMoneyDirection { None = 0, Receipt = 1, Disbursement = 2 }
public enum JahezMoneyFund { Cashbox = 1, PetroApp = 2, Bank = 3 }
public enum JahezMoneyPurpose { Settlement = 1, Fuel = 2, AccountFee = 3, TransferredDebtRepayment = 4 }
public enum JahezAdjustmentTarget { Settlement = 1, AdministrativeDebt = 2 }

public sealed class JahezOperation : AuditableEntity
{
    public Guid HandoverId { get; set; }
    public JahezOperationMode Mode { get; set; }
    public JahezOperationStatus Status { get; set; } = JahezOperationStatus.Ready;
    public JahezOperationPaymentStatus PaymentStatus { get; set; } = JahezOperationPaymentStatus.None;
    public DateOnly ThroughDate { get; set; }
    public DateTimeOffset EffectiveAtUtc { get; set; }
    public decimal Net { get; set; }
    public decimal Commission { get; set; }
    public decimal CalculatedCommission { get; set; }
    public decimal Remaining { get; set; }
    public string CalculationJson { get; set; } = "[]";
    public JahezMoneyFund? Fund { get; set; }
    public JahezMoneyPurpose Purpose { get; set; } = JahezMoneyPurpose.Settlement;
    public bool TransferToRider { get; set; }
    public string? PaymentReference { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ApprovedRequestId { get; set; }
    public DateTimeOffset? ExecutedAtUtc { get; set; }
    public Guid? ExecutedByUserId { get; set; }
}

// A single journal for actual receipts/disbursements, including migrated receipts.
// Cash/bank payout methods and references permit audited corrections; financial facts stay immutable.
public sealed class JahezMoneyMovement : HistoryEntity
{
    public Guid HandoverId { get; set; }
    public Guid RiderProfileId { get; set; }
    public Guid? OperationId { get; set; }
    public Guid? TransferredDebtId { get; set; }
    public JahezMoneyFund Fund { get; set; }
    public JahezMoneyDirection Direction { get; set; }
    public JahezMoneyPurpose Purpose { get; set; }
    public decimal Amount { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

// Explicit approved obligations, separate from informational platform debt.
public sealed class JahezTransferredDebt : AuditableEntity
{
    public Guid OperationId { get; set; }
    public Guid RiderProfileId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset? PaidAtUtc { get; set; }
    public Guid? PaidByUserId { get; set; }
}
