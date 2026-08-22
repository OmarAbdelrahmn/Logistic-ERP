using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Fleet;

public sealed class VehicleAttachment : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public VehicleAttachmentCategory Category { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid? CurrentVersionId { get; set; }
}

public sealed class VehicleAttachmentVersion : HistoryEntity
{
    public Guid VehicleAttachmentId { get; set; }
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Checksum { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAtUtc { get; set; }
    public Guid? SupersededVersionId { get; set; }
}
