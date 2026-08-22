using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Housing;

public sealed class HousingResidencePeriod : HistoryEntity
{
    public Guid EmployeeId { get; set; }
    public Guid HousingId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? MoveInReason { get; set; }
    public string? MoveOutReason { get; set; }
    public string? SourceReference { get; set; }
    public string? DestinationReference { get; set; }
    public bool CapacityOverrideUsed { get; set; }
    public string? CapacityOverrideReason { get; set; }
    public Guid AssignedByUserId { get; set; }
    public Guid? EndedByUserId { get; set; }
}
