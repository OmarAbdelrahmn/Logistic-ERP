using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Maintenance;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/maintenance")]
public sealed class MaintenanceController(IMaintenanceService service) : ControllerBase
{
    [HttpGet("plans")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersRead)]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var result = await service.GetPlansAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("plans")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersManage)]
    public async Task<IActionResult> CreatePlan([FromBody] MaintenancePlanRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertPlanAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("plans/{id:guid}")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersManage)]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] MaintenancePlanRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertPlanAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("oil-reminders")]
    [RequirePermission(PermissionKeys.Maintenance.OilRead)]
    public async Task<IActionResult> GetOilReminders(CancellationToken cancellationToken)
    {
        var result = await service.GetOilRemindersAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("vehicles/{vehicleId:guid}/material-history")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersRead)]
    public async Task<IActionResult> GetVehicleMaterialHistory(Guid vehicleId, CancellationToken cancellationToken)
    {
        var result = await service.GetVehicleMaterialHistoryAsync(vehicleId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("riders/{riderProfileId:guid}/material-history")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersRead)]
    public async Task<IActionResult> GetRiderMaterialHistory(Guid riderProfileId, CancellationToken cancellationToken)
    {
        var result = await service.GetRiderMaterialHistoryAsync(riderProfileId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("external-profit")]
    [RequirePermission(PermissionKeys.Maintenance.ProfitReportsRead)]
    public async Task<IActionResult> GetExternalProfit([FromQuery] Guid maintenanceLocationId, [FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate, CancellationToken cancellationToken)
    {
        var result = await service.GetWorkshopProfitAsync(maintenanceLocationId, startDate, endDate, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
