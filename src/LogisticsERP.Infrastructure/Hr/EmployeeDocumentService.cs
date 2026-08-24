using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class EmployeeDocumentService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IPrivateFileStorage fileStorage) : IEmployeeDocumentService
{
    // The employee-documents API is temporarily available anonymously for Swagger verification.
    // Keep a deterministic actor on document-version history until controller authorization is restored.
    private static readonly Guid AnonymousEmployeeDocumentsApiActorId =
        Guid.Parse("019c18d5-62e1-7000-d000-000000000003");

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png", "image/webp", "image/gif", "image/bmp"
    };

    public async Task<Result<IReadOnlyList<EmployeeDocumentResponse>>> GetEmployeeDocumentsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Employees.AnyAsync(item => item.Id == employeeId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<EmployeeDocumentResponse>>(HrErrors.NotFound);
        }
        return Result.Success<IReadOnlyList<EmployeeDocumentResponse>>(await BuildDocuments(employeeId, cancellationToken));
    }

    public async Task<Result<EmployeeDocumentResponse>> UploadAsync(
        Guid employeeId,
        Guid documentTypeId,
        EmployeeDocumentMetadataRequest metadata,
        FileUploadContent file,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? AnonymousEmployeeDocumentsApiActorId;
        var employee = await dbContext.Employees.AsNoTracking().SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        var documentType = await dbContext.DocumentTypes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == documentTypeId && item.Status == CatalogStatus.Active, cancellationToken);
        if (employee is null || documentType is null)
        {
            return Result.Failure<EmployeeDocumentResponse>(HrErrors.NotFound);
        }
        if (!ValidateMetadata(documentType, metadata))
        {
            return Result.Failure<EmployeeDocumentResponse>(HrErrors.InvalidDocumentMetadata);
        }
        if (!ValidateFileDeclaration(documentType, file))
        {
            return Result.Failure<EmployeeDocumentResponse>(HrErrors.InvalidFile);
        }
        var documentNumber = HrServiceSupport.TrimOrNull(metadata.DocumentNumber);
        if (documentNumber is not null && await dbContext.EmployeeDocuments.AnyAsync(
                item => item.DocumentTypeId == documentTypeId && item.DocumentNumber == documentNumber && item.Status != DocumentStatus.Superseded,
                cancellationToken))
        {
            return Result.Failure<EmployeeDocumentResponse>(HrErrors.Duplicate);
        }

        var document = new EmployeeDocument
        {
            EmployeeId = employeeId,
            DocumentTypeId = documentTypeId,
            DocumentNumber = documentNumber,
            IssueDate = metadata.IssueDate,
            ExpiryDate = metadata.ExpiryDate,
            Status = DocumentStatus.Active,
            Notes = HrServiceSupport.TrimOrNull(metadata.Notes)
        };
        dbContext.EmployeeDocuments.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        var versionId = Guid.CreateVersion7();
        var stored = await StoreFileAsync(employeeId, document.Id, versionId, documentType.MaxFileSizeBytes, file, cancellationToken);
        if (stored.IsFailure)
        {
            document.IsDeleted = true;
            document.DeletionReason = "The initial document file failed validation.";
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure<EmployeeDocumentResponse>(stored.Error);
        }
        var version = CreateVersion(versionId, document.Id, 1, userId, file, stored.Value!, null);
        dbContext.EmployeeDocumentVersions.Add(version);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            document.CurrentVersionId = version.Id;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            fileStorage.DeleteBestEffort(stored.Value!.StoragePath);
            throw;
        }
        return (await GetEmployeeDocumentsAsync(employeeId, cancellationToken)).MapSingle(item => item.Id == document.Id);
    }

    public async Task<Result<EmployeeDocumentResponse>> UploadForRiderAsync(
        Guid riderProfileId,
        Guid documentTypeId,
        EmployeeDocumentMetadataRequest metadata,
        FileUploadContent file,
        CancellationToken cancellationToken = default)
    {
        var employeeId = await dbContext.RiderProfiles.AsNoTracking()
            .Where(item => item.Id == riderProfileId)
            .Select(item => (Guid?)item.EmployeeId)
            .SingleOrDefaultAsync(cancellationToken);
        return employeeId is null
            ? Result.Failure<EmployeeDocumentResponse>(HrErrors.NotFound)
            : await UploadAsync(employeeId.Value, documentTypeId, metadata, file, cancellationToken);
    }

    public async Task<Result<EmployeeDocumentResponse>> UploadNewVersionAsync(
        Guid employeeId,
        Guid documentId,
        FileUploadContent file,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? AnonymousEmployeeDocumentsApiActorId;
        var document = await dbContext.EmployeeDocuments.SingleOrDefaultAsync(item => item.Id == documentId && item.EmployeeId == employeeId, cancellationToken);
        if (document is null)
        {
            return Result.Failure<EmployeeDocumentResponse>(HrErrors.NotFound);
        }
        var documentType = await dbContext.DocumentTypes.AsNoTracking().SingleAsync(item => item.Id == document.DocumentTypeId, cancellationToken);
        if (!ValidateFileDeclaration(documentType, file))
        {
            return Result.Failure<EmployeeDocumentResponse>(HrErrors.InvalidFile);
        }
        var versionNumber = await dbContext.EmployeeDocumentVersions
            .Where(item => item.EmployeeDocumentId == documentId)
            .MaxAsync(item => (int?)item.VersionNumber, cancellationToken) + 1 ?? 1;
        var versionId = Guid.CreateVersion7();
        var stored = await StoreFileAsync(employeeId, document.Id, versionId, documentType.MaxFileSizeBytes, file, cancellationToken);
        if (stored.IsFailure)
        {
            return Result.Failure<EmployeeDocumentResponse>(stored.Error);
        }
        var version = CreateVersion(versionId, document.Id, versionNumber, userId, file, stored.Value!, document.CurrentVersionId);
        dbContext.EmployeeDocumentVersions.Add(version);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            document.CurrentVersionId = version.Id;
            document.Status = DocumentStatus.Active;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            fileStorage.DeleteBestEffort(stored.Value!.StoragePath);
            throw;
        }
        return (await GetEmployeeDocumentsAsync(employeeId, cancellationToken)).MapSingle(item => item.Id == document.Id);
    }

    public async Task<Result<EmployeeDocumentResponse>> UpdateMetadataAsync(
        Guid employeeId,
        Guid documentId,
        EmployeeDocumentMetadataRequest request,
        string rowVersion,
        CancellationToken cancellationToken = default)
    {
        var document = await dbContext.EmployeeDocuments.SingleOrDefaultAsync(item => item.Id == documentId && item.EmployeeId == employeeId, cancellationToken);
        if (document is null)
        {
            return Result.Failure<EmployeeDocumentResponse>(HrErrors.NotFound);
        }
        if (!HrServiceSupport.MatchesRowVersion(document.RowVersion, rowVersion))
        {
            return Result.Failure<EmployeeDocumentResponse>(HrErrors.ConcurrencyConflict);
        }
        var type = await dbContext.DocumentTypes.AsNoTracking().SingleAsync(item => item.Id == document.DocumentTypeId, cancellationToken);
        if (!ValidateMetadata(type, request))
        {
            return Result.Failure<EmployeeDocumentResponse>(HrErrors.InvalidRequest);
        }
        var documentNumber = HrServiceSupport.TrimOrNull(request.DocumentNumber);
        if (documentNumber is not null && await dbContext.EmployeeDocuments.AnyAsync(item => item.Id != documentId && item.DocumentTypeId == document.DocumentTypeId && item.DocumentNumber == documentNumber && item.Status != DocumentStatus.Superseded, cancellationToken))
        {
            return Result.Failure<EmployeeDocumentResponse>(HrErrors.Duplicate);
        }
        document.DocumentNumber = documentNumber;
        document.IssueDate = request.IssueDate;
        document.ExpiryDate = request.ExpiryDate;
        document.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetEmployeeDocumentsAsync(employeeId, cancellationToken)).MapSingle(item => item.Id == document.Id);
    }

    public async Task<Result<IReadOnlyList<EmployeeDocumentVersionResponse>>> GetVersionsAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.EmployeeDocuments.AnyAsync(item => item.Id == documentId && item.EmployeeId == employeeId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<EmployeeDocumentVersionResponse>>(HrErrors.NotFound);
        }
        var versions = await dbContext.EmployeeDocumentVersions.AsNoTracking()
            .Where(item => item.EmployeeDocumentId == documentId)
            .OrderByDescending(item => item.VersionNumber)
            .Select(item => new EmployeeDocumentVersionResponse(item.Id, item.EmployeeDocumentId, item.VersionNumber,
                item.OriginalFileName, item.ContentType, item.FileSizeBytes, item.Sha256Checksum,
                item.UploadedByUserId, item.UploadedAtUtc))
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<EmployeeDocumentVersionResponse>>(versions);
    }

    public async Task<Result<DocumentDownloadResponse>> DownloadAsync(Guid employeeId, Guid documentId, Guid? versionId, CancellationToken cancellationToken = default)
    {
        var version = await (from document in dbContext.EmployeeDocuments.AsNoTracking()
                             join item in dbContext.EmployeeDocumentVersions.AsNoTracking() on document.Id equals item.EmployeeDocumentId
                             where document.Id == documentId && document.EmployeeId == employeeId
                                 && (versionId == null && document.CurrentVersionId == item.Id || versionId == item.Id)
                             select item).SingleOrDefaultAsync(cancellationToken);
        if (version is null)
        {
            return Result.Failure<DocumentDownloadResponse>(HrErrors.NotFound);
        }
        var stored = await fileStorage.OpenReadAsync(version.StoragePath, version.ContentType, version.OriginalFileName, version.FileSizeBytes, cancellationToken);
        return stored.IsFailure
            ? Result.Failure<DocumentDownloadResponse>(HrErrors.FileMissing)
            : Result.Success(new DocumentDownloadResponse(stored.Value!.Content, stored.Value.ContentType, stored.Value.DownloadFileName, stored.Value.Length));
    }

    public async Task<Result> ArchiveAsync(Guid employeeId, Guid documentId, ArchiveRequest request, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.EmployeeDocuments.SingleOrDefaultAsync(item => item.Id == documentId && item.EmployeeId == employeeId, cancellationToken);
        if (document is null)
        {
            return Result.Failure(HrErrors.NotFound);
        }
        if (!HrServiceSupport.HasText(request.Reason) || !HrServiceSupport.MatchesRowVersion(document.RowVersion, request.RowVersion))
        {
            return Result.Failure(HrErrors.ConcurrencyConflict);
        }
        document.Status = DocumentStatus.Archived;
        document.IsDeleted = true;
        document.DeletionReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<EmployeeDocumentResponse[]> BuildDocuments(Guid employeeId, CancellationToken cancellationToken)
    {
        var rows = await (from document in dbContext.EmployeeDocuments.AsNoTracking()
                          join type in dbContext.DocumentTypes.AsNoTracking() on document.DocumentTypeId equals type.Id
                          join version in dbContext.EmployeeDocumentVersions.AsNoTracking() on document.CurrentVersionId equals version.Id into versions
                          from version in versions.DefaultIfEmpty()
                          where document.EmployeeId == employeeId
                          orderby type.NameAr, document.CreatedAtUtc descending
                          select new EmployeeDocumentProjection(document, type, version))
            .ToArrayAsync(cancellationToken);

        return rows.Select(row => new EmployeeDocumentResponse(row.Document.Id, row.Document.EmployeeId,
            row.Type.Id, row.Type.Code, row.Type.NameAr, row.Document.DocumentNumber,
            row.Document.IssueDate, row.Document.ExpiryDate, row.Document.Status.ToString(), row.Document.Notes,
            row.Document.CurrentVersionId, row.Version?.VersionNumber, row.Version?.OriginalFileName,
            row.Version?.ContentType, row.Version?.FileSizeBytes,
            Convert.ToBase64String(row.Document.RowVersion))).ToArray();
    }

    private async Task<Result<StoredFile>> StoreFileAsync(Guid employeeId, Guid documentId, Guid versionId, long maxSize, FileUploadContent file, CancellationToken cancellationToken)
    {
        var result = await fileStorage.StoreAsync(
            $"employee-documents/{employeeId:N}/{documentId:N}/{versionId:N}",
            new PrivateFileUpload(file.Content, file.OriginalFileName, file.ContentType, file.Length),
            maxSize,
            cancellationToken);
        return result.IsFailure
            ? Result.Failure<StoredFile>(HrErrors.InvalidFile)
            : Result.Success(new StoredFile(result.Value!.StoragePath, result.Value.StoredFileName, result.Value.Length, result.Value.Sha256Checksum));
    }

    private static EmployeeDocumentVersion CreateVersion(Guid id, Guid documentId, int number, Guid userId,
        FileUploadContent file, StoredFile stored, Guid? supersededVersionId) => new()
    {
        Id = id,
        EmployeeDocumentId = documentId,
        VersionNumber = number,
        OriginalFileName = Path.GetFileName(file.OriginalFileName),
        StoredFileName = stored.StoredName,
        ContentType = file.ContentType.ToLowerInvariant(),
        FileSizeBytes = stored.Length,
        Sha256Checksum = stored.Checksum,
        StoragePath = stored.StoragePath,
        UploadedByUserId = userId,
        UploadedAtUtc = DateTimeOffset.UtcNow,
        SupersededVersionId = supersededVersionId
    };

    private static bool ValidateMetadata(DocumentType type, EmployeeDocumentMetadataRequest metadata) =>
        (!type.RequiresNumber || HrServiceSupport.HasText(metadata.DocumentNumber))
        && (!type.RequiresIssueDate || metadata.IssueDate is not null)
        && (!type.RequiresExpiryDate || metadata.ExpiryDate is not null)
        && (metadata.ExpiryDate is null || metadata.IssueDate is null || metadata.ExpiryDate >= metadata.IssueDate);

    private static bool ValidateFileDeclaration(DocumentType type, FileUploadContent file)
    {
        var allowedByType = type.AllowedMimeTypes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return file.Length is > 0
            && file.Length <= type.MaxFileSizeBytes
            && AllowedContentTypes.Contains(file.ContentType)
            && (allowedByType.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)
                || file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) && AllowedContentTypes.Contains(file.ContentType))
            && HrServiceSupport.HasText(file.OriginalFileName);
    }

    private sealed record EmployeeDocumentProjection(
        EmployeeDocument Document,
        DocumentType Type,
        EmployeeDocumentVersion? Version);

    private sealed record StoredFile(string StoragePath, string StoredName, long Length, string Checksum);
}
