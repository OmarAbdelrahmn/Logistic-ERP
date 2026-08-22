using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class LeaveRequest : AuditableEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly ExpectedReturnDate { get; set; }
    public int CalendarDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? DestinationCountryCode { get; set; }
    public string? ContactPhoneDuringLeave { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public LeaveWorkflowStatus Status { get; set; } = LeaveWorkflowStatus.Draft;
    public Guid? ApprovalWorkflowId { get; set; }
    public int? ApprovalWorkflowVersion { get; set; }
    public string? ApprovalWorkflowSnapshotJson { get; set; }
    public string? CurrentApprovalStepKey { get; set; }
    public int? CurrentApprovalStepSequence { get; set; }
    public LeaveHrStatus HrStatus { get; set; } = LeaveHrStatus.NotRequired;
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }
    public Guid? RelatedClientContractId { get; set; }
    public string? Notes { get; set; }
}
