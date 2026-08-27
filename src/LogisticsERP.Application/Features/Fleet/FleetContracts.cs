using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Fleet;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record VehicleManufacturerRequest(string Code, string NameAr, string NameEn, VehicleCatalogStatus Status, int DisplayOrder, string? RowVersion);
public sealed record VehicleManufacturerResponse(Guid Id, string Code, string NameAr, string NameEn, VehicleCatalogStatus Status, int DisplayOrder, string RowVersion);
public sealed record VehicleModelRequest(Guid VehicleManufacturerId, string Code, string NameAr, string NameEn, VehicleType VehicleType, VehicleFuelType DefaultFuelType, VehicleCatalogStatus Status, string? RowVersion);
public sealed record VehicleModelResponse(Guid Id, Guid VehicleManufacturerId, string Code, string NameAr, string NameEn, VehicleType VehicleType, VehicleFuelType DefaultFuelType, VehicleCatalogStatus Status, string RowVersion);
public sealed record FleetAddressRequest(string? BuildingNumber, string? Street, string? District, string? City, string? PostalCode, string? AdditionalNumber);
public sealed record FleetAddressResponse(string? BuildingNumber, string? Street, string? District, string? City, string? PostalCode, string? AdditionalNumber);
public sealed record VehicleSupplierRequest(string Code, string NameAr, string NameEn, string? CommercialRegistrationNumber, string? TaxNumber, string? Phone, FleetAddressRequest Address, VehicleCatalogStatus Status, string? Notes, string? RowVersion);
public sealed record VehicleSupplierResponse(Guid Id, string Code, string NameAr, string NameEn, string? CommercialRegistrationNumber, string? TaxNumber, string? Phone, FleetAddressResponse Address, VehicleCatalogStatus Status, string? Notes, string RowVersion);

public sealed record VehicleUpsertRequest(
    string? AssetNumber,
    string? SerialNumber,
    string? PlateNumberAr,
    string? PlateNumberEn,
    string? PlateLettersAr,
    string? PlateLettersEn,
    string? PlateDigits,
    string? Vin,
    string? ChassisNumber,
    string? EngineNumber,
    Guid? SponsorId,
    Guid? OperatingCityId,
    Guid? PurchasedFromSupplierId,
    VehicleRegistrationType? RegistrationType,
    Guid VehicleManufacturerId,
    Guid VehicleModelId,
    int? ModelYear,
    VehicleType VehicleType,
    VehicleFuelType FuelType,
    VehicleTransmissionType TransmissionType,
    string? ColorAr,
    string? ColorEn,
    VehicleOwnershipType OwnershipType,
    string? OwnerName,
    DateOnly? AcquisitionDate,
    string? LeaseReference,
    long CurrentOdometer,
    string? Notes,
    string? RowVersion);

public sealed record VehicleSummaryResponse(
    Guid Id,
    string AssetNumber,
    string? PlateNumberAr,
    string? PlateNumberEn,
    string? SerialNumber,
    string Manufacturer,
    string Model,
    VehicleType VehicleType,
    VehicleRegistrationType? RegistrationType,
    VehicleOperationalStatus Status,
    Guid? SponsorId,
    string? SponsorName,
    Guid? OperatingCityId,
    string? OperatingCity,
    long CurrentOdometer,
    Guid? CurrentAssignmentId,
    Guid? CurrentRiderProfileId,
    string? CurrentRiderName,
    DateOnly? RegistrationExpiryDate,
    VehicleComplianceDueStatus RegistrationStatus,
    DateOnly? InsuranceExpiryDate,
    VehicleComplianceDueStatus InsuranceStatus,
    DateOnly? InspectionExpiryDate,
    VehicleComplianceDueStatus InspectionStatus,
    bool IsReadyForAssignment,
    string RowVersion);

