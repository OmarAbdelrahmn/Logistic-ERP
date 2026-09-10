using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicles/{vehicleId:guid}")]
public sealed class VehicleComplianceController(IFleetService service) : ControllerBase
{
    [HttpGet("{type:regex(^(registrations|insurance-policies|inspections|operation-cards)$)}")]
    [RequirePermission(PermissionKeys.Fleet.ComplianceRead)]
    public async Task<IActionResult> Get(Guid vehicleId, string type, CancellationToken cancellationToken)
    {
        var result = await service.GetComplianceAsync(vehicleId, type, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("registrations")]
    [RequirePermission(PermissionKeys.Fleet.ComplianceManage)]
    public async Task<IActionResult> Registration(Guid vehicleId, [FromBody] VehicleRegistrationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RenewRegistrationAsync(vehicleId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("insurance-policies")]
    [RequirePermission(PermissionKeys.Fleet.ComplianceManage)]
    public async Task<IActionResult> Insurance(Guid vehicleId, [FromBody] VehicleInsuranceRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RenewInsuranceAsync(vehicleId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("inspections")]
    [RequirePermission(PermissionKeys.Fleet.ComplianceManage)]
    public async Task<IActionResult> Inspection(Guid vehicleId, [FromBody] VehicleInspectionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RenewInspectionAsync(vehicleId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("operation-cards")]
    [RequirePermission(PermissionKeys.Fleet.ComplianceManage)]
    public async Task<IActionResult> OperationCard(Guid vehicleId, [FromBody] VehicleOperationCardRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RenewOperationCardAsync(vehicleId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

[ApiController]
[Route("api/vehicle-compliance")]
public sealed class VehicleComplianceDueController(IFleetService service) : ControllerBase
{
    [HttpGet("due")]
    [RequirePermission(PermissionKeys.Fleet.ComplianceRead)]
    public async Task<IActionResult> Due([FromQuery] DateOnly? checkDate, CancellationToken cancellationToken)
    {
        var result = await service.GetComplianceDueAsync(checkDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
