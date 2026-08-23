namespace LogisticsERP.Application.Features.Hr;

public sealed record ResidencyPermitUpsertRequest(
    Guid? SponsorId,
    Guid ResidencyProfessionId,
    string? PermitNumber,
    DateOnly? IssueDate,
    DateOnly ExpiryDate,
    string Status,
    bool IsCurrent,
    Guid? PreviousPermitId,
    Guid? EmployeeDocumentId,
    string? Notes,
    string? RowVersion);

public sealed record ResidencyPermitResponse(
    Guid Id,
    Guid EmployeeId,
    Guid? SponsorId,
    string? SponsorNameAr,
    Guid ResidencyProfessionId,
    string ResidencyProfessionAr,
    string PermitNumberMasked,
    DateOnly? IssueDate,
    DateOnly ExpiryDate,
    string Status,
    bool IsCurrent,
    Guid? PreviousPermitId,
    Guid? EmployeeDocumentId,
    string? Notes,
    string RowVersion);

public sealed record DriverLicenseUpsertRequest(
    Guid DriverLicenseCategoryId,
    string? LicenseNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string BookingStatus,
    string IssuanceStatus,
    string LicenseStatus,
    bool IsCurrent,
    Guid? PreviousLicenseId,
    Guid? EmployeeDocumentId,
    string? Notes,
    string? RowVersion);

public sealed record DriverLicenseResponse(
    Guid Id,
    Guid EmployeeId,
    Guid DriverLicenseCategoryId,
    string CategoryAr,
    string? LicenseNumberMasked,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string BookingStatus,
    string IssuanceStatus,
    string LicenseStatus,
    bool IsCurrent,
    Guid? PreviousLicenseId,
    Guid? EmployeeDocumentId,
    string? Notes,
    string RowVersion);

public sealed record RiderCardUpsertRequest(
    string CardNumber,
    string CardType,
    string ValidityCycle,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string Status,
    bool IsCurrent,
    Guid? PreviousCardId,
    Guid? EmployeeDocumentId,
    string? Notes,
    string? RowVersion);

public sealed record RiderCardResponse(
    Guid Id,
    Guid RiderProfileId,
    string CardNumber,
    string CardType,
    string ValidityCycle,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string Status,
    bool IsCurrent,
    Guid? PreviousCardId,
    Guid? EmployeeDocumentId,
    string? Notes,
    string RowVersion);

public sealed record HealthCardUpsertRequest(
    string? CardNumber,
    string? CardType,
    string? IssuingAuthority,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string Status,
    bool IsCurrent,
    Guid? PreviousCardId,
    Guid? EmployeeDocumentId,
    string? Notes,
    string? RowVersion);

public sealed record HealthCardResponse(
    Guid Id,
    Guid RiderProfileId,
    string CardNumberMasked,
    string? CardType,
    string? IssuingAuthority,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string Status,
    bool IsCurrent,
    Guid? PreviousCardId,
    Guid? EmployeeDocumentId,
    string? Notes,
    string RowVersion);

public sealed record PromissoryNoteUpsertRequest(
    Guid? SponsorId,
    string NoteNumber,
    decimal Amount,
    string CurrencyCode,
    DateOnly IssueDate,
    DateOnly? DueDate,
    DateTimeOffset? SignedAtUtc,
    string Status,
    Guid BeneficiaryCompanyProfileId,
    Guid? EmployeeDocumentId,
    string? Notes,
    string? RowVersion);

public sealed record PromissoryNoteResponse(
    Guid Id,
    Guid EmployeeId,
    Guid? SponsorId,
    string? SponsorNameAr,
    string NoteNumber,
    decimal Amount,
    string CurrencyCode,
    DateOnly IssueDate,
    DateOnly? DueDate,
    DateTimeOffset? SignedAtUtc,
    string Status,
    Guid BeneficiaryCompanyProfileId,
    Guid? EmployeeDocumentId,
    string? Notes,
    string RowVersion);

public sealed record InsuranceCompanyUpsertRequest(
    string Code,
    string NameAr,
    string? NameEn,
    string? ProviderRegistrationNumber,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    string Status,
    string? Notes,
    string? RowVersion);

public sealed record InsuranceCompanyResponse(
    Guid Id,
    string Code,
    string NameAr,
    string? NameEn,
    string? ProviderRegistrationNumber,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    string Status,
    string? Notes,
    string RowVersion);

public sealed record InsurancePlanUpsertRequest(
    string Code,
    string NameAr,
    string? NameEn,
    int Rank,
    string? NetworkName,
    string? CoverageClass,
    decimal? AnnualCoverageLimit,
    decimal? DeductiblePercentage,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    string? RowVersion);

public sealed record InsurancePlanResponse(
    Guid Id,
    Guid InsuranceCompanyId,
    string Code,
    string NameAr,
    string? NameEn,
    int Rank,
    string? NetworkName,
    string? CoverageClass,
    decimal? AnnualCoverageLimit,
    decimal? DeductiblePercentage,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    string RowVersion);

public sealed record MedicalInsurancePolicyUpsertRequest(
    Guid InsuranceCompanyId,
    Guid InsurancePlanLevelId,
    string? PolicyNumber,
    string? MemberNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    bool IsCurrent,
    Guid? PreviousPolicyId,
    Guid? EmployeeDocumentId,
    string? Notes,
    string? RowVersion);

public sealed record MedicalInsurancePolicyResponse(
    Guid Id,
    Guid EmployeeId,
    Guid InsuranceCompanyId,
    string InsuranceCompanyAr,
    Guid InsurancePlanLevelId,
    string InsurancePlanAr,
    string? PolicyNumberMasked,
    string? MemberNumberMasked,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    bool IsCurrent,
    Guid? PreviousPolicyId,
    Guid? EmployeeDocumentId,
    string? Notes,
    string RowVersion);
