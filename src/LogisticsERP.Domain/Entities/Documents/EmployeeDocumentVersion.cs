using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Documents;

public sealed class EmployeeDocumentVersion : HistoryEntity
{
    public Guid EmployeeDocumentId { get; set; }
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
    public string? PreviewStatus { get; set; }
    public string? PreviewStoragePath { get; set; }
}
