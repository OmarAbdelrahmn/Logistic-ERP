using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class FleetModelTests
{
    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=FleetModelTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

    [Fact]
    public void AssignmentIndexesGuaranteeOneActiveVehiclePerRiderAndVehicle()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(RiderVehicleAssignment))!;
        var indexes = entity.GetIndexes().ToArray();

        var riderIndex = Assert.Single(indexes, index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(RiderVehicleAssignment.RiderProfileId)]));
        var vehicleIndex = Assert.Single(indexes, index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(RiderVehicleAssignment.VehicleId)]));
        Assert.True(riderIndex.IsUnique);
        Assert.True(vehicleIndex.IsUnique);
        Assert.Equal("[EndedAtUtc] IS NULL AND [IsDeleted] = 0", riderIndex.GetFilter());
        Assert.Equal("[EndedAtUtc] IS NULL AND [IsDeleted] = 0", vehicleIndex.GetFilter());
    }

    [Fact]
    public void StatusTimelineGuaranteesOneOpenPeriodPerVehicle()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(VehicleOperationalStatusPeriod))!;
        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.IsUnique && candidate.Properties.Select(property => property.Name).SequenceEqual([nameof(VehicleOperationalStatusPeriod.VehicleId)]));

        Assert.Equal("[EffectiveToUtc] IS NULL AND [IsDeleted] = 0", index.GetFilter());
    }

    [Fact]
    public void VehicleIdentifiersAndConcurrencyAreDatabaseProtected()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Vehicle))!;

        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedAssetNumber));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedPlateNumberAr));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedPlateNumberEn));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.Vin));

        var rowVersion = entity.FindProperty(nameof(Vehicle.RowVersion))!;
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
    }

    [Fact]
    public void LegacyTemporaryVehicleOperationIsNotPartOfTheModel()
    {
        using var context = CreateContext();

        Assert.DoesNotContain(context.Model.GetEntityTypes(), entity =>
            entity.ClrType.Name.Contains("TempVehicleOperation", StringComparison.Ordinal));
    }
}
