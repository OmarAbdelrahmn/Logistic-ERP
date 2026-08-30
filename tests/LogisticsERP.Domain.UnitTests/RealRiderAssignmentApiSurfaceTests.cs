using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Domain.Entities.Fleet;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class RealRiderAssignmentApiSurfaceTests
{
    [Fact]
    public void AssignmentAndTakeRequestExposeRealRiderSelection()
    {
        Assert.Equal(typeof(bool), typeof(RiderVehicleAssignment).GetProperty(nameof(RiderVehicleAssignment.IsRealRider))!.PropertyType);
        Assert.Equal(typeof(bool), typeof(TakeVehicleRequest).GetProperty(nameof(TakeVehicleRequest.IsRealRider))!.PropertyType);
        Assert.Equal(typeof(RealRiderRequest), Nullable.GetUnderlyingType(typeof(TakeVehicleRequest).GetProperty(nameof(TakeVehicleRequest.RealRider))!.PropertyType)
            ?? typeof(TakeVehicleRequest).GetProperty(nameof(TakeVehicleRequest.RealRider))!.PropertyType);
    }

    [Fact]
    public void RealRiderDetailsContainIdentityAndRelationship()
    {
        Assert.Equal(
            ["Name", "IqamaNo", "RelationshipToAssignedRider"],
            typeof(RealRiderRequest).GetProperties().Select(property => property.Name));
        Assert.Equal(typeof(Guid), typeof(RealRider).GetProperty(nameof(RealRider.RiderVehicleAssignmentId))!.PropertyType);
        Assert.NotNull(typeof(RiderVehicleAssignmentResponse).GetProperty(nameof(RiderVehicleAssignmentResponse.RealRider)));
    }

    [Fact]
    public void ListEndpointReturnsVehicleAssignmentResponses()
    {
        var method = typeof(VehicleAssignmentsController).GetMethod(nameof(VehicleAssignmentsController.Get));

        Assert.NotNull(method);
        Assert.Single(method!.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true));
    }
}
