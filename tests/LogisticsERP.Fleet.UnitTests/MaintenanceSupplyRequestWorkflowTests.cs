using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Maintenance;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class MaintenanceSupplyRequestWorkflowTests
{
    [Fact]
    public async Task VehicleAdminSubmitsOnceAndWarehouseApprovalIssuesAllLinesAndCompletesWorkOrderAtomically()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new TestRowVersionInterceptor())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var user = new TestUser();
        var service = new MaintenanceService(db, user, new TestTime(), new UnusedFileStorage());
        var now = DateTimeOffset.Parse("2026-09-06T10:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture);
        var maintenanceLocationId = Guid.NewGuid();
        var inventoryLocationId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        db.MaintenanceLocations.Add(new MaintenanceLocation
        {
            Id = maintenanceLocationId, Code = "TEST", NameAr = "مستودع الاختبار", NameEn = "Test warehouse",
            OperatingCityId = Guid.NewGuid(), LocationType = MaintenanceLocationType.WarehouseAndWorkshop,
            AllowsCompanyVehicles = true, InventoryEnabled = true
        });
        db.InventoryLocations.Add(new InventoryLocation
        {
            Id = inventoryLocationId, Code = "TEST-STOCK", NameAr = "مخزون الاختبار", NameEn = "Test stock",
            MaintenanceLocationId = maintenanceLocationId
        });
        db.Vehicles.Add(new Vehicle
        {
            Id = vehicleId, AssetNumber = "CAR-101", NormalizedAssetNumber = "CAR101",
            VehicleManufacturerId = Guid.NewGuid(), VehicleModelId = Guid.NewGuid(), VehicleType = VehicleType.Car
        });
        db.InventoryItems.Add(new InventoryItem
        {
            Id = itemId, Sku = "BRAKE-1", NormalizedSku = "BRAKE-1", NameAr = "فحمات فرامل", NameEn = "Brake pads",
            ItemType = InventoryItemType.SparePart, BaseUnitOfMeasure = InventoryUnitOfMeasure.Set,
            PurchaseUnitOfMeasure = InventoryUnitOfMeasure.Set
        });
        db.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(), InventoryItemId = itemId, InventoryLocationId = inventoryLocationId,
            QuantityOnHand = 10, ReportingAverageUnitCost = 25
        });
        db.StockCostLayers.Add(new StockCostLayer
        {
            Id = Guid.NewGuid(), InventoryItemId = itemId, InventoryLocationId = inventoryLocationId,
            ReceivedAtUtc = now.AddDays(-1), OriginalSequence = 1, OriginalQuantity = 10, RemainingQuantity = 10,
            BaseUnitOfMeasure = InventoryUnitOfMeasure.Set, UnitCost = 25, OriginalTotalCost = 250
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var created = await service.CreateWorkOrderAsync(new CreateMaintenanceWorkOrderRequest(
            MaintenanceServiceSubjectType.CompanyVehicle, vehicleId, null, maintenanceLocationId,
            MaintenanceType.Corrective, now, null, 12000, "Brake noise", null, null,
            new MaintenanceSupplyRequestInput(inventoryLocationId,
                [new InventorySupplyRequestLineInput(itemId, 2, MaintenanceUsageType.SparePart)])),
            TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error.Description);
        Assert.Equal(50, created.Value!.EstimatedCost);
        Assert.Equal(InventorySupplyRequestStatus.PendingWarehouseApproval, created.Value!.SupplyRequest!.Status);
        Assert.Equal(10, (await db.StockBalances.SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        Assert.Empty(db.MaintenanceMaterialUsages);

        var duplicate = await service.CreateWorkOrderAsync(new CreateMaintenanceWorkOrderRequest(
            MaintenanceServiceSubjectType.CompanyVehicle, vehicleId, null, maintenanceLocationId,
            MaintenanceType.Preventive, now.AddMinutes(1), null, 12001, "Second request", null, null),
            TestContext.Current.CancellationToken);

        Assert.True(duplicate.IsFailure);
        Assert.Equal(MaintenanceErrors.ActiveVehicleWorkOrderExists.Code, duplicate.Error.Code);

        var approved = await service.ApproveAndIssueSupplyRequestAsync(created.Value.SupplyRequest.Id,
            new InventorySupplyDecisionRequest(now.AddMinutes(5), created.Value.SupplyRequest.RowVersion, "Handed to vehicle admin"),
            TestContext.Current.CancellationToken);

        Assert.True(approved.IsSuccess, approved.Error.Description);
        Assert.Equal(InventorySupplyRequestStatus.ApprovedAndIssued, approved.Value!.Status);
        Assert.Equal(50, approved.Value.TotalIssuedCost);
        Assert.Equal(8, (await db.StockBalances.SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        Assert.Equal(8, (await db.StockCostLayers.SingleAsync(TestContext.Current.CancellationToken)).RemainingQuantity);
        Assert.Single(db.MaintenanceMaterialUsages);
        var completedWorkOrder = await service.GetWorkOrderAsync(created.Value.Id, TestContext.Current.CancellationToken);
        Assert.True(completedWorkOrder.IsSuccess, completedWorkOrder.Error.Description);
        Assert.Equal(MaintenanceWorkOrderStatus.Completed, completedWorkOrder.Value!.Status);
        Assert.Equal(now.AddMinutes(5), completedWorkOrder.Value.CompletedAtUtc);

        var closed = await service.ActOnWorkOrderAsync(created.Value.Id, "close",
            new MaintenanceWorkOrderActionRequest(now.AddMinutes(6), null, null, null, completedWorkOrder.Value.RowVersion),
            TestContext.Current.CancellationToken);

        Assert.True(closed.IsSuccess, closed.Error.Description);
        Assert.Equal(MaintenanceWorkOrderStatus.Closed, closed.Value!.Status);
        Assert.Contains(db.Notifications, x => x.RecipientUserId == user.UserId && x.EventType == "inventory.supply_request.approved");
    }

    [Fact]
    public async Task CompanyOilChangeIsSubmittedOnceAndCompletedByWarehouseApproval()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new TestRowVersionInterceptor())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var service = new MaintenanceService(db, new TestUser(), new TestTime(), new UnusedFileStorage());
        var now = DateTimeOffset.Parse("2026-09-06T10:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture);
        var maintenanceLocationId = Guid.NewGuid();
        var inventoryLocationId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var oilItemId = Guid.NewGuid();
        var filterItemId = Guid.NewGuid();
        var oilLayerId = Guid.NewGuid();
        db.MaintenanceLocations.Add(new MaintenanceLocation
        {
            Id = maintenanceLocationId, Code = "OIL-TEST", NameAr = "مستودع الزيت", NameEn = "Oil warehouse",
            OperatingCityId = Guid.NewGuid(), LocationType = MaintenanceLocationType.WarehouseAndWorkshop,
            AllowsCompanyVehicles = true, InventoryEnabled = true
        });
        db.InventoryLocations.Add(new InventoryLocation
        {
            Id = inventoryLocationId, Code = "OIL-STOCK", NameAr = "مخزون الزيت", NameEn = "Oil stock",
            MaintenanceLocationId = maintenanceLocationId
        });
        db.Vehicles.Add(new Vehicle
        {
            Id = vehicleId, AssetNumber = "CAR-OIL-1", NormalizedAssetNumber = "CAROIL1",
            VehicleManufacturerId = Guid.NewGuid(), VehicleModelId = Guid.NewGuid(), VehicleType = VehicleType.Car,
            CurrentOdometer = 1900
        });
        db.InventoryItems.AddRange(
            new InventoryItem
            {
                Id = oilItemId, Sku = "OIL-5W30", NormalizedSku = "OIL-5W30", NameAr = "زيت محرك", NameEn = "Engine oil",
                ItemType = InventoryItemType.Oil, BaseUnitOfMeasure = InventoryUnitOfMeasure.Liter,
                PurchaseUnitOfMeasure = InventoryUnitOfMeasure.Barrel
            },
            new InventoryItem
            {
                Id = filterItemId, Sku = "FILTER-1", NormalizedSku = "FILTER-1", NameAr = "فلتر زيت", NameEn = "Oil filter",
                ItemType = InventoryItemType.SparePart, BaseUnitOfMeasure = InventoryUnitOfMeasure.Piece,
                PurchaseUnitOfMeasure = InventoryUnitOfMeasure.Piece
            });
        db.StockBalances.AddRange(
            new StockBalance { Id = Guid.NewGuid(), InventoryItemId = oilItemId, InventoryLocationId = inventoryLocationId, QuantityOnHand = 10, ReportingAverageUnitCost = 10 },
            new StockBalance { Id = Guid.NewGuid(), InventoryItemId = filterItemId, InventoryLocationId = inventoryLocationId, QuantityOnHand = 2, ReportingAverageUnitCost = 25 });
        db.StockCostLayers.AddRange(
            new StockCostLayer
            {
                Id = oilLayerId, InventoryItemId = oilItemId, InventoryLocationId = inventoryLocationId,
                ReceivedAtUtc = now.AddDays(-2), OriginalSequence = 1, OriginalQuantity = 10, RemainingQuantity = 10,
                BaseUnitOfMeasure = InventoryUnitOfMeasure.Liter, UnitCost = 10, OriginalTotalCost = 100
            },
            new StockCostLayer
            {
                Id = Guid.NewGuid(), InventoryItemId = filterItemId, InventoryLocationId = inventoryLocationId,
                ReceivedAtUtc = now.AddDays(-1), OriginalSequence = 2, OriginalQuantity = 2, RemainingQuantity = 2,
                BaseUnitOfMeasure = InventoryUnitOfMeasure.Piece, UnitCost = 25, OriginalTotalCost = 50
            });
        db.OilBarrels.Add(new OilBarrel
        {
            Id = Guid.NewGuid(), BarrelNumber = "OB-1", PurchaseReceiptLineId = Guid.NewGuid(),
            InventoryItemId = oilItemId, InventoryLocationId = inventoryLocationId, StockCostLayerId = oilLayerId,
            PackageSequence = 1, NominalCapacityLiters = 10, RemainingLiters = 10, UnitCostPerLiter = 10,
            MaximumAllowedLossLiters = 0.2m, Status = OilBarrelStatus.Open, OpenedAtUtc = now.AddDays(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var created = await service.CreateWorkOrderAsync(new CreateMaintenanceWorkOrderRequest(
            MaintenanceServiceSubjectType.CompanyVehicle, vehicleId, null, maintenanceLocationId,
            MaintenanceType.OilChange, now, null, 2000, "تغيير زيت", null, null,
            OilChange: new OilChangeSupplyRequestInput(inventoryLocationId, oilItemId, true, filterItemId)),
            TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error.Description);
        Assert.Equal(65, created.Value!.EstimatedCost);
        Assert.Equal(2, created.Value.SupplyRequest!.Lines.Count);
        Assert.Contains(created.Value.SupplyRequest.Lines, x => x.MaintenanceUsageType == MaintenanceUsageType.Oil && x.RequestedQuantity == 4);
        Assert.Contains(created.Value.SupplyRequest.Lines, x => x.MaintenanceUsageType == MaintenanceUsageType.OilFilter && x.RequestedQuantity == 1);

        var approved = await service.ApproveAndIssueSupplyRequestAsync(created.Value.SupplyRequest.Id,
            new InventorySupplyDecisionRequest(now.AddMinutes(10), created.Value.SupplyRequest.RowVersion, "تم الصرف"),
            TestContext.Current.CancellationToken);

        Assert.True(approved.IsSuccess, approved.Error.Description);
        Assert.Equal(65, approved.Value!.TotalIssuedCost);
        var workOrder = await db.MaintenanceWorkOrders.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(MaintenanceWorkOrderStatus.Completed, workOrder.Status);
        Assert.Equal(65, workOrder.ActualMaterialCost);
        Assert.Equal(0, workOrder.ActualLaborCost);
        Assert.Equal(65, workOrder.ActualTotalCost);
        var operation = await db.OilChangeOperations.SingleAsync(TestContext.Current.CancellationToken);
        Assert.True(operation.OilFilterChanged);
        Assert.Equal(0, operation.LaborCost);
        Assert.Equal(2000, operation.OdometerAtChange);
    }

    [Fact]
    public async Task WorkOrderListsSeparateCompanyVehiclesFromOutsideVehicles()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .AddInterceptors(new TestRowVersionInterceptor())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var service = new MaintenanceService(db, new TestUser(), new TestTime(), new UnusedFileStorage());
        var locationId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        db.MaintenanceLocations.Add(new MaintenanceLocation
        {
            Id = locationId, Code = "MIXED", NameAr = "موقع مختلط", NameEn = "Mixed location",
            OperatingCityId = Guid.NewGuid(), AllowsCompanyVehicles = true, AllowsExternalVehicles = true
        });
        db.Vehicles.Add(new Vehicle
        {
            Id = vehicleId, AssetNumber = "INTERNAL-1", NormalizedAssetNumber = "INTERNAL1",
            VehicleManufacturerId = Guid.NewGuid(), VehicleModelId = Guid.NewGuid()
        });
        var companyOrderId = Guid.NewGuid();
        var externalOrderId = Guid.NewGuid();
        db.MaintenanceWorkOrders.AddRange(
            new MaintenanceWorkOrder
            {
                Id = companyOrderId, WorkOrderNumber = "MWO-INTERNAL", ServiceSubjectType = MaintenanceServiceSubjectType.CompanyVehicle,
                VehicleId = vehicleId, MaintenanceLocationId = locationId, MaintenanceType = MaintenanceType.Corrective,
                OpenedAtUtc = DateTimeOffset.UtcNow
            },
            new MaintenanceWorkOrder
            {
                Id = externalOrderId, WorkOrderNumber = "MWO-EXTERNAL", ServiceSubjectType = MaintenanceServiceSubjectType.ExternalVehicle,
                MaintenanceLocationId = locationId, MaintenanceType = MaintenanceType.Corrective,
                OpenedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
            });
        db.ExternalVehicleSnapshots.Add(new ExternalVehicleSnapshot
        {
            MaintenanceWorkOrderId = externalOrderId, PlateOrReference = "EXT-1"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var company = await service.GetWorkOrdersAsync(MaintenanceServiceSubjectType.CompanyVehicle, null, null, null, TestContext.Current.CancellationToken);
        var external = await service.GetWorkOrdersAsync(MaintenanceServiceSubjectType.ExternalVehicle, null, null, null, TestContext.Current.CancellationToken);

        Assert.True(company.IsSuccess);
        Assert.Equal(companyOrderId, Assert.Single(company.Value!).Id);
        Assert.True(external.IsSuccess);
        Assert.Equal(externalOrderId, Assert.Single(external.Value!).Id);
    }

    [Fact]
    public async Task InternalOilChangeCannotPostLaborCostOutsideWarehouseFlow()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new TestRowVersionInterceptor())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var service = new MaintenanceService(db, new TestUser(), new TestTime(), new UnusedFileStorage());
        var order = new MaintenanceWorkOrder
        {
            Id = Guid.NewGuid(), WorkOrderNumber = "MWO-OIL-LEGACY", ServiceSubjectType = MaintenanceServiceSubjectType.CompanyVehicle,
            VehicleId = Guid.NewGuid(), MaintenanceLocationId = Guid.NewGuid(), MaintenanceType = MaintenanceType.OilChange,
            OpenedAtUtc = DateTimeOffset.UtcNow
        };
        db.MaintenanceWorkOrders.Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await service.CompleteOilChangeAsync(order.Id, new CompleteOilChangeRequest(
            DateTimeOffset.UtcNow, 1000, Guid.NewGuid(), Guid.NewGuid(), null, false, null, null,
            LaborCost: 50, OtherCost: 0, Notes: null, WorkOrderRowVersion: Convert.ToBase64String(order.RowVersion)),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(MaintenanceErrors.LaborCostExternalVehiclesOnly.Code, result.Error.Code);
    }

    [Fact]
    public async Task RiderRequestAlsoWaitsForWarehouseBeforeCreatingTheIssue()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new TestRowVersionInterceptor())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var service = new MaintenanceService(db, new TestUser(), new TestTime(), new UnusedFileStorage());
        var now = DateTimeOffset.Parse("2026-09-06T10:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture);
        var maintenanceLocationId = Guid.NewGuid();
        var inventoryLocationId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var riderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        db.MaintenanceLocations.Add(new MaintenanceLocation
        {
            Id = maintenanceLocationId, Code = "RIDER-TEST", NameAr = "مستودع الرايدر", NameEn = "Rider warehouse",
            OperatingCityId = Guid.NewGuid(), LocationType = MaintenanceLocationType.Warehouse, InventoryEnabled = true
        });
        db.InventoryLocations.Add(new InventoryLocation
        {
            Id = inventoryLocationId, Code = "RIDER-STOCK", NameAr = "مخزون الرايدر", NameEn = "Rider stock",
            MaintenanceLocationId = maintenanceLocationId
        });
        db.Employees.Add(new Employee { Id = employeeId, FullNameAr = "رايدر اختبار" });
        db.RiderProfiles.Add(new RiderProfile { Id = riderId, EmployeeId = employeeId });
        db.InventoryItems.Add(new InventoryItem
        {
            Id = itemId, Sku = "HELMET-1", NormalizedSku = "HELMET-1", NameAr = "خوذة", NameEn = "Helmet",
            ItemType = InventoryItemType.RiderAccessory, BaseUnitOfMeasure = InventoryUnitOfMeasure.Piece,
            PurchaseUnitOfMeasure = InventoryUnitOfMeasure.Piece
        });
        db.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(), InventoryItemId = itemId, InventoryLocationId = inventoryLocationId,
            QuantityOnHand = 3, ReportingAverageUnitCost = 80
        });
        db.StockCostLayers.Add(new StockCostLayer
        {
            Id = Guid.NewGuid(), InventoryItemId = itemId, InventoryLocationId = inventoryLocationId,
            ReceivedAtUtc = now.AddDays(-1), OriginalSequence = 1, OriginalQuantity = 3, RemainingQuantity = 3,
            BaseUnitOfMeasure = InventoryUnitOfMeasure.Piece, UnitCost = 80, OriginalTotalCost = 240
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var created = await service.CreateRiderSupplyRequestAsync(new CreateRiderSupplyRequest(
            riderId, inventoryLocationId, now,
            [new InventorySupplyRequestLineInput(itemId, 1, ExpectedReturn: true)], "New rider equipment"),
            TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error.Description);
        Assert.Empty(db.RiderInventoryIssues);
        Assert.Equal(3, (await db.StockBalances.SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);

        var approved = await service.ApproveAndIssueSupplyRequestAsync(created.Value!.Id,
            new InventorySupplyDecisionRequest(now.AddMinutes(1), created.Value.RowVersion), TestContext.Current.CancellationToken);

        Assert.True(approved.IsSuccess, approved.Error.Description);
        Assert.NotNull(approved.Value!.RiderInventoryIssueId);
        Assert.Equal(2, (await db.StockBalances.SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        var issueLine = await db.RiderInventoryIssueLines.SingleAsync(TestContext.Current.CancellationToken);
        Assert.True(issueLine.ExpectedReturn);
        Assert.Equal(80, issueLine.TotalCost);
    }

    private sealed class TestUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "supply-request-test";
    }

    private sealed class TestTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-09-06T10:00:00Z", global::System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class UnusedFileStorage : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file, long maximumBytes, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure<StoredPrivateFile>(PrivateFileErrors.InvalidFile));
        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType, string downloadFileName, long length, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure<PrivateFileDownload>(PrivateFileErrors.FileMissing));
        public void DeleteBestEffort(string storagePath) { }
    }

    private sealed class TestRowVersionInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            foreach (var entry in eventData.Context!.ChangeTracker.Entries<AuditableEntity>()
                         .Where(x => x.State is EntityState.Added or EntityState.Modified))
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            return ValueTask.FromResult(result);
        }
    }
}
