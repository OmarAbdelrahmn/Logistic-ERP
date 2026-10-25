using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class PlatformAccountDayUsageSqlTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveRunsInsideRetryStrategyAndReloadsBeforeReplay(bool transientFailure)
    {
        var ct = TestContext.Current.CancellationToken;
        var guard = new RetryGuard { FailOnce = transientFailure };
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"day-usage-retry-{Guid.NewGuid():N}", options => options.EnableNullChecks(false))
            .ReplaceService<IExecutionStrategyFactory, RetryFactory>()
            .AddInterceptors(guard).Options);
        var platform = new ClientPlatform { Code = "JAHEZ" };
        var account = new PlatformRiderAccount { ClientPlatformId = platform.Id };
        var date = new DateOnly(2026, 10, 21);
        var first = new RiderClientAssignment { PlatformRiderAccountId = account.Id, RiderProfileId = Guid.NewGuid(),
            EffectiveFrom = date.AddDays(-1), EffectiveTo = date, Status = RiderAssignmentStatus.Ended };
        var last = new RiderClientAssignment { PlatformRiderAccountId = account.Id, RiderProfileId = Guid.NewGuid(),
            EffectiveFrom = date, Status = RiderAssignmentStatus.Active };
        db.AddRange(platform, account, first, last);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        guard.Armed = true;
        var service = new PlatformAccountDayUsageService(db, new User(), new Permissions(), TimeProvider.System);
        var result = await service.UpdateAsync(account.Id, date, new([
            new(first.Id, new(OrdersCount: 0)), new(last.Id, new(OrdersCount: 12))], "Shared day figures"), ct);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(transientFailure ? 2 : 1, guard.Attempts);
        db.ChangeTracker.Clear();
        var saved = await db.Set<PlatformAccountDayUsage>().OrderBy(a => a.Sequence).ToArrayAsync(ct);
        Assert.Equal(new int?[] { 1, 2 }, saved.Select(a => a.Sequence));
        Assert.Equal(new int?[] { 0, 12 }, saved.Select(a => a.OrdersCount));
        Assert.Equal(2, saved.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedDaySaveSupportsSqlRetriesAndPreservesConcurrency(bool transientFailure)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("LOGISTICS_DAY_USAGE_SQL_TESTS") == "1",
            "Set LOGISTICS_DAY_USAGE_SQL_TESTS=1 on Windows with SQL LocalDB to run isolated day usage write tests.");
        var ct = TestContext.Current.CancellationToken;
        var failure = new FailSecondSave();
        // Application/hosted database configuration is never used by this test.
        var database = $"LogisticsDayUsageTest_{Guid.NewGuid():N}";
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=True;TrustServerCertificate=True",
                sql => sql.EnableRetryOnFailure(2, TimeSpan.Zero, null))
            .AddInterceptors(failure).Options);
        var createdDatabase = false;
        try
        {
            await db.Database.EnsureCreatedAsync(ct);
            createdDatabase = true;
            var platform = new ClientPlatform { Code = "JAHEZ", NameAr = "Test", NameEn = "Test" };
            var city = new GlobalCity { Code = "TEST", NameAr = "Test", NameEn = "Test" };
            var operatingCity = new OperatingCity { GlobalCityId = city.Id, EnabledFrom = new(2026, 1, 1) };
            var account = new PlatformRiderAccount { ClientPlatformId = platform.Id, OperatingCityId = operatingCity.Id,
                Code = "JHZ-491298", ExternalAccountId = "491298" };
            var contract = new ClientContract { ClientPlatformId = platform.Id, Code = "TEST", DisplayNameAr = "Test", DisplayNameEn = "Test" };
            var firstEmployee = new Employee { FullNameAr = "First" };
            var lastEmployee = new Employee { FullNameAr = "Last" };
            var firstRider = new RiderProfile { EmployeeId = firstEmployee.Id };
            var lastRider = new RiderProfile { EmployeeId = lastEmployee.Id };
            var date = new DateOnly(2026, 10, 21);
            var first = new RiderClientAssignment { PlatformRiderAccountId = account.Id, ClientContractId = contract.Id,
                RiderProfileId = firstRider.Id, EffectiveFrom = date.AddDays(-1), EffectiveTo = date, Status = RiderAssignmentStatus.Ended };
            var last = new RiderClientAssignment { PlatformRiderAccountId = account.Id, ClientContractId = contract.Id,
                RiderProfileId = lastRider.Id, EffectiveFrom = date, Status = RiderAssignmentStatus.Active };
            db.AddRange(platform, city, operatingCity, account, contract, firstEmployee, lastEmployee, firstRider, lastRider, first, last);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
            var service = new PlatformAccountDayUsageService(db, new User(), new Permissions(), TimeProvider.System);
            var created = await service.UpdateAsync(account.Id, date, new([
                new(first.Id, new(OrdersCount: 0)), new(last.Id, new(OrdersCount: 12))], "Initial figures"), ct);
            Assert.True(created.IsSuccess, created.Error.Description);
            Assert.All(created.Value!.Assignments, a => Assert.False(string.IsNullOrEmpty(a.RowVersion)));
            var changes = created.Value.Assignments.Select((a, i) => new PlatformAccountDayUsageUpdate(a.AssignmentId,
                new(OrdersCount: i == 0 ? 2 : 10), a.RowVersion)).ToArray();
            failure.RemainingSaves = transientFailure ? 2 : 0;
            var updated = await service.UpdateAsync(account.Id, date, new(changes, "Correct figures"), ct);
            Assert.True(updated.IsSuccess, updated.Error.Description);
            Assert.Equal(transientFailure ? 1 : 0, failure.Failures);
            Assert.Null(db.Database.CurrentTransaction);
            db.ChangeTracker.Clear();
            var saved = await db.Set<PlatformAccountDayUsage>().AsNoTracking().OrderBy(a => a.Sequence).ToArrayAsync(ct);
            Assert.Equal(new int?[] { 1, 2 }, saved.Select(a => a.Sequence));
            Assert.Equal(new int?[] { 2, 10 }, saved.Select(a => a.OrdersCount));
            Assert.Equal(2, saved.Length);
            var stale = await service.UpdateAsync(account.Id, date, new(changes, "Stale edit"), ct);
            Assert.Equal(HrErrors.ConcurrencyConflict, stale.Error);
        }
        finally
        {
            if (createdDatabase) await db.Database.EnsureDeletedAsync(CancellationToken.None);
        }
    }

    private sealed class RetryFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new RetryStrategy(dependencies);
    }

    private sealed class RetryStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is TimeoutException;
    }

    private sealed class RetryGuard : SaveChangesInterceptor
    {
        public bool Armed { get; set; }
        public bool FailOnce { get; set; }
        public int Attempts { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!Armed) return ValueTask.FromResult(result);
            Assert.NotNull(ExecutionStrategy.Current);
            Attempts++;
            if (FailOnce)
            {
                FailOnce = false;
                throw new TimeoutException("Injected transient failure before persistence.");
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FailSecondSave : SaveChangesInterceptor
    {
        public int RemainingSaves { get; set; }
        public int Failures { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (RemainingSaves > 0 && --RemainingSaves == 0)
            {
                Failures++;
                throw new TimeoutException("Injected failure after sequence slots were cleared; retry must reload rolled-back rowversions.");
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class User : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "day-usage-sql-tests";
    }

    private sealed class Permissions : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
}
