using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}/documents")]
[AllowAnonymous]
public sealed class EmployeeDocumentsController(IEmployeeDocumentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await service.GetEmployeeDocumentsAsync(employeeId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public Task<IActionResult> Upload(Guid employeeId, [FromForm] EmployeeDocumentUploadForm request, CancellationToken cancellationToken) =>
        UploadInternal(employeeId, request.DocumentTypeId, request, cancellationToken);

    [HttpPost("{documentId:guid}/versions")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadVersion(Guid employeeId, Guid documentId, [FromForm] DocumentVersionUploadForm request, CancellationToken cancellationToken)
    {
        var validation = CreateFile(request.File);
        if (validation is null) return BadRequest();
        await using var stream = request.File.OpenReadStream();
        var result = await service.UploadNewVersionAsync(employeeId, documentId, validation with { Content = stream }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{documentId:guid}")]
    public async Task<IActionResult> UpdateMetadata(Guid employeeId, Guid documentId, [FromBody] UpdateDocumentMetadataRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateMetadataAsync(employeeId, documentId, request.Metadata, request.RowVersion, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{documentId:guid}/versions")]
    public async Task<IActionResult> Versions(Guid employeeId, Guid documentId, CancellationToken cancellationToken)
    {
        var result = await service.GetVersionsAsync(employeeId, documentId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid employeeId, Guid documentId, [FromQuery] Guid? versionId, CancellationToken cancellationToken)
    {
        var result = await service.DownloadAsync(employeeId, documentId, versionId, cancellationToken);
        return result.IsSuccess
            ? File(result.Value!.Content, result.Value.ContentType, result.Value.DownloadFileName, enableRangeProcessing: true)
            : result.ToProblem(HttpContext);
    }

    [HttpGet("{documentId:guid}/preview")]
    public async Task<IActionResult> Preview(Guid employeeId, Guid documentId, [FromQuery] Guid? versionId, CancellationToken cancellationToken)
    {
        var result = await service.DownloadAsync(employeeId, documentId, versionId, cancellationToken);
        if (result.IsFailure) return result.ToProblem(HttpContext);

        Response.Headers.ContentDisposition = $"inline; filename=\"{Uri.EscapeDataString(result.Value!.DownloadFileName)}\"";
        return File(result.Value.Content, result.Value.ContentType, enableRangeProcessing: true);
    }

    [HttpPatch("{documentId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid employeeId, Guid documentId, [FromBody] ArchiveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveAsync(employeeId, documentId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    private async Task<IActionResult> UploadInternal(Guid employeeId, Guid documentTypeId, EmployeeDocumentUploadForm request, CancellationToken cancellationToken)
    {
        var file = CreateFile(request.File);
        if (file is null) return BadRequest();
        await using var stream = request.File.OpenReadStream();
        var result = await service.UploadAsync(employeeId, documentTypeId, request.ToMetadata(), file with { Content = stream }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    internal static FileUploadContent? CreateFile(IFormFile? file) => file is null
        ? null
        : new FileUploadContent(Stream.Null, file.FileName, file.ContentType, file.Length);
}

[ApiController]
[Route("api/riders/{riderProfileId:guid}/documents")]
[RequestSizeLimit(11 * 1024 * 1024)]
public sealed class RiderDocumentsController(IEmployeeDocumentService service) : ControllerBase
{
    [HttpPost("residency-permit")]
    [RequirePermission(PermissionKeys.Documents.Upload)]
    public Task<IActionResult> Residency(Guid riderProfileId, [FromForm] EmployeeDocumentUploadForm request, CancellationToken cancellationToken) => Upload(riderProfileId, DocumentType.ResidencyPermitId, request, cancellationToken);

    [HttpPost("driver-license")]
    [RequirePermission(PermissionKeys.Documents.Upload)]
    public Task<IActionResult> DriverLicense(Guid riderProfileId, [FromForm] EmployeeDocumentUploadForm request, CancellationToken cancellationToken) => Upload(riderProfileId, DocumentType.DriverLicenseId, request, cancellationToken);

    [HttpPost("rider-card")]
    [RequirePermission(PermissionKeys.Documents.Upload)]
    public Task<IActionResult> RiderCard(Guid riderProfileId, [FromForm] EmployeeDocumentUploadForm request, CancellationToken cancellationToken) => Upload(riderProfileId, DocumentType.RiderCardId, request, cancellationToken);

    [HttpPost("health-card")]
    [RequirePermission(PermissionKeys.Documents.Upload)]
    public Task<IActionResult> HealthCard(Guid riderProfileId, [FromForm] EmployeeDocumentUploadForm request, CancellationToken cancellationToken) => Upload(riderProfileId, DocumentType.HealthCardId, request, cancellationToken);

    [HttpPost("promissory-note")]
    [RequirePermission(PermissionKeys.Documents.Upload)]
    public Task<IActionResult> PromissoryNote(Guid riderProfileId, [FromForm] EmployeeDocumentUploadForm request, CancellationToken cancellationToken) => Upload(riderProfileId, DocumentType.PromissoryNoteId, request, cancellationToken);

    [HttpPost("medical-insurance")]
    [RequirePermission(PermissionKeys.Documents.Upload)]
    public Task<IActionResult> MedicalInsurance(Guid riderProfileId, [FromForm] EmployeeDocumentUploadForm request, CancellationToken cancellationToken) => Upload(riderProfileId, DocumentType.MedicalInsuranceId, request, cancellationToken);

    private async Task<IActionResult> Upload(Guid riderProfileId, Guid documentTypeId, EmployeeDocumentUploadForm request, CancellationToken cancellationToken)
    {
        var file = EmployeeDocumentsController.CreateFile(request.File);
        if (file is null) return BadRequest();
        await using var stream = request.File.OpenReadStream();
        var result = await service.UploadForRiderAsync(riderProfileId, documentTypeId, request.ToMetadata(), file with { Content = stream }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

public sealed class EmployeeDocumentUploadForm
{
    public Guid DocumentTypeId { get; init; }
    public string? DocumentNumber { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public string? Notes { get; init; }
    public IFormFile File { get; init; } = null!;

    public EmployeeDocumentMetadataRequest ToMetadata() => new(DocumentNumber, IssueDate, ExpiryDate, Notes);
}

public sealed class DocumentVersionUploadForm
{
    public IFormFile File { get; init; } = null!;
}

public sealed record UpdateDocumentMetadataRequest(EmployeeDocumentMetadataRequest Metadata, string RowVersion);
