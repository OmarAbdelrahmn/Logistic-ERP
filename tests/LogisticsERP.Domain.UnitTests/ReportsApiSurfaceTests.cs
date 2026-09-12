using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Reporting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class ReportsApiSurfaceTests
{
    [Theory]
    [InlineData(nameof(ReportsController.Dashboard), "dashboard")]
    [InlineData(nameof(ReportsController.HrDashboard), "hr/dashboard")]
    [InlineData(nameof(ReportsController.PeopleComplianceDashboard), "people-compliance/dashboard")]
    [InlineData(nameof(ReportsController.FleetDashboard), "fleet/dashboard")]
    [InlineData(nameof(ReportsController.OperationsDashboard), "operations/dashboard")]
    [InlineData(nameof(ReportsController.MaintenanceInventoryDashboard), "maintenance-inventory/dashboard")]
    public void DashboardEndpointsAreReadOnlyAndRequireReportsReadPermission(string actionName, string route)
    {
        var action = typeof(ReportsController).GetMethod(actionName);

        Assert.NotNull(action);
        var httpGet = Assert.IsType<HttpGetAttribute>(Assert.Single(action!.GetCustomAttributes(typeof(HttpMethodAttribute), true)));
        Assert.Equal(route, httpGet.Template);
        Assert.Contains(action.GetCustomAttributes(typeof(RequirePermissionAttribute), true).Cast<RequirePermissionAttribute>(),
            attribute => attribute.Policy?.EndsWith(PermissionKeys.Reporting.ReportsRead, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void HrDashboardContractIncludesSponsorAndPlatformCoverageBreakdowns()
    {
        var properties = typeof(HrDashboardReportResponse).GetProperties().Select(property => property.Name);

        Assert.Contains(nameof(HrDashboardReportResponse.Headcount), properties);
        Assert.Contains(nameof(HrDashboardReportResponse.Sponsors), properties);
        Assert.Contains(nameof(HrDashboardReportResponse.ActivePlatforms), properties);
        Assert.Contains(nameof(PlatformCoverageResponse.ActiveRidersWithoutAccount),
            typeof(PlatformCoverageResponse).GetProperties().Select(property => property.Name));
        Assert.Contains(nameof(SystemDashboardReportResponse.Fleet),
            typeof(SystemDashboardReportResponse).GetProperties().Select(property => property.Name));
        Assert.Contains(nameof(SystemDashboardReportResponse.PeopleCompliance),
            typeof(SystemDashboardReportResponse).GetProperties().Select(property => property.Name));
    }
}
