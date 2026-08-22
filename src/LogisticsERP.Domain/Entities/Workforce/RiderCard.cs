using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class RiderCard : AuditableEntity
{
    public Guid RiderProfileId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string NormalizedCardNumber { get; set; } = string.Empty;
    public RiderCardType CardType { get; set; }
    public CardValidityCycle ValidityCycle { get; set; } = CardValidityCycle.Annual;
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public RiderCardStatus Status { get; set; } = RiderCardStatus.Draft;
    public Guid? PreviousCardId { get; set; }
    public bool IsCurrent { get; set; }
    public Guid? EmployeeDocumentId { get; set; }
    public string? Notes { get; set; }
}
