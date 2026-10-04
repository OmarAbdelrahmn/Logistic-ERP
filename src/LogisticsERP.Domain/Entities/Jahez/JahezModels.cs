using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Jahez;

public enum JahezApprovalKind { FeeException = 1, FreeSwitch = 2, PercentageCommission = 3, ResetAccount = 4 }
public enum JahezApprovalStatus { Pending = 1, Approved = 2, Rejected = 3, Cancelled = 4 }
public enum JahezLedgerBucket { AccountFees = 1, PlatformDebt = 2, Commission = 3 }
public enum JahezLedgerKind { Charge = 1, Payment = 2, Adjustment = 3, Transfer = 4, OpeningBalance = 5 }
public enum JahezCashboxSection { AccountFees = 1, Settlements = 2 }
public enum JahezCashboxHandoverStatus { Pending = 1, AccountantConfirmed = 2, Approved = 3, Rejected = 4 }
public enum JahezImportKind { Transactions = 1, DailyDispatches = 2 }

public sealed class JahezAccountHandover : AuditableEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public Guid RiderProfileId { get; set; }
    public Guid RiderClientAssignmentId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
    public DateOnly CommissionStartsOn { get; set; }
    public DateOnly? CommissionPostedThrough { get; set; }
    public DateTimeOffset? LastSettlementPaymentAtUtc { get; set; }
    public bool IsLegacy { get; set; }
    public bool DebtTransferred { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class JahezAccountFee : AuditableEntity
{
    public Guid HandoverId { get; set; }
    public decimal Amount { get; set; } = 200m;
    public decimal WaivedAmount { get; set; }
    public Guid? ApprovalRequestId { get; set; }
}

public sealed class JahezApprovalRequest : AuditableEntity
{
    public Guid HandoverId { get; set; }
    public JahezApprovalKind Kind { get; set; }
    public JahezApprovalStatus Status { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? TargetAccountId { get; set; }
    public decimal WaiverAmount { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public DateTimeOffset? EffectiveAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ExternalResetReference { get; set; }
}

public sealed class JahezApprovalDecision : HistoryEntity
{
    public Guid RequestId { get; set; }
    public Guid ActorUserId { get; set; }
    public JahezApprovalStatus Status { get; set; }
    public DateTimeOffset DecidedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class JahezCommissionPolicyPeriod : HistoryEntity
{
    public Guid HandoverId { get; set; }
    public Guid ApprovalRequestId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal Rate { get; set; } = 0.15m;
}

public sealed class JahezEarningsStatement : HistoryEntity
{
    public Guid HandoverId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal TotalDeliveryPrice { get; set; }
    public decimal TotalPenalties { get; set; }
    public decimal TotalCashAmount { get; set; }
    public decimal TotalDriverDebit { get; set; }
    public decimal TotalServiceDeduction { get; set; }
    public decimal TotalDriverCredit { get; set; }
    public decimal TotalBonuses { get; set; }
    public decimal TotalTips { get; set; }
    public decimal TotalFreeOrders { get; set; }
    public Guid? SupersedesId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class JahezImportBatch : AuditableEntity
{
    public JahezImportKind Kind { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset? CommittedAtUtc { get; set; }
    public Guid? ReplacesBatchId { get; set; }
    public string? CorrectionReason { get; set; }
}

public sealed class JahezImportFile : HistoryEntity
{
    public Guid BatchId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}

public sealed class JahezImportRow : HistoryEntity
{
    public Guid FileId { get; set; }
    public int RowNumber { get; set; }
    public string DriverId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateOnly? ToDate { get; set; }
    public decimal DeliveryPrice { get; set; }
    public decimal CashAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal DriverAdjustment { get; set; }
    public int? Dispatches { get; set; }
    public string RawValuesJson { get; set; } = string.Empty;
    public string? ParseError { get; set; }
}

public sealed class JahezTransaction : HistoryEntity
{
    public Guid BatchId { get; set; }
    public Guid ImportRowId { get; set; }
    public Guid HandoverId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public decimal DeliveryPrice { get; set; }
    public decimal CashAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal DriverAdjustment { get; set; }
}

public sealed class JahezDailyDispatch : HistoryEntity
{
    public Guid BatchId { get; set; }
    public Guid ImportRowId { get; set; }
    public Guid HandoverId { get; set; }
    public DateOnly Date { get; set; }
    public int Count { get; set; }
    public string? AllocationReason { get; set; }
}

public sealed class JahezRiderSettlement : HistoryEntity
{
    public Guid HandoverId { get; set; }
    public DateOnly ThroughDate { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public Guid CollectedByUserId { get; set; }
    public decimal FeePayment { get; set; }
    public decimal DebtPayment { get; set; }
    public decimal CommissionPayment { get; set; }
    public bool CountsAsSettlement { get; set; }
    public string Reason { get; set; } = string.Empty;
}

// Signed receivable: charges are positive, payments/credits are negative.
// Transfer entries carry zero: obligations stay on their original rider and handover.
public sealed class JahezLedgerEntry : HistoryEntity
{
    public Guid HandoverId { get; set; }
    public JahezLedgerBucket Bucket { get; set; }
    public JahezLedgerKind Kind { get; set; }
    public decimal Amount { get; set; }
    public Guid SourceId { get; set; }
    public Guid? ReversesEntryId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateOnly? FromDate { get; set; }
    public DateOnly? ThroughDate { get; set; }
    public string? CalculationJson { get; set; }
}

public sealed class JahezCashboxEntry : AuditableEntity
{
    public Guid SettlementId { get; set; }
    public Guid HandoverId { get; set; }
    public JahezCashboxSection Section { get; set; }
    public decimal Amount { get; set; }
    public Guid CollectedByUserId { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public Guid? CashboxHandoverId { get; set; }
}

public sealed class JahezCashboxHandover : AuditableEntity
{
    public DateOnly BusinessDate { get; set; }
    public JahezCashboxHandoverStatus Status { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal SettlementAmount { get; set; }
    public decimal? AccountantFeeAmount { get; set; }
    public decimal? AccountantSettlementAmount { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? AccountantUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ConfirmationReason { get; set; }
    public string? DecisionReason { get; set; }
}

public sealed class JahezReminderState : AuditableEntity
{
    public Guid HandoverId { get; set; }
    public DateTimeOffset AnchorAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
}

public sealed class JahezCommandReceipt : HistoryEntity
{
    public Guid ActorUserId { get; set; }
    public string CommandKey { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string ResultJson { get; set; } = string.Empty;
}
