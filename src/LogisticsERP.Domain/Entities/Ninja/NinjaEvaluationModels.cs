using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Ninja;

public sealed class NinjaSuggestionPolicy : HistoryEntity
{
    public string Version { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = string.Empty;
}

public sealed class NinjaEvaluationContextRevision : HistoryEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public int Revision { get; set; }
    public Guid? SupersedesId { get; set; }
    public DateOnly? ConfirmedFirstDeliveryDate { get; set; }
    public Guid? ReviewedSnapshotId { get; set; }
    public string PricingPeriodsJson { get; set; } = "[]";
    public string Reason { get; set; } = string.Empty;
}

public sealed class NinjaResponsibilityRevision : HistoryEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public DateOnly Month { get; set; }
    public int Revision { get; set; }
    public Guid? SupersedesId { get; set; }
    public Guid? ReviewedSnapshotId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class NinjaResponsibilityLine : HistoryEntity
{
    public Guid RevisionId { get; set; }
    public Guid RiderClientAssignmentId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public decimal Weight { get; set; }
}

public sealed class NinjaPerformancePreview : HistoryEntity
{
    public Guid UserId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string OptionsJson { get; set; } = string.Empty;
    public string InputRevision { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

public sealed class NinjaPerformanceArtifact : HistoryEntity
{
    public Guid? OriginalArtifactId { get; set; }
    public Guid? PreviewId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
    public string ResultJson { get; set; } = string.Empty;
}

public sealed class NinjaPerformanceImportArtifact : HistoryEntity
{
    public Guid ArtifactId { get; set; }
    public Guid ImportId { get; set; }
}

public sealed class NinjaAllocationProvenance : HistoryEntity
{
    public Guid SourceId { get; set; }
    public Guid RiderClientAssignmentId { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string EvidenceReference { get; set; } = string.Empty;
}

public sealed class NinjaEvaluationSnapshot : HistoryEntity
{
    public string PolicyVersion { get; set; } = string.Empty;
    public string InputRevision { get; set; } = string.Empty;
    public string QueryJson { get; set; } = string.Empty;
    public string AccountIdsJson { get; set; } = "[]";
    public string ResultJson { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool Frozen { get; set; }
    public Guid? SupersedesId { get; set; }
}
