using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Hunger;

public sealed class HungerQualitySnapshot : AuditableEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public Guid ImportId { get; set; }
    public DateOnly ReportDate { get; set; }
    public DateOnly CoverageFrom { get; set; }
    public int SourceRowNumber { get; set; }
    public string CountersJson { get; set; } = "{}";
    public string DailyCountersJson { get; set; } = "{}";
    public string Segment { get; set; } = string.Empty;
    public decimal VerificationSuccessRate { get; set; }
    public decimal OnTimeDeliveryScore { get; set; }
    public decimal FinalDeliveryQualityScore { get; set; }
    public string AllocationStatus { get; set; } = "UnresolvedAllocation";
    public Guid? SoleRiderProfileId { get; set; }
    public string AllocationsJson { get; set; } = "[]";
}

public sealed class HungerImportPreview : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Kind { get; set; } = "Quality";
    public DateOnly ReportDate { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string InputRevision { get; set; } = string.Empty;
    public string RowsJson { get; set; } = "[]";
    public string? ResultJson { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

public sealed class HungerQualityRevision : HistoryEntity
{
    public Guid SnapshotId { get; set; }
    public Guid TriggerImportId { get; set; }
    public string BeforeJson { get; set; } = string.Empty;
    public string AfterJson { get; set; } = string.Empty;
}
