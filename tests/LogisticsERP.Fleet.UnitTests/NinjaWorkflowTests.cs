using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Ninja;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Fact]
    public async Task AssignmentReturnAndReassignmentKeepLockedPasswordAndChangePhoneImmediately()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111", "initial"), ct));
        Assert.True(assigned.Credentials.PasswordChanged); Assert.Null(assigned.Credentials.Notification);
        var deadline = assigned.Credentials.PasswordChangeAllowedAtUtc;
        var listed = Assert.Single(Value(await f.Service.GetAccountsAsync(1, 50, ct)).Items);
        Assert.Equal(assigned.Assignment.Id, listed.CurrentAssignmentId);
        var returned = Value(await f.Service.ReturnAsync(f.Account.Id, new(new(2026, 10, 12), "0502222222", "blocked", "Returned", listed.AssignmentRowVersion!), ct));
        Assert.False(returned.Credentials.PasswordChanged); Assert.Equal(deadline, returned.Credentials.Notification!.ChangeAllowedAtUtc);
        Assert.Equal("0502222222", Assert.Single(f.Db.NinjaAccountDetails).PhoneNumber);
        Assert.Single(f.Db.PlatformAccountCredentialVersions); Assert.Single(f.Db.Notifications);
        Assert.Equal("initial", f.Password()); Assert.Equal(PlatformRiderAccountStatus.Available, f.Account.Status);
        f.Clock.Now = f.Clock.Now.AddDays(1);
        var reassigned = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 13), "0503333333", "blocked-again"), ct));
        Assert.False(reassigned.Credentials.PasswordChanged); Assert.Equal("initial", f.Password());
        Assert.Equal("0503333333", reassigned.Credentials.PhoneNumber);
        f.Clock.Now = deadline!.Value.AddTicks(-1);
        var locked = Value(await f.Service.UpdateCredentialsAsync(f.Account.Id, new("next", "0504444444", "Change", reassigned.Credentials.RowVersion), ct));
        Assert.False(locked.PasswordChanged); Assert.Equal(deadline, locked.PasswordChangeAllowedAtUtc);
        f.Clock.Now = deadline.Value;
        var unlocked = Value(await f.Service.UpdateCredentialsAsync(f.Account.Id, new("next", null, "Change", locked.RowVersion), ct));
        Assert.True(unlocked.PasswordChanged); Assert.Equal("next", f.Password());
        var same = Value(await f.Service.UpdateCredentialsAsync(f.Account.Id, new("next", null, "Same", unlocked.RowVersion), ct));
        Assert.False(same.PasswordChanged); Assert.Equal(unlocked.PasswordChangeAllowedAtUtc, same.PasswordChangeAllowedAtUtc);
    }

    [Fact]
    public async Task UnlockedReturnRequiresDifferentPasswordAndFailedAssignmentDoesNotStageChanges()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var invalid = await f.Service.AssignAsync(f.Account.Id, new(Guid.NewGuid(), new(2026, 10, 12), "0501111111", "unused"), ct);
        Assert.True(invalid.IsFailure); await f.Db.SaveChangesAsync(ct);
        Assert.Empty(f.Db.PlatformAccountCredentialVersions); Assert.Empty(f.Db.NinjaAccountDetails);
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111", "initial"), ct));
        f.Clock.Now = f.Clock.Now.AddDays(3);
        Assert.True((await f.Service.ReturnAsync(f.Account.Id, new(new(2026, 10, 15), "0502222222", null, "Return", assigned.Assignment.RowVersion), ct)).IsFailure);
        Assert.True((await f.Service.ReturnAsync(f.Account.Id, new(new(2026, 10, 15), "0502222222", "initial", "Return", assigned.Assignment.RowVersion), ct)).IsFailure);
        var returned = Value(await f.Service.ReturnAsync(f.Account.Id, new(new(2026, 10, 15), "0502222222", "returned", "Return", assigned.Assignment.RowVersion), ct));
        Assert.True(returned.Credentials.PasswordChanged); Assert.Equal("returned", f.Password());
        Assert.Equal("0502222222", returned.Credentials.PhoneNumber);
    }

    [Fact]
    public async Task SameDayReassignmentProceedsDuringCooldownButAmbiguousDailyReportIsRejected()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111", "initial"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(new(2026, 10, 12), "0502222222", null, "Return", first.Assignment.RowVersion), ct));
        var next = Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, new(2026, 10, 12), "0503333333", "next"), ct));
        Assert.NotNull(next.Credentials.Notification); Assert.False(next.Credentials.PasswordChanged);
        Assert.Equal("initial", f.Password()); Assert.Equal("0503333333", next.Credentials.PhoneNumber);
        var report = Value(await f.Service.ImportAsync(false, new(2026, 10, 12), "x.xlsx", Sheet(false, "272578", 18, 12), ct));
        Assert.Single(report.Issues); Assert.Empty(f.Db.NinjaDailyPerformance);
    }

    [Fact]
    public async Task DailyImportsCreditHistoricalAssignedRiderAndPreserveDeductionSign()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111"), ct));
        var performance = Sheet(false, "272578", 18, 12.90777778m);
        var deductions = Sheet(true, "272578", 0, -58.24m);
        Assert.Equal(1, Value(await f.Service.ImportAsync(false, new(2026, 10, 12), "performance.xlsx", performance, ct)).ImportedRows);
        Assert.Equal(1, Value(await f.Service.ImportAsync(true, new(2026, 10, 12), "deductions.xlsx", deductions, ct)).ImportedRows);
        Assert.Equal(1, Value(await f.Service.ImportAsync(false, new(2026, 10, 12), "again.xlsx", performance, ct)).SkippedRows);
        Assert.Equal(1, Value(await f.Service.ImportAsync(true, new(2026, 10, 12), "again.xlsx", deductions, ct)).SkippedRows);
        var saved = Assert.Single(f.Db.NinjaDailyPerformance); Assert.Equal(f.Rider.Id, saved.RiderProfileId);
        Assert.NotEqual(f.OwnerRider.Id, saved.RiderProfileId); Assert.Equal(assigned.Assignment.Id, saved.RiderClientAssignmentId);
        Assert.Equal(12.90777778m, saved.TotalWorkingHours); Assert.Equal(-58.24m, Assert.Single(f.Db.NinjaDailyDeductions).TotalDeductions);
        f.Clock.Now = f.Clock.Now.AddDays(1);
        Value(await f.Service.ReturnAsync(f.Account.Id, new(new(2026, 10, 12), "0502222222", "return", "Return", assigned.Assignment.RowVersion), ct));
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, new(2026, 10, 13), "0503333333"), ct));
        Value(await f.Service.ImportAsync(false, new(2026, 10, 13), "performance.xlsx", performance, ct));
        var history = Value(await f.Service.GetPerformanceAsync(new(2026, 10, 12), new(2026, 10, 13), null, f.Rider.Id, 1, 50, ct));
        Assert.Equal(new DateOnly(2026, 10, 12), Assert.Single(history.Items).ReportDate);
        Assert.Equal(1, Value(await f.Service.ImportAsync(false, new(2026, 10, 12), "again.xlsx", performance, ct)).SkippedRows);
        Assert.Single(Value(await f.Service.GetDeductionsAsync(new(2026, 10, 12), new(2026, 10, 13), f.Account.Id, null, 1, 50, ct)).Items);
    }

    [Fact]
    public async Task InvalidUnknownOverlappingAndConflictingRowsLeaveWholeImportUnchanged()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var missing = Value(await f.Service.ImportAsync(false, new(2026, 10, 12), "x.xlsx", Sheet(false, "272578", 18, 12), ct));
        Assert.Single(missing.Issues); Assert.Empty(f.Db.NinjaDailyImports);
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111"), ct));
        var mixed = Sheet(false, "272578", 18, 12, "999");
        Assert.Single(Value(await f.Service.ImportAsync(false, new(2026, 10, 12), "x.xlsx", mixed, ct)).Issues);
        await f.Db.SaveChangesAsync(ct); Assert.Empty(f.Db.NinjaDailyPerformance); Assert.Empty(f.Db.NinjaDailyImports);
        Assert.NotEmpty(NinjaSpreadsheetParser.Parse(Sheet(false, "272578", 18, 25), false, ct).Issues);
        Assert.NotEmpty(NinjaSpreadsheetParser.Parse(Sheet(false, "272578", 18, 12, "272578"), false, ct).Issues);
        Value(await f.Service.ImportAsync(false, new(2026, 10, 12), "x.xlsx", Sheet(false, "272578", 18, 12), ct));
        Assert.Single(Value(await f.Service.ImportAsync(false, new(2026, 10, 12), "x.xlsx", Sheet(false, "272578", 19, 12), ct)).Issues);
        Assert.Equal(18, Assert.Single(f.Db.NinjaDailyPerformance).TotalOrders);
        var contract = await f.Db.ClientContracts.FirstAsync(ct);
        f.Db.RiderClientAssignments.Add(new() { PlatformRiderAccountId = f.Account.Id, ClientContractId = contract.Id,
            RiderProfileId = f.OwnerRider.Id, EffectiveFrom = new(2026, 10, 12), EffectiveTo = new(2026, 10, 12), Status = RiderAssignmentStatus.Ended });
        await f.Db.SaveChangesAsync(ct);
        Assert.Single(Value(await f.Service.ImportAsync(true, new(2026, 10, 12), "x.xlsx", Sheet(true, "272578", 0, -6), ct)).Issues);
        Assert.Empty(f.Db.NinjaDailyDeductions);
    }

    [Fact]
    public async Task PermissionsAndGenericRoutesCannotBypassNinjaWorkflow()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        Assert.Equal("ninja.use_workflow", (await f.Simple.AssignAccountAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), null, false, null), ct)).Error.Code);
        Assert.Equal("ninja.use_workflow", (await f.Simple.RotateCredentialAsync(f.Account.Id, new("x", "reason"), ct)).Error.Code);
        var legacy = new PlatformOperationsService(f.Db, f.User, f.Clock, f.Protector);
        Assert.Equal("ninja.use_workflow", (await legacy.RotateCredentialAsync(f.Account.Id, new("x", "reason"), ct)).Error.Code);
        f.Checker.Denied = PermissionKeys.Ninja.CredentialsRotate;
        Assert.True((await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111", "x"), ct)).IsFailure);
        Assert.Empty(f.Db.PlatformAccountCredentialVersions);
        f.Checker.Denied = null;
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111", "x"), ct));
        Assert.Equal("ninja.use_workflow", (await f.Simple.ReleaseAccountAsync(f.Account.Id, new(new(2026, 10, 12), "Ended", "Return", assigned.Assignment.RowVersion), ct)).Error.Code);
        Assert.Equal("ninja.use_workflow", (await legacy.CloseAssignmentAsync(assigned.Assignment.Id, new(new(2026, 10, 12), "Ended", "Return", assigned.Assignment.RowVersion), ct)).Error.Code);
        f.Checker.Denied = PermissionKeys.Ninja.PerformanceImport;
        Assert.True((await f.Service.ImportAsync(false, new(2026, 10, 12), "x.xlsx", Sheet(false, "272578", 18, 12), ct)).IsFailure);
        Assert.Empty(f.Db.NinjaDailyPerformance);
        f.Checker.Denied = PermissionKeys.Ninja.Read;
        Assert.True((await f.Service.GetAccountsAsync(1, 50, ct)).IsFailure);
    }

    [Fact]
    public void ProvidedWorkbooksParseWithoutErrors()
    {
        var performance = Environment.GetEnvironmentVariable("NINJA_PERFORMANCE_SAMPLE");
        var deductions = Environment.GetEnvironmentVariable("NINJA_DEDUCTIONS_SAMPLE");
        Assert.SkipUnless(performance is not null && deductions is not null, "Set both Ninja sample paths for read-only workbook validation.");
        static byte[] ReadShared(string path)
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var content = new MemoryStream(); file.CopyTo(content); return content.ToArray();
        }
        var p = NinjaSpreadsheetParser.Parse(ReadShared(performance!), false, TestContext.Current.CancellationToken);
        var d = NinjaSpreadsheetParser.Parse(ReadShared(deductions!), true, TestContext.Current.CancellationToken);
        Assert.Empty(p.Issues); Assert.Equal(72, p.Rows.Count); Assert.Equal(18, p.Rows[0].TotalOrders);
        Assert.Equal(12.90777778m, p.Rows[0].WorkingHours);
        Assert.Empty(d.Issues); Assert.Equal(10, d.Rows.Count); Assert.Equal(-6, d.Rows[0].Deductions);
    }

    [Fact]
    public async Task SqlServerEnforcesRowversionUniquenessAndAtomicLifecycle()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("NINJA_SQL_TEST_SERVER") is not null,
            "Set NINJA_SQL_TEST_SERVER to a working local SQL Server for isolated database verification.");
        await using var f = await Fixture.Create(true); var ct = TestContext.Current.CancellationToken;
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, new(2026, 10, 12), "0501111111", "initial"), ct));
        Value(await f.Service.GetAccountsAsync(1, 50, ct));
        Value(await f.Service.ImportAsync(false, new(2026, 10, 12), "x.xlsx", Sheet(false, "272578", 18, 12), ct));
        Value(await f.Service.ImportAsync(true, new(2026, 10, 12), "x.xlsx", Sheet(true, "272578", 0, -6), ct));
        Value(await f.Service.GetPerformanceAsync(new(2026, 10, 12), new(2026, 10, 12), null, null, 1, 50, ct));
        Value(await f.Service.GetDeductionsAsync(new(2026, 10, 12), new(2026, 10, 12), null, null, 1, 50, ct));
        // Load a second context before another writer changes the account.
        await using var second = f.OtherContext(); var other = f.CreateService(second);
        await second.PlatformRiderAccounts.SingleAsync(a => a.Id == f.Account.Id, ct);
        var changed = Value(await f.Service.UpdateCredentialsAsync(f.Account.Id, new(null, "0502222222", "Phone", assigned.Credentials.RowVersion), ct));
        Assert.NotEqual(assigned.Credentials.RowVersion, changed.RowVersion);
        var conflict = await other.ReturnAsync(f.Account.Id, new(new(2026, 10, 12), "0503333333", "blocked", "Return", assigned.Assignment.RowVersion), ct);
        Assert.True(conflict.IsFailure);
        f.Db.ChangeTracker.Clear();
        Assert.Null((await f.Db.RiderClientAssignments.SingleAsync(ct)).EffectiveTo);
        Assert.Equal("0502222222", (await f.Db.NinjaAccountDetails.SingleAsync(ct)).PhoneNumber);
        Assert.Empty(await f.Db.Notifications.ToArrayAsync(ct));
        Assert.Single(await f.Db.PlatformAccountCredentialVersions.ToArrayAsync(ct));
    }

    private static T Value<T>(Result<T> result) { Assert.True(result.IsSuccess, result.Error.Description); return result.Value!; }
    private static byte[] Sheet(bool deductions, string captain, decimal first, decimal second, string? extra = null)
    {
        using var book = new XLWorkbook(); var s = book.AddWorksheet("Report");
        string[] headers = deductions ? ["CAPTAIN ID ", "labels.TotalCollectedAmountAtDelivery", "TOTAL DEDUCTIONS"] : ["Captain ID", "Total Orders", "Total Working Hours"];
        for (var i = 0; i < headers.Length; i++) s.Cell(1, i + 1).Value = headers[i];
        s.Cell(2, 1).Value = captain; s.Cell(2, 2).Value = (double)first; s.Cell(2, 3).Value = (double)second;
        if (extra is not null) { s.Row(2).CopyTo(s.Row(3)); s.Cell(3, 1).Value = extra; }
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }
    private sealed class CurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid(); public Guid? SessionId => null;
        public long? AuthorizationVersion => 1; public string? CorrelationId => null;
    }
    private sealed class Checker : IPermissionChecker
    {
        public string? Denied { get; set; }
        public HashSet<string>? Granted { get; set; }
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(permissionKey != Denied && (Granted is null || Granted.Contains(permissionKey)));
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class InMemoryRowversionInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            foreach (var e in eventData.Context!.ChangeTracker.Entries<AuditableEntity>().Where(e => e.State == EntityState.Added))
                e.Entity.RowVersion = [1];
            return ValueTask.FromResult(result);
        }
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public required ApplicationDbContext Db { get; init; }
        public required CurrentUser User { get; init; }
        public required Checker Checker { get; init; }
        public required Clock Clock { get; init; }
        public required PlatformCredentialProtector Protector { get; init; }
        public required SimplePlatformService Simple { get; init; }
        public required NinjaService Service { get; init; }
        public required PlatformRiderAccount Account { get; init; }
        public required RiderProfile Rider { get; init; }
        public required RiderProfile OwnerRider { get; init; }
        public string? Connection { get; init; }
        public static async Task<Fixture> Create(bool sql = false)
        {
            var user = new CurrentUser(); var clock = new Clock(); var checker = new Checker();
            var server = Environment.GetEnvironmentVariable("NINJA_SQL_TEST_SERVER");
            var connection = sql ? $"Server={server};Database=NinjaTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True" : null;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (connection is not null) options.UseSqlServer(connection);
            else options.UseInMemoryDatabase($"NinjaTests_{Guid.NewGuid():N}", m => m.EnableNullChecks(false)).AddInterceptors(new InMemoryRowversionInterceptor());
            options.AddInterceptors(new ApplicationPersistenceInterceptor(user, clock));
            var db = new ApplicationDbContext(options.Options, clock);
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var p = new ClientPlatform { Code = "NINJA", NameAr = "نينجا", NameEn = "Ninja" };
            var owner = new Employee { FullNameAr = "صاحب الحساب", Status = EmployeeStatus.Active };
            var employee = new Employee { FullNameAr = "المندوب", Status = EmployeeStatus.Active };
            var rider = new RiderProfile { EmployeeId = employee.Id }; var ownerRider = new RiderProfile { EmployeeId = owner.Id };
            var account = new PlatformRiderAccount { ClientPlatformId = p.Id, RegisteredEmployeeId = owner.Id,
                OperatingCityId = OperatingCity.RiyadhId, Code = "NINJA-1", ExternalAccountId = "272578" };
            db.AddRange(p, owner, employee, rider, ownerRider, account); await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            var protector = new PlatformCredentialProtector(() => new byte[32]); var simple = new SimplePlatformService(db, user, clock, protector);
            return new() { Db = db, User = user, Clock = clock, Checker = checker, Protector = protector, Simple = simple,
                Service = new(db, user, checker, clock, protector, simple), Account = account, Rider = rider, OwnerRider = ownerRider, Connection = connection };
        }
        public string Password()
        {
            var v = Db.PlatformAccountCredentialVersions.OrderByDescending(v => v.KeyVersion).First();
            return Protector.Unprotect(new(v.Ciphertext, v.Nonce, v.AuthenticationTag));
        }
        public ApplicationDbContext OtherContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(Connection!)
            .AddInterceptors(new ApplicationPersistenceInterceptor(User, Clock)).Options, Clock);
        public NinjaService CreateService(ApplicationDbContext db) => new(db, User, Checker, Clock, Protector, new(db, User, Clock, Protector));
        public async ValueTask DisposeAsync()
        {
            if (Connection is not null) await Db.Database.EnsureDeletedAsync();
            await Db.DisposeAsync();
        }
    }
}
