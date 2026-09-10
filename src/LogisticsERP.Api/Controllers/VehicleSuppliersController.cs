using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicle-suppliers")]
public sealed class VehicleSuppliersController(IFleetService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await service.GetSuppliersAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetSupplierAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Fleet.VehiclesManage)]
    public async Task<IActionResult> Create([FromBody] VehicleSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertSupplierAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] VehicleSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertSupplierAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPatch("{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesManage)]
    public async Task<IActionResult> Archive(Guid id, [FromBody] ArchiveFleetRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveSupplierAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}
