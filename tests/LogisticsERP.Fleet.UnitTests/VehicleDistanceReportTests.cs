using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleDistanceReportTests
{
    private static readonly DateOnly Start = new(2026, 9, 1);

    [Fact]
    public async Task SingleVehicleReturnsInclusiveCalendarBothSourcesAndAppliedTotalWithoutDoubleCounting()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = Context();
        var vehicle = AddVehicle(db, "V001");
        db.AddRange(new GlobalCity { Id = GlobalCity.JeddahId, NameAr = "جدة", NameEn = "Jeddah" },
            new OperatingCity { Id = OperatingCity.JeddahId, GlobalCityId = GlobalCity.JeddahId });
        AddDay(db, vehicle, 1, VehicleDailyDistanceSource.Gps, 12.5m, gps: 12.5m, manual: 15m);
        AddDay(db, vehicle, 2, VehicleDailyDistanceSource.Manual, 5.25m, manual: 5.25m);
        AddDay(db, vehicle, 3, VehicleDailyDistanceSource.Gps, 0m, gps: 0m);
        AddDay(db, vehicle, 4, VehicleDailyDistanceSource.None, 0m);
        AddDay(db, vehicle, 5, VehicleDailyDistanceSource.Gps, 100m, gps: 100m, deleted: true);
        AddDay(db, vehicle, 6, VehicleDailyDistanceSource.Gps, 999m, gps: 999m);
        AddDay(db, AddVehicle(db, "V002"), 1, VehicleDailyDistanceSource.Gps, 888m, gps: 888m);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        var result = await Service(db).GetVehiclePeriodReportAsync(vehicle.Id, Start, Start.AddDays(4), ct);

        Assert.True(result.IsSuccess, result.Error.Description);
        var report = result.Value!;
        Assert.Equal(vehicle.Id, report.Vehicle.VehicleId);
        Assert.Equal(VehicleType.Car, report.Vehicle.VehicleType);
        Assert.Equal("جدة", report.Vehicle.OperatingCity);
        Assert.Equal(5, report.TotalDays);
        Assert.Equal(3, report.RecordedDays);
        Assert.Equal(2, report.GpsDays);
        Assert.Equal(2, report.ManualDays);
        Assert.Equal(1, report.ManualFallbackDays);
        Assert.Equal(2, report.MissingDays);
        Assert.Equal(12.5m, report.GpsTotalKm);
        Assert.Equal(20.25m, report.ManualTotalKm);
        Assert.Equal(17.75m, report.AppliedTotalKm);
        Assert.Equal(Start, report.Days[0].WorkDate);
        Assert.Equal(Start.AddDays(4), report.Days[4].WorkDate);
        Assert.True(report.Days[2].HasDistance);
        Assert.True(report.Days[3].HasRecord);
        Assert.False(report.Days[3].HasDistance);
        Assert.False(report.Days[4].HasRecord);
        Assert.Null(report.Days[4].EffectiveOdometerAfterKm);
        Assert.NotNull(report.Days[0].ManualEnteredByUserId);
        Assert.Equal("Manual note", report.Days[0].ManualNotes);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task MissingReportReturnsDaysFourFiveSevenAndOnlyCurrentlyAssignedVehiclesWithGaps()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = Context();
        var gaps = AddVehicle(db, "A-GAPS");
        var noRecords = AddVehicle(db, "B-NONE");
        var complete = AddVehicle(db, "C-COMPLETE");
        AddVehicle(db, "D-AVAILABLE", VehicleOperationalStatus.Available);
        AddVehicle(db, "E-HOLD", VehicleOperationalStatus.ProblemHold);
        AddVehicle(db, "F-DELETED", deleted: true);
        foreach (var day in new[] { 1, 2, 3, 6 })
            AddDay(db, gaps, day, VehicleDailyDistanceSource.Gps, 0m, gps: 0m);
        AddDay(db, gaps, 4, VehicleDailyDistanceSource.None, 0m);
        AddDay(db, gaps, 5, VehicleDailyDistanceSource.Gps, 10m, gps: 10m, deleted: true);
        AddDay(db, gaps, 8, VehicleDailyDistanceSource.Gps, 10m, gps: 10m);
        for (var day = 1; day <= 7; day++)
            AddDay(db, complete, day, VehicleDailyDistanceSource.Manual, 0m, manual: 0m);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        var result = await Service(db).GetMissingRecordsReportAsync(Start, Start.AddDays(6), null, null, null, 1, 1, ct);

        Assert.True(result.IsSuccess, result.Error.Description);
        var report = result.Value!;
        Assert.Equal(VehicleOperationalStatus.Assigned, report.WorkingStatus);
        Assert.Equal(3, report.WorkingVehicleCount);
        Assert.Equal(2, report.TotalCount);
        Assert.Equal(10L, report.TotalMissingDays);
        var item = Assert.Single(report.Items);
        Assert.Equal(gaps.Id, item.Vehicle.VehicleId);
        Assert.Equal(4, item.RecordedDays);
        Assert.Equal(3, item.MissingDays);
        Assert.Equal(Start.AddDays(3), item.MissingDates[0]);
        Assert.Equal(Start.AddDays(4), item.MissingDates[1]);
        Assert.Equal(Start.AddDays(6), item.MissingDates[2]);
        var second = await Service(db).GetMissingRecordsReportAsync(Start, Start.AddDays(6), null, null, null, 2, 1, ct);
        var emptyVehicle = Assert.Single(second.Value!.Items);
        Assert.Equal(noRecords.Id, emptyVehicle.Vehicle.VehicleId);
        Assert.Equal(7, emptyVehicle.MissingDays);
        Assert.Equal(2, second.Value.TotalCount);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task FiltersApplyBeforeSummaryAndPagingAndHugePageCannotOverflow()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = Context();
        var selected = AddVehicle(db, "V-111");
        var otherCity = AddVehicle(db, "V-112");
        otherCity.OperatingCityId = OperatingCity.RiyadhId;
        var otherType = AddVehicle(db, "V-113");
        otherType.VehicleType = VehicleType.Motorcycle;
        await db.SaveChangesAsync(ct);
        var service = Service(db);
        var result = await service.GetMissingRecordsReportAsync(Start, Start, "V-11", OperatingCity.JeddahId, VehicleType.Car, 0, 1000, ct);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(selected.Id, Assert.Single(result.Value!.Items).Vehicle.VehicleId);
        Assert.Equal(1, result.Value.WorkingVehicleCount);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(1L, result.Value.TotalMissingDays);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(100, result.Value.PageSize);
        var beyond = await service.GetMissingRecordsReportAsync(Start, Start, null, null, null, int.MaxValue, 100, ct);
        Assert.Empty(beyond.Value!.Items);
        Assert.Equal(3, beyond.Value.TotalCount);
        var noMatches = await service.GetMissingRecordsReportAsync(Start, Start, "NO-MATCH", null, null, 1, 0, ct);
        Assert.Empty(noMatches.Value!.Items);
        Assert.Equal(0, noMatches.Value.WorkingVehicleCount);
        Assert.Equal(0L, noMatches.Value.TotalMissingDays);
        Assert.Equal(50, noMatches.Value.PageSize);
    }

    [Fact]
    public async Task PeriodLimitsMissingVehiclesAndAuthorizationReturnExpectedFailures()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = Context();
        var vehicle = AddVehicle(db, "V001", deleted: true);
        await db.SaveChangesAsync(ct);
        var service = Service(db);
        foreach (var period in new[] { (default(DateOnly), Start), (Start, Start.AddDays(-1)), (Start, Start.AddDays(366)) })
        {
            var detail = await service.GetVehiclePeriodReportAsync(vehicle.Id, period.Item1, period.Item2, ct);
            var missing = await service.GetMissingRecordsReportAsync(period.Item1, period.Item2, null, null, null, 1, 50, ct);
            Assert.Equal(FleetErrors.InvalidDistanceReportPeriod.Code, detail.Error.Code);
            Assert.Equal(FleetErrors.InvalidDistanceReportPeriod.Code, missing.Error.Code);
        }
        var deleted = await service.GetVehiclePeriodReportAsync(vehicle.Id, Start, Start, ct);
        Assert.Equal(FleetErrors.NotFound.Code, deleted.Error.Code);
        var absent = await service.GetVehiclePeriodReportAsync(Guid.NewGuid(), Start, Start, ct);
        Assert.Equal(FleetErrors.NotFound.Code, absent.Error.Code);
        var maxRange = await service.GetMissingRecordsReportAsync(Start, Start.AddDays(365), null, null, null, 1, 50, ct);
        Assert.True(maxRange.IsSuccess, maxRange.Error.Description);
        Assert.Equal(366, maxRange.Value!.TotalDays);
        var denied = Service(db, allow: false);
        Assert.Equal(FleetErrors.Forbidden.Code, (await denied.GetVehiclePeriodReportAsync(vehicle.Id, Start, Start, ct)).Error.Code);
        Assert.Equal(FleetErrors.Forbidden.Code, (await denied.GetMissingRecordsReportAsync(Start, Start, null, null, null, 1, 50, ct)).Error.Code);
        var invalidType = await service.GetMissingRecordsReportAsync(Start, Start, null, null, (VehicleType)99, 1, 50, ct);
        Assert.Equal(FleetErrors.InvalidRequest.Code, invalidType.Error.Code);
    }

    [Fact]
    public async Task DateOnlyMaximumAndLeapDayAreIncludedWithoutOverflow()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = Context();
        var vehicle = AddVehicle(db, "V001");
        await db.SaveChangesAsync(ct);
        var service = Service(db);
        var max = await service.GetVehiclePeriodReportAsync(vehicle.Id, DateOnly.MaxValue, DateOnly.MaxValue, ct);
        Assert.Equal(DateOnly.MaxValue, Assert.Single(max.Value!.Days).WorkDate);
        var leap = await service.GetVehiclePeriodReportAsync(vehicle.Id, new DateOnly(2024, 2, 28), new DateOnly(2024, 3, 1), ct);
        Assert.Equal(3, leap.Value!.TotalDays);
        Assert.Equal(new DateOnly(2024, 2, 29), leap.Value.Days[1].WorkDate);
    }

    [Fact]
    public async Task ReportsExecuteAgainstHostedSqlServerWhenConfigured()
    {
        var connectionString = Environment.GetEnvironmentVariable("LOGISTICS_INTEGRATION_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        var ct = TestContext.Current.CancellationToken;
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString).Options);
        var service = Service(db);
        var missing = await service.GetMissingRecordsReportAsync(Start, Start.AddDays(29), null, null, null, 1, 5, ct);
        Assert.True(missing.IsSuccess, missing.Error.Description);
        Assert.All(missing.Value!.Items, item =>
        {
            Assert.Equal(VehicleOperationalStatus.Assigned, item.Vehicle.CurrentOperationalStatus);
            Assert.Equal(item.MissingDates.Count, item.MissingDays);
            Assert.Equal(30, item.RecordedDays + item.MissingDays);
        });
        var vehicleId = await db.Vehicles.AsNoTracking().Select(x => x.Id).FirstOrDefaultAsync(ct);
        Assert.NotEqual(Guid.Empty, vehicleId);
        var detail = await service.GetVehiclePeriodReportAsync(vehicleId, Start, Start.AddDays(29), ct);
        Assert.True(detail.IsSuccess, detail.Error.Description);
        Assert.Equal(30, detail.Value!.Days.Count);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"DistanceReport_{Guid.NewGuid():N}", options => options.EnableNullChecks(false)).Options);

    private static VehicleDailyDistanceService Service(ApplicationDbContext db, bool allow = true) =>
        new(db, new FleetServiceSupport(new CurrentUser(), new PermissionChecker(allow), TimeProvider.System));

    private static Vehicle AddVehicle(ApplicationDbContext db, string asset,
        VehicleOperationalStatus status = VehicleOperationalStatus.Assigned, bool deleted = false)
    {
        var vehicle = new Vehicle
        {
            AssetNumber = asset, NormalizedAssetNumber = FleetServiceSupport.NormalizeIdentifier(asset),
            VehicleType = VehicleType.Car, CurrentOperationalStatus = status,
            OperatingCityId = OperatingCity.JeddahId, IsDeleted = deleted, TrackedDistanceKm = 10000m
        };
        db.Vehicles.Add(vehicle);
        return vehicle;
    }

    private static void AddDay(ApplicationDbContext db, Vehicle vehicle, int day,
        VehicleDailyDistanceSource source, decimal applied, decimal? gps = null, decimal? manual = null, bool deleted = false) =>
        db.VehicleDailyDistances.Add(new VehicleDailyDistance
        {
            VehicleId = vehicle.Id, WorkDate = Start.AddDays(day - 1), AppliedSource = source,
            AppliedDistanceKm = applied, GpsDistanceKm = gps, ManualDistanceKm = manual,
            ManualOdometerReading = manual.HasValue ? 10015 : null,
            ManualBaselineOdometerReading = manual.HasValue ? 10000m : null,
            EffectiveOdometerAfterKm = 10000m + applied, IsDeleted = deleted,
            ManualNotes = manual.HasValue ? "Manual note" : null,
            ManualEnteredByUserId = manual.HasValue ? Guid.NewGuid() : null
        });

    private sealed class CurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => null;
    }

    private sealed class PermissionChecker(bool allow) : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(allow);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
}
