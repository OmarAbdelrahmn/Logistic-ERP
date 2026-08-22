using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.System;

public sealed class Notification : AuditableEntity
{
    public Guid RecipientUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Information;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string BodyAr { get; set; } = string.Empty;
    public string BodyEn { get; set; } = string.Empty;
    public string? SourceEntityType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public string? DeepLink { get; set; }
    public string? ScopeSnapshotJson { get; set; }
    public string DeduplicationKey { get; set; } = string.Empty;
    public DateTimeOffset VisibleAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
    public Guid? AcknowledgedByUserId { get; set; }
    public DateTimeOffset? ArchivedAtUtc { get; set; }
    public Guid? ArchivedByUserId { get; set; }
}
