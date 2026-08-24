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
    private static readonly Guid SystemActorId = Guid.Parse("019c18d5-62e1-7000-d000-000000000002");

    public async Task<Result<IReadOnlyList<EmployeeListItemResponse>>> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var employees = await dbContext.Employees.AsNoTracking().OrderBy(item => item.FullNameAr).ToArrayAsync(cancellationToken);
        var employeeIds = employees.Select(item => item.Id).ToArray();
        var riderIds = await dbContext.RiderProfiles.AsNoTracking().Where(item => employeeIds.Contains(item.EmployeeId))
            .ToDictionaryAsync(item => item.EmployeeId, item => item.Id, cancellationToken);
        var riders = await dbContext.RiderProfiles.AsNoTracking().Where(item => employeeIds.Contains(item.EmployeeId))
            .ToDictionaryAsync(item => item.EmployeeId, cancellationToken);
        var sponsorIds = employees.Where(item => item.SponsorId.HasValue).Select(item => item.SponsorId!.Value).Distinct().ToArray();
        var sponsors = await dbContext.Sponsors.AsNoTracking().Where(item => sponsorIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.RegistryNameAr, cancellationToken);
        var workTypeIds = employees.Where(item => item.OperationalWorkTypeId.HasValue).Select(item => item.OperationalWorkTypeId!.Value).Distinct().ToArray();
        var workTypes = await dbContext.OperationalWorkTypes.AsNoTracking().Where(item => workTypeIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var operatingCityIds = employees.Where(item => item.OperatingCityId.HasValue).Select(item => item.OperatingCityId!.Value).Distinct().ToArray();
        var operatingCities = await (from operatingCity in dbContext.OperatingCities.AsNoTracking()
                                     join city in dbContext.GlobalCities.AsNoTracking() on operatingCity.GlobalCityId equals city.Id
                                     where operatingCityIds.Contains(operatingCity.Id)
                                     select new OperatingCityResponse(operatingCity.Id, operatingCity.GlobalCityId, city.Code,
                                         city.NameAr, city.NameEn, operatingCity.Status.ToString(), operatingCity.EnabledFrom,
                                         operatingCity.DisabledAt, HrServiceSupport.EncodeRowVersion(operatingCity.RowVersion)))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var housingNames = (await (from residence in dbContext.HousingResidencePeriods.AsNoTracking()
                                   join housing in dbContext.Housing.AsNoTracking() on residence.HousingId equals housing.Id
                                   where employeeIds.Contains(residence.EmployeeId) && residence.EffectiveTo == null
                                   orderby residence.EffectiveFrom descending
                                   select new EmployeeHousingNameProjection(residence.EmployeeId, housing.NameAr)).ToArrayAsync(cancellationToken))
            .GroupBy(item => item.EmployeeId)
            .ToDictionary(group => group.Key, group => group.First().HousingNameAr);

        return Result.Success<IReadOnlyList<EmployeeListItemResponse>>(employees.Select(item => new EmployeeListItemResponse(
            item.Id, item.IqamaNo, item.FullNameAr, item.FullNameEn, item.Nationality, item.PrimaryPhone,
            item.IsEmployee, item.EngagementType.ToString(), item.Status.ToString(), item.WorkingForMeAs,
            item.ResidencyProfession, item.SponsorId,
            item.SponsorId is { } sponsorId && sponsors.TryGetValue(sponsorId, out var sponsorName) ? sponsorName : null,
            riderIds.GetValueOrDefault(item.Id), HrServiceSupport.EncodeRowVersion(item.RowVersion),
            ToEmployee(item), riders.TryGetValue(item.Id, out var rider) ? ToRider(rider, item) : null,
            item.OperationalWorkTypeId is { } workTypeId && workTypes.TryGetValue(workTypeId, out var workType)
                ? new CatalogResponse(workType.Id, workType.Code, workType.NameAr, workType.NameEn, workType.Status.ToString(), HrServiceSupport.EncodeRowVersion(workType.RowVersion))
                : null,
            item.OperatingCityId is { } operatingCityId && operatingCities.TryGetValue(operatingCityId, out var operatingCity)
                ? operatingCity : null,
            housingNames.GetValueOrDefault(item.Id))).ToArray());
    }

    public async Task<Result<EmployeeDetailsResponse>> GetEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.AsNoTracking().SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        return employee is null
            ? Result.Failure<EmployeeDetailsResponse>(HrErrors.NotFound)
            : Result.Success(await BuildEmployeeDetailsAsync(employee, cancellationToken));
    }

    public async Task<Result<EmployeeDetailsResponse>> CreateEmployeeAsync(EmployeeUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateRequestAsync(request, null, cancellationToken);
        if (validation.IsFailure) return Result.Failure<EmployeeDetailsResponse>(validation.Error);

        var employee = new Employee();
        Apply(employee, request, validation.Value!.Gender, validation.Value.MaritalStatus, validation.Value.EngagementType, validation.Value.Status);
        dbContext.Employees.Add(employee);

        var actor = ActorId;
        AddHistory(employee.Id, EmployeeWorkChangeType.Role, null, employee.IsEmployee ? "Administrative" : "Rider", DateOnly.FromDateTime(DateTime.UtcNow), "Employee record created.", actor);
        AddHistory(employee.Id, EmployeeWorkChangeType.Status, null, employee.Status.ToString(), DateOnly.FromDateTime(DateTime.UtcNow), "Employee record created.", actor);
        AddHistory(employee.Id, EmployeeWorkChangeType.Engagement, null, employee.EngagementType.ToString(), DateOnly.FromDateTime(DateTime.UtcNow), "Employee record created.", actor);

        if (!employee.IsEmployee)
        {
            var rider = new RiderProfile { EmployeeId = employee.Id };
            ApplyRider(rider, request.Rider!);
            dbContext.RiderProfiles.Add(rider);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildEmployeeDetailsAsync(employee, cancellationToken));
    }

    public async Task<Result<EmployeeDetailsResponse>> UpdateEmployeeAsync(Guid employeeId, EmployeeUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        if (employee is null) return Result.Failure<EmployeeDetailsResponse>(HrErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(employee.RowVersion, request.RowVersion))
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.ConcurrencyConflict);

        var validation = await ValidateRequestAsync(request, employeeId, cancellationToken);
        if (validation.IsFailure) return Result.Failure<EmployeeDetailsResponse>(validation.Error);
        if (employee.IsEmployee != request.IsEmployee || employee.Status != validation.Value!.Status)
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);

        var actor = ActorId;
        var effectiveDate = DateOnly.FromDateTime(DateTime.UtcNow);
        TrackChanges(employee, request, validation.Value!, effectiveDate, "Employee details updated.", actor);
        Apply(employee, request, validation.Value!.Gender, validation.Value.MaritalStatus, validation.Value.EngagementType, validation.Value.Status);

        var rider = await dbContext.RiderProfiles.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
        if (!request.IsEmployee)
        {
            if (rider is null && request.Rider is null)
                return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);
            rider ??= new RiderProfile { EmployeeId = employeeId };
            if (request.Rider is not null)
            {
                if (dbContext.Entry(rider).State != EntityState.Detached && request.Rider.RowVersion is not null
                    && !HrServiceSupport.MatchesRowVersion(rider.RowVersion, request.Rider.RowVersion))
                    return Result.Failure<EmployeeDetailsResponse>(HrErrors.ConcurrencyConflict);
                ApplyRider(rider, request.Rider);
            }
            if (dbContext.Entry(rider).State == EntityState.Detached) dbContext.RiderProfiles.Add(rider);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildEmployeeDetailsAsync(employee, cancellationToken));
    }

    public async Task<Result> ArchiveEmployeeAsync(Guid employeeId, ArchiveRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        if (employee is null) return Result.Failure(HrErrors.NotFound);
        if (!HrServiceSupport.HasText(request.Reason) || !HrServiceSupport.MatchesRowVersion(employee.RowVersion, request.RowVersion))
            return Result.Failure(HrErrors.ConcurrencyConflict);
        var riderId = await dbContext.RiderProfiles.Where(item => item.EmployeeId == employeeId).Select(item => (Guid?)item.Id).SingleOrDefaultAsync(cancellationToken);
        if (riderId is not null && (await dbContext.RiderClientAssignments.AnyAsync(item => item.RiderProfileId == riderId && item.EffectiveTo == null, cancellationToken)
            || await dbContext.RiderVehicleAssignments.AnyAsync(item => item.RiderProfileId == riderId && item.EndedAtUtc == null, cancellationToken)))
            return Result.Failure(HrErrors.Conflict);

        AddHistory(employee.Id, EmployeeWorkChangeType.Status, employee.Status.ToString(), EmployeeStatus.Archived.ToString(),
            DateOnly.FromDateTime(DateTime.UtcNow), request.Reason.Trim(), ActorId);
        employee.Status = EmployeeStatus.Archived;
        employee.IsDeleted = true;
        employee.DeletionReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<EmployeeDetailsResponse>> ChangeStatusAsync(Guid employeeId, ChangeEmployeeStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryParseEnum<EmployeeStatus>(request.Status, out var status) || status == EmployeeStatus.Archived
            || !HrServiceSupport.HasText(request.Reason))
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);
        var employee = await dbContext.Employees.SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        if (employee is null) return Result.Failure<EmployeeDetailsResponse>(HrErrors.NotFound);
        if (employee.Status == status) return Result.Failure<EmployeeDetailsResponse>(HrErrors.Conflict);
        if (status == EmployeeStatus.Active && (!IsValidIqama(employee.IqamaNo)
            || employee.EngagementType == EmployeeRelationshipType.SponsoredInternal && employee.SponsorId is null
            || !employee.IsEmployee && !await dbContext.RiderProfiles.AnyAsync(item => item.EmployeeId == employeeId, cancellationToken)))
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);

        AddHistory(employeeId, EmployeeWorkChangeType.Status, employee.Status.ToString(), status.ToString(), request.EffectiveDate, request.Reason.Trim(), ActorId);
        employee.Status = status;
        employee.StatusReason = status is EmployeeStatus.Suspended or EmployeeStatus.Terminated ? request.Reason.Trim() : null;
        if (status == EmployeeStatus.Terminated) employee.TerminationDate = request.EffectiveDate;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildEmployeeDetailsAsync(employee, cancellationToken));
    }

    public async Task<Result<EmployeeDetailsResponse>> ChangeRoleAsync(Guid employeeId, ChangeEmployeeRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Reason) || !request.IsEmployee && request.Rider is null)
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);
        var employee = await dbContext.Employees.SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        if (employee is null) return Result.Failure<EmployeeDetailsResponse>(HrErrors.NotFound);
        if (employee.IsEmployee == request.IsEmployee) return Result.Failure<EmployeeDetailsResponse>(HrErrors.Conflict);
        if (request.IsEmployee && employee.EngagementType == EmployeeRelationshipType.OutsideRider)
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.InvalidRequest);

        var rider = await dbContext.RiderProfiles.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
        if (request.IsEmployee && rider is not null && (await dbContext.RiderClientAssignments.AnyAsync(item => item.RiderProfileId == rider.Id && item.EffectiveTo == null, cancellationToken)
            || await dbContext.RiderVehicleAssignments.AnyAsync(item => item.RiderProfileId == rider.Id && item.EndedAtUtc == null, cancellationToken)))
            return Result.Failure<EmployeeDetailsResponse>(HrErrors.Conflict);

        if (!request.IsEmployee)
        {
            rider ??= new RiderProfile { EmployeeId = employeeId };
            ApplyRider(rider, request.Rider!);
            if (dbContext.Entry(rider).State == EntityState.Detached) dbContext.RiderProfiles.Add(rider);
        }

        AddHistory(employeeId, EmployeeWorkChangeType.Role, employee.IsEmployee ? "Administrative" : "Rider",
            request.IsEmployee ? "Administrative" : "Rider", request.EffectiveDate, request.Reason.Trim(), ActorId);
        employee.IsEmployee = request.IsEmployee;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildEmployeeDetailsAsync(employee, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<EmployeeWorkHistoryResponse>>> GetWorkHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Employees.AnyAsync(item => item.Id == employeeId, cancellationToken))
            return Result.Failure<IReadOnlyList<EmployeeWorkHistoryResponse>>(HrErrors.NotFound);
        return Result.Success<IReadOnlyList<EmployeeWorkHistoryResponse>>(await BuildHistoryAsync(employeeId, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<RiderDetailsResponse>>> GetRidersAsync(bool? outsideOnly, CancellationToken cancellationToken = default)
    {
        var rows = await (from rider in dbContext.RiderProfiles.AsNoTracking()
                          join employee in dbContext.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
                          where !employee.IsEmployee && (outsideOnly != true || employee.EngagementType == EmployeeRelationshipType.OutsideRider)
                          orderby employee.FullNameAr
                          select new { Rider = rider, Employee = employee }).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<RiderDetailsResponse>>(rows.Select(row => ToRider(row.Rider, row.Employee)).ToArray());
    }

    public async Task<Result<RiderDetailsResponse>> UpdateRiderProfileAsync(Guid riderProfileId, RiderProfileUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryParseOptionalEnum<TShirtSize>(request.TShirtSize, out var size))
            return Result.Failure<RiderDetailsResponse>(HrErrors.InvalidRequest);
        var rider = await dbContext.RiderProfiles.SingleOrDefaultAsync(item => item.Id == riderProfileId, cancellationToken);
        if (rider is null) return Result.Failure<RiderDetailsResponse>(HrErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(rider.RowVersion, request.RowVersion))
            return Result.Failure<RiderDetailsResponse>(HrErrors.ConcurrencyConflict);
        rider.TShirtSize = size;
        rider.OperationalNotes = HrServiceSupport.TrimOrNull(request.OperationalNotes);
        await dbContext.SaveChangesAsync(cancellationToken);
        var employee = await dbContext.Employees.AsNoTracking().SingleAsync(item => item.Id == rider.EmployeeId, cancellationToken);
        return Result.Success(ToRider(rider, employee));
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
        if (!HrServiceSupport.HasText(request.EmployerIdentityNumber) || !HrServiceSupport.HasText(request.RegistryNameAr)
            || !TryParseEnum<SponsorType>(request.SponsorType, out var type)
            || !TryParseEnum<CatalogStatus>(request.Status, out var status)
            || !IsValidDateRange(request.ActiveFrom, request.ActiveTo))
            return Result.Failure<SponsorResponse>(HrErrors.InvalidRequest);

        Sponsor sponsor;
        if (sponsorId is null)
        {
            sponsor = new Sponsor { CompanyProfileId = Domain.Entities.Platform.CompanyProfile.FixedId };
            dbContext.Sponsors.Add(sponsor);
        }
        else
        {
            sponsor = await dbContext.Sponsors.SingleOrDefaultAsync(item => item.Id == sponsorId, cancellationToken) ?? null!;
            if (sponsor is null) return Result.Failure<SponsorResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(sponsor.RowVersion, request.RowVersion))
                return Result.Failure<SponsorResponse>(HrErrors.ConcurrencyConflict);
        }

        var employerId = HrServiceSupport.NormalizeIdentifier(request.EmployerIdentityNumber);
        if (await dbContext.Sponsors.AnyAsync(item => item.Id != sponsor.Id && item.EmployerIdentityNumber == employerId, cancellationToken))
            return Result.Failure<SponsorResponse>(HrErrors.Duplicate);

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
        if (sponsor is null) return Result.Failure(HrErrors.NotFound);
        if (!HrServiceSupport.HasText(request.Reason) || !HrServiceSupport.MatchesRowVersion(sponsor.RowVersion, request.RowVersion))
            return Result.Failure(HrErrors.ConcurrencyConflict);
        if (await dbContext.Employees.AnyAsync(item => item.SponsorId == sponsorId, cancellationToken))
            return Result.Failure(HrErrors.Conflict);
        sponsor.IsDeleted = true;
        sponsor.Status = CatalogStatus.Archived;
        sponsor.DeletionReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result<ValidatedEmployeeRequest>> ValidateRequestAsync(EmployeeUpsertRequest request, Guid? employeeId, CancellationToken cancellationToken)
    {
        if (!HrServiceSupport.HasText(request.FullNameAr)
            || !TryParseEnum<EmployeeRelationshipType>(request.EngagementType, out var engagement)
            || !TryParseEnum<EmployeeStatus>(request.Status, out var status)
            || !TryParseOptionalEnum<Gender>(request.Gender, out var gender)
            || !TryParseOptionalEnum<MaritalStatus>(request.MaritalStatus, out var maritalStatus)
            || request.EngagementType.Equals(nameof(EmployeeRelationshipType.OutsideRider), StringComparison.OrdinalIgnoreCase) && request.IsEmployee
            || !request.IsEmployee && request.Rider is null && employeeId is null
            || status == EmployeeStatus.Active && !IsValidIqama(request.IqamaNo)
            || status == EmployeeStatus.Active && engagement == EmployeeRelationshipType.SponsoredInternal && request.SponsorId is null
            || !IsValidDateRange(request.ContractStartDate, request.ContractEndDate))
            return Result.Failure<ValidatedEmployeeRequest>(HrErrors.InvalidRequest);

        var iqama = HrServiceSupport.TrimOrNull(request.IqamaNo);
        if (iqama is not null && (!IsValidIqama(iqama)
            || await dbContext.Employees.AnyAsync(item => item.Id != employeeId && item.IqamaNo == iqama, cancellationToken)))
            return Result.Failure<ValidatedEmployeeRequest>(HrErrors.Duplicate);

        if (request.OperationalWorkTypeId is { } workTypeId && !await dbContext.OperationalWorkTypes.AnyAsync(item => item.Id == workTypeId, cancellationToken)
            || request.OperatingCityId is { } cityId && !await dbContext.OperatingCities.AnyAsync(item => item.Id == cityId, cancellationToken)
            || request.SponsorId is { } sponsorId && !await dbContext.Sponsors.AnyAsync(item => item.Id == sponsorId && item.Status == CatalogStatus.Active, cancellationToken)
            || request.ProfilePhotoDocumentId is { } documentId && (employeeId is null
                || !await dbContext.EmployeeDocuments.AnyAsync(item => item.Id == documentId && item.EmployeeId == employeeId, cancellationToken)))
            return Result.Failure<ValidatedEmployeeRequest>(HrErrors.NotFound);

        if (!request.IsEmployee && request.Rider is not null && !TryParseOptionalEnum<TShirtSize>(request.Rider.TShirtSize, out _))
            return Result.Failure<ValidatedEmployeeRequest>(HrErrors.InvalidRequest);

        return Result.Success(new ValidatedEmployeeRequest(gender, maritalStatus, engagement, status));
    }

    private static void Apply(Employee employee, EmployeeUpsertRequest request, Gender? gender, MaritalStatus? maritalStatus,
        EmployeeRelationshipType engagement, EmployeeStatus status)
    {
        employee.IqamaNo = HrServiceSupport.TrimOrNull(request.IqamaNo);
        employee.ResidencyProfession = HrServiceSupport.TrimOrNull(request.ResidencyProfession);
        employee.WorkingForMeAs = HrServiceSupport.TrimOrNull(request.WorkingForMeAs);
        employee.FullNameAr = request.FullNameAr.Trim();
        employee.FullNameEn = HrServiceSupport.TrimOrNull(request.FullNameEn);
        employee.Nationality = HrServiceSupport.TrimOrNull(request.Nationality);
        employee.BirthDate = request.BirthDate;
        employee.Gender = gender;
        employee.PrimaryPhone = HrServiceSupport.TrimOrNull(request.PrimaryPhone);
        employee.SecondaryPhone = HrServiceSupport.TrimOrNull(request.SecondaryPhone);
        employee.Email = HrServiceSupport.TrimOrNull(request.Email);
        employee.ProfilePhotoDocumentId = request.ProfilePhotoDocumentId;
        employee.MaritalStatus = maritalStatus;
        employee.EmergencyContactName = HrServiceSupport.TrimOrNull(request.EmergencyContactName);
        employee.EmergencyContactRelationship = HrServiceSupport.TrimOrNull(request.EmergencyContactRelationship);
        employee.EmergencyContactPhone = HrServiceSupport.TrimOrNull(request.EmergencyContactPhone);
        employee.IsEmployee = request.IsEmployee;
        employee.EngagementType = engagement;
        employee.Status = status;
        employee.StatusReason = HrServiceSupport.TrimOrNull(request.StatusReason);
        employee.HireDate = request.HireDate;
        employee.OperationalWorkTypeId = request.OperationalWorkTypeId;
        employee.OperatingCityId = request.OperatingCityId;
        employee.SponsorId = request.SponsorId;
        employee.ContractStartDate = request.ContractStartDate;
        employee.ContractEndDate = request.ContractEndDate;
        employee.ProbationEndDate = request.ProbationEndDate;
        employee.TerminationDate = request.TerminationDate;
        employee.AlternateContactName = HrServiceSupport.TrimOrNull(request.AlternateContactName);
        employee.AlternateContactPhone = HrServiceSupport.TrimOrNull(request.AlternateContactPhone);
        employee.Notes = HrServiceSupport.TrimOrNull(request.Notes);
    }

    private static void ApplyRider(RiderProfile rider, RiderProfileUpsertRequest request)
    {
        if (!TryParseOptionalEnum<TShirtSize>(request.TShirtSize, out var size))
            throw new InvalidOperationException("The rider T-shirt size was not validated.");
        rider.TShirtSize = size;
        rider.OperationalNotes = HrServiceSupport.TrimOrNull(request.OperationalNotes);
    }

    private void TrackChanges(Employee employee, EmployeeUpsertRequest request, ValidatedEmployeeRequest validation,
        DateOnly effectiveDate, string reason, Guid actor)
    {
        Track(employee.Id, EmployeeWorkChangeType.Role, employee.IsEmployee ? "Administrative" : "Rider", request.IsEmployee ? "Administrative" : "Rider", effectiveDate, reason, actor);
        Track(employee.Id, EmployeeWorkChangeType.Status, employee.Status.ToString(), validation.Status.ToString(), effectiveDate, reason, actor);
        Track(employee.Id, EmployeeWorkChangeType.Engagement, employee.EngagementType.ToString(), validation.EngagementType.ToString(), effectiveDate, reason, actor);
        Track(employee.Id, EmployeeWorkChangeType.Profession, employee.WorkingForMeAs, HrServiceSupport.TrimOrNull(request.WorkingForMeAs), effectiveDate, reason, actor);
        Track(employee.Id, EmployeeWorkChangeType.Sponsor, employee.SponsorId?.ToString(), request.SponsorId?.ToString(), effectiveDate, reason, actor);
        Track(employee.Id, EmployeeWorkChangeType.OperationalWorkType, employee.OperationalWorkTypeId?.ToString(), request.OperationalWorkTypeId?.ToString(), effectiveDate, reason, actor);
        Track(employee.Id, EmployeeWorkChangeType.OperatingCity, employee.OperatingCityId?.ToString(), request.OperatingCityId?.ToString(), effectiveDate, reason, actor);
    }

    private void Track(Guid employeeId, EmployeeWorkChangeType type, string? oldValue, string? newValue,
        DateOnly effectiveDate, string reason, Guid actor)
    {
        if (!string.Equals(oldValue, newValue, StringComparison.Ordinal)) AddHistory(employeeId, type, oldValue, newValue, effectiveDate, reason, actor);
    }

    private void AddHistory(Guid employeeId, EmployeeWorkChangeType type, string? oldValue, string? newValue,
        DateOnly effectiveDate, string reason, Guid actor) => dbContext.EmployeeWorkHistory.Add(new EmployeeWorkHistory
    {
        EmployeeId = employeeId,
        ChangeType = type,
        OldValue = oldValue,
        NewValue = newValue,
        EffectiveDate = effectiveDate,
        Reason = reason,
        ChangedByUserId = actor,
        CreatedAtUtc = DateTimeOffset.UtcNow,
        CreatedByUserId = actor
    });

    private async Task<EmployeeDetailsResponse> BuildEmployeeDetailsAsync(Employee employee, CancellationToken cancellationToken)
    {
        var rider = await dbContext.RiderProfiles.AsNoTracking().SingleOrDefaultAsync(item => item.EmployeeId == employee.Id, cancellationToken);
        return new EmployeeDetailsResponse(
            ToEmployee(employee),
            rider is null ? null : ToRider(rider, employee),
            await BuildHistoryAsync(employee.Id, cancellationToken),
            await BuildCurrentHousingAsync(employee.Id, cancellationToken));
    }

    private async Task<HousingResponse?> BuildCurrentHousingAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var row = await (from residence in dbContext.HousingResidencePeriods.AsNoTracking()
                         join housing in dbContext.Housing.AsNoTracking() on residence.HousingId equals housing.Id
                         join city in dbContext.GlobalCities.AsNoTracking() on housing.CityId equals city.Id
                         where residence.EmployeeId == employeeId && residence.EffectiveTo == null
                         let currentResidents = dbContext.HousingResidencePeriods.Count(item => item.HousingId == housing.Id && item.EffectiveTo == null)
                         select new EmployeeHousingProjection(housing, city.NameAr, currentResidents))
            .SingleOrDefaultAsync(cancellationToken);

        return row is null ? null : ToHousing(row);
    }

    private async Task<EmployeeWorkHistoryResponse[]> BuildHistoryAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await dbContext.EmployeeWorkHistory.AsNoTracking().Where(item => item.EmployeeId == employeeId)
            .OrderByDescending(item => item.EffectiveDate).ThenByDescending(item => item.CreatedAtUtc)
            .Select(item => new EmployeeWorkHistoryResponse(item.Id, item.ChangeType.ToString(), item.OldValue, item.NewValue,
                item.EffectiveDate, item.Reason, item.ChangedByUserId, item.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

    private static EmployeeResponse ToEmployee(Employee item) => new(
        item.Id, item.IqamaNo, item.ResidencyProfession, item.WorkingForMeAs, item.FullNameAr, item.FullNameEn,
        item.Nationality, item.BirthDate, item.Gender?.ToString(), item.PrimaryPhone, item.SecondaryPhone, item.Email,
        item.ProfilePhotoDocumentId, item.MaritalStatus?.ToString(), item.EmergencyContactName,
        item.EmergencyContactRelationship, item.EmergencyContactPhone, item.IsEmployee, item.EngagementType.ToString(),
        item.Status.ToString(), item.StatusReason, item.HireDate, item.OperationalWorkTypeId, item.OperatingCityId,
        item.SponsorId, item.ContractStartDate, item.ContractEndDate, item.ProbationEndDate, item.TerminationDate,
        item.AlternateContactName, item.AlternateContactPhone, item.Notes, HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private static RiderDetailsResponse ToRider(RiderProfile rider, Employee employee) => new(
        rider.Id, employee.Id, employee.IqamaNo, employee.FullNameAr, employee.FullNameEn,
        employee.EngagementType.ToString(), employee.Status.ToString(), rider.TShirtSize?.ToString(),
        rider.OperationalNotes, HrServiceSupport.EncodeRowVersion(rider.RowVersion));

    private static HousingResponse ToHousing(EmployeeHousingProjection row) => new(
        row.Housing.Id, row.Housing.Code, row.Housing.NameAr, row.Housing.NameEn, row.Housing.CityId, row.CityNameAr,
        HrServiceSupport.ToAddressResponse(row.Housing.Address), row.Housing.Latitude, row.Housing.Longitude,
        row.Housing.TotalCapacity, row.CurrentResidents, Math.Max(0, row.Housing.TotalCapacity - row.CurrentResidents),
        row.Housing.ContactPhone, row.Housing.OpenedDate, row.Housing.ClosedDate, row.Housing.Status.ToString(),
        row.Housing.StatusReason, row.Housing.Notes, HrServiceSupport.EncodeRowVersion(row.Housing.RowVersion));

    private static SponsorResponse ToSponsor(Sponsor item) => new(
        item.Id, item.CompanyProfileId, item.EmployerIdentityNumber, item.RegistryNameAr, item.RegistryNameEn,
        item.CommercialRegistrationNumber, item.UnifiedNationalNumber, item.SponsorType.ToString(), item.Status.ToString(),
        item.ActiveFrom, item.ActiveTo, item.ContactName, item.ContactPhone, item.ContactEmail,
        HrServiceSupport.ToAddressResponse(item.Address), item.Notes, HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private Guid ActorId => currentUser.UserId ?? SystemActorId;

    private static bool TryParseEnum<TEnum>(string? value, out TEnum parsed) where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed);

    private static bool TryParseOptionalEnum<TEnum>(string? value, out TEnum? parsed) where TEnum : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!TryParseEnum<TEnum>(value, out var result)) return false;
        parsed = result;
        return true;
    }

    private static bool IsValidIqama(string? value) => value is { Length: 10 } && value.All(char.IsAsciiDigit);
    private static bool IsValidDateRange(DateOnly? start, DateOnly? end) => end is null || start is null || end >= start;

    private sealed record ValidatedEmployeeRequest(Gender? Gender, MaritalStatus? MaritalStatus,
        EmployeeRelationshipType EngagementType, EmployeeStatus Status);

    private sealed record EmployeeHousingProjection(Domain.Entities.Housing.Housing Housing, string CityNameAr, int CurrentResidents);
    private sealed record EmployeeHousingNameProjection(Guid EmployeeId, string HousingNameAr);
}
