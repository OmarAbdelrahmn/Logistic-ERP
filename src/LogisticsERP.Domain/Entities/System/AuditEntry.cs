using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.System;

public sealed class AuditEntry : HistoryEntity
{
    public long Sequence { get; set; }
    public Guid EventId { get; set; } = Guid.CreateVersion7();
    public Guid? ActorUserId { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public Guid? SessionId { get; set; }
    public Guid? SupportAccessGrantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Reason { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? PreviousHash { get; set; }
    public string? CurrentHash { get; set; }
    public int SchemaVersion { get; set; } = 1;
}
