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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed class NinjaAuthorizationTests
{
    [Theory]
    [InlineData(nameof(NinjaController.UploadAccountSheet), PermissionKeys.Ninja.AccountsUpdate)]
    [InlineData(nameof(NinjaController.AccountSheetUploads), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaController.AccountSheetUpload), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaController.AccountSheetRows), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaController.AccountSheetNote), PermissionKeys.Ninja.AccountsUpdate)]
    [InlineData(nameof(NinjaController.CompareAccounts), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaController.Accounts), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaController.Account), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaController.SetAccountStatus), PermissionKeys.Ninja.AccountsUpdate)]
    [InlineData(nameof(NinjaController.PerformanceReport), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaController.DailyReport), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaController.RiderReport), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaController.Assign), PermissionKeys.Ninja.AssignmentsCreate)]
    [InlineData(nameof(NinjaController.Return), PermissionKeys.Ninja.AssignmentsDelete)]
    [InlineData(nameof(NinjaController.Credentials), PermissionKeys.Ninja.AccountsUpdate)]
    [InlineData(nameof(NinjaController.ImportPerformance), PermissionKeys.Ninja.PerformanceImport)]
    [InlineData(nameof(NinjaController.ImportDeductions), PermissionKeys.Ninja.DeductionsImport)]
    [InlineData(nameof(NinjaController.Performance), PermissionKeys.Ninja.Read)]
    [InlineData(nameof(NinjaController.Deductions), PermissionKeys.Ninja.Read)]
    public void NinjaActionRequiresItsDedicatedPermission(string actionName, string permission)
    {
        var action = typeof(NinjaController).GetMethod(actionName)!;
        Assert.Equal(AuthenticationPolicies.PermissionPrefix + permission,
            action.GetCustomAttribute<RequirePermissionAttribute>()!.Policy);
    }

    [Fact]
    public void EveryNinjaEndpointUsesOnlyNinjaPermissionKeys()
    {
        var actions = typeof(NinjaController).GetMethods().Where(m => m.GetCustomAttributes().OfType<HttpMethodAttribute>().Any()).ToArray();
        Assert.Equal(19, actions.Length);
        Assert.All(actions, action => Assert.StartsWith(AuthenticationPolicies.PermissionPrefix + "ninja.",
            action.GetCustomAttribute<RequirePermissionAttribute>()!.Policy, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true, "/api/ninja/accounts")]
    [InlineData(false, "/api/ninja/accounts")]
    [InlineData(true, "/api/ninja/accounts/compare")]
    [InlineData(false, "/api/ninja/accounts/compare")]
    [InlineData(true, "/api/ninja/accounts/00000000-0000-0000-0000-000000000001/status")]
    [InlineData(false, "/api/ninja/accounts/00000000-0000-0000-0000-000000000001/status")]
    [InlineData(true, "/api/ninja/reports/performance")]
    [InlineData(false, "/api/ninja/reports/performance")]
    [InlineData(true, "/api/ninja/reports/daily")]
    [InlineData(false, "/api/ninja/reports/daily")]
    [InlineData(true, "/api/ninja/reports/riders")]
    [InlineData(false, "/api/ninja/reports/riders")]
    [InlineData(true, "/api/ninja/accounts/sheet-updates")]
    [InlineData(false, "/api/ninja/accounts/sheet-updates")]
    [InlineData(true, "/api/ninja/accounts/sheet-updates/00000000-0000-0000-0000-000000000001")]
    [InlineData(false, "/api/ninja/accounts/sheet-updates/00000000-0000-0000-0000-000000000001")]
    [InlineData(true, "/api/ninja/accounts/sheet-updates/00000000-0000-0000-0000-000000000001/rows")]
    [InlineData(false, "/api/ninja/accounts/sheet-updates/00000000-0000-0000-0000-000000000001/rows")]
    [InlineData(true, "/api/ninja/accounts/sheet-updates/00000000-0000-0000-0000-000000000001/note")]
    [InlineData(false, "/api/ninja/accounts/sheet-updates/00000000-0000-0000-0000-000000000001/note")]
    public async Task NinjaEndpointsCheckTheirPlatformScopeBeforeReachingTheService(bool granted, string path)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"NinjaAuthorization_{Guid.NewGuid():N}", m => m.EnableNullChecks(false)).Options);
        var platform = new ClientPlatform { Code = "NINJA", NameAr = "نينجا", NameEn = "Ninja" };
        db.ClientPlatforms.Add(platform); await db.SaveChangesAsync(ct);
        var checker = new Checker(platform.Id, granted);
        var handler = new PermissionAuthorizationHandler(checker, db);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(AuthenticationClaimNames.Subject, Guid.NewGuid().ToString()),
            new Claim(AuthenticationClaimNames.AuthorizationVersion, "1"),
            new Claim(AuthenticationClaimNames.PasswordChangeRequired, "false") }, "test"));
        foreach (var permission in new[] { PermissionKeys.Ninja.Read, PermissionKeys.Ninja.AccountsUpdate,
            PermissionKeys.Ninja.AssignmentsCreate, PermissionKeys.Ninja.AssignmentsDelete,
            PermissionKeys.Ninja.CredentialsRotate, PermissionKeys.Ninja.PerformanceImport, PermissionKeys.Ninja.DeductionsImport })
        {
            var http = new DefaultHttpContext(); http.Request.Path = path;
            var context = new AuthorizationHandlerContext([new PermissionRequirement(permission)], principal, http);
            await handler.HandleAsync(context);
            Assert.Equal(granted, context.HasSucceeded); Assert.True(checker.CorrectScope);
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
