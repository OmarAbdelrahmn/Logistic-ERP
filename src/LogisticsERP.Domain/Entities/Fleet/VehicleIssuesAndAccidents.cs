using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Fleet;

public sealed class VehicleIssue : AuditableEntity
{
    public string IssueNumber { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public VehicleIssueCategory Category { get; set; }
    public VehicleIssueSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset ReportedAtUtc { get; set; }
    public string? LocationDescription { get; set; }
    public long? OdometerAtReport { get; set; }
    public Guid? RelatedAssignmentId { get; set; }
    public bool BlocksOperation { get; set; }
    public VehicleIssueStatus Status { get; set; } = VehicleIssueStatus.Open;
    public Guid ReportedByUserId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public string? ResolutionSummary { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public Guid? ClosedByUserId { get; set; }
}

public sealed class VehicleIssueEvent : HistoryEntity
{
    public Guid VehicleIssueId { get; set; }
    public VehicleIssueEventType EventType { get; set; }
    public VehicleIssueStatus? FromStatus { get; set; }
    public VehicleIssueStatus ToStatus { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid ActorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? SnapshotJson { get; set; }
}

public sealed class VehicleAccident : AuditableEntity
{
    public string AccidentNumber { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public Guid RiderProfileId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid RiderVehicleAssignmentId { get; set; }
    public Guid VehicleIssueId { get; set; }
    public Guid? VehicleInsurancePolicyId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset ReportedAtUtc { get; set; }
    public string LocationDescription { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? PoliceReportNumber { get; set; }
    public string? InsuranceClaimNumber { get; set; }
    public VehicleAccidentSeverity Severity { get; set; }
    public bool IsDrivable { get; set; }
    public bool HasInjuries { get; set; }
    public string? InjuryDetails { get; set; }
    public string? ThirdPartyDetails { get; set; }
    public string DamageDescription { get; set; } = string.Empty;
    public string? FaultAssessment { get; set; }
    public string Narrative { get; set; } = string.Empty;
    public VehicleAccidentStatus Status { get; set; } = VehicleAccidentStatus.Reported;
    public Guid ReportedByUserId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public Guid? CurrentReportVersionId { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public Guid? ClosedByUserId { get; set; }
}

public sealed class VehicleAccidentEvent : HistoryEntity
{
    public Guid VehicleAccidentId { get; set; }
    public VehicleAccidentEventType EventType { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid ActorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? SnapshotJson { get; set; }
}

public sealed class VehicleAccidentAttachment : AuditableEntity
{
    public Guid VehicleAccidentId { get; set; }
    public VehicleAccidentEvidenceType EvidenceType { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Checksum { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAtUtc { get; set; }
}

public sealed class VehicleAccidentReportVersion : HistoryEntity
{
    public Guid VehicleAccidentId { get; set; }
    public int VersionNumber { get; set; }
    public string ReportNumber { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Checksum { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public Guid GeneratedByUserId { get; set; }
    public Guid? SupersedesReportVersionId { get; set; }
    public string? CorrectionReason { get; set; }
}
