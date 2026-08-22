using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class EmployeeResidencyPermit : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid SponsorId { get; set; }
    public Guid ResidencyProfessionId { get; set; }
    public byte[] PermitNumberCiphertext { get; set; } = [];
    public string PermitNumberLookupHash { get; set; } = string.Empty;
    public string PermitNumberLastFour { get; set; } = string.Empty;
    public DateOnly? IssueDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public ResidencyPermitStatus Status { get; set; } = ResidencyPermitStatus.PendingIssuance;
    public Guid? PreviousPermitId { get; set; }
    public bool IsCurrent { get; set; }
    public Guid? EmployeeDocumentId { get; set; }
    public string? Notes { get; set; }
}
