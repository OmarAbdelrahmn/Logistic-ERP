using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class LeaveDateChangeRequest : AuditableEntity
{
    public Guid LeaveRequestId { get; set; }
    public DateOnly PreviousStartDate { get; set; }
    public DateOnly PreviousEndDate { get; set; }
    public DateOnly RequestedStartDate { get; set; }
    public DateOnly RequestedEndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveChangeRequestStatus Status { get; set; } = LeaveChangeRequestStatus.Pending;
    public Guid RequestedByUserId { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public string? ResolutionReason { get; set; }
}
