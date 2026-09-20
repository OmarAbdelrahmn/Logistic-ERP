using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class HrLegalCase : AuditableEntity
{
    public string CaseNumber { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
    public LegalCasePersonType PersonType { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? RiderProfileId { get; set; }
    public Guid SponsorId { get; set; }
    public LegalCasePartyRole SponsorPartyRole { get; set; }
    public DateOnly CaseDate { get; set; }
    public TimeOnly CaseTime { get; set; }
    public LegalCaseStatus Status { get; set; } = LegalCaseStatus.Open;
    public string Details { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid ResponsibleUserId { get; set; }
}

public sealed class HrLegalCaseHearing : AuditableEntity
{
    public Guid LegalCaseId { get; set; }
    public int HearingNumber { get; set; }
    public DateOnly HearingDate { get; set; }
    public TimeOnly HearingTime { get; set; }
    public LegalCaseHearingStatus Status { get; set; } = LegalCaseHearingStatus.Scheduled;
    public string Details { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Location { get; set; }
}

public sealed class HrLegalCaseHearingFile : AuditableEntity
{
    public Guid HearingId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Checksum { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAtUtc { get; set; }
}

public sealed class HrLegalCaseHistory : HistoryEntity
{
    public Guid LegalCaseId { get; set; }
    public Guid? HearingId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string ChangedFieldsJson { get; set; } = "[]";
    public string? BeforeJson { get; set; }
    public string AfterJson { get; set; } = "{}";
    public string ChangeReason { get; set; } = string.Empty;
}
