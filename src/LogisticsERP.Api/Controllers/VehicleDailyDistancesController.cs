using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicle-daily-distances")]
public sealed class VehicleDailyDistancesController(IVehicleDailyDistanceService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Fleet.DailyDistancesRead)]
    public async Task<IActionResult> GetDaily(
        [FromQuery] DateOnly workDate,
        [FromQuery] string? search,
        [FromQuery] string? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetDailyAsync(workDate, search, source, page, pageSize, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{vehicleId:guid}/{workDate}")]
    [RequirePermission(PermissionKeys.Fleet.DailyDistancesManage)]
    public async Task<IActionResult> UpsertManual(
        Guid vehicleId,
        DateOnly workDate,
        [FromBody] UpsertManualVehicleDistanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpsertManualAsync(vehicleId, workDate, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("gps-import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequirePermission(PermissionKeys.Fleet.DailyDistancesImport)]
    public async Task<IActionResult> ImportGps(
        [FromForm] GpsDistanceImportForm form,
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
        var result = await service.ImportGpsAsync(upload, form.ExpectedWorkDate, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("gps-imports")]
    [RequirePermission(PermissionKeys.Fleet.DailyDistancesRead)]
    public async Task<IActionResult> GetImports(
        [FromQuery] DateOnly? workDate,
        CancellationToken cancellationToken)
    {
        var result = await service.GetImportsAsync(workDate, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

public sealed class GpsDistanceImportForm
{
    public DateOnly? ExpectedWorkDate { get; init; }
    public IFormFile File { get; init; } = null!;
}
