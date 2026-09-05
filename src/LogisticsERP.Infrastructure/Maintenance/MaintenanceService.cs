using System.Security.Cryptography;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Maintenance;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Maintenance;

internal sealed partial class MaintenanceService : IMaintenanceService
{
    private const long MaximumBillFileSize = 10 * 1024 * 1024;
    private readonly ApplicationDbContext dbContext;
    private readonly ICurrentUser currentUser;
    private readonly TimeProvider timeProvider;
    private readonly IPrivateFileStorage fileStorage;

    public MaintenanceService(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IPrivateFileStorage fileStorage)
    {
        this.dbContext = dbContext;
        this.currentUser = currentUser;
        this.timeProvider = timeProvider;
        this.fileStorage = fileStorage;
    }

    public async Task<Result<IReadOnlyList<MaintenanceLocationResponse>>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await (from location in dbContext.MaintenanceLocations.AsNoTracking()
                          join operatingCity in dbContext.OperatingCities.AsNoTracking() on location.OperatingCityId equals operatingCity.Id
                          join city in dbContext.GlobalCities.AsNoTracking() on operatingCity.GlobalCityId equals city.Id
                          orderby location.Code
                          select new { Location = location, CityNameAr = city.NameAr })
            .ToArrayAsync(cancellationToken);

