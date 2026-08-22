using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Company;

public sealed record CompanyAddressContract(
    string? BuildingNumber,
    string? Street,
    string? District,
    string? City,
    string? PostalCode,
    string? AdditionalNumber);

public sealed record UpdateCompanyProfileRequest(
    string Code,
    string LegalNameAr,
    string LegalNameEn,
    string DisplayNameAr,
    string DisplayNameEn,
    string? CommercialRegistrationNumber,
    string? UnifiedNationalNumber,
    string? VatNumber,
    string ContactPhone,
    string? ContactEmail,
    CompanyAddressContract Address,
    string? LogoAssetKey,
    string DefaultLocale,
    string TimeZoneId,
    string Status,
    string? SuspensionReason,
    string RowVersion);

public sealed record CompanyProfileResponse(
    Guid Id,
    string Code,
    string LegalNameAr,
    string LegalNameEn,
    string DisplayNameAr,
    string DisplayNameEn,
    string? CommercialRegistrationNumber,
    string? UnifiedNationalNumber,
    string? VatNumber,
    string ContactPhone,
    string? ContactEmail,
    CompanyAddressContract Address,
    string? LogoAssetKey,
    string DefaultLocale,
    string TimeZoneId,
    string Status,
    string? SuspensionReason,
    DateTimeOffset? SuspendedAtUtc,
    string RowVersion);

public interface ICompanyProfileService
{
    Task<Result<CompanyProfileResponse>> GetAsync(CancellationToken cancellationToken = default);
    Task<Result<CompanyProfileResponse>> UpdateAsync(UpdateCompanyProfileRequest request, CancellationToken cancellationToken = default);
}

