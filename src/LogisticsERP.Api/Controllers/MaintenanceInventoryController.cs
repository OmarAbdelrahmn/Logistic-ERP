using System.Text.Json;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Maintenance;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/maintenance-inventory")]
public sealed class MaintenanceInventoryController(IMaintenanceService service) : ControllerBase
{
    [HttpGet("items")]
    [RequirePermission(PermissionKeys.Inventory.ItemsRead)]
    public async Task<IActionResult> GetItems([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await service.GetItemsAsync(search, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("items")]
    [RequirePermission(PermissionKeys.Inventory.ItemsManage)]
    public async Task<IActionResult> CreateItem([FromBody] InventoryItemRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertItemAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("items/{id:guid}")]
    [RequirePermission(PermissionKeys.Inventory.ItemsManage)]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] InventoryItemRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertItemAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("suppliers")]
    [RequirePermission(PermissionKeys.Inventory.ReceiptsManage)]
    public async Task<IActionResult> GetSuppliers(CancellationToken cancellationToken)
    {
        var result = await service.GetSuppliersAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("suppliers")]
    [RequirePermission(PermissionKeys.Inventory.ReceiptsManage)]
    public async Task<IActionResult> CreateSupplier([FromBody] MaintenanceSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertSupplierAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("suppliers/{id:guid}")]
    [RequirePermission(PermissionKeys.Inventory.ReceiptsManage)]
    public async Task<IActionResult> UpdateSupplier(Guid id, [FromBody] MaintenanceSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertSupplierAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("balances")]
    [RequirePermission(PermissionKeys.Inventory.StockRead)]
    public async Task<IActionResult> GetBalances([FromQuery] Guid? inventoryLocationId, [FromQuery] Guid? inventoryItemId, CancellationToken cancellationToken)
    {
        var result = await service.GetBalancesAsync(inventoryLocationId, inventoryItemId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("cost-layers")]
    [RequirePermission(PermissionKeys.Inventory.CostLayersRead)]
    public async Task<IActionResult> GetCostLayers([FromQuery] Guid? inventoryLocationId, [FromQuery] Guid? inventoryItemId, [FromQuery] bool availableOnly = true, CancellationToken cancellationToken = default)
    {
        var result = await service.GetCostLayersAsync(inventoryLocationId, inventoryItemId, availableOnly, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("receipts")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequirePermission(PermissionKeys.Inventory.ReceiptsManage)]
    public async Task<IActionResult> PostReceipt([FromForm] PurchaseReceiptForm form, CancellationToken cancellationToken)
    {
        if (form.BillFile is null || form.BillFile.Length == 0 || string.IsNullOrWhiteSpace(form.ReceiptJson)) return BadRequest();
        PostPurchaseReceiptRequest? request;
        try { request = JsonSerializer.Deserialize<PostPurchaseReceiptRequest>(form.ReceiptJson, JsonSerializerOptions.Web); }
        catch (JsonException) { return BadRequest(); }
        if (request is null) return BadRequest();
        await using var stream = form.BillFile.OpenReadStream();
        var upload = new PrivateFileUpload(stream, form.BillFile.FileName, form.BillFile.ContentType, form.BillFile.Length);
        var result = await service.PostPurchaseReceiptAsync(request, upload, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("receipts")]
    [RequirePermission(PermissionKeys.Inventory.ReceiptsManage)]
    public async Task<IActionResult> GetReceipts(CancellationToken cancellationToken)
    {
        var result = await service.GetPurchaseReceiptsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("receipts/{id:guid}")]
    [RequirePermission(PermissionKeys.Inventory.ReceiptsManage)]
    public async Task<IActionResult> GetReceipt(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetPurchaseReceiptAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("receipts/{id:guid}/bill-file")]
    [RequirePermission(PermissionKeys.Inventory.ReceiptsManage)]
    public async Task<IActionResult> DownloadReceiptFile(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.DownloadPurchaseReceiptAttachmentAsync(id, cancellationToken);
        return result.IsSuccess ? File(result.Value!.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true) : result.ToProblem(HttpContext);
    }

    [HttpGet("oil-barrels")]
    [RequirePermission(PermissionKeys.Inventory.StockRead)]
    [RequirePermission(PermissionKeys.Inventory.CostLayersRead)]
    public async Task<IActionResult> GetOilBarrels([FromQuery] Guid? inventoryLocationId, [FromQuery] Guid? inventoryItemId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await service.GetOilBarrelsAsync(inventoryLocationId, inventoryItemId, status, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("oil-barrels/{id:guid}/open")]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    public async Task<IActionResult> OpenOilBarrel(Guid id, [FromBody] OpenOilBarrelRequest request, CancellationToken cancellationToken)
    {
        var result = await service.OpenOilBarrelAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("oil-barrels/{id:guid}/losses")]
    [RequirePermission(PermissionKeys.Inventory.StockAdjust)]
    public async Task<IActionResult> RecordOilBarrelLoss(Guid id, [FromBody] RecordOilBarrelLossRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RecordOilBarrelLossAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("transfers")]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    public async Task<IActionResult> PostTransfer([FromBody] PostStockTransferRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostTransferAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("supplier-returns")]
    [RequirePermission(PermissionKeys.Inventory.ReturnsManage)]
    public async Task<IActionResult> PostSupplierReturn([FromBody] PostSupplierReturnRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostSupplierReturnAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("rider-issues")]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    public async Task<IActionResult> PostRiderIssue([FromBody] PostRiderInventoryIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PostRiderIssueAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("rider-supply-requests")]
    [RequirePermission(PermissionKeys.Inventory.SupplyRequestsSubmit)]
    public async Task<IActionResult> CreateRiderSupplyRequest([FromBody] CreateRiderSupplyRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateRiderSupplyRequestAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("my-supply-requests/{id:guid}")]
    [RequirePermission(PermissionKeys.Inventory.SupplyRequestsSubmit)]
    public async Task<IActionResult> GetOwnSupplyRequest(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetOwnSupplyRequestAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("supply-requests")]
    [RequirePermission(PermissionKeys.Inventory.SupplyRequestsRead)]
    public async Task<IActionResult> GetSupplyRequests([FromQuery] Guid? inventoryLocationId, [FromQuery] Guid? vehicleId,
        [FromQuery] Guid? riderProfileId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await service.GetSupplyRequestsAsync(inventoryLocationId, vehicleId, riderProfileId, status, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("supply-requests/{id:guid}")]
    [RequirePermission(PermissionKeys.Inventory.SupplyRequestsRead)]
    public async Task<IActionResult> GetSupplyRequest(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetSupplyRequestAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("supply-requests/{id:guid}/approve-and-issue")]
    [RequirePermission(PermissionKeys.Inventory.SupplyRequestsApprove)]
    [RequirePermission(PermissionKeys.Inventory.StockMove)]
    public async Task<IActionResult> ApproveAndIssueSupplyRequest(Guid id, [FromBody] InventorySupplyDecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ApproveAndIssueSupplyRequestAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("supply-requests/{id:guid}/reject")]
    [RequirePermission(PermissionKeys.Inventory.SupplyRequestsApprove)]
    public async Task<IActionResult> RejectSupplyRequest(Guid id, [FromBody] InventorySupplyDecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RejectSupplyRequestAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("supply-requests/{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.Inventory.SupplyRequestsSubmit)]
    public async Task<IActionResult> CancelSupplyRequest(Guid id, [FromBody] InventorySupplyDecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CancelSupplyRequestAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

public sealed class PurchaseReceiptForm
{
    public string ReceiptJson { get; init; } = string.Empty;
    public IFormFile BillFile { get; init; } = null!;
}
