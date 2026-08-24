using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class RiderProfile : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public TShirtSize? TShirtSize { get; set; }
    public string? OperationalNotes { get; set; }
}
