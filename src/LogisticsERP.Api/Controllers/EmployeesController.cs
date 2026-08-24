using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(IWorkforceService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Workforce.EmployeesRead)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetEmployeesAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{employeeId:guid}")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesRead)]
    public async Task<IActionResult> Get(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await service.GetEmployeeAsync(employeeId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Workforce.EmployeesCreate)]
    public async Task<IActionResult> Create([FromBody] EmployeeUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateEmployeeAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { employeeId = result.Value!.Employee.Id }, result.Value)
            : result.ToProblem(HttpContext);
    }

    [HttpPut("{employeeId:guid}")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesUpdate)]
    public async Task<IActionResult> Update(Guid employeeId, [FromBody] EmployeeUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateEmployeeAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{employeeId:guid}/status-transitions")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesUpdate)]
    public async Task<IActionResult> ChangeStatus(Guid employeeId, [FromBody] ChangeEmployeeStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ChangeStatusAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{employeeId:guid}/role-transitions")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesUpdate)]
    public async Task<IActionResult> ChangeRole(Guid employeeId, [FromBody] ChangeEmployeeRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ChangeRoleAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{employeeId:guid}/work-history")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesRead)]
    public async Task<IActionResult> GetWorkHistory(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await service.GetWorkHistoryAsync(employeeId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPatch("{employeeId:guid}/archive")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesArchive)]
    public async Task<IActionResult> Archive(Guid employeeId, [FromBody] ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveEmployeeAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}
