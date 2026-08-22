using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class RiderHealthCard : AuditableEntity
{
    public Guid RiderProfileId { get; set; }
    public byte[] CardNumberCiphertext { get; set; } = [];
    public string CardNumberLookupHash { get; set; } = string.Empty;
    public string CardNumberLastFour { get; set; } = string.Empty;
    public string? CardType { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public RiderHealthCardStatus Status { get; set; } = RiderHealthCardStatus.Draft;
    public Guid? PreviousCardId { get; set; }
    public bool IsCurrent { get; set; }
    public Guid? EmployeeDocumentId { get; set; }
    public string? Notes { get; set; }
}