public sealed record VehicleDetailResponse(
    VehicleSummaryResponse Summary,
    string? SerialNumber,
    string? Vin,
    string? ChassisNumber,
    string? EngineNumber,
    Guid? SponsorId,
    Guid? OperatingCityId,
    Guid? PurchasedFromSupplierId,
    string? PurchasedFromSupplier,
    VehicleRegistrationType? RegistrationType,
    Guid VehicleManufacturerId,
    Guid VehicleModelId,
    int? ModelYear,
    VehicleFuelType FuelType,
    VehicleTransmissionType TransmissionType,
    string? ColorAr,
    string? ColorEn,
    VehicleOwnershipType OwnershipType,
    string? OwnerName,
    DateOnly? AcquisitionDate,
    string? LeaseReference,
    DateTimeOffset? DecommissionedAtUtc,
    string? DecommissionReason,
    string? Notes);

public sealed record VehicleLookupResponse(Guid Id, string AssetNumber, string? PlateNumberAr, string? PlateNumberEn, VehicleOperationalStatus Status);
public sealed record VehicleReadinessResponse(Guid VehicleId, IReadOnlyList<string> MissingCoreIdentityFields, IReadOnlyList<VehicleFileKind> MissingPhotoSides, IReadOnlyList<VehicleFileKind> MissingDocuments, IReadOnlyList<string> Warnings, bool IsEligibleForAssignment);
public sealed record VehicleIdentityCorrectionRequest(string AssetNumber, string SerialNumber, string ChassisNumber, string? Vin, string PlateNumberAr, string PlateNumberEn, string? PlateLettersAr, string? PlateLettersEn, string? PlateDigits, Guid SponsorId, Guid OperatingCityId, Guid? PurchasedFromSupplierId, VehicleRegistrationType RegistrationType, string Reason, DateTimeOffset EffectiveAtUtc, IReadOnlyList<Guid>? DocumentVersionReferences, string RowVersion);
public sealed record VehicleIdentityCorrectionResponse(Guid Id, Guid VehicleId, string BeforeJson, string AfterJson, string? DocumentVersionReferencesJson, string Reason, DateTimeOffset EffectiveAtUtc, Guid ActorUserId, DateTimeOffset CreatedAtUtc);
public sealed record VehicleRegistrationTransitionRequest(string PlateNumberAr, string PlateNumberEn, string? PlateLettersAr, string? PlateLettersEn, string? PlateDigits, DateTimeOffset EffectiveAtUtc, string Reason, string RowVersion);
public sealed record VehicleRegistrationTransitionResponse(Guid Id, Guid VehicleId, VehicleRegistrationType FromType, VehicleRegistrationType ToType, string OldPlateNumberAr, string OldPlateNumberEn, string NewPlateNumberAr, string NewPlateNumberEn, DateTimeOffset EffectiveAtUtc, string Reason, Guid IstimaraVersionId, Guid OperationCardVersionId, Guid ActorUserId, DateTimeOffset CreatedAtUtc);
public sealed record ArchiveFleetRequest(string Reason, string RowVersion);
public sealed record VehicleStatusCommandRequest(DateTimeOffset EffectiveAtUtc, string Reason, string RowVersion);
public sealed record OdometerReadingRequest(long Reading, DateTimeOffset RecordedAtUtc, string? Notes, bool IsCorrection, string? CorrectionReason, string RowVersion);
public sealed record VehicleStatusPeriodResponse(Guid Id, VehicleOperationalStatus Status, DateTimeOffset EffectiveFromUtc, DateTimeOffset? EffectiveToUtc, string Reason, VehicleStatusSourceType SourceType, Guid? SourceEntityId);
public sealed record VehicleOdometerReadingResponse(Guid Id, long Reading, DateTimeOffset RecordedAtUtc, VehicleOdometerSourceType SourceType, bool IsCorrection, string? CorrectionReason, string? Notes);

