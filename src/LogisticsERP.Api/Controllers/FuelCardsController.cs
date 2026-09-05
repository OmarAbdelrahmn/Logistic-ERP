using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fuel;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/fuel-cards")]
public sealed class FuelCardsController(IFuelCardService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Fuel.Read)]
    public async Task<IActionResult> GetCards(
        [FromQuery] string? search,
        [FromQuery] string? provider,
        [FromQuery] Guid? riderProfileId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetCardsAsync(search, provider, riderProfileId, page, pageSize, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Fuel.Read)]
    public async Task<IActionResult> GetCard(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetCardAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Fuel.Manage)]
    public async Task<IActionResult> CreateCard(
        [FromBody] CreateFuelCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateCardAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetCard), new { id = result.Value!.Id }, result.Value)
            : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/assignments")]
    [RequirePermission(PermissionKeys.Fuel.Read)]
    public async Task<IActionResult> GetAssignments(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAssignmentsAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/assignments")]
    [RequirePermission(PermissionKeys.Fuel.Manage)]
    public async Task<IActionResult> AssignRider(
        Guid id,
        [FromBody] AssignFuelCardRiderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.AssignRiderAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/stop-rider")]
    [RequirePermission(PermissionKeys.Fuel.Manage)]
    public async Task<IActionResult> StopRider(
        Guid id,
        [FromBody] StopFuelCardRiderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.StopRiderAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("monthly-usage")]
    [RequirePermission(PermissionKeys.Fuel.Read)]
    public async Task<IActionResult> GetMonthlyUsage(
        [FromQuery] DateOnly month,
        [FromQuery] string? search,
        [FromQuery] string? provider,
        [FromQuery] Guid? riderProfileId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetMonthlyUsageAsync(
            month,
            search,
            provider,
            riderProfileId,
            page,
            pageSize,
            cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("imports")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(26 * 1024 * 1024)]
    [RequirePermission(PermissionKeys.Fuel.Import)]
    public async Task<IActionResult> Import(
        [FromForm] FuelImportForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
        {
            return BadRequest();
        }

        await using var stream = form.File.OpenReadStream();
        var upload = new PrivateFileUpload(
            stream,
            form.File.FileName,
            form.File.ContentType,
            form.File.Length);
        var result = await service.ImportAsync(upload, form.ExpectedMonth, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("imports")]
    [RequirePermission(PermissionKeys.Fuel.Read)]
    public async Task<IActionResult> GetImports(
        [FromQuery] DateOnly? month,
        [FromQuery] string? provider,
        CancellationToken cancellationToken)
    {
        var result = await service.GetImportsAsync(month, provider, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

public sealed class FuelImportForm
{
    public DateOnly? ExpectedMonth { get; init; }
    public IFormFile File { get; init; } = null!;
}
