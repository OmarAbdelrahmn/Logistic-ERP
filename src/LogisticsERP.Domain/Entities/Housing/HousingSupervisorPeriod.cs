using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Housing;

public sealed class HousingSupervisorPeriod : HistoryEntity
{
    public Guid HousingId { get; set; }
    public Guid SupervisorEmployeeId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? AssignmentReason { get; set; }
    public string? EndReason { get; set; }
    public Guid AssignedByUserId { get; set; }
    public Guid? EndedByUserId { get; set; }
}
