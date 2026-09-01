using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class PayrollEmployeeApiSurfaceTests
{
    public static TheoryData<string, Type, string?, string> Endpoints => new()
    {
        { nameof(PayrollEmployeesController.GetAll), typeof(HttpGetAttribute), null, PermissionKeys.Workforce.EmployeesRead },
        { nameof(PayrollEmployeesController.Get), typeof(HttpGetAttribute), "{id:guid}", PermissionKeys.Workforce.EmployeesRead },
        { nameof(PayrollEmployeesController.Create), typeof(HttpPostAttribute), null, PermissionKeys.Workforce.EmployeesCreate },
        { nameof(PayrollEmployeesController.Update), typeof(HttpPutAttribute), "{id:guid}", PermissionKeys.Workforce.EmployeesUpdate },
        { nameof(PayrollEmployeesController.Delete), typeof(HttpDeleteAttribute), "{id:guid}", PermissionKeys.Workforce.EmployeesArchive }
    };

    [Fact]
    public void ModelContainsAllRequestedBusinessFields()
    {
        var expected = new[]
        {
            nameof(PayrollEmployee.Number),
            nameof(PayrollEmployee.SponsorId),
            nameof(PayrollEmployee.Name),
            nameof(PayrollEmployee.NationalId),
            nameof(PayrollEmployee.Country),
            nameof(PayrollEmployee.JoiningDate),
            nameof(PayrollEmployee.PersonalIban),
            nameof(PayrollEmployee.Salary),
            nameof(PayrollEmployee.Status)
        };

        var actual = typeof(PayrollEmployee).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(expected, property => Assert.Contains(property, actual));
    }

    [Fact]
    public void ControllerUsesPayrollEmployeeRoute()
    {
        var route = Assert.Single(typeof(PayrollEmployeesController).GetCustomAttributes(typeof(RouteAttribute), true));
        Assert.Equal("api/payroll-employees", Assert.IsType<RouteAttribute>(route).Template);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void EndpointsUseExpectedVerbRouteAndPermission(
        string methodName,
        Type verbType,
        string? routeTemplate,
        string permission)
    {
        var method = typeof(PayrollEmployeesController).GetMethod(methodName);
        Assert.NotNull(method);
        var verb = Assert.Single(method!.GetCustomAttributes<HttpMethodAttribute>());
        Assert.Equal(verbType, verb.GetType());
        Assert.Equal(routeTemplate, verb.Template);
        Assert.Contains(method.GetCustomAttributes<RequirePermissionAttribute>(), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void UpdateAndDeleteExposeOptimisticConcurrency()
    {
        Assert.NotNull(typeof(PayrollEmployeeResponse).GetProperty(nameof(PayrollEmployeeResponse.RowVersion)));
        Assert.NotNull(typeof(UpdatePayrollEmployeeRequest).GetProperty(nameof(UpdatePayrollEmployeeRequest.RowVersion)));

        var rowVersion = typeof(PayrollEmployeesController).GetMethod(nameof(PayrollEmployeesController.Delete))!
            .GetParameters()
            .Single(parameter => parameter.Name == "rowVersion");
        Assert.NotNull(rowVersion.GetCustomAttribute<FromQueryAttribute>());
    }

    [Fact]
    public void RequestsAndResponsesRequireSponsorRelationship()
    {
        Assert.Equal(typeof(Guid), typeof(CreatePayrollEmployeeRequest)
            .GetProperty(nameof(CreatePayrollEmployeeRequest.SponsorId))!.PropertyType);
        Assert.Equal(typeof(Guid), typeof(UpdatePayrollEmployeeRequest)
            .GetProperty(nameof(UpdatePayrollEmployeeRequest.SponsorId))!.PropertyType);
        Assert.Equal(typeof(Guid), typeof(PayrollEmployeeResponse)
            .GetProperty(nameof(PayrollEmployeeResponse.SponsorId))!.PropertyType);
        Assert.Equal(typeof(PayrollEmployeeSponsorResponse), typeof(PayrollEmployeeResponse)
            .GetProperty(nameof(PayrollEmployeeResponse.Sponsor))!.PropertyType);
    }

    [Fact]
    public void DatabaseModelRequiresSponsorWithRestrictedDelete()
    {
        using var dbContext = new ApplicationDbContextFactory().CreateDbContext([]);
        var entity = dbContext.Model.FindEntityType(typeof(PayrollEmployee));
        Assert.NotNull(entity);

        var foreignKey = Assert.Single(entity!.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(Sponsor));
        Assert.False(foreignKey.IsRequiredDependent);
        Assert.True(foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }
}
