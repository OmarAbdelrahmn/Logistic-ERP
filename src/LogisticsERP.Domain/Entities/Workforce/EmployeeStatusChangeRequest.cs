using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class EmployeeStatusChangeRequest : AuditableEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public EmployeeStatus FromStatus { get; set; }
    public EmployeeStatus RequestedStatus { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public string Reason { get; set; } = string.Empty;
    public EmployeeStatusChangeRequestStatus Status { get; set; } = EmployeeStatusChangeRequestStatus.Pending;
    public Guid RequestedByUserId { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public string? ResolutionReason { get; set; }
    public Guid? ResultingStatusPeriodId { get; set; }
}
