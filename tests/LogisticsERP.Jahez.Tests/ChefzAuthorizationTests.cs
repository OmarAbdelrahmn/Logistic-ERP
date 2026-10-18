using System.Security.Claims;
using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed class ChefzAuthorizationTests
{
    [Fact]
    public void AssignmentStartDateEndpointRequiresChefzAccountUpdatePermission()
    {
        var action = typeof(ChefzController).GetMethod(nameof(ChefzController.UpdateAssignmentStartDate))!;
        Assert.Equal(AuthenticationPolicies.PermissionPrefix + PermissionKeys.Chefz.AccountsUpdate,
            action.GetCustomAttribute<RequirePermissionAttribute>()!.Policy);
        Assert.Equal("assignments/{assignmentId:guid}/start-date", action.GetCustomAttribute<HttpPutAttribute>()!.Template);
    }

    [Fact]
    public void StatusEndpointRequiresChefzAccountUpdatePermission()
    {
        var action = typeof(ChefzController).GetMethod(nameof(ChefzController.SetAccountStatus))!;
        Assert.Equal(AuthenticationPolicies.PermissionPrefix + PermissionKeys.Chefz.AccountsUpdate,
            action.GetCustomAttribute<RequirePermissionAttribute>()!.Policy);
        Assert.Equal("accounts/{accountId:guid}/status", action.GetCustomAttribute<HttpPutAttribute>()!.Template);
    }

    [Theory]
    [InlineData(nameof(ChefzController.PreviewOrderSettlements), PermissionKeys.Chefz.Read)]
    [InlineData(nameof(ChefzController.ImportOrderSettlements), PermissionKeys.Chefz.SettlementsCreate)]
    [InlineData(nameof(ChefzController.OrderSettlementImport), PermissionKeys.Chefz.Read)]
    public void BulkSettlementEndpointsRequireTheirChefzPermission(string actionName, string permission)
    {
        var action = typeof(ChefzController).GetMethod(actionName)!;
        Assert.Equal(AuthenticationPolicies.PermissionPrefix + permission,
            action.GetCustomAttribute<RequirePermissionAttribute>()!.Policy);
        if (actionName != nameof(ChefzController.OrderSettlementImport))
        {
            Assert.Equal(11L * 1024 * 1024, Assert.IsType<long>(action.GetCustomAttributesData()
                .Single(a => a.AttributeType == typeof(RequestSizeLimitAttribute)).ConstructorArguments[0].Value));
            Assert.Contains("multipart/form-data", action.GetCustomAttribute<ConsumesAttribute>()!.ContentTypes);
        }
    }

    [Fact]
    public void AllRidersReportRequiresChefzReadPermission()
    {
        var action = typeof(ChefzController).GetMethod(nameof(ChefzController.AllRidersReport))!;
        Assert.Equal("reports/riders/all", action.GetCustomAttribute<HttpGetAttribute>()!.Template);
        Assert.Equal(AuthenticationPolicies.PermissionPrefix + PermissionKeys.Chefz.Read,
            action.GetCustomAttribute<RequirePermissionAttribute>()!.Policy);
    }

    [Fact]
    public void PasswordResponseIsJsonAndCannotBeCached()
    {
        var action = typeof(ChefzController).GetMethod(nameof(ChefzController.Password))!;
        Assert.Contains("application/json", action.GetCustomAttribute<ProducesAttribute>()!.ContentTypes);
        var cache = action.GetCustomAttribute<ResponseCacheAttribute>()!;
        Assert.True(cache.NoStore);
        Assert.Equal(ResponseCacheLocation.None, cache.Location);
    }

    [Theory]
    [InlineData("CHEFZ", true)]
    [InlineData("THECHEFZ", true)]
    [InlineData("CHEFSZ", true)]
    [InlineData("SHIFTZ", true)]
    [InlineData("CHEFZ", false)]
    public async Task ScopedChefzPermissionsReachTheCorrectPlatform(string code, bool granted)
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ChefzAuth_{Guid.NewGuid():N}", o => o.EnableNullChecks(false)).Options);
        var platform = new ClientPlatform { Code = code, NameAr = "شيفز", NameEn = "Chefz" };
        db.ClientPlatforms.Add(platform);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var checker = new Checker(platform.Id, granted);
        var handler = new PermissionAuthorizationHandler(checker, db);
        foreach (var permission in new[] { PermissionKeys.Chefz.Read, PermissionKeys.Chefz.AccountsUpdate,
            PermissionKeys.Chefz.PerformanceImport, PermissionKeys.Chefz.SettlementsCreate })
        {
            var requirement = new PermissionRequirement(permission);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(AuthenticationClaimNames.Subject, Guid.NewGuid().ToString()),
                new Claim(AuthenticationClaimNames.AuthorizationVersion, "1"),
                new Claim(AuthenticationClaimNames.PasswordChangeRequired, "false") }, "test"));
            var context = new AuthorizationHandlerContext([requirement], user, null);
            await handler.HandleAsync(context);
            Assert.Equal(granted, context.HasSucceeded);
            Assert.True(checker.CorrectScope);
        }
    }

    private sealed class Checker(Guid platformId, bool granted) : IPermissionChecker
    {
        public bool CorrectScope { get; private set; }
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default)
        {
            CorrectScope = scope?.Type == AccessScopeType.ClientPlatform && scope.TargetId == platformId;
            return Task.FromResult(CorrectScope && granted);
        }
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
}
