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
        return Result.Success<IReadOnlyList<HousingResponse>>(rows.Select(row => ToHousing(row)).ToArray());
    }

    public async Task<Result<HousingResponse>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await BuildHousingQuery(id).SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return Result.Failure<HousingResponse>(HrErrors.HousingNotFound);
        }

        var rooms = await BuildRoomsAsync(id, cancellationToken);
        return Result.Success(ToHousing(row, rooms));
    }

    public async Task<Result<HousingResponse>> UpsertAsync(
        Guid? id,
        HousingUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Code))
            return Result.Failure<HousingResponse>(HrErrors.Required("code"));
        if (!HrServiceSupport.HasText(request.NameAr))
            return Result.Failure<HousingResponse>(HrErrors.Required("nameAr"));
        if (!HrServiceSupport.HasText(request.NameEn))
            return Result.Failure<HousingResponse>(HrErrors.Required("nameEn"));
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
        if (await dbContext.HousingRooms.AnyAsync(item => item.HousingId == id && item.CurrentOccupancy > 0, cancellationToken))
            return Result.Failure(HrErrors.Conflict);

        housing.Status = HousingStatus.Archived;
        housing.IsDeleted = true;
        housing.DeletionReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<HousingRoomResponse>>> GetRoomsAsync(
        Guid housingId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Housing.AnyAsync(item => item.Id == housingId, cancellationToken))
            return Result.Failure<IReadOnlyList<HousingRoomResponse>>(HrErrors.HousingNotFound);

        return Result.Success<IReadOnlyList<HousingRoomResponse>>(await BuildRoomsAsync(housingId, cancellationToken));
    }

    public async Task<Result<HousingRoomResponse>> GetRoomAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await (from item in dbContext.HousingRooms.AsNoTracking()
                          join housing in dbContext.Housing.AsNoTracking() on item.HousingId equals housing.Id
                          where item.Id == roomId
                          select item).SingleOrDefaultAsync(cancellationToken);
        if (room is null)
            return Result.Failure<HousingRoomResponse>(HrErrors.RoomNotFound);

        var occupants = await BuildCurrentOccupantsAsync([room.Id], cancellationToken);
        return Result.Success(ToRoom(room, occupants.GetValueOrDefault(room.Id, [])));
    }

    public async Task<Result<HousingRoomResponse>> UpsertRoomAsync(
        Guid? housingId,
        Guid? roomId,
        HousingRoomUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Name))
            return Result.Failure<HousingRoomResponse>(HrErrors.Required("name"));
        if (request.Name.Trim().Length > 100)
            return Result.Failure<HousingRoomResponse>(HrErrors.Invalid("name", "It cannot exceed 100 characters."));
        if (request.Capacity <= 0)
            return Result.Failure<HousingRoomResponse>(HrErrors.Invalid("capacity", "It must be greater than zero."));

        HousingRoom room;
        if (roomId is null)
        {
            if (housingId is null || !await dbContext.Housing.AnyAsync(item => item.Id == housingId, cancellationToken))
                return Result.Failure<HousingRoomResponse>(HrErrors.HousingNotFound);
            room = new HousingRoom { HousingId = housingId.Value };
            dbContext.HousingRooms.Add(room);
        }
        else
        {
            room = await dbContext.HousingRooms.SingleOrDefaultAsync(item => item.Id == roomId, cancellationToken) ?? null!;
            if (room is null || !await dbContext.Housing.AnyAsync(item => item.Id == room.HousingId, cancellationToken))
                return Result.Failure<HousingRoomResponse>(HrErrors.RoomNotFound);
            if (!HrServiceSupport.MatchesRowVersion(room.RowVersion, request.RowVersion))
                return Result.Failure<HousingRoomResponse>(HrErrors.ConcurrencyConflict);
            if (request.Capacity < room.CurrentOccupancy)
                return Result.Failure<HousingRoomResponse>(HrErrors.CapacityExceeded);
        }

        var name = request.Name.Trim();
        if (await dbContext.HousingRooms.AnyAsync(
                item => item.HousingId == room.HousingId && item.Id != room.Id && item.Name == name,
                cancellationToken))
            return Result.Failure<HousingRoomResponse>(HrErrors.RoomNameDuplicate);

        room.Name = name;
        room.Capacity = request.Capacity;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRoomAsync(room.Id, cancellationToken);
    }

    public async Task<Result> ArchiveRoomAsync(
        Guid roomId,
        ArchiveHousingRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await dbContext.HousingRooms.SingleOrDefaultAsync(item => item.Id == roomId, cancellationToken);
        if (room is null) return Result.Failure(HrErrors.RoomNotFound);
        if (!HrServiceSupport.HasText(request.Reason)) return Result.Failure(HrErrors.InvalidRequest);
        if (!HrServiceSupport.MatchesRowVersion(room.RowVersion, request.RowVersion))
            return Result.Failure(HrErrors.ConcurrencyConflict);
        if (room.CurrentOccupancy > 0) return Result.Failure(HrErrors.RoomOccupied);

        room.IsDeleted = true;
        room.DeletionReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<RoomOccupantResponse>> AssignEmployeeToRoomAsync(
        Guid roomId,
        AssignRoomEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Employees.AnyAsync(item => item.Id == request.EmployeeId && item.IsEmployee, cancellationToken))
            return Result.Failure<RoomOccupantResponse>(HrErrors.EmployeeNotFound);

        return await AssignPersonToRoomAsync(
            roomId,
            request.EmployeeId,
            request.EffectiveFrom,
            request.MoveInReason,
            request.SourceReference,
            cancellationToken);
    }

    public async Task<Result<RoomOccupantResponse>> AssignRiderToRoomAsync(
        Guid roomId,
        AssignRoomRiderRequest request,
        CancellationToken cancellationToken = default)
    {
        var employeeId = await dbContext.RiderProfiles.AsNoTracking()
            .Where(item => item.Id == request.RiderProfileId)
            .Select(item => (Guid?)item.EmployeeId)
            .SingleOrDefaultAsync(cancellationToken);
        if (employeeId is null)
            return Result.Failure<RoomOccupantResponse>(HrErrors.RiderNotFound);

        return await AssignPersonToRoomAsync(
            roomId,
            employeeId.Value,
            request.EffectiveFrom,
            request.MoveInReason,
            request.SourceReference,
            cancellationToken);
    }

    public async Task<Result<RoomOccupantResponse>> MoveOccupantAsync(
        Guid occupancyPeriodId,
        MoveRoomOccupantRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || request.EffectiveFrom == default)
            return Result.Failure<RoomOccupantResponse>(HrErrors.InvalidRequest);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var current = await dbContext.HousingResidencePeriods
                .SingleOrDefaultAsync(item => item.Id == occupancyPeriodId, cancellationToken);
            if (current is null)
                return Result.Failure<RoomOccupantResponse>(HrErrors.ResidencePeriodNotFound);
            if (current.EffectiveTo is not null || request.EffectiveFrom <= current.EffectiveFrom)
                return Result.Failure<RoomOccupantResponse>(HrErrors.Conflict);
            if (current.RoomId == request.DestinationRoomId)
                return Result.Failure<RoomOccupantResponse>(HrErrors.Conflict);

            var destination = await (from room in dbContext.HousingRooms.AsNoTracking()
                                     join housing in dbContext.Housing.AsNoTracking() on room.HousingId equals housing.Id
                                     where room.Id == request.DestinationRoomId
                                     select new { Room = room, HousingStatus = housing.Status })
                .SingleOrDefaultAsync(cancellationToken);
            if (destination is null)
                return Result.Failure<RoomOccupantResponse>(HrErrors.RoomNotFound);
            if (destination.HousingStatus != HousingStatus.Active)
                return Result.Failure<RoomOccupantResponse>(HrErrors.HousingNotActive);

            var sourceRoom = await dbContext.HousingRooms.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == current.RoomId, cancellationToken);
            if (sourceRoom is null)
                return Result.Failure<RoomOccupantResponse>(HrErrors.RoomNotFound);

            var incremented = await TryIncrementOccupancyAsync(destination.Room.Id, cancellationToken);
            if (!incremented)
                return Result.Failure<RoomOccupantResponse>(HrErrors.CapacityExceeded);

            current.EffectiveTo = request.EffectiveFrom.AddDays(-1);
            current.MoveOutReason = HrServiceSupport.TrimOrNull(request.Reason) ?? $"Moved to room {destination.Room.Name}.";
            current.DestinationReference = destination.Room.Name;
            var replacement = new HousingResidencePeriod
            {
                EmployeeId = current.EmployeeId,
                RoomId = destination.Room.Id,
                EffectiveFrom = request.EffectiveFrom,
                MoveInReason = HrServiceSupport.TrimOrNull(request.Reason),
                SourceReference = sourceRoom.Name,
                CapacityOverrideUsed = false,
                AssignedByUserId = userId
            };
            dbContext.HousingResidencePeriods.Add(replacement);
            await DecrementOccupancyAsync(sourceRoom.Id, cancellationToken);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
                return Result.Failure<RoomOccupantResponse>(HrErrors.Conflict);
            }

            return await GetOccupantAsync(replacement.Id, cancellationToken);
        });
    }

    public async Task<Result> RemoveOccupantAsync(
        Guid occupancyPeriodId,
        RemoveRoomOccupantRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Reason)) return Result.Failure(HrErrors.InvalidRequest);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var period = await dbContext.HousingResidencePeriods
                .SingleOrDefaultAsync(item => item.Id == occupancyPeriodId, cancellationToken);
            if (period is null) return Result.Failure(HrErrors.ResidencePeriodNotFound);
            if (period.EffectiveTo is not null || request.EffectiveTo < period.EffectiveFrom)
                return Result.Failure(HrErrors.Conflict);

            period.EffectiveTo = request.EffectiveTo;
            period.MoveOutReason = request.Reason.Trim();
            await DecrementOccupancyAsync(period.RoomId, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        });
    }

    public async Task<Result<IReadOnlyList<HousingPeriodResponse>>> GetResidentsAsync(
        Guid housingId,
        bool currentOnly,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Housing.AnyAsync(item => item.Id == housingId, cancellationToken))
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.HousingNotFound);

        return Result.Success<IReadOnlyList<HousingPeriodResponse>>(
            await BuildResidencePeriods(housingId, currentOnly, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<HousingPeriodResponse>>> AssignResidentAsync(
        Guid housingId,
        AssignHousingResidentRequest request,
        CancellationToken cancellationToken = default)
    {
        var roomHousingId = await dbContext.HousingRooms.AsNoTracking()
            .Where(item => item.Id == request.RoomId)
            .Select(item => (Guid?)item.HousingId)
            .SingleOrDefaultAsync(cancellationToken);
        if (roomHousingId is null)
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.RoomNotFound);
        if (roomHousingId != housingId)
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.RoomNotInHousing);
        if (!await dbContext.Employees.AnyAsync(item => item.Id == request.EmployeeId, cancellationToken))
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.EmployeeNotFound);

        var assignment = await AssignPersonToRoomAsync(
            request.RoomId,
            request.EmployeeId,
            request.EffectiveFrom,
            request.MoveInReason,
            request.SourceReference,
            cancellationToken);
        if (!assignment.IsSuccess)
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(assignment.Error);

        return Result.Success<IReadOnlyList<HousingPeriodResponse>>(
            await BuildResidencePeriods(housingId, false, cancellationToken));
    }

    public Task<Result> CloseResidenceAsync(
        Guid periodId,
        ClosePeriodRequest request,
        CancellationToken cancellationToken = default) =>
        RemoveOccupantAsync(periodId, new RemoveRoomOccupantRequest(request.EffectiveTo, request.Reason), cancellationToken);

    public async Task<Result<IReadOnlyList<HousingPeriodResponse>>> GetSupervisorsAsync(
        Guid housingId,
        bool currentOnly,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Housing.AnyAsync(item => item.Id == housingId, cancellationToken))
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.HousingNotFound);

        return Result.Success<IReadOnlyList<HousingPeriodResponse>>(
            await BuildSupervisorPeriods(housingId, currentOnly, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<HousingPeriodResponse>>> AssignSupervisorAsync(
        Guid housingId,
        AssignHousingSupervisorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.CurrentUserUnavailable);
        if (!await dbContext.Housing.AnyAsync(item => item.Id == housingId, cancellationToken))
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.HousingNotFound);
        if (!await dbContext.Employees.AnyAsync(item => item.Id == request.EmployeeId && item.IsEmployee, cancellationToken))
            return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.EmployeeNotFound);

        // One housing has one active supervisor. A single employee may supervise any number of housing locations.
        var current = await dbContext.HousingSupervisorPeriods
            .SingleOrDefaultAsync(item => item.HousingId == housingId && item.EffectiveTo == null, cancellationToken);
        if (current is not null)
        {
            if (request.EffectiveFrom <= current.EffectiveFrom)
                return Result.Failure<IReadOnlyList<HousingPeriodResponse>>(HrErrors.Conflict);
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
        return Result.Success<IReadOnlyList<HousingPeriodResponse>>(
            await BuildSupervisorPeriods(housingId, false, cancellationToken));
    }

    public async Task<Result> CloseSupervisorAsync(
        Guid periodId,
        ClosePeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Reason)) return Result.Failure(HrErrors.InvalidRequest);
        var period = await dbContext.HousingSupervisorPeriods.SingleOrDefaultAsync(item => item.Id == periodId, cancellationToken);
        if (period is null) return Result.Failure(HrErrors.SupervisorPeriodNotFound);
        if (period.EffectiveTo is not null || request.EffectiveTo < period.EffectiveFrom)
            return Result.Failure(HrErrors.Conflict);
        period.EffectiveTo = request.EffectiveTo;
        period.EndReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result<RoomOccupantResponse>> AssignPersonToRoomAsync(
        Guid roomId,
        Guid employeeId,
        DateOnly effectiveFrom,
        string? moveInReason,
        string? sourceReference,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || effectiveFrom == default)
            return Result.Failure<RoomOccupantResponse>(HrErrors.InvalidRequest);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            if (await dbContext.HousingResidencePeriods.AnyAsync(
                    item => item.EmployeeId == employeeId && item.EffectiveTo == null,
                    cancellationToken))
                return Result.Failure<RoomOccupantResponse>(HrErrors.PersonAlreadyAssigned);

            var room = await (from item in dbContext.HousingRooms.AsNoTracking()
                              join housing in dbContext.Housing.AsNoTracking() on item.HousingId equals housing.Id
                              where item.Id == roomId
                              select new { Room = item, HousingStatus = housing.Status })
                .SingleOrDefaultAsync(cancellationToken);
            if (room is null)
                return Result.Failure<RoomOccupantResponse>(HrErrors.RoomNotFound);
            if (room.HousingStatus != HousingStatus.Active)
                return Result.Failure<RoomOccupantResponse>(HrErrors.HousingNotActive);

            if (!await TryIncrementOccupancyAsync(roomId, cancellationToken))
                return Result.Failure<RoomOccupantResponse>(HrErrors.CapacityExceeded);

            var period = new HousingResidencePeriod
            {
                EmployeeId = employeeId,
                RoomId = roomId,
                EffectiveFrom = effectiveFrom,
                MoveInReason = HrServiceSupport.TrimOrNull(moveInReason),
                SourceReference = HrServiceSupport.TrimOrNull(sourceReference),
                CapacityOverrideUsed = false,
                AssignedByUserId = userId
            };
            dbContext.HousingResidencePeriods.Add(period);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
                return Result.Failure<RoomOccupantResponse>(HrErrors.PersonAlreadyAssigned);
            }

            return await GetOccupantAsync(period.Id, cancellationToken);
        });
    }

    private async Task<bool> TryIncrementOccupancyAsync(Guid roomId, CancellationToken cancellationToken) =>
        await dbContext.HousingRooms
            .Where(item => item.Id == roomId && item.CurrentOccupancy < item.Capacity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.CurrentOccupancy, item => item.CurrentOccupancy + 1),
                cancellationToken) == 1;

    private async Task DecrementOccupancyAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var changed = await dbContext.HousingRooms
            .Where(item => item.Id == roomId && item.CurrentOccupancy > 0)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.CurrentOccupancy, item => item.CurrentOccupancy - 1),
                cancellationToken);
        if (changed != 1)
            throw new DbUpdateException("Room occupancy could not be decremented because persisted occupancy is inconsistent.");
    }

    private async Task<Result<RoomOccupantResponse>> GetOccupantAsync(
        Guid periodId,
        CancellationToken cancellationToken)
    {
        var occupant = await BuildOccupantQuery(periodId)
            .SingleOrDefaultAsync(cancellationToken);
        return occupant is null
            ? Result.Failure<RoomOccupantResponse>(HrErrors.ResidencePeriodNotFound)
            : Result.Success(occupant);
    }

    private IQueryable<RoomOccupantResponse> BuildOccupantQuery(
        Guid? periodId = null,
        Guid[]? roomIds = null) =>
        from period in dbContext.HousingResidencePeriods.AsNoTracking()
        join room in dbContext.HousingRooms.AsNoTracking() on period.RoomId equals room.Id
        join employee in dbContext.Employees.AsNoTracking() on period.EmployeeId equals employee.Id
        join riderProfile in dbContext.RiderProfiles.AsNoTracking() on employee.Id equals riderProfile.EmployeeId into riderProfiles
        from rider in riderProfiles.DefaultIfEmpty()
        where period.EffectiveTo == null
              && (periodId == null || period.Id == periodId)
              && (roomIds == null || roomIds.Contains(room.Id))
        orderby employee.FullNameAr
        select new RoomOccupantResponse(
            period.Id,
            room.Id,
            room.HousingId,
            employee.Id,
            rider == null ? null : rider.Id,
            rider == null ? "Employee" : "Rider",
            employee.IqamaNo,
            employee.FullNameAr,
            employee.FullNameEn,
            period.EffectiveFrom,
            period.MoveInReason,
            period.SourceReference);

    private IQueryable<HousingProjection> BuildHousingQuery(Guid? housingId = null) =>
        from housing in dbContext.Housing.AsNoTracking()
        join city in dbContext.GlobalCities.AsNoTracking() on housing.CityId equals city.Id
        where housingId == null || housing.Id == housingId
        let totalCapacity = dbContext.HousingRooms
            .Where(room => room.HousingId == housing.Id)
            .Sum(room => (int?)room.Capacity) ?? 0
        let currentResidents = dbContext.HousingRooms
            .Where(room => room.HousingId == housing.Id)
            .Sum(room => (int?)room.CurrentOccupancy) ?? 0
        orderby housing.NameAr
        select new HousingProjection(housing, city.NameAr, totalCapacity, currentResidents);

    private async Task<HousingRoomResponse[]> BuildRoomsAsync(Guid housingId, CancellationToken cancellationToken)
    {
        var rooms = await dbContext.HousingRooms.AsNoTracking()
            .Where(item => item.HousingId == housingId)
            .OrderBy(item => item.Name)
            .ToArrayAsync(cancellationToken);
        var occupants = await BuildCurrentOccupantsAsync(rooms.Select(item => item.Id).ToArray(), cancellationToken);
        return rooms.Select(room => ToRoom(room, occupants.GetValueOrDefault(room.Id, []))).ToArray();
    }

    private async Task<Dictionary<Guid, RoomOccupantResponse[]>> BuildCurrentOccupantsAsync(
        Guid[] roomIds,
        CancellationToken cancellationToken)
    {
        if (roomIds.Length == 0) return [];
        return (await BuildOccupantQuery(roomIds: roomIds).ToArrayAsync(cancellationToken))
            .GroupBy(item => item.RoomId)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }

    private async Task<HousingPeriodResponse[]> BuildResidencePeriods(
        Guid housingId,
        bool currentOnly,
        CancellationToken cancellationToken) =>
        await (from period in dbContext.HousingResidencePeriods.AsNoTracking()
               join room in dbContext.HousingRooms.IgnoreQueryFilters().AsNoTracking() on period.RoomId equals room.Id
               join employee in dbContext.Employees.AsNoTracking() on period.EmployeeId equals employee.Id
               join riderProfile in dbContext.RiderProfiles.AsNoTracking() on employee.Id equals riderProfile.EmployeeId into riderProfiles
               from rider in riderProfiles.DefaultIfEmpty()
               where room.HousingId == housingId && (!currentOnly || period.EffectiveTo == null)
               orderby period.EffectiveFrom descending
               select new HousingPeriodResponse(
                   period.Id,
                   room.HousingId,
                   room.Id,
                   room.Name,
                   employee.Id,
                   rider == null ? null : rider.Id,
                   rider == null ? "Employee" : "Rider",
                   employee.IqamaNo,
                   employee.FullNameAr,
                   period.EffectiveFrom,
                   period.EffectiveTo,
                   period.MoveInReason,
                   period.MoveOutReason,
                   period.CapacityOverrideUsed,
                   period.CapacityOverrideReason)).ToArrayAsync(cancellationToken);

    private async Task<HousingPeriodResponse[]> BuildSupervisorPeriods(
        Guid housingId,
        bool currentOnly,
        CancellationToken cancellationToken) =>
        await (from period in dbContext.HousingSupervisorPeriods.AsNoTracking()
               join employee in dbContext.Employees.AsNoTracking() on period.SupervisorEmployeeId equals employee.Id
               where period.HousingId == housingId && (!currentOnly || period.EffectiveTo == null)
               orderby period.EffectiveFrom descending
               select new HousingPeriodResponse(
                   period.Id,
                   period.HousingId,
                   null,
                   null,
                   employee.Id,
                   null,
                   "Employee",
                   employee.IqamaNo,
                   employee.FullNameAr,
                   period.EffectiveFrom,
                   period.EffectiveTo,
                   period.AssignmentReason,
                   period.EndReason,
                   false,
                   null)).ToArrayAsync(cancellationToken);

    private static HousingResponse ToHousing(
        HousingProjection row,
        IReadOnlyList<HousingRoomResponse>? rooms = null) =>
        new(
            row.Item.Id,
            row.Item.Code,
            row.Item.NameAr,
            row.Item.NameEn,
            row.Item.CityId,
            row.CityNameAr,
            HrServiceSupport.ToAddressResponse(row.Item.Address),
            row.Item.Latitude,
            row.Item.Longitude,
            row.TotalCapacity,
            row.CurrentResidents,
            Math.Max(0, row.TotalCapacity - row.CurrentResidents),
            row.Item.ContactPhone,
            row.Item.OpenedDate,
            row.Item.ClosedDate,
            row.Item.Status.ToString(),
            row.Item.StatusReason,
            row.Item.Notes,
            HrServiceSupport.EncodeRowVersion(row.Item.RowVersion),
            rooms);

    private static HousingRoomResponse ToRoom(HousingRoom room, IReadOnlyList<RoomOccupantResponse> occupants) =>
        new(
            room.Id,
            room.HousingId,
            room.Name,
            room.Capacity,
            room.CurrentOccupancy,
            Math.Max(0, room.Capacity - room.CurrentOccupancy),
            HrServiceSupport.EncodeRowVersion(room.RowVersion),
            occupants);

    private static bool TryParseEnum<TEnum>(string value, out TEnum parsed) where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed);

    private sealed record HousingProjection(Housing Item, string CityNameAr, int TotalCapacity, int CurrentResidents);
}
