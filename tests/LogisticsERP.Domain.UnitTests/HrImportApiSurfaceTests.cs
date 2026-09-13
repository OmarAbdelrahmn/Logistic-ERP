using System.Reflection;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Authorization;
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

        AssertAnonymousPostRoute(
            nameof(ImportController.Validate),
            "employees-riders/validate");
        AssertAnonymousPostRoute(
            nameof(ImportController.Import),
            "employees-riders");
    }

    [Fact]
    public void ControllerExposesAnonymousPhoneNumberCheckerAndHandlerEndpoints()
    {
        AssertAnonymousPostRoute(
            nameof(ImportController.ValidatePhoneNumbers),
            "employees-riders/phone-numbers/validate");
        AssertAnonymousPostRoute(
            nameof(ImportController.UpdatePhoneNumbers),
            "employees-riders/phone-numbers");
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

    private static void AssertAnonymousPostRoute(string actionName, string template)
    {
        var action = typeof(ImportController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);
        var route = Assert.Single(action!.GetCustomAttributes<HttpPostAttribute>());
        Assert.Equal(template, route.Template);
        Assert.NotNull(action.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}
