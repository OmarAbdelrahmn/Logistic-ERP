using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class LeaveApprovalDecision : HistoryEntity
{
    public Guid LeaveRequestId { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public int StepSequence { get; set; }
    public string RequiredPermissionKey { get; set; } = string.Empty;
    public Guid DecidedByUserId { get; set; }
    public DateTimeOffset DecidedAtUtc { get; set; }
    public LeaveDecisionType Decision { get; set; }
    public LeaveWorkflowStatus FromStatus { get; set; }
    public LeaveWorkflowStatus ToStatus { get; set; }
    public string? ReturnedToStepKey { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string AuthorizationSnapshotJson { get; set; } = "{}";
}
