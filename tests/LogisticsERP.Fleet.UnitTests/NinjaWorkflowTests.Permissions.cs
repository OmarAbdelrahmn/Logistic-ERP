using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Authentication;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Identity.SeedData;
using LogisticsERP.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Theory]
    [InlineData(PermissionKeys.Ninja.Read, PermissionKeys.Operations.PlatformAccountsRead)]
    [InlineData(PermissionKeys.Ninja.AccountsUpdate, PermissionKeys.Operations.PlatformAccountsUpdate)]
    [InlineData(PermissionKeys.Ninja.AssignmentsCreate, PermissionKeys.Operations.PlatformAssignmentsCreate)]
    [InlineData(PermissionKeys.Ninja.AssignmentsDelete, PermissionKeys.Operations.PlatformAssignmentsDelete)]
    [InlineData(PermissionKeys.Ninja.CredentialsRotate, PermissionKeys.Operations.PlatformCredentialsRotate)]
    [InlineData(PermissionKeys.Ninja.PerformanceImport, PermissionKeys.Operations.PlatformAccountsUpdate)]
    [InlineData(PermissionKeys.Ninja.DeductionsImport, PermissionKeys.Operations.PlatformAccountsUpdate)]
    public async Task NinjaPermissionCheckerRequiresItsExactKeyAndClientScope(string ninjaKey, string sharedKey)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"NinjaPermissionIdentity_{Guid.NewGuid():N}", x => x.EnableNullChecks(false)).Options);
        var userId = f.User.UserId!.Value;
        identity.Users.Add(new ApplicationUser { Id = userId, Status = UserAccountStatus.Active });
        var grant = new UserDirectPermissionAssignment { UserId = userId, PermissionKey = sharedKey,
            Effect = PermissionEffect.Grant, StartsAtUtc = f.Clock.GetUtcNow().AddDays(-1), GrantedByUserId = userId };
        identity.AddRange(grant, new AccessScope { DirectPermissionAssignmentId = grant.Id,
            ScopeType = AccessScopeType.ClientPlatform, TargetId = f.Account.ClientPlatformId });
        await identity.SaveChangesAsync(ct);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var checker = new PermissionChecker(identity, f.Db, cache, f.Clock);
        var scope = new PermissionScope(AccessScopeType.ClientPlatform, f.Account.ClientPlatformId);
        Assert.False(await checker.HasPermissionAsync(userId, 1, ninjaKey, scope, ct));
        grant.PermissionKey = ninjaKey;
        await identity.SaveChangesAsync(ct);
        checker.InvalidateUser(userId, 1);
        Assert.True(await checker.HasPermissionAsync(userId, 1, ninjaKey, scope, ct));
        Assert.False(await checker.HasPermissionAsync(userId, 1, ninjaKey, new(AccessScopeType.ClientPlatform, Guid.NewGuid()), ct));
        foreach (var otherKey in PermissionKeys.All.Where(k => k.StartsWith("ninja.", StringComparison.Ordinal) && k != ninjaKey))
            Assert.False(await checker.HasPermissionAsync(userId, 1, otherKey, scope, ct));
        grant.Effect = PermissionEffect.Deny;
        await identity.SaveChangesAsync(ct);
        checker.InvalidateUser(userId, 1);
        Assert.False(await checker.HasPermissionAsync(userId, 1, ninjaKey, scope, ct));
    }

    [Fact]
    public void NinjaPermissionsHaveTheirOwnCatalogGroupAndAdminGrants()
    {
        var permissions = PermissionKeys.All.Where(k => k.StartsWith("ninja.", StringComparison.Ordinal)).ToArray();
        Assert.Equal(7, permissions.Length);
        foreach (var key in permissions)
        {
            var definition = Assert.Single(PermissionSeedCatalog.All, p => p.Key == key);
            Assert.Equal("Ninja", definition.Category);
            Assert.True(definition.RequiresClientScope);
            Assert.True(definition.IsSensitive);
            Assert.Contains(key, AuthorizationSeedCatalog.SystemAdminPermissions);
            Assert.Single(AuthorizationSeedCatalog.RolePermissions, p => p.PermissionKey == key);
        }
        Assert.Equal(PermissionSeedCatalog.All.Count, PermissionSeedCatalog.All.Select(p => p.Id).Distinct().Count());
        Assert.Equal(AuthorizationSeedCatalog.RolePermissions.Count, AuthorizationSeedCatalog.RolePermissions.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public async Task SharedPlatformPermissionsCannotAuthorizeNinjaServiceOperations()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        f.Checker.Granted = PermissionKeys.All.Where(k => !k.StartsWith("ninja.", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
        Func<Task<Result>>[] operations =
        [
            async () => await f.Service.GetAccountsAsync(1, 50, ct),
            async () => await f.Service.GetAccountAsync(f.Account.Id, ct),
            async () => await f.Service.SetAccountStatusAsync(f.Account.Id, new("Suspended", Convert.ToBase64String(f.Account.RowVersion)), ct),
            async () => await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111", "x", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct),
            async () => await f.Service.ReturnAsync(f.Account.Id, new(new(2026, 10, 12), "0501111111", "x", "Return", "AQ=="), ct),
            async () => await f.Service.UpdateCredentialsAsync(f.Account.Id, new("x", "0501111111", "Change", "AQ=="), ct),
            async () => await f.Service.ImportAsync(false, new(2026, 10, 12), "x.xlsx", [], ct),
            async () => await f.Service.ImportAsync(true, new(2026, 10, 12), "x.xlsx", [], ct),
            async () => await f.Service.UploadAccountSheetAsync("x.xlsx", [], null, ct),
            async () => await f.Service.GetAccountSheetUploadsAsync(1, 50, ct),
            async () => await f.Service.CompareAccountsAsync("x.xlsx", [], ct),
            async () => await f.Service.GetPerformanceAsync(new(2026, 10, 1), new(2026, 10, 12), null, null, 1, 50, ct),
            async () => await f.Service.GetDeductionsAsync(new(2026, 10, 1), new(2026, 10, 12), null, null, 1, 50, ct)
        ];
        foreach (var operation in operations) Assert.Equal("ninja.forbidden", (await operation()).Error.Code);
        Assert.Empty(f.Db.RiderClientAssignments);
        Assert.Empty(f.Db.PlatformAccountCredentialVersions);
    }

    [Fact]
    public async Task DedicatedNinjaGrantsSupportLifecycleAndIndependentImports()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        f.Checker.Granted = PermissionKeys.All.Where(k => k.StartsWith("ninja.", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111", "x", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.GetAccountsAsync(1, 50, ct));
        f.Checker.Granted = [PermissionKeys.Ninja.PerformanceImport];
        Assert.Empty(Value(await f.Service.ImportAsync(false, new(2026, 10, 12), "performance.xlsx", Sheet(false, "272578", 18, 12), ct)).Issues);
        Assert.Equal("ninja.forbidden", (await f.Service.ImportAsync(true, new(2026, 10, 12), "deductions.xlsx", [], ct)).Error.Code);
        f.Checker.Granted = [PermissionKeys.Ninja.DeductionsImport];
        Assert.Empty(Value(await f.Service.ImportAsync(true, new(2026, 10, 12), "deductions.xlsx", Sheet(true, "272578", 0, -6), ct)).Issues);
        Assert.Equal("ninja.forbidden", (await f.Service.ImportAsync(false, new(2026, 10, 12), "performance.xlsx", [], ct)).Error.Code);
    }
}
