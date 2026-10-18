using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class EmployeePlatformAssignmentTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnerWithoutAvailableProfileReturnsConflictWithoutStagingAssignment(bool deletedProfile)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        var owner = new Employee { FullNameAr = "مالك الحساب", IsEmployee = true };
        f.Db.Employees.Add(owner);
        if (deletedProfile)
            f.Db.RiderProfiles.Add(new RiderProfile { EmployeeId = owner.Id, IsDeleted = true });
        (await f.Db.PlatformRiderAccounts.SingleAsync(ct)).RegisteredEmployeeId = owner.Id;
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();

        var result = await f.Assign(false, ct);

        Assert.Equal("platform.account_owner_profile_required", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("ownerRiderProfileId", result.Error.Field);
        Assert.Equal(owner.Id, result.Error.Details!["registeredEmployeeId"]);
        Assert.Equal(f.Account.Id, result.Error.Details["accountId"]);
        Assert.Empty(await f.Db.RiderClientAssignments.ToArrayAsync(ct));
        Assert.Empty(await f.Db.RiderAssignmentEvents.ToArrayAsync(ct));
        Assert.Empty(await f.Db.PlatformAccountRegistrations.ToArrayAsync(ct));
        Assert.False(f.Db.ChangeTracker.HasChanges());
        Assert.Equal(PlatformRiderAccountStatus.Available, (await f.Db.PlatformRiderAccounts.SingleAsync(ct)).Status);
    }

    [Fact]
    public async Task ProvisioningOwnerProfileAllowsAssignmentToDifferentRiderWithoutChangingOwnershipOrRole()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        var owner = new Employee { FullNameAr = "مالك الحساب", IsEmployee = true };
        f.Db.Employees.Add(owner);
        (await f.Db.PlatformRiderAccounts.SingleAsync(ct)).RegisteredEmployeeId = owner.Id;
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();
        Assert.Equal("platform.account_owner_profile_required", (await f.Assign(false, ct)).Error.Code);

        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"OwnerProfileIdentity_{Guid.NewGuid():N}").Options);
        var provisioned = await new WorkforceService(f.Db, identity, new CurrentUser()).EnsureVehicleProfileAsync(owner.Id, ct);
        Assert.True(provisioned.IsSuccess, provisioned.Error.Code);
        f.Db.ChangeTracker.Clear();
        var result = await f.Assign(false, ct);

        Assert.True(result.IsSuccess, result.Error.Code);
        var registration = await f.Db.PlatformAccountRegistrations.SingleAsync(ct);
        Assert.Equal(owner.Id, registration.RegisteredEmployeeId);
        Assert.Equal(provisioned.Value!.RiderProfileId, registration.RiderProfileId);
        Assert.Equal(f.RiderId, (await f.Db.RiderClientAssignments.SingleAsync(ct)).RiderProfileId);
        Assert.Equal(owner.Id, (await f.Db.PlatformRiderAccounts.SingleAsync(ct)).RegisteredEmployeeId);
        Assert.True((await f.Db.Employees.SingleAsync(e => e.Id == owner.Id, ct)).IsEmployee);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProvisionedActiveEmployeeCanBeAssignedWithoutChangingEmployeeRole(bool legacy)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        var result = await f.Assign(legacy, ct);
        Assert.True(result.IsSuccess, result.Error.Code);
        var assignment = Assert.Single(await f.Db.RiderClientAssignments.AsNoTracking().ToArrayAsync(ct));
        Assert.Equal(f.RiderId, assignment.RiderProfileId);
        Assert.Equal(new DateOnly(2026, 10, 17), assignment.EffectiveFrom);
        Assert.Equal(RiderAssignmentStatus.Active, assignment.Status);
        Assert.Equal(PlatformRiderAccountStatus.Assigned, (await f.Db.PlatformRiderAccounts.SingleAsync(ct)).Status);
        var employee = await f.Db.Employees.SingleAsync(ct);
        Assert.True(employee.IsEmployee);
        Assert.Equal(EmployeeStatus.Active, employee.Status);
        Assert.Null(employee.OperationalWorkTypeId);
        Assert.Null(employee.OperatingCityId);
        Assert.Single(await f.Db.RiderProfiles.ToArrayAsync(ct));
    }

    [Fact]
    public async Task EmployeeAssignmentCanBeReadAndReleasedAfterEmployeeBecomesInactive()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        var assigned = await f.Assign(false, ct);
        Assert.True(assigned.IsSuccess, assigned.Error.Code);
        var assignment = await f.Db.RiderClientAssignments.SingleAsync(ct);
        assignment.RowVersion = [1];
        (await f.Db.Employees.SingleAsync(ct)).Status = EmployeeStatus.Suspended;
        await f.Db.SaveChangesAsync(ct);
        var history = await f.Simple.GetRiderPlatformHistoryAsync(f.RiderId, ct);
        Assert.True(history.IsSuccess, history.Error.Code);
        var released = await f.Simple.ReleaseAccountAsync(f.Account.Id,
            new(new(2026, 10, 18), "Ended", "Returned", "AQ=="), ct);
        Assert.True(released.IsSuccess, released.Error.Code);
        Assert.Equal(new DateOnly(2026, 10, 18), released.Value!.EffectiveTo);
        Assert.Equal("Ended", released.Value.Status);
        Assert.True((await f.Db.Employees.SingleAsync(ct)).IsEmployee);
    }

    [Fact]
    public async Task AccountOwnerStillRequiresAnExistingProfile()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        var result = await f.Simple.CreateAccountAsync(new(f.Account.ClientPlatformId, OperatingCity.RiyadhId,
            Guid.NewGuid(), Guid.NewGuid(), "NEW", "NEW", null, "PayPerOrder", "Available", null,
            null, null, null, null, null, null), ct);
        Assert.Equal("platform.account_reference_unavailable", result.Error.Code);
        Assert.Equal("ownerRiderProfileId", result.Error.Field);
        Assert.Single(await f.Db.PlatformRiderAccounts.ToArrayAsync(ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProvisioningDoesNotMakeInactiveEmployeesEligibleForAssignment(bool legacy)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        foreach (var status in Enum.GetValues<EmployeeStatus>().Where(s => s != EmployeeStatus.Active))
        {
            (await f.Db.Employees.SingleAsync(ct)).Status = status;
            await f.Db.SaveChangesAsync(ct);
            Assert.Equal("platform.rider_profile_unavailable", (await f.Assign(legacy, ct)).Error.Code);
            Assert.Empty(await f.Db.RiderClientAssignments.ToArrayAsync(ct));
            Assert.Equal(PlatformRiderAccountStatus.Available, (await f.Db.PlatformRiderAccounts.SingleAsync(ct)).Status);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DeletedEmployeeOrProfileRemainsIneligible(bool legacy, bool deleteEmployee)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        if (deleteEmployee) (await f.Db.Employees.SingleAsync(ct)).IsDeleted = true;
        else (await f.Db.RiderProfiles.SingleAsync(ct)).IsDeleted = true;
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal(deleteEmployee ? "platform.rider_profile_unavailable" : "platform.rider_profile_not_found",
            (await f.Assign(legacy, ct)).Error.Code);
        Assert.Empty(await f.Db.RiderClientAssignments.ToArrayAsync(ct));
    }

    private sealed class Fixture(ApplicationDbContext db, CurrentUser user, PlatformRiderAccount account,
        ClientContract contract, Guid riderId) : IAsyncDisposable
    {
        public ApplicationDbContext Db => db;
        public Guid RiderId => riderId;
        public PlatformRiderAccount Account => account;
        public SimplePlatformService Simple { get; } = new(db, user, TimeProvider.System, null!);

        public async Task<Result> Assign(bool legacy, CancellationToken ct) => legacy
            ? await new PlatformOperationsService(db, user, TimeProvider.System, null!).AssignAccountAsync(
                new(riderId, contract.Id, account.Id, new(2026, 10, 17), "Active", "Account handover", null, null, false, null), ct)
            : await Simple.AssignAccountAsync(account.Id, new(riderId, new(2026, 10, 17), "Account handover", false, null), ct);

        public static async Task<Fixture> Create()
        {
            var ct = TestContext.Current.CancellationToken;
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"EmployeePlatformAssignment_{Guid.NewGuid():N}", options => options.EnableNullChecks(false)).Options);
            var user = new CurrentUser();
            var employee = new Employee { FullNameAr = "موظف نشط", IsEmployee = true, Status = EmployeeStatus.Active };
            var platform = new ClientPlatform { Code = "KEETA", NameAr = "كيتا", NameEn = "Keeta" };
            var contract = new ClientContract { ClientPlatformId = platform.Id, Code = "KEETA-TEST",
                DisplayNameAr = "اختبار", DisplayNameEn = "Test", Status = ClientContractStatus.Active };
            var account = new PlatformRiderAccount { ClientPlatformId = platform.Id, RegisteredEmployeeId = employee.Id,
                Code = "KEETA-1", ExternalAccountId = "external-1", OperatingCityId = OperatingCity.RiyadhId };
            db.AddRange(employee, platform, contract, account);
            await db.SaveChangesAsync(ct);
            await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase($"EmployeePlatformAssignmentIdentity_{Guid.NewGuid():N}").Options);
            var provisioned = await new WorkforceService(db, identity, user).EnsureVehicleProfileAsync(employee.Id, ct);
            Assert.True(provisioned.IsSuccess, provisioned.Error.Code);
            var riderId = provisioned.Value!.RiderProfileId!.Value;
            // Provisioning and assignment normally run in different request contexts.
            db.ChangeTracker.Clear();
            return new(db, user, account, contract, riderId);
        }

        public ValueTask DisposeAsync() => db.DisposeAsync();
    }

    private sealed class CurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => null;
    }
}
