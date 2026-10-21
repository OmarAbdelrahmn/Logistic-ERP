using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Jahez;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.SystemServices;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    private static DateTimeOffset At(int day, int hour = 8) => new(2026, 10, day, hour, 0, 0, TimeSpan.FromHours(3));
    private static T Success<T>(Result<T> result) { Assert.True(result.IsSuccess, result.Error.Description); return result.Value!; }

    internal static byte[] Transactions(string driver, string date, decimal delivery, decimal cash, decimal net, decimal adjustment, bool duplicate = false)
    {
        using var w = new XLWorkbook(); var s = w.AddWorksheet("SDP_Report");
        string[] headers = ["Driver ID", "Date", "Delivery Price", "Cash Amount", "Net Amount", "Driver Adjustment"];
        for (var i = 0; i < headers.Length; i++) s.Cell(1, i + 1).Value = headers[i];
        for (var r = 2; r <= (duplicate ? 3 : 2); r++)
        {
            s.Cell(r, 1).Value = driver; s.Cell(r, 2).Value = date; s.Cell(r, 3).Value = delivery;
            s.Cell(r, 4).Value = cash; s.Cell(r, 5).Value = net; s.Cell(r, 6).Value = adjustment;
        }
        using var stream = new MemoryStream(); w.SaveAs(stream); return stream.ToArray();
    }

    private static byte[] Dispatches(string driver, string date, int count)
    {
        using var w = new XLWorkbook(); var s = w.AddWorksheet("Delivery Insights Report");
        string[] headers = ["Driver ID", "From", "To", "Number Of Dispatches"];
        for (var i = 0; i < headers.Length; i++) s.Cell(1, i + 1).Value = headers[i];
        s.Cell(2, 1).Value = driver; s.Cell(2, 2).Value = date; s.Cell(2, 3).Value = date; s.Cell(2, 4).Value = count;
        using var stream = new MemoryStream(); w.SaveAs(stream); return stream.ToArray();
    }

    private sealed class Fixture(ApplicationDbContext db, MutableUser user, TestPermissions permissions, MutableClock clock, bool sql) : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; } = db;
        public MutableUser User { get; } = user;
        public TestPermissions Permissions { get; } = permissions;
        public MutableClock Clock { get; } = clock;
        public JahezWorkflowService Service { get; } = new(db, user, permissions, clock);
        public Guid PlatformId { get; private set; }
        public Guid RiderId { get; private set; }
        public Guid OtherRiderId { get; private set; }

        public static async Task<Fixture> Create(bool sql = false, bool createCurrentSchema = false)
        {
            var ct = TestContext.Current.CancellationToken;
            var name = $"LogisticsJahezTest_{Guid.NewGuid():N}";
            var user = new MutableUser(); var clock = new MutableClock { UtcNow = At(6, 23).ToUniversalTime() };
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (sql)
            {
                var testConnection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(
                    Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TEST_CONNECTION")
                    ?? "Server=(localdb)\\mssqllocaldb;Trusted_Connection=True;TrustServerCertificate=True");
                // Never migrate/drop the database named in an environment-provided connection string.
                testConnection.InitialCatalog = name;
                builder.UseSqlServer(testConnection.ConnectionString,
                    o => o.MigrationsHistoryTable("__ApplicationMigrationsHistory", "migration"));
            }
            else builder.UseInMemoryDatabase(name, o => o.EnableNullChecks(false));
            builder.AddInterceptors(new ApplicationPersistenceInterceptor(user, clock));
            var f = new Fixture(new ApplicationDbContext(builder.Options, clock), user, new TestPermissions(), clock, sql);
            if (sql && createCurrentSchema) await f.Db.Database.EnsureCreatedAsync(ct);
            else if (sql) { await f.Db.Database.MigrateAsync(ct); Assert.False(f.Db.Database.HasPendingModelChanges()); }
            else await f.Db.Database.EnsureCreatedAsync(ct);
            var platform = await f.Db.ClientPlatforms.SingleOrDefaultAsync(x => x.Code == "JAHEZ", ct);
            if (platform is null) { platform = new ClientPlatform { Code = "JAHEZ", NameAr = "جاهز", NameEn = "Jahez", SupportedPaymentModels = SupportedPlatformPaymentModels.PayPerOrder }; f.Db.Add(platform); }
            f.PlatformId = platform.Id;
            f.RiderId = await f.AddRider("المندوب الفعلي"); f.OtherRiderId = await f.AddRider("المندوب التالي");
            await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
            return f;
        }

        private async Task<Guid> AddRider(string name)
        {
            var e = new Employee { FullNameAr = name, Status = EmployeeStatus.Active, EngagementType = EmployeeRelationshipType.OutsideRider };
            var r = new RiderProfile { EmployeeId = e.Id };
            Db.Add(e); Db.Add(r); await Db.SaveChangesAsync(TestContext.Current.CancellationToken); return r.Id;
        }

        public async Task<PlatformRiderAccount> Account(string external)
        {
            var owner = await AddRider("صاحب الحساب");
            var ownerEmployee = await Db.RiderProfiles.Where(x => x.Id == owner).Select(x => x.EmployeeId).SingleAsync(TestContext.Current.CancellationToken);
            var sponsor = await Db.Sponsors.FirstAsync(TestContext.Current.CancellationToken);
            var city = await Db.OperatingCities.FirstAsync(TestContext.Current.CancellationToken);
            var a = new PlatformRiderAccount { Code = $"J-{external}", ExternalAccountId = external, ClientPlatformId = PlatformId,
                RegisteredEmployeeId = ownerEmployee, SponsorId = sponsor.Id,
                OperatingCityId = city.Id, Status = PlatformRiderAccountStatus.Available };
            Db.Add(a); await Db.SaveChangesAsync(TestContext.Current.CancellationToken); Db.ChangeTracker.Clear(); return a;
        }

        public async ValueTask DisposeAsync()
        {
            if (sql) await Db.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
            await Db.DisposeAsync();
        }
    }

    private sealed class UnusedProtector : IPlatformCredentialProtector
    {
        public ProtectedPlatformCredential Protect(string value) => throw new NotSupportedException();
        public string Unprotect(ProtectedPlatformCredential value) => throw new NotSupportedException();
    }

    private sealed class MutableUser : ICurrentUser
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "jahez-tests";
    }
    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; }
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
    private sealed class TestPermissions : IPermissionChecker
    {
        public string? Denied { get; set; }
        public Guid? RequiredScope { get; set; }
        public List<PermissionScope> Scopes { get; } = [];
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default)
        {
            if (scope is not null) { lock (Scopes) Scopes.Add(scope); }
            return Task.FromResult(permissionKey != Denied && (!RequiredScope.HasValue || scope?.TargetId == RequiredScope.Value));
        }
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
}
