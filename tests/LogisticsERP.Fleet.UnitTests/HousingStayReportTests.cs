using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HousingStayReportTests
{
    [Fact]
    public async Task DateWindowCountsOnlyOverlappingDaysAndOmitsUndatedPeopleFromPastWindow()
    {
        await using var db = CreateContext();
        await SeedAsync(db);
        var service = CreateService(db);

        var result = await service.GetStayReportAsync(
            new HousingStayReportRequest(new(2026, 9, 3), new(2026, 9, 10)),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.All(result.Value.Items, item => Assert.Equal("Rider", item.RecordType));
        var former = Assert.Single(result.Value.Items, item => item.MoveOutDate is not null);
        Assert.Equal(2, former.DaysInSelectedPeriod);
        Assert.Equal(4, former.TotalStayDays);
        Assert.False(former.IsCurrentlyInside);
        var current = Assert.Single(result.Value.Items, item => item.MoveOutDate is null);
        Assert.Equal(6, current.DaysInSelectedPeriod);
        Assert.Equal(16, current.TotalStayDays);
        Assert.True(current.IsCurrentlyInside);
    }

    [Fact]
    public async Task NoDatesReturnsAllHistoryAndCurrentUnlinkedPeopleAcrossPages()
    {
        await using var db = CreateContext();
        await SeedAsync(db);
        var service = CreateService(db);

        var first = await service.GetStayReportAsync(new HousingStayReportRequest(PageSize: 2), TestContext.Current.CancellationToken);
        var second = await service.GetStayReportAsync(new HousingStayReportRequest(Page: 2, PageSize: 2), TestContext.Current.CancellationToken);

        Assert.True(first.IsSuccess, first.Error.Description);
        Assert.True(second.IsSuccess, second.Error.Description);
        Assert.Equal(4, first.Value!.TotalCount);
        Assert.Equal(4, second.Value!.TotalCount);
        Assert.Equal(["Rider", "Rider"], first.Value.Items.Select(item => item.RecordType));
        Assert.Equal(["External", "PendingMatch"], second.Value.Items.Select(item => item.RecordType));
        Assert.All(second.Value.Items, item =>
        {
            Assert.True(item.IsCurrentlyInside);
            Assert.Null(item.MoveInDate);
            Assert.Null(item.MoveOutDate);
            Assert.Null(item.DaysInSelectedPeriod);
        });
        Assert.Null(second.Value.Items[0].IqamaNo);
        Assert.Equal("2560000000", second.Value.Items[1].IqamaNo);
    }

    [Fact]
    public async Task InvalidDateRangeReturnsValidationError()
    {
        await using var db = CreateContext();
        var result = await CreateService(db).GetStayReportAsync(
            new HousingStayReportRequest(new(2026, 9, 11), new(2026, 9, 10)),
            TestContext.Current.CancellationToken);
        Assert.False(result.IsSuccess);
        Assert.Equal("housing.invalid_toDate", result.Error.Code);
    }

    [Fact]
    public async Task WindowContainingTodayIncludesCurrentUnlinkedPeopleWithoutInventingDates()
    {
        await using var db = CreateContext();
        await SeedAsync(db);

        var result = await CreateService(db).GetStayReportAsync(
            new HousingStayReportRequest(new(2026, 9, 20), new(2026, 9, 20)),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(3, result.Value!.TotalCount);
        var rider = Assert.Single(result.Value.Items, item => item.RecordType == "Rider");
        Assert.Equal(1, rider.DaysInSelectedPeriod);
        Assert.Single(result.Value.Items, item => item.RecordType == "External");
        Assert.Single(result.Value.Items, item => item.RecordType == "PendingMatch");
    }

    [Fact]
    public async Task PageSizeAcceptsFiveThousandAndRejectsLargerValues()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var allowed = await service.GetStayReportAsync(
            new HousingStayReportRequest(PageSize: 5000), TestContext.Current.CancellationToken);
        var rejected = await service.GetStayReportAsync(
            new HousingStayReportRequest(PageSize: 5001), TestContext.Current.CancellationToken);

        Assert.True(allowed.IsSuccess, allowed.Error.Description);
        Assert.Equal(5000, allowed.Value!.PageSize);
        Assert.False(rejected.IsSuccess);
        Assert.Equal("hr.invalid_request", rejected.Error.Code);
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static HousingService CreateService(ApplicationDbContext db) =>
        new(db, new ReadOnlyUser(), new FixedTimeProvider(new DateTimeOffset(2026, 9, 20, 9, 0, 0, TimeSpan.Zero)));

    private static async Task SeedAsync(ApplicationDbContext db)
    {
        var housing = new Housing { Code = "H1", NameAr = "سكن 1", NameEn = "Housing 1" };
        var floor = new HousingFloor { HousingId = housing.Id, Name = "1" };
        var firstRoom = new HousingRoom { HousingId = housing.Id, FloorId = floor.Id, Name = "1", Capacity = 5 };
        var secondRoom = new HousingRoom { HousingId = housing.Id, FloorId = floor.Id, Name = "2", Capacity = 5 };
        var employee = new Employee { IqamaNo = "2550000000", FullNameAr = "راكب" };
        db.Housing.Add(housing);
        db.HousingFloors.Add(floor);
        db.HousingRooms.AddRange(firstRoom, secondRoom);
        db.Employees.Add(employee);
        db.RiderProfiles.Add(new RiderProfile { EmployeeId = employee.Id });
        db.HousingResidencePeriods.AddRange(
            new HousingResidencePeriod { EmployeeId = employee.Id, RoomId = firstRoom.Id, EffectiveFrom = new(2026, 9, 1), EffectiveTo = new(2026, 9, 4) },
            new HousingResidencePeriod { EmployeeId = employee.Id, RoomId = secondRoom.Id, EffectiveFrom = new(2026, 9, 5) });
        db.HousingExternalOccupants.Add(new HousingExternalOccupant { RoomId = firstRoom.Id, Name = "ضيف خارجي" });
        db.HousingPendingOccupants.Add(new HousingPendingOccupant { RoomId = firstRoom.Id, Name = "غير مطابق", IqamaNo = "2560000000", SourceRow = 7 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed class ReadOnlyUser : ICurrentUser
    {
        public Guid? UserId => null;
        public Guid? SessionId => null;
        public long? AuthorizationVersion => null;
        public string? CorrelationId => null;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
