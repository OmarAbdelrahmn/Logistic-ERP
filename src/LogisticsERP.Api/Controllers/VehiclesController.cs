using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public sealed class VehiclesController(IFleetService service) : ControllerBase
{
    [HttpGet("{id:guid}/complete-history")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsRead)]
    [RequirePermission(PermissionKeys.Fleet.IssuesRead)]
    [RequirePermission(PermissionKeys.Fleet.AccidentsRead)]
    [RequirePermission(PermissionKeys.Maintenance.WorkOrdersRead)]
    [RequirePermission(PermissionKeys.Inventory.StockRead)]
    public async Task<IActionResult> CompleteHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetCompleteVehicleHistoryAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
    [HttpGet]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status, [FromQuery] Guid? operatingCityId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await service.GetVehiclesAsync(search, status, operatingCityId, page, pageSize, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("lookup")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    public async Task<IActionResult> Lookup([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await service.LookupVehiclesAsync(search, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetVehicleAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Fleet.VehiclesManage)]
    public async Task<IActionResult> Create([FromBody] VehicleUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertVehicleAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] VehicleUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertVehicleAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPatch("{id:guid}/archive")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesArchive)]
    public async Task<IActionResult> Archive(Guid id, [FromBody] ArchiveFleetRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveVehicleAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpPatch("{id:guid}/restore")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesArchive)]
    public async Task<IActionResult> Restore(Guid id, [FromBody] RowVersionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RestoreVehicleAsync(id, request.RowVersion, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/{statusAction:regex(^(stolen|recover|out-of-service|restore|decommission)$)}")]
    [Authorize]
    public async Task<IActionResult> Status(Guid id, string statusAction, [FromBody] VehicleStatusCommandRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ChangeAdministrativeStatusAsync(id, statusAction, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/status-history")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    public async Task<IActionResult> StatusHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetStatusHistoryAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/odometer")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    public async Task<IActionResult> OdometerHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetOdometerHistoryAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/odometer")]
    [Authorize]
    public async Task<IActionResult> Odometer(Guid id, [FromBody] OdometerReadingRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RecordOdometerAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/rider-timeline")]
    [RequirePermission(PermissionKeys.Fleet.AssignmentsRead)]
    public async Task<IActionResult> RiderTimeline(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetVehicleTimelineAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/readiness")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    public async Task<IActionResult> Readiness(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetReadinessAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/identity-corrections")]
    [RequirePermission(PermissionKeys.Fleet.CorrectionsManage)]
    public async Task<IActionResult> CorrectIdentity(Guid id, [FromBody] VehicleIdentityCorrectionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CorrectIdentityAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/identity-corrections")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    public async Task<IActionResult> IdentityCorrections(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetIdentityCorrectionHistoryAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/registration-transitions/private-to-public")]
    [RequirePermission(PermissionKeys.Fleet.RegistrationTransitionsManage)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(22 * 1024 * 1024)]
    public async Task<IActionResult> TransitionToPublic(Guid id, [FromForm] VehicleRegistrationTransitionForm form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.PlateNumberAr)) return Result.Failure(FleetErrors.TransitionPlateNumberArInvalid).ToProblem(HttpContext);
        if (string.IsNullOrWhiteSpace(form.PlateNumberEn)) return Result.Failure(FleetErrors.TransitionPlateNumberEnInvalid).ToProblem(HttpContext);
        if (form.EffectiveAtUtc is null) return Result.Failure(FleetErrors.TransitionEffectiveDateRequired).ToProblem(HttpContext);
        if (string.IsNullOrWhiteSpace(form.Reason)) return Result.Failure(FleetErrors.TransitionReasonInvalid).ToProblem(HttpContext);
        if (string.IsNullOrWhiteSpace(form.RowVersion)) return Result.Failure(FleetErrors.TransitionRowVersionRequired).ToProblem(HttpContext);
        if (form.Istimara is null || form.Istimara.Length == 0) return Result.Failure(FleetErrors.TransitionIstimaraInvalid).ToProblem(HttpContext);
        if (form.OperationCard is null || form.OperationCard.Length == 0) return Result.Failure(FleetErrors.TransitionOperationCardInvalid).ToProblem(HttpContext);
        await using var istimara = form.Istimara.OpenReadStream();
        await using var operationCard = form.OperationCard.OpenReadStream();
        var request = new VehicleRegistrationTransitionRequest(form.PlateNumberAr, form.PlateNumberEn, form.PlateLettersAr, form.PlateLettersEn, form.PlateDigits, form.EffectiveAtUtc.Value, form.Reason, form.RowVersion);
        var result = await service.TransitionToPublicTransportAsync(id, request,
            new PrivateFileUpload(istimara, form.Istimara.FileName, form.Istimara.ContentType, form.Istimara.Length),
            new PrivateFileUpload(operationCard, form.OperationCard.FileName, form.OperationCard.ContentType, form.OperationCard.Length), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/registration-transitions")]
    [RequirePermission(PermissionKeys.Fleet.VehiclesRead)]
    public async Task<IActionResult> RegistrationTransitions(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetRegistrationTransitionHistoryAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

public sealed record RowVersionRequest(string RowVersion);

[ApiController]
[Route("api/vehicles/{vehicleId:guid}/files")]
[RequestSizeLimit(11 * 1024 * 1024)]
public sealed class VehicleFilesController(IVehicleFileService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Fleet.FilesRead)]
    public async Task<IActionResult> Get(Guid vehicleId, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(vehicleId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{kind}")]
    [RequirePermission(PermissionKeys.Fleet.FilesUpload)]
    public async Task<IActionResult> Upload(Guid vehicleId, VehicleFileKind kind, [FromForm] VehicleFileUploadForm form, CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0) return BadRequest();
        await using var stream = form.File.OpenReadStream();
        var result = await service.UploadSlotAsync(vehicleId, kind, new PrivateFileUpload(stream, form.File.FileName, form.File.ContentType, form.File.Length), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{attachmentId:guid}/versions")]
    [RequirePermission(PermissionKeys.Fleet.FilesRead)]
    public async Task<IActionResult> Versions(Guid vehicleId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await service.GetVersionsAsync(vehicleId, attachmentId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{attachmentId:guid}/download")]
    [RequirePermission(PermissionKeys.Fleet.FilesDownload)]
    public async Task<IActionResult> Download(Guid vehicleId, Guid attachmentId, [FromQuery] Guid? versionId, CancellationToken cancellationToken)
    {
        var result = await service.DownloadAsync(vehicleId, attachmentId, versionId, cancellationToken);
        return result.IsSuccess ? File(result.Value!.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true) : result.ToProblem(HttpContext);
    }

}

public sealed class VehicleFileUploadForm
{
    public IFormFile File { get; init; } = null!;
}

public sealed class VehicleRegistrationTransitionForm
{
    public string? PlateNumberAr { get; init; }
    public string? PlateNumberEn { get; init; }
    public string? PlateLettersAr { get; init; }
    public string? PlateLettersEn { get; init; }
    public string? PlateDigits { get; init; }
    public DateTimeOffset? EffectiveAtUtc { get; init; }
    public string? Reason { get; init; }
    public string? RowVersion { get; init; }
    public IFormFile? Istimara { get; init; }
    public IFormFile? OperationCard { get; init; }
}
