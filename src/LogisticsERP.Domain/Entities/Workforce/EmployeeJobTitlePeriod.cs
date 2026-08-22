using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class EmployeeJobTitlePeriod : HistoryEntity
{
    public Guid EmployeeId { get; set; }
    public Guid JobTitleId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? Reason { get; set; }
    public Guid ChangedByUserId { get; set; }
}
