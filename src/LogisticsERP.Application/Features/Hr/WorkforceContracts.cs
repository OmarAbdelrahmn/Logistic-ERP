namespace LogisticsERP.Application.Features.Hr;

public sealed record EmployeeListItemResponse(
    Guid Id,
    string? IqamaNo,
    string FullNameAr,
    string? FullNameEn,
    string? Nationality,
    string? Iban,
    string? PrimaryPhone,
    AddressResponse? Address,
    bool IsEmployee,
    string EngagementType,
    string Status,
    string? WorkingForMeAs,
    string? ResidencyProfession,
    Guid? SponsorId,
    string? SponsorNameAr,
    Guid? RiderProfileId,
    string RowVersion,
    EmployeeResponse EmployeeDetails,
    RiderDetailsResponse? RiderDetails,
    CurrentRiderWorkPlatformResponse? CurrentWorkPlatform,
    IReadOnlyList<CurrentRiderWorkPlatformResponse> CurrentWorkPlatforms,
    CatalogResponse? OperationalWorkType,
    OperatingCityResponse? OperatingCity,
    string? HousingNameAr);

public sealed record CurrentRiderWorkPlatformResponse(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    Guid PlatformRiderAccountId,
    string ExternalAccountId,
    string PaymentModel);

public sealed record EmployeeDetailsResponse(
    EmployeeResponse Employee,
    RiderDetailsResponse? Rider,
    IReadOnlyList<EmployeeWorkHistoryResponse> WorkHistory,
    HousingResponse? Housing);

public sealed record EmployeeResponse(
    Guid Id,
    string? IqamaNo,
    string? ResidencyProfession,
    string? WorkingForMeAs,
    string FullNameAr,
    string? FullNameEn,
    string? Nationality,
    string? Iban,
    DateOnly? BirthDate,
    string? Gender,
    string? PrimaryPhone,
    string? SecondaryPhone,
    string? Email,
    AddressResponse? Address,
    Guid? ProfilePhotoDocumentId,
    string? MaritalStatus,
    string? EmergencyContactName,
    string? EmergencyContactRelationship,
    string? EmergencyContactPhone,
    bool IsEmployee,
    string EngagementType,
    string Status,
    string? StatusReason,
    DateOnly? HireDate,
    Guid? OperationalWorkTypeId,
    Guid? OperatingCityId,
    Guid? SponsorId,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate,
    DateOnly? ProbationEndDate,
    DateOnly? TerminationDate,
    string? AlternateContactName,
    string? AlternateContactPhone,
    string? Notes,
    string RowVersion);

public sealed record EmployeeUpsertRequest(
    string? IqamaNo,
    string? ResidencyProfession,
    string? WorkingForMeAs,
    string FullNameAr,
    string? FullNameEn,
    string? Nationality,
    string? Iban,
    DateOnly? BirthDate,
    string? Gender,
    string? PrimaryPhone,
    string? SecondaryPhone,
    string? Email,
    AddressRequest? Address,
    Guid? ProfilePhotoDocumentId,
    string? MaritalStatus,
    string? EmergencyContactName,
    string? EmergencyContactRelationship,
    string? EmergencyContactPhone,
    bool IsEmployee,
    string EngagementType,
    string Status,
    string? StatusReason,
    DateOnly? HireDate,
    Guid? OperationalWorkTypeId,
    Guid? OperatingCityId,
    Guid? SponsorId,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate,
    DateOnly? ProbationEndDate,
    DateOnly? TerminationDate,
    string? AlternateContactName,
    string? AlternateContactPhone,
    string? Notes,
    RiderProfileUpsertRequest? Rider,
    string? RowVersion);

public sealed record ChangeEmployeeStatusRequest(string Status, DateOnly EffectiveDate, string Reason);
public sealed record ChangeEmployeeRoleRequest(bool IsEmployee, DateOnly EffectiveDate, string Reason, RiderProfileUpsertRequest? Rider);

public sealed record RiderProfileUpsertRequest(string? TShirtSize, string? OperationalNotes, string? RowVersion = null);

public sealed record RiderDetailsResponse(
    Guid Id,
    Guid EmployeeId,
    string? IqamaNo,
    string FullNameAr,
    string? FullNameEn,
    string? Nationality,
    string? Iban,
    AddressResponse? Address,
    string EngagementType,
    string Status,
    string? TShirtSize,
    string? OperationalNotes,
    string RowVersion);

public sealed record CreateExternalRiderRequest(
    string IqamaNo,
    string FullNameAr,
    string? Nationality,
    string? Iban,
    string PrimaryPhone,
    AddressRequest? Address,
    Guid OperatingCityId,
    Guid OperationalWorkTypeId);

public sealed record UpdateExternalRiderRequest(
    string IqamaNo,
    string FullNameAr,
    string? Nationality,
    string? Iban,
    AddressRequest? Address,
    string RowVersion);

public sealed record ExternalRiderResponse(
    Guid EmployeeId,
    Guid RiderProfileId,
    string? IqamaNo,
    string FullNameAr,
    string? Nationality,
    string? Iban,
    string? PrimaryPhone,
    AddressResponse? Address,
    Guid? OperatingCityId,
    Guid? OperationalWorkTypeId,
    string Status,
    string RowVersion);

public sealed record EmployeeWorkHistoryResponse(
    Guid Id,
    string ChangeType,
    string? OldValue,
    string? NewValue,
    DateOnly EffectiveDate,
    string Reason,
    Guid ChangedByUserId,
    DateTimeOffset CreatedAtUtc);

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
