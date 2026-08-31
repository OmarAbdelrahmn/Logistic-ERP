using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public sealed record EmployeeDocumentMetadataRequest(
    string? DocumentNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? Notes);

public sealed record FileUploadContent(
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long Length);

public sealed record EmployeeDocumentResponse(
    Guid Id,
    Guid EmployeeId,
    Guid DocumentTypeId,
    string DocumentTypeCode,
    string DocumentTypeNameAr,
    string? DocumentNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string Status,
    string? Notes,
    Guid? CurrentVersionId,
    int? CurrentVersionNumber,
    string? CurrentFileName,
    string? CurrentContentType,
    long? CurrentFileSizeBytes,
    string RowVersion);

public sealed record EmployeeDocumentVersionResponse(
    Guid Id,
    Guid EmployeeDocumentId,
    int VersionNumber,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string Sha256Checksum,
    Guid UploadedByUserId,
    DateTimeOffset UploadedAtUtc);

public sealed record EmployeeDocumentChecklistItemResponse(
    Guid DocumentTypeId,
    string DocumentTypeCode,
    string DocumentTypeNameAr,
    string DocumentTypeNameEn,
    bool RequiresNumber,
    bool RequiresIssueDate,
    bool RequiresExpiryDate,
    bool RequiresFile,
    bool IsRequired,
    IReadOnlyList<int> ReminderOffsetsDays,
    string FulfillmentStatus,
    IReadOnlyList<string> MissingFields,
    IReadOnlyList<EmployeeDocumentResponse> Documents);

public sealed record DocumentDownloadResponse(
    Stream Content,
    string ContentType,
    string DownloadFileName,
    long Length);

public interface IEmployeeDocumentService
{
    Task<Result<IReadOnlyList<EmployeeDocumentResponse>>> GetEmployeeDocumentsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EmployeeDocumentChecklistItemResponse>>> GetEmployeeDocumentChecklistAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EmployeeDocumentChecklistItemResponse>>> GetRiderDocumentChecklistAsync(Guid riderProfileId, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDocumentResponse>> UploadAsync(Guid employeeId, Guid documentTypeId, EmployeeDocumentMetadataRequest metadata, FileUploadContent file, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDocumentResponse>> UploadForRiderAsync(Guid riderProfileId, Guid documentTypeId, EmployeeDocumentMetadataRequest metadata, FileUploadContent file, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDocumentResponse>> UploadNewVersionAsync(Guid employeeId, Guid documentId, FileUploadContent file, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDocumentResponse>> UpdateMetadataAsync(Guid employeeId, Guid documentId, EmployeeDocumentMetadataRequest request, string rowVersion, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EmployeeDocumentVersionResponse>>> GetVersionsAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken = default);
    Task<Result<DocumentDownloadResponse>> DownloadAsync(Guid employeeId, Guid documentId, Guid? versionId, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(Guid employeeId, Guid documentId, ArchiveRequest request, CancellationToken cancellationToken = default);
}
