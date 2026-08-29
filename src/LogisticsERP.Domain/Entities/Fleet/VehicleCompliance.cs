using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Fleet;

public sealed class VehicleRegistration : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public ComplianceRecordStatus Status { get; set; } = ComplianceRecordStatus.Active;
    public bool IsCurrent { get; set; } = true;
    public Guid? PreviousRecordId { get; set; }
    public Guid? ProofAttachmentId { get; set; }
    public string? Notes { get; set; }
}

public sealed class VehicleInsurancePolicy : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string? CoverageType { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public string? ClaimReference { get; set; }
    public string? ClaimContact { get; set; }
    public ComplianceRecordStatus Status { get; set; } = ComplianceRecordStatus.Active;
    public bool IsCurrent { get; set; } = true;
    public Guid? PreviousRecordId { get; set; }
    public Guid? ProofAttachmentId { get; set; }
    public string? Notes { get; set; }
}

public sealed class VehiclePeriodicInspection : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public string InspectionNumber { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
    public DateOnly InspectionDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public VehicleInspectionResult Result { get; set; }
    public long? Odometer { get; set; }
    public string? FailureNotes { get; set; }
    public ComplianceRecordStatus Status { get; set; } = ComplianceRecordStatus.Active;
    public bool IsCurrent { get; set; } = true;
    public Guid? PreviousRecordId { get; set; }
    public Guid? ProofAttachmentId { get; set; }
    public string? Notes { get; set; }
}

public sealed class VehicleOperationCard : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public ComplianceRecordStatus Status { get; set; } = ComplianceRecordStatus.Active;
    public bool IsCurrent { get; set; } = true;
    public Guid? PreviousRecordId { get; set; }
    public Guid? ProofAttachmentId { get; set; }
    public string? Notes { get; set; }
}
