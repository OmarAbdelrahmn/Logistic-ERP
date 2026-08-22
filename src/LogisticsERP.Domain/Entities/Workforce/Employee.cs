using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class Employee : AuditableEntity
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string? FullNameEn { get; set; }
    public string NormalizedNameAr { get; set; } = string.Empty;
    public string? NormalizedNameEn { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? NationalityCountryCode { get; set; }
    public DateOnly? HireDate { get; set; }
    public EmployeeStatus CurrentStatus { get; set; } = EmployeeStatus.Draft;
    public EmployeeRelationshipType? CurrentRelationshipType { get; set; }
    public string? Notes { get; set; }
}
