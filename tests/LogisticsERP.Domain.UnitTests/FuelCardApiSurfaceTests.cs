using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fuel;
using LogisticsERP.Domain.Entities.Fuel;
using LogisticsERP.Domain.Fuel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class FuelCardApiSurfaceTests
{
    public static TheoryData<string, Type, string?, string> Endpoints => new()
    {
        { nameof(FuelCardsController.GetCards), typeof(HttpGetAttribute), null, PermissionKeys.Fuel.Read },
        { nameof(FuelCardsController.GetCard), typeof(HttpGetAttribute), "{id:guid}", PermissionKeys.Fuel.Read },
        { nameof(FuelCardsController.CreateCard), typeof(HttpPostAttribute), null, PermissionKeys.Fuel.Manage },
        { nameof(FuelCardsController.GetAssignments), typeof(HttpGetAttribute), "{id:guid}/assignments", PermissionKeys.Fuel.Read },
        { nameof(FuelCardsController.AssignRider), typeof(HttpPostAttribute), "{id:guid}/assignments", PermissionKeys.Fuel.Manage },
        { nameof(FuelCardsController.StopRider), typeof(HttpPostAttribute), "{id:guid}/stop-rider", PermissionKeys.Fuel.Manage },
        { nameof(FuelCardsController.GetMonthlyUsage), typeof(HttpGetAttribute), "monthly-usage", PermissionKeys.Fuel.Read },
        { nameof(FuelCardsController.Import), typeof(HttpPostAttribute), "imports", PermissionKeys.Fuel.Import },
        { nameof(FuelCardsController.GetImports), typeof(HttpGetAttribute), "imports", PermissionKeys.Fuel.Read }
    };

    [Fact]
    public void ControllerUsesFuelCardRoute()
    {
        var route = Assert.Single(typeof(FuelCardsController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/fuel-cards", route.Template);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void EndpointsUseExpectedVerbRouteAndPermission(string methodName, Type verbType, string? route, string permission)
    {
        var method = typeof(FuelCardsController).GetMethod(methodName);
        Assert.NotNull(method);
        var verb = Assert.Single(method!.GetCustomAttributes<HttpMethodAttribute>());
        Assert.Equal(verbType, verb.GetType());
        Assert.Equal(route, verb.Template);
        Assert.Contains(method.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void DomainKeepsFuelPlateTextIndependentFromRealVehicles()
    {
        Assert.NotNull(typeof(FuelCard).GetProperty(nameof(FuelCard.PlateNumberText)));
        Assert.Null(typeof(FuelCard).GetProperty("VehicleId"));
        Assert.Null(typeof(FuelCardMonthlyUsage).GetProperty("VehicleId"));
        Assert.Equal(typeof(Guid), typeof(FuelCardRiderAssignment).GetProperty(nameof(FuelCardRiderAssignment.RiderProfileId))!.PropertyType);
        Assert.Equal(typeof(Guid), typeof(FuelCardRiderAssignment).GetProperty(nameof(FuelCardRiderAssignment.EmployeeId))!.PropertyType);
    }

    [Fact]
    public void MonthlyUsageCarriesOneRiderAndEmployeeAndImportIsMultipart()
    {
        Assert.Equal(typeof(Guid), typeof(FuelCardMonthlyUsage).GetProperty(nameof(FuelCardMonthlyUsage.RiderProfileId))!.PropertyType);
        Assert.Equal(typeof(Guid), typeof(FuelCardMonthlyUsage).GetProperty(nameof(FuelCardMonthlyUsage.EmployeeId))!.PropertyType);
        Assert.NotNull(typeof(FuelCardMonthlyUsage).GetProperty(nameof(FuelCardMonthlyUsage.ReportMonth)));
        Assert.Contains(PermissionKeys.Fuel.Read, (IEnumerable<string>)PermissionKeys.All);
        Assert.Contains(PermissionKeys.Fuel.Manage, (IEnumerable<string>)PermissionKeys.All);
        Assert.Contains(PermissionKeys.Fuel.Import, (IEnumerable<string>)PermissionKeys.All);

        var method = typeof(FuelCardsController).GetMethod(nameof(FuelCardsController.Import))!;
        Assert.Single(method.GetCustomAttributes<ConsumesAttribute>());
        Assert.NotNull(Assert.Single(method.GetParameters(), parameter => parameter.ParameterType == typeof(FuelImportForm))
            .GetCustomAttribute<FromFormAttribute>());
    }

    [Fact]
    public void ACardCannotUseTwoDifferentRidersInTheSameMonth()
    {
        var rider = Guid.NewGuid();
        Assert.True(FuelCardRules.CanUseRiderForMonth(rider, [rider, rider]));
        Assert.False(FuelCardRules.CanUseRiderForMonth(rider, [rider, Guid.NewGuid()]));
        Assert.True(FuelCardRules.PeriodTouchesMonth(
            new DateOnly(2026, 8, 15),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 1)));
        Assert.False(FuelCardRules.PeriodTouchesMonth(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 9, 1)));
    }
}
