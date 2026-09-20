using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleOperationCardServiceTests
{
    [Fact]
    public async Task MotorcycleCanRenewOperationCardWithSameCardNumber()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var currentUser = new TestCurrentUser();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"MotorcycleOperationCard_{Guid.NewGuid():N}")
            .AddInterceptors(new ApplicationPersistenceInterceptor(currentUser, TimeProvider.System))
            .Options;
        await using var db = new ApplicationDbContext(options);
        var vehicle = CreateVehicle(VehicleType.Motorcycle, VehicleRegistrationType.Motorcycle);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, currentUser);
        var firstRequest = new VehicleOperationCardRequest(
            "38-00048920",
            "Transport General Authority",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            null);

        var first = await service.RenewOperationCardAsync(vehicle.Id, firstRequest, cancellationToken);
        var renewed = await service.RenewOperationCardAsync(
            vehicle.Id,
            firstRequest with
            {
                IssueDate = new DateOnly(2027, 1, 1),
                ExpiryDate = new DateOnly(2027, 12, 31)
            },
            cancellationToken);
        var history = await service.GetComplianceAsync(vehicle.Id, "operation-cards", cancellationToken);

        Assert.True(first.IsSuccess, first.Error.Description);
        Assert.True(renewed.IsSuccess, renewed.Error.Description);
        Assert.True(history.IsSuccess, history.Error.Description);
        Assert.Equal(2, history.Value!.Count);
        Assert.Equal(2, await db.VehicleOperationCards.CountAsync(cancellationToken));
        Assert.Single(await db.VehicleOperationCards.Where(x => x.IsCurrent).ToArrayAsync(cancellationToken));
        Assert.All(
            await db.VehicleOperationCards.ToArrayAsync(cancellationToken),
            card => Assert.Equal("38-00048920", card.CardNumber));
    }

    [Fact]
    public async Task PrivateCarStillRejectsOperationCard()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var currentUser = new TestCurrentUser();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"PrivateCarOperationCard_{Guid.NewGuid():N}")
            .AddInterceptors(new ApplicationPersistenceInterceptor(currentUser, TimeProvider.System))
            .Options;
        await using var db = new ApplicationDbContext(options);
        var vehicle = CreateVehicle(VehicleType.Car, VehicleRegistrationType.Private);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db, currentUser);

        var result = await service.RenewOperationCardAsync(
            vehicle.Id,
            new VehicleOperationCardRequest(
                "CARD-1",
                "Authority",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31),
                null),
            cancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(FleetErrors.InvalidRequest.Code, result.Error.Code);
    }

    private static FleetService CreateService(ApplicationDbContext db, ICurrentUser currentUser) =>
        new(db, new FleetServiceSupport(currentUser, new PermitAll(), TimeProvider.System), new UnusedFileStorage());

    private static Vehicle CreateVehicle(VehicleType vehicleType, VehicleRegistrationType registrationType) => new()
    {
        Id = Guid.CreateVersion7(),
        AssetNumber = $"VEH-{Guid.NewGuid():N}"[..16],
        NormalizedAssetNumber = Guid.NewGuid().ToString("N"),
        VehicleManufacturerId = Guid.CreateVersion7(),
        VehicleModelId = Guid.CreateVersion7(),
        VehicleType = vehicleType,
        RegistrationType = registrationType
    };

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.CreateVersion7();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "operation-card-test";
    }

    private sealed class PermitAll : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(
            Guid userId,
            long authorizationVersion,
            string permissionKey,
            PermissionScope? scope = null,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public void InvalidateUser(Guid userId, long authorizationVersion)
        {
        }
    }

    private sealed class UnusedFileStorage : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(
            string relativeDirectory,
            PrivateFileUpload file,
            long maximumBytes,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<PrivateFileDownload>> OpenReadAsync(
            string storagePath,
            string contentType,
            string downloadFileName,
            long length,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void DeleteBestEffort(string storagePath)
        {
        }
    }
}