public sealed record TakeVehicleRequest(Guid RiderProfileId, Guid VehicleId, DateTimeOffset StartedAtUtc, long StartOdometer, VehicleCondition StartCondition, byte? StartFuelLevelPercentage, string PermissionReference, string Reason, string? Notes);
public sealed record ReturnVehicleRequest(Guid AssignmentId, DateTimeOffset EndedAtUtc, long EndOdometer, VehicleCondition EndCondition, byte? EndFuelLevelPercentage, string Reason, string RowVersion);
public sealed record SwitchVehicleRequest(Guid CurrentAssignmentId, Guid NewVehicleId, DateTimeOffset SwitchedAtUtc, long OldVehicleOdometer, long NewVehicleOdometer, VehicleCondition OldVehicleCondition, VehicleCondition NewVehicleCondition, byte? OldFuelLevelPercentage, byte? NewFuelLevelPercentage, string PermissionReference, string Reason, string RowVersion);
public sealed record RenewVehiclePermissionRequest(DateOnly PermissionStartsOn, string PermissionReference, string Reason, string RowVersion);
public sealed record RiderPromissoryFileResponse(Guid Id, Guid RiderProfileId, Guid CurrentVersionId, int VersionNumber, string OriginalFileName, string ContentType, long FileSizeBytes, string Sha256Checksum, DateTimeOffset UploadedAtUtc, string RowVersion);
public sealed record RiderVehicleAssignmentResponse(Guid Id, Guid RiderProfileId, Guid EmployeeId, Guid VehicleId, string AssetNumber, string RiderName, DateTimeOffset StartedAtUtc, DateTimeOffset? EndedAtUtc, string? StartLocationSnapshot, string? EndLocationSnapshot, long StartOdometer, long? EndOdometer, string? PermissionReference, DateOnly? PermissionStartsOn, DateOnly? PermissionEndsOn, RiderVehicleAssignmentStatus Status, string AssignmentReason, string? CompletionReason, Guid OperationId, IReadOnlyList<Guid> PromissoryFileVersionIds, string RowVersion);
public sealed record RiderVehicleTimelineResponse(RiderVehicleAssignmentResponse Assignment, IReadOnlyList<VehicleIssueSummaryResponse> Issues, IReadOnlyList<VehicleAccidentSummaryResponse> Accidents);

public sealed record VehicleRegistrationRequest(string RegistrationNumber, string IssuingAuthority, DateOnly IssueDate, DateOnly ExpiryDate, string? Notes);
public sealed record VehicleInsuranceRequest(string ProviderName, string PolicyNumber, string? CoverageType, DateOnly EffectiveFrom, DateOnly ExpiryDate, string? ClaimReference, string? ClaimContact, string? Notes);
public sealed record VehicleInspectionRequest(string InspectionNumber, string StationName, DateOnly InspectionDate, DateOnly ExpiryDate, VehicleInspectionResult Result, long? Odometer, string? FailureNotes, string? Notes);
public sealed record VehicleComplianceResponse(Guid Id, Guid VehicleId, string Type, string Number, string Issuer, DateOnly EffectiveFrom, DateOnly ExpiryDate, VehicleComplianceDueStatus DueStatus, bool IsCurrent, Guid? PreviousRecordId, string RowVersion);
public sealed record VehicleComplianceDueResponse(Guid VehicleId, string AssetNumber, string Type, Guid? RecordId, DateOnly? ExpiryDate, VehicleComplianceDueStatus Status, int? DaysRemaining);

public sealed record VehicleAttachmentResponse(Guid Id, Guid VehicleId, VehicleFileKind Kind, string DisplayName, Guid? CurrentVersionId, int? CurrentVersionNumber, string? OriginalFileName, string? ContentType, long? FileSizeBytes, bool IsLegacy, string RowVersion);
public sealed record VehicleAttachmentVersionResponse(Guid Id, Guid VehicleAttachmentId, int VersionNumber, string OriginalFileName, string ContentType, long FileSizeBytes, string Sha256Checksum, DateTimeOffset UploadedAtUtc);

