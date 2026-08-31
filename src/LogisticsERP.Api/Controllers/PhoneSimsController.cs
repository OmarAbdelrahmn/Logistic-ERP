using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Telecom;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/phone-sims")]
public sealed class PhoneSimsController(IPhoneSimService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsRead)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] Guid? responsibleEmployeeId,
        [FromQuery] Guid? riderProfileId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetAllAsync(
            search,
            status,
            responsibleEmployeeId,
            riderProfileId,
            page,
            pageSize,
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsManage)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePhoneSimRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : result.ToProblem(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsManage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePhoneSimRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPatch("{id:guid}/responsible-employee")]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsManage)]
    public async Task<IActionResult> ChangeResponsibleEmployee(
        Guid id,
        [FromBody] ChangePhoneSimResponsibleEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ChangeResponsibleEmployeeAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPatch("{id:guid}/status")]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsManage)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangePhoneSimStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ChangeStatusAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPatch("{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsManage)]
    public async Task<IActionResult> Archive(
        Guid id,
        [FromBody] ArchivePhoneSimRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ArchiveAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/responsibility-history")]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsRead)]
    public async Task<IActionResult> GetResponsibilityHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.GetResponsibilityHistoryAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/assignments")]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsRead)]
    public async Task<IActionResult> GetAssignments(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAssignmentsAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/assignments")]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsManage)]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignPhoneSimRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.AssignAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/assignments/{assignmentId:guid}/close")]
    [RequirePermission(PermissionKeys.Operations.PhoneSimsManage)]
    public async Task<IActionResult> CloseAssignment(
        Guid id,
        Guid assignmentId,
        [FromBody] ClosePhoneSimAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CloseAssignmentAsync(id, assignmentId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
