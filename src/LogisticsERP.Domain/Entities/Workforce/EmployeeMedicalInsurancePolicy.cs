using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class EmployeeMedicalInsurancePolicy : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Guid InsuranceCompanyId { get; set; }
    public Guid InsurancePlanLevelId { get; set; }
    public byte[]? PolicyNumberCiphertext { get; set; }
    public string? PolicyNumberLookupHash { get; set; }
    public string? PolicyNumberLastFour { get; set; }
    public byte[]? MemberNumberCiphertext { get; set; }
    public string? MemberNumberLookupHash { get; set; }
    public string? MemberNumberLastFour { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public MedicalInsurancePolicyStatus Status { get; set; } = MedicalInsurancePolicyStatus.Pending;
    public Guid? PreviousPolicyId { get; set; }
    public bool IsCurrent { get; set; }
    public Guid? EmployeeDocumentId { get; set; }
    public string? Notes { get; set; }
}
