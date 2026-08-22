using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public sealed record LeaveDocumentMetadataRequest(
    string Kind,
    string? ReferenceNumber,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    string? Notes);

public sealed record LeaveDocumentResponse(
    Guid Id,
    Guid LeaveRequestId,
    string Kind,
    string? ReferenceNumber,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    string? Notes,
    Guid? CurrentVersionId,
    int? CurrentVersionNumber,
    string? CurrentFileName,
    string? CurrentContentType,
    long? CurrentFileSizeBytes,
    string RowVersion);

public sealed record LeaveDocumentVersionResponse(
    Guid Id,
    Guid LeaveRequestDocumentId,
    int VersionNumber,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string Sha256Checksum,
    Guid UploadedByUserId,
    DateTimeOffset UploadedAtUtc);

public interface ILeaveDocumentService
{
    Task<Result<IReadOnlyList<LeaveDocumentResponse>>> GetAsync(Guid leaveRequestId, CancellationToken cancellationToken = default);
    Task<Result<LeaveDocumentResponse>> UploadAsync(Guid leaveRequestId, LeaveDocumentMetadataRequest metadata, FileUploadContent file, CancellationToken cancellationToken = default);
    Task<Result<LeaveDocumentResponse>> UploadNewVersionAsync(Guid leaveRequestId, Guid documentId, FileUploadContent file, CancellationToken cancellationToken = default);
    Task<Result<LeaveDocumentResponse>> UpdateMetadataAsync(Guid leaveRequestId, Guid documentId, LeaveDocumentMetadataRequest request, string rowVersion, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<LeaveDocumentVersionResponse>>> GetVersionsAsync(Guid leaveRequestId, Guid documentId, CancellationToken cancellationToken = default);
    Task<Result<DocumentDownloadResponse>> DownloadAsync(Guid leaveRequestId, Guid documentId, Guid? versionId, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(Guid leaveRequestId, Guid documentId, ArchiveRequest request, CancellationToken cancellationToken = default);
}

