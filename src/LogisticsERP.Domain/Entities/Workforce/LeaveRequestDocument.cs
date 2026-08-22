using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class LeaveRequestDocument : AuditableEntity
{
    public Guid LeaveRequestId { get; set; }
    public LeaveDocumentKind Kind { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public Guid? CurrentVersionId { get; set; }
    public string? Notes { get; set; }
}

public sealed class LeaveRequestDocumentVersion : HistoryEntity
{
    public Guid LeaveRequestDocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Checksum { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAtUtc { get; set; }
    public Guid? SupersededVersionId { get; set; }
}
