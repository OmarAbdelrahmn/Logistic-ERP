using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/hr-workflows")]
public sealed class HrWorkflowsController(IHrWorkflowService service) : ControllerBase
{
    [HttpGet("leave-types")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsRead)]
    public Task<IActionResult> LeaveTypes(CancellationToken cancellationToken) => ToAction(service.GetLeaveTypesAsync(cancellationToken));

    [HttpPost("leave-types")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsManage)]
    public Task<IActionResult> CreateLeaveType([FromBody] LeaveTypeUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertLeaveTypeAsync(null, request, cancellationToken));

    [HttpPut("leave-types/{id:guid}")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsManage)]
    public Task<IActionResult> UpdateLeaveType(Guid id, [FromBody] LeaveTypeUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertLeaveTypeAsync(id, request, cancellationToken));

    [HttpGet("leave-approval-workflows")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsRead)]
    public Task<IActionResult> LeaveWorkflows(CancellationToken cancellationToken) => ToAction(service.GetLeaveWorkflowsAsync(cancellationToken));

    [HttpPost("leave-approval-workflows")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsManage)]
    public Task<IActionResult> CreateLeaveWorkflow([FromBody] LeaveWorkflowUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertLeaveWorkflowAsync(null, request, cancellationToken));

    [HttpPut("leave-approval-workflows/{id:guid}")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsManage)]
    public Task<IActionResult> UpdateLeaveWorkflow(Guid id, [FromBody] LeaveWorkflowUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertLeaveWorkflowAsync(id, request, cancellationToken));

    [HttpGet("leave-requests")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsRead)]
    public Task<IActionResult> LeaveRequests([FromQuery] Guid? employeeId, CancellationToken cancellationToken) => ToAction(service.GetLeaveRequestsAsync(employeeId, cancellationToken));

    [HttpPost("leave-requests")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsManage)]
    public Task<IActionResult> CreateLeaveRequest([FromBody] LeaveRequestUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertLeaveRequestAsync(null, request, cancellationToken));

    [HttpPut("leave-requests/{id:guid}")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsManage)]
    public Task<IActionResult> UpdateLeaveRequest(Guid id, [FromBody] LeaveRequestUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertLeaveRequestAsync(id, request, cancellationToken));

    [HttpPost("leave-requests/{id:guid}/transitions")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsManage)]
    public Task<IActionResult> TransitionLeave(Guid id, [FromBody] LeaveTransitionRequest request, CancellationToken cancellationToken)
    {
        var action = request.Action.Trim().ToLowerInvariant();
        return action is "submit" or "activate" or "complete"
            ? ToAction(service.TransitionLeaveAsync(id, request, cancellationToken))
            : Task.FromResult<IActionResult>(BadRequest());
    }

    [HttpPost("leave-requests/{id:guid}/force-cancel")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsApprove)]
    public Task<IActionResult> ForceCancelLeave(Guid id, [FromBody] LeaveTransitionRequest request, CancellationToken cancellationToken) =>
        string.Equals(request.Action, "force-cancel", StringComparison.OrdinalIgnoreCase)
            ? ToAction(service.TransitionLeaveAsync(id, request, cancellationToken))
            : Task.FromResult<IActionResult>(BadRequest());

    [HttpPost("leave-requests/{id:guid}/approval-decisions")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsApprove)]
    public Task<IActionResult> DecideLeave(Guid id, [FromBody] LeaveTransitionRequest request, CancellationToken cancellationToken)
    {
        var action = request.Action.Trim().ToLowerInvariant();
        return action is "approve" or "reject" or "return"
            ? ToAction(service.TransitionLeaveAsync(id, request, cancellationToken))
            : Task.FromResult<IActionResult>(BadRequest());
    }

    [HttpGet("leave-requests/{id:guid}/date-change-requests")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsRead)]
    public Task<IActionResult> DateChanges(Guid id, CancellationToken cancellationToken) =>
        ToAction(service.GetLeaveDateChangesAsync(id, cancellationToken));

    [HttpPost("leave-requests/{id:guid}/date-change-requests")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsManage)]
    public Task<IActionResult> RequestDateChange(Guid id, [FromBody] LeaveDateChangeCreateRequest request, CancellationToken cancellationToken) =>
        ToAction(service.RequestLeaveDateChangeAsync(id, request, cancellationToken));

    [HttpPost("leave-requests/{id:guid}/date-change-requests/{changeId:guid}/resolve")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsApprove)]
    public Task<IActionResult> ResolveDateChange(Guid id, Guid changeId, [FromBody] LeaveChangeResolveRequest request, CancellationToken cancellationToken) =>
        ToAction(service.ResolveLeaveDateChangeAsync(id, changeId, request, cancellationToken));

    [HttpGet("leave-requests/{id:guid}/cancellation-requests")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsRead)]
    public Task<IActionResult> Cancellations(Guid id, CancellationToken cancellationToken) =>
        ToAction(service.GetLeaveCancellationsAsync(id, cancellationToken));

    [HttpPost("leave-requests/{id:guid}/cancellation-requests")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsManage)]
    public Task<IActionResult> RequestCancellation(Guid id, [FromBody] LeaveCancellationCreateRequest request, CancellationToken cancellationToken) =>
        ToAction(service.RequestLeaveCancellationAsync(id, request, cancellationToken));

    [HttpPost("leave-requests/{id:guid}/cancellation-requests/{cancellationId:guid}/resolve")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsApprove)]
    public Task<IActionResult> ResolveCancellation(Guid id, Guid cancellationId, [FromBody] LeaveChangeResolveRequest request, CancellationToken cancellationToken) =>
        ToAction(service.ResolveLeaveCancellationAsync(id, cancellationId, request, cancellationToken));

    [HttpGet("absence-cases")]
    [RequirePermission(PermissionKeys.Workflows.AbsenceCasesRead)]
    public Task<IActionResult> AbsenceCases([FromQuery] Guid? employeeId, CancellationToken cancellationToken) => ToAction(service.GetAbsenceCasesAsync(employeeId, cancellationToken));

    [HttpPost("absence-cases")]
    [RequirePermission(PermissionKeys.Workflows.AbsenceCasesManage)]
    public Task<IActionResult> CreateAbsenceCase([FromBody] AbsenceCaseUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertAbsenceCaseAsync(null, request, cancellationToken));

    [HttpPut("absence-cases/{id:guid}")]
    [RequirePermission(PermissionKeys.Workflows.AbsenceCasesManage)]
    public Task<IActionResult> UpdateAbsenceCase(Guid id, [FromBody] AbsenceCaseUpsertRequest request, CancellationToken cancellationToken) => ToAction(service.UpsertAbsenceCaseAsync(id, request, cancellationToken));

    [HttpPost("absence-cases/{id:guid}/transitions")]
    [RequirePermission(PermissionKeys.Workflows.AbsenceCasesManage)]
    public Task<IActionResult> TransitionAbsenceCase(Guid id, [FromBody] AbsenceCaseTransitionRequest request, CancellationToken cancellationToken) => ToAction(service.TransitionAbsenceCaseAsync(id, request, cancellationToken));

    [HttpGet("employee-status-change-requests")]
    [RequirePermission(PermissionKeys.Workflows.EmployeeStatusChangesRead)]
    public Task<IActionResult> StatusChanges([FromQuery] Guid? employeeId, CancellationToken cancellationToken) => ToAction(service.GetStatusChangeRequestsAsync(employeeId, cancellationToken));

    [HttpPost("employee-status-change-requests")]
    [RequirePermission(PermissionKeys.Workflows.EmployeeStatusChangesManage)]
    public Task<IActionResult> CreateStatusChange([FromBody] EmployeeStatusChangeCreateRequest request, CancellationToken cancellationToken) => ToAction(service.CreateStatusChangeRequestAsync(request, cancellationToken));

    [HttpPost("employee-status-change-requests/{id:guid}/resolve")]
    [RequirePermission(PermissionKeys.Workflows.EmployeeStatusChangesApprove)]
    public Task<IActionResult> ResolveStatusChange(Guid id, [FromBody] EmployeeStatusChangeResolveRequest request, CancellationToken cancellationToken) => ToAction(service.ResolveStatusChangeRequestAsync(id, request, cancellationToken));

    private async Task<IActionResult> ToAction<T>(Task<LogisticsERP.Application.Common.Results.Result<T>> task)
    {
        var result = await task;
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
