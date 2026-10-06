using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicle-daily-distances")]
public sealed class VehicleDailyDistancesController(IVehicleDailyDistanceService service) : ControllerBase
{
    [HttpGet("reports/vehicles/{vehicleId:guid}")]
    [RequirePermission(PermissionKeys.Fleet.DailyDistancesRead)]
    public async Task<IActionResult> GetVehiclePeriodReport(
        Guid vehicleId, [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var result = await service.GetVehiclePeriodReportAsync(vehicleId, fromDate, toDate, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext, "تعذر عرض تقرير مسافات المركبة");
    }

    [HttpGet("reports/missing-records")]
    [RequirePermission(PermissionKeys.Fleet.DailyDistancesRead)]
    public async Task<IActionResult> GetMissingRecordsReport(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate,
        [FromQuery] string? search, [FromQuery] Guid? operatingCityId,
        [FromQuery] LogisticsERP.Domain.Enums.VehicleType? vehicleType,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetMissingRecordsReportAsync(
            fromDate, toDate, search, operatingCityId, vehicleType, page, pageSize, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext, "تعذر عرض تقرير المسافات المفقودة");
    }

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
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext, "تعذر عرض المسافات اليومية");
    }

    [HttpPut("{vehicleId:guid}/{workDate}")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> UpsertManual(
        Guid vehicleId,
        DateOnly workDate,
        [FromBody] UpsertManualVehicleDistanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpsertManualAsync(vehicleId, workDate, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext, "تعذر حفظ قراءة العداد");
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
            return Result.Failure(FleetErrors.GpsFileRequired).ToProblem(HttpContext, "تعذر رفع تقرير GPS");
        }

        await using var stream = form.File.OpenReadStream();
        var upload = new PrivateFileUpload(
            stream,
            form.File.FileName,
            form.File.ContentType,
            form.File.Length);
        var result = await service.ImportGpsAsync(upload, form.ExpectedWorkDate, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext, "تعذر رفع تقرير GPS");
    }

    [HttpGet("gps-imports")]
    [RequirePermission(PermissionKeys.Fleet.DailyDistancesRead)]
    public async Task<IActionResult> GetImports(
        [FromQuery] DateOnly? workDate,
        CancellationToken cancellationToken)
    {
        var result = await service.GetImportsAsync(workDate, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext, "تعذر عرض سجل تقارير GPS");
    }
}

public sealed class GpsDistanceImportForm
{
    public DateOnly? ExpectedWorkDate { get; init; }
    public IFormFile File { get; init; } = null!;
}
