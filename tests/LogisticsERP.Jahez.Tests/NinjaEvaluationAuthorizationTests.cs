using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed class NinjaEvaluationAuthorizationTests
{
    [Theory]
    [InlineData(nameof(NinjaEvaluationController.Evaluate), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaEvaluationController.Day), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaEvaluationController.Policy), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaEvaluationController.Context), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaEvaluationController.SetContext), PermissionKeys.Ninja.EvaluationManage)]
    [InlineData(nameof(NinjaEvaluationController.Responsibility), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaEvaluationController.SetResponsibility), PermissionKeys.Ninja.EvaluationManage)]
    [InlineData(nameof(NinjaEvaluationController.Export), PermissionKeys.Ninja.EvaluationExport)]
    [InlineData(nameof(NinjaEvaluationController.Snapshot), PermissionKeys.Ninja.EvaluationManage)]
    [InlineData(nameof(NinjaEvaluationController.GetSnapshot), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaEvaluationController.Template), PermissionKeys.Ninja.PerformanceImport)]
    [InlineData(nameof(NinjaEvaluationController.Preview), PermissionKeys.Ninja.PerformanceImport)]
    [InlineData(nameof(NinjaEvaluationController.Confirm), PermissionKeys.Ninja.PerformanceImport)]
    public void EveryEvaluationRouteHasItsExpectedNinjaPermission(string method, string permission)
    {
        var action = typeof(NinjaEvaluationController).GetMethod(method)!;
        Assert.NotNull(action.GetCustomAttributes().OfType<HttpMethodAttribute>().Single());
        Assert.Equal(AuthenticationPolicies.PermissionPrefix + permission, action.GetCustomAttribute<RequirePermissionAttribute>()!.Policy);
    }
    [Fact]
    public void NewRoutesDoNotReplaceLegacyEndpoints()
    {
        Assert.Equal(13, typeof(NinjaEvaluationController).GetMethods().Count(m => m.GetCustomAttributes().OfType<HttpMethodAttribute>().Any()));
        Assert.Equal(20, typeof(NinjaController).GetMethods().Count(m => m.GetCustomAttributes().OfType<HttpMethodAttribute>().Any()));
    }
}
