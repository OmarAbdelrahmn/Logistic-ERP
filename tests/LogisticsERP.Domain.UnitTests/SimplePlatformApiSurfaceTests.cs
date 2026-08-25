using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class SimplePlatformApiSurfaceTests
{
    public static TheoryData<Type, string, Type, string?, string> Endpoints => new()
    {
        { typeof(PlatformsController), nameof(PlatformsController.GetAll), typeof(HttpGetAttribute), null, PermissionKeys.Operations.PlatformAccountsRead },
        { typeof(PlatformsController), nameof(PlatformsController.Create), typeof(HttpPostAttribute), null, PermissionKeys.Operations.PlatformAccountsManage },
        { typeof(PlatformsController), nameof(PlatformsController.Update), typeof(HttpPutAttribute), "{id:guid}", PermissionKeys.Operations.PlatformAccountsManage },
        { typeof(PlatformAccountsController), nameof(PlatformAccountsController.GetAll), typeof(HttpGetAttribute), null, PermissionKeys.Operations.PlatformAccountsRead },
        { typeof(PlatformAccountsController), nameof(PlatformAccountsController.Get), typeof(HttpGetAttribute), "{id:guid}", PermissionKeys.Operations.PlatformAccountsRead },
        { typeof(PlatformAccountsController), nameof(PlatformAccountsController.Create), typeof(HttpPostAttribute), null, PermissionKeys.Operations.PlatformAccountsManage },
        { typeof(PlatformAccountsController), nameof(PlatformAccountsController.Update), typeof(HttpPutAttribute), "{id:guid}", PermissionKeys.Operations.PlatformAccountsManage },
        { typeof(PlatformAccountsController), nameof(PlatformAccountsController.Assign), typeof(HttpPostAttribute), "{id:guid}/assign", PermissionKeys.Operations.PlatformAssignmentsManage },
        { typeof(PlatformAccountsController), nameof(PlatformAccountsController.Release), typeof(HttpPostAttribute), "{id:guid}/release", PermissionKeys.Operations.PlatformAssignmentsManage },
        { typeof(PlatformAccountsController), nameof(PlatformAccountsController.AssignmentHistory), typeof(HttpGetAttribute), "{id:guid}/assignment-history", PermissionKeys.Operations.PlatformAssignmentsRead },
        { typeof(PlatformAccountsController), nameof(PlatformAccountsController.CredentialHistory), typeof(HttpGetAttribute), "{id:guid}/credential-history", PermissionKeys.Operations.PlatformCredentialsRead },
        { typeof(PlatformAccountsController), nameof(PlatformAccountsController.RotateCredential), typeof(HttpPostAttribute), "{id:guid}/rotate-credential", PermissionKeys.Operations.PlatformCredentialsRotate },
        { typeof(RiderPlatformHistoryController), nameof(RiderPlatformHistoryController.Get), typeof(HttpGetAttribute), null, PermissionKeys.Operations.PlatformAssignmentsRead }
    };

    [Fact]
    public void SimplifiedSurfaceContainsExactlyThirteenActions()
    {
        Type[] controllers =
        [
            typeof(PlatformsController),
            typeof(PlatformAccountsController),
            typeof(RiderPlatformHistoryController)
        ];

        var actions = controllers.SelectMany(controller => controller
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(true).Any()));

        Assert.Equal(13, actions.Count());
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public void EndpointUsesExpectedVerbRouteAndPermission(
        Type controllerType,
        string actionName,
        Type verbAttributeType,
        string? routeTemplate,
        string permission)
    {
        var action = controllerType.GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);

        var verb = Assert.Single(action!.GetCustomAttributes<HttpMethodAttribute>(true));
        Assert.Equal(verbAttributeType, verb.GetType());
        Assert.Equal(routeTemplate, verb.Template);

        var permissions = action.GetCustomAttributes<RequirePermissionAttribute>(true);
        Assert.Contains(permissions, attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void DatabaseModelEnforcesOwnerAndActualRiderUniqueness()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer()
            .Options;
        using var dbContext = new ApplicationDbContext(options);

        var account = dbContext.Model.FindEntityType(typeof(PlatformRiderAccount));
        Assert.NotNull(account);
        var ownerIndex = Assert.Single(account!.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(PlatformRiderAccount.RegisteredEmployeeId), nameof(PlatformRiderAccount.ClientPlatformId)]));
        Assert.True(ownerIndex.IsUnique);
        Assert.Contains("[IsDeleted] = 0", ownerIndex.GetFilter(), StringComparison.Ordinal);

        var assignment = dbContext.Model.FindEntityType(typeof(RiderClientAssignment));
        Assert.NotNull(assignment);
        var activeAccountIndex = Assert.Single(assignment!.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(RiderClientAssignment.PlatformRiderAccountId)]));
        var activeRiderIndex = Assert.Single(assignment.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(RiderClientAssignment.RiderProfileId)]));

        Assert.True(activeAccountIndex.IsUnique);
        Assert.True(activeRiderIndex.IsUnique);
        Assert.Contains("[EffectiveTo] IS NULL", activeAccountIndex.GetFilter(), StringComparison.Ordinal);
        Assert.Contains("[EffectiveTo] IS NULL", activeRiderIndex.GetFilter(), StringComparison.Ordinal);
    }

    [Fact]
    public void DatabaseModelIncludesPlatformHistoryIndexes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=LogisticsERP-Model-Test;Trusted_Connection=True")
            .Options;
        using var dbContext = new ApplicationDbContext(options);

        var account = dbContext.Model.FindEntityType(typeof(PlatformRiderAccount));
        Assert.NotNull(account);
        var accountFilterIndex = Assert.Single(account!.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                [
                    nameof(PlatformRiderAccount.OperatingCityId),
                    nameof(PlatformRiderAccount.Status),
                    nameof(PlatformRiderAccount.ClientPlatformId)
                ]));
        Assert.Equal("[IsDeleted] = 0", accountFilterIndex.GetFilter());

        var assignment = dbContext.Model.FindEntityType(typeof(RiderClientAssignment));
        Assert.NotNull(assignment);
        Assert.Single(assignment!.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(RiderClientAssignment.PlatformRiderAccountId), nameof(RiderClientAssignment.EffectiveFrom)]));
        Assert.Single(assignment.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(RiderClientAssignment.RiderProfileId), nameof(RiderClientAssignment.EffectiveFrom)]));
    }
}
