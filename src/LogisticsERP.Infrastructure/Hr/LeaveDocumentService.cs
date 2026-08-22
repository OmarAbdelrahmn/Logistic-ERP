using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class LeaveDocumentService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IPrivateFileStorage fileStorage,
    TimeProvider timeProvider) : ILeaveDocumentService
{
    private const long MaximumFileBytes = 10 * 1024 * 1024;

    public async Task<Result<IReadOnlyList<LeaveDocumentResponse>>> GetAsync(
        Guid leaveRequestId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.LeaveRequests.AsNoTracking().AnyAsync(item => item.Id == leaveRequestId, cancellationToken))
            return Result.Failure<IReadOnlyList<LeaveDocumentResponse>>(HrErrors.NotFound);
        return Result.Success<IReadOnlyList<LeaveDocumentResponse>>(await BuildAsync(leaveRequestId, cancellationToken));
    }

    public async Task<Result<LeaveDocumentResponse>> UploadAsync(
        Guid leaveRequestId,
        LeaveDocumentMetadataRequest metadata,
        FileUploadContent file,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId
            || !ValidateMetadata(metadata, out var kind)
            || !await dbContext.LeaveRequests.AnyAsync(item => item.Id == leaveRequestId, cancellationToken))
            return Result.Failure<LeaveDocumentResponse>(HrErrors.InvalidRequest);

        var document = new LeaveRequestDocument
        {
            LeaveRequestId = leaveRequestId,
            Kind = kind,
            ReferenceNumber = HrServiceSupport.TrimOrNull(metadata.ReferenceNumber),
            IssuedOn = metadata.IssuedOn,
            ExpiresOn = metadata.ExpiresOn,
            Notes = HrServiceSupport.TrimOrNull(metadata.Notes)
        };
        dbContext.LeaveRequestDocuments.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        var versionId = Guid.CreateVersion7();
        var stored = await StoreAsync(leaveRequestId, document.Id, versionId, file, cancellationToken);
        if (stored.IsFailure)
        {
            document.IsDeleted = true;
            document.DeletionReason = "The initial leave document file failed validation.";
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure<LeaveDocumentResponse>(HrErrors.InvalidFile);
        }
        var version = CreateVersion(versionId, document.Id, 1, userId, stored.Value!, null);
        dbContext.LeaveRequestDocumentVersions.Add(version);
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
        return (await GetAsync(leaveRequestId, cancellationToken)).MapSingle(item => item.Id == document.Id);
    }

    public async Task<Result<LeaveDocumentResponse>> UploadNewVersionAsync(
        Guid leaveRequestId,
        Guid documentId,
        FileUploadContent file,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure<LeaveDocumentResponse>(HrErrors.CurrentUserUnavailable);
        var document = await dbContext.LeaveRequestDocuments.SingleOrDefaultAsync(
            item => item.Id == documentId && item.LeaveRequestId == leaveRequestId,
            cancellationToken);
        if (document is null) return Result.Failure<LeaveDocumentResponse>(HrErrors.NotFound);
        var number = await dbContext.LeaveRequestDocumentVersions
            .Where(item => item.LeaveRequestDocumentId == documentId)
            .MaxAsync(item => (int?)item.VersionNumber, cancellationToken) + 1 ?? 1;
        var versionId = Guid.CreateVersion7();
        var stored = await StoreAsync(leaveRequestId, document.Id, versionId, file, cancellationToken);
        if (stored.IsFailure) return Result.Failure<LeaveDocumentResponse>(HrErrors.InvalidFile);
        var version = CreateVersion(versionId, document.Id, number, userId, stored.Value!, document.CurrentVersionId);
        dbContext.LeaveRequestDocumentVersions.Add(version);
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
        return (await GetAsync(leaveRequestId, cancellationToken)).MapSingle(item => item.Id == document.Id);
    }

    public async Task<Result<LeaveDocumentResponse>> UpdateMetadataAsync(
        Guid leaveRequestId,
        Guid documentId,
        LeaveDocumentMetadataRequest request,
        string rowVersion,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateMetadata(request, out var kind)) return Result.Failure<LeaveDocumentResponse>(HrErrors.InvalidRequest);
        var document = await dbContext.LeaveRequestDocuments.SingleOrDefaultAsync(
            item => item.Id == documentId && item.LeaveRequestId == leaveRequestId,
            cancellationToken);
        if (document is null) return Result.Failure<LeaveDocumentResponse>(HrErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(document.RowVersion, rowVersion))
            return Result.Failure<LeaveDocumentResponse>(HrErrors.ConcurrencyConflict);
        document.Kind = kind;
        document.ReferenceNumber = HrServiceSupport.TrimOrNull(request.ReferenceNumber);
        document.IssuedOn = request.IssuedOn;
        document.ExpiresOn = request.ExpiresOn;
        document.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAsync(leaveRequestId, cancellationToken)).MapSingle(item => item.Id == document.Id);
    }

    public async Task<Result<IReadOnlyList<LeaveDocumentVersionResponse>>> GetVersionsAsync(
        Guid leaveRequestId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.LeaveRequestDocuments.AnyAsync(
            item => item.Id == documentId && item.LeaveRequestId == leaveRequestId,
            cancellationToken))
            return Result.Failure<IReadOnlyList<LeaveDocumentVersionResponse>>(HrErrors.NotFound);
        var rows = await dbContext.LeaveRequestDocumentVersions.AsNoTracking()
            .Where(item => item.LeaveRequestDocumentId == documentId)
            .OrderByDescending(item => item.VersionNumber)
            .Select(item => new LeaveDocumentVersionResponse(
                item.Id, item.LeaveRequestDocumentId, item.VersionNumber, item.OriginalFileName,
                item.ContentType, item.FileSizeBytes, item.Sha256Checksum, item.UploadedByUserId, item.UploadedAtUtc))
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<LeaveDocumentVersionResponse>>(rows);
    }

    public async Task<Result<DocumentDownloadResponse>> DownloadAsync(
        Guid leaveRequestId,
        Guid documentId,
        Guid? versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await (from document in dbContext.LeaveRequestDocuments.AsNoTracking()
                             join item in dbContext.LeaveRequestDocumentVersions.AsNoTracking()
                                 on document.Id equals item.LeaveRequestDocumentId
                             where document.Id == documentId && document.LeaveRequestId == leaveRequestId
                                 && (versionId == null && document.CurrentVersionId == item.Id || versionId == item.Id)
                             select item).SingleOrDefaultAsync(cancellationToken);
        if (version is null) return Result.Failure<DocumentDownloadResponse>(HrErrors.NotFound);
        var file = await fileStorage.OpenReadAsync(
            version.StoragePath, version.ContentType, version.OriginalFileName, version.FileSizeBytes, cancellationToken);
        return file.IsFailure
            ? Result.Failure<DocumentDownloadResponse>(HrErrors.FileMissing)
            : Result.Success(new DocumentDownloadResponse(
                file.Value!.Content, file.Value.ContentType, file.Value.DownloadFileName, file.Value.Length));
    }

    public async Task<Result> ArchiveAsync(
        Guid leaveRequestId,
        Guid documentId,
        ArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var document = await dbContext.LeaveRequestDocuments.SingleOrDefaultAsync(
            item => item.Id == documentId && item.LeaveRequestId == leaveRequestId,
            cancellationToken);
        if (document is null) return Result.Failure(HrErrors.NotFound);
        if (string.IsNullOrWhiteSpace(request.Reason)
            || !HrServiceSupport.MatchesRowVersion(document.RowVersion, request.RowVersion))
            return Result.Failure(HrErrors.ConcurrencyConflict);
        document.DeletionReason = request.Reason.Trim();
        dbContext.LeaveRequestDocuments.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<LeaveDocumentResponse[]> BuildAsync(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        var rows = await (from document in dbContext.LeaveRequestDocuments.AsNoTracking()
                          join version in dbContext.LeaveRequestDocumentVersions.AsNoTracking()
                              on document.CurrentVersionId equals version.Id into versions
                          from version in versions.DefaultIfEmpty()
                          where document.LeaveRequestId == leaveRequestId
                          orderby document.Kind, document.CreatedAtUtc descending
                          select new LeaveDocumentProjection(document, version)).ToArrayAsync(cancellationToken);
        return rows.Select(row => new LeaveDocumentResponse(
            row.Document.Id, row.Document.LeaveRequestId, row.Document.Kind.ToString(),
            row.Document.ReferenceNumber, row.Document.IssuedOn, row.Document.ExpiresOn, row.Document.Notes,
            row.Document.CurrentVersionId, row.Version?.VersionNumber, row.Version?.OriginalFileName,
            row.Version?.ContentType, row.Version?.FileSizeBytes,
            HrServiceSupport.EncodeRowVersion(row.Document.RowVersion))).ToArray();
    }

    private async Task<Result<StoredPrivateFile>> StoreAsync(
        Guid leaveRequestId,
        Guid documentId,
        Guid versionId,
        FileUploadContent file,
        CancellationToken cancellationToken) =>
        await fileStorage.StoreAsync(
            $"leave-request-documents/{leaveRequestId:N}/{documentId:N}/{versionId:N}",
            new PrivateFileUpload(file.Content, file.OriginalFileName, file.ContentType, file.Length),
            MaximumFileBytes,
            cancellationToken);

    private LeaveRequestDocumentVersion CreateVersion(
        Guid id,
        Guid documentId,
        int number,
        Guid userId,
        StoredPrivateFile stored,
        Guid? supersededVersionId) => new()
    {
        Id = id,
        LeaveRequestDocumentId = documentId,
        VersionNumber = number,
        OriginalFileName = stored.OriginalFileName,
        ContentType = stored.ContentType,
        FileSizeBytes = stored.Length,
        Sha256Checksum = stored.Sha256Checksum,
        StoragePath = stored.StoragePath,
        UploadedByUserId = userId,
        UploadedAtUtc = timeProvider.GetUtcNow(),
        SupersededVersionId = supersededVersionId
    };

    private static bool ValidateMetadata(LeaveDocumentMetadataRequest request, out LeaveDocumentKind kind)
    {
        kind = default;
        return Enum.TryParse(request.Kind, true, out kind)
            && (request.ExpiresOn is null || request.IssuedOn is null || request.ExpiresOn >= request.IssuedOn)
            && request.ReferenceNumber?.Trim().Length is not > 150
            && request.Notes?.Trim().Length is not > 2000;
    }

    private sealed record LeaveDocumentProjection(
        LeaveRequestDocument Document,
        LeaveRequestDocumentVersion? Version);
}

