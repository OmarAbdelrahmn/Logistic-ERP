using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class ControllerAuthorizationAuditTests
{
    [Fact]
    public void EveryControllerActionDeclaresAuthorizationIntent()
    {
        var unprotectedActions = GetControllerActions()
            .Where(action => !HasAuthorizationMetadata(action.Controller, action.Action))
            .Select(action => $"{action.Controller.Name}.{action.Action.Name}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(unprotectedActions);
    }

    [Fact]
    public void OnlyLoginAndRefreshAreAnonymous()
    {
        var anonymousActions = GetControllerActions()
            .Where(action => action.Action.GetCustomAttribute<AllowAnonymousAttribute>(true) is not null
                || action.Controller.GetCustomAttribute<AllowAnonymousAttribute>(true) is not null)
            .Select(action => $"{action.Controller.Name}.{action.Action.Name}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["AuthController.Login", "AuthController.Refresh"],
            anonymousActions);
    }

    [Fact]
    public void ControllersDoNotRestrictAccessByRoleName()
    {
        var roleRestrictedEndpoints = GetControllerActions()
            .SelectMany(action => action.Action.GetCustomAttributes<AuthorizeAttribute>(true)
                .Concat(action.Controller.GetCustomAttributes<AuthorizeAttribute>(true))
                .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Roles))
                .Select(_ => $"{action.Controller.Name}.{action.Action.Name}"))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(roleRestrictedEndpoints);
    }

    [Fact]
    public void EveryControllerPermissionUsesTheRegisteredCatalog()
    {
        var unknownPolicies = GetControllerActions()
            .SelectMany(action => action.Action.GetCustomAttributes<RequirePermissionAttribute>(true)
                .Concat(action.Controller.GetCustomAttributes<RequirePermissionAttribute>(true)))
            .Select(attribute => attribute.Policy)
            .Where(policy => policy is null
                || !policy.StartsWith(AuthenticationPolicies.PermissionPrefix, StringComparison.Ordinal)
                || !PermissionKeys.All.Contains(policy[AuthenticationPolicies.PermissionPrefix.Length..]))
            .ToArray();

        Assert.Empty(unknownPolicies);
    }

    private static bool HasAuthorizationMetadata(Type controller, MethodInfo action) =>
        action.GetCustomAttributes(true).Any(attribute => attribute is IAuthorizeData or IAllowAnonymous)
        || controller.GetCustomAttributes(true).Any(attribute => attribute is IAuthorizeData or IAllowAnonymous);

    private static IEnumerable<(Type Controller, MethodInfo Action)> GetControllerActions() =>
        typeof(HousingController).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(controller => controller
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(action => action.GetCustomAttributes(true).Any(attribute => attribute is IRouteTemplateProvider))
                .Select(action => (Controller: controller, Action: action)));
}
