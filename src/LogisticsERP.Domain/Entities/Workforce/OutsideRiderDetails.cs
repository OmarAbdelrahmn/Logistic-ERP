using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class OutsideRiderDetails : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public string? AlternateContactName { get; set; }
    public string? AlternateContactPhone { get; set; }
    public string? EngagementReference { get; set; }
    public string? EngagementNotes { get; set; }
}
