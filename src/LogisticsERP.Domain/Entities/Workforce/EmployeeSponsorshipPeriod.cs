using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class EmployeeSponsorshipPeriod : TemporalPeriodEntity
{
    public Guid EmployeeId { get; set; }
    public Guid SponsorId { get; set; }
    public SponsorshipStatus Status { get; set; } = SponsorshipStatus.Pending;
    public string? Reason { get; set; }
    public string? SourceReference { get; set; }
    public Guid ChangedByUserId { get; set; }
}
