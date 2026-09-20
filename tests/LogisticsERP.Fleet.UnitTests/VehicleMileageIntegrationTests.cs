using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleMileageIntegrationTests
{
    [Fact]
    public async Task GpsImportUpdatesVehicleOdometerAndCorrectionAppliesOnlyTheDelta()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var currentUser = new TestCurrentUser();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VehicleMileage_{Guid.NewGuid():N}")
            .AddInterceptors(new ApplicationPersistenceInterceptor(currentUser, TimeProvider.System))
            .Options;
        await using var db = new ApplicationDbContext(options);
        var vehicle = new Vehicle
        {
            Id = Guid.CreateVersion7(),
            AssetNumber = "VEH-GPS-1",
            NormalizedAssetNumber = "VEHGPS1",
            PlateNumberEn = "2429 AH",
            NormalizedPlateNumberEn = "2429AH",
            VehicleManufacturerId = Guid.CreateVersion7(),
            VehicleModelId = Guid.CreateVersion7(),
            VehicleType = VehicleType.Car,
            CurrentOdometer = 10_000,
            TrackedDistanceKm = 10_000m
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);
        var service = new VehicleDailyDistanceService(
            db,
            new FleetServiceSupport(currentUser, new PermitAll(), TimeProvider.System));

        await using var firstFile = CreateGpsWorkbook(150.75m);
        var first = await service.ImportGpsAsync(
            new PrivateFileUpload(firstFile, "gps-first.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", firstFile.Length),
            new DateOnly(2026, 8, 31),
            cancellationToken);

        Assert.True(first.IsSuccess, first.Error.Description);
        Assert.Equal(10_150.75m, vehicle.TrackedDistanceKm);
        Assert.Equal(10_150, vehicle.CurrentOdometer);

        await using var correctedFile = CreateGpsWorkbook(160.25m);
        var corrected = await service.ImportGpsAsync(
            new PrivateFileUpload(correctedFile, "gps-corrected.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", correctedFile.Length),
            new DateOnly(2026, 8, 31),
            cancellationToken);

        Assert.True(corrected.IsSuccess, corrected.Error.Description);
        Assert.Equal(10_160.25m, vehicle.TrackedDistanceKm);
        Assert.Equal(10_160, vehicle.CurrentOdometer);
        var daily = await db.VehicleDailyDistances.SingleAsync(cancellationToken);
        Assert.Equal(160.25m, daily.AppliedDistanceKm);
        Assert.Equal(10_160.25m, daily.EffectiveOdometerAfterKm);
        Assert.Equal(VehicleDailyDistanceSource.Gps, daily.AppliedSource);
        Assert.Equal(2, await db.VehicleOdometerReadings.CountAsync(cancellationToken));
        Assert.All(
            await db.VehicleOdometerReadings.ToArrayAsync(cancellationToken),
            reading => Assert.Equal(VehicleOdometerSourceType.Gps, reading.SourceType));
    }

    [Fact]
    public void GpsDistanceUpdatesDailyAndVehicleEffectiveMileage()
    {
        var vehicle = new Vehicle
        {
            CurrentOdometer = 10_000,
            TrackedDistanceKm = 10_000m
        };
        var distance = new VehicleDailyDistance
        {
            WorkDate = new DateOnly(2026, 9, 18),
            GpsDistanceKm = 150.75m
        };

        var valid = VehicleDailyDistanceRules.TryRecalculate([distance], vehicle.TrackedDistanceKm, out var effective);
        VehicleMileageRules.ApplyEffectiveMileage(vehicle, effective, new DateTimeOffset(2026, 9, 18, 20, 59, 59, TimeSpan.Zero));

        Assert.True(valid);
        Assert.Equal(VehicleDailyDistanceSource.Gps, distance.AppliedSource);
        Assert.Equal(150.75m, distance.AppliedDistanceKm);
        Assert.Equal(10_150.75m, distance.EffectiveOdometerAfterKm);
        Assert.Equal(10_150.75m, vehicle.TrackedDistanceKm);
        Assert.Equal(10_150, vehicle.CurrentOdometer);
    }

    [Fact]
    public void ManualFallbackAfterGpsUsesEffectiveMileageWithoutDoubleCounting()
    {
        var gpsDay = new VehicleDailyDistance
        {
            WorkDate = new DateOnly(2026, 9, 17),
            GpsDistanceKm = 100.50m
        };
        var manualDay = new VehicleDailyDistance
        {
            WorkDate = new DateOnly(2026, 9, 18),
            ManualOdometerReading = 10_125
        };

        var valid = VehicleDailyDistanceRules.TryRecalculate([gpsDay, manualDay], 10_000m, out var effective);

        Assert.True(valid);
        Assert.Equal(10_100.50m, manualDay.ManualBaselineOdometerReading);
        Assert.Equal(24.50m, manualDay.ManualDistanceKm);
        Assert.Equal(VehicleDailyDistanceSource.Manual, manualDay.AppliedSource);
        Assert.Equal(10_125m, effective);
    }

    [Fact]
    public void GpsRemainsAppliedSourceWhenManualReadingExistsForSameDay()
    {
        var distance = new VehicleDailyDistance
        {
            WorkDate = new DateOnly(2026, 9, 18),
            GpsDistanceKm = 80.25m,
            ManualOdometerReading = 10_090
        };

        var valid = VehicleDailyDistanceRules.TryRecalculate([distance], 10_000m, out var effective);

        Assert.True(valid);
        Assert.Equal(90m, distance.ManualDistanceKm);
        Assert.Equal(80.25m, distance.AppliedDistanceKm);
        Assert.Equal(VehicleDailyDistanceSource.Gps, distance.AppliedSource);
        Assert.Equal(10_080.25m, effective);
    }

    [Fact]
    public void BackdatedGpsCorrectionRecalculatesFollowingManualFallback()
    {
        var gpsDay = new VehicleDailyDistance
        {
            WorkDate = new DateOnly(2026, 9, 17),
            GpsDistanceKm = 120m,
            AppliedDistanceKm = 100m
        };
        var manualDay = new VehicleDailyDistance
        {
            WorkDate = new DateOnly(2026, 9, 18),
            ManualOdometerReading = 10_150,
            ManualDistanceKm = 50m,
            AppliedDistanceKm = 50m
        };

        var valid = VehicleDailyDistanceRules.TryRecalculate([gpsDay, manualDay], 10_000m, out var effective);

        Assert.True(valid);
        Assert.Equal(30m, manualDay.ManualDistanceKm);
        Assert.Equal(10_150m, effective);
    }

    [Fact]
    public void RecalculationRejectsManualReadingBelowEffectiveMileage()
    {
        var records = new[]
        {
            new VehicleDailyDistance { WorkDate = new DateOnly(2026, 9, 17), GpsDistanceKm = 150m },
            new VehicleDailyDistance { WorkDate = new DateOnly(2026, 9, 18), ManualOdometerReading = 10_100 }
        };

        var valid = VehicleDailyDistanceRules.TryRecalculate(records, 10_000m, out _);

        Assert.False(valid);
    }

    [Fact]
    public void VerifiedReadingRebasesGpsProjection()
    {
        var vehicle = new Vehicle
        {
            CurrentOdometer = 10_150,
            TrackedDistanceKm = 10_150.75m
        };
        var recordedAt = DateTimeOffset.UtcNow;

        VehicleMileageRules.ApplyVerifiedReading(vehicle, 10_160, recordedAt);

        Assert.Equal(10_160, vehicle.CurrentOdometer);
        Assert.Equal(10_160m, vehicle.TrackedDistanceKm);
        Assert.Equal(recordedAt, vehicle.LastOdometerAtUtc);
    }

    private static MemoryStream CreateGpsWorkbook(decimal distanceKm)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("GPS");
        sheet.Cell(1, 1).Value = "معلومات عامة";
        sheet.Cell(3, 1).Value = "فترة:";
        sheet.Cell(3, 2).Value = "2026-08-31 00:00:00 - 2026-09-01 00:00:00";
        sheet.Cell(5, 1).Value = "عربة";
        sheet.Cell(5, 2).Value = "طول الطريق";
        sheet.Cell(6, 1).Value = "2429 AH";
        sheet.Cell(6, 2).Value = $"{distanceKm:0.00} كيلومترا";
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.CreateVersion7();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "vehicle-mileage-integration-test";
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
}
