using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class SponsoredInternalDetails : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Gender? Gender { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? NationalityCountryCode { get; set; }
    public string? SecondaryPhone { get; set; }
    public string? Email { get; set; }
    public Guid? ProfilePhotoDocumentId { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }
    public int? DependentsCount { get; set; }
    public string? EducationLevel { get; set; }
    public string? EducationDetails { get; set; }
    public string? Profession { get; set; }
    public Address HomeAddress { get; set; } = new();
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateOnly? HireDate { get; set; }
    public DateOnly? ContractStartDate { get; set; }
    public DateOnly? ContractEndDate { get; set; }
    public DateOnly? ProbationEndDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public string? SponsorLegalReference { get; set; }
    public Guid? CurrentJobTitleId { get; set; }
    public string? InternalNotes { get; set; }
}
