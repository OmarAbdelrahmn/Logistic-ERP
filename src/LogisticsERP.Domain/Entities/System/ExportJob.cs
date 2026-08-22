using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.System;

public sealed class ExportJob : AuditableEntity
{
    public Guid RequestedByUserId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public int ReportVersion { get; set; } = 1;
    public string ScopeSnapshotJson { get; set; } = string.Empty;
    public string FilterSnapshotJson { get; set; } = string.Empty;
    public ExportFormat Format { get; set; }
    public bool IncludesSensitiveValues { get; set; }
    public string? SensitiveExportReason { get; set; }
    public ExportStatus Status { get; set; } = ExportStatus.Pending;
    public int ProgressPercentage { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? ArtifactPath { get; set; }
    public string? ArtifactChecksum { get; set; }
    public long? ArtifactSizeBytes { get; set; }
    public DateTimeOffset? ArtifactExpiresAtUtc { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorDetails { get; set; }
}
