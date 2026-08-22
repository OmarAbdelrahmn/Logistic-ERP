using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Company;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Company;

internal sealed class CompanyProfileService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICompanyProfileService
{
    public async Task<Result<CompanyProfileResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.CompanyProfiles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == CompanyProfile.FixedId, cancellationToken);
        return profile is null
            ? Result.Failure<CompanyProfileResponse>(CompanyErrors.NotFound)
            : Result.Success(ToResponse(profile));
    }

    public async Task<Result<CompanyProfileResponse>> UpdateAsync(
        UpdateCompanyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId
            || !IsValid(request)
            || !Enum.TryParse<CompanyStatus>(request.Status, true, out var status))
        {
            return Result.Failure<CompanyProfileResponse>(CompanyErrors.InvalidRequest);
        }

        var profile = await dbContext.CompanyProfiles.SingleOrDefaultAsync(
            item => item.Id == CompanyProfile.FixedId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Failure<CompanyProfileResponse>(CompanyErrors.NotFound);
        }
        if (!MatchesRowVersion(profile.RowVersion, request.RowVersion))
        {
            return Result.Failure<CompanyProfileResponse>(CompanyErrors.ConcurrencyConflict);
        }

        var previousStatus = profile.Status;
        profile.Code = request.Code.Trim().ToUpperInvariant();
        profile.LegalNameAr = request.LegalNameAr.Trim();
        profile.LegalNameEn = request.LegalNameEn.Trim();
        profile.DisplayNameAr = request.DisplayNameAr.Trim();
        profile.DisplayNameEn = request.DisplayNameEn.Trim();
        profile.CommercialRegistrationNumber = TrimOrNull(request.CommercialRegistrationNumber);
        profile.UnifiedNationalNumber = TrimOrNull(request.UnifiedNationalNumber);
        profile.VatNumber = TrimOrNull(request.VatNumber);
        profile.ContactPhone = request.ContactPhone.Trim();
        profile.ContactEmail = TrimOrNull(request.ContactEmail);
        profile.Address = new Address
        {
            BuildingNumber = TrimOrNull(request.Address.BuildingNumber),
            Street = TrimOrNull(request.Address.Street),
            District = TrimOrNull(request.Address.District),
            City = TrimOrNull(request.Address.City),
            PostalCode = TrimOrNull(request.Address.PostalCode),
            AdditionalNumber = TrimOrNull(request.Address.AdditionalNumber)
        };
        profile.LogoAssetKey = TrimOrNull(request.LogoAssetKey);
        profile.DefaultLocale = request.DefaultLocale.Trim().ToLowerInvariant();
        profile.TimeZoneId = request.TimeZoneId.Trim();
        profile.Status = status;
        profile.SuspensionReason = status == CompanyStatus.Suspended
            ? request.SuspensionReason!.Trim()
            : null;
        if (status == CompanyStatus.Suspended && previousStatus != CompanyStatus.Suspended)
        {
            profile.SuspendedAtUtc = timeProvider.GetUtcNow();
            profile.SuspendedByUserId = userId;
        }
        else if (status != CompanyStatus.Suspended)
        {
            profile.SuspendedAtUtc = null;
            profile.SuspendedByUserId = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(profile));
    }

    private static bool IsValid(UpdateCompanyProfileRequest request)
    {
        if (!HasText(request.Code, 32)
            || !HasText(request.LegalNameAr, 200)
            || !HasText(request.LegalNameEn, 200)
            || !HasText(request.DisplayNameAr, 200)
            || !HasText(request.DisplayNameEn, 200)
            || !HasText(request.ContactPhone, 32)
            || request.DefaultLocale.Trim() is not ("ar" or "en")
            || request.Status.Equals(nameof(CompanyStatus.Suspended), StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(request.SuspensionReason))
        {
            return false;
        }
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }

    private static CompanyProfileResponse ToResponse(CompanyProfile item) => new(
        item.Id,
        item.Code,
        item.LegalNameAr,
        item.LegalNameEn,
        item.DisplayNameAr,
        item.DisplayNameEn,
        item.CommercialRegistrationNumber,
        item.UnifiedNationalNumber,
        item.VatNumber,
        item.ContactPhone,
        item.ContactEmail,
        new CompanyAddressContract(
            item.Address.BuildingNumber,
            item.Address.Street,
            item.Address.District,
            item.Address.City,
            item.Address.PostalCode,
            item.Address.AdditionalNumber),
        item.LogoAssetKey,
        item.DefaultLocale,
        item.TimeZoneId,
        item.Status.ToString(),
        item.SuspensionReason,
        item.SuspendedAtUtc,
        Convert.ToBase64String(item.RowVersion));

    private static bool MatchesRowVersion(byte[] value, string? supplied) =>
        !string.IsNullOrWhiteSpace(supplied)
        && string.Equals(Convert.ToBase64String(value), supplied, StringComparison.Ordinal);
    private static bool HasText(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength;
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

