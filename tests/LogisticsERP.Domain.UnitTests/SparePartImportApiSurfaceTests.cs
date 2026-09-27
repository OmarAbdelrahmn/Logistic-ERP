using System.Reflection;
using LogisticsERP.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class SparePartImportApiSurfaceTests
{
    [Fact]
    public void ImportControllerOwnsAnonymousValidationAndImportRoutes()
    {
        AssertAnonymousPostRoute(nameof(ImportController.ValidateSpareParts),
            "/api/maintenance-inventory/items/import/validate");
        AssertAnonymousPostRoute(nameof(ImportController.ImportSpareParts),
            "/api/maintenance-inventory/items/import");
        Assert.Null(typeof(MaintenanceInventoryController).GetMethod("ImportSpareParts"));
    }

    private static void AssertAnonymousPostRoute(string actionName, string template)
    {
        var action = typeof(ImportController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);
        var route = Assert.Single(action!.GetCustomAttributes<HttpPostAttribute>());
        Assert.Equal(template, route.Template);
        Assert.NotNull(action.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Null(action.GetCustomAttribute<AuthorizeAttribute>());
    }
}
