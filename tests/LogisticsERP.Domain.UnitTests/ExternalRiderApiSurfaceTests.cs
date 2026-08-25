using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class ExternalRiderApiSurfaceTests
{
    public static TheoryData<string, Type, string> ProtectedEndpoints => new()
    {
        { nameof(ExternalRidersController.GetAll), typeof(HttpGetAttribute), PermissionKeys.Workforce.RidersRead },
        { nameof(ExternalRidersController.Get), typeof(HttpGetAttribute), PermissionKeys.Workforce.RidersRead },
        { nameof(ExternalRidersController.Create), typeof(HttpPostAttribute), PermissionKeys.Workforce.EmployeesCreate },
        { nameof(ExternalRidersController.Update), typeof(HttpPutAttribute), PermissionKeys.Workforce.EmployeesUpdate }
    };

    [Fact]
    public void ControllerUsesDedicatedExternalRiderRoute()
    {
        var route = Assert.Single(typeof(ExternalRidersController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/external-riders", route.Template);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public void EndpointsUseExpectedVerbAndPermission(string actionName, Type verbType, string permission)
    {
        var action = typeof(ExternalRidersController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(action);
        Assert.Contains(action!.GetCustomAttributes(), attribute => attribute.GetType() == verbType);
        Assert.Contains(action.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void CreateContractContainsOnlyRequiredIdentityFields()
    {
        var properties = typeof(CreateExternalRiderRequest).GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(
            ["IqamaNo", "FullNameAr", "PrimaryPhone", "OperatingCityId", "OperationalWorkTypeId"],
            properties);
    }

    [Fact]
    public void UpdateContractAddsOnlyTheConcurrencyToken()
    {
        var properties = typeof(UpdateExternalRiderRequest).GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["IqamaNo", "FullNameAr", "RowVersion"], properties);
    }
}
