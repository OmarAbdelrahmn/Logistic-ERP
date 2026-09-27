using System.Reflection;
using LogisticsERP.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class VehicleStatusImportApiSurfaceTests
{
    [Fact]
    public void BothVehicleStatusImportRoutesAllowAnonymousCallers()
    {
        AssertAnonymousPostRoute(nameof(ImportController.ValidateVehicleStatuses), "vehicles/statuses/validate");
        AssertAnonymousPostRoute(nameof(ImportController.ImportVehicleStatuses), "vehicles/statuses");
    }

    private static void AssertAnonymousPostRoute(string actionName, string routeTemplate)
    {
        var action = typeof(ImportController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);
        Assert.Equal(routeTemplate, Assert.Single(action!.GetCustomAttributes<HttpPostAttribute>()).Template);
        Assert.NotNull(action.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Empty(action.GetCustomAttributes<AuthorizeAttribute>());
    }
}
