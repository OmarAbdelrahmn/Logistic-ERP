using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HousingRoomModelTests
{
    [Fact]
    public void RoomCapacityAndAssignmentRelationshipsAreDatabaseConstrained()
    {
        using var db = CreateContext();
        var model = db.GetService<IDesignTimeModel>().Model;
        var room = model.FindEntityType(typeof(HousingRoom))!;
        var period = model.FindEntityType(typeof(HousingResidencePeriod))!;

        Assert.Contains(room.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_HousingRooms_CurrentOccupancy"
            && constraint.Sql!.Contains("[CurrentOccupancy] <= [Capacity]", StringComparison.Ordinal));
        Assert.Contains(room.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(HousingRoom.HousingId), nameof(HousingRoom.Name)]));
        Assert.Contains(period.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(HousingResidencePeriod.RoomId)])
            && foreignKey.PrincipalEntityType.ClrType == typeof(HousingRoom));
    }

    [Fact]
    public void OneEmployeeCanSuperviseMultipleHousingLocations()
    {
        using var db = CreateContext();
        var supervisor = db.Model.FindEntityType(typeof(HousingSupervisorPeriod))!;

        Assert.DoesNotContain(supervisor.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(HousingSupervisorPeriod.SupervisorEmployeeId)]));
        Assert.Contains(supervisor.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(HousingSupervisorPeriod.HousingId)]));
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HousingRoomModel;Trusted_Connection=True")
            .Options);
}
