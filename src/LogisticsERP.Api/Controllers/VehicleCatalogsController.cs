using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicle-catalogs")]
public sealed class VehicleCatalogsController(IFleetService service) : ControllerBase
{
    [HttpGet("manufacturers")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    public async Task<IActionResult> Manufacturers(CancellationToken cancellationToken)
    {
        var result = await service.GetManufacturersAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("manufacturers")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesManage)]
    public async Task<IActionResult> CreateManufacturer([FromBody] VehicleManufacturerRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertManufacturerAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("manufacturers/{id:guid}")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesManage)]
    public async Task<IActionResult> UpdateManufacturer(Guid id, [FromBody] VehicleManufacturerRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertManufacturerAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("models")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    public async Task<IActionResult> Models([FromQuery] Guid? manufacturerId, CancellationToken cancellationToken)
    {
        var result = await service.GetModelsAsync(manufacturerId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("models")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesManage)]
    public async Task<IActionResult> CreateModel([FromBody] VehicleModelRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertModelAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("models/{id:guid}")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesManage)]
    public async Task<IActionResult> UpdateModel(Guid id, [FromBody] VehicleModelRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertModelAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

}
