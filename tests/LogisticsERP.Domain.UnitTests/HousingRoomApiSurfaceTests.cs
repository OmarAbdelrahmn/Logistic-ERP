using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Housing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class HousingRoomApiSurfaceTests
{
    public static TheoryData<Type, string, Type, string, string> Endpoints => new()
    {
        { typeof(HousingController), nameof(HousingController.GetRooms), typeof(HttpGetAttribute), "{id:guid}/rooms", PermissionKeys.Operations.HousingRead },
        { typeof(HousingController), nameof(HousingController.CreateRoom), typeof(HttpPostAttribute), "{id:guid}/rooms", PermissionKeys.Operations.HousingManage },
        { typeof(RoomsController), nameof(RoomsController.Get), typeof(HttpGetAttribute), "{id:guid}", PermissionKeys.Operations.HousingRead },
        { typeof(RoomsController), nameof(RoomsController.Update), typeof(HttpPutAttribute), "{id:guid}", PermissionKeys.Operations.HousingManage },
        { typeof(RoomsController), nameof(RoomsController.Archive), typeof(HttpDeleteAttribute), "{id:guid}", PermissionKeys.Operations.HousingManage },
        { typeof(RoomsController), nameof(RoomsController.AssignEmployee), typeof(HttpPostAttribute), "{id:guid}/occupants/employees", PermissionKeys.Operations.HousingManage },
        { typeof(RoomsController), nameof(RoomsController.AssignRider), typeof(HttpPostAttribute), "{id:guid}/occupants/riders", PermissionKeys.Operations.HousingManage },
        { typeof(RoomsController), nameof(RoomsController.MoveOccupant), typeof(HttpPostAttribute), "occupants/{occupancyPeriodId:guid}/move", PermissionKeys.Operations.HousingManage },
        { typeof(RoomsController), nameof(RoomsController.RemoveOccupant), typeof(HttpPostAttribute), "occupants/{occupancyPeriodId:guid}/remove", PermissionKeys.Operations.HousingManage }
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void EndpointsUseExpectedVerbRouteAndPermission(
        Type controller,
        string methodName,
        Type verbType,
        string route,
        string permission)
    {
        var method = controller.GetMethod(methodName);
        Assert.NotNull(method);
        var verb = Assert.Single(method!.GetCustomAttributes<HttpMethodAttribute>());
        Assert.Equal(verbType, verb.GetType());
        Assert.Equal(route, verb.Template);
        Assert.Contains(method.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ResidenceUsesRoomAsItsOnlyLocationRelationship()
    {
        Assert.Equal(typeof(Guid), typeof(HousingResidencePeriod).GetProperty(nameof(HousingResidencePeriod.RoomId))!.PropertyType);
        Assert.Null(typeof(HousingResidencePeriod).GetProperty("HousingId"));
        Assert.NotNull(typeof(HousingRoom).GetProperty(nameof(HousingRoom.HousingId)));
        Assert.Null(typeof(Housing).GetProperty("TotalCapacity"));
    }

    [Fact]
    public void DedicatedRiderAndEmployeeRequestsUseTheirOwnIdentifiers()
    {
        Assert.NotNull(typeof(AssignRoomEmployeeRequest).GetProperty(nameof(AssignRoomEmployeeRequest.EmployeeId)));
        Assert.NotNull(typeof(AssignRoomRiderRequest).GetProperty(nameof(AssignRoomRiderRequest.RiderProfileId)));
        Assert.NotNull(typeof(AssignHousingResidentRequest).GetProperty(nameof(AssignHousingResidentRequest.RoomId)));
    }
}
