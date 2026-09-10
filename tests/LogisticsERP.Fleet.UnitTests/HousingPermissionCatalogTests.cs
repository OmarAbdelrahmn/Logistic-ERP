using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Authentication;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HousingPermissionCatalogTests
{
    [Theory]
    [InlineData(PermissionKeys.Operations.HousingRead)]
    [InlineData(PermissionKeys.Operations.HousingManage)]
    public void HousingControllerPermissionsAreModuleLevel(string permissionKey)
    {
        var definition = Assert.Single(PermissionSeedCatalog.All, item => item.Key == permissionKey);

        Assert.False(definition.RequiresHousingScope);
        Assert.False(definition.RequiresClientScope);
    }

    [Fact]
    public void PermissionConstantsAndDatabaseCatalogStayInSync()
    {
        Assert.Equal(
            PermissionKeys.All.Order(StringComparer.Ordinal),
            PermissionSeedCatalog.All.Select(item => item.Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void RolePermissionUniquenessIgnoresSoftDeletedHistory()
    {
        using var identity = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RolePermissionModel;Trusted_Connection=True")
                .Options);
        var entityType = identity.Model.FindEntityType(typeof(RolePermissionGrant));
        var index = Assert.Single(entityType!.GetIndexes(), candidate =>
            candidate.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(RolePermissionGrant.RoleId), nameof(RolePermissionGrant.PermissionKey)]));

        Assert.True(index.IsUnique);
        Assert.Equal("[IsDeleted] = 0", index.GetFilter());
    }

    [Fact]
    public async Task CustomRoleHousingGrantWorksWithoutRoleNameOrHousingScope()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databaseName = Guid.NewGuid().ToString();
        await using var identity = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase(databaseName + "-identity", options => options.EnableNullChecks(false))
                .Options);
        await using var application = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName + "-application", options => options.EnableNullChecks(false))
                .Options);
        await identity.Database.EnsureCreatedAsync(cancellationToken);
        await application.Database.EnsureCreatedAsync(cancellationToken);

        var now = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);
        var userId = Guid.CreateVersion7();
        var roleId = Guid.CreateVersion7();
        identity.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "housing-user",
            NormalizedUserName = "HOUSING-USER",
            DisplayNameAr = "مستخدم السكن",
            DisplayNameEn = "Housing User",
            EmailConfirmed = true,
            Status = UserAccountStatus.Active,
            AuthorizationVersion = 1
        });
        identity.Roles.Add(new ApplicationRole
        {
            Id = roleId,
            Name = "HOUSING_COORDINATOR",
            NormalizedName = "HOUSING_COORDINATOR",
            Code = "HOUSING_COORDINATOR",
            NameAr = "منسق السكن",
            NameEn = "Housing Coordinator",
            Status = RoleStatus.Active
        });
        identity.RolePermissionGrants.Add(new RolePermissionGrant
        {
            RoleId = roleId,
            PermissionKey = PermissionKeys.Operations.HousingManage
        });
        identity.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = userId,
            RoleId = roleId,
            StartsAtUtc = now.AddDays(-1),
            GrantedByUserId = userId,
            GrantReason = "Test assignment"
        });
        await identity.SaveChangesAsync(cancellationToken);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var checker = new PermissionChecker(identity, application, cache, new FixedTimeProvider(now));

        Assert.True(await checker.HasPermissionAsync(
            userId,
            1,
            PermissionKeys.Operations.HousingManage,
            cancellationToken: cancellationToken));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
