using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class WorkforceService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser) : IWorkforceService
{
    public async Task<Result<IReadOnlyList<EmployeeListItemResponse>>> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Employees.AsNoTracking()
            .OrderBy(employee => employee.FullNameAr)
            .Select(employee => new EmployeeListProjection(
                employee.Id,
                employee.EmployeeNumber,
                employee.FullNameAr,
                employee.FullNameEn,
                employee.PrimaryPhone,
                employee.NationalityCountryCode,
                employee.HireDate,
                employee.CurrentStatus,
                employee.CurrentRelationshipType,
                dbContext.RiderProfiles.Where(rider => rider.EmployeeId == employee.Id).Select(rider => (Guid?)rider.Id).SingleOrDefault(),
                dbContext.RiderProfiles.Where(rider => rider.EmployeeId == employee.Id).Select(rider => (RiderStatus?)rider.Status).SingleOrDefault(),
                (from assignment in dbContext.EmployeeJobTitlePeriods
                 join title in dbContext.JobTitles on assignment.JobTitleId equals title.Id
                 where assignment.EmployeeId == employee.Id && assignment.EffectiveTo == null
                 select title.NameAr).SingleOrDefault(),
                (from assignment in dbContext.EmployeeJobTitlePeriods
                 join workType in dbContext.OperationalWorkTypes on assignment.OperationalWorkTypeId equals workType.Id
                 where assignment.EmployeeId == employee.Id && assignment.EffectiveTo == null
                 select workType.NameAr).SingleOrDefault(),
                (from assignment in dbContext.EmployeeJobTitlePeriods
                 join operatingCity in dbContext.OperatingCities on assignment.OperatingCityId equals operatingCity.Id
                 join city in dbContext.GlobalCities on operatingCity.GlobalCityId equals city.Id
                 where assignment.EmployeeId == employee.Id && assignment.EffectiveTo == null
                 select city.NameAr).SingleOrDefault(),
                (from period in dbContext.EmployeeSponsorshipPeriods
                 join sponsor in dbContext.Sponsors on period.SponsorId equals sponsor.Id
                 where period.EmployeeId == employee.Id && period.EffectiveTo == null
                 select sponsor.RegistryNameAr).SingleOrDefault(),
                employee.RowVersion))
            .ToArrayAsync(cancellationToken);

        return Result.Success<IReadOnlyList<EmployeeListItemResponse>>(rows.Select(ToEmployeeListItem).ToArray());
    }

    public async Task<Result<EmployeeDetailsResponse>> GetEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.AsNoTracking().SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        return employee is null
            ? Result.Failure<EmployeeDetailsResponse>(HrErrors.NotFound)
            : Result.Success(await BuildEmployeeDetailsAsync(employee, cancellationToken));
    }

    public async Task<Result<EmployeeDetailsResponse>> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)
            || !ValidateEmployee(request, out var status, out var relationshipType))
        {
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);
        }

        var employeeNumber = HrServiceSupport.NormalizeCode(request.EmployeeNumber);
        if (await dbContext.Employees.AnyAsync(item => item.EmployeeNumber == employeeNumber, cancellationToken))
        {
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.Duplicate);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var employee = new Employee
        {
            EmployeeNumber = employeeNumber,
            FullNameAr = request.FullNameAr.Trim(),
            FullNameEn = HrServiceSupport.TrimOrNull(request.FullNameEn),
            NormalizedNameAr = HrServiceSupport.NormalizeText(request.FullNameAr),
            NormalizedNameEn = HrServiceSupport.HasText(request.FullNameEn) ? HrServiceSupport.NormalizeText(request.FullNameEn!) : null,
            PrimaryPhone = HrServiceSupport.TrimOrNull(request.PrimaryPhone),
            NationalityCountryCode = HrServiceSupport.TrimOrNull(request.NationalityCountryCode)?.ToUpperInvariant(),
            HireDate = request.HireDate,
            CurrentStatus = status,
            CurrentRelationshipType = relationshipType,
            Notes = HrServiceSupport.TrimOrNull(request.Notes)
        };
        dbContext.Employees.Add(employee);
        var effectiveFrom = request.HireDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        dbContext.EmployeeStatusPeriods.Add(new EmployeeStatusPeriod
        {
            EmployeeId = employee.Id,
            Status = status,
            EffectiveFrom = effectiveFrom,
            Reason = "Initial employee status.",
            ChangedByUserId = userId
        });
        dbContext.EmployeeRelationshipPeriods.Add(new EmployeeRelationshipPeriod
        {
            EmployeeId = employee.Id,
            RelationshipType = relationshipType,
            EffectiveFrom = effectiveFrom,
            Reason = "Initial employee relationship.",
            ChangedByUserId = userId
        });

        if (relationshipType == EmployeeRelationshipType.SponsoredInternal)
        {
            if (request.SponsoredDetails is null || !await ValidateSponsoredReferencesAsync(employee.Id, request.SponsoredDetails, cancellationToken))
            {
                return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);
            }
            dbContext.SponsoredInternalDetails.Add(CreateSponsoredDetails(employee.Id, request.SponsoredDetails));
        }
        else
        {
            dbContext.OutsideRiderDetails.Add(CreateOutsideDetails(employee.Id, request.OutsideRiderDetails ?? new(null, null, null, null)));
        }

        if (request.Rider is not null)
        {
            if (!TryParseEnum<RiderStatus>(request.Rider.Status, out var riderStatus)
                || !IsValidDateRange(request.Rider.RiderStartDate, request.Rider.RiderEndDate)
                || request.Rider.PreferredCityId is not null
                    && !await dbContext.GlobalCities.AnyAsync(item => item.Id == request.Rider.PreferredCityId, cancellationToken))
            {
                return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);
            }
            dbContext.RiderProfiles.Add(new RiderProfile
            {
                EmployeeId = employee.Id,
                Status = riderStatus,
                RiderStartDate = request.Rider.RiderStartDate,
                RiderEndDate = request.Rider.RiderEndDate,
                PreferredCityId = request.Rider.PreferredCityId,
                OperationalNotes = HrServiceSupport.TrimOrNull(request.Rider.OperationalNotes)
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success(await BuildEmployeeDetailsAsync(employee, cancellationToken));
    }

    public async Task<Result<EmployeeDetailsResponse>> UpdateEmployeeAsync(Guid employeeId, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.NotFound);
        }
        if (!HrServiceSupport.MatchesRowVersion(employee.RowVersion, request.RowVersion))
        {
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.ConcurrencyConflict);
        }
        if (!HrServiceSupport.HasText(request.FullNameAr) || request.NationalityCountryCode?.Trim().Length is > 2)
        {
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);
        }

        employee.FullNameAr = request.FullNameAr.Trim();
        employee.FullNameEn = HrServiceSupport.TrimOrNull(request.FullNameEn);
        employee.NormalizedNameAr = HrServiceSupport.NormalizeText(request.FullNameAr);
        employee.NormalizedNameEn = HrServiceSupport.HasText(request.FullNameEn) ? HrServiceSupport.NormalizeText(request.FullNameEn!) : null;
        employee.PrimaryPhone = HrServiceSupport.TrimOrNull(request.PrimaryPhone);
        employee.NationalityCountryCode = HrServiceSupport.TrimOrNull(request.NationalityCountryCode)?.ToUpperInvariant();
        employee.HireDate = request.HireDate;
        employee.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildEmployeeDetailsAsync(employee, cancellationToken));
    }

    public async Task<Result> ArchiveEmployeeAsync(Guid employeeId, ArchiveRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Reason))
        {
            return Result.Failure(HrErrors.InvalidRequest);
        }
        var employee = await dbContext.Employees.SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(HrErrors.NotFound);
        }
        if (!HrServiceSupport.MatchesRowVersion(employee.RowVersion, request.RowVersion))
        {
            return Result.Failure(HrErrors.ConcurrencyConflict);
        }
        employee.IsDeleted = true;
        employee.DeletionReason = request.Reason.Trim();
        employee.CurrentStatus = EmployeeStatus.Archived;
        var rider = await dbContext.RiderProfiles.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
        if (rider is not null)
        {
            rider.Status = RiderStatus.Archived;
            rider.IsDeleted = true;
            rider.DeletionReason = request.Reason.Trim();
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<EmployeeDetailsResponse>> ChangeStatusAsync(Guid employeeId, ChangeEmployeeStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)
            || !TryParseEnum<EmployeeStatus>(request.Status, out var status)
            || !HrServiceSupport.HasText(request.Reason))
        {
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);
        }
        var employee = await dbContext.Employees.SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.NotFound);
        }
        var current = await dbContext.EmployeeStatusPeriods.SingleOrDefaultAsync(item => item.EmployeeId == employeeId && item.EffectiveTo == null, cancellationToken);
        if (current is not null)
        {
            if (request.EffectiveFrom <= current.EffectiveFrom)
            {
                return Result.Failure<EmployeeDetailsResponse>(HrErrors.Conflict);
            }
            current.EffectiveTo = request.EffectiveFrom.AddDays(-1);
        }
        dbContext.EmployeeStatusPeriods.Add(new EmployeeStatusPeriod
        {
            EmployeeId = employeeId,
            Status = status,
            EffectiveFrom = request.EffectiveFrom,
            ReasonCode = HrServiceSupport.TrimOrNull(request.ReasonCode),
            Reason = request.Reason.Trim(),
            ChangedByUserId = userId
        });
        employee.CurrentStatus = status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildEmployeeDetailsAsync(employee, cancellationToken));
    }

    public async Task<Result<EmployeeDetailsResponse>> ChangeRelationshipAsync(Guid employeeId, ChangeEmployeeRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)
            || !TryParseEnum<EmployeeRelationshipType>(request.RelationshipType, out var type)
            || !HrServiceSupport.HasText(request.Reason))
        {
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);
        }
        var employee = await dbContext.Employees.SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.NotFound);
        }
        var current = await dbContext.EmployeeRelationshipPeriods.SingleOrDefaultAsync(item => item.EmployeeId == employeeId && item.EffectiveTo == null, cancellationToken);
        if (current is not null)
        {
            if (request.EffectiveFrom <= current.EffectiveFrom)
            {
                return Result.Failure<EmployeeDetailsResponse>(HrErrors.Conflict);
            }
            current.EffectiveTo = request.EffectiveFrom.AddDays(-1);
        }
        dbContext.EmployeeRelationshipPeriods.Add(new EmployeeRelationshipPeriod
        {
            EmployeeId = employeeId,
            RelationshipType = type,
            EffectiveFrom = request.EffectiveFrom,
            ReasonCode = HrServiceSupport.TrimOrNull(request.ReasonCode),
            Reason = request.Reason.Trim(),
            SourceReference = HrServiceSupport.TrimOrNull(request.SourceReference),
            ChangedByUserId = userId
        });
        employee.CurrentRelationshipType = type;

        if (type == EmployeeRelationshipType.SponsoredInternal)
        {
            if (request.SponsoredDetails is null || !await ValidateSponsoredReferencesAsync(employeeId, request.SponsoredDetails, cancellationToken))
            {
                return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);
            }
            var details = await dbContext.SponsoredInternalDetails.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
            if (details is null)
            {
                dbContext.SponsoredInternalDetails.Add(CreateSponsoredDetails(employeeId, request.SponsoredDetails));
            }
            else
            {
                ApplySponsoredDetails(details, request.SponsoredDetails);
            }
        }
        else
        {
            var details = await dbContext.OutsideRiderDetails.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
            if (details is null)
            {
                dbContext.OutsideRiderDetails.Add(CreateOutsideDetails(employeeId, request.OutsideRiderDetails ?? new(null, null, null, null)));
            }
            else if (request.OutsideRiderDetails is not null)
            {
                ApplyOutsideDetails(details, request.OutsideRiderDetails);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildEmployeeDetailsAsync(employee, cancellationToken));
    }

    public async Task<Result<EmployeeDetailsResponse>> AssignOperationalWorkAsync(Guid employeeId, AssignOperationalWorkRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId) || !HrServiceSupport.HasText(request.Reason))
        {
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);
        }
        var employee = await dbContext.Employees.SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        var referencesExist = employee is not null
            && await dbContext.JobTitles.AnyAsync(item => item.Id == request.JobTitleId, cancellationToken)
            && await dbContext.OperationalWorkTypes.AnyAsync(item => item.Id == request.OperationalWorkTypeId, cancellationToken)
            && await dbContext.OperatingCities.AnyAsync(item => item.Id == request.OperatingCityId, cancellationToken);
        if (!referencesExist)
        {
            return Result.Failure<EmployeeDetailsResponse>(employee is null ? HrErrors.NotFound : HrErrors.InvalidRequest);
        }
        var allowedTypesExist = await dbContext.JobTitleOperationalWorkTypes.AnyAsync(item => item.JobTitleId == request.JobTitleId, cancellationToken);
        if (allowedTypesExist && !await dbContext.JobTitleOperationalWorkTypes.AnyAsync(
                item => item.JobTitleId == request.JobTitleId && item.OperationalWorkTypeId == request.OperationalWorkTypeId,
                cancellationToken))
        {
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.Conflict);
        }
        var current = await dbContext.EmployeeJobTitlePeriods.SingleOrDefaultAsync(item => item.EmployeeId == employeeId && item.EffectiveTo == null, cancellationToken);
        if (current is not null)
        {
            if (request.EffectiveFrom <= current.EffectiveFrom)
            {
                return Result.Failure<EmployeeDetailsResponse>(HrErrors.Conflict);
            }
            current.EffectiveTo = request.EffectiveFrom.AddDays(-1);
        }
        dbContext.EmployeeJobTitlePeriods.Add(new EmployeeJobTitlePeriod
        {
            EmployeeId = employeeId,
            JobTitleId = request.JobTitleId,
            OperationalWorkTypeId = request.OperationalWorkTypeId,
            OperatingCityId = request.OperatingCityId,
            EffectiveFrom = request.EffectiveFrom,
            Reason = request.Reason.Trim(),
            ChangedByUserId = userId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildEmployeeDetailsAsync(employee!, cancellationToken));
    }

    public async Task<Result<SponsoredInternalDetailsResponse>> UpdateSponsoredDetailsAsync(Guid employeeId, SponsoredInternalDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var details = await dbContext.SponsoredInternalDetails.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
        if (details is null) return Result.Failure<SponsoredInternalDetailsResponse>(HrErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(details.RowVersion, request.RowVersion)) return Result.Failure<SponsoredInternalDetailsResponse>(HrErrors.ConcurrencyConflict);
        if (!await ValidateSponsoredReferencesAsync(employeeId, request, cancellationToken)) return Result.Failure<SponsoredInternalDetailsResponse>(HrErrors.InvalidRequest);
        ApplySponsoredDetails(details, request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToSponsored(details));
    }

    public async Task<Result<OutsideRiderDetailsResponse>> UpdateOutsideRiderDetailsAsync(Guid employeeId, OutsideRiderDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var details = await dbContext.OutsideRiderDetails.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
        if (details is null) return Result.Failure<OutsideRiderDetailsResponse>(HrErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(details.RowVersion, request.RowVersion)) return Result.Failure<OutsideRiderDetailsResponse>(HrErrors.ConcurrencyConflict);
        ApplyOutsideDetails(details, request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToOutside(details));
    }

    public async Task<Result<IReadOnlyList<RiderDetailsResponse>>> GetRidersAsync(bool? outsideOnly, CancellationToken cancellationToken = default)
    {
        var query = from rider in dbContext.RiderProfiles.AsNoTracking()
                    join employee in dbContext.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
                    join city in dbContext.GlobalCities.AsNoTracking() on rider.PreferredCityId equals city.Id into cities
                    from city in cities.DefaultIfEmpty()
                    where outsideOnly != true || employee.CurrentRelationshipType == EmployeeRelationshipType.OutsideRider
                    orderby employee.FullNameAr
                    select new RiderProjection(rider.Id, rider.EmployeeId, employee.EmployeeNumber, employee.FullNameAr,
                        employee.FullNameEn, rider.Status, rider.RiderStartDate, rider.RiderEndDate, rider.PreferredCityId,
                        city == null ? null : city.NameAr, rider.OperationalNotes,
                        employee.CurrentRelationshipType == EmployeeRelationshipType.OutsideRider, rider.RowVersion);
        var rows = await query.ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<RiderDetailsResponse>>(rows.Select(ToRider).ToArray());
    }

    public async Task<Result<RiderDetailsResponse>> CreateRiderProfileAsync(Guid employeeId, CreateRiderProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryParseEnum<RiderStatus>(request.Status, out var status)
            || !IsValidDateRange(request.RiderStartDate, request.RiderEndDate))
        {
            return Result.Failure<RiderDetailsResponse>(HrErrors.InvalidRequest);
        }
        var employee = await dbContext.Employees.SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<RiderDetailsResponse>(HrErrors.NotFound);
        }
        if (await dbContext.RiderProfiles.AnyAsync(item => item.EmployeeId == employeeId, cancellationToken)
            || request.PreferredCityId is not null && !await dbContext.GlobalCities.AnyAsync(item => item.Id == request.PreferredCityId, cancellationToken))
        {
            return Result.Failure<RiderDetailsResponse>(HrErrors.Conflict);
        }
        var rider = new RiderProfile
        {
            EmployeeId = employeeId,
            Status = status,
            RiderStartDate = request.RiderStartDate,
            RiderEndDate = request.RiderEndDate,
            PreferredCityId = request.PreferredCityId,
            OperationalNotes = HrServiceSupport.TrimOrNull(request.OperationalNotes)
        };
        dbContext.RiderProfiles.Add(rider);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await GetRiderAsync(rider.Id, cancellationToken));
    }

    public async Task<Result<RiderDetailsResponse>> UpdateRiderProfileAsync(Guid riderProfileId, UpdateRiderProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryParseEnum<RiderStatus>(request.Status, out var status)
            || !IsValidDateRange(request.RiderStartDate, request.RiderEndDate))
        {
            return Result.Failure<RiderDetailsResponse>(HrErrors.InvalidRequest);
        }
        var rider = await dbContext.RiderProfiles.SingleOrDefaultAsync(item => item.Id == riderProfileId, cancellationToken);
        if (rider is null)
        {
            return Result.Failure<RiderDetailsResponse>(HrErrors.NotFound);
        }
        if (!HrServiceSupport.MatchesRowVersion(rider.RowVersion, request.RowVersion))
        {
            return Result.Failure<RiderDetailsResponse>(HrErrors.ConcurrencyConflict);
        }
        if (request.PreferredCityId is not null && !await dbContext.GlobalCities.AnyAsync(item => item.Id == request.PreferredCityId, cancellationToken))
        {
            return Result.Failure<RiderDetailsResponse>(HrErrors.InvalidRequest);
        }
        rider.Status = status;
        rider.RiderStartDate = request.RiderStartDate;
        rider.RiderEndDate = request.RiderEndDate;
        rider.PreferredCityId = request.PreferredCityId;
        rider.OperationalNotes = HrServiceSupport.TrimOrNull(request.OperationalNotes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await GetRiderAsync(rider.Id, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<SponsorResponse>>> GetSponsorsAsync(CancellationToken cancellationToken = default)
    {
        var sponsors = await dbContext.Sponsors.AsNoTracking().OrderBy(item => item.RegistryNameAr).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<SponsorResponse>>(sponsors.Select(ToSponsor).ToArray());
    }

    public async Task<Result<SponsorResponse>> GetSponsorAsync(Guid sponsorId, CancellationToken cancellationToken = default)
    {
        var sponsor = await dbContext.Sponsors.AsNoTracking().SingleOrDefaultAsync(item => item.Id == sponsorId, cancellationToken);
        return sponsor is null ? Result.Failure<SponsorResponse>(HrErrors.NotFound) : Result.Success(ToSponsor(sponsor));
    }

    public async Task<Result<SponsorResponse>> UpsertSponsorAsync(Guid? sponsorId, SponsorUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.EmployerIdentityNumber)
            || !HrServiceSupport.HasText(request.RegistryNameAr)
            || !TryParseEnum<SponsorType>(request.SponsorType, out var type)
            || !TryParseEnum<CatalogStatus>(request.Status, out var status)
            || !IsValidDateRange(request.ActiveFrom, request.ActiveTo))
        {
            return Result.Failure<SponsorResponse>(HrErrors.InvalidRequest);
        }
        Sponsor sponsor;
        if (sponsorId is null)
        {
            sponsor = new Sponsor { CompanyProfileId = Domain.Entities.Platform.CompanyProfile.FixedId };
            dbContext.Sponsors.Add(sponsor);
        }
        else
        {
            sponsor = await dbContext.Sponsors.SingleOrDefaultAsync(item => item.Id == sponsorId, cancellationToken) ?? null!;
            if (sponsor is null)
            {
                return Result.Failure<SponsorResponse>(HrErrors.NotFound);
            }
            if (!HrServiceSupport.MatchesRowVersion(sponsor.RowVersion, request.RowVersion))
            {
                return Result.Failure<SponsorResponse>(HrErrors.ConcurrencyConflict);
            }
        }
        var employerId = HrServiceSupport.NormalizeIdentifier(request.EmployerIdentityNumber);
        if (await dbContext.Sponsors.AnyAsync(item => item.Id != sponsor.Id && item.EmployerIdentityNumber == employerId, cancellationToken))
        {
            return Result.Failure<SponsorResponse>(HrErrors.Duplicate);
        }
        sponsor.EmployerIdentityNumber = employerId;
        sponsor.RegistryNameAr = request.RegistryNameAr.Trim();
        sponsor.RegistryNameEn = HrServiceSupport.TrimOrNull(request.RegistryNameEn);
        sponsor.CommercialRegistrationNumber = HrServiceSupport.TrimOrNull(request.CommercialRegistrationNumber);
        sponsor.UnifiedNationalNumber = HrServiceSupport.TrimOrNull(request.UnifiedNationalNumber);
        sponsor.SponsorType = type;
        sponsor.Status = status;
        sponsor.ActiveFrom = request.ActiveFrom;
        sponsor.ActiveTo = request.ActiveTo;
        sponsor.ContactName = HrServiceSupport.TrimOrNull(request.ContactName);
        sponsor.ContactPhone = HrServiceSupport.TrimOrNull(request.ContactPhone);
        sponsor.ContactEmail = HrServiceSupport.TrimOrNull(request.ContactEmail);
        sponsor.Address = HrServiceSupport.ToAddress(request.Address);
        sponsor.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToSponsor(sponsor));
    }

    public async Task<Result> ArchiveSponsorAsync(Guid sponsorId, ArchiveRequest request, CancellationToken cancellationToken = default)
    {
        var sponsor = await dbContext.Sponsors.SingleOrDefaultAsync(item => item.Id == sponsorId, cancellationToken);
        if (sponsor is null)
        {
            return Result.Failure(HrErrors.NotFound);
        }
        if (!HrServiceSupport.HasText(request.Reason) || !HrServiceSupport.MatchesRowVersion(sponsor.RowVersion, request.RowVersion))
        {
            return Result.Failure(HrErrors.ConcurrencyConflict);
        }
        if (await dbContext.EmployeeSponsorshipPeriods.AnyAsync(item => item.SponsorId == sponsorId && item.EffectiveTo == null, cancellationToken))
        {
            return Result.Failure(HrErrors.Conflict);
        }
        sponsor.IsDeleted = true;
        sponsor.Status = CatalogStatus.Archived;
        sponsor.DeletionReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<SponsorshipPeriodResponse>>> GetSponsorshipHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        Result.Success<IReadOnlyList<SponsorshipPeriodResponse>>(await BuildSponsorshipHistory(employeeId, cancellationToken));

    public async Task<Result<IReadOnlyList<SponsorshipPeriodResponse>>> ChangeSponsorshipAsync(Guid employeeId, ChangeSponsorshipRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)
            || !TryParseEnum<SponsorshipStatus>(request.Status, out var status)
            || !HrServiceSupport.HasText(request.Reason))
        {
            return Result.Failure<IReadOnlyList<SponsorshipPeriodResponse>>(HrErrors.InvalidRequest);
        }
        var employeeExists = await dbContext.Employees.AnyAsync(item => item.Id == employeeId, cancellationToken);
        var sponsorExists = await dbContext.Sponsors.AnyAsync(item => item.Id == request.SponsorId && item.Status == CatalogStatus.Active, cancellationToken);
        if (!employeeExists || !sponsorExists)
        {
            return Result.Failure<IReadOnlyList<SponsorshipPeriodResponse>>(HrErrors.NotFound);
        }
        var current = await dbContext.EmployeeSponsorshipPeriods.SingleOrDefaultAsync(item => item.EmployeeId == employeeId && item.EffectiveTo == null, cancellationToken);
        if (current is not null)
        {
            if (request.EffectiveFrom <= current.EffectiveFrom)
            {
                return Result.Failure<IReadOnlyList<SponsorshipPeriodResponse>>(HrErrors.Conflict);
            }
            current.EffectiveTo = request.EffectiveFrom.AddDays(-1);
        }
        dbContext.EmployeeSponsorshipPeriods.Add(new EmployeeSponsorshipPeriod
        {
            EmployeeId = employeeId,
            SponsorId = request.SponsorId,
            Status = status,
            EffectiveFrom = request.EffectiveFrom,
            Reason = request.Reason.Trim(),
            SourceReference = HrServiceSupport.TrimOrNull(request.SourceReference),
            ChangedByUserId = userId
        });
        var details = await dbContext.SponsoredInternalDetails.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
        if (details is not null)
        {
            details.CurrentSponsorId = request.SponsorId;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<SponsorshipPeriodResponse>>(await BuildSponsorshipHistory(employeeId, cancellationToken));
    }

    private async Task<EmployeeDetailsResponse> BuildEmployeeDetailsAsync(Employee employee, CancellationToken cancellationToken)
    {
        var listItem = (await GetEmployeesAsync(cancellationToken)).Value!.Single(item => item.Id == employee.Id);
        var sponsored = await dbContext.SponsoredInternalDetails.AsNoTracking().SingleOrDefaultAsync(item => item.EmployeeId == employee.Id, cancellationToken);
        var outside = await dbContext.OutsideRiderDetails.AsNoTracking().SingleOrDefaultAsync(item => item.EmployeeId == employee.Id, cancellationToken);
        var rider = await dbContext.RiderProfiles.AsNoTracking().SingleOrDefaultAsync(item => item.EmployeeId == employee.Id, cancellationToken);
        var statusHistory = await dbContext.EmployeeStatusPeriods.AsNoTracking().Where(item => item.EmployeeId == employee.Id)
            .OrderByDescending(item => item.EffectiveFrom)
            .Select(item => new PeriodResponse(item.Id, item.Status.ToString(), item.EffectiveFrom, item.EffectiveTo, item.Reason, item.ChangedByUserId))
            .ToArrayAsync(cancellationToken);
        var relationshipHistory = await dbContext.EmployeeRelationshipPeriods.AsNoTracking().Where(item => item.EmployeeId == employee.Id)
            .OrderByDescending(item => item.EffectiveFrom)
            .Select(item => new PeriodResponse(item.Id, item.RelationshipType.ToString(), item.EffectiveFrom, item.EffectiveTo, item.Reason, item.ChangedByUserId))
            .ToArrayAsync(cancellationToken);
        var assignments = await (from period in dbContext.EmployeeJobTitlePeriods.AsNoTracking()
                                 join title in dbContext.JobTitles.AsNoTracking() on period.JobTitleId equals title.Id
                                 join workType in dbContext.OperationalWorkTypes.AsNoTracking() on period.OperationalWorkTypeId equals workType.Id
                                 join operatingCity in dbContext.OperatingCities.AsNoTracking() on period.OperatingCityId equals operatingCity.Id
                                 join city in dbContext.GlobalCities.AsNoTracking() on operatingCity.GlobalCityId equals city.Id
                                 where period.EmployeeId == employee.Id
                                 orderby period.EffectiveFrom descending
                                 select new OperationalAssignmentResponse(period.Id, title.Id, title.NameAr, workType.Id, workType.NameAr,
                                     operatingCity.Id, city.NameAr, period.EffectiveFrom, period.EffectiveTo, period.Reason))
            .ToArrayAsync(cancellationToken);

        return new EmployeeDetailsResponse(
            listItem,
            sponsored is null ? null : ToSponsored(sponsored),
            outside is null ? null : ToOutside(outside),
            rider is null ? null : await GetRiderAsync(rider.Id, cancellationToken),
            statusHistory,
            relationshipHistory,
            assignments,
            await BuildSponsorshipHistory(employee.Id, cancellationToken));
    }

    private async Task<RiderDetailsResponse> GetRiderAsync(Guid riderId, CancellationToken cancellationToken)
    {
        var row = await (from rider in dbContext.RiderProfiles.AsNoTracking()
                         join employee in dbContext.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
                         join city in dbContext.GlobalCities.AsNoTracking() on rider.PreferredCityId equals city.Id into cities
                         from city in cities.DefaultIfEmpty()
                         where rider.Id == riderId
                         select new RiderProjection(rider.Id, rider.EmployeeId, employee.EmployeeNumber, employee.FullNameAr,
                             employee.FullNameEn, rider.Status, rider.RiderStartDate, rider.RiderEndDate, rider.PreferredCityId,
                             city == null ? null : city.NameAr, rider.OperationalNotes,
                             employee.CurrentRelationshipType == EmployeeRelationshipType.OutsideRider, rider.RowVersion))
            .SingleAsync(cancellationToken);
        return ToRider(row);
    }

    private async Task<SponsorshipPeriodResponse[]> BuildSponsorshipHistory(Guid employeeId, CancellationToken cancellationToken) =>
        await (from period in dbContext.EmployeeSponsorshipPeriods.AsNoTracking()
               join sponsor in dbContext.Sponsors.IgnoreQueryFilters().AsNoTracking() on period.SponsorId equals sponsor.Id
               where period.EmployeeId == employeeId
               orderby period.EffectiveFrom descending
               select new SponsorshipPeriodResponse(period.Id, sponsor.Id, sponsor.RegistryNameAr, sponsor.EmployerIdentityNumber,
                   period.Status.ToString(), period.EffectiveFrom, period.EffectiveTo, period.Reason, period.SourceReference))
            .ToArrayAsync(cancellationToken);

    private async Task<bool> ValidateSponsoredReferencesAsync(Guid employeeId, SponsoredInternalDetailsRequest request, CancellationToken cancellationToken)
    {
        if (request.DependentsCount is < 0 || !IsValidDateRange(request.ContractStartDate, request.ContractEndDate))
        {
            return false;
        }
        if (request.CurrentSponsorId is not null && !await dbContext.Sponsors.AnyAsync(item => item.Id == request.CurrentSponsorId, cancellationToken))
        {
            return false;
        }
        if (request.ManagerEmployeeId is not null && (request.ManagerEmployeeId == employeeId || !await dbContext.Employees.AnyAsync(item => item.Id == request.ManagerEmployeeId, cancellationToken)))
        {
            return false;
        }
        return (request.Gender is null || TryParseEnum<Gender>(request.Gender, out _))
            && (request.MaritalStatus is null || TryParseEnum<MaritalStatus>(request.MaritalStatus, out _));
    }

    private static SponsoredInternalDetails CreateSponsoredDetails(Guid employeeId, SponsoredInternalDetailsRequest request)
    {
        var entity = new SponsoredInternalDetails { EmployeeId = employeeId };
        ApplySponsoredDetails(entity, request);
        return entity;
    }

    private static void ApplySponsoredDetails(SponsoredInternalDetails entity, SponsoredInternalDetailsRequest request)
    {
        entity.Gender = request.Gender is null ? null : Enum.Parse<Gender>(request.Gender, true);
        entity.BirthDate = request.BirthDate;
        entity.SecondaryPhone = HrServiceSupport.TrimOrNull(request.SecondaryPhone);
        entity.Email = HrServiceSupport.TrimOrNull(request.Email);
        entity.ProfilePhotoDocumentId = request.ProfilePhotoDocumentId;
        entity.MaritalStatus = request.MaritalStatus is null ? null : Enum.Parse<MaritalStatus>(request.MaritalStatus, true);
        entity.DependentsCount = request.DependentsCount;
        entity.EducationLevel = HrServiceSupport.TrimOrNull(request.EducationLevel);
        entity.EducationDetails = HrServiceSupport.TrimOrNull(request.EducationDetails);
        entity.Profession = HrServiceSupport.TrimOrNull(request.Profession);
        entity.HomeAddress = HrServiceSupport.ToAddress(request.HomeAddress);
        entity.EmergencyContactName = HrServiceSupport.TrimOrNull(request.EmergencyContactName);
        entity.EmergencyContactRelationship = HrServiceSupport.TrimOrNull(request.EmergencyContactRelationship);
        entity.EmergencyContactPhone = HrServiceSupport.TrimOrNull(request.EmergencyContactPhone);
        entity.ContractStartDate = request.ContractStartDate;
        entity.ContractEndDate = request.ContractEndDate;
        entity.ProbationEndDate = request.ProbationEndDate;
        entity.TerminationDate = request.TerminationDate;
        entity.ManagerEmployeeId = request.ManagerEmployeeId;
        entity.CurrentSponsorId = request.CurrentSponsorId;
        entity.InternalNotes = HrServiceSupport.TrimOrNull(request.InternalNotes);
    }

    private static OutsideRiderDetails CreateOutsideDetails(Guid employeeId, OutsideRiderDetailsRequest request)
    {
        var entity = new OutsideRiderDetails { EmployeeId = employeeId };
        ApplyOutsideDetails(entity, request);
        return entity;
    }

    private static void ApplyOutsideDetails(OutsideRiderDetails entity, OutsideRiderDetailsRequest request)
    {
        entity.AlternateContactName = HrServiceSupport.TrimOrNull(request.AlternateContactName);
        entity.AlternateContactPhone = HrServiceSupport.TrimOrNull(request.AlternateContactPhone);
        entity.EngagementReference = HrServiceSupport.TrimOrNull(request.EngagementReference);
        entity.EngagementNotes = HrServiceSupport.TrimOrNull(request.EngagementNotes);
    }

    private static SponsoredInternalDetailsResponse ToSponsored(SponsoredInternalDetails item) => new(
        item.Id, item.EmployeeId, item.Gender?.ToString(), item.BirthDate, item.SecondaryPhone, item.Email,
        item.ProfilePhotoDocumentId, item.MaritalStatus?.ToString(), item.DependentsCount, item.EducationLevel,
        item.EducationDetails, item.Profession, HrServiceSupport.ToAddressResponse(item.HomeAddress),
        item.EmergencyContactName, item.EmergencyContactRelationship, item.EmergencyContactPhone,
        item.ContractStartDate, item.ContractEndDate, item.ProbationEndDate, item.TerminationDate,
        item.ManagerEmployeeId, item.CurrentSponsorId, item.InternalNotes, HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private static OutsideRiderDetailsResponse ToOutside(OutsideRiderDetails item) => new(
        item.Id, item.EmployeeId, item.AlternateContactName, item.AlternateContactPhone, item.EngagementReference,
        item.EngagementNotes, HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private static SponsorResponse ToSponsor(Sponsor item) => new(
        item.Id, item.CompanyProfileId, item.EmployerIdentityNumber, item.RegistryNameAr, item.RegistryNameEn,
        item.CommercialRegistrationNumber, item.UnifiedNationalNumber, item.SponsorType.ToString(), item.Status.ToString(),
        item.ActiveFrom, item.ActiveTo, item.ContactName, item.ContactPhone, item.ContactEmail,
        HrServiceSupport.ToAddressResponse(item.Address), item.Notes, HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private static EmployeeListItemResponse ToEmployeeListItem(EmployeeListProjection item) => new(
        item.Id, item.EmployeeNumber, item.FullNameAr, item.FullNameEn, item.PrimaryPhone,
        item.NationalityCountryCode, item.HireDate, item.Status.ToString(), item.RelationshipType?.ToString(),
        item.RiderProfileId, item.RiderStatus?.ToString(), item.JobTitleAr, item.OperationalWorkTypeAr,
        item.OperatingCityAr, item.SponsorNameAr, HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private static RiderDetailsResponse ToRider(RiderProjection item) => new(
        item.Id, item.EmployeeId, item.EmployeeNumber, item.FullNameAr, item.FullNameEn, item.Status.ToString(),
        item.RiderStartDate, item.RiderEndDate, item.PreferredCityId, item.PreferredCityAr, item.OperationalNotes,
        item.IsOutsideRider, HrServiceSupport.EncodeRowVersion(item.RowVersion));

    // The Employees API is temporarily available anonymously for Swagger verification.
    // Keep a deterministic actor on history records until controller authorization is restored.
    private static readonly Guid AnonymousEmployeeApiActorId =
        Guid.Parse("019c18d5-62e1-7000-d000-000000000002");

    private bool TryGetUserId(out Guid userId)
    {
        userId = currentUser.UserId ?? AnonymousEmployeeApiActorId;
        return true;
    }

    private static bool ValidateEmployee(CreateEmployeeRequest request, out EmployeeStatus status, out EmployeeRelationshipType type)
    {
        status = default;
        type = default;
        return HrServiceSupport.HasText(request.EmployeeNumber)
            && HrServiceSupport.HasText(request.FullNameAr)
            && request.NationalityCountryCode?.Trim().Length is not > 2
            && TryParseEnum(request.Status, out status)
            && TryParseEnum(request.RelationshipType, out type);
    }

    private static bool TryParseEnum<TEnum>(string? value, out TEnum parsed) where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed);

    private static bool IsValidDateRange(DateOnly? start, DateOnly? end) => end is null || start is null || end >= start;

    private sealed record EmployeeListProjection(Guid Id, string EmployeeNumber, string FullNameAr, string? FullNameEn,
        string? PrimaryPhone, string? NationalityCountryCode, DateOnly? HireDate, EmployeeStatus Status,
        EmployeeRelationshipType? RelationshipType, Guid? RiderProfileId, RiderStatus? RiderStatus,
        string? JobTitleAr, string? OperationalWorkTypeAr, string? OperatingCityAr, string? SponsorNameAr, byte[] RowVersion);

    private sealed record RiderProjection(Guid Id, Guid EmployeeId, string EmployeeNumber, string FullNameAr,
        string? FullNameEn, RiderStatus Status, DateOnly? RiderStartDate, DateOnly? RiderEndDate,
        Guid? PreferredCityId, string? PreferredCityAr, string? OperationalNotes, bool IsOutsideRider, byte[] RowVersion);
}
