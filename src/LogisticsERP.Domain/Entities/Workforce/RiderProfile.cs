using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class RiderProfile : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public RiderStatus Status { get; set; } = RiderStatus.Draft;
    public DateOnly? RiderStartDate { get; set; }
    public DateOnly? RiderEndDate { get; set; }
    public Guid? PreferredCityId { get; set; }
    public Guid? LicenseDocumentId { get; set; }
    public string? OperationalNotes { get; set; }
}
