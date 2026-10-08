using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Maintenance;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class OilBarrelVehicleTypeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CompletedOilChangeVehicleCorrectionRejectsAnotherTypeAndWorkOrderRecords()
    {
        await using var db = CreateContext();
        var (service, car, motorcycle, item, _) = await SeedAsync(db);
        var direct = new OilChangeOperation
        {
            VehicleId = car.Id, VehicleTypeSnapshot = VehicleType.Car,
            OilInventoryItemId = item.Id, OilMaterialUsageId = Guid.NewGuid(),
            PerformedAtUtc = Now, OdometerAtChange = 100, OilQuantityLiters = 3.5m
        };
        var workOrder = new OilChangeOperation
        {
            VehicleId = car.Id, MaintenanceWorkOrderId = Guid.NewGuid(), VehicleTypeSnapshot = VehicleType.Car,
            OilInventoryItemId = item.Id, OilMaterialUsageId = Guid.NewGuid(),
            PerformedAtUtc = Now, OdometerAtChange = 100, OilQuantityLiters = 3.5m
        };
        db.OilChangeOperations.AddRange(direct, workOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var differentType = await service.CorrectCompletedOilChangeVehicleAsync(direct.Id,
            new(motorcycle.Id, "Wrong vehicle selected"), TestContext.Current.CancellationToken);
        var linkedWorkOrder = await service.CorrectCompletedOilChangeVehicleAsync(workOrder.Id,
            new(motorcycle.Id, "Wrong vehicle selected"), TestContext.Current.CancellationToken);

        Assert.Equal(MaintenanceErrors.OilChangeCorrectionVehicleType.Code, differentType.Error.Code);
        Assert.Equal(MaintenanceErrors.OilChangeCorrectionWorkOrder.Code, linkedWorkOrder.Error.Code);
        Assert.Equal(car.Id, direct.VehicleId);
        Assert.Empty(db.AuditEntries);
    }

    [Fact]
    public async Task TwoTypesCanOpenAndEachConsumesItsOwnBarrelAndCostLayer()
    {
        await using var db = CreateContext();
        var (service, car, motorcycle, item, location) = await SeedAsync(db);
        var motorcycleBarrel = AddBarrel(db, item, location, null, 10, 5, 1);
        var carBarrel = AddBarrel(db, item, location, null, 10, 20, 2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var separateAssignment = await service.SetOilBarrelVehicleTypeAsync(carBarrel.Id,
            new(VehicleType.Car, Version(carBarrel)), TestContext.Current.CancellationToken);
        Assert.Equal(MaintenanceErrors.OilBarrelVehicleTypeLocked.Code, separateAssignment.Error.Code);
        Assert.Null(carBarrel.AllowedVehicleType);
        var first = await service.OpenOilBarrelAsync(motorcycleBarrel.Id,
            new(Now, Version(motorcycleBarrel), VehicleType.Motorcycle), TestContext.Current.CancellationToken);
        Assert.True(first.Value!.Opened);
        var second = await service.OpenOilBarrelAsync(carBarrel.Id,
            new(Now, Version(carBarrel), VehicleType.Car), TestContext.Current.CancellationToken);
        Assert.True(second.Value!.Opened);

        var carUse = await service.CompleteDirectOilChangeAsync(car.Id,
            Request(car, item, location), "car-use", TestContext.Current.CancellationToken);
        Assert.True(carUse.IsSuccess, carUse.Error.Description);
        Assert.Equal(70m, carUse.Value!.OilCost);
        Assert.Equal(6.5m, carBarrel.RemainingLiters);
        Assert.Equal(10m, motorcycleBarrel.RemainingLiters);
        var motorcycleUse = await service.CompleteDirectOilChangeAsync(motorcycle.Id,
            Request(motorcycle, item, location), "motorcycle-use", TestContext.Current.CancellationToken);
        Assert.True(motorcycleUse.IsSuccess, motorcycleUse.Error.Description);
        Assert.Equal(5m, motorcycleUse.Value!.OilCost);
        Assert.Equal(9m, motorcycleBarrel.RemainingLiters);
        Assert.Equal(15.5m, (await db.StockBalances.SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        Assert.Equal(2, await db.OilBarrelUsageAllocations.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WrongTypeAndUnclassifiedOpenBarrelsCannotSupplyACar()
    {
        await using var db = CreateContext();
        var (service, car, _, item, location) = await SeedAsync(db);
        var barrel = AddBarrel(db, item, location, VehicleType.Motorcycle, 10, 5, 1, open: true);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var wrongType = await service.CompleteDirectOilChangeAsync(car.Id, Request(car, item, location),
            "wrong-type", TestContext.Current.CancellationToken);
        Assert.True(wrongType.IsFailure);
        Assert.Equal(10m, barrel.RemainingLiters);
        barrel.AllowedVehicleType = null;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var unclassified = await service.CompleteDirectOilChangeAsync(car.Id, Request(car, item, location),
            "unclassified", TestContext.Current.CancellationToken);
        Assert.True(unclassified.IsFailure);
        Assert.Empty(db.OilBarrelUsageAllocations);
        var assigned = await service.SetOilBarrelVehicleTypeAsync(barrel.Id,
            new(VehicleType.Car, Version(barrel)), TestContext.Current.CancellationToken);
        Assert.True(assigned.IsSuccess, assigned.Error.Description);
        var accepted = await service.CompleteDirectOilChangeAsync(car.Id, Request(car, item, location),
            "classified", TestContext.Current.CancellationToken);
        Assert.True(accepted.IsSuccess, accepted.Error.Description);
    }

    [Fact]
    public async Task RolloverUsesOnlyMatchingTypeAndAssignsAnUnclassifiedSealedBarrel()
    {
        await using var db = CreateContext();
        var (service, car, _, item, location) = await SeedAsync(db);
        var current = AddBarrel(db, item, location, VehicleType.Car, 1, 10, 1, open: true);
        var wrongNext = AddBarrel(db, item, location, VehicleType.Motorcycle, 10, 5, 2);
        var next = AddBarrel(db, item, location, null, 10, 20, 3);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var rejected = await service.CompleteDirectOilChangeAsync(car.Id,
            Request(car, item, location) with { NextOilBarrelId = wrongNext.Id }, "wrong-next", TestContext.Current.CancellationToken);
        Assert.Equal(MaintenanceErrors.OilBarrelVehicleTypeMismatch.Code, rejected.Error.Code);
        Assert.Equal(1m, current.RemainingLiters);
        var accepted = await service.CompleteDirectOilChangeAsync(car.Id,
            Request(car, item, location) with { NextOilBarrelId = next.Id }, "correct-next", TestContext.Current.CancellationToken);
        Assert.True(accepted.IsSuccess, accepted.Error.Description);
        Assert.Equal(60m, accepted.Value!.OilCost);
        Assert.Equal(OilBarrelStatus.Depleted, current.Status);
        Assert.Equal(VehicleType.Car, next.AllowedVehicleType);
        Assert.Equal(OilBarrelStatus.Open, next.Status);
        Assert.Equal(7.5m, next.RemainingLiters);
        Assert.Equal(10m, wrongNext.RemainingLiters);
    }

    [Fact]
    public async Task AnotherOpenBarrelOfSameTypeIsBlockedAndOpenedTypeCannotChange()
    {
        await using var db = CreateContext();
        var (service, _, _, item, location) = await SeedAsync(db);
        var current = AddBarrel(db, item, location, VehicleType.Car, 10, 10, 1, open: true);
        var next = AddBarrel(db, item, location, VehicleType.Car, 10, 10, 2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var blocked = await service.OpenOilBarrelAsync(next.Id,
            new(Now, Version(next), VehicleType.Car), TestContext.Current.CancellationToken);
        Assert.False(blocked.Value!.Opened);
        Assert.True(blocked.Value.HasPreviousBarrelWarning);
        var switched = await service.SetOilBarrelVehicleTypeAsync(current.Id,
            new(VehicleType.Motorcycle, Version(current)), TestContext.Current.CancellationToken);
        Assert.Equal(MaintenanceErrors.OilBarrelVehicleTypeLocked.Code, switched.Error.Code);
    }

    [Fact]
    public async Task UsageGroupsVehiclesIncludesReversalsAndKeepsTotalsAcrossPages()
    {
        await using var db = CreateContext();
        var (service, car, motorcycle, item, location) = await SeedAsync(db);
        var barrel = AddBarrel(db, item, location, null, 10, 10, 1, open: true);
        foreach (var (vehicle, direction, quantity) in new[]
        {
            (car, MaintenanceUsageDirection.Issue, 4m),
            (car, MaintenanceUsageDirection.Issue, 3m),
            (car, MaintenanceUsageDirection.Reversal, 4m),
            (motorcycle, MaintenanceUsageDirection.Issue, 1m)
        })
        {
            var usage = new MaintenanceMaterialUsage
            {
                VehicleId = vehicle.Id, InventoryItemId = item.Id, InventoryLocationId = location.Id,
                Quantity = quantity, UsedAtUtc = Now, Direction = direction
            };
            db.MaintenanceMaterialUsages.Add(usage);
            db.OilBarrelUsageAllocations.Add(new OilBarrelUsageAllocation
            {
                OilBarrelId = barrel.Id, MaintenanceMaterialUsageId = usage.Id,
                Direction = direction, QuantityLiters = quantity
            });
        }
        car.IsDeleted = true;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var report = await service.GetOilBarrelUsageAsync(barrel.Id, 1, 50, TestContext.Current.CancellationToken);
        Assert.Equal(8m, report.Value!.TotalIssuedLiters);
        Assert.Equal(4m, report.Value.TotalReversedLiters);
        Assert.Equal(4m, report.Value.NetUsedLiters);
        var carRow = Assert.Single(report.Value.Vehicles, x => x.VehicleId == car.Id);
        Assert.Equal(3m, carRow.NetUsedLiters);
        Assert.Equal(2, carRow.IssueCount);
        Assert.Equal("CAR", carRow.PlateNumberEn);
        var page = await service.GetOilBarrelUsageAsync(barrel.Id, 1, 1, TestContext.Current.CancellationToken);
        Assert.Single(page.Value!.Vehicles);
        Assert.Equal(2, page.Value.TotalCount);
        Assert.Equal(4m, page.Value.NetUsedLiters);
    }

    [Fact]
    public void DatabaseAllowsOneOpenBarrelPerVehicleTypeAndRejectsOtherTypes()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=OilBarrelModel;Trusted_Connection=True").Options);
        var model = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(OilBarrel))!;
        var index = Assert.Single(model.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name)
            .SequenceEqual([nameof(OilBarrel.InventoryLocationId), nameof(OilBarrel.InventoryItemId), nameof(OilBarrel.AllowedVehicleType)]));
        Assert.Equal("[Status] = 2 AND [IsDeleted] = 0", index.GetFilter());
        Assert.Contains(model.GetCheckConstraints(), x => x.Name == "CK_OilBarrels_AllowedVehicleType");
    }

    [Theory]
    [InlineData(0)]
    [InlineData((int)VehicleType.Van)]
    [InlineData((int)VehicleType.Truck)]
    public async Task OpeningRequiresAnExplicitCarOrMotorcycleChoice(int type)
    {
        await using var db = CreateContext();
        var (service, _, _, item, location) = await SeedAsync(db);
        var barrel = AddBarrel(db, item, location, null, 10, 10, 1);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var result = await service.OpenOilBarrelAsync(barrel.Id,
            new(Now, Version(barrel), (VehicleType)type), TestContext.Current.CancellationToken);
        Assert.Equal(MaintenanceErrors.InvalidOilBarrelVehicleType.Code, result.Error.Code);
        Assert.Equal(OilBarrelStatus.Sealed, barrel.Status);
        Assert.Null(barrel.AllowedVehicleType);
    }

    [Fact]
    public async Task UsageIdentifiesExternalVehiclesByTheirWorkOrderAndPlate()
    {
        await using var db = CreateContext();
        var (service, _, _, item, location) = await SeedAsync(db);
        var barrel = AddBarrel(db, item, location, VehicleType.Car, 10, 10, 1, open: true);
        var workOrderId = Guid.NewGuid();
        db.ExternalVehicleSnapshots.Add(new ExternalVehicleSnapshot
        {
            MaintenanceWorkOrderId = workOrderId, PlateOrReference = "EXTERNAL-123",
            VehicleType = VehicleType.Car
        });
        var usage = new MaintenanceMaterialUsage
        {
            MaintenanceWorkOrderId = workOrderId, InventoryItemId = item.Id,
            InventoryLocationId = location.Id, Quantity = 3m, UsedAtUtc = Now
        };
        db.MaintenanceMaterialUsages.Add(usage);
        db.OilBarrelUsageAllocations.Add(new OilBarrelUsageAllocation
        {
            OilBarrelId = barrel.Id, MaintenanceMaterialUsageId = usage.Id, QuantityLiters = 3m
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var report = await service.GetOilBarrelUsageAsync(barrel.Id, 1, 50, TestContext.Current.CancellationToken);
        var row = Assert.Single(report.Value!.Vehicles);
        Assert.Null(row.VehicleId);
        Assert.Equal(workOrderId, row.ExternalWorkOrderId);
        Assert.Equal("EXTERNAL-123", row.ExternalPlateOrReference);
        Assert.Equal(3m, row.NetUsedLiters);
    }

    [Theory]
    [InlineData(VehicleType.Car)]
    [InlineData(VehicleType.Motorcycle)]
    public async Task BackdatedDirectOilChangeKeepsLowerReadingInHistoryWithoutChangingVehicleMileage(VehicleType type)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var (service, car, motorcycle, item, location) = await SeedAsync(db);
        var vehicle = type == VehicleType.Car ? car : motorcycle;
        vehicle.CurrentOdometer = 10000;
        vehicle.TrackedDistanceKm = 10000.875m;
        vehicle.LastOdometerAtUtc = Now;
        var barrel = AddBarrel(db, item, location, type, 30, 10, 1, open: true);
        await db.SaveChangesAsync(ct);
        var originalVersion = Version(vehicle);
        var performedAt = Now.AddDays(-2);
        var request = Request(vehicle, item, location) with { OdometerAtChange = 9900, PerformedAtUtc = performedAt };
        var negative = await service.CompleteDirectOilChangeAsync(vehicle.Id,
            request with { OdometerAtChange = -1 }, "negative-reading", ct);
        Assert.True(negative.IsFailure);
        Assert.Empty(db.OilChangeOperations);

        var result = await service.CompleteDirectOilChangeAsync(vehicle.Id, request, "historical-oil", ct);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(9900, result.Value!.OdometerAtChange);
        Assert.Equal(performedAt, result.Value.PerformedAtUtc);
        var persisted = await db.Vehicles.AsNoTracking().SingleAsync(x => x.Id == vehicle.Id, ct);
        Assert.Equal(10000, persisted.CurrentOdometer);
        Assert.Equal(10000.875m, persisted.TrackedDistanceKm);
        Assert.Equal(Now, persisted.LastOdometerAtUtc);
        Assert.Equal(originalVersion, Version(persisted));
        var reading = await db.VehicleOdometerReadings.SingleAsync(ct);
        Assert.Equal(9900, reading.Reading);
        Assert.Equal(performedAt, reading.RecordedAtUtc);
        Assert.Equal(30m - result.Value.OilQuantityLiters, barrel.RemainingLiters);
        var replay = await service.CompleteDirectOilChangeAsync(vehicle.Id, request, "historical-oil", ct);
        Assert.Equal(result.Value.Id, replay.Value!.Id);
        Assert.Single(db.OilChangeOperations);
        Assert.Single(db.VehicleOdometerReadings);
    }

    [Theory]
    [InlineData(10000)]
    [InlineData(10500)]
    public async Task DirectOilChangeStillAppliesEqualOrHigherVehicleMileage(long reading)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var (service, car, _, item, location) = await SeedAsync(db);
        car.CurrentOdometer = 10000;
        car.TrackedDistanceKm = 10000;
        car.LastOdometerAtUtc = Now.AddDays(-1);
        AddBarrel(db, item, location, VehicleType.Car, 30, 10, 1, open: true);
        await db.SaveChangesAsync(ct);
        var result = await service.CompleteDirectOilChangeAsync(car.Id,
            Request(car, item, location) with { OdometerAtChange = reading }, "current-oil", ct);
        Assert.True(result.IsSuccess, result.Error.Description);
        var persisted = await db.Vehicles.AsNoTracking().SingleAsync(x => x.Id == car.Id, ct);
        Assert.Equal(reading, persisted.CurrentOdometer);
        Assert.Equal(reading, persisted.TrackedDistanceKm);
        Assert.Equal(Now, persisted.LastOdometerAtUtc);
    }

    [Fact]
    public async Task WarehouseOilChangeAcceptsLowerMileageAtSubmissionAndAfterVehicleMovesBeforeApproval()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var (service, car, _, item, location) = await SeedAsync(db);
        car.CurrentOdometer = 10000;
        car.TrackedDistanceKm = 10000.75m;
        car.LastOdometerAtUtc = Now;
        var barrel = AddBarrel(db, item, location, VehicleType.Car, 30, 10, 1, open: true);
        await db.SaveChangesAsync(ct);
        var request = new CreateMaintenanceWorkOrderRequest(MaintenanceServiceSubjectType.CompanyVehicle,
            car.Id, null, location.MaintenanceLocationId, MaintenanceType.OilChange,
            Now.AddDays(-2), null, 9500, "Historical oil change", null, null,
            OilChange: new(location.Id, item.Id, false, null));
        var negative = await service.CreateWorkOrderAsync(request with { OdometerAtOpen = -1 }, ct);
        Assert.Equal(MaintenanceErrors.InvalidOdometer.Code, negative.Error.Code);
        var created = await service.CreateWorkOrderAsync(request, ct);
        Assert.True(created.IsSuccess, created.Error.Description);
        car.CurrentOdometer = 10100;
        car.TrackedDistanceKm = 10100.875m;
        car.LastOdometerAtUtc = Now.AddHours(1);
        await db.SaveChangesAsync(ct);
        var versionAtApproval = Version(car);
        var supplied = created.Value!.SupplyRequest!;
        var issuedAt = Now.AddDays(-2).AddHours(1);
        var approved = await service.ApproveAndIssueSupplyRequestAsync(supplied.Id,
            new(issuedAt, supplied.RowVersion), ct);
        Assert.True(approved.IsSuccess, approved.Error.Description);
        var persisted = await db.Vehicles.AsNoTracking().SingleAsync(x => x.Id == car.Id, ct);
        Assert.Equal(10100, persisted.CurrentOdometer);
        Assert.Equal(10100.875m, persisted.TrackedDistanceKm);
        Assert.Equal(Now.AddHours(1), persisted.LastOdometerAtUtc);
        Assert.Equal(versionAtApproval, Version(persisted));
        var operation = await db.OilChangeOperations.SingleAsync(ct);
        Assert.Equal(9500, operation.OdometerAtChange);
        Assert.Equal(issuedAt, operation.PerformedAtUtc);
        var order = await db.MaintenanceWorkOrders.SingleAsync(ct);
        Assert.Equal(MaintenanceWorkOrderStatus.Completed, order.Status);
        Assert.Equal(9500, order.OdometerAtCompletion);
        Assert.Equal(9500, (await db.VehicleOdometerReadings.SingleAsync(ct)).Reading);
        Assert.Equal(26.5m, barrel.RemainingLiters);
    }

    [Fact]
    public async Task OlderOilChangeDoesNotReplaceNewerScheduleOrLatestReminder()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var (service, car, _, item, location) = await SeedAsync(db);
        car.CurrentOdometer = 11000;
        car.TrackedDistanceKm = 11000;
        car.LastOdometerAtUtc = Now;
        AddBarrel(db, item, location, VehicleType.Car, 30, 10, 1, open: true);
        db.MaintenancePlans.Add(new MaintenancePlan
        {
            Code = "CAR-OIL", VehicleType = VehicleType.Car, TriggerType = MaintenanceTriggerType.OdometerWindow,
            ReminderAfterKilometers = 4000, MaximumAfterKilometers = 5000
        });
        await db.SaveChangesAsync(ct);
        var recent = await service.CompleteDirectOilChangeAsync(car.Id,
            Request(car, item, location) with { OdometerAtChange = 9900, PerformedAtUtc = Now.AddDays(-1) }, "recent-oil", ct);
        Assert.True(recent.IsSuccess, recent.Error.Description);
        var older = await service.CompleteDirectOilChangeAsync(car.Id,
            Request(car, item, location) with { OdometerAtChange = 9950, PerformedAtUtc = Now.AddDays(-2) }, "older-oil", ct);
        Assert.True(older.IsSuccess, older.Error.Description);
        var schedule = await db.VehicleMaintenanceSchedules.SingleAsync(ct);
        Assert.Equal(9900, schedule.LastCompletedOdometer);
        Assert.Equal(Now.AddDays(-1), schedule.LastCompletedAtUtc);
        Assert.Equal(13900, schedule.ReminderFromOdometer);
        var reminders = await service.GetOilRemindersAsync(ct);
        var reminder = Assert.Single(reminders.Value!, x => x.VehicleId == car.Id);
        Assert.Equal(9900, reminder.LastOilChangeOdometer);
        Assert.Equal(Now.AddDays(-1), reminder.LastCompletedAtUtc);
        Assert.Equal(11000, reminder.CurrentOdometer);
        Assert.Equal(2, await db.OilChangeOperations.CountAsync(ct));
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
        .AddInterceptors(new TestRowVersionInterceptor())
        .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private sealed class TestRowVersionInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            foreach (var entry in eventData.Context!.ChangeTracker.Entries<LogisticsERP.Domain.Common.AuditableEntity>()
                         .Where(x => x.State is EntityState.Added or EntityState.Modified))
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            return ValueTask.FromResult(result);
        }
    }

    private static async Task<(MaintenanceService Service, Vehicle Car, Vehicle Motorcycle, InventoryItem Item, InventoryLocation Location)>
        SeedAsync(ApplicationDbContext db)
    {
        var site = new MaintenanceLocation { AllowsCompanyVehicles = true, InventoryEnabled = true };
        var location = new InventoryLocation { MaintenanceLocationId = site.Id };
        var item = new InventoryItem { ItemType = InventoryItemType.Oil, BaseUnitOfMeasure = InventoryUnitOfMeasure.Liter };
        var car = new Vehicle { VehicleType = VehicleType.Car, PlateNumberEn = "CAR" };
        var motorcycle = new Vehicle { VehicleType = VehicleType.Motorcycle, PlateNumberEn = "MOTORCYCLE" };
        db.AddRange(site, location, item, car, motorcycle);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (new MaintenanceService(db, new TestUser(), TimeProvider.System, new NoFiles()), car, motorcycle, item, location);
    }

    private static OilBarrel AddBarrel(ApplicationDbContext db, InventoryItem item, InventoryLocation location,
        VehicleType? type, decimal quantity, decimal unitCost, int sequence, bool open = false)
    {
        var layer = new StockCostLayer { InventoryItemId = item.Id, InventoryLocationId = location.Id,
            ReceivedAtUtc = Now.AddDays(sequence - 10), OriginalSequence = sequence,
            OriginalQuantity = quantity, RemainingQuantity = quantity, UnitCost = unitCost };
        var barrel = new OilBarrel { BarrelNumber = $"OB-{sequence}", InventoryItemId = item.Id,
            InventoryLocationId = location.Id, StockCostLayerId = layer.Id, AllowedVehicleType = type,
            NominalCapacityLiters = quantity, RemainingLiters = quantity,
            MaximumAllowedLossLiters = quantity * 0.02m, UnitCostPerLiter = unitCost,
            Status = open ? OilBarrelStatus.Open : OilBarrelStatus.Sealed, OpenedAtUtc = open ? Now.AddDays(-1) : null };
        var balance = db.StockBalances.Local.SingleOrDefault(x => x.InventoryItemId == item.Id);
        if (balance is null)
        {
            balance = new StockBalance { InventoryItemId = item.Id, InventoryLocationId = location.Id };
            db.StockBalances.Add(balance);
        }
        balance.QuantityOnHand += quantity;
        db.AddRange(layer, barrel);
        return barrel;
    }

    private static DirectOilChangeRequest Request(Vehicle vehicle, InventoryItem item, InventoryLocation location) =>
        new(Now, 100, location.Id, item.Id, null, false, null, 1m, 0, null, Version(vehicle));
    private static string Version(LogisticsERP.Domain.Common.AuditableEntity entity) => Convert.ToBase64String(entity.RowVersion);
    private sealed class TestUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => null;
    }
    private sealed class NoFiles : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file,
            long maximumBytes, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType,
            string downloadFileName, long length, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void DeleteBestEffort(string storagePath) { }
    }
}
