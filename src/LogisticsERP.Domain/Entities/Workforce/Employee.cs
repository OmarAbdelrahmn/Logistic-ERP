using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class Employee : AuditableEntity
{
    public string? IqamaNo { get; set; }
    public string? ResidencyProfession { get; set; }
    public string? WorkingForMeAs { get; set; }
    public string FullNameAr { get; set; } = string.Empty;
    public string? FullNameEn { get; set; }
    public string? Nationality { get; set; }
    public DateOnly? BirthDate { get; set; }
    public Gender? Gender { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? SecondaryPhone { get; set; }
    public string? Email { get; set; }
    public Guid? ProfilePhotoDocumentId { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool IsEmployee { get; set; }
    public EmployeeRelationshipType EngagementType { get; set; } = EmployeeRelationshipType.SponsoredInternal;
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Draft;
    public string? StatusReason { get; set; }
    public DateOnly? HireDate { get; set; }
    public Guid? OperationalWorkTypeId { get; set; }
    public Guid? OperatingCityId { get; set; }
    public Guid? SponsorId { get; set; }
    public DateOnly? ContractStartDate { get; set; }
    public DateOnly? ContractEndDate { get; set; }
    public DateOnly? ProbationEndDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public string? AlternateContactName { get; set; }
    public string? AlternateContactPhone { get; set; }
    public string? Notes { get; set; }
}
