using System.Text.Json;
using LogisticsERP.Application.Abstractions.Files;
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
    FleetServiceSupport support,
    IPrivateFileStorage fileStorage) : IFleetService
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

    public async Task<Result<IReadOnlyList<VehicleSupplierResponse>>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesRead, null, cancellationToken)) return Result.Failure<IReadOnlyList<VehicleSupplierResponse>>(FleetErrors.Forbidden);
        var items = await dbContext.VehicleSuppliers.AsNoTracking().OrderBy(x => x.NameEn).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<VehicleSupplierResponse>>(items.Select(MapSupplier).ToArray());
    }

    public async Task<Result<VehicleSupplierResponse>> GetSupplierAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesRead, null, cancellationToken)) return Result.Failure<VehicleSupplierResponse>(FleetErrors.Forbidden);
        var item = await dbContext.VehicleSuppliers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return item is null ? Result.Failure<VehicleSupplierResponse>(FleetErrors.NotFound) : Result.Success(MapSupplier(item));
    }

    public async Task<Result<VehicleSupplierResponse>> UpsertSupplierAsync(Guid? id, VehicleSupplierRequest request, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesManage, null, cancellationToken)) return Result.Failure<VehicleSupplierResponse>(FleetErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NameAr) || string.IsNullOrWhiteSpace(request.NameEn) || request.Address is null) return Result.Failure<VehicleSupplierResponse>(FleetErrors.InvalidRequest);
        var code = FleetServiceSupport.NormalizeIdentifier(request.Code);
        var cr = FleetServiceSupport.TrimOrNull(request.CommercialRegistrationNumber);
        var tax = FleetServiceSupport.TrimOrNull(request.TaxNumber);
        var item = id.HasValue ? await dbContext.VehicleSuppliers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) : null;
        if (id.HasValue && item is null) return Result.Failure<VehicleSupplierResponse>(FleetErrors.NotFound);
        if (item is not null && !FleetServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion)) return Result.Failure<VehicleSupplierResponse>(FleetErrors.ConcurrencyConflict);
        if (await dbContext.VehicleSuppliers.AnyAsync(x => x.Id != id && (x.Code == code || cr != null && x.CommercialRegistrationNumber == cr || tax != null && x.TaxNumber == tax), cancellationToken)) return Result.Failure<VehicleSupplierResponse>(FleetErrors.Duplicate);
        item ??= new VehicleSupplier();
        item.Code = code; item.NameAr = request.NameAr.Trim(); item.NameEn = request.NameEn.Trim(); item.CommercialRegistrationNumber = cr; item.TaxNumber = tax;
        item.Phone = FleetServiceSupport.TrimOrNull(request.Phone); item.Status = request.Status; item.Notes = FleetServiceSupport.TrimOrNull(request.Notes);
        item.Address.BuildingNumber = FleetServiceSupport.TrimOrNull(request.Address.BuildingNumber); item.Address.Street = FleetServiceSupport.TrimOrNull(request.Address.Street);
        item.Address.District = FleetServiceSupport.TrimOrNull(request.Address.District); item.Address.City = FleetServiceSupport.TrimOrNull(request.Address.City);
        item.Address.PostalCode = FleetServiceSupport.TrimOrNull(request.Address.PostalCode); item.Address.AdditionalNumber = FleetServiceSupport.TrimOrNull(request.Address.AdditionalNumber);
        if (!id.HasValue) dbContext.VehicleSuppliers.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(MapSupplier(item));
    }

    public async Task<Result> ArchiveSupplierAsync(Guid id, ArchiveFleetRequest request, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesManage, null, cancellationToken)) return Result.Failure(FleetErrors.Forbidden);
        var item = await dbContext.VehicleSuppliers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return Result.Failure(FleetErrors.NotFound);
        if (!FleetServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion) || string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure(FleetErrors.ConcurrencyConflict);
        item.IsDeleted = true; item.DeletionReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PagedResponse<VehicleSummaryResponse>>> GetVehiclesAsync(string? search, string? status, Guid? operatingCityId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePage(page, pageSize);
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesRead, null, cancellationToken)) return Result.Failure<PagedResponse<VehicleSummaryResponse>>(FleetErrors.Forbidden);
        var query = dbContext.Vehicles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = FleetServiceSupport.NormalizeIdentifier(search);
            query = query.Where(x => x.NormalizedAssetNumber.Contains(normalized) || x.NormalizedPlateNumberAr != null && x.NormalizedPlateNumberAr.Contains(normalized) || x.NormalizedPlateNumberEn != null && x.NormalizedPlateNumberEn.Contains(normalized));
        }
        if (Enum.TryParse<VehicleOperationalStatus>(status, true, out var parsedStatus)) query = query.Where(x => x.CurrentOperationalStatus == parsedStatus);
        if (operatingCityId.HasValue) query = query.Where(x => x.OperatingCityId == operatingCityId);
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
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.VehiclesManage, null, cancellationToken)) return Result.Failure<VehicleDetailResponse>(FleetErrors.Forbidden);
        var assetNumber = string.IsNullOrWhiteSpace(request.AssetNumber) && !id.HasValue
            ? FleetServiceSupport.NewVehicleAssetNumber(Guid.CreateVersion7())
            : request.AssetNumber;
        if (string.IsNullOrWhiteSpace(assetNumber) || request.CurrentOdometer < 0 || request.ModelYear is < 1950 or > 2200) return Result.Failure<VehicleDetailResponse>(FleetErrors.InvalidRequest);
        var modelValid = await dbContext.VehicleModels.AnyAsync(x => x.Id == request.VehicleModelId && x.VehicleManufacturerId == request.VehicleManufacturerId, cancellationToken);
        var sponsorValid = request.SponsorId.HasValue && await dbContext.Sponsors.AnyAsync(x => x.Id == request.SponsorId, cancellationToken);
        var cityValid = request.OperatingCityId.HasValue && await dbContext.OperatingCities.AnyAsync(x => x.Id == request.OperatingCityId, cancellationToken);
        var supplierValid = !request.PurchasedFromSupplierId.HasValue || await dbContext.VehicleSuppliers.AnyAsync(x => x.Id == request.PurchasedFromSupplierId && x.Status == VehicleCatalogStatus.Active, cancellationToken);
        if (!modelValid || !sponsorValid || !cityValid || !supplierValid) return Result.Failure<VehicleDetailResponse>(FleetErrors.NotFound);
        if (request.OwnershipType == VehicleOwnershipType.Owned && !request.PurchasedFromSupplierId.HasValue || request.RegistrationType.HasValue && !Enum.IsDefined(request.RegistrationType.Value)) return Result.Failure<VehicleDetailResponse>(FleetErrors.InvalidRequest);
        var normalizedAsset = FleetServiceSupport.NormalizeIdentifier(assetNumber);
        var normalizedSerial = string.IsNullOrWhiteSpace(request.SerialNumber) ? null : FleetServiceSupport.NormalizeIdentifier(request.SerialNumber);
        var normalizedChassis = string.IsNullOrWhiteSpace(request.ChassisNumber) ? null : FleetServiceSupport.NormalizeIdentifier(request.ChassisNumber);
        var normalizedAr = string.IsNullOrWhiteSpace(request.PlateNumberAr) ? null : FleetServiceSupport.NormalizeIdentifier(request.PlateNumberAr);
        var normalizedEn = string.IsNullOrWhiteSpace(request.PlateNumberEn) ? null : FleetServiceSupport.NormalizeIdentifier(request.PlateNumberEn);
        var normalizedVin = FleetServiceSupport.TrimOrNull(request.Vin)?.ToUpperInvariant();
        if (await dbContext.Vehicles.AnyAsync(x => x.Id != id && (x.NormalizedAssetNumber == normalizedAsset || normalizedSerial != null && x.NormalizedSerialNumber == normalizedSerial || normalizedChassis != null && x.NormalizedChassisNumber == normalizedChassis || normalizedAr != null && x.NormalizedPlateNumberAr == normalizedAr || normalizedEn != null && x.NormalizedPlateNumberEn == normalizedEn || normalizedVin != null && x.Vin == normalizedVin), cancellationToken)) return Result.Failure<VehicleDetailResponse>(FleetErrors.Duplicate);
        var vehicle = id.HasValue ? await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) : null;
        if (id.HasValue && vehicle is null) return Result.Failure<VehicleDetailResponse>(FleetErrors.NotFound);
        if (vehicle is not null && !FleetServiceSupport.MatchesRowVersion(vehicle.RowVersion, request.RowVersion)) return Result.Failure<VehicleDetailResponse>(FleetErrors.ConcurrencyConflict);
        if (vehicle is not null &&
            (!string.Equals(vehicle.SerialNumber, FleetServiceSupport.TrimOrNull(request.SerialNumber), StringComparison.Ordinal)
             || !string.Equals(vehicle.ChassisNumber, FleetServiceSupport.TrimOrNull(request.ChassisNumber), StringComparison.Ordinal)
             || !string.Equals(vehicle.PlateNumberAr, FleetServiceSupport.TrimOrNull(request.PlateNumberAr), StringComparison.Ordinal)
             || !string.Equals(vehicle.PlateNumberEn, FleetServiceSupport.TrimOrNull(request.PlateNumberEn), StringComparison.Ordinal)
             || !string.Equals(vehicle.PlateLettersAr, FleetServiceSupport.TrimOrNull(request.PlateLettersAr), StringComparison.Ordinal)
             || !string.Equals(vehicle.PlateLettersEn, FleetServiceSupport.TrimOrNull(request.PlateLettersEn), StringComparison.Ordinal)
             || !string.Equals(vehicle.PlateDigits, FleetServiceSupport.TrimOrNull(request.PlateDigits), StringComparison.Ordinal)
             || vehicle.RegistrationType != request.RegistrationType)) return Result.Failure<VehicleDetailResponse>(FleetErrors.InvalidState);
        if (vehicle is null && (normalizedSerial is null || normalizedChassis is null || normalizedAr is null || normalizedEn is null || !request.RegistrationType.HasValue)) return Result.Failure<VehicleDetailResponse>(FleetErrors.InvalidRequest);
        if (vehicle is not null && request.CurrentOdometer < vehicle.CurrentOdometer) return Result.Failure<VehicleDetailResponse>(FleetErrors.OdometerDecreased);
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<VehicleDetailResponse>(FleetErrors.CurrentUserUnavailable);
        var isNew = vehicle is null;
        vehicle ??= new Vehicle();
        ApplyVehicle(vehicle, request with { AssetNumber = assetNumber }, normalizedAsset, normalizedSerial, normalizedChassis, normalizedAr, normalizedEn);
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

    public async Task<Result<VehicleReadinessResponse>> GetReadinessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(id, PermissionKeys.Fleet.VehiclesRead, cancellationToken);
        return access.IsFailure
            ? Result.Failure<VehicleReadinessResponse>(access.Error)
            : Result.Success(await BuildReadinessAsync(access.Value!, cancellationToken));
    }

    public async Task<Result<VehicleDetailResponse>> CorrectIdentityAsync(Guid id, VehicleIdentityCorrectionRequest request, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.CorrectionsManage, null, cancellationToken)) return Result.Failure<VehicleDetailResponse>(FleetErrors.Forbidden);
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (vehicle is null) return Result.Failure<VehicleDetailResponse>(FleetErrors.NotFound);
        if (!FleetServiceSupport.MatchesRowVersion(vehicle.RowVersion, request.RowVersion)) return Result.Failure<VehicleDetailResponse>(FleetErrors.ConcurrencyConflict);
        if (string.IsNullOrWhiteSpace(request.AssetNumber) || string.IsNullOrWhiteSpace(request.SerialNumber) || string.IsNullOrWhiteSpace(request.ChassisNumber) || string.IsNullOrWhiteSpace(request.PlateNumberAr) || string.IsNullOrWhiteSpace(request.PlateNumberEn) || string.IsNullOrWhiteSpace(request.Reason) || !Enum.IsDefined(request.RegistrationType)) return Result.Failure<VehicleDetailResponse>(FleetErrors.InvalidRequest);
        if (vehicle.OwnershipType == VehicleOwnershipType.Owned && !request.PurchasedFromSupplierId.HasValue) return Result.Failure<VehicleDetailResponse>(FleetErrors.InvalidRequest);
        if (vehicle.RegistrationType == VehicleRegistrationType.PrivateTransport && request.RegistrationType == VehicleRegistrationType.PublicTransport) return Result.Failure<VehicleDetailResponse>(FleetErrors.InvalidState);
        var referencesValid = request.DocumentVersionReferences is null || await dbContext.VehicleAttachmentVersions.CountAsync(x => request.DocumentVersionReferences.Contains(x.Id) && dbContext.VehicleAttachments.Any(a => a.Id == x.VehicleAttachmentId && a.VehicleId == id), cancellationToken) == request.DocumentVersionReferences.Distinct().Count();
        var relationsValid = await dbContext.Sponsors.AnyAsync(x => x.Id == request.SponsorId, cancellationToken)
            && await dbContext.OperatingCities.AnyAsync(x => x.Id == request.OperatingCityId, cancellationToken)
            && (!request.PurchasedFromSupplierId.HasValue || await dbContext.VehicleSuppliers.AnyAsync(x => x.Id == request.PurchasedFromSupplierId, cancellationToken));
        if (!relationsValid || !referencesValid) return Result.Failure<VehicleDetailResponse>(FleetErrors.NotFound);
        var normalizedAsset = FleetServiceSupport.NormalizeIdentifier(request.AssetNumber);
        var normalizedSerial = FleetServiceSupport.NormalizeIdentifier(request.SerialNumber);
        var normalizedChassis = FleetServiceSupport.NormalizeIdentifier(request.ChassisNumber);
        var normalizedAr = FleetServiceSupport.NormalizeIdentifier(request.PlateNumberAr);
        var normalizedEn = FleetServiceSupport.NormalizeIdentifier(request.PlateNumberEn);
        var vin = FleetServiceSupport.TrimOrNull(request.Vin)?.ToUpperInvariant();
        if (await dbContext.Vehicles.AnyAsync(x => x.Id != id && (x.NormalizedAssetNumber == normalizedAsset || x.NormalizedSerialNumber == normalizedSerial || x.NormalizedChassisNumber == normalizedChassis || x.NormalizedPlateNumberAr == normalizedAr || x.NormalizedPlateNumberEn == normalizedEn || vin != null && x.Vin == vin), cancellationToken)) return Result.Failure<VehicleDetailResponse>(FleetErrors.Duplicate);
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<VehicleDetailResponse>(FleetErrors.CurrentUserUnavailable);
        var before = IdentitySnapshot(vehicle);
        vehicle.AssetNumber = request.AssetNumber.Trim(); vehicle.NormalizedAssetNumber = normalizedAsset;
        vehicle.SerialNumber = request.SerialNumber.Trim(); vehicle.NormalizedSerialNumber = normalizedSerial;
        vehicle.ChassisNumber = request.ChassisNumber.Trim(); vehicle.NormalizedChassisNumber = normalizedChassis; vehicle.Vin = vin;
        vehicle.PlateNumberAr = request.PlateNumberAr.Trim(); vehicle.NormalizedPlateNumberAr = normalizedAr; vehicle.PlateNumberEn = request.PlateNumberEn.Trim(); vehicle.NormalizedPlateNumberEn = normalizedEn;
        vehicle.PlateLettersAr = FleetServiceSupport.TrimOrNull(request.PlateLettersAr); vehicle.PlateLettersEn = FleetServiceSupport.TrimOrNull(request.PlateLettersEn); vehicle.PlateDigits = FleetServiceSupport.TrimOrNull(request.PlateDigits);
        vehicle.SponsorId = request.SponsorId; vehicle.OperatingCityId = request.OperatingCityId; vehicle.PurchasedFromSupplierId = request.PurchasedFromSupplierId; vehicle.RegistrationType = request.RegistrationType;
        dbContext.VehicleIdentityCorrections.Add(new VehicleIdentityCorrection
        {
            VehicleId = vehicle.Id, BeforeJson = JsonSerializer.Serialize(before), AfterJson = JsonSerializer.Serialize(IdentitySnapshot(vehicle)),
            DocumentVersionReferencesJson = request.DocumentVersionReferences is null ? null : JsonSerializer.Serialize(request.DocumentVersionReferences.Distinct()),
            Reason = request.Reason.Trim(), EffectiveAtUtc = request.EffectiveAtUtc, ActorUserId = actor.Value
        });
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Result.Failure<VehicleDetailResponse>(FleetErrors.Duplicate); }
        return Result.Success(await BuildDetailAsync(vehicle, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<VehicleIdentityCorrectionResponse>>> GetIdentityCorrectionHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(id, PermissionKeys.Fleet.VehiclesRead, cancellationToken);
        if (access.IsFailure) return Result.Failure<IReadOnlyList<VehicleIdentityCorrectionResponse>>(access.Error);
        var rows = await dbContext.VehicleIdentityCorrections.AsNoTracking().Where(x => x.VehicleId == id).OrderByDescending(x => x.EffectiveAtUtc).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<VehicleIdentityCorrectionResponse>>(rows.Select(x => new VehicleIdentityCorrectionResponse(x.Id, x.VehicleId, x.BeforeJson, x.AfterJson, x.DocumentVersionReferencesJson, x.Reason, x.EffectiveAtUtc, x.ActorUserId, x.CreatedAtUtc)).ToArray());
    }

    public async Task<Result<VehicleRegistrationTransitionResponse>> TransitionToPublicTransportAsync(Guid id, VehicleRegistrationTransitionRequest request, PrivateFileUpload istimara, PrivateFileUpload operationCard, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.RegistrationTransitionsManage, null, cancellationToken)) return Result.Failure<VehicleRegistrationTransitionResponse>(FleetErrors.Forbidden);
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (vehicle is null) return Result.Failure<VehicleRegistrationTransitionResponse>(FleetErrors.NotFound);
        if (!FleetServiceSupport.MatchesRowVersion(vehicle.RowVersion, request.RowVersion)) return Result.Failure<VehicleRegistrationTransitionResponse>(FleetErrors.ConcurrencyConflict);
        if (vehicle.CurrentAssignmentId.HasValue || vehicle.RegistrationType != VehicleRegistrationType.PrivateTransport) return Result.Failure<VehicleRegistrationTransitionResponse>(FleetErrors.InvalidState);
        if (string.IsNullOrWhiteSpace(vehicle.PlateNumberAr) || string.IsNullOrWhiteSpace(vehicle.PlateNumberEn) || string.IsNullOrWhiteSpace(request.PlateNumberAr) || string.IsNullOrWhiteSpace(request.PlateNumberEn) || string.IsNullOrWhiteSpace(request.Reason) || !IsDocument(istimara) || !IsDocument(operationCard)) return Result.Failure<VehicleRegistrationTransitionResponse>(FleetErrors.InvalidRequest);
        var normalizedAr = FleetServiceSupport.NormalizeIdentifier(request.PlateNumberAr);
        var normalizedEn = FleetServiceSupport.NormalizeIdentifier(request.PlateNumberEn);
        if (vehicle.NormalizedPlateNumberAr == normalizedAr || vehicle.NormalizedPlateNumberEn == normalizedEn) return Result.Failure<VehicleRegistrationTransitionResponse>(FleetErrors.InvalidRequest);
        if (await dbContext.Vehicles.AnyAsync(x => x.Id != id && (x.NormalizedPlateNumberAr == normalizedAr || x.NormalizedPlateNumberEn == normalizedEn), cancellationToken)) return Result.Failure<VehicleRegistrationTransitionResponse>(FleetErrors.Duplicate);
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<VehicleRegistrationTransitionResponse>(FleetErrors.CurrentUserUnavailable);
        var istimaraStaged = await StageVehicleSlotAsync(vehicle.Id, VehicleFileKind.Istimara, istimara, actor.Value, cancellationToken);
        if (istimaraStaged.IsFailure) return Result.Failure<VehicleRegistrationTransitionResponse>(istimaraStaged.Error);
        var operationCardStaged = await StageVehicleSlotAsync(vehicle.Id, VehicleFileKind.OperationCard, operationCard, actor.Value, cancellationToken);
        if (operationCardStaged.IsFailure) { fileStorage.DeleteBestEffort(istimaraStaged.Value!.Stored.StoragePath); return Result.Failure<VehicleRegistrationTransitionResponse>(operationCardStaged.Error); }
        var staged = new[] { istimaraStaged.Value!, operationCardStaged.Value! };
        try
        {
            return await dbContext.ExecuteTransactionAsync(async _ =>
            {
                foreach (var file in staged)
                {
                    if (file.IsNew) dbContext.VehicleAttachments.Add(file.Attachment);
                    dbContext.VehicleAttachmentVersions.Add(file.Version);
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                foreach (var file in staged) file.Attachment.CurrentVersionId = file.Version.Id;
                var transition = new VehicleRegistrationTransition
                {
                    VehicleId = vehicle.Id, FromType = VehicleRegistrationType.PrivateTransport, ToType = VehicleRegistrationType.PublicTransport,
                    OldPlateNumberAr = vehicle.PlateNumberAr, OldPlateNumberEn = vehicle.PlateNumberEn, NewPlateNumberAr = request.PlateNumberAr.Trim(), NewPlateNumberEn = request.PlateNumberEn.Trim(),
                    OldPlateLettersAr = vehicle.PlateLettersAr, OldPlateLettersEn = vehicle.PlateLettersEn, OldPlateDigits = vehicle.PlateDigits,
                    NewPlateLettersAr = FleetServiceSupport.TrimOrNull(request.PlateLettersAr), NewPlateLettersEn = FleetServiceSupport.TrimOrNull(request.PlateLettersEn), NewPlateDigits = FleetServiceSupport.TrimOrNull(request.PlateDigits),
                    EffectiveAtUtc = request.EffectiveAtUtc, Reason = request.Reason.Trim(), IstimaraVersionId = staged[0].Version.Id, OperationCardVersionId = staged[1].Version.Id, ActorUserId = actor.Value
                };
                vehicle.PlateNumberAr = transition.NewPlateNumberAr; vehicle.NormalizedPlateNumberAr = normalizedAr; vehicle.PlateNumberEn = transition.NewPlateNumberEn; vehicle.NormalizedPlateNumberEn = normalizedEn;
                vehicle.PlateLettersAr = transition.NewPlateLettersAr; vehicle.PlateLettersEn = transition.NewPlateLettersEn; vehicle.PlateDigits = transition.NewPlateDigits; vehicle.RegistrationType = VehicleRegistrationType.PublicTransport;
                dbContext.VehicleRegistrationTransitions.Add(transition);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success(MapTransition(transition));
            }, cancellationToken);
        }
        catch (DbUpdateException)
        {
            foreach (var file in staged) fileStorage.DeleteBestEffort(file.Stored.StoragePath);
            return Result.Failure<VehicleRegistrationTransitionResponse>(FleetErrors.Conflict);
        }
        catch
        {
            foreach (var file in staged) fileStorage.DeleteBestEffort(file.Stored.StoragePath);
            throw;
        }
    }

    public async Task<Result<IReadOnlyList<VehicleRegistrationTransitionResponse>>> GetRegistrationTransitionHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(id, PermissionKeys.Fleet.VehiclesRead, cancellationToken);
        if (access.IsFailure) return Result.Failure<IReadOnlyList<VehicleRegistrationTransitionResponse>>(access.Error);
        var rows = await dbContext.VehicleRegistrationTransitions.AsNoTracking().Where(x => x.VehicleId == id).OrderByDescending(x => x.EffectiveAtUtc).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<VehicleRegistrationTransitionResponse>>(rows.Select(MapTransition).ToArray());
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
        if (target is VehicleOperationalStatus.Stolen or VehicleOperationalStatus.OutOfService or VehicleOperationalStatus.Decommissioned)
        {
            await EndActiveAssignmentForHoldAsync(vehicle, request.EffectiveAtUtc, request.Reason, actor.Value, cancellationToken);
        }
        if (target == VehicleOperationalStatus.Available && await HasBlockingIssueAsync(vehicle.Id, null, cancellationToken)) return Result.Failure<VehicleDetailResponse>(FleetErrors.Conflict);
        await SetStatusAsync(vehicle, target.Value, request.EffectiveAtUtc, request.Reason, VehicleStatusSourceType.Administrative, vehicle.Id, actor.Value, cancellationToken);
        if (target == VehicleOperationalStatus.Decommissioned) { vehicle.DecommissionedAtUtc = request.EffectiveAtUtc; vehicle.DecommissionReason = request.Reason.Trim(); }
        await dbContext.SaveChangesAsync(cancellationToken);
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

    public Task<Result<RiderVehicleAssignmentResponse>> TakeAsync(TakeVehicleRequest request, IReadOnlyList<PrivateFileUpload> promissoryFiles, string idempotencyKey, CancellationToken cancellationToken = default) =>
        ExecuteTakeAsync(request, promissoryFiles, idempotencyKey, null, RiderVehicleAssignmentEventType.Taken, cancellationToken);

    private async Task<Result<RiderVehicleAssignmentResponse>> ExecuteTakeAsync(TakeVehicleRequest request, IReadOnlyList<PrivateFileUpload> promissoryFiles, string idempotencyKey, Guid? previousAssignmentId, RiderVehicleAssignmentEventType eventType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.IdempotencyRequired);
        if (promissoryFiles.Count > 3) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.FileLimit);
        if (!TryNormalizeRealRider(request.IsRealRider, request.RealRider, out var realRiderDetails)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.InvalidRequest);
        var stagedResult = await StagePromissoryFilesAsync(request.RiderProfileId, promissoryFiles, cancellationToken);
        if (stagedResult.IsFailure) return Result.Failure<RiderVehicleAssignmentResponse>(stagedResult.Error);
        var staged = stagedResult.Value!;
        var hash = FleetServiceSupport.HashRequest(new { Request = request, Files = staged.Select(x => x.Stored.Sha256Checksum).ToArray() });
        var replay = await ReplayAssignmentAsync("take", idempotencyKey, hash, cancellationToken);
        if (replay is not null) { CleanupStaged(staged); return replay; }
        var actor = support.UserId;
        if (!actor.HasValue) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.CurrentUserUnavailable); }
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == request.VehicleId, cancellationToken);
        if (vehicle is null) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.NotFound); }
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.AssignmentsManage, cancellationToken)) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Forbidden); }
        var permitStart = FleetBusinessRules.RiyadhDate(request.StartedAtUtc);
        if (vehicle.CurrentOperationalStatus != VehicleOperationalStatus.Available || vehicle.CurrentAssignmentId.HasValue || !FleetBusinessRules.IsCoreIdentityReady(vehicle) || request.StartOdometer < vehicle.CurrentOdometer || !ValidFuel(request.StartFuelLevelPercentage) || string.IsNullOrWhiteSpace(request.PermissionReference) || string.IsNullOrWhiteSpace(request.Reason)) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.VehicleUnavailable); }
        var rider = await dbContext.RiderProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.RiderProfileId, cancellationToken);
        if (rider is null || !await dbContext.Employees.AnyAsync(x => x.Id == rider.EmployeeId && !x.IsEmployee && x.Status == EmployeeStatus.Active, cancellationToken)
            || await dbContext.RiderVehicleAssignments.AnyAsync(x => x.RiderProfileId == request.RiderProfileId && x.EndedAtUtc == null, cancellationToken)) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.RiderUnavailable); }
        var existingPromissoryVersions = await CurrentPromissoryVersionsAsync(rider.Id, cancellationToken);
        if (existingPromissoryVersions.Count == 0 && staged.Count == 0) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.InvalidRequest); }
        if (existingPromissoryVersions.Count + staged.Count > 3) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.FileLimit); }
        try
        {
            return await dbContext.ExecuteTransactionAsync(async _ =>
            {
                var operationId = Guid.CreateVersion7();
                var assignment = new RiderVehicleAssignment
                {
                    RiderProfileId = rider.Id, IsRealRider = request.IsRealRider, VehicleId = vehicle.Id, OperationId = operationId, PreviousAssignmentId = previousAssignmentId,
                    StartedAtUtc = request.StartedAtUtc, StartLocationSnapshot = await OperatingCitySnapshotAsync(vehicle.OperatingCityId, cancellationToken), StartOdometer = request.StartOdometer,
                    StartVehicleCondition = request.StartCondition, StartFuelLevelPercentage = request.StartFuelLevelPercentage, PermissionReference = request.PermissionReference.Trim(),
                    PermissionStartsOn = permitStart, PermissionEndsOn = FleetBusinessRules.PermitEnd(permitStart), AssignmentReason = request.Reason.Trim(), AssignedByUserId = actor.Value,
                    WasBackdated = request.StartedAtUtc < support.UtcNow.AddMinutes(-5), BackdatedReason = request.StartedAtUtc < support.UtcNow.AddMinutes(-5) ? request.Reason.Trim() : null, Notes = FleetServiceSupport.TrimOrNull(request.Notes)
                };
                dbContext.RiderVehicleAssignments.Add(assignment);
                if (realRiderDetails is not null)
                {
                    dbContext.RealRiders.Add(new RealRider
                    {
                        RiderVehicleAssignmentId = assignment.Id,
                        Name = realRiderDetails.Name,
                        IqamaNo = realRiderDetails.IqamaNo,
                        RelationshipToAssignedRider = realRiderDetails.RelationshipToAssignedRider,
                        CreatedByUserId = actor.Value
                    });
                }
                var versions = existingPromissoryVersions
                    .Concat(AddStagedPromissoryFiles(rider.Id, staged, actor.Value))
                    .ToArray();
                foreach (var versionId in versions) dbContext.RiderVehicleAssignmentPromissoryFiles.Add(new RiderVehicleAssignmentPromissoryFile { RiderVehicleAssignmentId = assignment.Id, RiderPromissoryFileVersionId = versionId });
                dbContext.RiderVehicleAssignmentEvents.Add(NewAssignmentEvent(assignment.Id, operationId, eventType, request.StartedAtUtc, actor.Value, request.Reason));
                if (request.StartOdometer > vehicle.CurrentOdometer)
                {
                    vehicle.CurrentOdometer = request.StartOdometer; vehicle.LastOdometerAtUtc = request.StartedAtUtc;
                    dbContext.VehicleOdometerReadings.Add(NewOdometer(vehicle.Id, request.StartOdometer, request.StartedAtUtc, VehicleOdometerSourceType.AssignmentTake, assignment.Id, request.Reason));
                }
                vehicle.CurrentAssignmentId = assignment.Id;
                await SetStatusAsync(vehicle, VehicleOperationalStatus.Assigned, request.StartedAtUtc, request.Reason, VehicleStatusSourceType.Assignment, assignment.Id, actor.Value, cancellationToken);
                dbContext.FleetCommandReceipts.Add(new FleetCommandReceipt { CommandName = "take", IdempotencyKey = idempotencyKey.Trim(), RequestHash = hash, ResultEntityId = assignment.Id });
                await dbContext.SaveChangesAsync(cancellationToken);
                ActivateStagedPromissoryFiles(staged);
                if (staged.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success(await MapAssignmentAsync(assignment, cancellationToken));
            }, cancellationToken);
        }
        catch (DbUpdateException)
        {
            CleanupStaged(staged);
            return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Conflict);
        }
        catch
        {
            CleanupStaged(staged);
            throw;
        }
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
        try
        {
            return await dbContext.ExecuteTransactionAsync(async _ =>
            {
                EndAssignment(assignment, vehicle, request.EndedAtUtc, request.EndOdometer, request.EndCondition, request.EndFuelLevelPercentage, request.Reason, actor.Value, RiderVehicleAssignmentEventType.Returned);
                var target = await ResolveAvailableStatusAsync(vehicle.Id, null, cancellationToken);
                await SetStatusAsync(vehicle, target, request.EndedAtUtc, request.Reason, VehicleStatusSourceType.Assignment, assignment.Id, actor.Value, cancellationToken);
                dbContext.FleetCommandReceipts.Add(new FleetCommandReceipt { CommandName = "return", IdempotencyKey = idempotencyKey.Trim(), RequestHash = hash, ResultEntityId = assignment.Id });
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success(await MapAssignmentAsync(assignment, cancellationToken));
            }, cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Conflict);
        }
    }

    public async Task<Result<RiderVehicleAssignmentResponse>> SwitchAsync(SwitchVehicleRequest request, IReadOnlyList<PrivateFileUpload> promissoryFiles, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.IdempotencyRequired);
        if (promissoryFiles.Count > 3) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.FileLimit);
        var oldForRider = await dbContext.RiderVehicleAssignments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.CurrentAssignmentId && x.EndedAtUtc == null, cancellationToken);
        if (oldForRider is null) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.NotFound);
        var oldRealRider = oldForRider.IsRealRider
            ? null
            : await dbContext.RealRiders.AsNoTracking().SingleOrDefaultAsync(x => x.RiderVehicleAssignmentId == oldForRider.Id, cancellationToken);
        if (!oldForRider.IsRealRider && oldRealRider is null) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.InvalidRequest);
        var stagedResult = await StagePromissoryFilesAsync(oldForRider.RiderProfileId, promissoryFiles, cancellationToken);
        if (stagedResult.IsFailure) return Result.Failure<RiderVehicleAssignmentResponse>(stagedResult.Error);
        var staged = stagedResult.Value!;
        var hash = FleetServiceSupport.HashRequest(new { Request = request, Files = staged.Select(x => x.Stored.Sha256Checksum).ToArray() });
        var replay = await ReplayAssignmentAsync("switch", idempotencyKey, hash, cancellationToken);
        if (replay is not null) { CleanupStaged(staged); return replay; }
        var actor = support.UserId;
        if (!actor.HasValue) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.CurrentUserUnavailable); }
        var old = await dbContext.RiderVehicleAssignments.SingleOrDefaultAsync(x => x.Id == request.CurrentAssignmentId && x.EndedAtUtc == null, cancellationToken);
        var next = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == request.NewVehicleId, cancellationToken);
        if (old is null || next is null) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.NotFound); }
        var oldVehicle = await dbContext.Vehicles.SingleAsync(x => x.Id == old.VehicleId, cancellationToken);
        if (!await support.HasVehiclePermissionAsync(oldVehicle, PermissionKeys.Fleet.AssignmentsManage, cancellationToken) || !await support.HasVehiclePermissionAsync(next, PermissionKeys.Fleet.AssignmentsManage, cancellationToken)) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Forbidden); }
        var permitStart = FleetBusinessRules.RiyadhDate(request.SwitchedAtUtc);
        if (!FleetServiceSupport.MatchesRowVersion(old.RowVersion, request.RowVersion) || next.CurrentOperationalStatus != VehicleOperationalStatus.Available || next.CurrentAssignmentId.HasValue || !FleetBusinessRules.IsCoreIdentityReady(next) || request.OldVehicleOdometer < old.StartOdometer || request.NewVehicleOdometer < next.CurrentOdometer || string.IsNullOrWhiteSpace(request.PermissionReference) || string.IsNullOrWhiteSpace(request.Reason)) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Conflict); }
        var existingPromissoryVersions = await CurrentPromissoryVersionsAsync(old.RiderProfileId, cancellationToken);
        if (existingPromissoryVersions.Count == 0 && staged.Count == 0) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.InvalidRequest); }
        if (existingPromissoryVersions.Count + staged.Count > 3) { CleanupStaged(staged); return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.FileLimit); }
        try
        {
            return await dbContext.ExecuteTransactionAsync(async _ =>
            {
                EndAssignment(old, oldVehicle, request.SwitchedAtUtc, request.OldVehicleOdometer, request.OldVehicleCondition, request.OldFuelLevelPercentage, request.Reason, actor.Value, RiderVehicleAssignmentEventType.SwitchedOut);
                await SetStatusAsync(oldVehicle, await ResolveAvailableStatusAsync(oldVehicle.Id, null, cancellationToken), request.SwitchedAtUtc, request.Reason, VehicleStatusSourceType.Assignment, old.Id, actor.Value, cancellationToken);
                var newAssignment = new RiderVehicleAssignment
                {
                    RiderProfileId = old.RiderProfileId, IsRealRider = old.IsRealRider, VehicleId = next.Id, OperationId = old.OperationId, PreviousAssignmentId = old.Id,
                    StartedAtUtc = request.SwitchedAtUtc, StartLocationSnapshot = await OperatingCitySnapshotAsync(next.OperatingCityId, cancellationToken), StartOdometer = request.NewVehicleOdometer,
                    StartVehicleCondition = request.NewVehicleCondition, StartFuelLevelPercentage = request.NewFuelLevelPercentage, PermissionReference = request.PermissionReference.Trim(),
                    PermissionStartsOn = permitStart, PermissionEndsOn = FleetBusinessRules.PermitEnd(permitStart), AssignmentReason = request.Reason.Trim(), AssignedByUserId = actor.Value
                };
                dbContext.RiderVehicleAssignments.Add(newAssignment);
                if (oldRealRider is not null)
                {
                    dbContext.RealRiders.Add(new RealRider
                    {
                        RiderVehicleAssignmentId = newAssignment.Id,
                        Name = oldRealRider.Name,
                        IqamaNo = oldRealRider.IqamaNo,
                        RelationshipToAssignedRider = oldRealRider.RelationshipToAssignedRider,
                        CreatedByUserId = actor.Value
                    });
                }
                var versions = existingPromissoryVersions
                    .Concat(AddStagedPromissoryFiles(old.RiderProfileId, staged, actor.Value))
                    .ToArray();
                foreach (var versionId in versions) dbContext.RiderVehicleAssignmentPromissoryFiles.Add(new RiderVehicleAssignmentPromissoryFile { RiderVehicleAssignmentId = newAssignment.Id, RiderPromissoryFileVersionId = versionId });
                dbContext.RiderVehicleAssignmentEvents.Add(NewAssignmentEvent(newAssignment.Id, old.OperationId, RiderVehicleAssignmentEventType.SwitchedIn, request.SwitchedAtUtc, actor.Value, request.Reason));
                if (request.NewVehicleOdometer > next.CurrentOdometer) dbContext.VehicleOdometerReadings.Add(NewOdometer(next.Id, request.NewVehicleOdometer, request.SwitchedAtUtc, VehicleOdometerSourceType.AssignmentTake, newAssignment.Id, request.Reason));
                next.CurrentOdometer = request.NewVehicleOdometer; next.LastOdometerAtUtc = request.SwitchedAtUtc; next.CurrentAssignmentId = newAssignment.Id;
                await SetStatusAsync(next, VehicleOperationalStatus.Assigned, request.SwitchedAtUtc, request.Reason, VehicleStatusSourceType.Assignment, newAssignment.Id, actor.Value, cancellationToken);
                dbContext.FleetCommandReceipts.Add(new FleetCommandReceipt { CommandName = "switch", IdempotencyKey = idempotencyKey.Trim(), RequestHash = hash, ResultEntityId = newAssignment.Id });
                await dbContext.SaveChangesAsync(cancellationToken);
                ActivateStagedPromissoryFiles(staged);
                if (staged.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success(await MapAssignmentAsync(newAssignment, cancellationToken));
            }, cancellationToken);
        }
        catch (DbUpdateException)
        {
            CleanupStaged(staged);
            return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.Conflict);
        }
        catch
        {
            CleanupStaged(staged);
            throw;
        }
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
        if (!FleetServiceSupport.MatchesRowVersion(assignment.RowVersion, request.RowVersion) || assignment.PermissionEndsOn.HasValue && request.PermissionStartsOn <= assignment.PermissionEndsOn.Value || string.IsNullOrWhiteSpace(request.PermissionReference) || string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.InvalidRequest);
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<RiderVehicleAssignmentResponse>(FleetErrors.CurrentUserUnavailable);
        assignment.PermissionStartsOn = request.PermissionStartsOn; assignment.PermissionEndsOn = FleetBusinessRules.PermitEnd(request.PermissionStartsOn); assignment.PermissionReference = request.PermissionReference.Trim();
        dbContext.RiderVehicleAssignmentEvents.Add(NewAssignmentEvent(assignment.Id, assignment.OperationId, RiderVehicleAssignmentEventType.PermissionRenewed, support.UtcNow, actor.Value, request.Reason));
        dbContext.FleetCommandReceipts.Add(new FleetCommandReceipt { CommandName = "renew-permission", IdempotencyKey = idempotencyKey.Trim(), RequestHash = hash, ResultEntityId = assignment.Id });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await MapAssignmentAsync(assignment, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<RiderVehicleAssignmentResponse>>> GetAssignmentsAsync(Guid? vehicleId, Guid? riderProfileId, bool activeOnly, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsRead, null, cancellationToken))
            return Result.Failure<IReadOnlyList<RiderVehicleAssignmentResponse>>(FleetErrors.Forbidden);

        var query = dbContext.RiderVehicleAssignments.AsNoTracking();
        if (vehicleId.HasValue) query = query.Where(item => item.VehicleId == vehicleId.Value);
        if (riderProfileId.HasValue) query = query.Where(item => item.RiderProfileId == riderProfileId.Value);
        if (activeOnly) query = query.Where(item => item.EndedAtUtc == null);

        var assignments = await query.OrderByDescending(item => item.StartedAtUtc).ToArrayAsync(cancellationToken);
        var responses = new List<RiderVehicleAssignmentResponse>(assignments.Length);
        foreach (var assignment in assignments)
        {
            responses.Add(await MapAssignmentAsync(assignment, cancellationToken));
        }

        return Result.Success<IReadOnlyList<RiderVehicleAssignmentResponse>>(responses);
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
            "operation-cards" when access.Value!.RegistrationType == VehicleRegistrationType.PublicTransport => await dbContext.VehicleOperationCards.AsNoTracking().Where(x => x.VehicleId == vehicleId).OrderByDescending(x => x.ExpiryDate).Select(x => new VehicleComplianceResponse(x.Id, x.VehicleId, "OperationCard", x.CardNumber, x.IssuingAuthority, x.IssueDate, x.ExpiryDate, FleetServiceSupport.DueStatus(x.ExpiryDate, check), x.IsCurrent, x.PreviousRecordId, Convert.ToBase64String(x.RowVersion))).ToArrayAsync(cancellationToken),
            _ => []
        };
        return type is "registrations" or "insurance-policies" or "inspections" or "operation-cards" && (type != "operation-cards" || access.Value!.RegistrationType == VehicleRegistrationType.PublicTransport)
            ? Result.Success(result)
            : Result.Failure<IReadOnlyList<VehicleComplianceResponse>>(FleetErrors.InvalidRequest);
    }

    public async Task<Result<VehicleComplianceResponse>> RenewRegistrationAsync(Guid vehicleId, VehicleRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(vehicleId, PermissionKeys.Fleet.ComplianceManage, cancellationToken);
        if (access.IsFailure) return Result.Failure<VehicleComplianceResponse>(access.Error);
        if (string.IsNullOrWhiteSpace(request.RegistrationNumber) || string.IsNullOrWhiteSpace(request.IssuingAuthority) || request.ExpiryDate < request.IssueDate) return Result.Failure<VehicleComplianceResponse>(FleetErrors.InvalidRequest);
        var previous = await dbContext.VehicleRegistrations.SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.IsCurrent, cancellationToken);
        if (previous is not null) { previous.IsCurrent = false; previous.Status = ComplianceRecordStatus.Superseded; }
        var item = new VehicleRegistration { VehicleId = vehicleId, RegistrationNumber = request.RegistrationNumber.Trim(), IssuingAuthority = request.IssuingAuthority.Trim(), IssueDate = request.IssueDate, ExpiryDate = request.ExpiryDate, PreviousRecordId = previous?.Id, Notes = FleetServiceSupport.TrimOrNull(request.Notes) };
        dbContext.VehicleRegistrations.Add(item); await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(MapCompliance(item));
    }

    public async Task<Result<VehicleComplianceResponse>> RenewInsuranceAsync(Guid vehicleId, VehicleInsuranceRequest request, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(vehicleId, PermissionKeys.Fleet.ComplianceManage, cancellationToken);
        if (access.IsFailure) return Result.Failure<VehicleComplianceResponse>(access.Error);
        if (string.IsNullOrWhiteSpace(request.ProviderName) || string.IsNullOrWhiteSpace(request.PolicyNumber) || request.ExpiryDate < request.EffectiveFrom) return Result.Failure<VehicleComplianceResponse>(FleetErrors.InvalidRequest);
        var previous = await dbContext.VehicleInsurancePolicies.SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.IsCurrent, cancellationToken);
        if (previous is not null) { previous.IsCurrent = false; previous.Status = ComplianceRecordStatus.Superseded; }
        var item = new VehicleInsurancePolicy { VehicleId = vehicleId, ProviderName = request.ProviderName.Trim(), PolicyNumber = request.PolicyNumber.Trim(), CoverageType = FleetServiceSupport.TrimOrNull(request.CoverageType), EffectiveFrom = request.EffectiveFrom, ExpiryDate = request.ExpiryDate, ClaimReference = FleetServiceSupport.TrimOrNull(request.ClaimReference), ClaimContact = FleetServiceSupport.TrimOrNull(request.ClaimContact), PreviousRecordId = previous?.Id, Notes = FleetServiceSupport.TrimOrNull(request.Notes) };
        dbContext.VehicleInsurancePolicies.Add(item); await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(MapCompliance(item));
    }

    public async Task<Result<VehicleComplianceResponse>> RenewInspectionAsync(Guid vehicleId, VehicleInspectionRequest request, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(vehicleId, PermissionKeys.Fleet.ComplianceManage, cancellationToken);
        if (access.IsFailure) return Result.Failure<VehicleComplianceResponse>(access.Error);
        if (string.IsNullOrWhiteSpace(request.InspectionNumber) || string.IsNullOrWhiteSpace(request.StationName) || request.ExpiryDate < request.InspectionDate || request.Odometer < 0) return Result.Failure<VehicleComplianceResponse>(FleetErrors.InvalidRequest);
        var previous = await dbContext.VehiclePeriodicInspections.SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.IsCurrent, cancellationToken);
        if (previous is not null) { previous.IsCurrent = false; previous.Status = ComplianceRecordStatus.Superseded; }
        var item = new VehiclePeriodicInspection { VehicleId = vehicleId, InspectionNumber = request.InspectionNumber.Trim(), StationName = request.StationName.Trim(), InspectionDate = request.InspectionDate, ExpiryDate = request.ExpiryDate, Result = request.Result, Odometer = request.Odometer, FailureNotes = FleetServiceSupport.TrimOrNull(request.FailureNotes), PreviousRecordId = previous?.Id, Notes = FleetServiceSupport.TrimOrNull(request.Notes) };
        dbContext.VehiclePeriodicInspections.Add(item); await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(MapCompliance(item));
    }

    public async Task<Result<VehicleComplianceResponse>> RenewOperationCardAsync(Guid vehicleId, VehicleOperationCardRequest request, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessibleVehicleAsync(vehicleId, PermissionKeys.Fleet.ComplianceManage, cancellationToken);
        if (access.IsFailure) return Result.Failure<VehicleComplianceResponse>(access.Error);
        if (access.Value!.RegistrationType != VehicleRegistrationType.PublicTransport
            || string.IsNullOrWhiteSpace(request.CardNumber)
            || string.IsNullOrWhiteSpace(request.IssuingAuthority)
            || request.ExpiryDate < request.IssueDate) return Result.Failure<VehicleComplianceResponse>(FleetErrors.InvalidRequest);

        var previous = await dbContext.VehicleOperationCards.SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.IsCurrent, cancellationToken);
        if (previous is not null) { previous.IsCurrent = false; previous.Status = ComplianceRecordStatus.Superseded; }
        var item = new VehicleOperationCard
        {
            VehicleId = vehicleId,
            CardNumber = request.CardNumber.Trim(),
            IssuingAuthority = request.IssuingAuthority.Trim(),
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            PreviousRecordId = previous?.Id,
            Notes = FleetServiceSupport.TrimOrNull(request.Notes)
        };
        dbContext.VehicleOperationCards.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
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
            if (vehicle.CurrentAssignmentId.HasValue)
                AddDue(result, vehicle.Id, vehicle.AssetNumber, "Permit", vehicle.PermitEndDate, vehicle.PermitStatus, checkDate, vehicle.CurrentAssignmentId);
            if (vehicle.RegistrationType == VehicleRegistrationType.PublicTransport)
                AddDue(result, vehicle.Id, vehicle.AssetNumber, "OperationCard", vehicle.OperationCardExpiryDate, vehicle.OperationCardStatus, checkDate);
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
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.IssuesRead, null, cancellationToken)) return Result.Failure<PagedResponse<VehicleIssueSummaryResponse>>(FleetErrors.Forbidden);
        var vehicleIds = dbContext.Vehicles.AsNoTracking().Select(v => v.Id);
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
        var assignment = await dbContext.RiderVehicleAssignments.SingleOrDefaultAsync(x => x.VehicleId == vehicle.Id && x.EndedAtUtc == null, cancellationToken);
        var issue = new VehicleIssue { IssueNumber = FleetServiceSupport.NewNumber("ISS", support.UtcNow, Guid.CreateVersion7()), VehicleId = vehicle.Id, Category = request.Category, Severity = request.Severity, Description = request.Description.Trim(), ReportedAtUtc = request.ReportedAtUtc, LocationDescription = FleetServiceSupport.TrimOrNull(request.LocationDescription), OdometerAtReport = request.OdometerAtReport, RelatedAssignmentId = assignment?.Id, BlocksOperation = request.BlocksOperation, ReportedByUserId = actor.Value };
        dbContext.VehicleIssues.Add(issue);
        dbContext.VehicleIssueEvents.Add(NewIssueEvent(issue.Id, VehicleIssueEventType.Reported, null, VehicleIssueStatus.Open, request.ReportedAtUtc, actor.Value, request.Description));
        if (request.BlocksOperation)
        {
            await EndActiveAssignmentForHoldAsync(vehicle, request.ReportedAtUtc, request.Description, actor.Value, cancellationToken);
            await SetStatusAsync(vehicle, VehicleOperationalStatus.ProblemHold, request.ReportedAtUtc, request.Description, VehicleStatusSourceType.Issue, issue.Id, actor.Value, cancellationToken);
        }
        dbContext.FleetCommandReceipts.Add(new FleetCommandReceipt { CommandName = "create-issue", IdempotencyKey = idempotencyKey.Trim(), RequestHash = hash, ResultEntityId = issue.Id });
        await dbContext.SaveChangesAsync(cancellationToken);
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
        var realRider = await dbContext.RealRiders.AsNoTracking()
            .Where(x => x.RiderVehicleAssignmentId == item.Id)
            .Select(x => new RealRiderResponse(x.Id, x.Name, x.IqamaNo, x.RelationshipToAssignedRider))
            .SingleOrDefaultAsync(cancellationToken);
        var versionIds = await dbContext.RiderVehicleAssignmentPromissoryFiles.AsNoTracking().Where(x => x.RiderVehicleAssignmentId == item.Id).Select(x => x.RiderPromissoryFileVersionId).ToArrayAsync(cancellationToken);
        return new RiderVehicleAssignmentResponse(item.Id, item.RiderProfileId, employeeId, item.IsRealRider, realRider, item.VehicleId, info.AssetNumber, info.RiderName, item.StartedAtUtc, item.EndedAtUtc, item.StartLocationSnapshot, item.EndLocationSnapshot, item.StartOdometer, item.EndOdometer, item.PermissionReference, item.PermissionStartsOn, item.PermissionEndsOn, item.Status, item.AssignmentReason, item.CompletionReason, item.OperationId, versionIds, FleetServiceSupport.EncodeRowVersion(item.RowVersion));
    }

    private async Task<VehicleSummaryResponse[]> BuildSummariesAsync(Vehicle[] vehicles, CancellationToken cancellationToken)
    {
        if (vehicles.Length == 0) return [];
        var ids = vehicles.Select(x => x.Id).ToArray();
        var manufacturers = await dbContext.VehicleManufacturers.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var models = await dbContext.VehicleModels.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var sponsors = await dbContext.Sponsors.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var cities = await (from oc in dbContext.OperatingCities.AsNoTracking() join gc in dbContext.GlobalCities.AsNoTracking() on oc.GlobalCityId equals gc.Id select new { oc.Id, gc.NameAr }).ToDictionaryAsync(x => x.Id, cancellationToken);
        var assignments = await (
            from a in dbContext.RiderVehicleAssignments.AsNoTracking()
            join r in dbContext.RiderProfiles.AsNoTracking() on a.RiderProfileId equals r.Id
            join e in dbContext.Employees.AsNoTracking() on r.EmployeeId equals e.Id
            join realRiderRow in dbContext.RealRiders.AsNoTracking() on a.Id equals realRiderRow.RiderVehicleAssignmentId into realRiders
            from realRider in realRiders.DefaultIfEmpty()
            where ids.Contains(a.VehicleId) && a.EndedAtUtc == null
            select new
            {
                a.VehicleId,
                a.Id,
                a.RiderProfileId,
                a.PermissionEndsOn,
                a.IsRealRider,
                e.FullNameAr,
                RealRiderId = realRider == null ? null : (Guid?)realRider.Id,
                RealRiderName = realRider == null ? null : realRider.Name,
                RealRiderIqamaNo = realRider == null ? null : realRider.IqamaNo,
                RealRiderRelationship = realRider == null ? null : realRider.RelationshipToAssignedRider
            }).ToDictionaryAsync(x => x.VehicleId, cancellationToken);
        var registrations = await dbContext.VehicleRegistrations.AsNoTracking().Where(x => ids.Contains(x.VehicleId) && x.IsCurrent).ToDictionaryAsync(x => x.VehicleId, cancellationToken);
        var insurance = await dbContext.VehicleInsurancePolicies.AsNoTracking().Where(x => ids.Contains(x.VehicleId) && x.IsCurrent).ToDictionaryAsync(x => x.VehicleId, cancellationToken);
        var inspections = await dbContext.VehiclePeriodicInspections.AsNoTracking().Where(x => ids.Contains(x.VehicleId) && x.IsCurrent).ToDictionaryAsync(x => x.VehicleId, cancellationToken);
        var operationCards = await dbContext.VehicleOperationCards.AsNoTracking().Where(x => ids.Contains(x.VehicleId) && x.IsCurrent).ToDictionaryAsync(x => x.VehicleId, cancellationToken);
        var check = DateOnly.FromDateTime(support.UtcNow.UtcDateTime);
        return vehicles.Select(v =>
        {
            assignments.TryGetValue(v.Id, out var a); registrations.TryGetValue(v.Id, out var reg); insurance.TryGetValue(v.Id, out var ins); inspections.TryGetValue(v.Id, out var chk); operationCards.TryGetValue(v.Id, out var operationCard);
            var sponsorName = v.SponsorId.HasValue && sponsors.TryGetValue(v.SponsorId.Value, out var sponsor) ? sponsor.RegistryNameAr : null;
            var cityName = v.OperatingCityId.HasValue && cities.TryGetValue(v.OperatingCityId.Value, out var city) ? city.NameAr : null;
            var realRider = a?.IsRealRider is false && a.RealRiderId.HasValue
                ? new RealRiderResponse(a.RealRiderId.Value, a.RealRiderName!, a.RealRiderIqamaNo!, a.RealRiderRelationship!)
                : null;
            return new VehicleSummaryResponse(v.Id, v.AssetNumber, v.PlateNumberAr, v.PlateNumberEn, v.SerialNumber, manufacturers[v.VehicleManufacturerId].NameEn, models[v.VehicleModelId].NameEn, v.VehicleType, v.RegistrationType, v.CurrentOperationalStatus, v.SponsorId, sponsorName, v.OperatingCityId, cityName, v.CurrentOdometer, a?.Id, a?.RiderProfileId, a?.FullNameAr, a?.IsRealRider, realRider, reg?.ExpiryDate, FleetServiceSupport.DueStatus(reg?.ExpiryDate, check), ins?.ExpiryDate, FleetServiceSupport.DueStatus(ins?.ExpiryDate, check), chk?.ExpiryDate, FleetServiceSupport.DueStatus(chk?.ExpiryDate, check), a?.PermissionEndsOn, FleetServiceSupport.DueStatus(a?.PermissionEndsOn, check), operationCard?.ExpiryDate, FleetServiceSupport.DueStatus(operationCard?.ExpiryDate, check), FleetBusinessRules.IsCoreIdentityReady(v), FleetServiceSupport.EncodeRowVersion(v.RowVersion));
        }).ToArray();
    }

    private async Task<VehicleDetailResponse> BuildDetailAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        var summary = (await BuildSummariesAsync([vehicle], cancellationToken))[0];
        var supplier = vehicle.PurchasedFromSupplierId.HasValue ? await dbContext.VehicleSuppliers.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == vehicle.PurchasedFromSupplierId).Select(x => x.NameAr).SingleOrDefaultAsync(cancellationToken) : null;
        return new VehicleDetailResponse(summary, vehicle.SerialNumber, vehicle.Vin, vehicle.ChassisNumber, vehicle.EngineNumber, vehicle.SponsorId, vehicle.OperatingCityId, vehicle.PurchasedFromSupplierId, supplier, vehicle.RegistrationType, vehicle.VehicleManufacturerId, vehicle.VehicleModelId, vehicle.ModelYear, vehicle.FuelType, vehicle.TransmissionType, vehicle.ColorAr, vehicle.ColorEn, vehicle.OwnershipType, vehicle.OwnerName, vehicle.AcquisitionDate, vehicle.LeaseReference, vehicle.DecommissionedAtUtc, vehicle.DecommissionReason, vehicle.Notes);
    }

    private static void ApplyVehicle(Vehicle v, VehicleUpsertRequest r, string normalizedAsset, string? normalizedSerial, string? normalizedChassis, string? normalizedAr, string? normalizedEn)
    {
        v.AssetNumber = r.AssetNumber!.Trim(); v.NormalizedAssetNumber = normalizedAsset; v.SerialNumber = FleetServiceSupport.TrimOrNull(r.SerialNumber); v.NormalizedSerialNumber = normalizedSerial; v.PlateNumberAr = FleetServiceSupport.TrimOrNull(r.PlateNumberAr); v.NormalizedPlateNumberAr = normalizedAr; v.PlateNumberEn = FleetServiceSupport.TrimOrNull(r.PlateNumberEn); v.NormalizedPlateNumberEn = normalizedEn;
        v.PlateLettersAr = FleetServiceSupport.TrimOrNull(r.PlateLettersAr); v.PlateLettersEn = FleetServiceSupport.TrimOrNull(r.PlateLettersEn); v.PlateDigits = FleetServiceSupport.TrimOrNull(r.PlateDigits); v.Vin = FleetServiceSupport.TrimOrNull(r.Vin)?.ToUpperInvariant(); v.ChassisNumber = FleetServiceSupport.TrimOrNull(r.ChassisNumber); v.NormalizedChassisNumber = normalizedChassis; v.EngineNumber = FleetServiceSupport.TrimOrNull(r.EngineNumber);
        v.SponsorId = r.SponsorId; v.OperatingCityId = r.OperatingCityId; v.PurchasedFromSupplierId = r.PurchasedFromSupplierId; v.RegistrationType = r.RegistrationType;
        v.VehicleManufacturerId = r.VehicleManufacturerId; v.VehicleModelId = r.VehicleModelId; v.ModelYear = r.ModelYear; v.VehicleType = r.VehicleType; v.FuelType = r.FuelType; v.TransmissionType = r.TransmissionType; v.ColorAr = FleetServiceSupport.TrimOrNull(r.ColorAr); v.ColorEn = FleetServiceSupport.TrimOrNull(r.ColorEn); v.OwnershipType = r.OwnershipType; v.OwnerName = FleetServiceSupport.TrimOrNull(r.OwnerName); v.AcquisitionDate = r.AcquisitionDate; v.LeaseReference = FleetServiceSupport.TrimOrNull(r.LeaseReference); v.CurrentOdometer = r.CurrentOdometer; v.Notes = FleetServiceSupport.TrimOrNull(r.Notes);
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
    private static VehicleIssueSummaryResponse MapIssue(VehicleIssue x) => new(x.Id, x.IssueNumber, x.VehicleId, x.Category, x.Severity, x.BlocksOperation, x.Status, x.ReportedAtUtc, x.Description, x.LocationDescription, x.ResolutionSummary, FleetServiceSupport.EncodeRowVersion(x.RowVersion));
    private static VehicleAccidentSummaryResponse MapAccident(VehicleAccident x) => new(x.Id, x.AccidentNumber, x.VehicleId, x.RiderProfileId, x.RiderVehicleAssignmentId, x.VehicleIssueId, x.OccurredAtUtc, x.Severity, x.IsDrivable, x.Status, x.LocationDescription, FleetServiceSupport.EncodeRowVersion(x.RowVersion));

    private static bool TryNormalizeRealRider(bool isRealRider, RealRiderRequest? request, out RealRiderRequest? normalized)
    {
        normalized = null;
        if (isRealRider) return request is null;
        if (request is null) return false;

        var name = request.Name?.Trim();
        var iqamaNo = string.IsNullOrWhiteSpace(request.IqamaNo) ? null : FleetServiceSupport.NormalizeIdentifier(request.IqamaNo);
        var relationship = request.RelationshipToAssignedRider?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200
            || iqamaNo is not { Length: 10 } || !iqamaNo.All(char.IsAsciiDigit)
            || string.IsNullOrWhiteSpace(relationship) || relationship.Length > 200)
        {
            return false;
        }

        normalized = new RealRiderRequest(name, iqamaNo, relationship);
        return true;
    }

    private void EndAssignment(RiderVehicleAssignment assignment, Vehicle vehicle, DateTimeOffset endedAt, long odometer, VehicleCondition condition, byte? fuel, string reason, Guid actor, RiderVehicleAssignmentEventType eventType)
    {
        assignment.EndedAtUtc = endedAt; assignment.EndLocationSnapshot = assignment.StartLocationSnapshot; assignment.EndOdometer = odometer; assignment.EndVehicleCondition = condition; assignment.EndFuelLevelPercentage = fuel; assignment.Status = RiderVehicleAssignmentStatus.Completed; assignment.CompletionReason = reason.Trim(); assignment.EndedByUserId = actor;
        vehicle.CurrentAssignmentId = null; vehicle.CurrentOdometer = Math.Max(vehicle.CurrentOdometer, odometer); vehicle.LastOdometerAtUtc = endedAt;
        dbContext.RiderVehicleAssignmentEvents.Add(NewAssignmentEvent(assignment.Id, assignment.OperationId, eventType, endedAt, actor, reason));
        dbContext.VehicleOdometerReadings.Add(NewOdometer(vehicle.Id, odometer, endedAt, VehicleOdometerSourceType.AssignmentReturn, assignment.Id, reason));
    }

    private async Task EndActiveAssignmentForHoldAsync(Vehicle vehicle, DateTimeOffset at, string reason, Guid actor, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.RiderVehicleAssignments.SingleOrDefaultAsync(x => x.VehicleId == vehicle.Id && x.EndedAtUtc == null, cancellationToken);
        if (assignment is not null) EndAssignment(assignment, vehicle, at, vehicle.CurrentOdometer, VehicleCondition.Damaged, null, reason, actor, RiderVehicleAssignmentEventType.Returned);
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

    private async Task<VehicleReadinessResponse> BuildReadinessAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        var missingCore = new List<string>();
        if (string.IsNullOrWhiteSpace(vehicle.SerialNumber) || string.IsNullOrWhiteSpace(vehicle.NormalizedSerialNumber)) missingCore.Add(nameof(vehicle.SerialNumber));
        if (string.IsNullOrWhiteSpace(vehicle.ChassisNumber) || string.IsNullOrWhiteSpace(vehicle.NormalizedChassisNumber)) missingCore.Add(nameof(vehicle.ChassisNumber));
        if (string.IsNullOrWhiteSpace(vehicle.PlateNumberAr)) missingCore.Add(nameof(vehicle.PlateNumberAr));
        if (string.IsNullOrWhiteSpace(vehicle.PlateNumberEn)) missingCore.Add(nameof(vehicle.PlateNumberEn));
        if (!vehicle.SponsorId.HasValue) missingCore.Add(nameof(vehicle.SponsorId));
        if (!vehicle.OperatingCityId.HasValue) missingCore.Add(nameof(vehicle.OperatingCityId));
        if (!vehicle.RegistrationType.HasValue) missingCore.Add(nameof(vehicle.RegistrationType));
        if (vehicle.OwnershipType == VehicleOwnershipType.Owned && !vehicle.PurchasedFromSupplierId.HasValue) missingCore.Add(nameof(vehicle.PurchasedFromSupplierId));
        var present = await dbContext.VehicleAttachments.AsNoTracking().Where(x => x.VehicleId == vehicle.Id && x.CurrentVersionId != null).Select(x => x.Kind).ToArrayAsync(cancellationToken);
        var (missingPhotos, missingDocuments) = FleetBusinessRules.MissingFiles(vehicle.RegistrationType, present);
        var warnings = missingPhotos.Select(x => $"Missing {x}.").Concat(missingDocuments.Select(x => $"Missing {x}.")).ToArray();
        var eligible = missingCore.Count == 0 && vehicle.CurrentOperationalStatus == VehicleOperationalStatus.Available && !vehicle.CurrentAssignmentId.HasValue;
        return new VehicleReadinessResponse(vehicle.Id, missingCore, missingPhotos, missingDocuments, warnings, eligible);
    }

    private static Dictionary<string, object?> IdentitySnapshot(Vehicle vehicle) => new(StringComparer.Ordinal)
    {
        [nameof(vehicle.AssetNumber)] = vehicle.AssetNumber, [nameof(vehicle.SerialNumber)] = vehicle.SerialNumber,
        [nameof(vehicle.ChassisNumber)] = vehicle.ChassisNumber, [nameof(vehicle.Vin)] = vehicle.Vin,
        [nameof(vehicle.PlateNumberAr)] = vehicle.PlateNumberAr, [nameof(vehicle.PlateNumberEn)] = vehicle.PlateNumberEn,
        [nameof(vehicle.PlateLettersAr)] = vehicle.PlateLettersAr, [nameof(vehicle.PlateLettersEn)] = vehicle.PlateLettersEn,
        [nameof(vehicle.PlateDigits)] = vehicle.PlateDigits, [nameof(vehicle.SponsorId)] = vehicle.SponsorId,
        [nameof(vehicle.OperatingCityId)] = vehicle.OperatingCityId, [nameof(vehicle.PurchasedFromSupplierId)] = vehicle.PurchasedFromSupplierId,
        [nameof(vehicle.RegistrationType)] = vehicle.RegistrationType
    };

    private async Task<Result<StagedVehicleSlot>> StageVehicleSlotAsync(Guid vehicleId, VehicleFileKind kind, PrivateFileUpload upload, Guid actor, CancellationToken cancellationToken)
    {
        var attachment = await dbContext.VehicleAttachments.SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.Kind == kind, cancellationToken)
            ?? new VehicleAttachment { VehicleId = vehicleId, Kind = kind, DisplayName = FileDisplayName(kind) };
        var isNew = dbContext.Entry(attachment).State == EntityState.Detached;
        var versionId = Guid.CreateVersion7();
        var stored = await fileStorage.StoreAsync($"vehicles/{vehicleId:N}/{attachment.Id:N}/{versionId:N}", upload, 10 * 1024 * 1024, cancellationToken);
        if (stored.IsFailure) return Result.Failure<StagedVehicleSlot>(FleetErrors.InvalidFile);
        var number = await dbContext.VehicleAttachmentVersions.Where(x => x.VehicleAttachmentId == attachment.Id).MaxAsync(x => (int?)x.VersionNumber, cancellationToken) + 1 ?? 1;
        var version = new VehicleAttachmentVersion
        {
            Id = versionId, VehicleAttachmentId = attachment.Id, VersionNumber = number, OriginalFileName = stored.Value!.OriginalFileName,
            StoredFileName = stored.Value.StoredFileName, ContentType = stored.Value.ContentType, FileSizeBytes = stored.Value.Length,
            Sha256Checksum = stored.Value.Sha256Checksum, StoragePath = stored.Value.StoragePath, UploadedByUserId = actor,
            UploadedAtUtc = support.UtcNow, SupersededVersionId = attachment.CurrentVersionId
        };
        return Result.Success(new StagedVehicleSlot(attachment, version, stored.Value, isNew));
    }

    private async Task<Result<List<StagedPromissoryFile>>> StagePromissoryFilesAsync(Guid riderProfileId, IReadOnlyList<PrivateFileUpload> uploads, CancellationToken cancellationToken)
    {
        var result = new List<StagedPromissoryFile>(uploads.Count);
        foreach (var upload in uploads)
        {
            if (!IsDocument(upload)) { CleanupStaged(result); return Result.Failure<List<StagedPromissoryFile>>(FleetErrors.InvalidFile); }
            var fileId = Guid.CreateVersion7(); var versionId = Guid.CreateVersion7();
            var stored = await fileStorage.StoreAsync($"riders/{riderProfileId:N}/promissory-notes/{fileId:N}/{versionId:N}", upload, 10 * 1024 * 1024, cancellationToken);
            if (stored.IsFailure) { CleanupStaged(result); return Result.Failure<List<StagedPromissoryFile>>(FleetErrors.InvalidFile); }
            result.Add(new StagedPromissoryFile(fileId, versionId, stored.Value!));
        }
        return Result.Success(result);
    }

    private List<Guid> AddStagedPromissoryFiles(Guid riderProfileId, IReadOnlyList<StagedPromissoryFile> staged, Guid actor)
    {
        var result = new List<Guid>(staged.Count);
        foreach (var item in staged)
        {
            var file = new RiderPromissoryFile { Id = item.FileId, RiderProfileId = riderProfileId };
            var version = new RiderPromissoryFileVersion
            {
                Id = item.VersionId, RiderPromissoryFileId = item.FileId, VersionNumber = 1, OriginalFileName = item.Stored.OriginalFileName,
                StoredFileName = item.Stored.StoredFileName, ContentType = item.Stored.ContentType, FileSizeBytes = item.Stored.Length,
                Sha256Checksum = item.Stored.Sha256Checksum, StoragePath = item.Stored.StoragePath, UploadedByUserId = actor, UploadedAtUtc = support.UtcNow
            };
            dbContext.RiderPromissoryFiles.Add(file); dbContext.RiderPromissoryFileVersions.Add(version); result.Add(version.Id);
        }
        return result;
    }

    private void ActivateStagedPromissoryFiles(IEnumerable<StagedPromissoryFile> staged)
    {
        foreach (var item in staged) dbContext.RiderPromissoryFiles.Local.Single(x => x.Id == item.FileId).CurrentVersionId = item.VersionId;
    }

    private async Task<List<Guid>> CurrentPromissoryVersionsAsync(Guid riderProfileId, CancellationToken cancellationToken) =>
        await dbContext.RiderPromissoryFiles.AsNoTracking().Where(x => x.RiderProfileId == riderProfileId && x.CurrentVersionId != null).OrderBy(x => x.CreatedAtUtc).Select(x => x.CurrentVersionId!.Value).ToListAsync(cancellationToken);

    private void CleanupStaged(IEnumerable<StagedPromissoryFile> staged)
    {
        foreach (var item in staged) fileStorage.DeleteBestEffort(item.Stored.StoragePath);
    }

    private async Task<string?> OperatingCitySnapshotAsync(Guid? operatingCityId, CancellationToken cancellationToken)
    {
        if (!operatingCityId.HasValue) return null;
        return await (from oc in dbContext.OperatingCities.AsNoTracking() join gc in dbContext.GlobalCities.AsNoTracking() on oc.GlobalCityId equals gc.Id where oc.Id == operatingCityId select gc.NameAr).SingleOrDefaultAsync(cancellationToken);
    }

    private static bool IsDocument(PrivateFileUpload file) => file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) || file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    private static string FileDisplayName(VehicleFileKind kind) => kind switch { VehicleFileKind.Istimara => "الاستمارة", VehicleFileKind.OperationCard => "كرت تشغيل", VehicleFileKind.FrontImage => "صورة أمامية", VehicleFileKind.RearImage => "صورة خلفية", VehicleFileKind.LeftImage => "صورة الجانب الأيسر", VehicleFileKind.RightImage => "صورة الجانب الأيمن", _ => "ملف قديم" };
    private static VehicleSupplierResponse MapSupplier(VehicleSupplier item) => new(item.Id, item.Code, item.NameAr, item.NameEn, item.CommercialRegistrationNumber, item.TaxNumber, item.Phone, new FleetAddressResponse(item.Address.BuildingNumber, item.Address.Street, item.Address.District, item.Address.City, item.Address.PostalCode, item.Address.AdditionalNumber), item.Status, item.Notes, FleetServiceSupport.EncodeRowVersion(item.RowVersion));
    private static VehicleRegistrationTransitionResponse MapTransition(VehicleRegistrationTransition item) => new(item.Id, item.VehicleId, item.FromType, item.ToType, item.OldPlateNumberAr, item.OldPlateNumberEn, item.NewPlateNumberAr, item.NewPlateNumberEn, item.EffectiveAtUtc, item.Reason, item.IstimaraVersionId, item.OperationCardVersionId, item.ActorUserId, item.CreatedAtUtc);

    private sealed record StagedVehicleSlot(VehicleAttachment Attachment, VehicleAttachmentVersion Version, StoredPrivateFile Stored, bool IsNew);
    private sealed record StagedPromissoryFile(Guid FileId, Guid VersionId, StoredPrivateFile Stored);

    private static bool ValidFuel(byte? value) => value is null or <= 100;
    private static (int Page, int PageSize) NormalizePage(int page, int pageSize) => (Math.Max(1, page), Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 200));

    private static VehicleComplianceResponse MapCompliance(VehicleRegistration x) => new(x.Id, x.VehicleId, "Registration", x.RegistrationNumber, x.IssuingAuthority, x.IssueDate, x.ExpiryDate, FleetServiceSupport.DueStatus(x.ExpiryDate, DateOnly.FromDateTime(DateTime.UtcNow)), x.IsCurrent, x.PreviousRecordId, FleetServiceSupport.EncodeRowVersion(x.RowVersion));
    private static VehicleComplianceResponse MapCompliance(VehicleInsurancePolicy x) => new(x.Id, x.VehicleId, "Insurance", x.PolicyNumber, x.ProviderName, x.EffectiveFrom, x.ExpiryDate, FleetServiceSupport.DueStatus(x.ExpiryDate, DateOnly.FromDateTime(DateTime.UtcNow)), x.IsCurrent, x.PreviousRecordId, FleetServiceSupport.EncodeRowVersion(x.RowVersion));
    private static VehicleComplianceResponse MapCompliance(VehiclePeriodicInspection x) => new(x.Id, x.VehicleId, "Inspection", x.InspectionNumber, x.StationName, x.InspectionDate, x.ExpiryDate, FleetServiceSupport.DueStatus(x.ExpiryDate, DateOnly.FromDateTime(DateTime.UtcNow)), x.IsCurrent, x.PreviousRecordId, FleetServiceSupport.EncodeRowVersion(x.RowVersion));
    private static VehicleComplianceResponse MapCompliance(VehicleOperationCard x) => new(x.Id, x.VehicleId, "OperationCard", x.CardNumber, x.IssuingAuthority, x.IssueDate, x.ExpiryDate, FleetServiceSupport.DueStatus(x.ExpiryDate, DateOnly.FromDateTime(DateTime.UtcNow)), x.IsCurrent, x.PreviousRecordId, FleetServiceSupport.EncodeRowVersion(x.RowVersion));
    private static void AddDue(List<VehicleComplianceDueResponse> result, Guid vehicleId, string asset, string type, DateOnly? expiry, VehicleComplianceDueStatus ignored, DateOnly check, Guid? recordId = null)
    {
        var status = FleetServiceSupport.DueStatus(expiry, check);
        result.Add(new VehicleComplianceDueResponse(vehicleId, asset, type, recordId, expiry, status, expiry.HasValue ? expiry.Value.DayNumber - check.DayNumber : null));
    }
}
