using LogisticsERP.Domain.Entities.Fleet;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class FleetEntityIdentifierTests
{
    [Fact]
    public void NewFleetEntitiesUseVersionSevenIdentifiers()
    {
        var entities = new object[]
        {
            new Vehicle(),
            new RiderVehicleAssignment(),
            new VehicleOperationalStatusPeriod(),
            new VehicleInsurancePolicy(),
            new VehicleAttachment(),
            new VehicleIssue(),
            new VehicleAccident()
        };

        foreach (var entity in entities)
        {
            var id = (Guid)(entity.GetType().GetProperty(nameof(Vehicle.Id))?.GetValue(entity)
                ?? throw new InvalidOperationException("Fleet entity did not expose an identifier."));
            Assert.Equal(7, id.Version);
        }
    }
}
