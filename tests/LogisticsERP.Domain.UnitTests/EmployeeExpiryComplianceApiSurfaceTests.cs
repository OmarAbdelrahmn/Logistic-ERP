using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class EmployeeExpiryComplianceApiSurfaceTests
{
    [Fact]
    public void DashboardUsesTheComplianceExpiriesRoute()
    {
        var route = Assert.Single(typeof(EmployeeExpiryComplianceController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/compliance/expiries", route.Template);
    }

    [Fact]
    public void EmployeeDetailUsesTheEmployeeComplianceExpiriesRoute()
    {
        var route = Assert.Single(typeof(EmployeeComplianceExpiriesController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/employees/{employeeId:guid}/compliance-expiries", route.Template);
    }

    [Theory]
    [InlineData(typeof(EmployeeExpiryComplianceController), nameof(EmployeeExpiryComplianceController.Get))]
    [InlineData(typeof(EmployeeComplianceExpiriesController), nameof(EmployeeComplianceExpiriesController.Get))]
    public void ExpiryEndpointsRequireEmployeeReadPermission(Type controllerType, string actionName)
    {
        var action = controllerType.GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(action);
        Assert.Contains(action!.GetCustomAttributes(), attribute => attribute is HttpGetAttribute);
        Assert.Contains(action.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(PermissionKeys.Workforce.EmployeesRead, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void DashboardContractIncludesPagingAndCalculatedStatus()
    {
        var pageProperties = typeof(EmployeeExpiryCompliancePageResponse).GetProperties().Select(property => property.Name).ToArray();
        var itemProperties = typeof(EmployeeExpiryComplianceItemResponse).GetProperties().Select(property => property.Name).ToArray();

        Assert.Equal(["Items", "Summary", "Page", "PageSize", "TotalCount", "CheckDate"], pageProperties);
        Assert.Contains("DueStatus", itemProperties);
        Assert.Contains("DaysRemaining", itemProperties);
        Assert.Contains("ReferenceMasked", itemProperties);
    }
}
