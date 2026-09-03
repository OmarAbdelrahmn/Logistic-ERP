using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class VehicleDailyDistanceApiSurfaceTests
{
    public static TheoryData<string, Type, string?, string> Endpoints => new()
    {
        { nameof(VehicleDailyDistancesController.GetDaily), typeof(HttpGetAttribute), null, PermissionKeys.Fleet.DailyDistancesRead },
        { nameof(VehicleDailyDistancesController.UpsertManual), typeof(HttpPutAttribute), "{vehicleId:guid}/{workDate}", PermissionKeys.Fleet.DailyDistancesManage },
        { nameof(VehicleDailyDistancesController.ImportGps), typeof(HttpPostAttribute), "gps-import", PermissionKeys.Fleet.DailyDistancesImport },
        { nameof(VehicleDailyDistancesController.GetImports), typeof(HttpGetAttribute), "gps-imports", PermissionKeys.Fleet.DailyDistancesRead }
    };

    [Fact]
    public void ControllerUsesDailyDistanceRoute()
    {
        var route = Assert.Single(typeof(VehicleDailyDistancesController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/vehicle-daily-distances", route.Template);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void EndpointsUseExpectedVerbRouteAndPermission(string methodName, Type verbType, string? route, string permission)
    {
        var method = typeof(VehicleDailyDistancesController).GetMethod(methodName);
        Assert.NotNull(method);
        var verb = Assert.Single(method!.GetCustomAttributes<HttpMethodAttribute>());
        Assert.Equal(verbType, verb.GetType());
        Assert.Equal(route, verb.Template);
        Assert.Contains(method.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void PermissionsAndContractsExposeGpsAndManualWorkflow()
    {
        Assert.Contains(PermissionKeys.Fleet.DailyDistancesRead, (IEnumerable<string>)PermissionKeys.All);
        Assert.Contains(PermissionKeys.Fleet.DailyDistancesManage, (IEnumerable<string>)PermissionKeys.All);
        Assert.Contains(PermissionKeys.Fleet.DailyDistancesImport, (IEnumerable<string>)PermissionKeys.All);
        Assert.NotNull(typeof(VehicleDailyDistanceResponse).GetProperty(nameof(VehicleDailyDistanceResponse.GpsDistanceKm)));
        Assert.NotNull(typeof(VehicleDailyDistanceResponse).GetProperty(nameof(VehicleDailyDistanceResponse.ManualOdometerReading)));
        Assert.NotNull(typeof(VehicleDailyDistanceResponse).GetProperty(nameof(VehicleDailyDistanceResponse.ManualDistanceKm)));
        Assert.NotNull(typeof(VehicleDailyDistanceResponse).GetProperty(nameof(VehicleDailyDistanceResponse.AppliedSource)));
        Assert.NotNull(typeof(VehicleDailyDistanceResponse).GetProperty(nameof(VehicleDailyDistanceResponse.VehicleTrackedDistanceKm)));
        Assert.Equal(typeof(decimal), typeof(Vehicle).GetProperty(nameof(Vehicle.TrackedDistanceKm))!.PropertyType);
    }

    [Fact]
    public void GpsWinsAndManualIsTheFallbackWithoutDoubleCounting()
    {
        var manualOnly = VehicleDailyDistanceRules.SelectAppliedDistance(null, 164m);
        var gpsAvailable = VehicleDailyDistanceRules.SelectAppliedDistance(150m, 164m);

        Assert.Equal((164m, VehicleDailyDistanceSource.Manual), manualOnly);
        Assert.Equal((150m, VehicleDailyDistanceSource.Gps), gpsAvailable);
        Assert.Equal(-14m, VehicleDailyDistanceRules.CalculateTotalAdjustment(manualOnly.Item1, gpsAvailable.Item1));
        Assert.Equal(164m, VehicleDailyDistanceRules.CalculateManualDistance(10_000, 10_164));
    }
}
