using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed class FleetService(
    ApplicationDbContext dbContext,
    FleetServiceSupport support) : IFleetService
{
    public async Task<Result<IReadOnlyList<VehicleManufacturerResponse>>> GetManufacturersAsync(CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesRead, null, cancellationToken)) return Result.Failure<IReadOnlyList<VehicleManufacturerResponse>>(FleetErrors.Forbidden);
        var items = await dbContext.VehicleManufacturers.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.NameEn)
            .Select(x => new VehicleManufacturerResponse(x.Id, x.Code, x.NameAr, x.NameEn, x.Status, x.DisplayOrder, Convert.ToBase64String(x.RowVersion)))
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<VehicleManufacturerResponse>>(items);
    }

    public async Task<Result<VehicleManufacturerResponse>> UpsertManufacturerAsync(Guid? id, VehicleManufacturerRequest request, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesManage, null, cancellationToken)) return Result.Failure<VehicleManufacturerResponse>(FleetErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NameAr) || string.IsNullOrWhiteSpace(request.NameEn)) return Result.Failure<VehicleManufacturerResponse>(FleetErrors.InvalidRequest);
        var code = FleetServiceSupport.NormalizeIdentifier(request.Code);
        var item = id.HasValue ? await dbContext.VehicleManufacturers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) : null;
        if (id.HasValue && item is null) return Result.Failure<VehicleManufacturerResponse>(FleetErrors.NotFound);
        if (item is not null && !FleetServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion)) return Result.Failure<VehicleManufacturerResponse>(FleetErrors.ConcurrencyConflict);
        if (await dbContext.VehicleManufacturers.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken)) return Result.Failure<VehicleManufacturerResponse>(FleetErrors.Duplicate);
        item ??= new VehicleManufacturer();
        item.Code = code;
        item.NameAr = request.NameAr.Trim();
        item.NameEn = request.NameEn.Trim();
        item.Status = request.Status;
        item.DisplayOrder = request.DisplayOrder;
        if (!id.HasValue) dbContext.VehicleManufacturers.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(new VehicleManufacturerResponse(item.Id, item.Code, item.NameAr, item.NameEn, item.Status, item.DisplayOrder, FleetServiceSupport.EncodeRowVersion(item.RowVersion)));
    }

    public async Task<Result<IReadOnlyList<VehicleModelResponse>>> GetModelsAsync(Guid? manufacturerId, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesRead, null, cancellationToken)) return Result.Failure<IReadOnlyList<VehicleModelResponse>>(FleetErrors.Forbidden);
        var query = dbContext.VehicleModels.AsNoTracking();
        if (manufacturerId.HasValue) query = query.Where(x => x.VehicleManufacturerId == manufacturerId);
        var items = await query.OrderBy(x => x.NameEn).Select(x => new VehicleModelResponse(x.Id, x.VehicleManufacturerId, x.Code, x.NameAr, x.NameEn, x.VehicleType, x.DefaultFuelType, x.Status, Convert.ToBase64String(x.RowVersion))).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<VehicleModelResponse>>(items);
    }

    public async Task<Result<VehicleModelResponse>> UpsertModelAsync(Guid? id, VehicleModelRequest request, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesManage, null, cancellationToken)) return Result.Failure<VehicleModelResponse>(FleetErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NameAr) || string.IsNullOrWhiteSpace(request.NameEn)
            || !await dbContext.VehicleManufacturers.AnyAsync(x => x.Id == request.VehicleManufacturerId, cancellationToken)) return Result.Failure<VehicleModelResponse>(FleetErrors.InvalidRequest);
        var code = FleetServiceSupport.NormalizeIdentifier(request.Code);
        var item = id.HasValue ? await dbContext.VehicleModels.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) : null;
        if (id.HasValue && item is null) return Result.Failure<VehicleModelResponse>(FleetErrors.NotFound);
        if (item is not null && !FleetServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion)) return Result.Failure<VehicleModelResponse>(FleetErrors.ConcurrencyConflict);
        if (await dbContext.VehicleModels.AnyAsync(x => x.Id != id && x.VehicleManufacturerId == request.VehicleManufacturerId && x.Code == code, cancellationToken)) return Result.Failure<VehicleModelResponse>(FleetErrors.Duplicate);
        item ??= new VehicleModel();
        item.VehicleManufacturerId = request.VehicleManufacturerId;
        item.Code = code;
        item.NameAr = request.NameAr.Trim();
        item.NameEn = request.NameEn.Trim();
        item.VehicleType = request.VehicleType;
        item.DefaultFuelType = request.DefaultFuelType;
        item.Status = request.Status;
        if (!id.HasValue) dbContext.VehicleModels.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(new VehicleModelResponse(item.Id, item.VehicleManufacturerId, item.Code, item.NameAr, item.NameEn, item.VehicleType, item.DefaultFuelType, item.Status, FleetServiceSupport.EncodeRowVersion(item.RowVersion)));
    }

    public async Task<Result<IReadOnlyList<FleetLocationResponse>>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await support.AccessibleLocationIdsAsync(PermissionKeys.Fleet.VehiclesRead, cancellationToken);
        var items = await dbContext.FleetLocations.AsNoTracking().Where(x => ids.Contains(x.Id)).OrderBy(x => x.NameEn)
            .Select(x => new FleetLocationResponse(x.Id, x.Code, x.NameAr, x.NameEn, x.LocationType, x.HousingId, x.Address, x.Latitude, x.Longitude, x.Status, Convert.ToBase64String(x.RowVersion)))
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<FleetLocationResponse>>(items);
    }

    public async Task<Result<FleetLocationResponse>> UpsertLocationAsync(Guid? id, FleetLocationRequest request, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesManage, request.HousingId, cancellationToken)) return Result.Failure<FleetLocationResponse>(FleetErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NameAr) || string.IsNullOrWhiteSpace(request.NameEn)
            || request.LocationType == FleetLocationType.Housing != request.HousingId.HasValue
            || request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180) return Result.Failure<FleetLocationResponse>(FleetErrors.InvalidRequest);
        if (request.HousingId.HasValue && !await dbContext.Housing.AnyAsync(x => x.Id == request.HousingId, cancellationToken)) return Result.Failure<FleetLocationResponse>(FleetErrors.NotFound);
        var code = FleetServiceSupport.NormalizeIdentifier(request.Code);
        var item = id.HasValue ? await dbContext.FleetLocations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) : null;
        if (id.HasValue && item is null) return Result.Failure<FleetLocationResponse>(FleetErrors.NotFound);
        if (item is not null && !FleetServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion)) return Result.Failure<FleetLocationResponse>(FleetErrors.ConcurrencyConflict);
        if (await dbContext.FleetLocations.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken)) return Result.Failure<FleetLocationResponse>(FleetErrors.Duplicate);
        item ??= new FleetLocation();
        item.Code = code; item.NameAr = request.NameAr.Trim(); item.NameEn = request.NameEn.Trim(); item.LocationType = request.LocationType;
        item.HousingId = request.HousingId; item.Address = FleetServiceSupport.TrimOrNull(request.Address); item.Latitude = request.Latitude; item.Longitude = request.Longitude; item.Status = request.Status;
        if (!id.HasValue) dbContext.FleetLocations.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(new FleetLocationResponse(item.Id, item.Code, item.NameAr, item.NameEn, item.LocationType, item.HousingId, item.Address, item.Latitude, item.Longitude, item.Status, FleetServiceSupport.EncodeRowVersion(item.RowVersion)));
    }

    public async Task<Result<PagedResponse<VehicleSummaryResponse>>> GetVehiclesAsync(string? search, string? status, Guid? locationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        var accessible = await support.AccessibleLocationIdsAsync(PermissionKeys.Fleet.VehiclesRead, cancellationToken);
        var hasGlobal = await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesRead, null, cancellationToken);
        var query = dbContext.Vehicles.AsNoTracking().Where(x => hasGlobal || x.CurrentLocationId != null && accessible.Contains(x.CurrentLocationId.Value));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = FleetServiceSupport.NormalizeIdentifier(search);
            query = query.Where(x => x.NormalizedAssetNumber.Contains(normalized) || x.NormalizedPlateNumberAr != null && x.NormalizedPlateNumberAr.Contains(normalized) || x.NormalizedPlateNumberEn != null && x.NormalizedPlateNumberEn.Contains(normalized));
        }
        if (Enum.TryParse<VehicleOperationalStatus>(status, true, out var parsedStatus)) query = query.Where(x => x.CurrentOperationalStatus == parsedStatus);
        if (locationId.HasValue) query = query.Where(x => x.CurrentLocationId == locationId);
        var count = await query.CountAsync(cancellationToken);
        var vehicles = await query.OrderBy(x => x.AssetNumber).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        var summaries = await BuildSummariesAsync(vehicles, cancellationToken);
        return Result.Success(new PagedResponse<VehicleSummaryResponse>(summaries, page, pageSize, count));
    }

    public async Task<Result<IReadOnlyList<VehicleLookupResponse>>> LookupVehiclesAsync(string? search, CancellationToken cancellationToken = default)
    {
        var result = await GetVehiclesAsync(search, null, null, 1, 200, cancellationToken);
        return result.IsFailure
            ? Result.Failure<IReadOnlyList<VehicleLookupResponse>>(result.Error)
            : Result.Success<IReadOnlyList<VehicleLookupResponse>>(result.Value!.Items.Select(x => new VehicleLookupResponse(x.Id, x.AssetNumber, x.PlateNumberAr, x.PlateNumberEn, x.Status)).ToArray());
    }

    public async Task<Result<VehicleDetailResponse>> GetVehicleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (vehicle is null) return Result.Failure<VehicleDetailResponse>(FleetErrors.NotFound);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.VehiclesRead, cancellationToken)) return Result.Failure<VehicleDetailResponse>(FleetErrors.Forbidden);
        return Result.Success(await BuildDetailAsync(vehicle, cancellationToken));
    }

    public async Task<Result<VehicleDetailResponse>> UpsertVehicleAsync(Guid? id, VehicleUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var location = request.CurrentLocationId.HasValue ? await dbContext.FleetLocations.SingleOrDefaultAsync(x => x.Id == request.CurrentLocationId, cancellationToken) : null;
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesManage, location?.HousingId, cancellationToken)) return Result.Failure<VehicleDetailResponse>(FleetErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(request.AssetNumber) || request.CurrentOdometer < 0 || request.ModelYear is < 1950 or > 2200) return Result.Failure<VehicleDetailResponse>(FleetErrors.InvalidRequest);
        var modelValid = await dbContext.VehicleModels.AnyAsync(x => x.Id == request.VehicleModelId && x.VehicleManufacturerId == request.VehicleManufacturerId, cancellationToken);
        if (!modelValid || request.CurrentLocationId.HasValue && location is null) return Result.Failure<VehicleDetailResponse>(FleetErrors.NotFound);
        var normalizedAsset = FleetServiceSupport.NormalizeIdentifier(request.AssetNumber);
        var normalizedAr = string.IsNullOrWhiteSpace(request.PlateNumberAr) ? null : FleetServiceSupport.NormalizeIdentifier(request.PlateNumberAr);
        var normalizedEn = string.IsNullOrWhiteSpace(request.PlateNumberEn) ? null : FleetServiceSupport.NormalizeIdentifier(request.PlateNumberEn);
        if (await dbContext.Vehicles.AnyAsync(x => x.Id != id && (x.NormalizedAssetNumber == normalizedAsset || normalizedAr != null && x.NormalizedPlateNumberAr == normalizedAr || normalizedEn != null && x.NormalizedPlateNumberEn == normalizedEn || request.Vin != null && x.Vin == request.Vin), cancellationToken)) return Result.Failure<VehicleDetailResponse>(FleetErrors.Duplicate);
        var vehicle = id.HasValue ? await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) : null;
        if (id.HasValue && vehicle is null) return Result.Failure<VehicleDetailResponse>(FleetErrors.NotFound);
        if (vehicle is not null && !FleetServiceSupport.MatchesRowVersion(vehicle.RowVersion, request.RowVersion)) return Result.Failure<VehicleDetailResponse>(FleetErrors.ConcurrencyConflict);
        if (vehicle is not null && request.CurrentOdometer < vehicle.CurrentOdometer) return Result.Failure<VehicleDetailResponse>(FleetErrors.OdometerDecreased);
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<VehicleDetailResponse>(FleetErrors.CurrentUserUnavailable);
        var isNew = vehicle is null;
        vehicle ??= new Vehicle();
        ApplyVehicle(vehicle, request, normalizedAsset, normalizedAr, normalizedEn);
        if (isNew)
        {
            dbContext.Vehicles.Add(vehicle);
            dbContext.VehicleOperationalStatusPeriods.Add(NewStatus(vehicle.Id, VehicleOperationalStatus.Available, support.UtcNow, "Vehicle created.", VehicleStatusSourceType.Vehicle, vehicle.Id, actor.Value));
            dbContext.VehicleOdometerReadings.Add(NewOdometer(vehicle.Id, request.CurrentOdometer, support.UtcNow, VehicleOdometerSourceType.Manual, vehicle.Id, "Initial odometer reading."));
        }
        else if (dbContext.Entry(vehicle).Property(x => x.CurrentOdometer).OriginalValue != request.CurrentOdometer)
        {
            vehicle.LastOdometerAtUtc = support.UtcNow;
            dbContext.VehicleOdometerReadings.Add(NewOdometer(vehicle.Id, request.CurrentOdometer, support.UtcNow, VehicleOdometerSourceType.Manual, vehicle.Id, "Vehicle odometer updated."));
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildDetailAsync(vehicle, cancellationToken));
    }

    public async Task<Result> ArchiveVehicleAsync(Guid id, ArchiveFleetRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (vehicle is null) return Result.Failure(FleetErrors.NotFound);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.VehiclesArchive, cancellationToken)) return Result.Failure(FleetErrors.Forbidden);
        if (!FleetServiceSupport.MatchesRowVersion(vehicle.RowVersion, request.RowVersion) || string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure(FleetErrors.ConcurrencyConflict);
        if (vehicle.CurrentAssignmentId.HasValue) return Result.Failure(FleetErrors.Conflict);
        vehicle.IsDeleted = true; vehicle.DeletionReason = request.Reason.Trim();
        await CloseCurrentStatusAsync(vehicle.Id, support.UtcNow, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<VehicleDetailResponse>> RestoreVehicleAsync(Guid id, string rowVersion, CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == id && x.IsDeleted, cancellationToken);
        if (vehicle is null) return Result.Failure<VehicleDetailResponse>(FleetErrors.NotFound);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.VehiclesArchive, cancellationToken)) return Result.Failure<VehicleDetailResponse>(FleetErrors.Forbidden);
        if (!FleetServiceSupport.MatchesRowVersion(vehicle.RowVersion, rowVersion)) return Result.Failure<VehicleDetailResponse>(FleetErrors.ConcurrencyConflict);
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<VehicleDetailResponse>(FleetErrors.CurrentUserUnavailable);
        vehicle.IsDeleted = false; vehicle.DeletedAtUtc = null; vehicle.DeletedByUserId = null; vehicle.DeletionReason = null;
        vehicle.CurrentOperationalStatus = VehicleOperationalStatus.Available;
        dbContext.VehicleOperationalStatusPeriods.Add(NewStatus(vehicle.Id, VehicleOperationalStatus.Available, support.UtcNow, "Vehicle restored from archive.", VehicleStatusSourceType.Administrative, vehicle.Id, actor.Value));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildDetailAsync(vehicle, cancellationToken));
    }

    public async Task<Result<VehicleDetailResponse>> ChangeAdministrativeStatusAsync(Guid id, string action, VehicleStatusCommandRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (vehicle is null) return Result.Failure<VehicleDetailResponse>(FleetErrors.NotFound);
        var permission = action.Equals("decommission", StringComparison.OrdinalIgnoreCase) ? PermissionKeys.Fleet.VehiclesDecommission : PermissionKeys.Fleet.VehiclesManage;
        if (!await support.HasVehiclePermissionAsync(vehicle, permission, cancellationToken)) return Result.Failure<VehicleDetailResponse>(FleetErrors.Forbidden);
        if (!FleetServiceSupport.MatchesRowVersion(vehicle.RowVersion, request.RowVersion) || string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure<VehicleDetailResponse>(FleetErrors.ConcurrencyConflict);
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<VehicleDetailResponse>(FleetErrors.CurrentUserUnavailable);
        var target = action.ToLowerInvariant() switch
        {
            "stolen" when vehicle.CurrentOperationalStatus != VehicleOperationalStatus.Decommissioned => VehicleOperationalStatus.Stolen,
            "recover" when vehicle.CurrentOperationalStatus == VehicleOperationalStatus.Stolen => VehicleOperationalStatus.Available,
            "out-of-service" when vehicle.CurrentOperationalStatus != VehicleOperationalStatus.Decommissioned => VehicleOperationalStatus.OutOfService,
            "restore" when vehicle.CurrentOperationalStatus == VehicleOperationalStatus.OutOfService => VehicleOperationalStatus.Available,
            "decommission" when vehicle.CurrentOperationalStatus != VehicleOperationalStatus.Decommissioned => VehicleOperationalStatus.Decommissioned,
            _ => (VehicleOperationalStatus?)null
        };
        if (!target.HasValue) return Result.Failure<VehicleDetailResponse>(FleetErrors.InvalidState);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (target is VehicleOperationalStatus.Stolen or VehicleOperationalStatus.OutOfService or VehicleOperationalStatus.Decommissioned)
        {
            await EndActiveAssignmentForHoldAsync(vehicle, request.EffectiveAtUtc, request.Reason, actor.Value, cancellationToken);
        }
        if (target == VehicleOperationalStatus.Available && await HasBlockingIssueAsync(vehicle.Id, null, cancellationToken)) return Result.Failure<VehicleDetailResponse>(FleetErrors.Conflict);
        await SetStatusAsync(vehicle, target.Value, request.EffectiveAtUtc, request.Reason, VehicleStatusSourceType.Administrative, vehicle.Id, actor.Value, cancellationToken);
        if (target == VehicleOperationalStatus.Decommissioned) { vehicle.DecommissionedAtUtc = request.EffectiveAtUtc; vehicle.DecommissionReason = request.Reason.Trim(); }
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Result.Success(await BuildDetailAsync(vehicle, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<VehicleStatusPeriodResponse>>> GetStatusHistoryAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(vehicleId, PermissionKeys.Fleet.VehiclesRead, cancellationToken);
        if (access.IsFailure) return Result.Failure<IReadOnlyList<VehicleStatusPeriodResponse>>(access.Error);
        var items = await dbContext.VehicleOperationalStatusPeriods.AsNoTracking().Where(x => x.VehicleId == vehicleId).OrderByDescending(x => x.EffectiveFromUtc)
            .Select(x => new VehicleStatusPeriodResponse(x.Id, x.Status, x.EffectiveFromUtc, x.EffectiveToUtc, x.Reason, x.SourceType, x.SourceEntityId)).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<VehicleStatusPeriodResponse>>(items);
    }

    public async Task<Result<VehicleOdometerReadingResponse>> RecordOdometerAsync(Guid vehicleId, OdometerReadingRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == vehicleId, cancellationToken);
        if (vehicle is null) return Result.Failure<VehicleOdometerReadingResponse>(FleetErrors.NotFound);
        var permission = request.IsCorrection ? PermissionKeys.Fleet.CorrectionsManage : PermissionKeys.Fleet.VehiclesManage;
        if (!await support.HasVehiclePermissionAsync(vehicle, permission, cancellationToken)) return Result.Failure<VehicleOdometerReadingResponse>(FleetErrors.Forbidden);
        if (!FleetServiceSupport.MatchesRowVersion(vehicle.RowVersion, request.RowVersion) || request.Reading < 0 || request.IsCorrection && string.IsNullOrWhiteSpace(request.CorrectionReason)) return Result.Failure<VehicleOdometerReadingResponse>(FleetErrors.InvalidRequest);
        if (request.Reading < vehicle.CurrentOdometer && !request.IsCorrection) return Result.Failure<VehicleOdometerReadingResponse>(FleetErrors.OdometerDecreased);
        var reading = NewOdometer(vehicle.Id, request.Reading, request.RecordedAtUtc, request.IsCorrection ? VehicleOdometerSourceType.Correction : VehicleOdometerSourceType.Manual, vehicle.Id, request.Notes);
        reading.IsCorrection = request.IsCorrection; reading.CorrectionReason = FleetServiceSupport.TrimOrNull(request.CorrectionReason);
        vehicle.CurrentOdometer = request.Reading; vehicle.LastOdometerAtUtc = request.RecordedAtUtc;
        dbContext.VehicleOdometerReadings.Add(reading);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(MapOdometer(reading));
    }

    public async Task<Result<IReadOnlyList<VehicleOdometerReadingResponse>>> GetOdometerHistoryAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(vehicleId, PermissionKeys.Fleet.VehiclesRead, cancellationToken);
        if (access.IsFailure) return Result.Failure<IReadOnlyList<VehicleOdometerReadingResponse>>(access.Error);
        var readings = await dbContext.VehicleOdometerReadings.AsNoTracking().Where(x => x.VehicleId == vehicleId).OrderByDescending(x => x.RecordedAtUtc).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<VehicleOdometerReadingResponse>>(readings.Select(MapOdometer).ToArray());
    }

    public Task<Result<RiderVehicleAssignmentResponse>> TakeAsync(TakeVehicleRequest request, string idempotencyKey, CancellationToken cancellationToken = default) =>
        ExecuteTakeAsync(request, idempotencyKey, null, RiderVehicleAssignmentEventType.Taken, cancellationToken);

    private async Task<Result<RiderVehicleAssignmentResponse>> ExecuteTakeAsync(TakeVehicleRequest request, string idempotencyKey, Guid? previousAssignmentId, RiderVehicleAssignmentEventType eventType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.IdempotencyRequired);
        var hash = FleetServiceSupport.HashRequest(request);
        var replay = await ReplayAssignmentAsync("take", idempotencyKey, hash, cancellationToken);
        if (replay is not null) return replay;
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.CurrentUserUnavailable);
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == request.VehicleId, cancellationToken);
        if (vehicle is null) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.NotFound);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.AssignmentsManage, cancellationToken)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Forbidden);
        if (vehicle.CurrentOperationalStatus != VehicleOperationalStatus.Available || vehicle.CurrentAssignmentId.HasValue || request.StartOdometer < vehicle.CurrentOdometer || !ValidFuel(request.StartFuelLevelPercentage) || !ValidPermission(request.PermissionStartsOn, request.PermissionEndsOn) || string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.VehicleUnavailable);
        var rider = await dbContext.RiderProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.RiderProfileId, cancellationToken);
        if (rider is null || !await dbContext.Employees.AnyAsync(x => x.Id == rider.EmployeeId && !x.IsEmployee && x.Status == EmployeeStatus.Active, cancellationToken)
            || await dbContext.RiderVehicleAssignments.AnyAsync(x => x.RiderProfileId == request.RiderProfileId && x.EndedAtUtc == null, cancellationToken)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.RiderUnavailable);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var operationId = Guid.CreateVersion7();
        var assignment = new RiderVehicleAssignment
        {
            RiderProfileId = rider.Id, VehicleId = vehicle.Id, OperationId = operationId, PreviousAssignmentId = previousAssignmentId,
            StartedAtUtc = request.StartedAtUtc, StartLocationId = request.StartLocationId ?? vehicle.CurrentLocationId, StartOdometer = request.StartOdometer,
            StartVehicleCondition = request.StartCondition, StartFuelLevelPercentage = request.StartFuelLevelPercentage, PermissionReference = FleetServiceSupport.TrimOrNull(request.PermissionReference),
            PermissionStartsOn = request.PermissionStartsOn, PermissionEndsOn = request.PermissionEndsOn, AssignmentReason = request.Reason.Trim(), AssignedByUserId = actor.Value,
            WasBackdated = request.StartedAtUtc < support.UtcNow.AddMinutes(-5), BackdatedReason = request.StartedAtUtc < support.UtcNow.AddMinutes(-5) ? request.Reason.Trim() : null, Notes = FleetServiceSupport.TrimOrNull(request.Notes)
        };
        dbContext.RiderVehicleAssignments.Add(assignment);
        dbContext.RiderVehicleAssignmentEvents.Add(NewAssignmentEvent(assignment.Id, operationId, eventType, request.StartedAtUtc, actor.Value, request.Reason));
        if (request.StartOdometer > vehicle.CurrentOdometer)
        {
            vehicle.CurrentOdometer = request.StartOdometer; vehicle.LastOdometerAtUtc = request.StartedAtUtc;
            dbContext.VehicleOdometerReadings.Add(NewOdometer(vehicle.Id, request.StartOdometer, request.StartedAtUtc, VehicleOdometerSourceType.AssignmentTake, assignment.Id, request.Reason));
        }
        vehicle.CurrentAssignmentId = assignment.Id;
        vehicle.CurrentLocationId = request.StartLocationId ?? vehicle.CurrentLocationId;
        await SetStatusAsync(vehicle, VehicleOperationalStatus.Assigned, request.StartedAtUtc, request.Reason, VehicleStatusSourceType.Assignment, assignment.Id, actor.Value, cancellationToken);
        dbContext.FleetCommandReceipts.Add(new FleetCommandReceipt { CommandName = "take", IdempotencyKey = idempotencyKey.Trim(), RequestHash = hash, ResultEntityId = assignment.Id });
        try { await dbContext.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken); }
        catch (DbUpdateException) { await tx.RollbackAsync(cancellationToken); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Conflict); }
        return Result.Success(await MapAssignmentAsync(assignment, cancellationToken));
    }

    public async Task<Result<RiderVehicleAssignmentResponse>> ReturnAsync(ReturnVehicleRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.IdempotencyRequired);
        var hash = FleetServiceSupport.HashRequest(request);
        var replay = await ReplayAssignmentAsync("return", idempotencyKey, hash, cancellationToken);
        if (replay is not null) return replay;
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.CurrentUserUnavailable);
        var assignment = await dbContext.RiderVehicleAssignments.SingleOrDefaultAsync(x => x.Id == request.AssignmentId && x.EndedAtUtc == null, cancellationToken);
        if (assignment is null) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.NotFound);
        var vehicle = await dbContext.Vehicles.SingleAsync(x => x.Id == assignment.VehicleId, cancellationToken);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.AssignmentsManage, cancellationToken)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Forbidden);
        if (!FleetServiceSupport.MatchesRowVersion(assignment.RowVersion, request.RowVersion) || request.EndedAtUtc < assignment.StartedAtUtc || request.EndOdometer < assignment.StartOdometer || !ValidFuel(request.EndFuelLevelPercentage) || string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.InvalidRequest);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        EndAssignment(assignment, vehicle, request.EndedAtUtc, request.EndLocationId, request.EndOdometer, request.EndCondition, request.EndFuelLevelPercentage, request.Reason, actor.Value, RiderVehicleAssignmentEventType.Returned);
        var target = await ResolveAvailableStatusAsync(vehicle.Id, null, cancellationToken);
        await SetStatusAsync(vehicle, target, request.EndedAtUtc, request.Reason, VehicleStatusSourceType.Assignment, assignment.Id, actor.Value, cancellationToken);
        dbContext.FleetCommandReceipts.Add(new FleetCommandReceipt { CommandName = "return", IdempotencyKey = idempotencyKey.Trim(), RequestHash = hash, ResultEntityId = assignment.Id });
        try { await dbContext.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken); }
        catch (DbUpdateException) { await tx.RollbackAsync(cancellationToken); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Conflict); }
        return Result.Success(await MapAssignmentAsync(assignment, cancellationToken));
    }

    public async Task<Result<RiderVehicleAssignmentResponse>> SwitchAsync(SwitchVehicleRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.IdempotencyRequired);
        var hash = FleetServiceSupport.HashRequest(request);
        var replay = await ReplayAssignmentAsync("switch", idempotencyKey, hash, cancellationToken);
        if (replay is not null) return replay;
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.CurrentUserUnavailable);
        var old = await dbContext.RiderVehicleAssignments.SingleOrDefaultAsync(x => x.Id == request.CurrentAssignmentId && x.EndedAtUtc == null, cancellationToken);
        var next = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == request.NewVehicleId, cancellationToken);
        if (old is null || next is null) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.NotFound);
        var oldVehicle = await dbContext.Vehicles.SingleAsync(x => x.Id == old.VehicleId, cancellationToken);
        if (!await support.HasVehiclePermissionAsync(oldVehicle, PermissionKeys.Fleet.AssignmentsManage, cancellationToken) || !await support.HasVehiclePermissionAsync(next, PermissionKeys.Fleet.AssignmentsManage, cancellationToken)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Forbidden);
        if (!FleetServiceSupport.MatchesRowVersion(old.RowVersion, request.RowVersion) || next.CurrentOperationalStatus != VehicleOperationalStatus.Available || next.CurrentAssignmentId.HasValue || request.OldVehicleOdometer < old.StartOdometer || request.NewVehicleOdometer < next.CurrentOdometer || !ValidPermission(request.PermissionStartsOn, request.PermissionEndsOn)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Conflict);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        EndAssignment(old, oldVehicle, request.SwitchedAtUtc, request.LocationId, request.OldVehicleOdometer, request.OldVehicleCondition, request.OldFuelLevelPercentage, request.Reason, actor.Value, RiderVehicleAssignmentEventType.SwitchedOut);
        await SetStatusAsync(oldVehicle, await ResolveAvailableStatusAsync(oldVehicle.Id, null, cancellationToken), request.SwitchedAtUtc, request.Reason, VehicleStatusSourceType.Assignment, old.Id, actor.Value, cancellationToken);
        var newAssignment = new RiderVehicleAssignment
        {
            RiderProfileId = old.RiderProfileId, VehicleId = next.Id, OperationId = old.OperationId, PreviousAssignmentId = old.Id,
            StartedAtUtc = request.SwitchedAtUtc, StartLocationId = request.LocationId ?? next.CurrentLocationId, StartOdometer = request.NewVehicleOdometer,
            StartVehicleCondition = request.NewVehicleCondition, StartFuelLevelPercentage = request.NewFuelLevelPercentage, PermissionReference = FleetServiceSupport.TrimOrNull(request.PermissionReference),
            PermissionStartsOn = request.PermissionStartsOn, PermissionEndsOn = request.PermissionEndsOn, AssignmentReason = request.Reason.Trim(), AssignedByUserId = actor.Value
        };
        dbContext.RiderVehicleAssignments.Add(newAssignment);
        dbContext.RiderVehicleAssignmentEvents.Add(NewAssignmentEvent(newAssignment.Id, old.OperationId, RiderVehicleAssignmentEventType.SwitchedIn, request.SwitchedAtUtc, actor.Value, request.Reason));
        if (request.NewVehicleOdometer > next.CurrentOdometer) dbContext.VehicleOdometerReadings.Add(NewOdometer(next.Id, request.NewVehicleOdometer, request.SwitchedAtUtc, VehicleOdometerSourceType.AssignmentTake, newAssignment.Id, request.Reason));
        next.CurrentOdometer = request.NewVehicleOdometer; next.LastOdometerAtUtc = request.SwitchedAtUtc; next.CurrentLocationId = request.LocationId ?? next.CurrentLocationId; next.CurrentAssignmentId = newAssignment.Id;
        await SetStatusAsync(next, VehicleOperationalStatus.Assigned, request.SwitchedAtUtc, request.Reason, VehicleStatusSourceType.Assignment, newAssignment.Id, actor.Value, cancellationToken);
        dbContext.FleetCommandReceipts.Add(new FleetCommandReceipt { CommandName = "switch", IdempotencyKey = idempotencyKey.Trim(), RequestHash = hash, ResultEntityId = newAssignment.Id });
        try { await dbContext.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken); }
        catch (DbUpdateException) { await tx.RollbackAsync(cancellationToken); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Conflict); }
        return Result.Success(await MapAssignmentAsync(newAssignment, cancellationToken));
    }

    public async Task<Result<RiderVehicleAssignmentResponse>> RenewPermissionAsync(Guid assignmentId, RenewVehiclePermissionRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.IdempotencyRequired);
        var hash = FleetServiceSupport.HashRequest(request);
        var replay = await ReplayAssignmentAsync("renew-permission", idempotencyKey, hash, cancellationToken);
        if (replay is not null) return replay;
        var assignment = await dbContext.RiderVehicleAssignments.SingleOrDefaultAsync(x => x.Id == assignmentId && x.EndedAtUtc == null, cancellationToken);
        if (assignment is null) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.NotFound);
        var vehicle = await dbContext.Vehicles.SingleAsync(x => x.Id == assignment.VehicleId, cancellationToken);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.AssignmentsManage, cancellationToken)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Forbidden);
        if (!FleetServiceSupport.MatchesRowVersion(assignment.RowVersion, request.RowVersion) || request.PermissionEndsOn < request.PermissionStartsOn || assignment.PermissionEndsOn.HasValue && request.PermissionEndsOn <= assignment.PermissionEndsOn.Value || string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.InvalidRequest);
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.CurrentUserUnavailable);
        assignment.PermissionStartsOn = request.PermissionStartsOn; assignment.PermissionEndsOn = request.PermissionEndsOn; assignment.PermissionReference = FleetServiceSupport.TrimOrNull(request.PermissionReference);
        dbContext.RiderVehicleAssignmentEvents.Add(NewAssignmentEvent(assignment.Id, assignment.OperationId, RiderVehicleAssignmentEventType.PermissionRenewed, support.UtcNow, actor.Value, request.Reason));
        dbContext.FleetCommandReceipts.Add(new FleetCommandReceipt { CommandName = "renew-permission", IdempotencyKey = idempotencyKey.Trim(), RequestHash = hash, ResultEntityId = assignment.Id });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await MapAssignmentAsync(assignment, cancellationToken));
    }

    public Task<Result<IReadOnlyList<RiderVehicleTimelineResponse>>> GetVehicleTimelineAsync(Guid vehicleId, CancellationToken cancellationToken = default) => GetTimelineAsync(vehicleId, null, cancellationToken);
    public Task<Result<IReadOnlyList<RiderVehicleTimelineResponse>>> GetRiderTimelineAsync(Guid riderProfileId, CancellationToken cancellationToken = default) => GetTimelineAsync(null, riderProfileId, cancellationToken);

    private async Task<Result<IReadOnlyList<RiderVehicleTimelineResponse>>> GetTimelineAsync(Guid? vehicleId, Guid? riderId, CancellationToken cancellationToken)
    {
        var permissionVehicle = vehicleId.HasValue ? await dbContext.Vehicles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == vehicleId, cancellationToken) : null;
        if (vehicleId.HasValue && permissionVehicle is null) return Result.Failure<IReadOnlyList<RiderVehicleTimelineResponse>>(FleetErrors.NotFound);
        if (permissionVehicle is not null && !await support.HasVehiclePermissionAsync(permissionVehicle, PermissionKeys.Fleet.AssignmentsRead, cancellationToken)) return Result.Failure<IReadOnlyList<RiderVehicleTimelineResponse>>(FleetErrors.Forbidden);
        var query = dbContext.RiderVehicleAssignments.AsNoTracking();
        if (vehicleId.HasValue) query = query.Where(x => x.VehicleId == vehicleId);
        if (riderId.HasValue) query = query.Where(x => x.RiderProfileId == riderId);
        var assignments = await query.OrderByDescending(x => x.StartedAtUtc).ToArrayAsync(cancellationToken);
        var result = new List<RiderVehicleTimelineResponse>();
        foreach (var assignment in assignments)
        {
            var vehicle = await dbContext.Vehicles.AsNoTracking().SingleAsync(x => x.Id == assignment.VehicleId, cancellationToken);
            if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.AssignmentsRead, cancellationToken)) continue;
            var issues = await dbContext.VehicleIssues.AsNoTracking().Where(x => x.RelatedAssignmentId == assignment.Id).OrderByDescending(x => x.ReportedAtUtc).ToArrayAsync(cancellationToken);
            var accidents = await dbContext.VehicleAccidents.AsNoTracking().Where(x => x.RiderVehicleAssignmentId == assignment.Id).OrderByDescending(x => x.OccurredAtUtc).ToArrayAsync(cancellationToken);
            result.Add(new RiderVehicleTimelineResponse(await MapAssignmentAsync(assignment, cancellationToken), issues.Select(MapIssue).ToArray(), accidents.Select(MapAccident).ToArray()));
        }
        return Result.Success<IReadOnlyList<RiderVehicleTimelineResponse>>(result);
    }

    public async Task<Result<IReadOnlyList<VehicleComplianceResponse>>> GetComplianceAsync(Guid vehicleId, string type, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(vehicleId, PermissionKeys.Fleet.ComplianceRead, cancellationToken);
        if (access.IsFailure) return Result.Failure<IReadOnlyList<VehicleComplianceResponse>>(access.Error);
        var check = DateOnly.FromDateTime(support.UtcNow.UtcDateTime);
        IReadOnlyList<VehicleComplianceResponse> result = type.ToLowerInvariant() switch
        {
            "registrations" => await dbContext.VehicleRegistrations.AsNoTracking().Where(x => x.VehicleId == vehicleId).OrderByDescending(x => x.ExpiryDate).Select(x => new VehicleComplianceResponse(x.Id, x.VehicleId, "Registration", x.RegistrationNumber, x.IssuingAuthority, x.IssueDate, x.ExpiryDate, FleetServiceSupport.DueStatus(x.ExpiryDate, check), x.IsCurrent, x.PreviousRecordId, Convert.ToBase64String(x.RowVersion))).ToArrayAsync(cancellationToken),
            "insurance-policies" => await dbContext.VehicleInsurancePolicies.AsNoTracking().Where(x => x.VehicleId == vehicleId).OrderByDescending(x => x.ExpiryDate).Select(x => new VehicleComplianceResponse(x.Id, x.VehicleId, "Insurance", x.PolicyNumber, x.ProviderName, x.EffectiveFrom, x.ExpiryDate, FleetServiceSupport.DueStatus(x.ExpiryDate, check), x.IsCurrent, x.PreviousRecordId, Convert.ToBase64String(x.RowVersion))).ToArrayAsync(cancellationToken),
            "inspections" => await dbContext.VehiclePeriodicInspections.AsNoTracking().Where(x => x.VehicleId == vehicleId).OrderByDescending(x => x.ExpiryDate).Select(x => new VehicleComplianceResponse(x.Id, x.VehicleId, "Inspection", x.InspectionNumber, x.StationName, x.InspectionDate, x.ExpiryDate, FleetServiceSupport.DueStatus(x.ExpiryDate, check), x.IsCurrent, x.PreviousRecordId, Convert.ToBase64String(x.RowVersion))).ToArrayAsync(cancellationToken),
            _ => []
        };
        return type is "registrations" or "insurance-policies" or "inspections" ? Result.Success(result) : Result.Failure<IReadOnlyList<VehicleComplianceResponse>>(FleetErrors.InvalidRequest);
    }

    public async Task<Result<VehicleComplianceResponse>> RenewRegistrationAsync(Guid vehicleId, VehicleRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(vehicleId, PermissionKeys.Fleet.ComplianceManage, cancellationToken);
        if (access.IsFailure) return Result.Failure<VehicleComplianceResponse>(access.Error);
        if (string.IsNullOrWhiteSpace(request.RegistrationNumber) || string.IsNullOrWhiteSpace(request.IssuingAuthority) || request.ExpiryDate < request.IssueDate) return Result.Failure<VehicleComplianceResponse>(FleetErrors.InvalidRequest);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var previous = await dbContext.VehicleRegistrations.SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.IsCurrent, cancellationToken);
        if (previous is not null) { previous.IsCurrent = false; previous.Status = ComplianceRecordStatus.Superseded; }
        var item = new VehicleRegistration { VehicleId = vehicleId, RegistrationNumber = request.RegistrationNumber.Trim(), IssuingAuthority = request.IssuingAuthority.Trim(), IssueDate = request.IssueDate, ExpiryDate = request.ExpiryDate, PreviousRecordId = previous?.Id, Notes = FleetServiceSupport.TrimOrNull(request.Notes) };
        dbContext.VehicleRegistrations.Add(item); await dbContext.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken);
        return Result.Success(MapCompliance(item));
    }

    public async Task<Result<VehicleComplianceResponse>> RenewInsuranceAsync(Guid vehicleId, VehicleInsuranceRequest request, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(vehicleId, PermissionKeys.Fleet.ComplianceManage, cancellationToken);
        if (access.IsFailure) return Result.Failure<VehicleComplianceResponse>(access.Error);
        if (string.IsNullOrWhiteSpace(request.ProviderName) || string.IsNullOrWhiteSpace(request.PolicyNumber) || request.ExpiryDate < request.EffectiveFrom) return Result.Failure<VehicleComplianceResponse>(FleetErrors.InvalidRequest);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var previous = await dbContext.VehicleInsurancePolicies.SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.IsCurrent, cancellationToken);
        if (previous is not null) { previous.IsCurrent = false; previous.Status = ComplianceRecordStatus.Superseded; }
        var item = new VehicleInsurancePolicy { VehicleId = vehicleId, ProviderName = request.ProviderName.Trim(), PolicyNumber = request.PolicyNumber.Trim(), CoverageType = FleetServiceSupport.TrimOrNull(request.CoverageType), EffectiveFrom = request.EffectiveFrom, ExpiryDate = request.ExpiryDate, ClaimReference = FleetServiceSupport.TrimOrNull(request.ClaimReference), ClaimContact = FleetServiceSupport.TrimOrNull(request.ClaimContact), PreviousRecordId = previous?.Id, Notes = FleetServiceSupport.TrimOrNull(request.Notes) };
        dbContext.VehicleInsurancePolicies.Add(item); await dbContext.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken);
        return Result.Success(MapCompliance(item));
    }

    public async Task<Result<VehicleComplianceResponse>> RenewInspectionAsync(Guid vehicleId, VehicleInspectionRequest request, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(vehicleId, PermissionKeys.Fleet.ComplianceManage, cancellationToken);
        if (access.IsFailure) return Result.Failure<VehicleComplianceResponse>(access.Error);
        if (string.IsNullOrWhiteSpace(request.InspectionNumber) || string.IsNullOrWhiteSpace(request.StationName) || request.ExpiryDate < request.InspectionDate || request.Odometer < 0) return Result.Failure<VehicleComplianceResponse>(FleetErrors.InvalidRequest);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var previous = await dbContext.VehiclePeriodicInspections.SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.IsCurrent, cancellationToken);
        if (previous is not null) { previous.IsCurrent = false; previous.Status = ComplianceRecordStatus.Superseded; }
        var item = new VehiclePeriodicInspection { VehicleId = vehicleId, InspectionNumber = request.InspectionNumber.Trim(), StationName = request.StationName.Trim(), InspectionDate = request.InspectionDate, ExpiryDate = request.ExpiryDate, Result = request.Result, Odometer = request.Odometer, FailureNotes = FleetServiceSupport.TrimOrNull(request.FailureNotes), PreviousRecordId = previous?.Id, Notes = FleetServiceSupport.TrimOrNull(request.Notes) };
        dbContext.VehiclePeriodicInspections.Add(item); await dbContext.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken);
        return Result.Success(MapCompliance(item));
    }

    public async Task<Result<IReadOnlyList<VehicleComplianceDueResponse>>> GetComplianceDueAsync(DateOnly checkDate, CancellationToken cancellationToken = default)
    {
        var vehiclesResult = await GetVehiclesAsync(null, null, null, 1, 200, cancellationToken);
        if (vehiclesResult.IsFailure) return Result.Failure<IReadOnlyList<VehicleComplianceDueResponse>>(vehiclesResult.Error);
        var result = new List<VehicleComplianceDueResponse>();
        foreach (var vehicle in vehiclesResult.Value!.Items)
        {
            AddDue(result, vehicle.Id, vehicle.AssetNumber, "Registration", vehicle.RegistrationExpiryDate, vehicle.RegistrationStatus, checkDate);
            AddDue(result, vehicle.Id, vehicle.AssetNumber, "Insurance", vehicle.InsuranceExpiryDate, vehicle.InsuranceStatus, checkDate);
            AddDue(result, vehicle.Id, vehicle.AssetNumber, "Inspection", vehicle.InspectionExpiryDate, vehicle.InspectionStatus, checkDate);
        }
        return Result.Success<IReadOnlyList<VehicleComplianceDueResponse>>(result.Where(x => x.Status != VehicleComplianceDueStatus.Valid).OrderBy(x => x.ExpiryDate).ToArray());
    }

    public async Task<Result<PagedResponse<VehicleIssueSummaryResponse>>> GetIssuesAsync(Guid? vehicleId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        if (vehicleId.HasValue)
        {
            var access = await GetAccessibleVehicleAsync(vehicleId.Value, PermissionKeys.Fleet.IssuesRead, cancellationToken);
            if (access.IsFailure) return Result.Failure<PagedResponse<VehicleIssueSummaryResponse>>(access.Error);
        }
        var accessible = await support.AccessibleLocationIdsAsync(PermissionKeys.Fleet.IssuesRead, cancellationToken);
        var global = await support.HasPermissionAsync(PermissionKeys.Fleet.IssuesRead, null, cancellationToken);
        var vehicleIds = dbContext.Vehicles.AsNoTracking().Where(v => global || v.CurrentLocationId != null && accessible.Contains(v.CurrentLocationId.Value)).Select(v => v.Id);
        var query = dbContext.VehicleIssues.AsNoTracking().Where(x => vehicleIds.Contains(x.VehicleId));
        if (vehicleId.HasValue) query = query.Where(x => x.VehicleId == vehicleId);
        if (Enum.TryParse<VehicleIssueStatus>(status, true, out var parsed)) query = query.Where(x => x.Status == parsed);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.ReportedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        return Result.Success(new PagedResponse<VehicleIssueSummaryResponse>(items.Select(MapIssue).ToArray(), page, pageSize, count));
    }

    public async Task<Result<VehicleIssueSummaryResponse>> CreateIssueAsync(CreateVehicleIssueRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.IdempotencyRequired);
        var hash = FleetServiceSupport.HashRequest(request);
        var receipt = await dbContext.FleetCommandReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.CommandName == "create-issue" && x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (receipt is not null)
        {
            if (receipt.RequestHash != hash) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.IdempotencyConflict);
            var existing = await dbContext.VehicleIssues.AsNoTracking().SingleAsync(x => x.Id == receipt.ResultEntityId, cancellationToken);
            return Result.Success(MapIssue(existing));
        }
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == request.VehicleId, cancellationToken);
        if (vehicle is null) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.NotFound);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.IssuesManage, cancellationToken)) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(request.Description) || request.OdometerAtReport < 0) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.InvalidRequest);
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.CurrentUserUnavailable);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var assignment = await dbContext.RiderVehicleAssignments.SingleOrDefaultAsync(x => x.VehicleId == vehicle.Id && x.EndedAtUtc == null, cancellationToken);
        var issue = new VehicleIssue { IssueNumber = FleetServiceSupport.NewNumber("ISS", support.UtcNow, Guid.CreateVersion7()), VehicleId = vehicle.Id, Category = request.Category, Severity = request.Severity, Description = request.Description.Trim(), ReportedAtUtc = request.ReportedAtUtc, LocationId = request.LocationId ?? vehicle.CurrentLocationId, OdometerAtReport = request.OdometerAtReport, RelatedAssignmentId = assignment?.Id, BlocksOperation = request.BlocksOperation, ReportedByUserId = actor.Value };
        dbContext.VehicleIssues.Add(issue);
        dbContext.VehicleIssueEvents.Add(NewIssueEvent(issue.Id, VehicleIssueEventType.Reported, null, VehicleIssueStatus.Open, request.ReportedAtUtc, actor.Value, request.Description));
        if (request.BlocksOperation)
        {
            await EndActiveAssignmentForHoldAsync(vehicle, request.ReportedAtUtc, request.Description, actor.Value, cancellationToken);
            await SetStatusAsync(vehicle, VehicleOperationalStatus.ProblemHold, request.ReportedAtUtc, request.Description, VehicleStatusSourceType.Issue, issue.Id, actor.Value, cancellationToken);
        }
        dbContext.FleetCommandReceipts.Add(new FleetCommandReceipt { CommandName = "create-issue", IdempotencyKey = idempotencyKey.Trim(), RequestHash = hash, ResultEntityId = issue.Id });
        await dbContext.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken);
        return Result.Success(MapIssue(issue));
    }

    public async Task<Result<VehicleIssueSummaryResponse>> ActOnIssueAsync(Guid issueId, string action, VehicleIssueActionRequest request, CancellationToken cancellationToken = default)
    {
        var issue = await dbContext.VehicleIssues.SingleOrDefaultAsync(x => x.Id == issueId, cancellationToken);
        if (issue is null) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.NotFound);
        var vehicle = await dbContext.Vehicles.SingleAsync(x => x.Id == issue.VehicleId, cancellationToken);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.IssuesManage, cancellationToken)) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.Forbidden);
        if (!FleetServiceSupport.MatchesRowVersion(issue.RowVersion, request.RowVersion) || string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.ConcurrencyConflict);
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.CurrentUserUnavailable);
        var from = issue.Status;
        var eventType = VehicleIssueEventType.Corrected;
        switch (action.ToLowerInvariant())
        {
            case "review" when issue.Status == VehicleIssueStatus.Open: issue.Status = VehicleIssueStatus.UnderReview; issue.ReviewedByUserId = actor; eventType = VehicleIssueEventType.ReviewStarted; break;
            case "close" when issue.Status is VehicleIssueStatus.Resolved or VehicleIssueStatus.Rejected: issue.Status = VehicleIssueStatus.Closed; issue.ClosedAtUtc = support.UtcNow; issue.ClosedByUserId = actor; eventType = VehicleIssueEventType.Closed; break;
            case "reject" when issue.Status is VehicleIssueStatus.Open or VehicleIssueStatus.UnderReview: issue.Status = VehicleIssueStatus.Rejected; eventType = VehicleIssueEventType.Rejected; break;
            default: return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.InvalidState);
        }
        dbContext.VehicleIssueEvents.Add(NewIssueEvent(issue.Id, eventType, from, issue.Status, support.UtcNow, actor.Value, request.Reason));
        if (eventType == VehicleIssueEventType.Rejected && issue.BlocksOperation) await RestoreAfterIssueAsync(vehicle, issue.Id, request.Reason, actor.Value, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(MapIssue(issue));
    }

    public async Task<Result<VehicleIssueSummaryResponse>> ResolveIssueAsync(Guid issueId, ResolveVehicleIssueRequest request, CancellationToken cancellationToken = default)
    {
        var issue = await dbContext.VehicleIssues.SingleOrDefaultAsync(x => x.Id == issueId, cancellationToken);
        if (issue is null) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.NotFound);
        var vehicle = await dbContext.Vehicles.SingleAsync(x => x.Id == issue.VehicleId, cancellationToken);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.IssuesManage, cancellationToken)) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.Forbidden);
        if (!FleetServiceSupport.MatchesRowVersion(issue.RowVersion, request.RowVersion) || string.IsNullOrWhiteSpace(request.ResolutionSummary) || issue.Status is not (VehicleIssueStatus.Open or VehicleIssueStatus.UnderReview)) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.InvalidState);
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<VehicleIssueSummaryResponse>(FleetErrors.CurrentUserUnavailable);
        var from = issue.Status; issue.Status = VehicleIssueStatus.Resolved; issue.ResolutionSummary = request.ResolutionSummary.Trim(); issue.ResolvedAtUtc = support.UtcNow; issue.ResolvedByUserId = actor;
        dbContext.VehicleIssueEvents.Add(NewIssueEvent(issue.Id, VehicleIssueEventType.Resolved, from, issue.Status, support.UtcNow, actor.Value, request.ResolutionSummary));
        if (issue.BlocksOperation) await RestoreAfterIssueAsync(vehicle, issue.Id, request.ResolutionSummary, actor.Value, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(MapIssue(issue));
    }

    private async Task<Result<Vehicle>> GetAccessibleVehicleAsync(Guid id, string permission, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (vehicle is null) return Result.Failure<Vehicle>(FleetErrors.NotFound);
        return await support.HasVehiclePermissionAsync(vehicle, permission, cancellationToken) ? Result.Success(vehicle) : Result.Failure<Vehicle>(FleetErrors.Forbidden);
    }

    private async Task<Result<RiderVehicleAssignmentResponse>?> ReplayAssignmentAsync(string command, string key, string hash, CancellationToken cancellationToken)
    {
        var receipt = await dbContext.FleetCommandReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.CommandName == command && x.IdempotencyKey == key, cancellationToken);
        if (receipt is null) return null;
        if (receipt.RequestHash != hash) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.IdempotencyConflict);
        var assignment = await dbContext.RiderVehicleAssignments.AsNoTracking().SingleAsync(x => x.Id == receipt.ResultEntityId, cancellationToken);
        return Result.Success(await MapAssignmentAsync(assignment, cancellationToken));
    }

    private async Task<RiderVehicleAssignmentResponse> MapAssignmentAsync(RiderVehicleAssignment item, CancellationToken cancellationToken)
    {
        var info = await (from v in dbContext.Vehicles.AsNoTracking() join r in dbContext.RiderProfiles.AsNoTracking() on item.RiderProfileId equals r.Id join e in dbContext.Employees.AsNoTracking() on r.EmployeeId equals e.Id where v.Id == item.VehicleId select new { v.AssetNumber, RiderName = e.FullNameAr }).SingleAsync(cancellationToken);
        var employeeId = await dbContext.RiderProfiles.AsNoTracking().Where(x => x.Id == item.RiderProfileId).Select(x => x.EmployeeId).SingleAsync(cancellationToken);
        return new RiderVehicleAssignmentResponse(item.Id, item.RiderProfileId, employeeId, item.VehicleId, info.AssetNumber, info.RiderName, item.StartedAtUtc, item.EndedAtUtc, item.StartLocationId, item.EndLocationId, item.StartOdometer, item.EndOdometer, item.PermissionStartsOn, item.PermissionEndsOn, item.Status, item.AssignmentReason, item.CompletionReason, item.OperationId, FleetServiceSupport.EncodeRowVersion(item.RowVersion));
    }

    private async Task<VehicleSummaryResponse[]> BuildSummariesAsync(Vehicle[] vehicles, CancellationToken cancellationToken)
    {
        if (vehicles.Length == 0) return [];
        var ids = vehicles.Select(x => x.Id).ToArray();
        var manufacturers = await dbContext.VehicleManufacturers.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var models = await dbContext.VehicleModels.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var locations = await dbContext.FleetLocations.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var assignments = await (from a in dbContext.RiderVehicleAssignments.AsNoTracking() join r in dbContext.RiderProfiles.AsNoTracking() on a.RiderProfileId equals r.Id join e in dbContext.Employees.AsNoTracking() on r.EmployeeId equals e.Id where ids.Contains(a.VehicleId) && a.EndedAtUtc == null select new { a.VehicleId, a.Id, a.RiderProfileId, e.FullNameAr }).ToDictionaryAsync(x => x.VehicleId, cancellationToken);
        var registrations = await dbContext.VehicleRegistrations.AsNoTracking().Where(x => ids.Contains(x.VehicleId) && x.IsCurrent).ToDictionaryAsync(x => x.VehicleId, cancellationToken);
        var insurance = await dbContext.VehicleInsurancePolicies.AsNoTracking().Where(x => ids.Contains(x.VehicleId) && x.IsCurrent).ToDictionaryAsync(x => x.VehicleId, cancellationToken);
        var inspections = await dbContext.VehiclePeriodicInspections.AsNoTracking().Where(x => ids.Contains(x.VehicleId) && x.IsCurrent).ToDictionaryAsync(x => x.VehicleId, cancellationToken);
        var check = DateOnly.FromDateTime(support.UtcNow.UtcDateTime);
        return vehicles.Select(v =>
        {
            assignments.TryGetValue(v.Id, out var a); registrations.TryGetValue(v.Id, out var reg); insurance.TryGetValue(v.Id, out var ins); inspections.TryGetValue(v.Id, out var chk);
            return new VehicleSummaryResponse(v.Id, v.AssetNumber, v.PlateNumberAr, v.PlateNumberEn, manufacturers[v.VehicleManufacturerId].NameEn, models[v.VehicleModelId].NameEn, v.VehicleType, v.CurrentOperationalStatus, v.CurrentLocationId, v.CurrentLocationId.HasValue && locations.TryGetValue(v.CurrentLocationId.Value, out var l) ? l.NameEn : null, v.CurrentOdometer, a?.Id, a?.RiderProfileId, a?.FullNameAr, reg?.ExpiryDate, FleetServiceSupport.DueStatus(reg?.ExpiryDate, check), ins?.ExpiryDate, FleetServiceSupport.DueStatus(ins?.ExpiryDate, check), chk?.ExpiryDate, FleetServiceSupport.DueStatus(chk?.ExpiryDate, check), FleetServiceSupport.EncodeRowVersion(v.RowVersion));
        }).ToArray();
    }

    private async Task<VehicleDetailResponse> BuildDetailAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        var summary = (await BuildSummariesAsync([vehicle], cancellationToken))[0];
        return new VehicleDetailResponse(summary, vehicle.Vin, vehicle.ChassisNumber, vehicle.EngineNumber, vehicle.VehicleManufacturerId, vehicle.VehicleModelId, vehicle.ModelYear, vehicle.FuelType, vehicle.TransmissionType, vehicle.ColorAr, vehicle.ColorEn, vehicle.OwnershipType, vehicle.OwnerName, vehicle.AcquisitionDate, vehicle.LeaseReference, vehicle.DecommissionedAtUtc, vehicle.DecommissionReason, vehicle.Notes);
    }

    private static void ApplyVehicle(Vehicle v, VehicleUpsertRequest r, string normalizedAsset, string? normalizedAr, string? normalizedEn)
    {
        v.AssetNumber = r.AssetNumber.Trim(); v.NormalizedAssetNumber = normalizedAsset; v.PlateNumberAr = FleetServiceSupport.TrimOrNull(r.PlateNumberAr); v.NormalizedPlateNumberAr = normalizedAr; v.PlateNumberEn = FleetServiceSupport.TrimOrNull(r.PlateNumberEn); v.NormalizedPlateNumberEn = normalizedEn;
        v.PlateLettersAr = FleetServiceSupport.TrimOrNull(r.PlateLettersAr); v.PlateLettersEn = FleetServiceSupport.TrimOrNull(r.PlateLettersEn); v.PlateDigits = FleetServiceSupport.TrimOrNull(r.PlateDigits); v.Vin = FleetServiceSupport.TrimOrNull(r.Vin)?.ToUpperInvariant(); v.ChassisNumber = FleetServiceSupport.TrimOrNull(r.ChassisNumber); v.EngineNumber = FleetServiceSupport.TrimOrNull(r.EngineNumber);
        v.VehicleManufacturerId = r.VehicleManufacturerId; v.VehicleModelId = r.VehicleModelId; v.ModelYear = r.ModelYear; v.VehicleType = r.VehicleType; v.FuelType = r.FuelType; v.TransmissionType = r.TransmissionType; v.ColorAr = FleetServiceSupport.TrimOrNull(r.ColorAr); v.ColorEn = FleetServiceSupport.TrimOrNull(r.ColorEn); v.OwnershipType = r.OwnershipType; v.OwnerName = FleetServiceSupport.TrimOrNull(r.OwnerName); v.AcquisitionDate = r.AcquisitionDate; v.LeaseReference = FleetServiceSupport.TrimOrNull(r.LeaseReference); v.CurrentLocationId = r.CurrentLocationId; v.CurrentOdometer = r.CurrentOdometer; v.Notes = FleetServiceSupport.TrimOrNull(r.Notes);
    }

    private async Task SetStatusAsync(Vehicle vehicle, VehicleOperationalStatus target, DateTimeOffset at, string reason, VehicleStatusSourceType source, Guid? sourceId, Guid actor, CancellationToken cancellationToken)
    {
        await CloseCurrentStatusAsync(vehicle.Id, at, cancellationToken);
        vehicle.CurrentOperationalStatus = target;
        dbContext.VehicleOperationalStatusPeriods.Add(NewStatus(vehicle.Id, target, at, reason, source, sourceId, actor));
    }

    private async Task CloseCurrentStatusAsync(Guid vehicleId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var current = await dbContext.VehicleOperationalStatusPeriods.SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.EffectiveToUtc == null, cancellationToken);
        if (current is not null) current.EffectiveToUtc = at < current.EffectiveFromUtc ? current.EffectiveFromUtc : at;
    }

    private static VehicleOperationalStatusPeriod NewStatus(Guid vehicleId, VehicleOperationalStatus status, DateTimeOffset at, string reason, VehicleStatusSourceType source, Guid? sourceId, Guid actor) => new() { VehicleId = vehicleId, Status = status, EffectiveFromUtc = at, Reason = reason.Trim(), SourceType = source, SourceEntityId = sourceId, ChangedByUserId = actor };
    private static VehicleOdometerReading NewOdometer(Guid vehicleId, long reading, DateTimeOffset at, VehicleOdometerSourceType source, Guid? sourceId, string? notes) => new() { VehicleId = vehicleId, Reading = reading, RecordedAtUtc = at, SourceType = source, SourceEntityId = sourceId, Notes = FleetServiceSupport.TrimOrNull(notes) };
    private static VehicleOdometerReadingResponse MapOdometer(VehicleOdometerReading x) => new(x.Id, x.Reading, x.RecordedAtUtc, x.SourceType, x.IsCorrection, x.CorrectionReason, x.Notes);
    private static RiderVehicleAssignmentEvent NewAssignmentEvent(Guid assignmentId, Guid operationId, RiderVehicleAssignmentEventType type, DateTimeOffset at, Guid actor, string reason) => new() { RiderVehicleAssignmentId = assignmentId, OperationId = operationId, EventType = type, OccurredAtUtc = at, ActorUserId = actor, Reason = reason.Trim() };
    private static VehicleIssueEvent NewIssueEvent(Guid issueId, VehicleIssueEventType type, VehicleIssueStatus? from, VehicleIssueStatus to, DateTimeOffset at, Guid actor, string reason) => new() { VehicleIssueId = issueId, EventType = type, FromStatus = from, ToStatus = to, OccurredAtUtc = at, ActorUserId = actor, Reason = reason.Trim() };
    private static VehicleIssueSummaryResponse MapIssue(VehicleIssue x) => new(x.Id, x.IssueNumber, x.VehicleId, x.Category, x.Severity, x.BlocksOperation, x.Status, x.ReportedAtUtc, x.Description, x.ResolutionSummary, FleetServiceSupport.EncodeRowVersion(x.RowVersion));
    private static VehicleAccidentSummaryResponse MapAccident(VehicleAccident x) => new(x.Id, x.AccidentNumber, x.VehicleId, x.RiderProfileId, x.RiderVehicleAssignmentId, x.VehicleIssueId, x.OccurredAtUtc, x.Severity, x.IsDrivable, x.Status, x.LocationDescription, FleetServiceSupport.EncodeRowVersion(x.RowVersion));

    private void EndAssignment(RiderVehicleAssignment assignment, Vehicle vehicle, DateTimeOffset endedAt, Guid? locationId, long odometer, VehicleCondition condition, byte? fuel, string reason, Guid actor, RiderVehicleAssignmentEventType eventType)
    {
        assignment.EndedAtUtc = endedAt; assignment.EndLocationId = locationId ?? vehicle.CurrentLocationId; assignment.EndOdometer = odometer; assignment.EndVehicleCondition = condition; assignment.EndFuelLevelPercentage = fuel; assignment.Status = RiderVehicleAssignmentStatus.Completed; assignment.CompletionReason = reason.Trim(); assignment.EndedByUserId = actor;
        vehicle.CurrentAssignmentId = null; vehicle.CurrentLocationId = locationId ?? vehicle.CurrentLocationId; vehicle.CurrentOdometer = Math.Max(vehicle.CurrentOdometer, odometer); vehicle.LastOdometerAtUtc = endedAt;
        dbContext.RiderVehicleAssignmentEvents.Add(NewAssignmentEvent(assignment.Id, assignment.OperationId, eventType, endedAt, actor, reason));
        dbContext.VehicleOdometerReadings.Add(NewOdometer(vehicle.Id, odometer, endedAt, VehicleOdometerSourceType.AssignmentReturn, assignment.Id, reason));
    }

    private async Task EndActiveAssignmentForHoldAsync(Vehicle vehicle, DateTimeOffset at, string reason, Guid actor, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.RiderVehicleAssignments.SingleOrDefaultAsync(x => x.VehicleId == vehicle.Id && x.EndedAtUtc == null, cancellationToken);
        if (assignment is not null) EndAssignment(assignment, vehicle, at, vehicle.CurrentLocationId, vehicle.CurrentOdometer, VehicleCondition.Damaged, null, reason, actor, RiderVehicleAssignmentEventType.Returned);
    }

    private async Task<bool> HasBlockingIssueAsync(Guid vehicleId, Guid? excludedIssueId, CancellationToken cancellationToken) => await dbContext.VehicleIssues.AnyAsync(x => x.VehicleId == vehicleId && x.Id != excludedIssueId && x.BlocksOperation && (x.Status == VehicleIssueStatus.Open || x.Status == VehicleIssueStatus.UnderReview), cancellationToken);
    private async Task<VehicleOperationalStatus> ResolveAvailableStatusAsync(Guid vehicleId, Guid? excludedIssueId, CancellationToken cancellationToken)
    {
        var accident = await dbContext.VehicleIssues.AnyAsync(x => x.VehicleId == vehicleId && x.Id != excludedIssueId && x.BlocksOperation && x.Category == VehicleIssueCategory.Accident && (x.Status == VehicleIssueStatus.Open || x.Status == VehicleIssueStatus.UnderReview), cancellationToken);
        if (accident) return VehicleOperationalStatus.AccidentHold;
        return await HasBlockingIssueAsync(vehicleId, excludedIssueId, cancellationToken) ? VehicleOperationalStatus.ProblemHold : VehicleOperationalStatus.Available;
    }

    private async Task RestoreAfterIssueAsync(Vehicle vehicle, Guid issueId, string reason, Guid actor, CancellationToken cancellationToken)
    {
        if (vehicle.CurrentOperationalStatus is VehicleOperationalStatus.Stolen or VehicleOperationalStatus.OutOfService or VehicleOperationalStatus.Decommissioned) return;
        var target = await ResolveAvailableStatusAsync(vehicle.Id, issueId, cancellationToken);
        if (target == VehicleOperationalStatus.Available && await dbContext.RiderVehicleAssignments.AnyAsync(x => x.VehicleId == vehicle.Id && x.EndedAtUtc == null, cancellationToken)) target = VehicleOperationalStatus.Assigned;
        await SetStatusAsync(vehicle, target, support.UtcNow, reason, VehicleStatusSourceType.Issue, issueId, actor, cancellationToken);
    }

    private static bool ValidFuel(byte? value) => value is null or <= 100;
    private static bool ValidPermission(DateOnly? start, DateOnly? end) => !start.HasValue || !end.HasValue || end >= start;
    private static (int Page, int PageSize) NormalizePage(int page, int pageSize) => (Math.Max(1, page), Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 200));

    private static VehicleComplianceResponse MapCompliance(VehicleRegistration x) => new(x.Id, x.VehicleId, "Registration", x.RegistrationNumber, x.IssuingAuthority, x.IssueDate, x.ExpiryDate, FleetServiceSupport.DueStatus(x.ExpiryDate, DateOnly.FromDateTime(DateTime.UtcNow)), x.IsCurrent, x.PreviousRecordId, FleetServiceSupport.EncodeRowVersion(x.RowVersion));
    private static VehicleComplianceResponse MapCompliance(VehicleInsurancePolicy x) => new(x.Id, x.VehicleId, "Insurance", x.PolicyNumber, x.ProviderName, x.EffectiveFrom, x.ExpiryDate, FleetServiceSupport.DueStatus(x.ExpiryDate, DateOnly.FromDateTime(DateTime.UtcNow)), x.IsCurrent, x.PreviousRecordId, FleetServiceSupport.EncodeRowVersion(x.RowVersion));
    private static VehicleComplianceResponse MapCompliance(VehiclePeriodicInspection x) => new(x.Id, x.VehicleId, "Inspection", x.InspectionNumber, x.StationName, x.InspectionDate, x.ExpiryDate, FleetServiceSupport.DueStatus(x.ExpiryDate, DateOnly.FromDateTime(DateTime.UtcNow)), x.IsCurrent, x.PreviousRecordId, FleetServiceSupport.EncodeRowVersion(x.RowVersion));
    private static void AddDue(List<VehicleComplianceDueResponse> result, Guid vehicleId, string asset, string type, DateOnly? expiry, VehicleComplianceDueStatus ignored, DateOnly check)
    {
        var status = FleetServiceSupport.DueStatus(expiry, check);
        result.Add(new VehicleComplianceDueResponse(vehicleId, asset, type, null, expiry, status, expiry.HasValue ? expiry.Value.DayNumber - check.DayNumber : null));
    }
}
