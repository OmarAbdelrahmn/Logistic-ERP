using System.Text.Json;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleRiderPeriodReportServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VehicleReportShowsEachHandoverAndVehiclesWithoutAssignments(bool sponsorIsDeleted)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var fixture = await SeedHandoverAsync(dbContext, cancellationToken);
        dbContext.Sponsors.Single(x => x.Id == fixture.SponsorId).IsDeleted = sponsorIsDeleted;
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext, LocalTime(20));

        var result = await service.GetByVehicleAsync(new(2026, 9, 1), new(2026, 9, 20), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(2, result.Value!.Vehicles.Count);
        var vehicle = Assert.Single(result.Value.Vehicles, x => x.VehicleId == fixture.VehicleId);
        Assert.Equal(fixture.SponsorId, vehicle.SponsorId);
        Assert.Equal("مؤسسة البوابة التجارية", vehicle.SponsorName);
        Assert.Equal(15m, vehicle.TotalDaysAssignedInPeriod);
        Assert.Equal(900m, vehicle.TotalAmountToCollectInPeriodSar);
        Assert.Collection(vehicle.Assignments,
            first =>
            {
                Assert.Equal(fixture.FirstAssignmentId, first.AssignmentId);
                Assert.True(first.IsRealRider);
                Assert.Equal("Assigned A", first.ActualRiderName);
                Assert.Equal(10m, first.DaysInPeriod);
                Assert.Equal(600m, first.CostInPeriodSar);
                Assert.Equal(LocalTime(1), first.StartedAtUtc);
                Assert.Equal(LocalTime(11), first.EndedAtUtc);
            },
            second =>
            {
                Assert.Equal(fixture.SecondAssignmentId, second.AssignmentId);
                Assert.False(second.IsRealRider);
                Assert.Equal("Actual M", second.ActualRiderName);
                Assert.Equal("Assigned B", second.AssignedRiderName);
                Assert.Equal(5m, second.DaysInPeriod);
                Assert.Equal(300m, second.CostInPeriodSar);
            });
        var unused = Assert.Single(result.Value.Vehicles, x => x.VehicleId == fixture.UnusedVehicleId);
        Assert.Null(unused.SponsorId);
        Assert.Null(unused.SponsorName);
        Assert.Empty(unused.Assignments);
        Assert.Equal(0m, unused.TotalDaysAssignedInPeriod);
        Assert.Equal(0m, unused.TotalAmountToCollectInPeriodSar);
        var json = JsonSerializer.SerializeToElement(result.Value, JsonSerializerOptions.Web);
        var vehicleJson = json.GetProperty("vehicles")[0];
        Assert.Equal(fixture.SponsorId, vehicleJson.GetProperty("sponsorId").GetGuid());
        Assert.Equal("مؤسسة البوابة التجارية", vehicleJson.GetProperty("sponsorName").GetString());
        var unusedJson = json.GetProperty("vehicles")[1];
        Assert.Equal(JsonValueKind.Null, unusedJson.GetProperty("sponsorId").ValueKind);
        Assert.Equal(JsonValueKind.Null, unusedJson.GetProperty("sponsorName").ValueKind);
        Assert.Equal(900m, vehicleJson.GetProperty("totalAmountToCollectInPeriodSar").GetDecimal());
        var assignmentJson = vehicleJson.GetProperty("assignments")[0];
        Assert.Equal("Assigned A", assignmentJson.GetProperty("actualRiderName").GetString());
        Assert.Equal(600m, assignmentJson.GetProperty("costInPeriodSar").GetDecimal());
    }

    [Fact]
    public async Task RiderReportGroupsByPersonWhoActuallyDrove()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var fixture = await SeedHandoverAsync(dbContext, cancellationToken);
        var service = CreateService(dbContext, LocalTime(20));

        var result = await service.GetByRiderAsync(new(2026, 9, 1), new(2026, 9, 20), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(2, result.Value!.Riders.Count);
        var assignedA = Assert.Single(result.Value.Riders, x => x.RiderProfileId == fixture.FirstProfileId);
        Assert.Equal(10m, assignedA.TotalDaysWithVehiclesInPeriod);
        Assert.Equal(600m, assignedA.TotalVehicleCostInPeriodSar);
        var actualM = Assert.Single(result.Value.Riders, x => x.RiderIqamaNo == "2222222222");
        Assert.Null(actualM.RiderProfileId);
        Assert.Equal("Actual M", actualM.RiderName);
        Assert.Equal(5m, actualM.TotalDaysWithVehiclesInPeriod);
        Assert.Equal(300m, actualM.TotalVehicleCostInPeriodSar);
        Assert.Equal(fixture.SecondProfileId, Assert.Single(actualM.Assignments).AssignedRiderProfileId);
        Assert.DoesNotContain(result.Value.Riders, x => x.RiderProfileId == fixture.SecondProfileId);
    }

    [Fact]
    public async Task PartialAndOpenAssignmentsAreClippedToPeriodAndAsOfTime()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = new Vehicle
        {
            AssetNumber = "CAR-1", NormalizedAssetNumber = "CAR1", VehicleType = VehicleType.Car
        };
        var employee = new Employee { FullNameAr = "Rider A", IqamaNo = "1111111111" };
        var profile = new RiderProfile { EmployeeId = employee.Id };
        var closed = new RiderVehicleAssignment
        {
            VehicleId = vehicle.Id, RiderProfileId = profile.Id,
            StartedAtUtc = LocalTime(1).AddDays(-5), EndedAtUtc = LocalTime(5)
        };
        var open = new RiderVehicleAssignment
        {
            VehicleId = vehicle.Id, RiderProfileId = profile.Id,
            StartedAtUtc = LocalTime(10)
        };
        dbContext.AddRange(vehicle, employee, profile, closed, open);
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext, LocalTime(15).AddHours(12));

        var result = await service.GetByVehicleAsync(new(2026, 9, 3), new(2026, 9, 30), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        var vehicleRow = Assert.Single(result.Value!.Vehicles);
        Assert.Equal(420m, vehicleRow.TotalAmountToCollectInPeriodSar);
        var items = vehicleRow.Assignments;
        Assert.Equal(2m, items[0].DaysInPeriod);
        Assert.Equal(LocalTime(3), items[0].PeriodStartedAtUtc);
        Assert.Equal(9m, items[0].TotalAssignmentDays);
        Assert.Equal(120m, items[0].CostInPeriodSar);
        Assert.Null(items[1].EndedAtUtc);
        Assert.Equal(LocalTime(15).AddHours(12), items[1].PeriodEndedAtUtc);
        Assert.Equal(5m, items[1].DaysInPeriod);
        Assert.Equal(5m, items[1].TotalAssignmentDays);
        Assert.Equal(300m, items[1].CostInPeriodSar);
    }

    [Fact]
    public async Task SeparateRealRiderRecordsWithSameIqamaFormOneRiderRow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var fixture = await SeedHandoverAsync(dbContext, cancellationToken);
        var secondVehicle = dbContext.Vehicles.Single(x => x.Id == fixture.UnusedVehicleId);
        var next = new RiderVehicleAssignment
        {
            VehicleId = secondVehicle.Id,
            RiderProfileId = fixture.SecondProfileId,
            IsRealRider = false,
            StartedAtUtc = LocalTime(16),
            EndedAtUtc = LocalTime(18)
        };
        dbContext.Add(next);
        dbContext.RealRiders.Add(new RealRider
        {
            RiderVehicleAssignmentId = next.Id,
            Name = "Actual M",
            IqamaNo = "2222222222",
            RelationshipToAssignedRider = "Substitute"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext, LocalTime(20));

        var result = await service.GetByRiderAsync(new(2026, 9, 1), new(2026, 9, 20), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        var rider = Assert.Single(result.Value!.Riders, x => x.RiderIqamaNo == "2222222222");
        Assert.Equal(2, rider.Assignments.Count);
        Assert.Equal(7m, rider.TotalDaysWithVehiclesInPeriod);
        Assert.Equal(420m, rider.TotalVehicleCostInPeriodSar);
        Assert.NotEqual(rider.Assignments[0].VehicleId, rider.Assignments[1].VehicleId);
    }

    [Fact]
    public async Task MissingRealRiderDoesNotAttributeDrivingToAssignedProfile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = new Vehicle { AssetNumber = "CAR-1", NormalizedAssetNumber = "CAR1" };
        var employee = new Employee { FullNameAr = "Assigned Only", IqamaNo = "1111111111" };
        var profile = new RiderProfile { EmployeeId = employee.Id };
        var assignment = new RiderVehicleAssignment
        {
            VehicleId = vehicle.Id,
            RiderProfileId = profile.Id,
            IsRealRider = false,
            StartedAtUtc = LocalTime(1),
            EndedAtUtc = LocalTime(2)
        };
        dbContext.AddRange(vehicle, employee, profile, assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext, LocalTime(20));

        var result = await service.GetByRiderAsync(new(2026, 9, 1), new(2026, 9, 20), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        var rider = Assert.Single(result.Value!.Riders);
        Assert.StartsWith("unidentified:", rider.RiderKey);
        Assert.Null(rider.RiderName);
        Assert.Null(rider.RiderProfileId);
        Assert.Equal(profile.Id, Assert.Single(rider.Assignments).AssignedRiderProfileId);
    }

    [Fact]
    public async Task RiderCostIncludesCarAndMotorcycleUseAndExcludesUnassignedGap()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var car = new Vehicle
        {
            AssetNumber = "CAR-1", NormalizedAssetNumber = "CAR1", VehicleType = VehicleType.Car
        };
        var motorcycle = new Vehicle
        {
            AssetNumber = "BIKE-1", NormalizedAssetNumber = "BIKE1", VehicleType = VehicleType.Motorcycle
        };
        var employee = new Employee { FullNameAr = "Rider A", IqamaNo = "1111111111" };
        var profile = new RiderProfile { EmployeeId = employee.Id };
        dbContext.AddRange(car, motorcycle, employee, profile,
            new RiderVehicleAssignment
            {
                VehicleId = car.Id, RiderProfileId = profile.Id,
                StartedAtUtc = LocalTime(1), EndedAtUtc = LocalTime(7)
            },
            new RiderVehicleAssignment
            {
                VehicleId = motorcycle.Id, RiderProfileId = profile.Id,
                StartedAtUtc = LocalTime(17), EndedAtUtc = LocalTime(1).AddMonths(1)
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext, LocalTime(1).AddMonths(1));

        var result = await service.GetByRiderAsync(new(2026, 9, 1), new(2026, 9, 30), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        var rider = Assert.Single(result.Value!.Riders);
        Assert.Equal(20m, rider.TotalDaysWithVehiclesInPeriod);
        Assert.Equal(733.33m, rider.TotalVehicleCostInPeriodSar);
        Assert.Collection(rider.Assignments,
            first =>
            {
                Assert.Equal(VehicleType.Car, first.VehicleType);
                Assert.Equal(1800m, first.MonthlyCostSar);
                Assert.Equal(60m, first.DailyCostSar);
                Assert.Equal(6m, first.DaysInPeriod);
                Assert.Equal(360m, first.CostInPeriodSar);
            },
            second =>
            {
                Assert.Equal(VehicleType.Motorcycle, second.VehicleType);
                Assert.Equal(800m, second.MonthlyCostSar);
                Assert.Equal(800m / 30m, second.DailyCostSar);
                Assert.Equal(14m, second.DaysInPeriod);
                Assert.Equal(373.33m, second.CostInPeriodSar);
            });
        var json = JsonSerializer.SerializeToElement(result.Value, JsonSerializerOptions.Web);
        var riderJson = json.GetProperty("riders")[0];
        Assert.Equal(733.33m, riderJson.GetProperty("totalVehicleCostInPeriodSar").GetDecimal());
        var assignmentJson = riderJson.GetProperty("assignments")[0];
        Assert.Equal("Rider A", assignmentJson.GetProperty("actualRiderName").GetString());
        Assert.Equal(2, assignmentJson.GetProperty("vehicleType").GetInt32());
        Assert.Equal(360m, assignmentJson.GetProperty("costInPeriodSar").GetDecimal());
        var vehicleResult = await service.GetByVehicleAsync(new(2026, 9, 1), new(2026, 9, 30), cancellationToken);
        Assert.True(vehicleResult.IsSuccess, vehicleResult.Error.Description);
        Assert.Equal(360m, Assert.Single(vehicleResult.Value!.Vehicles, x => x.VehicleId == car.Id)
            .TotalAmountToCollectInPeriodSar);
        Assert.Equal(373.33m, Assert.Single(vehicleResult.Value.Vehicles, x => x.VehicleId == motorcycle.Id)
            .TotalAmountToCollectInPeriodSar);
        Assert.Equal(rider.TotalVehicleCostInPeriodSar,
            vehicleResult.Value.Vehicles.Sum(x => x.TotalAmountToCollectInPeriodSar));
    }

    [Fact]
    public async Task RiderCostUsesClippedPeriodAndCapsOpenUseAtAsOfTime()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = new Vehicle
        {
            AssetNumber = "CAR-1", NormalizedAssetNumber = "CAR1", VehicleType = VehicleType.Car
        };
        var employee = new Employee { FullNameAr = "Rider A", IqamaNo = "1111111111" };
        var profile = new RiderProfile { EmployeeId = employee.Id };
        dbContext.AddRange(vehicle, employee, profile,
            new RiderVehicleAssignment
            {
                VehicleId = vehicle.Id, RiderProfileId = profile.Id,
                StartedAtUtc = LocalTime(1).AddDays(-5), EndedAtUtc = LocalTime(5)
            },
            new RiderVehicleAssignment
            {
                VehicleId = vehicle.Id, RiderProfileId = profile.Id, StartedAtUtc = LocalTime(10)
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext, LocalTime(15).AddHours(12));

        var result = await service.GetByRiderAsync(new(2026, 9, 3), new(2026, 9, 30), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        var rider = Assert.Single(result.Value!.Riders);
        Assert.Equal(420m, rider.TotalVehicleCostInPeriodSar);
        Assert.Equal(120m, rider.Assignments[0].CostInPeriodSar);
        Assert.Equal(300m, rider.Assignments[1].CostInPeriodSar);
    }

    [Theory]
    [InlineData(VehicleType.Motorcycle, 2592000, 800)] // 30 days
    [InlineData(VehicleType.Motorcycle, 43200, 0)] // Returned before the cutoff on pickup day.
    [InlineData(VehicleType.Motorcycle, 50401, 26.67)] // Returned just after 14:00.
    [InlineData(VehicleType.Car, 7, 0)]
    [InlineData(VehicleType.Car, 108, 0)]
    public async Task RiderCostUsesWholeBillableDaysAndRoundsTheFinalAmount(
        VehicleType vehicleType, int seconds, decimal expectedCost)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = new Vehicle
        {
            AssetNumber = "VEHICLE-1", NormalizedAssetNumber = "VEHICLE1", VehicleType = vehicleType
        };
        var employee = new Employee { FullNameAr = "Rider A", IqamaNo = "1111111111" };
        var profile = new RiderProfile { EmployeeId = employee.Id };
        dbContext.AddRange(vehicle, employee, profile, new RiderVehicleAssignment
        {
            VehicleId = vehicle.Id, RiderProfileId = profile.Id,
            StartedAtUtc = LocalTime(1), EndedAtUtc = LocalTime(1).AddSeconds(seconds)
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext, LocalTime(1).AddMonths(1));

        var result = await service.GetByRiderAsync(new(2026, 9, 1), new(2026, 9, 30), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        var rider = Assert.Single(result.Value!.Riders);
        Assert.Equal(expectedCost, Assert.Single(rider.Assignments).CostInPeriodSar);
        Assert.Equal(expectedCost, rider.TotalVehicleCostInPeriodSar);
        var vehicleResult = await service.GetByVehicleAsync(new(2026, 9, 1), new(2026, 9, 30), cancellationToken);
        Assert.True(vehicleResult.IsSuccess, vehicleResult.Error.Description);
        var vehicleRow = Assert.Single(vehicleResult.Value!.Vehicles);
        Assert.Equal(expectedCost, vehicleRow.TotalAmountToCollectInPeriodSar);
        Assert.Equal(expectedCost, Assert.Single(vehicleRow.Assignments).CostInPeriodSar);
    }

    [Theory]
    [InlineData(1, 839, 6, 839, 1, 30, 5, 5)]
    [InlineData(1, 840, 6, 840, 1, 30, 5, 5)]
    [InlineData(1, 841, 6, 839, 1, 30, 4, 4)]
    [InlineData(1, 839, 6, 841, 1, 30, 6, 6)]
    [InlineData(1, 841, 6, 841, 1, 30, 5, 5)]
    [InlineData(1, 600, 1, 839, 1, 30, 0, 0)]
    [InlineData(1, 600, 1, 840, 1, 30, 0, 0)]
    [InlineData(1, 600, 1, 841, 1, 30, 1, 1)]
    [InlineData(1, 841, 1, 900, 1, 30, 0, 0)]
    [InlineData(1, 841, 2, 839, 1, 30, 0, 0)]
    [InlineData(1, 841, 7, 839, 3, 5, 3, 5)]
    [InlineData(3, 841, 7, 841, 3, 5, 2, 4)]
    [InlineData(1, 839, 5, 839, 3, 5, 2, 4)]
    [InlineData(1, 839, 5, 841, 3, 5, 3, 5)]
    [InlineData(1, 839, 6, 839, 5, 5, 1, 5)]
    [InlineData(5, 841, 6, 839, 5, 5, 0, 0)]
    public async Task ReportsUseRiyadhCutoffAndClipBillableDatesWithoutChangingTimestamps(
        int startDay, int startMinute, int endDay, int endMinute,
        int fromDay, int toDay, decimal expectedDays, decimal expectedTotalDays)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var fixture = await SeedHandoverAsync(dbContext, cancellationToken);
        dbContext.RiderVehicleAssignments.Single(x => x.Id == fixture.SecondAssignmentId).IsDeleted = true;
        var assignment = dbContext.RiderVehicleAssignments.Single(x => x.Id == fixture.FirstAssignmentId);
        var start = LocalTime(startDay).AddMinutes(startMinute).ToUniversalTime();
        var end = LocalTime(endDay).AddMinutes(endMinute).ToUniversalTime();
        assignment.StartedAtUtc = start;
        assignment.EndedAtUtc = end;
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext, LocalTime(30));
        var fromDate = new DateOnly(2026, 9, fromDay);
        var toDate = new DateOnly(2026, 9, toDay);

        var riderResult = await service.GetByRiderAsync(fromDate, toDate, cancellationToken);
        var vehicleResult = await service.GetByVehicleAsync(fromDate, toDate, cancellationToken);

        Assert.True(riderResult.IsSuccess, riderResult.Error.Description);
        Assert.True(vehicleResult.IsSuccess, vehicleResult.Error.Description);
        var rider = Assert.Single(riderResult.Value!.Riders);
        var vehicle = Assert.Single(vehicleResult.Value!.Vehicles, x => x.VehicleId == fixture.VehicleId);
        var item = Assert.Single(rider.Assignments);
        Assert.Equal(expectedDays, item.DaysInPeriod);
        Assert.Equal(expectedTotalDays, item.TotalAssignmentDays);
        Assert.Equal(expectedDays, rider.TotalDaysWithVehiclesInPeriod);
        Assert.Equal(expectedDays, vehicle.TotalDaysAssignedInPeriod);
        Assert.Equal(expectedDays * 60m, item.CostInPeriodSar);
        Assert.Equal(expectedDays * 60m, rider.TotalVehicleCostInPeriodSar);
        Assert.Equal(expectedDays * 60m, vehicle.TotalAmountToCollectInPeriodSar);
        Assert.Equal(item, Assert.Single(vehicle.Assignments));
        Assert.Equal(start, item.StartedAtUtc);
        Assert.Equal(end, item.EndedAtUtc);
        Assert.Equal(start > LocalTime(fromDay) ? start : LocalTime(fromDay), item.PeriodStartedAtUtc);
        var reportEnd = LocalTime(toDay).AddDays(1);
        Assert.Equal(end < reportEnd ? end : reportEnd, item.PeriodEndedAtUtc);
    }

    [Theory]
    [InlineData(839, 5)]
    [InlineData(840, 5)]
    [InlineData(841, 6)]
    public async Task OpenAssignmentsUseAsOfRiyadhCutoff(int nowMinute, decimal expectedDays)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var fixture = await SeedHandoverAsync(dbContext, cancellationToken);
        dbContext.RiderVehicleAssignments.Single(x => x.Id == fixture.SecondAssignmentId).IsDeleted = true;
        var assignment = dbContext.RiderVehicleAssignments.Single(x => x.Id == fixture.FirstAssignmentId);
        assignment.EndedAtUtc = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        var asOf = LocalTime(6).AddMinutes(nowMinute).ToUniversalTime();
        var service = CreateService(dbContext, asOf);

        var result = await service.GetByRiderAsync(new(2026, 9, 1), new(2026, 9, 30), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        var rider = Assert.Single(result.Value!.Riders);
        var item = Assert.Single(rider.Assignments);
        Assert.Equal(expectedDays, item.DaysInPeriod);
        Assert.Equal(expectedDays, item.TotalAssignmentDays);
        Assert.Equal(expectedDays * 60m, rider.TotalVehicleCostInPeriodSar);
        Assert.Null(item.EndedAtUtc);
        Assert.Equal(asOf, item.PeriodEndedAtUtc);
        Assert.Equal(asOf, result.Value.AsOfUtc);
    }

    [Theory]
    [InlineData(839, 10, 5)]
    [InlineData(840, 10, 5)]
    [InlineData(841, 11, 4)]
    public async Task HandoverDayIsChargedToOneRiderAtCutoff(
        int handoverMinute, decimal firstDays, decimal secondDays)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var fixture = await SeedHandoverAsync(dbContext, cancellationToken);
        var handover = LocalTime(11).AddMinutes(handoverMinute).ToUniversalTime();
        dbContext.RiderVehicleAssignments.Single(x => x.Id == fixture.FirstAssignmentId).EndedAtUtc = handover;
        dbContext.RiderVehicleAssignments.Single(x => x.Id == fixture.SecondAssignmentId).StartedAtUtc = handover;
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext, LocalTime(20));

        var result = await service.GetByVehicleAsync(new(2026, 9, 1), new(2026, 9, 20), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        var vehicle = Assert.Single(result.Value!.Vehicles, x => x.VehicleId == fixture.VehicleId);
        Assert.Equal(firstDays, vehicle.Assignments[0].DaysInPeriod);
        Assert.Equal(secondDays, vehicle.Assignments[1].DaysInPeriod);
        Assert.Equal(15m, vehicle.TotalDaysAssignedInPeriod);
        Assert.Equal(900m, vehicle.TotalAmountToCollectInPeriodSar);
    }

    [Theory]
    [InlineData(VehicleType.Van)]
    [InlineData(VehicleType.Truck)]
    [InlineData(VehicleType.Other)]
    public async Task VehicleTypesWithoutRatesHaveUnavailableCosts(VehicleType vehicleType)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var fixture = await SeedHandoverAsync(dbContext, cancellationToken);
        var vehicle = dbContext.Vehicles.Single(x => x.Id == fixture.UnusedVehicleId);
        vehicle.VehicleType = vehicleType;
        dbContext.Add(new RiderVehicleAssignment
        {
            VehicleId = vehicle.Id, RiderProfileId = fixture.FirstProfileId,
            StartedAtUtc = LocalTime(12), EndedAtUtc = LocalTime(14)
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext, LocalTime(20));

        var result = await service.GetByRiderAsync(new(2026, 9, 1), new(2026, 9, 20), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        var rider = Assert.Single(result.Value!.Riders, x => x.RiderProfileId == fixture.FirstProfileId);
        Assert.Equal(12m, rider.TotalDaysWithVehiclesInPeriod);
        Assert.Null(rider.TotalVehicleCostInPeriodSar);
        Assert.Equal(600m, rider.Assignments[0].CostInPeriodSar);
        Assert.Null(rider.Assignments[1].MonthlyCostSar);
        Assert.Null(rider.Assignments[1].DailyCostSar);
        Assert.Null(rider.Assignments[1].CostInPeriodSar);
        var vehicleResult = await service.GetByVehicleAsync(new(2026, 9, 1), new(2026, 9, 20), cancellationToken);
        Assert.True(vehicleResult.IsSuccess, vehicleResult.Error.Description);
        var vehicleRow = Assert.Single(vehicleResult.Value!.Vehicles, x => x.VehicleId == vehicle.Id);
        Assert.Null(vehicleRow.TotalAmountToCollectInPeriodSar);
        Assert.Null(Assert.Single(vehicleRow.Assignments).CostInPeriodSar);
    }

    [Fact]
    public async Task VehicleTotalSumsRoundedRiderChargesAndIncludesUnusedVehiclesWithZero()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var fixture = await SeedHandoverAsync(dbContext, cancellationToken);
        dbContext.Vehicles.Single(x => x.Id == fixture.VehicleId).VehicleType = VehicleType.Motorcycle;
        dbContext.Vehicles.Single(x => x.Id == fixture.UnusedVehicleId).VehicleType = VehicleType.Other;
        dbContext.RiderVehicleAssignments.Single(x => x.Id == fixture.FirstAssignmentId).EndedAtUtc = LocalTime(2);
        var second = dbContext.RiderVehicleAssignments.Single(x => x.Id == fixture.SecondAssignmentId);
        second.StartedAtUtc = LocalTime(3);
        second.EndedAtUtc = LocalTime(4);
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext, LocalTime(20));

        var vehicleResult = await service.GetByVehicleAsync(new(2026, 9, 1), new(2026, 9, 20), cancellationToken);
        var riderResult = await service.GetByRiderAsync(new(2026, 9, 1), new(2026, 9, 20), cancellationToken);

        Assert.True(vehicleResult.IsSuccess, vehicleResult.Error.Description);
        Assert.True(riderResult.IsSuccess, riderResult.Error.Description);
        var vehicle = Assert.Single(vehicleResult.Value!.Vehicles, x => x.VehicleId == fixture.VehicleId);
        Assert.Equal(26.67m, vehicle.Assignments[0].CostInPeriodSar);
        Assert.Equal(26.67m, vehicle.Assignments[1].CostInPeriodSar);
        Assert.Equal(2m, vehicle.TotalDaysAssignedInPeriod);
        Assert.Equal(53.34m, vehicle.TotalAmountToCollectInPeriodSar);
        Assert.Equal(riderResult.Value!.Riders.Sum(x => x.TotalVehicleCostInPeriodSar),
            vehicle.TotalAmountToCollectInPeriodSar);
        var unused = Assert.Single(vehicleResult.Value.Vehicles, x => x.VehicleId == fixture.UnusedVehicleId);
        Assert.Empty(unused.Assignments);
        Assert.Equal(0m, unused.TotalAmountToCollectInPeriodSar);
    }

    [Fact]
    public async Task InvalidDateRangeReturnsValidationFailure()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var service = CreateService(dbContext, LocalTime(20));

        var result = await service.GetByRiderAsync(new(2026, 9, 20), new(2026, 9, 1), cancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("fleet.assignment_period.invalid_period", result.Error.Code);
    }

    private static VehicleRiderPeriodReportService CreateService(ApplicationDbContext dbContext, DateTimeOffset now) =>
        new(dbContext, new FixedTimeProvider(now));

    private static DateTimeOffset LocalTime(int day) =>
        new(2026, 9, day, 0, 0, 0, TimeSpan.FromHours(3));

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VehicleRiderPeriodReport_{Guid.NewGuid():N}",
                options => options.EnableNullChecks(false))
            .Options,
        TimeProvider.System);

    private static async Task<HandoverFixture> SeedHandoverAsync(
        ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var sponsor = new Sponsor { RegistryNameAr = "مؤسسة البوابة التجارية" };
        var registeredOwner = new Sponsor { RegistryNameAr = "Registered owner" };
        var vehicle = new Vehicle
        {
            AssetNumber = "CAR-A", NormalizedAssetNumber = "CARA", VehicleType = VehicleType.Car,
            SponsorId = sponsor.Id, RegisteredOwnerSponsorId = registeredOwner.Id
        };
        var unused = new Vehicle
        {
            AssetNumber = "CAR-B", NormalizedAssetNumber = "CARB", VehicleType = VehicleType.Car
        };
        var employeeA = new Employee { FullNameAr = "Assigned A", IqamaNo = "1111111111" };
        var employeeB = new Employee { FullNameAr = "Assigned B", IqamaNo = "3333333333" };
        var profileA = new RiderProfile { EmployeeId = employeeA.Id };
        var profileB = new RiderProfile { EmployeeId = employeeB.Id };
        var first = new RiderVehicleAssignment
        {
            VehicleId = vehicle.Id, RiderProfileId = profileA.Id, IsRealRider = true,
            StartedAtUtc = LocalTime(1), EndedAtUtc = LocalTime(11)
        };
        var second = new RiderVehicleAssignment
        {
            VehicleId = vehicle.Id, RiderProfileId = profileB.Id, IsRealRider = false,
            StartedAtUtc = LocalTime(11), EndedAtUtc = LocalTime(16)
        };
        var real = new RealRider
        {
            RiderVehicleAssignmentId = second.Id, Name = "Actual M",
            IqamaNo = "2222222222", RelationshipToAssignedRider = "Substitute"
        };
        dbContext.AddRange(sponsor, registeredOwner, vehicle, unused, employeeA, employeeB,
            profileA, profileB, first, second, real);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(vehicle.Id, unused.Id, profileA.Id, profileB.Id, first.Id, second.Id, sponsor.Id);
    }

    private sealed record HandoverFixture(
        Guid VehicleId, Guid UnusedVehicleId, Guid FirstProfileId, Guid SecondProfileId,
        Guid FirstAssignmentId, Guid SecondAssignmentId, Guid SponsorId);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
    }
}
