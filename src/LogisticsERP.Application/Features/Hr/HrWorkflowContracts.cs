using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public sealed record LeaveTypeUpsertRequest(string Code, string NameAr, string NameEn, string? DescriptionAr,
    string? DescriptionEn, bool RequiresBalance, bool RequiresHrDocuments, bool RequiresExitReentryVisa,
    int? MaximumCalendarDays, string Status, string? RowVersion);
public sealed record LeaveTypeResponse(Guid Id, string Code, string NameAr, string NameEn, string? DescriptionAr,
    string? DescriptionEn, bool RequiresBalance, bool RequiresHrDocuments, bool RequiresExitReentryVisa,
    int? MaximumCalendarDays, string Status, string RowVersion);

public sealed record LeaveWorkflowStepRequest(string StepKey, int Sequence, string NameAr, string NameEn,
    string RequiredPermissionKey, string ScopeSource, bool AllowsReturnForChanges, bool RequiresCommentOnApproval,
    int? TargetResponseHours);
public sealed record LeaveWorkflowUpsertRequest(string Code, string NameAr, string NameEn, int Version,
    Guid? LeaveTypeId, string? RelationshipType, bool? AppliesToRider, Guid? ClientPlatformId, int Priority,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Status, IReadOnlyList<LeaveWorkflowStepRequest> Steps,
    string? RowVersion);
public sealed record LeaveWorkflowStepResponse(Guid Id, string StepKey, int Sequence, string NameAr, string NameEn,
    string RequiredPermissionKey, string ScopeSource, bool AllowsReturnForChanges, bool RequiresCommentOnApproval,
    int? TargetResponseHours);
public sealed record LeaveWorkflowResponse(Guid Id, string Code, string NameAr, string NameEn, int Version,
    Guid? LeaveTypeId, string? RelationshipType, bool? AppliesToRider, Guid? ClientPlatformId, int Priority,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Status, IReadOnlyList<LeaveWorkflowStepResponse> Steps,
    string RowVersion);

public sealed record LeaveRequestUpsertRequest(Guid EmployeeId, Guid LeaveTypeId, DateOnly StartDate,
    DateOnly EndDate, DateOnly ExpectedReturnDate, string Reason, string? DestinationCountryCode,
    string? ContactPhoneDuringLeave, string? EmergencyContactName, string? EmergencyContactPhone,
    Guid? RelatedClientContractId, string? Notes, string? RowVersion);
public sealed record LeaveTransitionRequest(string Action, string Comment, string? RowVersion);
public sealed record LeaveRequestResponse(Guid Id, string RequestNumber, Guid EmployeeId, string EmployeeNameAr,
    Guid LeaveTypeId, string LeaveTypeNameAr, DateOnly StartDate, DateOnly EndDate, DateOnly ExpectedReturnDate,
    int CalendarDays, string Reason, string Status, string HrStatus, Guid? ApprovalWorkflowId,
    string? CurrentApprovalStepKey, int? CurrentApprovalStepSequence, DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ApprovedAtUtc, DateTimeOffset? ActivatedAtUtc, DateTimeOffset? CompletedAtUtc,
    string? RejectionReason, string? CancellationReason, Guid? RelatedClientContractId, string? Notes,
    string RowVersion);

public sealed record LeaveDateChangeCreateRequest(DateOnly RequestedStartDate, DateOnly RequestedEndDate, string Reason);
public sealed record LeaveChangeResolveRequest(bool Approve, string ResolutionReason, string RowVersion);
public sealed record LeaveDateChangeResponse(
    Guid Id,
    Guid LeaveRequestId,
    DateOnly PreviousStartDate,
    DateOnly PreviousEndDate,
    DateOnly RequestedStartDate,
    DateOnly RequestedEndDate,
    string Reason,
    string Status,
    Guid RequestedByUserId,
    DateTimeOffset RequestedAtUtc,
    Guid? ResolvedByUserId,
    DateTimeOffset? ResolvedAtUtc,
    string? ResolutionReason,
    string RowVersion);

public sealed record LeaveCancellationCreateRequest(string Reason);
public sealed record LeaveCancellationResponse(
    Guid Id,
    Guid LeaveRequestId,
    string Reason,
    string? PreviousLeaveStatus,
    string Status,
    Guid RequestedByUserId,
    DateTimeOffset RequestedAtUtc,
    Guid? ResolvedByUserId,
    DateTimeOffset? ResolvedAtUtc,
    string? ResolutionReason,
    string RowVersion);

