using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleRegisteredOwnerServiceTests
{
    [Fact]
    public async Task UpsertAcceptsSponsorOrSupplierThroughExistingRegisteredOwnerField()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await Fixture.CreateAsync(cancellationToken);
        var request = fixture.CreateRequest(fixture.OwnerSponsor.Id);

        var created = await fixture.Service.UpsertVehicleAsync(null, request, cancellationToken);

        Assert.True(created.IsSuccess, created.Error.Description);
        Assert.Equal(fixture.OwnerSponsor.Id, created.Value!.RegisteredOwnerSupplierId);
        Assert.Equal(fixture.OwnerSponsor.RegistryNameAr, created.Value.RegisteredOwnerSupplier);
        Assert.Equal("Sponsor", created.Value.RegisteredOwnerType);
        var vehicle = await fixture.Db.Vehicles.SingleAsync(x => x.Id == created.Value.Summary.Id, cancellationToken);
        Assert.Null(vehicle.RegisteredOwnerSupplierId);
        Assert.Equal(fixture.OwnerSponsor.Id, vehicle.RegisteredOwnerSponsorId);
        Assert.NotEqual(vehicle.SponsorId, vehicle.RegisteredOwnerSponsorId);

        var updated = await fixture.Service.UpsertVehicleAsync(
            vehicle.Id,
            request with
            {
                RegisteredOwnerSupplierId = fixture.OwnerSupplier.Id,
                RowVersion = created.Value.Summary.RowVersion
            },
            cancellationToken);

        Assert.True(updated.IsSuccess, updated.Error.Description);
        Assert.Equal(fixture.OwnerSupplier.Id, updated.Value!.RegisteredOwnerSupplierId);
        Assert.Equal(fixture.OwnerSupplier.NameAr, updated.Value.RegisteredOwnerSupplier);
        Assert.Equal("Supplier", updated.Value.RegisteredOwnerType);
        Assert.Equal(fixture.OwnerSupplier.Id, vehicle.RegisteredOwnerSupplierId);
        Assert.Null(vehicle.RegisteredOwnerSponsorId);
    }

    [Fact]
    public async Task UpsertRejectsUnknownRegisteredOwner()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await Fixture.CreateAsync(cancellationToken);

        var result = await fixture.Service.UpsertVehicleAsync(
            null,
            fixture.CreateRequest(Guid.NewGuid()),
            cancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(FleetErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task OwnedVehicleWithoutPurchaseSupplierIsAllowedAndReadyForAssignment()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var fixture = await Fixture.CreateAsync(cancellationToken);
        var request = fixture.CreateRequest(fixture.OwnerSponsor.Id) with
        {
            OwnershipType = VehicleOwnershipType.Owned,
            PurchasedFromSupplierId = null
        };

        var created = await fixture.Service.UpsertVehicleAsync(null, request, cancellationToken);

        Assert.True(created.IsSuccess, created.Error.Description);
        Assert.Null(created.Value!.PurchasedFromSupplierId);
        Assert.True(created.Value.Summary.IsReadyForAssignment);

        var readiness = await fixture.Service.GetReadinessAsync(created.Value.Summary.Id, cancellationToken);

        Assert.True(readiness.IsSuccess, readiness.Error.Description);
        Assert.True(readiness.Value!.IsEligibleForAssignment);
        Assert.DoesNotContain(nameof(Vehicle.PurchasedFromSupplierId), readiness.Value.MissingCoreIdentityFields);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(ApplicationDbContext db, Sponsor vehicleSponsor, Sponsor ownerSponsor, VehicleSupplier ownerSupplier)
        {
            Db = db;
            VehicleSponsor = vehicleSponsor;
            OwnerSponsor = ownerSponsor;
            OwnerSupplier = ownerSupplier;
            Service = new FleetService(
                db,
                new FleetServiceSupport(new TestCurrentUser(), new PermitAll(), TimeProvider.System),
                new UnusedFileStorage());
        }

        public ApplicationDbContext Db { get; }
        public Sponsor VehicleSponsor { get; }
        public Sponsor OwnerSponsor { get; }
        public VehicleSupplier OwnerSupplier { get; }
        public FleetService Service { get; }
        private VehicleManufacturer Manufacturer { get; init; } = null!;
        private VehicleModel Model { get; init; } = null!;
        private OperatingCity City { get; init; } = null!;

        public static async Task<Fixture> CreateAsync(CancellationToken cancellationToken)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"VehicleRegisteredOwner_{Guid.NewGuid():N}", options => options.EnableNullChecks(false))
                .AddInterceptors(new TestRowVersionInterceptor())
                .Options;
            var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync(cancellationToken);

            var manufacturer = new VehicleManufacturer
            {
                Code = "TEST-MAKE",
                NameAr = "مصنع اختبار",
                NameEn = "Test Make"
            };
            var model = new VehicleModel
            {
                VehicleManufacturerId = manufacturer.Id,
                Code = "TEST-MODEL",
                NameAr = "طراز اختبار",
                NameEn = "Test Model",
                VehicleType = VehicleType.Car,
                DefaultFuelType = VehicleFuelType.Petrol
            };
            var globalCity = new GlobalCity
            {
                Code = "TEST-CITY",
                NameAr = "مدينة اختبار",
                NameEn = "Test City",
                CountryCode = "SA"
            };
            var city = new OperatingCity { GlobalCityId = globalCity.Id };
            var vehicleSponsor = new Sponsor
            {
                EmployerIdentityNumber = "7000000001",
                RegistryNameAr = "المستخدم الفعلي للمركبة",
                RegistryNameEn = "Vehicle User Sponsor"
            };
            var ownerSponsor = new Sponsor
            {
                EmployerIdentityNumber = "7000000002",
                RegistryNameAr = "المالك المسجل",
                RegistryNameEn = "Registered Owner Sponsor"
            };
            var ownerSupplier = new VehicleSupplier
            {
                Code = "OWNER-SUPPLIER",
                NameAr = "المورد المالك",
                NameEn = "Owner Supplier",
                Status = VehicleCatalogStatus.Active
            };
            db.AddRange(manufacturer, model, globalCity, city, vehicleSponsor, ownerSponsor, ownerSupplier);
            await db.SaveChangesAsync(cancellationToken);

            return new Fixture(db, vehicleSponsor, ownerSponsor, ownerSupplier)
            {
                Manufacturer = manufacturer,
                Model = model,
                City = city
            };
        }

        public VehicleUpsertRequest CreateRequest(Guid registeredOwnerId) => new(
            "VEH-OWNER-TEST",
            "SERIAL-OWNER-TEST",
            "أ ب ج 1000",
            "ABC 1000",
            "أ ب ج",
            "ABC",
            "1000",
            "1HGBH41JXMN000001",
            "CHASSIS-OWNER-TEST",
            "ENGINE-OWNER-TEST",
            VehicleSponsor.Id,
            City.Id,
            null,
            registeredOwnerId,
            VehicleRegistrationType.Private,
            Manufacturer.Id,
            Model.Id,
            2026,
            VehicleType.Car,
            VehicleFuelType.Petrol,
            VehicleTransmissionType.Automatic,
            "أبيض",
            "White",
            VehicleOwnershipType.Leased,
            null,
            new DateOnly(2026, 1, 1),
            null,
            100,
            null,
            null);

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "vehicle-owner-test";
    }

    private sealed class PermitAll : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }

    private sealed class UnusedFileStorage : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file, long maximumBytes, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType, string downloadFileName, long length, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
