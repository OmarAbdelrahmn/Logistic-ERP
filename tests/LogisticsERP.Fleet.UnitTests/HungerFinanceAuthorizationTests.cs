using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Authentication;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HungerFinanceAuthorizationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IndividualFinanceGrantsAreIsolatedAndLegacyManageIsScopedSuperset(bool throughRole)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var application = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"HungerAuthorization_{Guid.NewGuid():N}", b => b.EnableNullChecks(false)).Options);
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"HungerIdentity_{Guid.NewGuid():N}", b => b.EnableNullChecks(false)).Options);
        await application.Database.EnsureCreatedAsync(ct);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var checker = new PermissionChecker(identity, application, cache, TimeProvider.System);
        var userId = Guid.NewGuid();
        var platform = new PermissionScope(AccessScopeType.ClientPlatform, Guid.NewGuid());
        var other = new PermissionScope(AccessScopeType.ClientPlatform, Guid.NewGuid());
        var role = new ApplicationRole { Id = Guid.NewGuid(), Status = RoleStatus.Active, Code = "FINANCE", Name = "Finance" };
        identity.Add(role);
        identity.Add(new UserRoleAssignment { UserId = userId, RoleId = role.Id, StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            GrantedByUserId = userId, IsAllClientScope = true });
        await identity.SaveChangesAsync(ct);
        foreach (var granted in PermissionKeys.HungerFinancePermissions.Append(PermissionKeys.Hunger.FinanceManage))
        {
            identity.RemoveRange(await identity.RolePermissionGrants.ToArrayAsync(ct));
            identity.RemoveRange(await identity.UserDirectPermissionAssignments.ToArrayAsync(ct));
            if (throughRole) identity.Add(new RolePermissionGrant { RoleId = role.Id, PermissionKey = granted });
            else identity.Add(new UserDirectPermissionAssignment { UserId = userId, PermissionKey = granted,
                Effect = PermissionEffect.Grant, StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1), GrantedByUserId = userId, IsAllClientScope = true });
            await identity.SaveChangesAsync(ct); checker.InvalidateUser(userId, 1);
            foreach (var required in PermissionKeys.HungerFinancePermissions)
                Assert.Equal(granted == required || granted == PermissionKeys.Hunger.FinanceManage,
                    await checker.HasPermissionAsync(userId, 1, required, platform, ct));
            Assert.False(await checker.HasPermissionAsync(userId, 1, PermissionKeys.Hunger.AccountsUpdate, platform, ct));
        }
        // A granular denial overrides the compatibility grant.
        identity.Add(new UserDirectPermissionAssignment { UserId = userId, PermissionKey = PermissionKeys.Hunger.RemittancesConfirm,
            Effect = PermissionEffect.Deny, StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1), GrantedByUserId = userId, IsAllClientScope = true });
        await identity.SaveChangesAsync(ct); checker.InvalidateUser(userId, 1);
        Assert.False(await checker.HasPermissionAsync(userId, 1, PermissionKeys.Hunger.RemittancesConfirm, platform, ct));
        Assert.True(await checker.HasPermissionAsync(userId, 1, PermissionKeys.Hunger.FinanceRead, other, ct));
        // Compatibility must retain the original assignment scope.
        identity.RemoveRange(await identity.RolePermissionGrants.ToArrayAsync(ct));
        identity.RemoveRange(await identity.UserDirectPermissionAssignments.ToArrayAsync(ct));
        var scopedGrant = new UserDirectPermissionAssignment { UserId = userId, PermissionKey = PermissionKeys.Hunger.FinanceManage,
            Effect = PermissionEffect.Grant, StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1), GrantedByUserId = userId };
        identity.Add(scopedGrant);
        identity.Add(new AccessScope { DirectPermissionAssignmentId = scopedGrant.Id, ScopeType = platform.Type, TargetId = platform.TargetId });
        await identity.SaveChangesAsync(ct); checker.InvalidateUser(userId, 1);
        Assert.True(await checker.HasPermissionAsync(userId, 1, PermissionKeys.Hunger.FinanceRead, platform, ct));
        Assert.False(await checker.HasPermissionAsync(userId, 1, PermissionKeys.Hunger.FinanceRead, other, ct));
        Assert.False(await checker.HasPermissionAsync(userId, 1, PermissionKeys.Hunger.FinanceRead, cancellationToken: ct));
    }
}
