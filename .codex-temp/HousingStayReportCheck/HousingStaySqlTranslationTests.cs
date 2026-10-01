using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HousingStaySqlTranslationTests
{
    private sealed record LinkedProjection(Guid Id, string HousingCode, string RoomName,
        string? FloorName, Guid? RiderId, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
    private sealed record UnlinkedProjection(Guid Id, string HousingCode, string RoomName,
        string? FloorName, string? IqamaNo, int? SourceRow);

    [Fact]
    public void SqlServerTranslatesReportCountAndPageQueries()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=invalid;Database=diagnostic;User Id=invalid;Password=invalid;TrustServerCertificate=True")
            .Options);
        DateOnly? fromDate = new(2026, 9, 1);
        DateOnly? toDate = new(2026, 9, 30);
        Guid? housingId = null;
        var linked =
            from period in db.HousingResidencePeriods.AsNoTracking()
            join room in db.HousingRooms.IgnoreQueryFilters().AsNoTracking() on period.RoomId equals room.Id
            join housing in db.Housing.IgnoreQueryFilters().AsNoTracking() on room.HousingId equals housing.Id
            join employee in db.Employees.IgnoreQueryFilters().AsNoTracking() on period.EmployeeId equals employee.Id
            join floor in db.HousingFloors.IgnoreQueryFilters().AsNoTracking() on room.FloorId equals floor.Id into floors
            from floor in floors.DefaultIfEmpty()
            join rider in db.RiderProfiles.IgnoreQueryFilters().AsNoTracking() on employee.Id equals rider.EmployeeId into riders
            from rider in riders.DefaultIfEmpty()
            where (housingId == null || housing.Id == housingId)
                && (fromDate == null || period.EffectiveTo == null || period.EffectiveTo >= fromDate)
                && (toDate == null || period.EffectiveFrom <= toDate)
            select new { Period = period, Room = room, Housing = housing, Employee = employee, Floor = floor, Rider = rider };
        var page = linked
            .OrderBy(item => item.Housing.Code).ThenBy(item => item.Room.Name)
            .ThenBy(item => item.Period.EffectiveFrom).ThenBy(item => item.Period.Id)
            .Skip(0).Take(5000)
            .Select(item => new LinkedProjection(item.Period.Id, item.Housing.Code,
                item.Room.Name, item.Floor == null ? null : item.Floor.Name,
                item.Rider == null ? null : (Guid?)item.Rider.Id,
                item.Period.EffectiveFrom, item.Period.EffectiveTo));
        Assert.Contains("SELECT", linked.ToQueryString());
        Assert.Contains("SELECT", page.ToQueryString());

        var external = from person in db.HousingExternalOccupants.AsNoTracking()
                       join room in db.HousingRooms.AsNoTracking() on person.RoomId equals room.Id
                       join housing in db.Housing.AsNoTracking() on room.HousingId equals housing.Id
                       join floor in db.HousingFloors.AsNoTracking() on room.FloorId equals floor.Id into floors
                       from floor in floors.DefaultIfEmpty()
                       where housingId == null || housing.Id == housingId
                       select new { Person = person, Room = room, Housing = housing, Floor = floor };
        var externalPage = external.OrderBy(item => item.Housing.Code)
            .ThenBy(item => item.Room.Name).ThenBy(item => item.Person.Id)
            .Skip(0).Take(5000)
            .Select(item => new UnlinkedProjection(item.Person.Id, item.Housing.Code,
                item.Room.Name, item.Floor == null ? null : item.Floor.Name, null, null));
        Assert.Contains("SELECT", external.ToQueryString());
        Assert.Contains("SELECT", externalPage.ToQueryString());

        var pending = from person in db.HousingPendingOccupants.AsNoTracking()
                      join room in db.HousingRooms.AsNoTracking() on person.RoomId equals room.Id
                      join housing in db.Housing.AsNoTracking() on room.HousingId equals housing.Id
                      join floor in db.HousingFloors.AsNoTracking() on room.FloorId equals floor.Id into floors
                      from floor in floors.DefaultIfEmpty()
                      where housingId == null || housing.Id == housingId
                      select new { Person = person, Room = room, Housing = housing, Floor = floor };
        var pendingPage = pending.OrderBy(item => item.Housing.Code)
            .ThenBy(item => item.Room.Name).ThenBy(item => item.Person.Id)
            .Skip(0).Take(5000)
            .Select(item => new UnlinkedProjection(item.Person.Id, item.Housing.Code,
                item.Room.Name, item.Floor == null ? null : item.Floor.Name,
                item.Person.IqamaNo, item.Person.SourceRow));
        Assert.Contains("SELECT", pending.ToQueryString());
        Assert.Contains("SELECT", pendingPage.ToQueryString());
    }
}
