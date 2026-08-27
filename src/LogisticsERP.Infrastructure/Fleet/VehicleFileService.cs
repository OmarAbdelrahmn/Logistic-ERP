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

    public async Task<Result<VehicleAttachmentResponse>> UploadSlotAsync(Guid vehicleId, VehicleFileKind kind, PrivateFileUpload file, CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == vehicleId, cancellationToken);
        if (vehicle is null) return Result.Failure<VehicleAttachmentResponse>(FleetErrors.NotFound);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.FilesUpload, cancellationToken)) return Result.Failure<VehicleAttachmentResponse>(FleetErrors.Forbidden);
        if (kind == VehicleFileKind.Legacy || !Enum.IsDefined(kind) || IsImage(kind) != file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) && IsImage(kind)) return Result.Failure<VehicleAttachmentResponse>(FleetErrors.InvalidFile);
        if (kind == VehicleFileKind.OperationCard && vehicle.RegistrationType != VehicleRegistrationType.PublicTransport) return Result.Failure<VehicleAttachmentResponse>(FleetErrors.InvalidState);
        var attachment = await dbContext.VehicleAttachments.SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.Kind == kind, cancellationToken)
            ?? new VehicleAttachment { VehicleId = vehicleId, Kind = kind, DisplayName = DisplayName(kind) };
        var isNew = dbContext.Entry(attachment).State == EntityState.Detached;
        var versionId = Guid.CreateVersion7();
        var stored = await fileStorage.StoreAsync($"vehicles/{vehicleId:N}/{attachment.Id:N}/{versionId:N}", file, MaximumFileSize, cancellationToken);
        if (stored.IsFailure) return Result.Failure<VehicleAttachmentResponse>(FleetErrors.InvalidFile);
        try
        {
            await dbContext.ExecuteTransactionAsync(async _ =>
            {
                var number = await dbContext.VehicleAttachmentVersions.Where(x => x.VehicleAttachmentId == attachment.Id).MaxAsync(x => (int?)x.VersionNumber, cancellationToken) + 1 ?? 1;
                var version = new VehicleAttachmentVersion
                {
                    Id = versionId, VehicleAttachmentId = attachment.Id, VersionNumber = number, OriginalFileName = stored.Value!.OriginalFileName,
                    StoredFileName = stored.Value.StoredFileName, ContentType = stored.Value.ContentType, FileSizeBytes = stored.Value.Length,
                    Sha256Checksum = stored.Value.Sha256Checksum, StoragePath = stored.Value.StoragePath, UploadedByUserId = support.UserId!.Value,
                    UploadedAtUtc = support.UtcNow, SupersededVersionId = attachment.CurrentVersionId
                };
                if (isNew) dbContext.VehicleAttachments.Add(attachment);
                dbContext.VehicleAttachmentVersions.Add(version);
                await dbContext.SaveChangesAsync(cancellationToken);
                attachment.CurrentVersionId = version.Id;
                await dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }, cancellationToken);
        }
        catch (DbUpdateException)
        {
            fileStorage.DeleteBestEffort(stored.Value!.StoragePath);
            return Result.Failure<VehicleAttachmentResponse>(FleetErrors.Conflict);
        }
        catch
        {
            fileStorage.DeleteBestEffort(stored.Value!.StoragePath);
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

    public async Task<Result<IReadOnlyList<RiderPromissoryFileResponse>>> GetRiderPromissoryFilesAsync(Guid riderProfileId, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsRead, null, cancellationToken)) return Result.Failure<IReadOnlyList<RiderPromissoryFileResponse>>(FleetErrors.Forbidden);
        if (!await dbContext.RiderProfiles.AsNoTracking().AnyAsync(x => x.Id == riderProfileId, cancellationToken)) return Result.Failure<IReadOnlyList<RiderPromissoryFileResponse>>(FleetErrors.NotFound);
        var rows = await (from file in dbContext.RiderPromissoryFiles.AsNoTracking()
                          join version in dbContext.RiderPromissoryFileVersions.AsNoTracking() on file.CurrentVersionId equals version.Id
                          where file.RiderProfileId == riderProfileId
                          orderby file.CreatedAtUtc
                          select new { file, version }).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<RiderPromissoryFileResponse>>(rows.Select(x => new RiderPromissoryFileResponse(x.file.Id, x.file.RiderProfileId, x.version.Id, x.version.VersionNumber, x.version.OriginalFileName, x.version.ContentType, x.version.FileSizeBytes, x.version.Sha256Checksum, x.version.UploadedAtUtc, FleetServiceSupport.EncodeRowVersion(x.file.RowVersion))).ToArray());
    }

    public async Task<Result<PrivateFileDownload>> DownloadRiderPromissoryFileAsync(Guid riderProfileId, Guid fileId, Guid? versionId, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.FilesDownload, null, cancellationToken)) return Result.Failure<PrivateFileDownload>(FleetErrors.Forbidden);
        var file = await dbContext.RiderPromissoryFiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == fileId && x.RiderProfileId == riderProfileId, cancellationToken);
        if (file is null) return Result.Failure<PrivateFileDownload>(FleetErrors.NotFound);
        var version = await dbContext.RiderPromissoryFileVersions.AsNoTracking().SingleOrDefaultAsync(x => x.RiderPromissoryFileId == fileId && x.Id == (versionId ?? file.CurrentVersionId), cancellationToken);
        if (version is null) return Result.Failure<PrivateFileDownload>(FleetErrors.NotFound);
        var result = await fileStorage.OpenReadAsync(version.StoragePath, version.ContentType, version.OriginalFileName, version.FileSizeBytes, cancellationToken);
        return result.IsFailure ? Result.Failure<PrivateFileDownload>(FleetErrors.FileMissing) : result;
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
        return rows.Select(x => new VehicleAttachmentResponse(x.attachment.Id, x.attachment.VehicleId, x.attachment.Kind, x.attachment.DisplayName,
            x.attachment.CurrentVersionId, x.version == null ? null : x.version.VersionNumber, x.version == null ? null : x.version.OriginalFileName,
            x.version == null ? null : x.version.ContentType, x.version == null ? null : x.version.FileSizeBytes, x.attachment.Kind == VehicleFileKind.Legacy, FleetServiceSupport.EncodeRowVersion(x.attachment.RowVersion))).ToArray();
    }

    private static bool IsImage(VehicleFileKind kind) => kind is VehicleFileKind.FrontImage or VehicleFileKind.RearImage or VehicleFileKind.LeftImage or VehicleFileKind.RightImage;
    private static string DisplayName(VehicleFileKind kind) => kind switch
    {
        VehicleFileKind.Istimara => "الاستمارة",
        VehicleFileKind.OperationCard => "كرت تشغيل",
        VehicleFileKind.FrontImage => "صورة أمامية",
        VehicleFileKind.RearImage => "صورة خلفية",
        VehicleFileKind.LeftImage => "صورة الجانب الأيسر",
        VehicleFileKind.RightImage => "صورة الجانب الأيمن",
        _ => "ملف قديم"
    };
}
