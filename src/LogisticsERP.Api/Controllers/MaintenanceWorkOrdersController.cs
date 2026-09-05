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
        var result = await service.GetWorkOrdersAsync(maintenanceLocationId, vehicleId, status, cancellationToken);
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
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersManage)]
    public async Task<IActionResult> CreateCompany([FromBody] CreateMaintenanceWorkOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.ServiceSubjectType != MaintenanceServiceSubjectType.CompanyVehicle) return BadRequest();
        var result = await service.CreateWorkOrderAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("external")]
    [RequirePermission(PermissionKeys.Maintenance.ExternalJobsManage)]
    public async Task<IActionResult> CreateExternal([FromBody] CreateMaintenanceWorkOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.ServiceSubjectType != MaintenanceServiceSubjectType.ExternalVehicle) return BadRequest();
        var result = await service.CreateWorkOrderAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/{operation:regex(^(start|complete|close|cancel)$)}")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersManage)]
    public async Task<IActionResult> Act(Guid id, string operation, [FromBody] MaintenanceWorkOrderActionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ActOnWorkOrderAsync(id, operation, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/materials")]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersManage)]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    public async Task<IActionResult> PostMaterial(Guid id, [FromBody] PostMaterialUsageRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostMaterialUsageAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("materials/{usageId:guid}/reverse")]
    [RequirePermission(PermissionKeys.Inventory.StockAdjust)]
    public async Task<IActionResult> ReverseMaterial(Guid usageId, [FromBody] ReverseMaterialUsageRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ReverseMaterialUsageAsync(usageId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/oil-change")]
    [RequirePermission(PermissionKeys.Maintenance.OilComplete)]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    public async Task<IActionResult> CompleteOilChange(Guid id, [FromBody] CompleteOilChangeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CompleteOilChangeAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/part-sales")]
    [RequirePermission(PermissionKeys.Maintenance.PartSalesManage)]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    public async Task<IActionResult> PostPartSale(Guid id, [FromBody] ExternalPartSaleRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostExternalPartSaleAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/customer-labor-charges")]
    [RequirePermission(PermissionKeys.Maintenance.CustomerLaborChargesManage)]
    public async Task<IActionResult> PostCustomerLaborCharge(Guid id, [FromBody] ExternalFinancialEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostCustomerLaborChargeAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/mechanic-labor-payments")]
    [RequirePermission(PermissionKeys.Maintenance.MechanicLaborPaymentsManage)]
    public async Task<IActionResult> PostMechanicLaborPayment(Guid id, [FromBody] MechanicLaborPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostMechanicLaborPaymentAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/other-financial-entries")]
    [RequirePermission(PermissionKeys.Maintenance.ExternalJobsManage)]
    public async Task<IActionResult> PostOtherFinancialEntry(Guid id, [FromQuery] bool income, [FromBody] ExternalFinancialEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostOtherFinancialEntryAsync(id, income, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/customer-payments")]
    [RequirePermission(PermissionKeys.Maintenance.ExternalJobsManage)]
    public async Task<IActionResult> PostCustomerPayment(Guid id, [FromBody] ExternalCustomerPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostCustomerPaymentAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
