using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Maintenance;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/maintenance-locations")]
public sealed class MaintenanceLocationsController(IMaintenanceService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Maintenance.LocationsRead)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await service.GetLocationsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Maintenance.LocationsManage)]
    public async Task<IActionResult> Create([FromBody] MaintenanceLocationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertLocationAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Maintenance.LocationsManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] MaintenanceLocationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertLocationAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
