using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed class VehicleFileService(
    ApplicationDbContext dbContext,
    FleetServiceSupport support,
    IPrivateFileStorage fileStorage) : IVehicleFileService
{
    private const long MaximumFileSize = 10 * 1024 * 1024;

    public async Task<Result<IReadOnlyList<VehicleAttachmentResponse>>> GetAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == vehicleId, cancellationToken);
        if (vehicle is null) return Result.Failure<IReadOnlyList<VehicleAttachmentResponse>>(FleetErrors.NotFound);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.FilesRead, cancellationToken)) return Result.Failure<IReadOnlyList<VehicleAttachmentResponse>>(FleetErrors.Forbidden);
        return Result.Success<IReadOnlyList<VehicleAttachmentResponse>>(await BuildAsync(vehicleId, cancellationToken));
    }

    public async Task<Result<VehicleAttachmentResponse>> UploadAsync(Guid vehicleId, Guid? attachmentId, VehicleAttachmentCategory category, string displayName, PrivateFileUpload file, CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == vehicleId, cancellationToken);
        if (vehicle is null) return Result.Failure<VehicleAttachmentResponse>(FleetErrors.NotFound);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.FilesUpload, cancellationToken)) return Result.Failure<VehicleAttachmentResponse>(FleetErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(displayName)) return Result.Failure<VehicleAttachmentResponse>(FleetErrors.InvalidRequest);
        VehicleAttachment? attachment = null;
        if (attachmentId.HasValue)
        {
            attachment = await dbContext.VehicleAttachments.SingleOrDefaultAsync(x => x.Id == attachmentId && x.VehicleId == vehicleId, cancellationToken);
            if (attachment is null) return Result.Failure<VehicleAttachmentResponse>(FleetErrors.NotFound);
        }
        else if (await dbContext.VehicleAttachments.CountAsync(x => x.VehicleId == vehicleId, cancellationToken) >= 5)
        {
            return Result.Failure<VehicleAttachmentResponse>(FleetErrors.FileLimit);
        }

        attachment ??= new VehicleAttachment { VehicleId = vehicleId };
        var versionId = Guid.CreateVersion7();
        var stored = await fileStorage.StoreAsync($"vehicles/{vehicleId:N}/{attachment.Id:N}/{versionId:N}", file, MaximumFileSize, cancellationToken);
        if (stored.IsFailure) return Result.Failure<VehicleAttachmentResponse>(FleetErrors.InvalidFile);
        var number = await dbContext.VehicleAttachmentVersions.Where(x => x.VehicleAttachmentId == attachment.Id).MaxAsync(x => (int?)x.VersionNumber, cancellationToken) + 1 ?? 1;
        var version = new VehicleAttachmentVersion
        {
            Id = versionId, VehicleAttachmentId = attachment.Id, VersionNumber = number, OriginalFileName = stored.Value!.OriginalFileName,
            StoredFileName = stored.Value.StoredFileName, ContentType = stored.Value.ContentType, FileSizeBytes = stored.Value.Length,
            Sha256Checksum = stored.Value.Sha256Checksum, StoragePath = stored.Value.StoragePath, UploadedByUserId = support.UserId!.Value,
            UploadedAtUtc = support.UtcNow, SupersededVersionId = attachment.CurrentVersionId
        };
        attachment.Category = category; attachment.DisplayName = displayName.Trim();
        if (!attachmentId.HasValue) dbContext.VehicleAttachments.Add(attachment);
        dbContext.VehicleAttachmentVersions.Add(version);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            attachment.CurrentVersionId = version.Id;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            fileStorage.DeleteBestEffort(stored.Value.StoragePath);
            throw;
        }
        return Result.Success((await BuildAsync(vehicleId, cancellationToken)).Single(x => x.Id == attachment.Id));
    }

    public async Task<Result<IReadOnlyList<VehicleAttachmentVersionResponse>>> GetVersionsAsync(Guid vehicleId, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var access = await GetAttachmentAsync(vehicleId, attachmentId, PermissionKeys.Fleet.FilesRead, cancellationToken);
        if (access.IsFailure) return Result.Failure<IReadOnlyList<VehicleAttachmentVersionResponse>>(access.Error);
        var items = await dbContext.VehicleAttachmentVersions.AsNoTracking().Where(x => x.VehicleAttachmentId == attachmentId).OrderByDescending(x => x.VersionNumber)
            .Select(x => new VehicleAttachmentVersionResponse(x.Id, x.VehicleAttachmentId, x.VersionNumber, x.OriginalFileName, x.ContentType, x.FileSizeBytes, x.Sha256Checksum, x.UploadedAtUtc)).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<VehicleAttachmentVersionResponse>>(items);
    }

    public async Task<Result<PrivateFileDownload>> DownloadAsync(Guid vehicleId, Guid attachmentId, Guid? versionId, CancellationToken cancellationToken = default)
    {
        var access = await GetAttachmentAsync(vehicleId, attachmentId, PermissionKeys.Fleet.FilesDownload, cancellationToken);
        if (access.IsFailure) return Result.Failure<PrivateFileDownload>(access.Error);
        var version = await dbContext.VehicleAttachmentVersions.AsNoTracking().SingleOrDefaultAsync(x => x.VehicleAttachmentId == attachmentId && (versionId.HasValue ? x.Id == versionId : x.Id == access.Value!.CurrentVersionId), cancellationToken);
        if (version is null) return Result.Failure<PrivateFileDownload>(FleetErrors.NotFound);
        var result = await fileStorage.OpenReadAsync(version.StoragePath, version.ContentType, version.OriginalFileName, version.FileSizeBytes, cancellationToken);
        return result.IsFailure ? Result.Failure<PrivateFileDownload>(FleetErrors.FileMissing) : result;
    }

    public async Task<Result> ArchiveAsync(Guid vehicleId, Guid attachmentId, ArchiveFleetRequest request, CancellationToken cancellationToken = default)
    {
        var access = await GetAttachmentAsync(vehicleId, attachmentId, PermissionKeys.Fleet.FilesUpload, cancellationToken);
        if (access.IsFailure) return Result.Failure(access.Error);
        if (string.IsNullOrWhiteSpace(request.Reason) || !FleetServiceSupport.MatchesRowVersion(access.Value!.RowVersion, request.RowVersion)) return Result.Failure(FleetErrors.ConcurrencyConflict);
        access.Value.IsDeleted = true; access.Value.DeletionReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result<VehicleAttachment>> GetAttachmentAsync(Guid vehicleId, Guid attachmentId, string permission, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == vehicleId, cancellationToken);
        if (vehicle is null) return Result.Failure<VehicleAttachment>(FleetErrors.NotFound);
        if (!await support.HasVehiclePermissionAsync(vehicle, permission, cancellationToken)) return Result.Failure<VehicleAttachment>(FleetErrors.Forbidden);
        var attachment = await dbContext.VehicleAttachments.SingleOrDefaultAsync(x => x.Id == attachmentId && x.VehicleId == vehicleId, cancellationToken);
        return attachment is null ? Result.Failure<VehicleAttachment>(FleetErrors.NotFound) : Result.Success(attachment);
    }

    private async Task<VehicleAttachmentResponse[]> BuildAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var rows = await (from attachment in dbContext.VehicleAttachments.AsNoTracking()
                          join version in dbContext.VehicleAttachmentVersions.AsNoTracking() on attachment.CurrentVersionId equals version.Id into versions
                          from version in versions.DefaultIfEmpty()
                          where attachment.VehicleId == vehicleId
                          orderby attachment.CreatedAtUtc
                          select new { attachment, version }).ToArrayAsync(cancellationToken);
        return rows.Select(x => new VehicleAttachmentResponse(x.attachment.Id, x.attachment.VehicleId, x.attachment.Category, x.attachment.DisplayName,
            x.attachment.CurrentVersionId, x.version == null ? null : x.version.VersionNumber, x.version == null ? null : x.version.OriginalFileName,
            x.version == null ? null : x.version.ContentType, x.version == null ? null : x.version.FileSizeBytes, FleetServiceSupport.EncodeRowVersion(x.attachment.RowVersion))).ToArray();
    }
}
