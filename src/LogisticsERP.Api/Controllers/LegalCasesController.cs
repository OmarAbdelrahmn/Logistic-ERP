using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/hr/legal-cases")]
public sealed class LegalCasesController(ILegalCaseService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Workflows.LegalCasesRead)]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] Guid? sponsorId,
        [FromQuery] Guid? employeeId,
        [FromQuery] Guid? riderProfileId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetAsync(new LegalCaseQuery(search, status, sponsorId, employeeId, riderProfileId, fromDate, toDate, page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Workflows.LegalCasesRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Workflows.LegalCasesManage)]
    public async Task<IActionResult> Create([FromBody] LegalCaseUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Workflows.LegalCasesManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] LegalCaseUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Workflows.LegalCasesManage)]
    public async Task<IActionResult> Archive(Guid id, [FromBody] LegalCaseArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpGet("{id:guid}/history")]
    [RequirePermission(PermissionKeys.Workflows.LegalCasesRead)]
    public async Task<IActionResult> History(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetHistoryAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{caseId:guid}/hearings")]
    [RequirePermission(PermissionKeys.Workflows.LegalCasesManage)]
    public async Task<IActionResult> CreateHearing(Guid caseId, [FromBody] LegalCaseHearingUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateHearingAsync(caseId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{caseId:guid}/hearings/{hearingId:guid}")]
    [RequirePermission(PermissionKeys.Workflows.LegalCasesManage)]
    public async Task<IActionResult> UpdateHearing(Guid caseId, Guid hearingId, [FromBody] LegalCaseHearingUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateHearingAsync(caseId, hearingId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpDelete("{caseId:guid}/hearings/{hearingId:guid}")]
    [RequirePermission(PermissionKeys.Workflows.LegalCasesManage)]
    public async Task<IActionResult> ArchiveHearing(Guid caseId, Guid hearingId, [FromBody] LegalCaseArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveHearingAsync(caseId, hearingId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpPost("{caseId:guid}/hearings/{hearingId:guid}/files")]
    [RequirePermission(PermissionKeys.Workflows.LegalCasesManage)]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadFile(Guid caseId, Guid hearingId, [FromForm] LegalCaseFileForm form, CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0) return BadRequest();
        await using var stream = form.File.OpenReadStream();
        var result = await service.UploadFileAsync(caseId, hearingId,
            new(form.Description, new PrivateFileUpload(stream, form.File.FileName, form.File.ContentType, form.File.Length)), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{caseId:guid}/hearings/{hearingId:guid}/files/{fileId:guid}/download")]
    [RequirePermission(PermissionKeys.Workflows.LegalCaseFilesDownload)]
    public async Task<IActionResult> DownloadFile(Guid caseId, Guid hearingId, Guid fileId, CancellationToken cancellationToken)
    {
        var result = await service.DownloadFileAsync(caseId, hearingId, fileId, cancellationToken);
        return result.IsSuccess
            ? File(result.Value!.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true)
            : result.ToProblem(HttpContext);
    }

    [HttpDelete("{caseId:guid}/hearings/{hearingId:guid}/files/{fileId:guid}")]
    [RequirePermission(PermissionKeys.Workflows.LegalCasesManage)]
    public async Task<IActionResult> ArchiveFile(Guid caseId, Guid hearingId, Guid fileId, [FromBody] LegalCaseArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveFileAsync(caseId, hearingId, fileId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}

public sealed class LegalCaseFileForm
{
    public string? Description { get; init; }
    public IFormFile File { get; init; } = null!;
}
