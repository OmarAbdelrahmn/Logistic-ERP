using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class VehiclePlatformAccountAssignmentApiSurfaceTests
{
    public static TheoryData<string, Type, string?, string> Endpoints => new()
    {
        { nameof(VehiclePlatformAccountAssignmentsController.GetAll), typeof(HttpGetAttribute), null, PermissionKeys.Fleet.AssignmentsRead },
        { nameof(VehiclePlatformAccountAssignmentsController.GetProblems), typeof(HttpGetAttribute), "problems", PermissionKeys.Fleet.AssignmentsRead },
        { nameof(VehiclePlatformAccountAssignmentsController.Get), typeof(HttpGetAttribute), "{id:guid}", PermissionKeys.Fleet.AssignmentsRead },
        { nameof(VehiclePlatformAccountAssignmentsController.Approve), typeof(HttpPostAttribute), null, PermissionKeys.Fleet.AssignmentsManage },
        { nameof(VehiclePlatformAccountAssignmentsController.Close), typeof(HttpPostAttribute), "{id:guid}/close", PermissionKeys.Fleet.AssignmentsManage },
        { nameof(VehiclePlatformAccountAssignmentsController.GetSwitches), typeof(HttpGetAttribute), "switches", PermissionKeys.Fleet.AssignmentsRead },
        { nameof(VehiclePlatformAccountAssignmentsController.GetSwitch), typeof(HttpGetAttribute), "switches/{switchId:guid}", PermissionKeys.Fleet.AssignmentsRead },
        { nameof(VehiclePlatformAccountAssignmentsController.Switch), typeof(HttpPostAttribute), "{id:guid}/switch", PermissionKeys.Fleet.AssignmentsManage },
        { nameof(VehiclePlatformAccountAssignmentsController.AcceptSwitch), typeof(HttpPostAttribute), "switches/{switchId:guid}/accept", PermissionKeys.Fleet.AssignmentsManage }
    };

    [Fact]
    public void ControllerUsesIndependentVehiclePlatformRoute()
    {
        var route = Assert.Single(typeof(VehiclePlatformAccountAssignmentsController)
            .GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/vehicle-platform-account-assignments", route.Template);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void EndpointsUseExpectedVerbRouteAndPermission(
        string methodName,
        Type verbType,
        string? routeTemplate,
        string permission)
    {
        var method = typeof(VehiclePlatformAccountAssignmentsController).GetMethod(methodName);
        Assert.NotNull(method);
        var verb = Assert.Single(method!.GetCustomAttributes<HttpMethodAttribute>());
        Assert.Equal(verbType, verb.GetType());
        Assert.Equal(routeTemplate, verb.Template);
        Assert.Contains(method.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ResponseExposesApprovalAndStructuredProblems()
    {
        Assert.NotNull(typeof(VehiclePlatformAccountAssignmentResponse)
            .GetProperty(nameof(VehiclePlatformAccountAssignmentResponse.ApprovalStatus)));
        Assert.NotNull(typeof(VehiclePlatformAccountAssignmentResponse)
            .GetProperty(nameof(VehiclePlatformAccountAssignmentResponse.VehicleRegistrationNumber)));
        Assert.NotNull(typeof(VehiclePlatformAccountAssignmentResponse)
            .GetProperty(nameof(VehiclePlatformAccountAssignmentResponse.VehiclePlateNumberAr)));
        Assert.NotNull(typeof(VehiclePlatformAccountAssignmentResponse)
            .GetProperty(nameof(VehiclePlatformAccountAssignmentResponse.Problems)));
        Assert.NotNull(typeof(VehiclePlatformAssignmentProblemResponse)
            .GetProperty(nameof(VehiclePlatformAssignmentProblemResponse.Code)));
    }

    [Fact]
    public void SwitchResponseExposesPendingWorkflowAndResultingAssignment()
    {
        Assert.NotNull(typeof(VehiclePlatformAccountSwitchResponse)
            .GetProperty(nameof(VehiclePlatformAccountSwitchResponse.Mode)));
        Assert.NotNull(typeof(VehiclePlatformAccountSwitchResponse)
            .GetProperty(nameof(VehiclePlatformAccountSwitchResponse.Status)));
        Assert.NotNull(typeof(VehiclePlatformAccountSwitchResponse)
            .GetProperty(nameof(VehiclePlatformAccountSwitchResponse.NewAssignmentId)));
        Assert.NotNull(typeof(VehiclePlatformAccountSwitchResponse)
            .GetProperty(nameof(VehiclePlatformAccountSwitchResponse.SourceVehicleRegistrationNumber)));
        Assert.NotNull(typeof(VehiclePlatformAccountSwitchResponse)
            .GetProperty(nameof(VehiclePlatformAccountSwitchResponse.TargetVehiclePlateNumberAr)));
        Assert.NotNull(typeof(VehiclePlatformAccountSwitchResponse)
            .GetProperty(nameof(VehiclePlatformAccountSwitchResponse.RowVersion)));
    }

    [Fact]
    public void EntityHasNoRiderRelationship()
    {
        Assert.Null(typeof(VehiclePlatformAccountAssignment).GetProperty("RiderProfileId"));
        Assert.Null(typeof(VehiclePlatformAccountAssignment).GetProperty("RiderVehicleAssignmentId"));
    }
}
