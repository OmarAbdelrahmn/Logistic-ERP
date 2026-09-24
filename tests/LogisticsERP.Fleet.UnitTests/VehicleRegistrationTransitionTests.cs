using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleRegistrationTransitionTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("س ب 5149", "GBN 5149")]
    public async Task ConversionAcceptsMissingOrUnchangedStoredPlate(string? oldPlateAr, string? oldPlateEn)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var vehicle = new Vehicle
        {
            AssetNumber = "CONVERT-1",
            NormalizedAssetNumber = "CONVERT1",
            PlateNumberAr = oldPlateAr,
            NormalizedPlateNumberAr = oldPlateAr is null ? null : FleetServiceSupport.NormalizeIdentifier(oldPlateAr),
            PlateNumberEn = oldPlateEn,
            NormalizedPlateNumberEn = oldPlateEn is null ? null : FleetServiceSupport.NormalizeIdentifier(oldPlateEn),
            RegistrationType = VehicleRegistrationType.Private
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db);
        var request = new VehicleRegistrationTransitionRequest("س ب 5149", "GBN 5149", "س ب", "GBN", "5149",
            DateTimeOffset.UtcNow, "Convert to public transport", Convert.ToBase64String(vehicle.RowVersion));
        using var istimaraStream = new MemoryStream([1]);
        using var operationCardStream = new MemoryStream([2]);

        var result = await service.TransitionToPublicTransportAsync(vehicle.Id, request,
            new PrivateFileUpload(istimaraStream, "istimara.jpeg", "image/jpeg", 1),
            new PrivateFileUpload(operationCardStream, "operation-card.pdf", "application/pdf", 1), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(VehicleRegistrationType.PublicTransport, vehicle.RegistrationType);
        Assert.Equal(oldPlateAr ?? string.Empty, result.Value!.OldPlateNumberAr);
        Assert.Equal(oldPlateEn ?? string.Empty, result.Value.OldPlateNumberEn);
        Assert.NotNull(result.Value.VehicleDetailsSnapshot);
        Assert.Equal(2, await db.VehicleAttachmentVersions.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task UnsupportedIstimaraReturnsItsOwnFieldError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var vehicle = new Vehicle
        {
            AssetNumber = "CONVERT-2",
            NormalizedAssetNumber = "CONVERT2",
            RegistrationType = VehicleRegistrationType.Private
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);
        var service = CreateService(db);
        var request = new VehicleRegistrationTransitionRequest("س ب 5149", "GBN 5149", null, null, null,
            DateTimeOffset.UtcNow, "Convert to public transport", Convert.ToBase64String(vehicle.RowVersion));
        using var invalidStream = new MemoryStream([1]);
        using var operationCardStream = new MemoryStream([2]);

        var result = await service.TransitionToPublicTransportAsync(vehicle.Id, request,
            new PrivateFileUpload(invalidStream, "istimara.txt", "text/plain", 1),
            new PrivateFileUpload(operationCardStream, "operation-card.pdf", "application/pdf", 1), cancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(FleetErrors.TransitionIstimaraInvalid.Code, result.Error.Code);
        Assert.Equal("istimara", result.Error.Field);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VehicleRegistrationTransition_{Guid.NewGuid():N}", x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new TestRowVersionInterceptor())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static FleetService CreateService(ApplicationDbContext db) =>
        new(db, new FleetServiceSupport(new TestCurrentUser(), new PermitAll(), TimeProvider.System), new TestFileStorage());

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.CreateVersion7();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => null;
    }

    private sealed class PermitAll : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }

    private sealed class TestFileStorage : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file, long maximumBytes, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(new StoredPrivateFile($"{relativeDirectory}/{file.OriginalFileName}", file.OriginalFileName,
                file.Length, Guid.NewGuid().ToString("N"), file.ContentType, file.OriginalFileName)));

        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType, string downloadFileName, long length, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void DeleteBestEffort(string storagePath) { }
    }

    private sealed class TestRowVersionInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            foreach (var entry in eventData.Context!.ChangeTracker.Entries<AuditableEntity>()
                         .Where(x => x.State is EntityState.Added or EntityState.Modified))
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            return ValueTask.FromResult(result);
        }
    }
}
