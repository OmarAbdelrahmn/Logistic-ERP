using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/hr-workflows/leave-requests/{leaveRequestId:guid}/documents")]
public sealed class LeaveDocumentsController(ILeaveDocumentService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsRead)]
    public async Task<IActionResult> Get(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(leaveRequestId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequirePermission(PermissionKeys.Documents.Upload)]
    public async Task<IActionResult> Upload(Guid leaveRequestId, [FromForm] LeaveDocumentUploadForm request, CancellationToken cancellationToken)
    {
        if (request.File is null) return BadRequest();
        await using var stream = request.File.OpenReadStream();
        var file = new FileUploadContent(stream, request.File.FileName, request.File.ContentType, request.File.Length);
        var result = await service.UploadAsync(leaveRequestId, request.ToMetadata(), file, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{documentId:guid}/versions")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequirePermission(PermissionKeys.Documents.Upload)]
    public async Task<IActionResult> UploadVersion(Guid leaveRequestId, Guid documentId, [FromForm] DocumentVersionUploadForm request, CancellationToken cancellationToken)
    {
        if (request.File is null) return BadRequest();
        await using var stream = request.File.OpenReadStream();
        var file = new FileUploadContent(stream, request.File.FileName, request.File.ContentType, request.File.Length);
        var result = await service.UploadNewVersionAsync(leaveRequestId, documentId, file, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{documentId:guid}")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsManage)]
    public async Task<IActionResult> UpdateMetadata(Guid leaveRequestId, Guid documentId, [FromBody] UpdateLeaveDocumentRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateMetadataAsync(leaveRequestId, documentId, request.Metadata, request.RowVersion, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{documentId:guid}/versions")]
    [RequirePermission(PermissionKeys.Documents.Read)]
    public async Task<IActionResult> Versions(Guid leaveRequestId, Guid documentId, CancellationToken cancellationToken)
    {
        var result = await service.GetVersionsAsync(leaveRequestId, documentId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{documentId:guid}/download")]
    [RequirePermission(PermissionKeys.Documents.DownloadSensitive)]
    public async Task<IActionResult> Download(Guid leaveRequestId, Guid documentId, [FromQuery] Guid? versionId, CancellationToken cancellationToken)
    {
        var result = await service.DownloadAsync(leaveRequestId, documentId, versionId, cancellationToken);
        return result.IsSuccess
            ? File(result.Value!.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true)
            : result.ToProblem(HttpContext);
    }

    [HttpPatch("{documentId:guid}/archive")]
    [RequirePermission(PermissionKeys.Workflows.LeaveRequestsManage)]
    public async Task<IActionResult> Archive(Guid leaveRequestId, Guid documentId, [FromBody] ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveAsync(leaveRequestId, documentId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }
}

public sealed class LeaveDocumentUploadForm
{
    public string Kind { get; init; } = string.Empty;
    public string? ReferenceNumber { get; init; }
    public DateOnly? IssuedOn { get; init; }
    public DateOnly? ExpiresOn { get; init; }
    public string? Notes { get; init; }
    public IFormFile File { get; init; } = null!;

    public LeaveDocumentMetadataRequest ToMetadata() => new(Kind, ReferenceNumber, IssuedOn, ExpiresOn, Notes);
}

public sealed record UpdateLeaveDocumentRequest(LeaveDocumentMetadataRequest Metadata, string RowVersion);

