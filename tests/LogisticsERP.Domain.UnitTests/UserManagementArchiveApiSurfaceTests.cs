using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class UserManagementArchiveApiSurfaceTests
{
    [Theory]
    [InlineData(nameof(UsersController.GetArchived), typeof(HttpGetAttribute), "archived", PermissionKeys.Security.UsersRead)]
    [InlineData(nameof(UsersController.Restore), typeof(HttpPatchAttribute), "{userId:guid}/restore", PermissionKeys.Security.UsersArchive)]
    public void ArchiveManagementEndpointsUseExpectedRouteAndPermission(
        string actionName,
        Type verbType,
        string routeTemplate,
        string permission)
    {
        var action = typeof(UsersController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);

        var verb = Assert.Single(action!.GetCustomAttributes<HttpMethodAttribute>(true));
        Assert.Equal(verbType, verb.GetType());
        Assert.Equal(routeTemplate, verb.Template);
        Assert.Contains(action.GetCustomAttributes<RequirePermissionAttribute>(true), attribute =>
            attribute.Policy?.EndsWith(permission, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ServiceContractExposesArchivedQueryAndRestore()
    {
        Assert.NotNull(typeof(IUserManagementService).GetMethod(nameof(IUserManagementService.GetArchivedUsersAsync)));
        var restore = typeof(IUserManagementService).GetMethod(nameof(IUserManagementService.RestoreUserAsync));
        Assert.NotNull(restore);
        Assert.Contains(restore!.GetParameters(), parameter => parameter.ParameterType == typeof(RestoreManagedUserRequest));
    }
}
