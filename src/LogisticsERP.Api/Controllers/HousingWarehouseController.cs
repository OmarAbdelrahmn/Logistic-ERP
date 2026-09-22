using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/housing/{housingId:guid}/warehouse")]
public sealed class HousingWarehouseController(IHousingWarehouseService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Operations.HousingRead)]
    public async Task<IActionResult> Get(Guid housingId, CancellationToken cancellationToken)
    {
        var result = await service.GetWarehouseAsync(housingId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("items")]
    [RequirePermission(PermissionKeys.Operations.HousingRead)]
    public async Task<IActionResult> GetItems(
        Guid housingId,
        [FromQuery] string? search,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await service.GetItemsAsync(housingId, search, status, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("items/{itemId:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingRead)]
    public async Task<IActionResult> GetItem(Guid housingId, Guid itemId, CancellationToken cancellationToken)
    {
        var result = await service.GetItemAsync(housingId, itemId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("items")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> CreateItem(
        Guid housingId,
        [FromBody] CreateHousingWarehouseItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateItemAsync(housingId, request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetItem), new { housingId, itemId = result.Value!.Id }, result.Value)
            : result.ToProblem(HttpContext);
    }

    [HttpPut("items/{itemId:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> UpdateItem(
        Guid housingId,
        Guid itemId,
        [FromBody] UpdateHousingWarehouseItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateItemAsync(housingId, itemId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPatch("items/{itemId:guid}/statuses/{status}/quantity")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> SetStatusQuantity(
        Guid housingId,
        Guid itemId,
        string status,
        [FromBody] SetHousingWarehouseItemStatusQuantityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.SetStatusQuantityAsync(housingId, itemId, status, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("items/{itemId:guid}/status-transfers")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> TransferStatus(
        Guid housingId,
        Guid itemId,
        [FromBody] TransferHousingWarehouseItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.TransferStatusAsync(housingId, itemId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("items/{itemId:guid}/housing-transfers")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> TransferUnusedToHousing(
        Guid housingId,
        Guid itemId,
        [FromBody] TransferHousingWarehouseItemToHousingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.TransferUnusedToHousingAsync(housingId, itemId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpDelete("items/{itemId:guid}")]
    [RequirePermission(PermissionKeys.Operations.HousingManage)]
    public async Task<IActionResult> ArchiveItem(
        Guid housingId,
        Guid itemId,
        [FromBody] ArchiveHousingWarehouseItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ArchiveItemAsync(housingId, itemId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}
