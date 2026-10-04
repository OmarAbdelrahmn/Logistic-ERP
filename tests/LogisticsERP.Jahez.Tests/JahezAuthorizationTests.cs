using System.Reflection;
using System.Security.Claims;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed class JahezAuthorizationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ApiPermissionHandlerResolvesTheJahezPlatformScope(bool permitted)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"JahezAuthorization_{Guid.NewGuid():N}", o => o.EnableNullChecks(false)).Options);
        var platform = new ClientPlatform { Code = "JAHEZ", NameAr = "جاهز", NameEn = "Jahez" };
        db.Add(platform); await db.SaveChangesAsync(ct);
        var checker = new ScopedChecker(platform.Id, permitted);
        var handler = new PermissionAuthorizationHandler(checker, db);
        var requirement = new PermissionRequirement(PermissionKeys.Jahez.Read);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AuthenticationClaimNames.Subject, Guid.NewGuid().ToString()),
            new Claim(AuthenticationClaimNames.AuthorizationVersion, "1"),
            new Claim(AuthenticationClaimNames.PasswordChangeRequired, "false")
        }, "test"));
        var context = new AuthorizationHandlerContext([requirement], principal, null);
        await handler.HandleAsync(context);
        Assert.Equal(permitted, context.HasSucceeded);
        Assert.True(checker.ScopeWasCorrect);
    }

    [Fact]
    public void EveryJahezEndpointHasARegisteredPermissionAndEveryCommandHasAnIdempotencyHeader()
    {
        var actions = typeof(JahezController).GetMethods().Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any()).ToArray();
        Assert.NotEmpty(actions);
        foreach (var action in actions)
        {
            var permission = Assert.Single(action.GetCustomAttributes<RequirePermissionAttribute>());
            Assert.StartsWith(AuthenticationPolicies.PermissionPrefix + "jahez.", permission.Policy, StringComparison.Ordinal);
            Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>());
            if (action.GetCustomAttribute<HttpPostAttribute>() is not null)
            {
                var header = Assert.Single(action.GetParameters(), p => p.GetCustomAttribute<FromHeaderAttribute>()?.Name == "Idempotency-Key");
                Assert.Equal(typeof(string), header.ParameterType);
            }
        }
    }

    private sealed class ScopedChecker(Guid platformId, bool permitted) : IPermissionChecker
    {
        public bool ScopeWasCorrect { get; private set; }
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default)
        {
            ScopeWasCorrect = scope?.Type == AccessScopeType.ClientPlatform && scope.TargetId == platformId;
            return Task.FromResult(ScopeWasCorrect && permitted && permissionKey == PermissionKeys.Jahez.Read);
        }
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
}
