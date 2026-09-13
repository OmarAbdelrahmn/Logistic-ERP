using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class HrImportApiSurfaceTests
{
    [Fact]
    public void ControllerExposesSeparateValidationAndImportEndpoints()
    {
        var route = Assert.Single(typeof(ImportController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/import", route.Template);

        AssertPostRouteAndPermission(
            nameof(ImportController.Validate),
            "employees-riders/validate",
            PermissionKeys.Workforce.EmployeesRead);
        AssertPostRouteAndPermission(
            nameof(ImportController.Import),
            "employees-riders",
            PermissionKeys.Workforce.EmployeesCreate,
            PermissionKeys.Workforce.EmployeesUpdate);
    }

    [Fact]
    public void ResponseReportsValidationCommitAndExpiryOutcomes()
    {
        var properties = typeof(HrExcelImportResponse).GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Contains(nameof(HrExcelImportResponse.CanImport), properties);
        Assert.Contains(nameof(HrExcelImportResponse.Imported), properties);
        Assert.Contains(nameof(HrExcelImportResponse.CreatedResidencyDocuments), properties);
        Assert.Contains(nameof(HrExcelImportResponse.CreatedDriverLicenses), properties);
        Assert.Contains(nameof(HrExcelImportResponse.UpdatedDriverLicenses), properties);
        Assert.Contains(nameof(HrExcelImportResponse.DefaultedExpiryDates), properties);
    }

    private static void AssertPostRouteAndPermission(string actionName, string template, params string[] permissions)
    {
        var action = typeof(ImportController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);
        var route = Assert.Single(action!.GetCustomAttributes<HttpPostAttribute>());
        Assert.Equal(template, route.Template);
        var actualPermissions = action.GetCustomAttributes<RequirePermissionAttribute>()
            .Select(attribute => attribute.Policy)
            .ToArray();
        Assert.All(permissions, permission =>
            Assert.Contains(actualPermissions, policy => policy?.EndsWith(permission, StringComparison.Ordinal) == true));
    }
}
