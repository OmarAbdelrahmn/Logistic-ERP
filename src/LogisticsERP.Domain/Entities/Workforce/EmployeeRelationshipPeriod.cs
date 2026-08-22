using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class EmployeeRelationshipPeriod : TemporalPeriodEntity
{
    public Guid EmployeeId { get; set; }
    public EmployeeRelationshipType RelationshipType { get; set; }
    public string? ReasonCode { get; set; }
    public string? Reason { get; set; }
    public string? SourceReference { get; set; }
    public Guid ChangedByUserId { get; set; }
}
