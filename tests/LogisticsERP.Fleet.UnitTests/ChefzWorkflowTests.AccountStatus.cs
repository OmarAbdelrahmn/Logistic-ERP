using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Chefz;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdminCanSwitchAllManualStatusesAndRestoreWithoutChangingAssignments(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("CHEFZ_SQL_TESTS") == "1", "Set CHEFZ_SQL_TESTS=1 for disposable SQL Server tests.");
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(sql);
        f.Account.Status = PlatformRiderAccountStatus.Assigned;
        if (!sql) f.Account.RowVersion = [1];
        await f.Db.SaveChangesAsync(ct);
        var initial = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.Equal("Assigned", initial.Status);
        var details = Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        if (!sql)
        {
            Assert.Single(f.Db.ChefzAccountDetails).RowVersion = [2];
            await f.Db.SaveChangesAsync(ct);
            details = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        }
        var current = details;
        foreach (var from in new[] { "Suspended", "Retired", "Archived" })
        {
            current = Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new(from, current.AccountRowVersion), ct));
            foreach (var to in new[] { "Suspended", "Retired", "Archived" })
            {
                var previousVersion = current.AccountRowVersion;
                current = Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new(to, previousVersion, "  Admin choice  "), ct));
                Assert.Equal(to, current.Status);
                Assert.Equal("Admin choice", current.StatusReason);
                Assert.Equal(details.RowVersion, current.RowVersion);
                Assert.False(f.Account.IsDeleted);
                Assert.Equal(to, Assert.Single(Value(await f.Service.GetAccountsAsync(1, 50, ct)).Items).Status);
                Assert.Equal(2, await f.Db.RiderClientAssignments.CountAsync(ct));
                Assert.Null(f.Second.EffectiveTo);
                if (sql)
                {
                    Assert.NotEqual(previousVersion, current.AccountRowVersion);
                    Assert.Equal("Chefz.Conflict", (await f.Service.SetAccountStatusAsync(f.Account.Id, new("Active", previousVersion), ct)).Error.Code);
                }
            }
        }
        current = Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new("Active", current.AccountRowVersion), ct));
        Assert.Equal("Assigned", current.Status);
        Assert.Null(current.StatusReason);
        Assert.Equal(2, await f.Db.RiderClientAssignments.CountAsync(ct));
        var audits = await f.Db.AuditEntries.AsNoTracking().Where(a => a.EntityId == f.Account.Id && a.Action == "Updated").ToArrayAsync(ct);
        Assert.Contains(audits, a => a.ActorUserId is not null && a.OccurredAtUtc == new Clock().GetUtcNow()
            && a.AfterJson!.Contains("Admin choice", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Suspended", false)]
    [InlineData("Retired", false)]
    [InlineData("Archived", false)]
    [InlineData("Assigned", false)]
    [InlineData("Suspended", true)]
    [InlineData("Retired", true)]
    [InlineData("Archived", true)]
    [InlineData("Assigned", true)]
    public async Task ReturningRiderPreservesAdminStatusAndTakingAccountSetsAssigned(string status, bool legacy)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        f.Account.Status = Enum.Parse<PlatformRiderAccountStatus>(status);
        f.Account.RowVersion = [1]; f.Second.RowVersion = [2];
        f.Account.StatusReason = status == "Assigned" ? null : "Admin choice";
        f.Db.RiderProfiles.Add(new RiderProfile { EmployeeId = f.Account.RegisteredEmployeeId!.Value });
        var employee = await f.Db.Employees.SingleAsync(e => f.Db.RiderProfiles.Any(r => r.Id == f.Second.RiderProfileId && r.EmployeeId == e.Id), ct);
        employee.Status = EmployeeStatus.Active;
        await f.Db.SaveChangesAsync(ct);
        var simple = new SimplePlatformService(f.Db, new CurrentUser(), new Clock(), new PlatformCredentialProtector(() => new byte[32]));
        var operations = new PlatformOperationsService(f.Db, new CurrentUser(), new Clock(), new PlatformCredentialProtector(() => new byte[32]));
        if (legacy)
            Value(await operations.CloseAssignmentAsync(f.Second.Id, new(new(2026, 10, 20), "Ended", "Returned", "Ag=="), ct));
        else
            Value(await simple.ReleaseAccountAsync(f.Account.Id, new(new(2026, 10, 20), "Ended", "Returned", "Ag=="), ct));
        var released = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.Equal(status == "Assigned" ? "Available" : status, released.Status);
        Assert.Equal(new DateOnly(2026, 10, 20), f.Second.EffectiveTo);
        Assert.Equal(2, await f.Db.RiderClientAssignments.CountAsync(ct));
        if (status != "Assigned")
        {
            Assert.Equal("Admin choice", released.StatusReason);
            Assert.True((await simple.AssignAccountAsync(f.Account.Id, new(f.Second.RiderProfileId, new(2026, 10, 21), null, false, null), ct)).IsFailure);
            released = Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new("Active", released.AccountRowVersion), ct));
            Assert.Equal("Available", released.Status);
            Assert.Null(released.StatusReason);
        }
        if (legacy)
            Value(await operations.AssignAccountAsync(new(f.Second.RiderProfileId, f.Second.ClientContractId, f.Account.Id,
                new(2026, 10, 21), "Active", "Taken", null, null, false, null), ct));
        else
            Value(await simple.AssignAccountAsync(f.Account.Id, new(f.Second.RiderProfileId, new(2026, 10, 21), "Taken", false, null), ct));
        Assert.Equal("Assigned", Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).Status);
        Assert.Equal(3, await f.Db.RiderClientAssignments.CountAsync(ct));
    }

    [Theory]
    [InlineData("CHEFZ")]
    [InlineData("THECHEFZ")]
    [InlineData("CHEFSZ")]
    [InlineData("SHIFTZ")]
    public async Task StatusControlHonorsPermissionsTokensAndChefzScope(string code)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        (await f.Db.ClientPlatforms.SingleAsync(ct)).Code = code;
        f.Account.RowVersion = [1];
        await f.Db.SaveChangesAsync(ct);
        foreach (var status in new[] { "Available", "Assigned", "Unknown", "3", "", null })
            Assert.Equal("Chefz.Invalid", (await f.Service.SetAccountStatusAsync(f.Account.Id, new(status!, "AQ=="), ct)).Error.Code);
        foreach (var token in new[] { "Ag==", "bad", "", null })
            Assert.Equal("Chefz.Conflict", (await f.Service.SetAccountStatusAsync(f.Account.Id, new("Retired", token!), ct)).Error.Code);
        Assert.Equal("Chefz.Invalid", (await f.Service.SetAccountStatusAsync(f.Account.Id, new("Retired", "AQ==", new string('x', 501)), ct)).Error.Code);
        f.Checker.Allow = false;
        Assert.Equal("Chefz.Forbidden", (await f.Service.SetAccountStatusAsync(f.Account.Id, new("Archived", "AQ=="), ct)).Error.Code);
        f.Checker.Allow = true;
        Assert.Equal("Chefz.NotFound", (await f.Service.SetAccountStatusAsync(Guid.NewGuid(), new("Archived", "AQ=="), ct)).Error.Code);
        var other = new ClientPlatform { Code = "OTHER", NameAr = "آخر", NameEn = "Other" };
        var otherAccount = new PlatformRiderAccount { ClientPlatformId = other.Id, Code = "OTHER", ExternalAccountId = "other" };
        f.Db.AddRange(other, otherAccount);
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal("Chefz.NotFound", (await f.Service.SetAccountStatusAsync(otherAccount.Id, new("Archived", "AQ=="), ct)).Error.Code);
        Assert.Equal(PlatformRiderAccountStatus.Available, f.Account.Status);
        Assert.Null(f.Account.StatusReason);
        Assert.False(f.Account.IsDeleted);
        Assert.Equal("Retired", Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new("Retired", "AQ=="), ct)).Status);
    }

    [Fact]
    public async Task WorkbookMetadataEditsCannotOverwriteAdminLifecycle()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        f.Account.RowVersion = [1];
        await f.Db.SaveChangesAsync(ct);
        foreach (var status in new[] { "Suspended", "Retired", "Archived" })
        {
            Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new(status, "AQ==", "Admin choice"), ct));
            foreach (var dashboard in new[] { "active", "Block By CHEFZ", "The supervisor suspended" })
            {
                var current = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
                var saved = Value(await f.Service.SaveAccountAsync(f.Account.Id,
                    new("Freelancer", RowVersion: current.RowVersion, DashboardStatus: dashboard), ct));
                Assert.Equal(status, saved.Status);
                Assert.Equal("Admin choice", saved.StatusReason);
                Assert.Equal(dashboard, saved.DashboardStatus);
                // InMemory does not generate rowversions.
                Assert.Single(f.Db.ChefzAccountDetails).RowVersion = [2];
                await f.Db.SaveChangesAsync(ct);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingAccountEditAllowsChefzManualStatusesWithARider(bool legacy)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        var owner = new RiderProfile { EmployeeId = f.Account.RegisteredEmployeeId!.Value };
        var sponsor = new Sponsor { RegistryNameAr = "اختبار", EmployerIdentityNumber = "7000000000" };
        f.Db.AddRange(owner, sponsor);
        f.Account.SponsorId = sponsor.Id;
        f.Account.Status = PlatformRiderAccountStatus.Assigned;
        f.Account.RowVersion = [1];
        await f.Db.SaveChangesAsync(ct);
        var simple = new SimplePlatformService(f.Db, new CurrentUser(), new Clock(), new PlatformCredentialProtector(() => new byte[32]));
        var operations = new PlatformOperationsService(f.Db, new CurrentUser(), new Clock(), new PlatformCredentialProtector(() => new byte[32]));
        foreach (var status in new[] { "Suspended", "Retired", "Archived", "Assigned" })
        {
            if (legacy)
                Value(await operations.UpsertAccountAsync(f.Account.Id, new(f.Account.ClientPlatformId, owner.EmployeeId,
                    f.Account.OperatingCityId, sponsor.Id, f.Account.Code, f.Account.ExternalAccountId, null, "PayPerOrder", status,
                    null, null, null, null, null, null, "AQ=="), ct));
            else
                Value(await simple.UpdateAccountAsync(f.Account.Id, new(f.Account.ClientPlatformId, f.Account.OperatingCityId,
                    sponsor.Id, owner.Id, f.Account.Code, f.Account.ExternalAccountId, null, "PayPerOrder", status,
                    null, null, null, null, null, null, "AQ=="), ct));
            Assert.Equal(status, Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).Status);
            Assert.False(f.Account.IsDeleted);
            Assert.Null(f.Second.EffectiveTo);
        }
    }

    [Fact]
    public async Task ConcurrentStatusChangeReturnsConflictAndDoesNotOverwriteTheWinner()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("CHEFZ_SQL_TESTS") == "1", "Set CHEFZ_SQL_TESTS=1 for disposable SQL Server tests.");
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(true);
        await using var competing = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(f.Db.Database.GetConnectionString()).Options);
        var staleAccount = await competing.PlatformRiderAccounts.SingleAsync(a => a.Id == f.Account.Id, ct);
        var staleVersion = Convert.ToBase64String(staleAccount.RowVersion);
        Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new("Retired", staleVersion, "First admin"), ct));
        var staleService = new ChefzService(competing, new CurrentUser(), new Checker(), new Clock(), new PlatformCredentialProtector(() => new byte[32]));
        var result = await staleService.SetAccountStatusAsync(f.Account.Id, new("Archived", staleVersion, "Second admin"), ct);
        Assert.Equal("Chefz.Conflict", result.Error.Code);
        Assert.Empty(competing.ChangeTracker.Entries());
        Assert.Equal(0, await competing.SaveChangesAsync(ct));
        var persisted = await competing.PlatformRiderAccounts.AsNoTracking().SingleAsync(a => a.Id == f.Account.Id, ct);
        Assert.Equal(PlatformRiderAccountStatus.Retired, persisted.Status);
        Assert.Equal("First admin", persisted.StatusReason);
    }
}
