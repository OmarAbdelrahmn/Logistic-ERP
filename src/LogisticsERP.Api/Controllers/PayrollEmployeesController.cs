using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/payroll-employees")]
public sealed class PayrollEmployeesController(IPayrollEmployeeService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Workforce.EmployeesRead)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAllAsync(search, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Workforce.EmployeesCreate)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePayrollEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : result.ToProblem(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesUpdate)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePayrollEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Workforce.EmployeesArchive)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] string rowVersion,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, rowVersion, reason, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}
