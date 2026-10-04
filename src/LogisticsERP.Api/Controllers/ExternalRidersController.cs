using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/external-riders")]
public sealed class ExternalRidersController(IWorkforceService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Workforce.ExternalRidersRead)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetExternalRidersAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{employeeId:guid}")]
    [RequirePermission(PermissionKeys.Workforce.ExternalRidersRead)]
    public async Task<IActionResult> Get(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await service.GetExternalRiderAsync(employeeId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Workforce.ExternalRidersCreate)]
    public async Task<IActionResult> Create([FromBody] CreateExternalRiderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateExternalRiderAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { employeeId = result.Value!.EmployeeId }, result.Value)
            : result.ToProblem(HttpContext);
    }

    [HttpPut("{employeeId:guid}")]
    [RequirePermission(PermissionKeys.Workforce.ExternalRidersUpdate)]
    public async Task<IActionResult> Update(
        Guid employeeId,
        [FromBody] UpdateExternalRiderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateExternalRiderAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpDelete("{employeeId:guid}")]
    [RequirePermission(PermissionKeys.Workforce.ExternalRidersDelete)]
    public async Task<IActionResult> Delete(
        Guid employeeId, [FromBody] ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveExternalRiderAsync(employeeId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}