public sealed record AbsenceCaseUpsertRequest(Guid EmployeeId, DateOnly AbsenceDate, string CurrentPath,
    DateOnly? ReportedToAuthoritiesDate, string? AuthorityReportReference, DateOnly? ExitOrOutageDate,
    string? ExitVisaNumber, DateOnly RemovalDeadline, string? Notes, string? RowVersion);
public sealed record AbsenceCaseTransitionRequest(string Status, string Reason, string? ResolutionCode,
    string? ResolutionNotes, string RowVersion);
public sealed record AbsenceCaseResponse(Guid Id, string CaseNumber, Guid EmployeeId, string EmployeeNameAr,
    DateOnly AbsenceDate, string CurrentPath, string Status, DateOnly? ReportedToAuthoritiesDate,
    string? AuthorityReportReference, DateOnly? ExitOrOutageDate, string? ExitVisaNumber,
    DateOnly RemovalDeadline, string? Notes, DateTimeOffset? ResolvedAtUtc, string? ResolutionCode,
    string? ResolutionNotes, DateTimeOffset? ClosedAtUtc, string RowVersion);

public sealed record EmployeeStatusChangeCreateRequest(Guid EmployeeId, string RequestedStatus,
    DateOnly EffectiveFrom, string Reason);
public sealed record EmployeeStatusChangeResolveRequest(bool Approve, string ResolutionReason, string RowVersion);
public sealed record EmployeeStatusChangeResponse(Guid Id, string RequestNumber, Guid EmployeeId,
    string EmployeeNameAr, string FromStatus, string RequestedStatus, DateOnly EffectiveFrom, string Reason,
    string Status, DateTimeOffset RequestedAtUtc, Guid? ResolvedByUserId, DateTimeOffset? ResolvedAtUtc,
    string? ResolutionReason, Guid? ResultingStatusPeriodId, string RowVersion);

public interface IHrWorkflowService
{
    Task<Result<IReadOnlyList<LeaveTypeResponse>>> GetLeaveTypesAsync(CancellationToken cancellationToken = default);
    Task<Result<LeaveTypeResponse>> UpsertLeaveTypeAsync(Guid? id, LeaveTypeUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<LeaveWorkflowResponse>>> GetLeaveWorkflowsAsync(CancellationToken cancellationToken = default);
    Task<Result<LeaveWorkflowResponse>> UpsertLeaveWorkflowAsync(Guid? id, LeaveWorkflowUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<LeaveRequestResponse>>> GetLeaveRequestsAsync(Guid? employeeId, CancellationToken cancellationToken = default);
    Task<Result<LeaveRequestResponse>> UpsertLeaveRequestAsync(Guid? id, LeaveRequestUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<LeaveRequestResponse>> TransitionLeaveAsync(Guid id, LeaveTransitionRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<LeaveDateChangeResponse>>> GetLeaveDateChangesAsync(Guid leaveRequestId, CancellationToken cancellationToken = default);
    Task<Result<LeaveDateChangeResponse>> RequestLeaveDateChangeAsync(Guid leaveRequestId, LeaveDateChangeCreateRequest request, CancellationToken cancellationToken = default);
    Task<Result<LeaveDateChangeResponse>> ResolveLeaveDateChangeAsync(Guid leaveRequestId, Guid changeId, LeaveChangeResolveRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<LeaveCancellationResponse>>> GetLeaveCancellationsAsync(Guid leaveRequestId, CancellationToken cancellationToken = default);
    Task<Result<LeaveCancellationResponse>> RequestLeaveCancellationAsync(Guid leaveRequestId, LeaveCancellationCreateRequest request, CancellationToken cancellationToken = default);
    Task<Result<LeaveCancellationResponse>> ResolveLeaveCancellationAsync(Guid leaveRequestId, Guid cancellationId, LeaveChangeResolveRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<AbsenceCaseResponse>>> GetAbsenceCasesAsync(Guid? employeeId, CancellationToken cancellationToken = default);
    Task<Result<AbsenceCaseResponse>> UpsertAbsenceCaseAsync(Guid? id, AbsenceCaseUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<AbsenceCaseResponse>> TransitionAbsenceCaseAsync(Guid id, AbsenceCaseTransitionRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EmployeeStatusChangeResponse>>> GetStatusChangeRequestsAsync(Guid? employeeId, CancellationToken cancellationToken = default);
    Task<Result<EmployeeStatusChangeResponse>> CreateStatusChangeRequestAsync(EmployeeStatusChangeCreateRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeStatusChangeResponse>> ResolveStatusChangeRequestAsync(Guid id, EmployeeStatusChangeResolveRequest request, CancellationToken cancellationToken = default);
}
