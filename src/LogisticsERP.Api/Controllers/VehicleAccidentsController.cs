using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/vehicle-accidents")]
public sealed class VehicleAccidentsController(IVehicleAccidentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? vehicleId, [FromQuery] Guid? riderProfileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await service.GetAsync(vehicleId, riderProfileId, page, pageSize, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleAccidentRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, idempotencyKey ?? string.Empty, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/evidence")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Evidence(Guid id, [FromForm] AccidentEvidenceForm form, CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0) return BadRequest();
        await using var stream = form.File.OpenReadStream();
        var result = await service.UploadEvidenceAsync(id, form.EvidenceType, new PrivateFileUpload(stream, form.File.FileName, form.File.ContentType, form.File.Length), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/evidence/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadEvidence(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await service.DownloadEvidenceAsync(id, attachmentId, cancellationToken);
        return result.IsSuccess ? File(result.Value!.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/finalize")]
    public async Task<IActionResult> Finalize(Guid id, [FromBody] AccidentActionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.FinalizeAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/correct")]
    public async Task<IActionResult> Correct(Guid id, [FromBody] CorrectVehicleAccidentRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CorrectAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] AccidentActionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CloseAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, [FromQuery] Guid? reportVersionId, CancellationToken cancellationToken)
    {
        var result = await service.DownloadReportAsync(id, reportVersionId, cancellationToken);
        return result.IsSuccess ? File(result.Value!.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true) : result.ToProblem(HttpContext);
    }
}

public sealed class AccidentEvidenceForm
{
    public VehicleAccidentEvidenceType EvidenceType { get; init; }
    public IFormFile File { get; init; } = null!;
}
