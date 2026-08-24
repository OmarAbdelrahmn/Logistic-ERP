using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Documents;

public sealed class EmployeeDocument : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid DocumentTypeId { get; set; }
    public string? DocumentNumber { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Active;
    public string? Notes { get; set; }
    public Guid? CurrentVersionId { get; set; }
}
