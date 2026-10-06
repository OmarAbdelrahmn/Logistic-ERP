using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/maintenance-work-orders")]
public sealed class MaintenanceWorkOrdersController(IMaintenanceService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersRead)]
    public async Task<IActionResult> Get([FromQuery] Guid? maintenanceLocationId, [FromQuery] Guid? vehicleId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await service.GetWorkOrdersAsync(MaintenanceServiceSubjectType.CompanyVehicle, maintenanceLocationId, vehicleId, status, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("external")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersRead)]
    [RequirePermission(PermissionKeys.Maintenance.ExternalJobsRead)]
    public async Task<IActionResult> GetExternal([FromQuery] Guid? maintenanceLocationId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await service.GetWorkOrdersAsync(MaintenanceServiceSubjectType.ExternalVehicle, maintenanceLocationId, null, status, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersRead)]
    public async Task<IActionResult> GetOne(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetWorkOrderAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersCreate)]
    public async Task<IActionResult> CreateCompany([FromBody] CreateMaintenanceWorkOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.ServiceSubjectType != MaintenanceServiceSubjectType.CompanyVehicle)
            return ApiProblemDetails.BadRequest(HttpContext, "اختر مركبة تابعة للشركة لأمر الصيانة هذا.");
        var result = await service.CreateWorkOrderAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("external")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersCreate)]
    [RequirePermission(PermissionKeys.Maintenance.ExternalJobsCreate)]
    public async Task<IActionResult> CreateExternal([FromBody] CreateMaintenanceWorkOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.ServiceSubjectType != MaintenanceServiceSubjectType.ExternalVehicle)
            return ApiProblemDetails.BadRequest(HttpContext, "اختر مركبة خارجية لأمر الصيانة هذا.");
        var result = await service.CreateWorkOrderAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/start")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersUpdate)]
    public Task<IActionResult> Start(Guid id, [FromBody] MaintenanceWorkOrderActionRequest request, CancellationToken cancellationToken) =>
        Act(id, "start", request, cancellationToken);

    [HttpPost("{id:guid}/complete")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersUpdate)]
    public Task<IActionResult> Complete(Guid id, [FromBody] MaintenanceWorkOrderActionRequest request, CancellationToken cancellationToken) =>
        Act(id, "complete", request, cancellationToken);

    [HttpPost("{id:guid}/close")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersUpdate)]
    public Task<IActionResult> Close(Guid id, [FromBody] MaintenanceWorkOrderActionRequest request, CancellationToken cancellationToken) =>
        Act(id, "close", request, cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersDelete)]
    public Task<IActionResult> Cancel(Guid id, [FromBody] MaintenanceWorkOrderActionRequest request, CancellationToken cancellationToken) =>
        Act(id, "cancel", request, cancellationToken);

    private async Task<IActionResult> Act(Guid id, string operation, MaintenanceWorkOrderActionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ActOnWorkOrderAsync(id, operation, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/materials")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    public async Task<IActionResult> PostMaterial(Guid id, [FromBody] PostMaterialUsageRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostMaterialUsageAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("materials/{usageId:guid}/reverse")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [RequirePermission(PermissionKeys.Inventory.StockAdjust)]
    public async Task<IActionResult> ReverseMaterial(Guid usageId, [FromBody] ReverseMaterialUsageRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ReverseMaterialUsageAsync(usageId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/oil-change")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [RequirePermission(PermissionKeys.Maintenance.OilComplete)]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    public async Task<IActionResult> CompleteOilChange(Guid id, [FromBody] CompleteOilChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CompleteOilChangeAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/part-sales")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [RequirePermission(PermissionKeys.Maintenance.PartSalesCreate)]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    public async Task<IActionResult> PostPartSale(Guid id, [FromBody] ExternalPartSaleRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostExternalPartSaleAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/customer-labor-charges")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [RequirePermission(PermissionKeys.Maintenance.CustomerLaborChargesCreate)]
    public async Task<IActionResult> PostCustomerLaborCharge(Guid id, [FromBody] ExternalFinancialEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostCustomerLaborChargeAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/mechanic-labor-payments")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [RequirePermission(PermissionKeys.Maintenance.MechanicLaborPaymentsCreate)]
    public async Task<IActionResult> PostMechanicLaborPayment(Guid id, [FromBody] MechanicLaborPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostMechanicLaborPaymentAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/other-financial-entries")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [RequirePermission(PermissionKeys.Maintenance.ExternalJobsCreate)]
    public async Task<IActionResult> PostOtherFinancialEntry(Guid id, [FromQuery] bool income, [FromBody] ExternalFinancialEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostOtherFinancialEntryAsync(id, income, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/customer-payments")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersUpdate)]
    [RequirePermission(PermissionKeys.Maintenance.ExternalJobsCreate)]
    public async Task<IActionResult> PostCustomerPayment(Guid id, [FromBody] ExternalCustomerPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostCustomerPaymentAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
