using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Authentication;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class GranularPermissionIsolationTests
{
    private static readonly string[] Keys = PermissionKeys.All.Where(key =>
        key.EndsWith(".read", StringComparison.Ordinal)
        || key.EndsWith(".create", StringComparison.Ordinal)
        || key.EndsWith(".update", StringComparison.Ordinal)
        || key.EndsWith(".delete", StringComparison.Ordinal)).ToArray();

    public static TheoryData<string, bool> Grants => new(Keys.SelectMany(key => new[] { (key, false), (key, true) }));

    [Theory]
    [MemberData(nameof(Grants))]
    public async Task OneActionPermissionDoesNotAuthorizeOtherActions(string grantedKey, bool throughRole)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"GranularIdentity_{Guid.NewGuid():N}", x => x.EnableNullChecks(false)).Options);
        await using var application = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"GranularApplication_{Guid.NewGuid():N}", x => x.EnableNullChecks(false)).Options);
        await application.Database.EnsureCreatedAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var user = new ApplicationUser { Id = Guid.CreateVersion7(), Status = UserAccountStatus.Active };
        identity.Users.Add(user);
        if (throughRole)
        {
            var role = new ApplicationRole { Id = Guid.CreateVersion7(), Status = RoleStatus.Active, Code = "CUSTOM", Name = "Custom" };
            identity.AddRange(role, new RolePermissionGrant { RoleId = role.Id, PermissionKey = grantedKey },
                new UserRoleAssignment { UserId = user.Id, RoleId = role.Id, StartsAtUtc = now.AddDays(-1), GrantedByUserId = user.Id, IsAllHousingScope = true, IsAllClientScope = true });
        }
        else
        {
            identity.UserDirectPermissionAssignments.Add(new UserDirectPermissionAssignment
            {
                UserId = user.Id, PermissionKey = grantedKey, Effect = PermissionEffect.Grant,
                StartsAtUtc = now.AddDays(-1), GrantedByUserId = user.Id, IsAllHousingScope = true, IsAllClientScope = true
            });
        }
        await identity.SaveChangesAsync(ct);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var checker = new PermissionChecker(identity, application, cache, TimeProvider.System);

        foreach (var key in Keys)
            Assert.Equal(key == grantedKey, await checker.HasPermissionAsync(user.Id, 1, key, cancellationToken: ct));
        Assert.DoesNotContain(PermissionKeys.All, key => key.EndsWith(".manage", StringComparison.Ordinal));
        Assert.False(await checker.HasPermissionAsync(user.Id, 1, "fuel.manage", cancellationToken: ct));
        Assert.False(await checker.HasPermissionAsync(user.Id, 1, "external_riders.manage", cancellationToken: ct));
        Assert.False(await checker.HasPermissionAsync(user.Id, 1, "maintenance.work_orders.manage", cancellationToken: ct));
        if (throughRole)
        {
            identity.UserDirectPermissionAssignments.Add(new UserDirectPermissionAssignment
            {
                UserId = user.Id, PermissionKey = grantedKey, Effect = PermissionEffect.Deny,
                StartsAtUtc = now.AddDays(-1), GrantedByUserId = user.Id, IsAllHousingScope = true, IsAllClientScope = true
            });
        }
        else
        {
            (await identity.UserDirectPermissionAssignments.SingleAsync(ct)).ExpiresAtUtc = now.AddMinutes(-1);
        }
        await identity.SaveChangesAsync(ct);
        checker.InvalidateUser(user.Id, 1);
        Assert.False(await checker.HasPermissionAsync(user.Id, 1, grantedKey, cancellationToken: ct));
    }
}
