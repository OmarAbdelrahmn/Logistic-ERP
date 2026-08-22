using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class EmployeePromissoryNote : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid? SponsorId { get; set; }
    public string NoteNumber { get; set; } = string.Empty;
    public string NormalizedNoteNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "SAR";
    public DateOnly IssueDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset? SignedAtUtc { get; set; }
    public PromissoryNoteStatus Status { get; set; } = PromissoryNoteStatus.Draft;
    public Guid BeneficiaryCompanyProfileId { get; set; }
    public Guid? EmployeeDocumentId { get; set; }
    public string? Notes { get; set; }
}
