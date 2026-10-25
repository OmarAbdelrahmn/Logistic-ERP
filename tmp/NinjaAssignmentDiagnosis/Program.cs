using System.Text.Json;
using System.Data.Common;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Infrastructure.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Reflection;

if (args.Contains("--key-only"))
{
    var settings = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath("tmp/ninja-assignment-diagnostic-20261024/appsettings.json")).Build();
    var resolve = typeof(LogisticsERP.Infrastructure.DependencyInjection).GetMethod("ResolveSensitiveDataKey", BindingFlags.NonPublic | BindingFlags.Static)!;
    var configuredProtector = new PlatformCredentialProtector(() => (byte[])resolve.Invoke(null, [settings, false])!);
    try { configuredProtector.Protect("diagnostic-placeholder"); Console.WriteLine("Hosted file settings support credential encryption."); }
    catch (TargetInvocationException exception) { Console.WriteLine(exception.InnerException!.GetType().Name + ": " + exception.InnerException.Message); Environment.ExitCode = 1; }
    return;
}

// Diagnostic reads only. Reject every EF write and never invoke a lifecycle mutation.
using var json = JsonDocument.Parse(File.ReadAllText("src/LogisticsERP.Api/appsettings.json"));
var connection = json.RootElement.GetProperty("ConnectionStrings").GetProperty("LogisticsDatabase").GetString()!;
await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(connection, options => options.EnableRetryOnFailure()).AddInterceptors(new RejectWrites()).Options);
var accountId = Guid.Parse("01a13935-9d89-753e-a5b3-3afecfba099c");
var date = new DateOnly(2026, 10, 1);
try
{
    Console.WriteLine("Read account and owner");
    var account = await db.PlatformRiderAccounts.SingleAsync(a => a.Id == accountId);
    var owner = await (from employee in db.Employees.AsNoTracking()
        join rider in db.RiderProfiles.AsNoTracking() on employee.Id equals rider.EmployeeId
        where employee.Id == account.RegisteredEmployeeId select rider.Id).SingleAsync();
    Console.WriteLine("Read assignment start validation");
    var allocation = new PlatformAccountDayAllocationService(db);
    var validation = await allocation.ValidateStart(accountId, date, null, null, default);
    Console.WriteLine($"Start validation success: {validation.IsSuccess}");
    if (validation.IsFailure) Console.WriteLine(validation.Error.Description);
    Console.WriteLine("Read day allocation queries with an unsaved assignment");
    var assignment = new RiderClientAssignment { PlatformRiderAccountId = accountId, RiderProfileId = owner,
        EffectiveFrom = date, Status = RiderAssignmentStatus.Active };
    db.RiderClientAssignments.Add(assignment);
    var stage = await allocation.Stage(accountId, assignment.Id, date, null, "Diagnostic; never persisted", true, default);
    Console.WriteLine($"Stage success: {stage.IsSuccess}. No SaveChanges was called.");
    db.ChangeTracker.Clear();
    Console.WriteLine("Run assignment planning with database commands blocked before every write");
    var actual = await (from employee in db.Employees join rider in db.RiderProfiles on employee.Id equals rider.EmployeeId
        where employee.IqamaNo == "2642786293" select rider.Id).SingleAsync();
    var user = new User();
    var protector = new PlatformCredentialProtector(() => new byte[32]);
    var simple = new SimplePlatformService(db, user, TimeProvider.System, protector);
    var ninja = new NinjaService(db, user, new Permissions(), TimeProvider.System, protector, simple);
    var result = await ninja.AssignAsync(accountId, new(actual, date, "0545038175", "diagnostic-placeholder",
        "Diagnostic; never persisted", true, "Diagnostic; never persisted"));
    Console.WriteLine($"Lifecycle result: {result.IsSuccess}, {result.Error.Code}, {result.Error.Description}");
}
catch (Exception exception) { Console.WriteLine(exception); Environment.ExitCode = 1; }
finally { db.ChangeTracker.Clear(); }

sealed class RejectWrites : DbCommandInterceptor
{
    private static void Guard(DbCommand command)
    {
        var text = command.CommandText.TrimStart();
        if (text.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            || text.Contains("sys.sp_getapplock", StringComparison.Ordinal)
            || text.StartsWith("EXEC sys.sp_releaseapplock", StringComparison.Ordinal)) return;
        throw new InvalidOperationException("DIAGNOSTIC BLOCKED DATABASE WRITE before execution.");
    }
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data,
        InterceptionResult<DbDataReader> result, CancellationToken ct = default) { Guard(command); return ValueTask.FromResult(result); }
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData data,
        InterceptionResult<int> result, CancellationToken ct = default) { Guard(command); return ValueTask.FromResult(result); }
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData data,
        InterceptionResult<object> result, CancellationToken ct = default) { Guard(command); return ValueTask.FromResult(result); }
}

sealed class User : ICurrentUser
{
    public Guid? UserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
    public Guid? SessionId => null;
    public long? AuthorizationVersion => 1;
    public string? CorrelationId => "read-only-ninja-diagnostic";
}
sealed class Permissions : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(Guid userId, long version, string key, PermissionScope? scope = null,
        CancellationToken ct = default) => Task.FromResult(true);
    public void InvalidateUser(Guid userId, long version) { }
}
