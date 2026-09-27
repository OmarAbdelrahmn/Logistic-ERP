using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleRiderPeriodReportServiceTests
{
    [Fact]
    public async Task VehicleReportShowsEachHandoverAndVehiclesWithoutAssignments()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var fixture = await SeedHandoverAsync(dbContext, cancellationToken);
        var service = CreateService(dbContext, LocalTime(20));

        var result = await service.GetByVehicleAsync(new(2026, 9, 1), new(2026, 9, 20), cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(2, result.Value!.Vehicles.Count);
        var vehicle = Assert.Single(result.Value.Vehicles, x => x.VehicleId == fixture.VehicleId);
        Assert.Equal(15m, vehicle.TotalDaysAssignedInPeriod);
        Assert.Collection(vehicle.Assignments,
            first =>
            {
                Assert.Equal(fixture.FirstAssignmentId, first.AssignmentId);
                Assert.True(first.IsRealRider);
                Assert.Equal("Assigned A", first.ActualRiderName);
                Assert.Equal(10m, first.DaysInPeriod);
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
            });
        var unused = Assert.Single(result.Value.Vehicles, x => x.VehicleId == fixture.UnusedVehicleId);
        Assert.Empty(unused.Assignments);
        Assert.Equal(0m, unused.TotalDaysAssignedInPeriod);
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
        var actualM = Assert.Single(result.Value.Riders, x => x.RiderIqamaNo == "2222222222");
        Assert.Null(actualM.RiderProfileId);
        Assert.Equal("Actual M", actualM.RiderName);
        Assert.Equal(5m, actualM.TotalDaysWithVehiclesInPeriod);
        Assert.Equal(fixture.SecondProfileId, Assert.Single(actualM.Assignments).AssignedRiderProfileId);
        Assert.DoesNotContain(result.Value.Riders, x => x.RiderProfileId == fixture.SecondProfileId);
    }

    [Fact]
    public async Task PartialAndOpenAssignmentsAreClippedToPeriodAndAsOfTime()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = new Vehicle { AssetNumber = "CAR-1", NormalizedAssetNumber = "CAR1" };
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
        var items = Assert.Single(result.Value!.Vehicles).Assignments;
        Assert.Equal(2m, items[0].DaysInPeriod);
        Assert.Equal(LocalTime(3), items[0].PeriodStartedAtUtc);
        Assert.Equal(9m, items[0].TotalAssignmentDays);
        Assert.Null(items[1].EndedAtUtc);
        Assert.Equal(LocalTime(15).AddHours(12), items[1].PeriodEndedAtUtc);
        Assert.Equal(5.5m, items[1].DaysInPeriod);
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
        var vehicle = new Vehicle { AssetNumber = "CAR-A", NormalizedAssetNumber = "CARA" };
        var unused = new Vehicle { AssetNumber = "CAR-B", NormalizedAssetNumber = "CARB" };
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
        dbContext.AddRange(vehicle, unused, employeeA, employeeB, profileA, profileB, first, second, real);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(vehicle.Id, unused.Id, profileA.Id, profileB.Id, first.Id, second.Id);
    }

    private sealed record HandoverFixture(
        Guid VehicleId, Guid UnusedVehicleId, Guid FirstProfileId, Guid SecondProfileId,
        Guid FirstAssignmentId, Guid SecondAssignmentId);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
    }
}
