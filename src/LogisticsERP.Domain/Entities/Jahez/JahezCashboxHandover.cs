using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Jahez;

public enum JahezCashboxHandoverStatus { Pending = 1, Confirmed = 2, Cancelled = 3 }

public sealed class JahezCashboxHandover : AuditableEntity
{
    public int TreasuryVersion { get; set; } = 2;
    public string? DocumentNumber { get; set; }
    public string? RequestedByNameAr { get; set; }
    public string? AccountantNameAr { get; set; }
    public DateOnly BusinessDate { get; set; }
    public JahezCashboxHandoverStatus Status { get; set; } = JahezCashboxHandoverStatus.Pending;
    public Guid RequestedByUserId { get; set; }
    public decimal CashboxAmount { get; set; }
    public decimal PetroAppAmount { get; set; }
    public decimal BankAmount { get; set; }
    public int MovementCount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? AccountantUserId { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public string? ConfirmationReason { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
}

// Exact immutable membership; cancelled handovers retain their evidence but release their movements.
public sealed class JahezCashboxHandoverMovement : HistoryEntity
{
    public Guid CashboxHandoverId { get; set; }
    public Guid MoneyMovementId { get; set; }
}
