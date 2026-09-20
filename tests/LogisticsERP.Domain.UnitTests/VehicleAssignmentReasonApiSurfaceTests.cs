using System.Reflection;
using LogisticsERP.Application.Features.Fleet;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class VehicleAssignmentReasonApiSurfaceTests
{
    [Theory]
    [InlineData(typeof(TakeVehicleRequest))]
    [InlineData(typeof(ReturnVehicleRequest))]
    public void TakeAndReturnReasonIsNullable(Type requestType)
    {
        var reason = requestType.GetProperty("Reason");

        Assert.NotNull(reason);
        Assert.Equal(NullabilityState.Nullable, new NullabilityInfoContext().Create(reason!).ReadState);
    }
}
