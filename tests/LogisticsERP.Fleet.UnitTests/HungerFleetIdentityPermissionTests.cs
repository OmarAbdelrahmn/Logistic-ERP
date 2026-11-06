using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Authentication;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Identity.SeedData;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HungerFleetIdentityPermissionTests
{
    [Fact]
    public async Task FleetVehicleIdentityIsIndependentAndOnlySeededToAdministrator()
    {
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal(SystemRoles.SystemAdminId, Assert.Single(AuthorizationSeedCatalog.RolePermissions,
            p => p.PermissionKey == PermissionKeys.Fleet.VehicleIdentity).RoleId);
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"IdentityField_{Guid.NewGuid():N}", x => x.EnableNullChecks(false)).Options);
        await using var application = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"IdentityFieldApp_{Guid.NewGuid():N}", x => x.EnableNullChecks(false)).Options);
        await application.Database.EnsureCreatedAsync(ct);
        var user = new ApplicationUser { Id = Guid.CreateVersion7(), Status = UserAccountStatus.Active };
        identity.Users.Add(user);
        var grant = new UserDirectPermissionAssignment { UserId = user.Id, PermissionKey = PermissionKeys.Fleet.VehiclesUpdate,
            Effect = PermissionEffect.Grant, StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1), GrantedByUserId = user.Id,
            IsAllHousingScope = true, IsAllClientScope = true };
        identity.Add(grant); await identity.SaveChangesAsync(ct);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var checker = new PermissionChecker(identity, application, cache, TimeProvider.System);
        Assert.False(await checker.HasPermissionAsync(user.Id, 1, PermissionKeys.Fleet.VehicleIdentity, cancellationToken: ct));
        grant.PermissionKey = PermissionKeys.Fleet.VehicleIdentity;
        await identity.SaveChangesAsync(ct); checker.InvalidateUser(user.Id, 1);
        Assert.True(await checker.HasPermissionAsync(user.Id, 1, PermissionKeys.Fleet.VehicleIdentity, cancellationToken: ct));
        Assert.False(await checker.HasPermissionAsync(user.Id, 1, PermissionKeys.Fleet.VehiclesUpdate, cancellationToken: ct));
    }
}
