using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Clients;

public sealed class RiderClientAssignment : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid RiderProfileId { get; set; }
    public Guid ClientContractId { get; set; }
    public Guid PlatformRiderAccountId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public RiderAssignmentStatus Status { get; set; } = RiderAssignmentStatus.Planned;
    public string? StartReason { get; set; }
    public string? EndReason { get; set; }
    public string? OperationalAgreementReference { get; set; }
    public string? OperationalAgreementNotes { get; set; }
    public Guid AssignedByUserId { get; set; }
    public Guid? EndedByUserId { get; set; }
    public bool WasBackdated { get; set; }
    public string? BackdatedReason { get; set; }
}
