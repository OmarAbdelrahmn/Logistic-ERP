using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class EmployeeAbsenceComplianceCase : AuditableEntity
{
    public string CaseNumber { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public DateOnly AbsenceDate { get; set; }
    public AbsenceCasePath CurrentPath { get; set; }
    public AbsenceCaseStatus Status { get; set; } = AbsenceCaseStatus.Open;
    public DateOnly? ReportedToAuthoritiesDate { get; set; }
    public string? AuthorityReportReference { get; set; }
    public DateOnly? ExitOrOutageDate { get; set; }
    public string? ExitVisaNumber { get; set; }
    public DateOnly RemovalDeadline { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? ResolutionCode { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public Guid? ClosedByUserId { get; set; }
}

public sealed class EmployeeAbsenceComplianceCaseEvent : HistoryEntity
{
    public Guid EmployeeAbsenceComplianceCaseId { get; set; }
    public AbsenceCaseEventType EventType { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid ActorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string AfterJson { get; set; } = "{}";
    public string CorrelationId { get; set; } = string.Empty;
}
