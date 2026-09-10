using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicle-accidents/{accidentId:guid}/workflow")]
public sealed class AccidentWorkflowController(IAccidentWorkflowService service) : ControllerBase
{
    [HttpGet("/api/vehicle-accidents/workflows")]
    [RequirePermission(PermissionKeys.Fleet.AccidentsRead)]
    public async Task<IActionResult> Queue([FromQuery] Guid? vehicleId, [FromQuery] Guid? riderProfileId,
        [FromQuery] AccidentCaseStage? stage, [FromQuery] bool overdueOnly = false, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await service.GetWorkflowQueueAsync(vehicleId, riderProfileId, stage, overdueOnly, page, pageSize, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Fleet.AccidentsRead)]
    public async Task<IActionResult> Get(Guid accidentId, CancellationToken cancellationToken)
    {
        var result = await service.GetWorkflowAsync(accidentId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("actions")]
    [RequirePermission(PermissionKeys.Fleet.AccidentsFinalize)]
    public async Task<IActionResult> Action(Guid accidentId, [FromBody] AccidentWorkflowRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ExecuteWorkflowAsync(accidentId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("attachments")]
    [RequirePermission(PermissionKeys.Fleet.AccidentsReport)]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid accidentId, [FromForm] AccidentWorkflowAttachmentForm form, CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0) return BadRequest();
        await using var stream = form.File.OpenReadStream();
        var result = await service.UploadWorkflowAttachmentAsync(accidentId, form.EvidenceType,
            new(form.Description, form.FromLocation, form.ToLocation, form.TransportedAtUtc, form.Amount),
            new PrivateFileUpload(stream, form.File.FileName, form.File.ContentType, form.File.Length), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("installments")]
    [RequirePermission(PermissionKeys.Fleet.AccidentsFinalize)]
    public async Task<IActionResult> Installment(Guid accidentId, [FromBody] AccidentInstallmentRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AddInstallmentAsync(accidentId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("documents/{kind}/download")]
    [RequirePermission(PermissionKeys.Fleet.AccidentsFinalize)]
    public async Task<IActionResult> Document(Guid accidentId, string kind, CancellationToken cancellationToken)
    {
        var result = await service.DownloadSourceDocumentAsync(accidentId, kind, cancellationToken);
        return result.IsSuccess ? File(result.Value!.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true) : result.ToProblem(HttpContext);
    }
}

public sealed class AccidentWorkflowAttachmentForm
{
    public VehicleAccidentEvidenceType EvidenceType { get; init; }
    public IFormFile File { get; init; } = null!;
    public string? Description { get; init; }
    public string? FromLocation { get; init; }
    public string? ToLocation { get; init; }
    public DateTimeOffset? TransportedAtUtc { get; init; }
    public decimal? Amount { get; init; }
}
