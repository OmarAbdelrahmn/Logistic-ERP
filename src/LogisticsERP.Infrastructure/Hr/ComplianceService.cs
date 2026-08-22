using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class ComplianceService(
    ApplicationDbContext dbContext,
    ISensitiveValueProtector protector) : IComplianceService
{
    public async Task<Result<IReadOnlyList<ResidencyPermitResponse>>> GetResidencyPermitsAsync(Guid? employeeId, CancellationToken cancellationToken = default)
    {
        var query = from item in dbContext.EmployeeResidencyPermits.AsNoTracking()
                    join sponsor in dbContext.Sponsors.AsNoTracking() on item.SponsorId equals sponsor.Id
                    join profession in dbContext.ResidencyProfessions.AsNoTracking() on item.ResidencyProfessionId equals profession.Id
                    where employeeId == null || item.EmployeeId == employeeId
                    orderby item.IsCurrent descending, item.ExpiryDate descending
                    select new ResidencyPermitProjection(item, sponsor.RegistryNameAr, profession.NameAr);
        var rows = await query.ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ResidencyPermitResponse>>(rows.Select(ToResidency).ToArray());
    }

    public async Task<Result<ResidencyPermitResponse>> UpsertResidencyPermitAsync(Guid employeeId, Guid? id, ResidencyPermitUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryParseEnum<ResidencyPermitStatus>(request.Status, out var status)
            || request.IssueDate is not null && request.ExpiryDate < request.IssueDate
            || id is null && !HrServiceSupport.HasText(request.PermitNumber))
        {
            return Result.Failure<ResidencyPermitResponse>(HrErrors.InvalidRequest);
        }
        var refsValid = await dbContext.Employees.AnyAsync(item => item.Id == employeeId, cancellationToken)
            && await dbContext.Sponsors.AnyAsync(item => item.Id == request.SponsorId, cancellationToken)
            && await dbContext.ResidencyProfessions.AnyAsync(item => item.Id == request.ResidencyProfessionId, cancellationToken)
            && await DocumentBelongsToEmployeeAsync(employeeId, request.EmployeeDocumentId, cancellationToken);
        if (!refsValid)
        {
            return Result.Failure<ResidencyPermitResponse>(HrErrors.NotFound);
        }

        EmployeeResidencyPermit entity;
        if (id is null)
        {
            entity = new EmployeeResidencyPermit { EmployeeId = employeeId };
            dbContext.EmployeeResidencyPermits.Add(entity);
        }
        else
        {
            entity = await dbContext.EmployeeResidencyPermits.SingleOrDefaultAsync(item => item.Id == id && item.EmployeeId == employeeId, cancellationToken) ?? null!;
            if (entity is null)
            {
                return Result.Failure<ResidencyPermitResponse>(HrErrors.NotFound);
            }
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            {
                return Result.Failure<ResidencyPermitResponse>(HrErrors.ConcurrencyConflict);
            }
        }
        if (HrServiceSupport.HasText(request.PermitNumber))
        {
            var hash = protector.CreateLookupHash(request.PermitNumber!);
            if (await dbContext.EmployeeResidencyPermits.AnyAsync(item => item.Id != entity.Id && item.IsCurrent && item.PermitNumberLookupHash == hash, cancellationToken))
            {
                return Result.Failure<ResidencyPermitResponse>(HrErrors.Duplicate);
            }
            entity.PermitNumberCiphertext = protector.Protect(request.PermitNumber!);
            entity.PermitNumberLookupHash = hash;
            entity.PermitNumberLastFour = HrServiceSupport.LastFour(request.PermitNumber!);
        }
        await SupersedeCurrentResidencyAsync(employeeId, entity.Id, request.IsCurrent, cancellationToken);
        entity.SponsorId = request.SponsorId;
        entity.ResidencyProfessionId = request.ResidencyProfessionId;
        entity.IssueDate = request.IssueDate;
        entity.ExpiryDate = request.ExpiryDate;
        entity.Status = status;
        entity.IsCurrent = request.IsCurrent;
        entity.PreviousPermitId = request.PreviousPermitId;
        entity.EmployeeDocumentId = request.EmployeeDocumentId;
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetResidencyPermitsAsync(employeeId, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<IReadOnlyList<DriverLicenseResponse>>> GetDriverLicensesAsync(Guid? employeeId, CancellationToken cancellationToken = default)
    {
        var rows = await (from item in dbContext.EmployeeDriverLicenses.AsNoTracking()
                          join category in dbContext.DriverLicenseCategories.AsNoTracking() on item.DriverLicenseCategoryId equals category.Id
                          where employeeId == null || item.EmployeeId == employeeId
                          orderby item.IsCurrent descending, item.ExpiryDate descending
                          select new DriverLicenseProjection(item, category.NameAr)).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<DriverLicenseResponse>>(rows.Select(ToLicense).ToArray());
    }

    public async Task<Result<DriverLicenseResponse>> UpsertDriverLicenseAsync(Guid employeeId, Guid? id, DriverLicenseUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryParseEnum<DriverLicenseBookingStatus>(request.BookingStatus, out var booking)
            || !TryParseEnum<DriverLicenseIssuanceStatus>(request.IssuanceStatus, out var issuance)
            || !TryParseEnum<DriverLicenseStatus>(request.LicenseStatus, out var licenseStatus)
            || request.ExpiryDate is not null && request.IssueDate is not null && request.ExpiryDate < request.IssueDate)
        {
            return Result.Failure<DriverLicenseResponse>(HrErrors.InvalidRequest);
        }
        if (!await dbContext.Employees.AnyAsync(item => item.Id == employeeId, cancellationToken)
            || !await dbContext.DriverLicenseCategories.AnyAsync(item => item.Id == request.DriverLicenseCategoryId, cancellationToken)
            || !await DocumentBelongsToEmployeeAsync(employeeId, request.EmployeeDocumentId, cancellationToken))
        {
            return Result.Failure<DriverLicenseResponse>(HrErrors.NotFound);
        }
        EmployeeDriverLicense entity;
        if (id is null)
        {
            entity = new EmployeeDriverLicense { EmployeeId = employeeId };
            dbContext.EmployeeDriverLicenses.Add(entity);
        }
        else
        {
            entity = await dbContext.EmployeeDriverLicenses.SingleOrDefaultAsync(item => item.Id == id && item.EmployeeId == employeeId, cancellationToken) ?? null!;
            if (entity is null)
            {
                return Result.Failure<DriverLicenseResponse>(HrErrors.NotFound);
            }
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            {
                return Result.Failure<DriverLicenseResponse>(HrErrors.ConcurrencyConflict);
            }
        }
        if (HrServiceSupport.HasText(request.LicenseNumber))
        {
            entity.LicenseNumberCiphertext = protector.Protect(request.LicenseNumber!);
            entity.LicenseNumberLookupHash = protector.CreateLookupHash(request.LicenseNumber!);
            entity.LicenseNumberLastFour = HrServiceSupport.LastFour(request.LicenseNumber!);
        }
        await SupersedeCurrentLicenseAsync(employeeId, request.DriverLicenseCategoryId, entity.Id, request.IsCurrent, cancellationToken);
        entity.DriverLicenseCategoryId = request.DriverLicenseCategoryId;
        entity.IssueDate = request.IssueDate;
        entity.ExpiryDate = request.ExpiryDate;
        entity.BookingStatus = booking;
        entity.IssuanceStatus = issuance;
        entity.LicenseStatus = licenseStatus;
        entity.IsCurrent = request.IsCurrent;
        entity.PreviousLicenseId = request.PreviousLicenseId;
        entity.EmployeeDocumentId = request.EmployeeDocumentId;
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetDriverLicensesAsync(employeeId, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<IReadOnlyList<RiderCardResponse>>> GetRiderCardsAsync(Guid riderProfileId, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.RiderCards.AsNoTracking().Where(item => item.RiderProfileId == riderProfileId)
            .OrderByDescending(item => item.IsCurrent).ThenByDescending(item => item.ExpiryDate).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<RiderCardResponse>>(rows.Select(ToRiderCard).ToArray());
    }

    public async Task<Result<RiderCardResponse>> UpsertRiderCardAsync(Guid riderProfileId, Guid? id, RiderCardUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.CardNumber)
            || !TryParseEnum<RiderCardType>(request.CardType, out var cardType)
            || !TryParseEnum<CardValidityCycle>(request.ValidityCycle, out var cycle)
            || !TryParseEnum<RiderCardStatus>(request.Status, out var status)
            || request.ExpiryDate is not null && request.IssueDate is not null && request.ExpiryDate < request.IssueDate)
        {
            return Result.Failure<RiderCardResponse>(HrErrors.InvalidRequest);
        }
        var employeeId = await GetRiderEmployeeIdAsync(riderProfileId, cancellationToken);
        if (employeeId is null || !await DocumentBelongsToEmployeeAsync(employeeId.Value, request.EmployeeDocumentId, cancellationToken))
        {
            return Result.Failure<RiderCardResponse>(HrErrors.NotFound);
        }
        RiderCard entity;
        if (id is null)
        {
            entity = new RiderCard { RiderProfileId = riderProfileId };
            dbContext.RiderCards.Add(entity);
        }
        else
        {
            entity = await dbContext.RiderCards.SingleOrDefaultAsync(item => item.Id == id && item.RiderProfileId == riderProfileId, cancellationToken) ?? null!;
            if (entity is null)
            {
                return Result.Failure<RiderCardResponse>(HrErrors.NotFound);
            }
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            {
                return Result.Failure<RiderCardResponse>(HrErrors.ConcurrencyConflict);
            }
        }
        var normalizedNumber = HrServiceSupport.NormalizeIdentifier(request.CardNumber);
        if (await dbContext.RiderCards.AnyAsync(item => item.Id != entity.Id && item.IsCurrent && item.NormalizedCardNumber == normalizedNumber, cancellationToken))
        {
            return Result.Failure<RiderCardResponse>(HrErrors.Duplicate);
        }
        await SupersedeCurrentRiderCardAsync(riderProfileId, cardType, entity.Id, request.IsCurrent, cancellationToken);
        entity.CardNumber = request.CardNumber.Trim();
        entity.NormalizedCardNumber = normalizedNumber;
        entity.CardType = cardType;
        entity.ValidityCycle = cycle;
        entity.IssueDate = request.IssueDate;
        entity.ExpiryDate = request.ExpiryDate;
        entity.Status = status;
        entity.IsCurrent = request.IsCurrent;
        entity.PreviousCardId = request.PreviousCardId;
        entity.EmployeeDocumentId = request.EmployeeDocumentId;
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToRiderCard(entity));
    }

    public async Task<Result<IReadOnlyList<HealthCardResponse>>> GetHealthCardsAsync(Guid riderProfileId, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.RiderHealthCards.AsNoTracking().Where(item => item.RiderProfileId == riderProfileId)
            .OrderByDescending(item => item.IsCurrent).ThenByDescending(item => item.ExpiryDate).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<HealthCardResponse>>(rows.Select(ToHealthCard).ToArray());
    }

    public async Task<Result<HealthCardResponse>> UpsertHealthCardAsync(Guid riderProfileId, Guid? id, HealthCardUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryParseEnum<RiderHealthCardStatus>(request.Status, out var status)
            || id is null && !HrServiceSupport.HasText(request.CardNumber)
            || request.ExpiryDate is not null && request.IssueDate is not null && request.ExpiryDate < request.IssueDate)
        {
            return Result.Failure<HealthCardResponse>(HrErrors.InvalidRequest);
        }
        var employeeId = await GetRiderEmployeeIdAsync(riderProfileId, cancellationToken);
        if (employeeId is null || !await DocumentBelongsToEmployeeAsync(employeeId.Value, request.EmployeeDocumentId, cancellationToken))
        {
            return Result.Failure<HealthCardResponse>(HrErrors.NotFound);
        }
        RiderHealthCard entity;
        if (id is null)
        {
            entity = new RiderHealthCard { RiderProfileId = riderProfileId };
            dbContext.RiderHealthCards.Add(entity);
        }
        else
        {
            entity = await dbContext.RiderHealthCards.SingleOrDefaultAsync(item => item.Id == id && item.RiderProfileId == riderProfileId, cancellationToken) ?? null!;
            if (entity is null)
            {
                return Result.Failure<HealthCardResponse>(HrErrors.NotFound);
            }
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            {
                return Result.Failure<HealthCardResponse>(HrErrors.ConcurrencyConflict);
            }
        }
        if (HrServiceSupport.HasText(request.CardNumber))
        {
            var hash = protector.CreateLookupHash(request.CardNumber!);
            if (await dbContext.RiderHealthCards.AnyAsync(item => item.Id != entity.Id && item.IsCurrent && item.CardNumberLookupHash == hash, cancellationToken))
            {
                return Result.Failure<HealthCardResponse>(HrErrors.Duplicate);
            }
            entity.CardNumberCiphertext = protector.Protect(request.CardNumber!);
            entity.CardNumberLookupHash = hash;
            entity.CardNumberLastFour = HrServiceSupport.LastFour(request.CardNumber!);
        }
        await SupersedeCurrentHealthCardAsync(riderProfileId, HrServiceSupport.TrimOrNull(request.CardType), entity.Id, request.IsCurrent, cancellationToken);
        entity.CardType = HrServiceSupport.TrimOrNull(request.CardType);
        entity.IssuingAuthority = HrServiceSupport.TrimOrNull(request.IssuingAuthority);
        entity.IssueDate = request.IssueDate;
        entity.ExpiryDate = request.ExpiryDate;
        entity.Status = status;
        entity.IsCurrent = request.IsCurrent;
        entity.PreviousCardId = request.PreviousCardId;
        entity.EmployeeDocumentId = request.EmployeeDocumentId;
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToHealthCard(entity));
    }

    public async Task<Result<IReadOnlyList<PromissoryNoteResponse>>> GetPromissoryNotesAsync(Guid? employeeId, CancellationToken cancellationToken = default)
    {
        var rows = await (from item in dbContext.EmployeePromissoryNotes.AsNoTracking()
                          join sponsor in dbContext.Sponsors.AsNoTracking() on item.SponsorId equals sponsor.Id into sponsors
                          from sponsor in sponsors.DefaultIfEmpty()
                          where employeeId == null || item.EmployeeId == employeeId
                          orderby item.IssueDate descending
                          select new PromissoryNoteProjection(item, sponsor == null ? null : sponsor.RegistryNameAr)).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PromissoryNoteResponse>>(rows.Select(ToPromissoryNote).ToArray());
    }

    public async Task<Result<PromissoryNoteResponse>> UpsertPromissoryNoteAsync(Guid employeeId, Guid? id, PromissoryNoteUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.NoteNumber) || request.Amount <= 0 || request.CurrencyCode.Trim().Length != 3
            || !TryParseEnum<PromissoryNoteStatus>(request.Status, out var status)
            || request.DueDate is not null && request.DueDate < request.IssueDate)
        {
            return Result.Failure<PromissoryNoteResponse>(HrErrors.InvalidRequest);
        }
        var refsValid = await dbContext.Employees.AnyAsync(item => item.Id == employeeId, cancellationToken)
            && await dbContext.CompanyProfiles.AnyAsync(item => item.Id == request.BeneficiaryCompanyProfileId, cancellationToken)
            && (request.SponsorId is null || await dbContext.Sponsors.AnyAsync(item => item.Id == request.SponsorId, cancellationToken))
            && await DocumentBelongsToEmployeeAsync(employeeId, request.EmployeeDocumentId, cancellationToken);
        if (!refsValid)
        {
            return Result.Failure<PromissoryNoteResponse>(HrErrors.NotFound);
        }
        EmployeePromissoryNote entity;
        if (id is null)
        {
            entity = new EmployeePromissoryNote { EmployeeId = employeeId };
            dbContext.EmployeePromissoryNotes.Add(entity);
        }
        else
        {
            entity = await dbContext.EmployeePromissoryNotes.SingleOrDefaultAsync(item => item.Id == id && item.EmployeeId == employeeId, cancellationToken) ?? null!;
            if (entity is null)
            {
                return Result.Failure<PromissoryNoteResponse>(HrErrors.NotFound);
            }
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            {
                return Result.Failure<PromissoryNoteResponse>(HrErrors.ConcurrencyConflict);
            }
        }
        var normalized = HrServiceSupport.NormalizeIdentifier(request.NoteNumber);
        if (await dbContext.EmployeePromissoryNotes.AnyAsync(item => item.Id != entity.Id && item.BeneficiaryCompanyProfileId == request.BeneficiaryCompanyProfileId && item.NormalizedNoteNumber == normalized, cancellationToken))
        {
            return Result.Failure<PromissoryNoteResponse>(HrErrors.Duplicate);
        }
        entity.SponsorId = request.SponsorId;
        entity.NoteNumber = request.NoteNumber.Trim();
        entity.NormalizedNoteNumber = normalized;
        entity.Amount = request.Amount;
        entity.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        entity.IssueDate = request.IssueDate;
        entity.DueDate = request.DueDate;
        entity.SignedAtUtc = request.SignedAtUtc;
        entity.Status = status;
        entity.BeneficiaryCompanyProfileId = request.BeneficiaryCompanyProfileId;
        entity.EmployeeDocumentId = request.EmployeeDocumentId;
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetPromissoryNotesAsync(employeeId, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<IReadOnlyList<InsuranceCompanyResponse>>> GetInsuranceCompaniesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.InsuranceCompanies.AsNoTracking().OrderBy(item => item.NameAr).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<InsuranceCompanyResponse>>(rows.Select(ToInsuranceCompany).ToArray());
    }

    public async Task<Result<InsuranceCompanyResponse>> UpsertInsuranceCompanyAsync(Guid? id, InsuranceCompanyUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Code) || !HrServiceSupport.HasText(request.NameAr)
            || !TryParseEnum<InsuranceCompanyStatus>(request.Status, out var status))
        {
            return Result.Failure<InsuranceCompanyResponse>(HrErrors.InvalidRequest);
        }
        InsuranceCompany entity;
        if (id is null)
        {
            entity = new InsuranceCompany();
            dbContext.InsuranceCompanies.Add(entity);
        }
        else
        {
            entity = await dbContext.InsuranceCompanies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null)
            {
                return Result.Failure<InsuranceCompanyResponse>(HrErrors.NotFound);
            }
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            {
                return Result.Failure<InsuranceCompanyResponse>(HrErrors.ConcurrencyConflict);
            }
        }
        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.InsuranceCompanies.AnyAsync(item => item.Id != entity.Id && item.Code == code, cancellationToken))
        {
            return Result.Failure<InsuranceCompanyResponse>(HrErrors.Duplicate);
        }
        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = HrServiceSupport.TrimOrNull(request.NameEn);
        entity.ProviderRegistrationNumber = HrServiceSupport.TrimOrNull(request.ProviderRegistrationNumber);
        entity.ContactName = HrServiceSupport.TrimOrNull(request.ContactName);
        entity.ContactPhone = HrServiceSupport.TrimOrNull(request.ContactPhone);
        entity.ContactEmail = HrServiceSupport.TrimOrNull(request.ContactEmail);
        entity.Status = status;
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToInsuranceCompany(entity));
    }

    public async Task<Result<IReadOnlyList<InsurancePlanResponse>>> GetInsurancePlansAsync(Guid insuranceCompanyId, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.InsurancePlanLevels.AsNoTracking().Where(item => item.InsuranceCompanyId == insuranceCompanyId)
            .OrderBy(item => item.Rank).ThenBy(item => item.NameAr).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<InsurancePlanResponse>>(rows.Select(ToInsurancePlan).ToArray());
    }

    public async Task<Result<InsurancePlanResponse>> UpsertInsurancePlanAsync(Guid insuranceCompanyId, Guid? id, InsurancePlanUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Code) || !HrServiceSupport.HasText(request.NameAr) || request.Rank < 0
            || request.AnnualCoverageLimit is < 0 || request.DeductiblePercentage is < 0 or > 100
            || !TryParseEnum<InsurancePlanStatus>(request.Status, out var status)
            || request.EffectiveTo is not null && request.EffectiveFrom is not null && request.EffectiveTo < request.EffectiveFrom)
        {
            return Result.Failure<InsurancePlanResponse>(HrErrors.InvalidRequest);
        }
        if (!await dbContext.InsuranceCompanies.AnyAsync(item => item.Id == insuranceCompanyId, cancellationToken))
        {
            return Result.Failure<InsurancePlanResponse>(HrErrors.NotFound);
        }
        InsurancePlanLevel entity;
        if (id is null)
        {
            entity = new InsurancePlanLevel { InsuranceCompanyId = insuranceCompanyId };
            dbContext.InsurancePlanLevels.Add(entity);
        }
        else
        {
            entity = await dbContext.InsurancePlanLevels.SingleOrDefaultAsync(item => item.Id == id && item.InsuranceCompanyId == insuranceCompanyId, cancellationToken) ?? null!;
            if (entity is null)
            {
                return Result.Failure<InsurancePlanResponse>(HrErrors.NotFound);
            }
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            {
                return Result.Failure<InsurancePlanResponse>(HrErrors.ConcurrencyConflict);
            }
        }
        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.InsurancePlanLevels.AnyAsync(item => item.Id != entity.Id && item.InsuranceCompanyId == insuranceCompanyId && item.Code == code, cancellationToken))
        {
            return Result.Failure<InsurancePlanResponse>(HrErrors.Duplicate);
        }
        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = HrServiceSupport.TrimOrNull(request.NameEn);
        entity.Rank = request.Rank;
        entity.NetworkName = HrServiceSupport.TrimOrNull(request.NetworkName);
        entity.CoverageClass = HrServiceSupport.TrimOrNull(request.CoverageClass);
        entity.AnnualCoverageLimit = request.AnnualCoverageLimit;
        entity.DeductiblePercentage = request.DeductiblePercentage;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToInsurancePlan(entity));
    }

    public async Task<Result<IReadOnlyList<MedicalInsurancePolicyResponse>>> GetMedicalInsurancePoliciesAsync(Guid? employeeId, CancellationToken cancellationToken = default)
    {
        var rows = await (from item in dbContext.EmployeeMedicalInsurancePolicies.AsNoTracking()
                          join company in dbContext.InsuranceCompanies.AsNoTracking() on item.InsuranceCompanyId equals company.Id
                          join plan in dbContext.InsurancePlanLevels.AsNoTracking() on item.InsurancePlanLevelId equals plan.Id
                          where employeeId == null || item.EmployeeId == employeeId
                          orderby item.IsCurrent descending, item.EndDate descending
                          select new MedicalPolicyProjection(item, company.NameAr, plan.NameAr)).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<MedicalInsurancePolicyResponse>>(rows.Select(ToPolicy).ToArray());
    }

    public async Task<Result<MedicalInsurancePolicyResponse>> UpsertMedicalInsurancePolicyAsync(Guid employeeId, Guid? id, MedicalInsurancePolicyUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryParseEnum<MedicalInsurancePolicyStatus>(request.Status, out var status) || request.EndDate < request.StartDate)
        {
            return Result.Failure<MedicalInsurancePolicyResponse>(HrErrors.InvalidRequest);
        }
        var refsValid = await dbContext.Employees.AnyAsync(item => item.Id == employeeId, cancellationToken)
            && await dbContext.InsurancePlanLevels.AnyAsync(item => item.Id == request.InsurancePlanLevelId && item.InsuranceCompanyId == request.InsuranceCompanyId, cancellationToken)
            && await DocumentBelongsToEmployeeAsync(employeeId, request.EmployeeDocumentId, cancellationToken);
        if (!refsValid)
        {
            return Result.Failure<MedicalInsurancePolicyResponse>(HrErrors.NotFound);
        }
        EmployeeMedicalInsurancePolicy entity;
        if (id is null)
        {
            entity = new EmployeeMedicalInsurancePolicy { EmployeeId = employeeId };
            dbContext.EmployeeMedicalInsurancePolicies.Add(entity);
        }
        else
        {
            entity = await dbContext.EmployeeMedicalInsurancePolicies.SingleOrDefaultAsync(item => item.Id == id && item.EmployeeId == employeeId, cancellationToken) ?? null!;
            if (entity is null)
            {
                return Result.Failure<MedicalInsurancePolicyResponse>(HrErrors.NotFound);
            }
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            {
                return Result.Failure<MedicalInsurancePolicyResponse>(HrErrors.ConcurrencyConflict);
            }
        }
        SetProtectedOptional(request.PolicyNumber, value =>
        {
            entity.PolicyNumberCiphertext = protector.Protect(value);
            entity.PolicyNumberLookupHash = protector.CreateLookupHash(value);
            entity.PolicyNumberLastFour = HrServiceSupport.LastFour(value);
        });
        SetProtectedOptional(request.MemberNumber, value =>
        {
            entity.MemberNumberCiphertext = protector.Protect(value);
            entity.MemberNumberLookupHash = protector.CreateLookupHash(value);
            entity.MemberNumberLastFour = HrServiceSupport.LastFour(value);
        });
        await SupersedeCurrentPolicyAsync(employeeId, entity.Id, request.IsCurrent, cancellationToken);
        entity.InsuranceCompanyId = request.InsuranceCompanyId;
        entity.InsurancePlanLevelId = request.InsurancePlanLevelId;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.Status = status;
        entity.IsCurrent = request.IsCurrent;
        entity.PreviousPolicyId = request.PreviousPolicyId;
        entity.EmployeeDocumentId = request.EmployeeDocumentId;
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetMedicalInsurancePoliciesAsync(employeeId, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result> ArchiveAsync(string resource, Guid id, ArchiveRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Reason))
        {
            return Result.Failure(HrErrors.InvalidRequest);
        }
        AuditableEntity? entity = resource.Trim().ToLowerInvariant() switch
        {
            "residency" => await dbContext.EmployeeResidencyPermits.SingleOrDefaultAsync(item => item.Id == id, cancellationToken),
            "license" => await dbContext.EmployeeDriverLicenses.SingleOrDefaultAsync(item => item.Id == id, cancellationToken),
            "rider-card" => await dbContext.RiderCards.SingleOrDefaultAsync(item => item.Id == id, cancellationToken),
            "health-card" => await dbContext.RiderHealthCards.SingleOrDefaultAsync(item => item.Id == id, cancellationToken),
            "promissory-note" => await dbContext.EmployeePromissoryNotes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken),
            "insurance-company" => await dbContext.InsuranceCompanies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken),
            "insurance-plan" => await dbContext.InsurancePlanLevels.SingleOrDefaultAsync(item => item.Id == id, cancellationToken),
            "medical-policy" => await dbContext.EmployeeMedicalInsurancePolicies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken),
            _ => null
        };
        if (entity is null)
        {
            return Result.Failure(HrErrors.NotFound);
        }
        if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
        {
            return Result.Failure(HrErrors.ConcurrencyConflict);
        }
        entity.IsDeleted = true;
        entity.DeletionReason = request.Reason.Trim();
        switch (entity)
        {
            case EmployeeResidencyPermit permit: permit.IsCurrent = false; break;
            case EmployeeDriverLicense license: license.IsCurrent = false; license.LicenseStatus = DriverLicenseStatus.Superseded; break;
            case RiderCard card: card.IsCurrent = false; card.Status = RiderCardStatus.Superseded; break;
            case RiderHealthCard healthCard: healthCard.IsCurrent = false; healthCard.Status = RiderHealthCardStatus.Superseded; break;
            case EmployeeMedicalInsurancePolicy policy: policy.IsCurrent = false; policy.Status = MedicalInsurancePolicyStatus.Superseded; break;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<bool> DocumentBelongsToEmployeeAsync(Guid employeeId, Guid? documentId, CancellationToken cancellationToken) =>
        documentId is null || await dbContext.EmployeeDocuments.AnyAsync(item => item.Id == documentId && item.EmployeeId == employeeId, cancellationToken);

    private async Task<Guid?> GetRiderEmployeeIdAsync(Guid riderProfileId, CancellationToken cancellationToken) =>
        await dbContext.RiderProfiles.Where(item => item.Id == riderProfileId).Select(item => (Guid?)item.EmployeeId).SingleOrDefaultAsync(cancellationToken);

    private async Task SupersedeCurrentResidencyAsync(Guid employeeId, Guid excludeId, bool makeCurrent, CancellationToken cancellationToken)
    {
        if (!makeCurrent) return;
        var current = await dbContext.EmployeeResidencyPermits.SingleOrDefaultAsync(item => item.EmployeeId == employeeId && item.IsCurrent && item.Id != excludeId, cancellationToken);
        if (current is not null) current.IsCurrent = false;
    }

    private async Task SupersedeCurrentLicenseAsync(Guid employeeId, Guid categoryId, Guid excludeId, bool makeCurrent, CancellationToken cancellationToken)
    {
        if (!makeCurrent) return;
        var current = await dbContext.EmployeeDriverLicenses.SingleOrDefaultAsync(item => item.EmployeeId == employeeId && item.DriverLicenseCategoryId == categoryId && item.IsCurrent && item.Id != excludeId, cancellationToken);
        if (current is not null) { current.IsCurrent = false; current.LicenseStatus = DriverLicenseStatus.Superseded; }
    }

    private async Task SupersedeCurrentRiderCardAsync(Guid riderId, RiderCardType type, Guid excludeId, bool makeCurrent, CancellationToken cancellationToken)
    {
        if (!makeCurrent) return;
        var current = await dbContext.RiderCards.SingleOrDefaultAsync(item => item.RiderProfileId == riderId && item.CardType == type && item.IsCurrent && item.Id != excludeId, cancellationToken);
        if (current is not null) { current.IsCurrent = false; current.Status = RiderCardStatus.Superseded; }
    }

    private async Task SupersedeCurrentHealthCardAsync(Guid riderId, string? cardType, Guid excludeId, bool makeCurrent, CancellationToken cancellationToken)
    {
        if (!makeCurrent) return;
        var current = await dbContext.RiderHealthCards.SingleOrDefaultAsync(item => item.RiderProfileId == riderId && item.CardType == cardType && item.IsCurrent && item.Id != excludeId, cancellationToken);
        if (current is not null) { current.IsCurrent = false; current.Status = RiderHealthCardStatus.Superseded; }
    }

    private async Task SupersedeCurrentPolicyAsync(Guid employeeId, Guid excludeId, bool makeCurrent, CancellationToken cancellationToken)
    {
        if (!makeCurrent) return;
        var current = await dbContext.EmployeeMedicalInsurancePolicies.SingleOrDefaultAsync(item => item.EmployeeId == employeeId && item.IsCurrent && item.Id != excludeId, cancellationToken);
        if (current is not null) { current.IsCurrent = false; current.Status = MedicalInsurancePolicyStatus.Superseded; }
    }

    private static ResidencyPermitResponse ToResidency(ResidencyPermitProjection row) => new(row.Item.Id, row.Item.EmployeeId,
        row.Item.SponsorId, row.SponsorNameAr, row.Item.ResidencyProfessionId, row.ProfessionNameAr,
        HrServiceSupport.MaskLastFour(row.Item.PermitNumberLastFour), row.Item.IssueDate, row.Item.ExpiryDate,
        row.Item.Status.ToString(), row.Item.IsCurrent, row.Item.PreviousPermitId, row.Item.EmployeeDocumentId,
        row.Item.Notes, HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));

    private static DriverLicenseResponse ToLicense(DriverLicenseProjection row) => new(row.Item.Id, row.Item.EmployeeId,
        row.Item.DriverLicenseCategoryId, row.CategoryNameAr, row.Item.LicenseNumberLastFour is null ? null : HrServiceSupport.MaskLastFour(row.Item.LicenseNumberLastFour),
        row.Item.IssueDate, row.Item.ExpiryDate, row.Item.BookingStatus.ToString(), row.Item.IssuanceStatus.ToString(),
        row.Item.LicenseStatus.ToString(), row.Item.IsCurrent, row.Item.PreviousLicenseId, row.Item.EmployeeDocumentId,
        row.Item.Notes, HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));

    private static RiderCardResponse ToRiderCard(RiderCard item) => new(item.Id, item.RiderProfileId, item.CardNumber,
        item.CardType.ToString(), item.ValidityCycle.ToString(), item.IssueDate, item.ExpiryDate, item.Status.ToString(),
        item.IsCurrent, item.PreviousCardId, item.EmployeeDocumentId, item.Notes, HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private static HealthCardResponse ToHealthCard(RiderHealthCard item) => new(item.Id, item.RiderProfileId,
        HrServiceSupport.MaskLastFour(item.CardNumberLastFour), item.CardType, item.IssuingAuthority, item.IssueDate,
        item.ExpiryDate, item.Status.ToString(), item.IsCurrent, item.PreviousCardId, item.EmployeeDocumentId,
        item.Notes, HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private static PromissoryNoteResponse ToPromissoryNote(PromissoryNoteProjection row) => new(row.Item.Id,
        row.Item.EmployeeId, row.Item.SponsorId, row.SponsorNameAr, row.Item.NoteNumber, row.Item.Amount,
        row.Item.CurrencyCode, row.Item.IssueDate, row.Item.DueDate, row.Item.SignedAtUtc, row.Item.Status.ToString(),
        row.Item.BeneficiaryCompanyProfileId, row.Item.EmployeeDocumentId, row.Item.Notes,
        HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));

    private static InsuranceCompanyResponse ToInsuranceCompany(InsuranceCompany item) => new(item.Id, item.Code,
        item.NameAr, item.NameEn, item.ProviderRegistrationNumber, item.ContactName, item.ContactPhone, item.ContactEmail,
        item.Status.ToString(), item.Notes, HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private static InsurancePlanResponse ToInsurancePlan(InsurancePlanLevel item) => new(item.Id, item.InsuranceCompanyId,
        item.Code, item.NameAr, item.NameEn, item.Rank, item.NetworkName, item.CoverageClass, item.AnnualCoverageLimit,
        item.DeductiblePercentage, item.EffectiveFrom, item.EffectiveTo, item.Status.ToString(),
        HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private static MedicalInsurancePolicyResponse ToPolicy(MedicalPolicyProjection row) => new(row.Item.Id,
        row.Item.EmployeeId, row.Item.InsuranceCompanyId, row.CompanyNameAr, row.Item.InsurancePlanLevelId,
        row.PlanNameAr, row.Item.PolicyNumberLastFour is null ? null : HrServiceSupport.MaskLastFour(row.Item.PolicyNumberLastFour),
        row.Item.MemberNumberLastFour is null ? null : HrServiceSupport.MaskLastFour(row.Item.MemberNumberLastFour),
        row.Item.StartDate, row.Item.EndDate, row.Item.Status.ToString(), row.Item.IsCurrent, row.Item.PreviousPolicyId,
        row.Item.EmployeeDocumentId, row.Item.Notes, HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));

    private static void SetProtectedOptional(string? value, Action<string> setter)
    {
        if (HrServiceSupport.HasText(value)) setter(value!);
    }

    private static bool TryParseEnum<TEnum>(string? value, out TEnum parsed) where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed);

    private sealed record ResidencyPermitProjection(EmployeeResidencyPermit Item, string SponsorNameAr, string ProfessionNameAr);
    private sealed record DriverLicenseProjection(EmployeeDriverLicense Item, string CategoryNameAr);
    private sealed record PromissoryNoteProjection(EmployeePromissoryNote Item, string? SponsorNameAr);
    private sealed record MedicalPolicyProjection(EmployeeMedicalInsurancePolicy Item, string CompanyNameAr, string PlanNameAr);
}

internal static class HrResultExtensions
{
    public static Result<TItem> MapSingle<TItem>(this Result<IReadOnlyList<TItem>> result, Func<TItem, bool> predicate)
    {
        if (result.IsFailure)
        {
            return Result.Failure<TItem>(result.Error);
        }
        var item = result.Value!.SingleOrDefault(predicate);
        return item is null ? Result.Failure<TItem>(HrErrors.NotFound) : Result.Success(item);
    }
}
