using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class EmployeeStatusPeriod : HistoryEntity
{
    public Guid EmployeeId { get; set; }
    public EmployeeStatus Status { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? ReasonCode { get; set; }
    public string? Reason { get; set; }
    public Guid ChangedByUserId { get; set; }
}
