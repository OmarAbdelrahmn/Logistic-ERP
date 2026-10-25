using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Jahez;

// An immutable decision and payout, once per account session.
public sealed class JahezAccountFeeRefund : HistoryEntity
{
    public Guid HandoverId { get; set; }
    public Guid AccountFeeId { get; set; }
    public Guid MoneyMovementId { get; set; }
    public decimal GrossFeeRefund { get; set; }
    public decimal CancelledUnpaidFee { get; set; }
    public bool WaiveCommission { get; set; }
    public decimal CalculatedCommission { get; set; }
    public decimal DeductedCommission { get; set; }
    public decimal RefundAmount { get; set; }
    public string CalculationJson { get; set; } = "[]";
    public Guid ActorUserId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
