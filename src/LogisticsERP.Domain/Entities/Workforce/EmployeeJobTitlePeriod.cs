using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class EmployeeJobTitlePeriod : TemporalPeriodEntity
{
    public Guid EmployeeId { get; set; }
    public Guid JobTitleId { get; set; }
    public Guid OperationalWorkTypeId { get; set; }
    public Guid OperatingCityId { get; set; }
    public string? Reason { get; set; }
    public Guid ChangedByUserId { get; set; }
}
