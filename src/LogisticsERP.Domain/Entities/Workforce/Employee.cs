using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class Employee : AuditableEntity
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string NormalizedNameAr { get; set; } = string.Empty;
    public string NormalizedNameEn { get; set; } = string.Empty;
    public string PrimaryPhone { get; set; } = string.Empty;
    public EmployeeStatus CurrentStatus { get; set; } = EmployeeStatus.Draft;
    public EmployeeRelationshipType? CurrentRelationshipType { get; set; }
    public string? Notes { get; set; }
}
