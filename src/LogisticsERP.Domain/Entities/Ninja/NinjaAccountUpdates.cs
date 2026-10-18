using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Ninja;

public sealed class NinjaAccountUpdateUpload : AuditableEntity
{
    public Guid ClientPlatformId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int IgnoredFooterRows { get; set; }
}

public sealed class NinjaAccountUpdateRow : HistoryEntity
{
    public Guid UploadId { get; set; }
    public int SourceRowNumber { get; set; }
    public string CaptainId { get; set; } = string.Empty;
    public string SheetOwnerName { get; set; } = string.Empty;
    public string SheetCity { get; set; } = string.Empty;
    public string SheetIqamaNo { get; set; } = string.Empty;
    public string SheetStatus { get; set; } = string.Empty;
    public bool? SheetSuspended { get; set; }
    public Guid? PlatformRiderAccountId { get; set; }
    public string MatchStatus { get; set; } = string.Empty;
    public bool HasProblems { get; set; }
    public bool CityMismatch { get; set; }
    public bool IqamaProblem { get; set; }
    public bool NaqlBlocked { get; set; }
    public string? SystemSnapshotJson { get; set; }
    public string ProblemsJson { get; set; } = "[]";
}
