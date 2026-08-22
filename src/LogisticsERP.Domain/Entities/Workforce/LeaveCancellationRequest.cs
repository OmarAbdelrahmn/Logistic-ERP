using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class LeaveCancellationRequest : AuditableEntity
{
    public Guid LeaveRequestId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveWorkflowStatus? PreviousLeaveStatus { get; set; }
    public LeaveChangeRequestStatus Status { get; set; } = LeaveChangeRequestStatus.Pending;
    public Guid RequestedByUserId { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public string? ResolutionReason { get; set; }
}
