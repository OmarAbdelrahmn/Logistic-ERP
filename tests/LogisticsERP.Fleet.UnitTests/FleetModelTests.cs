using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Vehicle))!;

        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedAssetNumber));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedSerialNumber));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedChassisNumber));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedPlateNumberAr));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.NormalizedPlateNumberEn));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.Vin));

        var rowVersion = entity.FindProperty(nameof(Vehicle.RowVersion))!;
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
    }

    [Fact]
    public void SaudiRegistrationTypesHaveStableMoiOrder()
    {
        Assert.Equal(
            [
                VehicleRegistrationType.Private,
                VehicleRegistrationType.PrivateTransport,
                VehicleRegistrationType.SmallBus,
                VehicleRegistrationType.Taxi,
                VehicleRegistrationType.PublicTransport,
                VehicleRegistrationType.PublicBus,
                VehicleRegistrationType.Motorcycle,
                VehicleRegistrationType.PublicWorks
            ],
            Enum.GetValues<VehicleRegistrationType>());
        Assert.Equal(Enumerable.Range(1, 8), Enum.GetValues<VehicleRegistrationType>().Select(value => (int)value));
    }

    [Fact]
    public void VehicleRegistrationTypeIsDatabaseConstrainedToEightValues()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Vehicle))!;

        Assert.Contains(entity.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_Vehicles_RegistrationType"
            && constraint.Sql.Contains("BETWEEN 1 AND 8", StringComparison.Ordinal));
    }

    [Fact]
    public void FixedVehicleFileSlotsAllowOnlyOneCurrentSlotPerVehicle()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(VehicleAttachment))!;
        var index = Assert.Single(entity.GetIndexes(), candidate => candidate.IsUnique
            && candidate.Properties.Select(property => property.Name).SequenceEqual([nameof(VehicleAttachment.VehicleId), nameof(VehicleAttachment.Kind)]));

        Assert.Equal("[Kind] <> 99 AND [IsDeleted] = 0", index.GetFilter());
    }

    [Fact]
    public void SupplierCommercialAndTaxNumbersUseFilteredUniqueIndexes()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(VehicleSupplier))!;

        foreach (var propertyName in new[] { nameof(VehicleSupplier.CommercialRegistrationNumber), nameof(VehicleSupplier.TaxNumber) })
        {
            var index = Assert.Single(entity.GetIndexes(), candidate => candidate.Properties.Select(property => property.Name).SequenceEqual([propertyName]));
            Assert.True(index.IsUnique);
            Assert.Contains("IS NOT NULL", index.GetFilter(), StringComparison.Ordinal);
            Assert.Contains("[IsDeleted] = 0", index.GetFilter(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RiderPromissoryVersionsAreLinkedToAssignmentsForAudit()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(RiderVehicleAssignmentPromissoryFile))!;
        var index = Assert.Single(entity.GetIndexes(), candidate => candidate.IsUnique);

        Assert.Equal(
            [nameof(RiderVehicleAssignmentPromissoryFile.RiderVehicleAssignmentId), nameof(RiderVehicleAssignmentPromissoryFile.RiderPromissoryFileVersionId)],
            index.Properties.Select(property => property.Name));
    }

    [Fact]
    public void FleetLocationIsRemovedFromTheModel()
    {
        using var context = CreateContext();

        Assert.DoesNotContain(context.Model.GetEntityTypes(), entity => entity.ClrType.Name == "FleetLocation");
        Assert.Null(context.Model.FindEntityType(typeof(Vehicle))!.FindProperty("CurrentLocationId"));
    }

    [Fact]
    public void LegacyTemporaryVehicleOperationIsNotPartOfTheModel()
    {
        using var context = CreateContext();

        Assert.DoesNotContain(context.Model.GetEntityTypes(), entity =>
            entity.ClrType.Name.Contains("TempVehicleOperation", StringComparison.Ordinal));
    }
}