        return Result.Success<IReadOnlyList<MaintenanceLocationResponse>>(rows.Select(row =>
            MapLocation(row.Location, row.CityNameAr)).ToArray());
    }

    public async Task<Result<MaintenanceLocationResponse>> UpsertLocationAsync(Guid? id, MaintenanceLocationRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null) return Result.Failure<MaintenanceLocationResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (!ValidLocationRequest(request)) return Result.Failure<MaintenanceLocationResponse>(MaintenanceErrors.InvalidRequest);
        if (!await dbContext.OperatingCities.AsNoTracking().AnyAsync(x => x.Id == request.OperatingCityId && x.Status == CatalogStatus.Active, cancellationToken))
            return Result.Failure<MaintenanceLocationResponse>(MaintenanceErrors.InvalidLocation);

        var code = NormalizeCode(request.Code);
        if (await dbContext.MaintenanceLocations.AsNoTracking().AnyAsync(x => x.Code == code && (!id.HasValue || x.Id != id.Value), cancellationToken))
            return Result.Failure<MaintenanceLocationResponse>(MaintenanceErrors.Duplicate);

        MaintenanceLocation item;
        if (id.HasValue)
        {
            item = await dbContext.MaintenanceLocations.SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken) ?? null!;
            if (item is null) return Result.Failure<MaintenanceLocationResponse>(MaintenanceErrors.NotFound);
            if (!MatchesRowVersion(item.RowVersion, request.RowVersion)) return Result.Failure<MaintenanceLocationResponse>(MaintenanceErrors.ConcurrencyConflict);
        }
        else
        {
            item = new MaintenanceLocation();
            dbContext.MaintenanceLocations.Add(item);
        }

        item.Code = code;
        item.NameAr = request.NameAr.Trim();
        item.NameEn = request.NameEn.Trim();
        item.OperatingCityId = request.OperatingCityId;
        item.LocationType = request.LocationType;
        item.AllowsCompanyVehicles = request.AllowsCompanyVehicles;
        item.AllowsExternalVehicles = request.AllowsExternalVehicles;
        item.AllowsSparePartSales = request.AllowsSparePartSales;
        item.AllowsPaidExternalRepairs = request.AllowsPaidExternalRepairs;
        item.InventoryEnabled = request.InventoryEnabled;
        item.Address = TrimOrNull(request.Address);
        item.Latitude = request.Latitude;
        item.Longitude = request.Longitude;
        item.Notes = TrimOrNull(request.Notes);
        item.Status = CatalogStatus.Active;

        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Result.Failure<MaintenanceLocationResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<MaintenanceLocationResponse>(MaintenanceErrors.Duplicate); }

        var cityName = await (from operatingCity in dbContext.OperatingCities.AsNoTracking()
                              join city in dbContext.GlobalCities.AsNoTracking() on operatingCity.GlobalCityId equals city.Id
                              where operatingCity.Id == item.OperatingCityId
                              select city.NameAr).SingleAsync(cancellationToken);
        return Result.Success(MapLocation(item, cityName));
    }

    public async Task<Result<IReadOnlyList<InventoryItemResponse>>> GetItemsAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.InventoryItems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalized = NormalizeCode(term);
            query = query.Where(x => x.NormalizedSku.Contains(normalized) || x.NameAr.Contains(term) || x.NameEn.Contains(term));
        }

        var items = await query.OrderBy(x => x.Sku).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<InventoryItemResponse>>(items.Select(MapItem).ToArray());
    }

    public async Task<Result<InventoryItemResponse>> UpsertItemAsync(Guid? id, InventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null) return Result.Failure<InventoryItemResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (!ValidItemRequest(request)) return Result.Failure<InventoryItemResponse>(MaintenanceErrors.InvalidRequest);
        var sku = request.Sku.Trim();
        var normalized = NormalizeCode(sku);
        if (await dbContext.InventoryItems.AsNoTracking().AnyAsync(x => x.NormalizedSku == normalized && (!id.HasValue || x.Id != id.Value), cancellationToken))
            return Result.Failure<InventoryItemResponse>(MaintenanceErrors.Duplicate);

        InventoryItem item;
        if (id.HasValue)
        {
            item = await dbContext.InventoryItems.SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken) ?? null!;
            if (item is null) return Result.Failure<InventoryItemResponse>(MaintenanceErrors.NotFound);
            if (!MatchesRowVersion(item.RowVersion, request.RowVersion)) return Result.Failure<InventoryItemResponse>(MaintenanceErrors.ConcurrencyConflict);
            var hasMovements = await dbContext.StockMovementLines.AsNoTracking().AnyAsync(x => x.InventoryItemId == item.Id, cancellationToken);
            if (hasMovements && item.BaseUnitOfMeasure != request.BaseUnitOfMeasure)
                return Result.Failure<InventoryItemResponse>(MaintenanceErrors.InvalidInventoryItem);
        }
        else
        {
            item = new InventoryItem();
            dbContext.InventoryItems.Add(item);
        }

        item.Sku = sku;
        item.NormalizedSku = normalized;
        item.Barcode = TrimOrNull(request.Barcode);
        item.ItemType = request.ItemType;
        item.NameAr = request.NameAr.Trim();
        item.NameEn = request.NameEn.Trim();
        item.DescriptionAr = TrimOrNull(request.DescriptionAr);
        item.DescriptionEn = TrimOrNull(request.DescriptionEn);
        item.BaseUnitOfMeasure = request.BaseUnitOfMeasure;
        item.PurchaseUnitOfMeasure = request.PurchaseUnitOfMeasure;
        item.DefaultPackageQuantity = request.DefaultPackageQuantity;
        item.MinimumStockLevel = request.MinimumStockLevel;
        item.ReorderQuantity = request.ReorderQuantity;
        item.IsSerialized = request.IsSerialized;
        item.IsLotTracked = request.IsLotTracked;
        item.Status = CatalogStatus.Active;

        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Result.Failure<InventoryItemResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<InventoryItemResponse>(MaintenanceErrors.Duplicate); }
        return Result.Success(MapItem(item));
    }

    public async Task<Result<IReadOnlyList<MaintenanceSupplierResponse>>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        var items = await dbContext.MaintenanceSuppliers.AsNoTracking().OrderBy(x => x.SupplierNumber).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<MaintenanceSupplierResponse>>(items.Select(MapSupplier).ToArray());
    }

    public async Task<Result<MaintenanceSupplierResponse>> UpsertSupplierAsync(Guid? id, MaintenanceSupplierRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null) return Result.Failure<MaintenanceSupplierResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (string.IsNullOrWhiteSpace(request.SupplierNumber) || string.IsNullOrWhiteSpace(request.LegalNameAr) || string.IsNullOrWhiteSpace(request.LegalNameEn) || request.PaymentTermsDays < 0)
            return Result.Failure<MaintenanceSupplierResponse>(MaintenanceErrors.InvalidRequest);
        var number = NormalizeCode(request.SupplierNumber);
        if (await dbContext.MaintenanceSuppliers.AsNoTracking().AnyAsync(x => x.SupplierNumber == number && (!id.HasValue || x.Id != id.Value), cancellationToken))
            return Result.Failure<MaintenanceSupplierResponse>(MaintenanceErrors.Duplicate);

        MaintenanceSupplier item;
        if (id.HasValue)
        {
            item = await dbContext.MaintenanceSuppliers.SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken) ?? null!;
            if (item is null) return Result.Failure<MaintenanceSupplierResponse>(MaintenanceErrors.NotFound);
            if (!MatchesRowVersion(item.RowVersion, request.RowVersion)) return Result.Failure<MaintenanceSupplierResponse>(MaintenanceErrors.ConcurrencyConflict);
        }
        else
        {
            item = new MaintenanceSupplier();
            dbContext.MaintenanceSuppliers.Add(item);
        }

        item.SupplierNumber = number;
        item.LegalNameAr = request.LegalNameAr.Trim();
        item.LegalNameEn = request.LegalNameEn.Trim();
        item.VatNumber = TrimOrNull(request.VatNumber);
        item.CommercialRegistrationNumber = TrimOrNull(request.CommercialRegistrationNumber);
        item.ContactName = TrimOrNull(request.ContactName);
        item.Phone = TrimOrNull(request.Phone);
        item.Email = TrimOrNull(request.Email);
        item.Address = TrimOrNull(request.Address);
        item.PaymentTermsDays = request.PaymentTermsDays;
        item.Notes = TrimOrNull(request.Notes);
        item.Status = CatalogStatus.Active;

        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Result.Failure<MaintenanceSupplierResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<MaintenanceSupplierResponse>(MaintenanceErrors.Duplicate); }
        return Result.Success(MapSupplier(item));
    }

    public async Task<Result<MaintenanceWorkOrderResponse>> CreateWorkOrderAsync(CreateMaintenanceWorkOrderRequest request, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.OpenedAtUtc == default || request.EstimatedCost < 0 || !Enum.IsDefined(request.ServiceSubjectType) || !Enum.IsDefined(request.MaintenanceType))
            return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.InvalidRequest);

        var location = await dbContext.MaintenanceLocations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.MaintenanceLocationId && x.Status == CatalogStatus.Active, cancellationToken);
        if (location is null || !MaintenanceBusinessRules.CanServe(location.AllowsCompanyVehicles, location.AllowsExternalVehicles, request.ServiceSubjectType))
            return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.InvalidLocation);

        if (request.MaintenanceType == MaintenanceType.PartSaleOnly && (request.ServiceSubjectType != MaintenanceServiceSubjectType.ExternalVehicle || !location.AllowsSparePartSales))
            return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.InvalidLocation);

        Guid? assignmentId = null;
        Guid? riderId = null;
        if (request.ServiceSubjectType == MaintenanceServiceSubjectType.CompanyVehicle)
        {
            if (!request.VehicleId.HasValue || request.ExternalVehicle is not null)
                return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.InvalidSubject);
            var vehicleExists = await dbContext.Vehicles.AsNoTracking().AnyAsync(x => x.Id == request.VehicleId.Value, cancellationToken);
            if (!vehicleExists) return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.NotFound);
            if (request.VehicleIssueId.HasValue && !await dbContext.VehicleIssues.AsNoTracking().AnyAsync(x => x.Id == request.VehicleIssueId.Value && x.VehicleId == request.VehicleId.Value, cancellationToken))
                return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.InvalidSubject);
            var assignment = await dbContext.RiderVehicleAssignments.AsNoTracking()
                .Where(x => x.VehicleId == request.VehicleId.Value && x.StartedAtUtc <= request.OpenedAtUtc && (!x.EndedAtUtc.HasValue || x.EndedAtUtc >= request.OpenedAtUtc))
                .OrderByDescending(x => x.StartedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            assignmentId = assignment?.Id;
            riderId = assignment?.RiderProfileId;
        }
        else
        {
            if (request.VehicleId.HasValue || request.VehicleIssueId.HasValue || request.ExternalVehicle is null || !HasExternalReference(request.ExternalVehicle))
                return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.InvalidSubject);
        }

        var id = Guid.CreateVersion7();
        var item = new MaintenanceWorkOrder
        {
            Id = id,
            WorkOrderNumber = NewNumber("MWO", request.OpenedAtUtc, id),
            ServiceSubjectType = request.ServiceSubjectType,
            VehicleId = request.VehicleId,
            VehicleIssueId = request.VehicleIssueId,
            MaintenanceLocationId = request.MaintenanceLocationId,
            MaintenanceType = request.MaintenanceType,
            OpenedAtUtc = request.OpenedAtUtc,
            ScheduledAtUtc = request.ScheduledAtUtc,
            OdometerAtOpen = request.OdometerAtOpen,
            Diagnosis = TrimOrNull(request.Diagnosis),
            OpenedByUserId = actor.Value,
            RiderVehicleAssignmentId = assignmentId,
            AttributedRiderProfileId = riderId,
            EstimatedCost = request.EstimatedCost,
            Notes = TrimOrNull(request.Notes)
        };
        dbContext.MaintenanceWorkOrders.Add(item);
        if (request.ExternalVehicle is not null)
        {
            dbContext.ExternalVehicleSnapshots.Add(new ExternalVehicleSnapshot
            {
                MaintenanceWorkOrderId = id,
                PlateOrReference = TrimOrNull(request.ExternalVehicle.PlateOrReference),
                VehicleType = request.ExternalVehicle.VehicleType,
                CustomerName = TrimOrNull(request.ExternalVehicle.CustomerName),
                CustomerPhone = TrimOrNull(request.ExternalVehicle.CustomerPhone),
                Notes = TrimOrNull(request.ExternalVehicle.Notes)
            });
        }

        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.InvalidRequest); }
        return await GetWorkOrderAsync(id, cancellationToken);
    }

    public async Task<Result<MaintenanceWorkOrderResponse>> GetWorkOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.MaintenanceWorkOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.NotFound);
        return Result.Success(await MapWorkOrderAsync(item, cancellationToken));
    }

    public async Task<Result<IReadOnlyList<MaintenanceWorkOrderResponse>>> GetWorkOrdersAsync(Guid? maintenanceLocationId, Guid? vehicleId, string? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.MaintenanceWorkOrders.AsNoTracking();
        if (maintenanceLocationId.HasValue) query = query.Where(x => x.MaintenanceLocationId == maintenanceLocationId.Value);
        if (vehicleId.HasValue) query = query.Where(x => x.VehicleId == vehicleId.Value);
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<MaintenanceWorkOrderStatus>(status, true, out var parsed))
                return Result.Failure<IReadOnlyList<MaintenanceWorkOrderResponse>>(MaintenanceErrors.InvalidRequest);
            query = query.Where(x => x.Status == parsed);
        }
        var items = await query.OrderByDescending(x => x.OpenedAtUtc).Take(500).ToArrayAsync(cancellationToken);
        var result = new List<MaintenanceWorkOrderResponse>(items.Length);
        foreach (var item in items) result.Add(await MapWorkOrderAsync(item, cancellationToken));
        return Result.Success<IReadOnlyList<MaintenanceWorkOrderResponse>>(result);
    }

    public async Task<Result<MaintenanceWorkOrderResponse>> ActOnWorkOrderAsync(Guid id, string action, MaintenanceWorkOrderActionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.CurrentUserUnavailable);
        var item = await dbContext.MaintenanceWorkOrders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.NotFound);
        if (!MatchesRowVersion(item.RowVersion, request.RowVersion)) return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.ConcurrencyConflict);

        switch (action.Trim().ToLowerInvariant())
        {
            case "start" when item.Status == MaintenanceWorkOrderStatus.Open:
                item.Status = MaintenanceWorkOrderStatus.InProgress;
                item.StartedAtUtc = request.OccurredAtUtc;
                break;
            case "complete" when item.Status == MaintenanceWorkOrderStatus.InProgress && item.MaintenanceType != MaintenanceType.OilChange:
                item.Status = MaintenanceWorkOrderStatus.Completed;
                item.CompletedAtUtc = request.OccurredAtUtc;
                item.WorkPerformed = TrimOrNull(request.WorkPerformed);
                item.QualityCheckNotes = TrimOrNull(request.QualityCheckNotes);
                break;
            case "close" when item.Status == MaintenanceWorkOrderStatus.Completed:
                item.Status = MaintenanceWorkOrderStatus.Closed;
                item.ClosedAtUtc = request.OccurredAtUtc;
                item.ClosedByUserId = actor.Value;
                break;
            case "cancel" when item.Status == MaintenanceWorkOrderStatus.Open:
                if (await dbContext.MaintenanceMaterialUsages.AsNoTracking().AnyAsync(x => x.MaintenanceWorkOrderId == id, cancellationToken))
                    return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.InvalidState);
                item.Status = MaintenanceWorkOrderStatus.Cancelled;
                item.ClosedAtUtc = request.OccurredAtUtc;
                item.ClosedByUserId = actor.Value;
                break;
            default:
                return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.InvalidState);
        }
        item.Notes = TrimOrNull(request.Notes) ?? item.Notes;

        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Result.Failure<MaintenanceWorkOrderResponse>(MaintenanceErrors.ConcurrencyConflict); }
        return await GetWorkOrderAsync(id, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<MaintenancePlanResponse>>> GetPlansAsync(CancellationToken cancellationToken = default)
    {
        var items = await dbContext.MaintenancePlans.AsNoTracking().OrderBy(x => x.Code).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<MaintenancePlanResponse>>(items.Select(MapPlan).ToArray());
    }

    public async Task<Result<MaintenancePlanResponse>> UpsertPlanAsync(Guid? id, MaintenancePlanRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null) return Result.Failure<MaintenancePlanResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (!ValidPlan(request)) return Result.Failure<MaintenancePlanResponse>(MaintenanceErrors.InvalidRequest);
        var code = NormalizeCode(request.Code);
        if (await dbContext.MaintenancePlans.AsNoTracking().AnyAsync(x => x.Code == code && (!id.HasValue || x.Id != id.Value), cancellationToken))
            return Result.Failure<MaintenancePlanResponse>(MaintenanceErrors.Duplicate);

        MaintenancePlan item;
        if (id.HasValue)
        {
            item = await dbContext.MaintenancePlans.SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken) ?? null!;
            if (item is null) return Result.Failure<MaintenancePlanResponse>(MaintenanceErrors.NotFound);
            if (!MatchesRowVersion(item.RowVersion, request.RowVersion)) return Result.Failure<MaintenancePlanResponse>(MaintenanceErrors.ConcurrencyConflict);
        }
        else
        {
            item = new MaintenancePlan();
            dbContext.MaintenancePlans.Add(item);
        }

        item.Code = code;
        item.NameAr = request.NameAr.Trim();
        item.NameEn = request.NameEn.Trim();
        item.VehicleModelId = request.VehicleModelId;
        item.VehicleType = request.VehicleType;
        item.TriggerType = request.TriggerType;
        item.IntervalDays = request.IntervalDays;
        item.IntervalKilometers = request.IntervalKilometers;
        item.ReminderAfterKilometers = request.ReminderAfterKilometers;
        item.MaximumAfterKilometers = request.MaximumAfterKilometers;
        item.AlertDaysBefore = request.AlertDaysBefore;
        item.AlertKilometersBefore = request.AlertKilometersBefore;
        item.InventoryItemId = request.InventoryItemId;
        item.DefaultOilQuantityLiters = request.DefaultOilQuantityLiters;
        item.ChecklistJson = TrimOrNull(request.ChecklistJson);
        item.Status = CatalogStatus.Active;
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Result.Failure<MaintenancePlanResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<MaintenancePlanResponse>(MaintenanceErrors.InvalidRequest); }
        return Result.Success(MapPlan(item));
    }

    private async Task<MaintenanceWorkOrderResponse> MapWorkOrderAsync(MaintenanceWorkOrder item, CancellationToken cancellationToken)
    {
        var locationName = await dbContext.MaintenanceLocations.AsNoTracking().Where(x => x.Id == item.MaintenanceLocationId).Select(x => x.NameAr).SingleAsync(cancellationToken);
        var assetNumber = item.VehicleId.HasValue
            ? await dbContext.Vehicles.AsNoTracking().Where(x => x.Id == item.VehicleId.Value).Select(x => x.AssetNumber).SingleOrDefaultAsync(cancellationToken)
            : null;
        var external = await dbContext.ExternalVehicleSnapshots.AsNoTracking().SingleOrDefaultAsync(x => x.MaintenanceWorkOrderId == item.Id, cancellationToken);
        return new MaintenanceWorkOrderResponse(
            item.Id, item.WorkOrderNumber, item.ServiceSubjectType, item.VehicleId, assetNumber, item.VehicleIssueId,
            item.MaintenanceLocationId, locationName, item.MaintenanceType, item.Status, item.OpenedAtUtc,
            item.ScheduledAtUtc, item.StartedAtUtc, item.CompletedAtUtc, item.OdometerAtOpen, item.OdometerAtCompletion,
            item.RiderVehicleAssignmentId, item.AttributedRiderProfileId, item.EstimatedCost, item.ActualMaterialCost,
            item.ActualLaborCost, item.ActualOtherCost, item.ActualTotalCost,
            external is null ? null : new ExternalVehicleSnapshotResponse(external.PlateOrReference, external.VehicleType, external.CustomerName, external.CustomerPhone, external.Notes),
            item.Notes, EncodeRowVersion(item.RowVersion));
    }

    private static MaintenanceLocationResponse MapLocation(MaintenanceLocation item, string cityNameAr) => new(
        item.Id, item.Code, item.NameAr, item.NameEn, item.OperatingCityId, cityNameAr, item.LocationType,
        item.AllowsCompanyVehicles, item.AllowsExternalVehicles, item.AllowsSparePartSales,
        item.AllowsPaidExternalRepairs, item.InventoryEnabled, item.Status, item.Address, item.Notes,
        EncodeRowVersion(item.RowVersion));

    private static InventoryItemResponse MapItem(InventoryItem item) => new(
        item.Id, item.Sku, item.Barcode, item.ItemType, item.NameAr, item.NameEn, item.BaseUnitOfMeasure,
        item.PurchaseUnitOfMeasure, item.DefaultPackageQuantity, item.MinimumStockLevel, item.ReorderQuantity,
        item.Status, EncodeRowVersion(item.RowVersion));

    private static MaintenanceSupplierResponse MapSupplier(MaintenanceSupplier item) => new(
        item.Id, item.SupplierNumber, item.LegalNameAr, item.LegalNameEn, item.VatNumber,
        item.CommercialRegistrationNumber, item.Phone, item.Status, item.Notes, EncodeRowVersion(item.RowVersion));

    private static MaintenancePlanResponse MapPlan(MaintenancePlan item) => new(
        item.Id, item.Code, item.NameAr, item.NameEn, item.VehicleModelId, item.VehicleType, item.TriggerType,
        item.IntervalDays, item.IntervalKilometers, item.ReminderAfterKilometers, item.MaximumAfterKilometers,
        item.DefaultOilQuantityLiters, item.Status, EncodeRowVersion(item.RowVersion));

    private static bool ValidLocationRequest(MaintenanceLocationRequest request) =>
        !string.IsNullOrWhiteSpace(request.Code) && !string.IsNullOrWhiteSpace(request.NameAr) && !string.IsNullOrWhiteSpace(request.NameEn)
        && Enum.IsDefined(request.LocationType) && request.Latitude is >= -90 and <= 90 or null && request.Longitude is >= -180 and <= 180 or null;

    private static bool ValidItemRequest(InventoryItemRequest request) =>
        !string.IsNullOrWhiteSpace(request.Sku) && !string.IsNullOrWhiteSpace(request.NameAr) && !string.IsNullOrWhiteSpace(request.NameEn)
        && Enum.IsDefined(request.ItemType) && Enum.IsDefined(request.BaseUnitOfMeasure) && Enum.IsDefined(request.PurchaseUnitOfMeasure)
        && request.MinimumStockLevel >= 0 && request.ReorderQuantity >= 0 && request.DefaultPackageQuantity is null or > 0
        && (request.ItemType != InventoryItemType.Oil || request.BaseUnitOfMeasure == InventoryUnitOfMeasure.Liter);

    private static bool ValidPlan(MaintenancePlanRequest request) =>
        !string.IsNullOrWhiteSpace(request.Code) && !string.IsNullOrWhiteSpace(request.NameAr) && !string.IsNullOrWhiteSpace(request.NameEn)
        && request.VehicleModelId.HasValue != request.VehicleType.HasValue
        && (request.TriggerType != MaintenanceTriggerType.OdometerWindow
            || request.ReminderAfterKilometers is > 0 && request.MaximumAfterKilometers > request.ReminderAfterKilometers)
        && request.DefaultOilQuantityLiters is null or > 0;

    private static bool HasExternalReference(ExternalVehicleSnapshotRequest request) =>
        !string.IsNullOrWhiteSpace(request.PlateOrReference) || !string.IsNullOrWhiteSpace(request.CustomerName) || !string.IsNullOrWhiteSpace(request.CustomerPhone);

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant().Replace(" ", string.Empty, StringComparison.Ordinal);
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string EncodeRowVersion(byte[] value) => Convert.ToBase64String(value);
    private static bool MatchesRowVersion(byte[] current, string? supplied)
    {
        if (string.IsNullOrWhiteSpace(supplied)) return false;
        try { return CryptographicOperations.FixedTimeEquals(current, Convert.FromBase64String(supplied)); }
        catch (FormatException) { return false; }
    }

    private static string NewNumber(string prefix, DateTimeOffset occurredAt, Guid id) =>
        $"{prefix}-{occurredAt:yyyyMMdd}-{id.ToString("N")[^8..]}".ToUpperInvariant();

    private DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
