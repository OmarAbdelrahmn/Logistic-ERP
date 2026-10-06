using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed partial class HousingService
{
    public async Task<Result<RoomOccupantResponse>> AssignByIqamaToRoomAsync(Guid roomId, AssignRoomByIqamaRequest request, CancellationToken cancellationToken = default)
    {
        var iqama = request.IqamaNo?.Trim();
        if (string.IsNullOrWhiteSpace(iqama))
            return Result.Failure<RoomOccupantResponse>(HrErrors.InvalidRequest);
        var employees = await dbContext.Employees.AsNoTracking()
            .Where(item => item.IqamaNo == iqama)
            .Select(item => item.Id).Take(2).ToArrayAsync(cancellationToken);
        if (employees.Length == 0) return Result.Failure<RoomOccupantResponse>(HrErrors.EmployeeNotFound);
        if (employees.Length != 1) return Result.Failure<RoomOccupantResponse>(HrErrors.Conflict);
        var pendingId = await dbContext.HousingPendingOccupants.AsNoTracking()
            .Where(item => item.RoomId == roomId && item.IqamaNo == iqama)
            .Select(item => (Guid?)item.Id).SingleOrDefaultAsync(cancellationToken);
        if (pendingId is not null)
            return await ResolvePendingOccupantAsync(pendingId.Value, request.EffectiveFrom, cancellationToken);
        return await AssignPersonToRoomAsync(roomId, employees[0], request.EffectiveFrom,
            request.MoveInReason, request.SourceReference, cancellationToken);
    }

    private async Task<Dictionary<Guid, HousingPendingOccupantResponse[]>> BuildPendingOccupantsAsync(Guid[] roomIds, CancellationToken cancellationToken)
    {
        if (roomIds.Length == 0) return [];
        var items = await dbContext.HousingPendingOccupants.AsNoTracking()
            .Where(item => roomIds.Contains(item.RoomId)).OrderBy(item => item.Name).ToArrayAsync(cancellationToken);
        return items.GroupBy(item => item.RoomId)
            .ToDictionary(group => group.Key, group => group.Select(item =>
                new HousingPendingOccupantResponse(item.Id, item.RoomId, item.IqamaNo, item.Name,
                    item.SourceRow, HrServiceSupport.EncodeRowVersion(item.RowVersion))).ToArray());
    }

    public async Task<Result<RoomOccupantResponse>> ResolvePendingOccupantAsync(Guid pendingId, DateOnly effectiveFrom, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || effectiveFrom == default)
            return Result.Failure<RoomOccupantResponse>(HrErrors.InvalidRequest);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var pending = await dbContext.HousingPendingOccupants.SingleOrDefaultAsync(item => item.Id == pendingId, cancellationToken);
        if (pending is null) return Result.Failure<RoomOccupantResponse>(HrErrors.InvalidRequest);
        var employeeIds = await dbContext.Employees.AsNoTracking()
            .Where(item => item.IqamaNo == pending.IqamaNo)
            .Select(item => item.Id).Take(2).ToArrayAsync(cancellationToken);
        if (employeeIds.Length == 0) return Result.Failure<RoomOccupantResponse>(HrErrors.EmployeeNotFound);
        if (employeeIds.Length != 1 || await dbContext.HousingResidencePeriods.AnyAsync(
                item => item.EmployeeId == employeeIds[0] && item.EffectiveTo == null, cancellationToken))
            return Result.Failure<RoomOccupantResponse>(HrErrors.Conflict);
        var period = new HousingResidencePeriod
        {
            EmployeeId = employeeIds[0], RoomId = pending.RoomId, EffectiveFrom = effectiveFrom,
            SourceReference = $"Riyadh workbook row {pending.SourceRow}", AssignedByUserId = userId
        };
        dbContext.HousingResidencePeriods.Add(period);
        pending.IsDeleted = true;
        pending.DeletionReason = "Resolved to employee";
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<RoomOccupantResponse>(HrErrors.Conflict);
        }
        return await GetOccupantAsync(period.Id, cancellationToken);
    }

    public async Task<Result> DeletePendingOccupantAsync(Guid pendingId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var pending = await dbContext.HousingPendingOccupants.SingleOrDefaultAsync(item => item.Id == pendingId, cancellationToken);
        if (pending is null) return Result.Failure(HrErrors.InvalidRequest);
        pending.IsDeleted = true;
        pending.DeletionReason = "Removed";
        await DecrementOccupancyAsync(pending.RoomId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<HousingFloorResponse>>> GetFloorsAsync(Guid housingId, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Housing.AnyAsync(item => item.Id == housingId, cancellationToken))
            return Result.Failure<IReadOnlyList<HousingFloorResponse>>(HrErrors.HousingNotFound);
        var rooms = await BuildRoomsAsync(housingId, cancellationToken);
        return Result.Success<IReadOnlyList<HousingFloorResponse>>(await BuildFloorsAsync(housingId, rooms, cancellationToken));
    }

    private async Task<HousingFloorResponse[]> BuildFloorsAsync(Guid housingId, IReadOnlyList<HousingRoomResponse> rooms, CancellationToken cancellationToken)
    {
        var floors = await dbContext.HousingFloors.AsNoTracking().Where(item => item.HousingId == housingId)
            .OrderBy(item => item.Name).ToArrayAsync(cancellationToken);
        var ids = floors.Select(item => item.Id).ToArray();
        var equipment = await dbContext.HousingEquipment.AsNoTracking()
            .Where(item => item.FloorId != null && ids.Contains(item.FloorId.Value))
            .ToArrayAsync(cancellationToken);
        return floors.Select(floor =>
        {
            var floorRooms = rooms.Where(item => item.FloorId == floor.Id).ToArray();
            var own = equipment.Where(item => item.FloorId == floor.Id).Select(ToEquipment).ToArray();
            var total = own.Concat(floorRooms.SelectMany(item => item.Equipment))
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => new HousingEquipmentResponse(Guid.Empty, group.First().Name, group.Sum(item => item.Quantity), string.Empty))
                .OrderBy(item => item.Name).ToArray();
            return new HousingFloorResponse(floor.Id, floor.HousingId, floor.Name,
                HrServiceSupport.EncodeRowVersion(floor.RowVersion), own, total, floorRooms,
                floorRooms.Sum(item => item.Capacity), floorRooms.Sum(item => item.CurrentOccupancy),
                floorRooms.Sum(item => item.AvailableCapacity));
        }).ToArray();
    }

    public async Task<Result<HousingFloorResponse>> UpsertFloorAsync(Guid housingId, Guid? floorId, HousingFloorUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Name) || request.Name.Trim().Length > 100)
            return Result.Failure<HousingFloorResponse>(HrErrors.InvalidRequest);
        if (!await dbContext.Housing.AnyAsync(item => item.Id == housingId, cancellationToken))
            return Result.Failure<HousingFloorResponse>(HrErrors.HousingNotFound);
        var floor = floorId is null ? new HousingFloor { HousingId = housingId } :
            await dbContext.HousingFloors.SingleOrDefaultAsync(item => item.Id == floorId && item.HousingId == housingId, cancellationToken);
        if (floor is null) return Result.Failure<HousingFloorResponse>(HrErrors.InvalidRequest);
        if (floorId is not null && !HrServiceSupport.MatchesRowVersion(floor.RowVersion, request.RowVersion))
            return Result.Failure<HousingFloorResponse>(HrErrors.ConcurrencyConflict);
        var name = request.Name.Trim();
        if (await dbContext.HousingFloors.AnyAsync(item => item.HousingId == housingId && item.Id != floor.Id && item.Name == name, cancellationToken))
            return Result.Failure<HousingFloorResponse>(HrErrors.Duplicate);
        floor.Name = name;
        if (floorId is null) dbContext.HousingFloors.Add(floor);
        await dbContext.SaveChangesAsync(cancellationToken);
        var floors = await GetFloorsAsync(housingId, cancellationToken);
        return Result.Success(floors.Value!.Single(item => item.Id == floor.Id));
    }

    public async Task<Result> DeleteFloorAsync(Guid floorId, ArchiveRequest request, CancellationToken cancellationToken = default)
    {
        var floor = await dbContext.HousingFloors.SingleOrDefaultAsync(item => item.Id == floorId, cancellationToken);
        if (floor is null) return Result.Failure(HrErrors.InvalidRequest);
        if (!HrServiceSupport.HasText(request.Reason) || !HrServiceSupport.MatchesRowVersion(floor.RowVersion, request.RowVersion))
            return Result.Failure(HrErrors.ConcurrencyConflict);
        if (await dbContext.HousingRooms.AnyAsync(item => item.FloorId == floorId, cancellationToken) ||
            await dbContext.HousingEquipment.AnyAsync(item => item.FloorId == floorId, cancellationToken))
            return Result.Failure(HrErrors.Conflict);
        floor.IsDeleted = true;
        floor.DeletionReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Dictionary<Guid, HousingEquipmentResponse[]>> BuildRoomEquipmentAsync(Guid[] roomIds, CancellationToken cancellationToken)
    {
        if (roomIds.Length == 0) return [];
        var items = await dbContext.HousingEquipment.AsNoTracking()
            .Where(item => item.RoomId != null && roomIds.Contains(item.RoomId.Value))
            .OrderBy(item => item.Name).ToArrayAsync(cancellationToken);
        return items.GroupBy(item => item.RoomId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(ToEquipment).ToArray());
    }

    private async Task<Dictionary<Guid, HousingExternalOccupantResponse[]>> BuildExternalOccupantsAsync(Guid[] roomIds, CancellationToken cancellationToken)
    {
        if (roomIds.Length == 0) return [];
        var items = await dbContext.HousingExternalOccupants.AsNoTracking()
            .Where(item => roomIds.Contains(item.RoomId)).OrderBy(item => item.Name).ToArrayAsync(cancellationToken);
        return items.GroupBy(item => item.RoomId)
            .ToDictionary(group => group.Key, group => group.Select(ToExternal).ToArray());
    }

    public async Task<Result<HousingEquipmentResponse>> UpsertEquipmentAsync(Guid? floorId, Guid? roomId, Guid? equipmentId, HousingEquipmentUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Name) || request.Name.Trim().Length > 100 || request.Quantity < 0 ||
            (floorId is null) == (roomId is null))
            return Result.Failure<HousingEquipmentResponse>(HrErrors.InvalidRequest);
        if (floorId is { } f && !await dbContext.HousingFloors.AnyAsync(item => item.Id == f, cancellationToken))
            return Result.Failure<HousingEquipmentResponse>(HrErrors.InvalidRequest);
        if (roomId is { } r && !await dbContext.HousingRooms.AnyAsync(item => item.Id == r, cancellationToken))
            return Result.Failure<HousingEquipmentResponse>(HrErrors.RoomNotFound);
        var item = equipmentId is null ? new HousingEquipment { FloorId = floorId, RoomId = roomId } :
            await dbContext.HousingEquipment.SingleOrDefaultAsync(x => x.Id == equipmentId && x.FloorId == floorId && x.RoomId == roomId, cancellationToken);
        if (item is null) return Result.Failure<HousingEquipmentResponse>(HrErrors.InvalidRequest);
        if (equipmentId is not null && !HrServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion))
            return Result.Failure<HousingEquipmentResponse>(HrErrors.ConcurrencyConflict);
        var name = request.Name.Trim();
        if (await dbContext.HousingEquipment.AnyAsync(x => x.Id != item.Id && x.FloorId == floorId && x.RoomId == roomId && x.Name == name, cancellationToken))
            return Result.Failure<HousingEquipmentResponse>(HrErrors.Duplicate);
        item.Name = name;
        item.Quantity = request.Quantity;
        if (equipmentId is null) dbContext.HousingEquipment.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToEquipment(item));
    }

    public async Task<Result> DeleteEquipmentAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.HousingEquipment.SingleOrDefaultAsync(x => x.Id == equipmentId, cancellationToken);
        if (item is null) return Result.Failure(HrErrors.InvalidRequest);
        item.IsDeleted = true;
        item.DeletionReason = "Removed";
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<HousingExternalOccupantResponse>> UpsertExternalOccupantAsync(Guid? occupantId, Guid roomId, HousingExternalOccupantUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Name) || request.Name.Trim().Length > 200)
            return Result.Failure<HousingExternalOccupantResponse>(HrErrors.InvalidRequest);
        var targetId = request.RoomId ?? roomId;
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var target = await (from room in dbContext.HousingRooms.AsNoTracking()
                                join housing in dbContext.Housing.AsNoTracking() on room.HousingId equals housing.Id
                                where room.Id == targetId
                                select new { room.Id, housing.Status }).SingleOrDefaultAsync(cancellationToken);
            if (target is null) return Result.Failure<HousingExternalOccupantResponse>(HrErrors.RoomNotFound);
            if (target.Status != HousingStatus.Active) return Result.Failure<HousingExternalOccupantResponse>(HrErrors.HousingNotActive);
            var item = occupantId is null ? new HousingExternalOccupant { RoomId = targetId } :
                await dbContext.HousingExternalOccupants.SingleOrDefaultAsync(x => x.Id == occupantId, cancellationToken);
            if (item is null) return Result.Failure<HousingExternalOccupantResponse>(HrErrors.InvalidRequest);
            if (occupantId is not null && !HrServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion))
                return Result.Failure<HousingExternalOccupantResponse>(HrErrors.ConcurrencyConflict);
            if (occupantId is null || item.RoomId != targetId)
            {
                if (!await TryIncrementOccupancyAsync(targetId, cancellationToken))
                    return Result.Failure<HousingExternalOccupantResponse>(HrErrors.CapacityExceeded);
                if (occupantId is not null) await DecrementOccupancyAsync(item.RoomId, cancellationToken);
            }
            item.RoomId = targetId;
            item.Name = request.Name.Trim();
            if (occupantId is null) dbContext.HousingExternalOccupants.Add(item);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                dbContext.ChangeTracker.Clear();
                throw;
            }
            return Result.Success(ToExternal(item));
        });
    }

    public async Task<Result> DeleteExternalOccupantAsync(Guid occupantId, CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var item = await dbContext.HousingExternalOccupants.SingleOrDefaultAsync(x => x.Id == occupantId, cancellationToken);
            if (item is null) return Result.Failure(HrErrors.InvalidRequest);
            item.IsDeleted = true;
            item.DeletionReason = "Moved out";
            await DecrementOccupancyAsync(item.RoomId, cancellationToken);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                dbContext.ChangeTracker.Clear();
                throw;
            }
            return Result.Success();
        });
    }

    private static HousingEquipmentResponse ToEquipment(HousingEquipment item) =>
        new(item.Id, item.Name, item.Quantity, HrServiceSupport.EncodeRowVersion(item.RowVersion));
    private static HousingExternalOccupantResponse ToExternal(HousingExternalOccupant item) =>
        new(item.Id, item.RoomId, item.Name, HrServiceSupport.EncodeRowVersion(item.RowVersion));
}
