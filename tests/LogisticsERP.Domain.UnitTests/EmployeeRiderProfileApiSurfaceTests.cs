using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class EmployeeRiderProfileApiSurfaceTests
{
    [Fact]
    public void ProfileProvisioningRequiresEmployeeUpdatePermission()
    {
        var action = typeof(EmployeesController).GetMethod(nameof(EmployeesController.EnsureRiderProfile))!;
        Assert.Equal("{employeeId:guid}/rider-profile", Assert.Single(action.GetCustomAttributes<HttpPutAttribute>()).Template);
        var permission = Assert.Single(action.GetCustomAttributes<RequirePermissionAttribute>());
        Assert.Equal(AuthenticationPolicies.PermissionPrefix + PermissionKeys.Workforce.EmployeesUpdate, permission.Policy);
        Assert.DoesNotContain(action.GetParameters(), parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null);
    }
}
