namespace LogisticsERP.Application.Features.Hr;

public sealed record EmployeeListItemResponse(
    Guid Id,
    string EmployeeNumber,
    string FullNameAr,
    string? FullNameEn,
    string? PrimaryPhone,
    string? NationalityCountryCode,
    DateOnly? HireDate,
    string Status,
    string? RelationshipType,
    Guid? RiderProfileId,
    string? RiderStatus,
    string? JobTitleAr,
    string? OperationalWorkTypeAr,
    string? OperatingCityAr,
    string? SponsorNameAr,
    string RowVersion);

public sealed record EmployeeDetailsResponse(
    EmployeeListItemResponse Employee,
    SponsoredInternalDetailsResponse? SponsoredDetails,
    OutsideRiderDetailsResponse? OutsideRiderDetails,
    RiderDetailsResponse? Rider,
    IReadOnlyList<PeriodResponse> StatusHistory,
    IReadOnlyList<PeriodResponse> RelationshipHistory,
    IReadOnlyList<OperationalAssignmentResponse> OperationalAssignmentHistory,
    IReadOnlyList<SponsorshipPeriodResponse> SponsorshipHistory);

public sealed record CreateEmployeeRequest(
    string EmployeeNumber,
    string FullNameAr,
    string? FullNameEn,
    string? PrimaryPhone,
    string? NationalityCountryCode,
    DateOnly? HireDate,
    string Status,
    string RelationshipType,
    string? Notes,
    SponsoredInternalDetailsRequest? SponsoredDetails,
    OutsideRiderDetailsRequest? OutsideRiderDetails,
    CreateRiderProfileRequest? Rider);

public sealed record UpdateEmployeeRequest(
    string FullNameAr,
    string? FullNameEn,
    string? PrimaryPhone,
    string? NationalityCountryCode,
    DateOnly? HireDate,
    string? Notes,
    string RowVersion);

public sealed record ChangeEmployeeStatusRequest(
    string Status,
    DateOnly EffectiveFrom,
    string? ReasonCode,
    string Reason);

public sealed record ChangeEmployeeRelationshipRequest(
    string RelationshipType,
    DateOnly EffectiveFrom,
    string? ReasonCode,
    string Reason,
    string? SourceReference,
    SponsoredInternalDetailsRequest? SponsoredDetails,
    OutsideRiderDetailsRequest? OutsideRiderDetails);

public sealed record SponsoredInternalDetailsRequest(
    string? Gender,
    DateOnly? BirthDate,
    string? SecondaryPhone,
    string? Email,
    Guid? ProfilePhotoDocumentId,
    string? MaritalStatus,
    int? DependentsCount,
    string? EducationLevel,
    string? EducationDetails,
    string? Profession,
    AddressRequest? HomeAddress,
    string? EmergencyContactName,
    string? EmergencyContactRelationship,
    string? EmergencyContactPhone,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate,
    DateOnly? ProbationEndDate,
    DateOnly? TerminationDate,
    Guid? ManagerEmployeeId,
    Guid? CurrentSponsorId,
    string? InternalNotes,
    string? RowVersion = null);

public sealed record SponsoredInternalDetailsResponse(
    Guid Id,
    Guid EmployeeId,
    string? Gender,
    DateOnly? BirthDate,
    string? SecondaryPhone,
    string? Email,
    Guid? ProfilePhotoDocumentId,
    string? MaritalStatus,
    int? DependentsCount,
    string? EducationLevel,
    string? EducationDetails,
    string? Profession,
    AddressResponse HomeAddress,
    string? EmergencyContactName,
    string? EmergencyContactRelationship,
    string? EmergencyContactPhone,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate,
    DateOnly? ProbationEndDate,
    DateOnly? TerminationDate,
    Guid? ManagerEmployeeId,
    Guid? CurrentSponsorId,
    string? InternalNotes,
    string RowVersion);

public sealed record OutsideRiderDetailsRequest(
    string? AlternateContactName,
    string? AlternateContactPhone,
    string? EngagementReference,
    string? EngagementNotes,
    string? RowVersion = null);

public sealed record OutsideRiderDetailsResponse(
    Guid Id,
    Guid EmployeeId,
    string? AlternateContactName,
    string? AlternateContactPhone,
    string? EngagementReference,
    string? EngagementNotes,
    string RowVersion);

public sealed record CreateRiderProfileRequest(
    string Status,
    DateOnly? RiderStartDate,
    DateOnly? RiderEndDate,
    Guid? PreferredCityId,
    string? OperationalNotes);

public sealed record UpdateRiderProfileRequest(
    string Status,
    DateOnly? RiderStartDate,
    DateOnly? RiderEndDate,
    Guid? PreferredCityId,
    string? OperationalNotes,
    string RowVersion);

public sealed record RiderDetailsResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNumber,
    string FullNameAr,
    string? FullNameEn,
    string Status,
    DateOnly? RiderStartDate,
    DateOnly? RiderEndDate,
    Guid? PreferredCityId,
    string? PreferredCityAr,
    string? OperationalNotes,
    bool IsOutsideRider,
    string RowVersion);

public sealed record PeriodResponse(
    Guid Id,
    string Value,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Reason,
    Guid ChangedByUserId);

public sealed record AssignOperationalWorkRequest(
    Guid JobTitleId,
    Guid OperationalWorkTypeId,
    Guid OperatingCityId,
    DateOnly EffectiveFrom,
    string Reason);

public sealed record OperationalAssignmentResponse(
    Guid Id,
    Guid JobTitleId,
    string JobTitleAr,
    Guid OperationalWorkTypeId,
    string OperationalWorkTypeAr,
    Guid OperatingCityId,
    string OperatingCityAr,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Reason);

public sealed record SponsorUpsertRequest(
    string EmployerIdentityNumber,
    string RegistryNameAr,
    string? RegistryNameEn,
    string? CommercialRegistrationNumber,
    string? UnifiedNationalNumber,
    string SponsorType,
    string Status,
    DateOnly? ActiveFrom,
    DateOnly? ActiveTo,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    AddressRequest? Address,
    string? Notes,
    string? RowVersion);

public sealed record SponsorResponse(
    Guid Id,
    Guid CompanyProfileId,
    string EmployerIdentityNumber,
    string RegistryNameAr,
    string? RegistryNameEn,
    string? CommercialRegistrationNumber,
    string? UnifiedNationalNumber,
    string SponsorType,
    string Status,
    DateOnly? ActiveFrom,
    DateOnly? ActiveTo,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    AddressResponse Address,
    string? Notes,
    string RowVersion);

public sealed record ChangeSponsorshipRequest(
    Guid SponsorId,
    string Status,
    DateOnly EffectiveFrom,
    string Reason,
    string? SourceReference);

public sealed record SponsorshipPeriodResponse(
    Guid Id,
    Guid SponsorId,
    string SponsorNameAr,
    string EmployerIdentityNumber,
    string Status,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Reason,
    string? SourceReference);
