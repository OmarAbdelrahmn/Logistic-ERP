using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class HousingService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser) : IHousingService
{
    public async Task<Result<IReadOnlyList<HousingResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await BuildHousingQuery().ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<HousingResponse>>(rows.Select(ToHousing).ToArray());
    }

    public async Task<Result<HousingResponse>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await BuildHousingQuery(id).SingleOrDefaultAsync(cancellationToken);
        return row is null ? Result.Failure<HousingResponse>(HrErrors.HousingNotFound) : Result.Success(ToHousing(row));
    }

    public async Task<Result<HousingResponse>> UpsertAsync(Guid? id, HousingUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Code))
            return Result.Failure<HousingResponse>(HrErrors.Required("code"));
        if (!HrServiceSupport.HasText(request.NameAr))
            return Result.Failure<HousingResponse>(HrErrors.Required("nameAr"));
        if (!HrServiceSupport.HasText(request.NameEn))
            return Result.Failure<HousingResponse>(HrErrors.Required("nameEn"));
        if (request.TotalCapacity <= 0)
            return Result.Failure<HousingResponse>(HrErrors.Invalid("totalCapacity", "It must be greater than zero."));
        if (request.Latitude is < -90 or > 90)
            return Result.Failure<HousingResponse>(HrErrors.Invalid("latitude", "It must be between -90 and 90."));
        if (request.Longitude is < -180 or > 180)
            return Result.Failure<HousingResponse>(HrErrors.Invalid("longitude", "It must be between -180 and 180."));
        if (request.ClosedDate is not null && request.OpenedDate is not null && request.ClosedDate < request.OpenedDate)
            return Result.Failure<HousingResponse>(HrErrors.Invalid("closedDate", "It cannot be before 'openedDate'."));
        if (!TryParseEnum<HousingStatus>(request.Status, out var status))
            return Result.Failure<HousingResponse>(HrErrors.Invalid("status", "Use a valid housing status, such as 'Active', 'Inactive', or 'Archived'."));
        if (!await dbContext.GlobalCities.AnyAsync(item => item.Id == request.CityId, cancellationToken))
            return Result.Failure<HousingResponse>(HrErrors.CityNotFound);

        Housing entity;
        if (id is null)
        {
            entity = new Housing();
            dbContext.Housing.Add(entity);
        }
        else
        {
            entity = await dbContext.Housing.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null)
            {
                return Result.Failure<HousingResponse>(HrErrors.HousingNotFound);
            }
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            {
                return Result.Failure<HousingResponse>(HrErrors.ConcurrencyConflict);
            }
            var currentCount = await dbContext.HousingResidencePeriods.CountAsync(item => item.HousingId == id && item.EffectiveTo == null, cancellationToken);
            if (request.TotalCapacity < currentCount)
            {
                return Result.Failure<HousingResponse>(HrErrors.CapacityExceeded);
            }
        }
        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.Housing.AnyAsync(item => item.Id != entity.Id && item.Code == code, cancellationToken))
        {
            return Result.Failure<HousingResponse>(HrErrors.Duplicate);
        }
        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = request.NameEn.Trim();
        entity.CityId = request.CityId;
        entity.Address = HrServiceSupport.ToAddress(request.Address);
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.TotalCapacity = request.TotalCapacity;
        entity.ContactPhone = HrServiceSupport.TrimOrNull(request.ContactPhone);
        entity.OpenedDate = request.OpenedDate;
        entity.ClosedDate = request.ClosedDate;
        entity.Status = status;
        entity.StatusReason = HrServiceSupport.TrimOrNull(request.StatusReason);
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(entity.Id, cancellationToken);
    }

    public async Task<Result> ArchiveAsync(Guid id, ArchiveRequest request, CancellationToken cancellationToken = default)
    {
        var housing = await dbContext.Housing.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (housing is null) return Result.Failure(HrErrors.HousingNotFound);
        if (!HrServiceSupport.HasText(request.Reason) || !HrServiceSupport.MatchesRowVersion(housing.RowVersion, request.RowVersion))
            return Result.Failure(HrErrors.ConcurrencyConflict);
        if (await dbContext.HousingResidencePeriods.AnyAsync(item => item.HousingId == id && item.EffectiveTo == null, cancellationToken))
            return Result.Failure(HrErrors.Conflict);
        housing.Status = HousingStatus.Archived;
        housing.IsDeleted = true;
        housing.DeletionReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<HousingPeriodResponse>>> GetResidentsAsync(Guid housingId, bool currentOnly, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Housing.AnyAsync(item => item.Id == housingId, cancellationToken))
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.HousingNotFound);

        return Result.Success<IReadOnlyList<HousingPeriodResponse>>(await BuildResidencePeriods(housingId, currentOnly, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<HousingPeriodResponse>>> AssignResidentAsync(Guid housingId, AssignHousingResidentRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId
            || request.CapacityOverrideUsed && !HrServiceSupport.HasText(request.CapacityOverrideReason))
        {
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.InvalidRequest);
        }
        var housing = await dbContext.Housing.SingleOrDefaultAsync(item => item.Id == housingId, cancellationToken);
        if (housing is null)
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.HousingNotFound);
        if (housing.Status != HousingStatus.Active)
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.HousingNotActive);
        if (!await dbContext.Employees.AnyAsync(item => item.Id == request.EmployeeId, cancellationToken))
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.EmployeeNotFound);
        var currentResidents = await dbContext.HousingResidencePeriods.CountAsync(item => item.HousingId == housingId && item.EffectiveTo == null, cancellationToken);
        var existing = await dbContext.HousingResidencePeriods.SingleOrDefaultAsync(item => item.EmployeeId == request.EmployeeId && item.EffectiveTo == null, cancellationToken);
        if (currentResidents >= housing.TotalCapacity && existing?.HousingId != housingId && !request.CapacityOverrideUsed)
        {
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.CapacityExceeded);
        }
        if (existing is not null)
        {
            if (request.EffectiveFrom <= existing.EffectiveFrom)
            {
                return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.Conflict);
            }
            existing.EffectiveTo = request.EffectiveFrom.AddDays(-1);
            existing.MoveOutReason = $"Transferred to housing {housing.Code}.";
            existing.DestinationReference = housing.Code;
        }
        dbContext.HousingResidencePeriods.Add(new HousingResidencePeriod
        {
            HousingId = housingId,
            EmployeeId = request.EmployeeId,
            EffectiveFrom = request.EffectiveFrom,
            MoveInReason = HrServiceSupport.TrimOrNull(request.MoveInReason),
            SourceReference = HrServiceSupport.TrimOrNull(request.SourceReference),
            CapacityOverrideUsed = request.CapacityOverrideUsed,
            CapacityOverrideReason = HrServiceSupport.TrimOrNull(request.CapacityOverrideReason),
            AssignedByUserId = userId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<HousingPeriodResponse>>(await BuildResidencePeriods(housingId, false, cancellationToken));
    }

    public async Task<Result> CloseResidenceAsync(Guid periodId, ClosePeriodRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Reason)) return Result.Failure(HrErrors.InvalidRequest);
        var period = await dbContext.HousingResidencePeriods.SingleOrDefaultAsync(item => item.Id == periodId, cancellationToken);
        if (period is null) return Result.Failure(HrErrors.ResidencePeriodNotFound);
        if (period.EffectiveTo is not null || request.EffectiveTo < period.EffectiveFrom) return Result.Failure(HrErrors.Conflict);
        period.EffectiveTo = request.EffectiveTo;
        period.MoveOutReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<HousingPeriodResponse>>> GetSupervisorsAsync(Guid housingId, bool currentOnly, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Housing.AnyAsync(item => item.Id == housingId, cancellationToken))
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.HousingNotFound);

        return Result.Success<IReadOnlyList<HousingPeriodResponse>>(await BuildSupervisorPeriods(housingId, currentOnly, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<HousingPeriodResponse>>> AssignSupervisorAsync(Guid housingId, AssignHousingSupervisorRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.CurrentUserUnavailable);
        if (!await dbContext.Housing.AnyAsync(item => item.Id == housingId, cancellationToken))
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.HousingNotFound);
        if (!await dbContext.Employees.AnyAsync(item => item.Id == request.EmployeeId, cancellationToken))
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.EmployeeNotFound);
        var current = await dbContext.HousingSupervisorPeriods.SingleOrDefaultAsync(item => item.HousingId == housingId && item.EffectiveTo == null, cancellationToken);
        if (current is not null)
        {
            if (request.EffectiveFrom <= current.EffectiveFrom) return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.Conflict);
            current.EffectiveTo = request.EffectiveFrom.AddDays(-1);
            current.EndReason = "Replaced by a new supervisor assignment.";
        }
        dbContext.HousingSupervisorPeriods.Add(new HousingSupervisorPeriod
        {
            HousingId = housingId,
            SupervisorEmployeeId = request.EmployeeId,
            EffectiveFrom = request.EffectiveFrom,
            AssignmentReason = HrServiceSupport.TrimOrNull(request.AssignmentReason),
            AssignedByUserId = userId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<HousingPeriodResponse>>(await BuildSupervisorPeriods(housingId, false, cancellationToken));
    }

    public async Task<Result> CloseSupervisorAsync(Guid periodId, ClosePeriodRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Reason)) return Result.Failure(HrErrors.InvalidRequest);
        var period = await dbContext.HousingSupervisorPeriods.SingleOrDefaultAsync(item => item.Id == periodId, cancellationToken);
        if (period is null) return Result.Failure(HrErrors.SupervisorPeriodNotFound);
        if (period.EffectiveTo is not null || request.EffectiveTo < period.EffectiveFrom) return Result.Failure(HrErrors.Conflict);
        period.EffectiveTo = request.EffectiveTo;
        period.EndReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private IQueryable<HousingProjection> BuildHousingQuery(Guid? housingId = null) =>
        from housing in dbContext.Housing.AsNoTracking()
        join city in dbContext.GlobalCities.AsNoTracking() on housing.CityId equals city.Id
        where housingId == null || housing.Id == housingId
        let currentResidents = dbContext.HousingResidencePeriods.Count(item => item.HousingId == housing.Id && item.EffectiveTo == null)
        orderby housing.NameAr
        select new HousingProjection(housing, city.NameAr, currentResidents);

    private async Task<HousingPeriodResponse[]> BuildResidencePeriods(Guid housingId, bool currentOnly, CancellationToken cancellationToken) =>
        await (from period in dbContext.HousingResidencePeriods.AsNoTracking()
               join employee in dbContext.Employees.AsNoTracking() on period.EmployeeId equals employee.Id
               where period.HousingId == housingId && (!currentOnly || period.EffectiveTo == null)
               orderby period.EffectiveFrom descending
               select new HousingPeriodResponse(period.Id, period.HousingId, employee.Id, employee.IqamaNo,
                   employee.FullNameAr, period.EffectiveFrom, period.EffectiveTo, period.MoveInReason, period.MoveOutReason,
                   period.CapacityOverrideUsed, period.CapacityOverrideReason)).ToArrayAsync(cancellationToken);

    private async Task<HousingPeriodResponse[]> BuildSupervisorPeriods(Guid housingId, bool currentOnly, CancellationToken cancellationToken) =>
        await (from period in dbContext.HousingSupervisorPeriods.AsNoTracking()
               join employee in dbContext.Employees.AsNoTracking() on period.SupervisorEmployeeId equals employee.Id
               where period.HousingId == housingId && (!currentOnly || period.EffectiveTo == null)
               orderby period.EffectiveFrom descending
               select new HousingPeriodResponse(period.Id, period.HousingId, employee.Id, employee.IqamaNo,
                   employee.FullNameAr, period.EffectiveFrom, period.EffectiveTo, period.AssignmentReason, period.EndReason,
                   false, null)).ToArrayAsync(cancellationToken);

    private static HousingResponse ToHousing(HousingProjection row) => new(row.Item.Id, row.Item.Code, row.Item.NameAr,
        row.Item.NameEn, row.Item.CityId, row.CityNameAr, HrServiceSupport.ToAddressResponse(row.Item.Address),
        row.Item.Latitude, row.Item.Longitude, row.Item.TotalCapacity, row.CurrentResidents,
        Math.Max(0, row.Item.TotalCapacity - row.CurrentResidents), row.Item.ContactPhone, row.Item.OpenedDate,
        row.Item.ClosedDate, row.Item.Status.ToString(), row.Item.StatusReason, row.Item.Notes,
        HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));

    private static bool TryParseEnum<TEnum>(string value, out TEnum parsed) where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed);

    private sealed record HousingProjection(Housing Item, string CityNameAr, int CurrentResidents);
}