public sealed record CreateVehicleIssueRequest(Guid VehicleId, VehicleIssueCategory Category, VehicleIssueSeverity Severity, string Description, DateTimeOffset ReportedAtUtc, string? LocationDescription, long? OdometerAtReport, bool BlocksOperation);
public sealed record ResolveVehicleIssueRequest(string ResolutionSummary, string RowVersion);
public sealed record VehicleIssueActionRequest(string Reason, string RowVersion);
public sealed record VehicleIssueSummaryResponse(Guid Id, string IssueNumber, Guid VehicleId, VehicleIssueCategory Category, VehicleIssueSeverity Severity, bool BlocksOperation, VehicleIssueStatus Status, DateTimeOffset ReportedAtUtc, string Description, string? LocationDescription, string? ResolutionSummary, string RowVersion);

public sealed record CreateVehicleAccidentRequest(Guid VehicleId, Guid RiderProfileId, DateTimeOffset OccurredAtUtc, string LocationDescription, decimal? Latitude, decimal? Longitude, string? PoliceReportNumber, string? InsuranceClaimNumber, VehicleAccidentSeverity Severity, bool IsDrivable, bool HasInjuries, string? InjuryDetails, string? ThirdPartyDetails, string DamageDescription, string? FaultAssessment, string Narrative);
public sealed record CorrectVehicleAccidentRequest(string? PoliceReportNumber, string? InsuranceClaimNumber, string LocationDescription, decimal? Latitude, decimal? Longitude, VehicleAccidentSeverity Severity, bool IsDrivable, bool HasInjuries, string? InjuryDetails, string? ThirdPartyDetails, string DamageDescription, string? FaultAssessment, string Narrative, string CorrectionReason, string RowVersion);
public sealed record AccidentActionRequest(string Reason, string RowVersion);
public sealed record VehicleAccidentSummaryResponse(Guid Id, string AccidentNumber, Guid VehicleId, Guid RiderProfileId, Guid RiderVehicleAssignmentId, Guid VehicleIssueId, DateTimeOffset OccurredAtUtc, VehicleAccidentSeverity Severity, bool IsDrivable, VehicleAccidentStatus Status, string LocationDescription, string RowVersion);
public sealed record VehicleAccidentDetailResponse(VehicleAccidentSummaryResponse Summary, string RiderName, string AssetNumber, string? PlateNumberAr, string? PlateNumberEn, string? PoliceReportNumber, string? InsuranceClaimNumber, bool HasInjuries, string? InjuryDetails, string? ThirdPartyDetails, string DamageDescription, string? FaultAssessment, string Narrative, IReadOnlyList<VehicleAccidentAttachmentResponse> Attachments, IReadOnlyList<VehicleAccidentReportVersionResponse> Reports);
public sealed record VehicleAccidentAttachmentResponse(Guid Id, VehicleAccidentEvidenceType EvidenceType, string OriginalFileName, string ContentType, long FileSizeBytes, string Sha256Checksum, DateTimeOffset UploadedAtUtc, string RowVersion);
public sealed record VehicleAccidentReportVersionResponse(Guid Id, int VersionNumber, string ReportNumber, long FileSizeBytes, string Sha256Checksum, DateTimeOffset GeneratedAtUtc, Guid? SupersedesReportVersionId, string? CorrectionReason);

public sealed record AccidentPdfEvidence(string OriginalFileName, string ContentType, string Sha256Checksum, byte[]? ImageBytes);
public sealed record AccidentPdfSnapshot(string ReportNumber, string AccidentNumber, DateTimeOffset OccurredAtUtc, string RiderNameAr, string? RiderNameEn, string? IqamaNo, string AssetNumber, string? PlateNumberAr, string? PlateNumberEn, string LocationDescription, VehicleAccidentSeverity Severity, bool IsDrivable, bool HasInjuries, string? InjuryDetails, string? ThirdPartyDetails, string DamageDescription, string? FaultAssessment, string Narrative, string? PoliceReportNumber, string? InsuranceClaimNumber, string? InsuranceProvider, string? InsurancePolicyNumber, DateTimeOffset GeneratedAtUtc, IReadOnlyList<AccidentPdfEvidence> Evidence);

public interface IAccidentPdfGenerator
{
    byte[] Generate(AccidentPdfSnapshot snapshot);
}

public interface IFleetComplianceNotificationService
{
    Task RunDueNotificationsAsync(CancellationToken cancellationToken = default);
}
