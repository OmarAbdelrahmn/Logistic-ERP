using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/maintenance")]
public sealed class MaintenanceController(IMaintenanceService service) : ControllerBase
{

    [HttpGet("oil-changes/{id:guid}")]
    [RequirePermission(PermissionKeys.Maintenance.OilRead)]
    [RequirePermission(PermissionKeys.Inventory.StockRead)]
    [RequirePermission(PermissionKeys.Inventory.CostLayersRead)]
    public async Task<IActionResult> OilChangeDetails(Guid id, CancellationToken ct)
    {
        var result = await service.GetOilChangeDetailsAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("oil-changes/{id:guid}/revisions")]
    [RequirePermission(PermissionKeys.Maintenance.OilRead)]
    [RequirePermission(PermissionKeys.Inventory.StockRead)]
    [RequirePermission(PermissionKeys.Inventory.CostLayersRead)]
    public async Task<IActionResult> OilChangeRevisions(Guid id, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var result = await service.GetOilChangeRevisionsAsync(id, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("oil-changes/{id:guid}/corrections/preview")]
    [RequirePermission(PermissionKeys.Maintenance.OilEdit)]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    [RequirePermission(PermissionKeys.Inventory.CostLayersRead)]
    public async Task<IActionResult> PreviewOilChangeEdit(Guid id, OilChangeEditRequest request, CancellationToken ct)
    {
        var result = await service.EditOilChangeAsync(id, request, string.Empty, true, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("oil-changes/{id:guid}")]
    [RequirePermission(PermissionKeys.Maintenance.OilEdit)]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    [RequirePermission(PermissionKeys.Inventory.CostLayersRead)]
    public async Task<IActionResult> EditOilChange(Guid id, OilChangeEditRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct)
    {
        var result = await service.EditOilChangeAsync(id, request, key ?? string.Empty, false, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("oil-changes/{id:guid}/cancellation/preview")]
    [RequirePermission(PermissionKeys.Maintenance.OilCancel)]
    [RequirePermission(PermissionKeys.Inventory.StockAdjust)]
    [RequirePermission(PermissionKeys.Inventory.CostLayersRead)]
    public async Task<IActionResult> PreviewOilChangeCancel(Guid id, OilChangeCancelRequest request, CancellationToken ct)
    {
        var result = await service.CancelOilChangeAsync(id, request, string.Empty, true, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("oil-changes/{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.Maintenance.OilCancel)]
    [RequirePermission(PermissionKeys.Inventory.StockAdjust)]
    [RequirePermission(PermissionKeys.Inventory.CostLayersRead)]
    public async Task<IActionResult> CancelOilChange(Guid id, OilChangeCancelRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct)
    {
        var result = await service.CancelOilChangeAsync(id, request, key ?? string.Empty, false, ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("plans")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersRead)]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var result = await service.GetPlansAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("plans")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersCreate)]
    public async Task<IActionResult> CreatePlan([FromBody] MaintenancePlanRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertPlanAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("plans/{id:guid}")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersUpdate)]
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

    [HttpGet("oil-changes")]
    [RequirePermission(PermissionKeys.Maintenance.OilRead)]
    public async Task<IActionResult> GetOilChanges([FromQuery] Guid? vehicleId, CancellationToken cancellationToken)
    {
        var result = await service.GetOilChangesAsync(vehicleId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("vehicles/{vehicleId:guid}/oil-changes")]
    [RequirePermission(PermissionKeys.Maintenance.OilComplete)]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    public async Task<IActionResult> CompleteDirectOilChange(Guid vehicleId, [FromBody] DirectOilChangeRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var result = await service.CompleteDirectOilChangeAsync(vehicleId, request, idempotencyKey ?? string.Empty, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("oil-changes/{id:guid}/vehicle")]
    [Authorize(Roles = SystemRoles.SystemAdmin)]
    [RequirePermission(PermissionKeys.Maintenance.OilComplete)]
    public async Task<IActionResult> CorrectCompletedOilChangeVehicle(Guid id, [FromBody] CorrectCompletedOilChangeVehicleRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CorrectCompletedOilChangeVehicleAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("oil-inventory-locations")]
    [RequirePermission(PermissionKeys.Maintenance.OilRead)]
    public async Task<IActionResult> GetDirectOilInventoryLocations(CancellationToken cancellationToken)
    {
        var result = await service.GetDirectOilInventoryLocationsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("oil-barrels")]
    [RequirePermission(PermissionKeys.Maintenance.OilRead)]
    public async Task<IActionResult> GetDirectOilBarrels([FromQuery] Guid inventoryLocationId, [FromQuery] Guid inventoryItemId, [FromQuery] VehicleType? vehicleType = null, CancellationToken cancellationToken = default)
    {
        var result = await service.GetDirectOilBarrelsAsync(inventoryLocationId, inventoryItemId, vehicleType, cancellationToken);
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
