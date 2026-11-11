#pragma warning disable xUnit1051
using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Chefz;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Hunger;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class PlatformPerformanceDeletionTests
{
    private static readonly DateOnly Day = new(2026, 11, 1);
    private static T Value<T>(Result<T> result) { Assert.True(result.IsSuccess, result.Error.Description); return result.Value!; }

    [Theory]
    [InlineData("HUNGER", false)] [InlineData("HUNGER", true)]
    [InlineData("CHEFZ", false)] [InlineData("CHEFZ", true)]
    [InlineData("JAHEZ", false)] [InlineData("JAHEZ", true)]
    public async Task DeletionScopesDayAndSponsorAndSupportsSameFileRecovery(string platform, bool sponsorOnly)
    {
        await using var f = await Fixture.Create(platform);
        var ct = TestContext.Current.CancellationToken;
        var original = await f.Import(Day);
        await f.Import(Day.AddDays(1));
        var expected = sponsorOnly ? 1 : 2;
        Guid? sponsor = sponsorOnly ? f.Accounts[0].SponsorId : null;
        Assert.Equal(expected, Value(await f.Delete(Day, sponsor)));
        Assert.Equal(2 - expected, await f.Count(Day));
        Assert.Equal(2, await f.Count(Day.AddDays(1)));
        Assert.Equal(0, Value(await f.Delete(Day, sponsor)));
        Assert.Equal(2, await f.Db.AuditEntries.CountAsync(a => a.EntityType.EndsWith("PerformanceDay"), ct));
        f.Db.ChangeTracker.Clear();
        await f.Import(Day, original);
        Assert.Equal(2, await f.Count(Day));
        Assert.Equal(2, await f.Count(Day.AddDays(1)));
        if (platform == "HUNGER")
        {
            Assert.Equal(expected, await f.Db.Set<HungerDailyOperation>().IgnoreQueryFilters().CountAsync(o => o.IsDeleted, ct));
            Assert.Equal(4, await f.Db.Set<HungerPotentialFine>().CountAsync(ct));
        }
        if (platform == "CHEFZ") Assert.Equal(2, await f.Db.ChefzPerformanceImports.CountAsync(ct));
        if (platform == "JAHEZ") Assert.Equal(2, await f.Db.Set<JahezImportBatch>().CountAsync(ct));
    }

    [Theory]
    [InlineData("HUNGER")] [InlineData("CHEFZ")] [InlineData("JAHEZ")]
    public async Task DeletionRequiresItsPermissionAndValidScopeAndReason(string platform)
    {
        await using var f = await Fixture.Create(platform);
        f.Checker.Denied = f.Permission;
        Assert.True((await f.Delete(Day, null)).IsFailure);
        f.Checker.Denied = null;
        Assert.True((await f.Delete(default, null)).IsFailure);
        Assert.True((await f.Delete(Day.AddDays(10), null)).IsFailure);
        Assert.True((await f.Delete(Day, Guid.Empty)).IsFailure);
        Assert.True((await f.Delete(Day, Guid.NewGuid())).IsFailure);
        Assert.True((await f.Delete(Day, null, " ")).IsFailure);
        Assert.True((await f.Delete(Day, null, new string('x', 1001))).IsFailure);
    }

    [Theory]
    [InlineData("HUNGER")] [InlineData("CHEFZ")] [InlineData("JAHEZ")]
    public async Task DeletionRollsBackWhenAuditFails(string platform)
    {
        await using var f = await Fixture.Create(platform);
        await f.Import(Day);
        f.Fault.Enabled = true;
        Assert.True((await f.Delete(Day, null)).IsFailure);
        f.Fault.Enabled = false;
        await f.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, await f.Count(Day));
        Assert.False(await f.Db.AuditEntries.AnyAsync(a => a.EntityType == "HungerPerformanceDay"
            || a.EntityType == "ChefzPerformanceDay" || a.EntityType == "JahezPerformanceDay", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HungerReviewedFinePreventsDeletingAnyPartOfTheSelectedDay()
    {
        await using var f = await Fixture.Create("HUNGER");
        await f.Import(Day);
        var fine = await f.Db.Set<HungerPotentialFine>().FirstAsync(TestContext.Current.CancellationToken);
        fine.Status = "Waived"; fine.ResolutionEvidence = "Reviewed";
        await f.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.True((await f.Delete(Day, null)).IsFailure);
        Assert.Equal(2, await f.Count(Day));
    }

    [Fact]
    public async Task JahezRecoveryUsesCorrectedRiderAndRetainsSurvivingSponsorAttribution()
    {
        await using var f = await Fixture.Create("JAHEZ");
        var content = await f.Import(Day);
        var account = f.Accounts[0];
        var prior = await f.Db.JahezAccountSessions.AsNoTracking().SingleAsync(s => s.PlatformRiderAccountId == account.Id);
        var other = await f.Db.JahezAccountSessions.AsNoTracking().SingleAsync(s => s.PlatformRiderAccountId == f.Accounts[1].Id);
        Assert.Equal(1, Value(await f.Delete(Day, account.SponsorId)));
        f.Db.ChangeTracker.Clear();
        var oldSession = await f.Db.JahezAccountSessions.SingleAsync(s => s.Id == prior.Id);
        oldSession.EndedAtUtc = new(2026, 10, 31, 21, 0, 0, TimeSpan.Zero);
        var oldAssignment = await f.Db.RiderClientAssignments.SingleAsync(a => a.Id == oldSession.RiderClientAssignmentId);
        oldAssignment.EffectiveTo = Day.AddDays(-1); oldAssignment.Status = RiderAssignmentStatus.Ended;
        var employee = new Employee { FullNameAr = "Correct rider" };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var assignment = new RiderClientAssignment { PlatformRiderAccountId = account.Id, ClientContractId = oldAssignment.ClientContractId,
            RiderProfileId = rider.Id, Status = RiderAssignmentStatus.Active, EffectiveFrom = Day };
        var session = new JahezAccountSession { PlatformRiderAccountId = account.Id, RiderClientAssignmentId = assignment.Id,
            RiderProfileId = rider.Id, StartedAtUtc = oldSession.EndedAtUtc.Value, CommissionStartsOn = Day };
        f.Db.AddRange(employee, rider, assignment, session);
        await f.Db.SaveChangesAsync();
        await f.Import(Day, content);
        Assert.Equal(1, await f.Db.JahezDailyDispatches.CountAsync(d => d.HandoverId == session.Id && d.Count == 10));
        Assert.Equal(1, await f.Db.JahezDailyDispatches.CountAsync(d => d.HandoverId == other.Id && d.Count == 10));
        Assert.False(await f.Db.JahezDailyDispatches.AnyAsync(d => d.HandoverId == prior.Id));
    }

    private sealed class User : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid(); public Guid? SessionId => null;
        public long? AuthorizationVersion => 1; public string? CorrelationId => null;
    }
    private sealed class Checker : IPermissionChecker
    {
        public string? Denied { get; set; }
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null,
            CancellationToken cancellationToken = default) => Task.FromResult(permissionKey != Denied);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero); }
    private sealed class GeneratedValues : SaveChangesInterceptor
    {
        private long sequence;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            foreach (var entry in eventData.Context!.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
            {
                if (entry.Metadata.FindProperty("RowVersion") != null) entry.Property("RowVersion").CurrentValue = Guid.NewGuid().ToByteArray();
                if (entry.Entity is AuditEntry audit && entry.State == EntityState.Added) audit.Sequence = ++sequence;
            }
            return ValueTask.FromResult(result);
        }
    }
    private sealed class AuditFault : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Enabled && eventData.Context!.ChangeTracker.Entries<AuditEntry>().Any(e => e.State == EntityState.Added && e.Entity.EntityType.EndsWith("PerformanceDay", StringComparison.Ordinal)))
                throw new DbUpdateException("Injected deletion audit failure.");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public required ApplicationDbContext Db { get; init; }
        public required Checker Checker { get; init; }
        public required AuditFault Fault { get; init; }
        public required PlatformRiderAccount[] Accounts { get; init; }
        public required string Platform { get; init; }
        public required HungerQualityService Hunger { get; init; }
        public required ChefzService Chefz { get; init; }
        public required JahezWorkflowService Jahez { get; init; }
        public string Permission => Platform switch { "HUNGER" => PermissionKeys.Hunger.PerformanceDelete, "CHEFZ" => PermissionKeys.Chefz.PerformanceDelete, _ => PermissionKeys.Jahez.PerformanceDelete };
        public static async Task<Fixture> Create(string code)
        {
            var user = new User(); var clock = new Clock(); var checker = new Checker(); var fault = new AuditFault();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("Data Source=:memory:;Foreign Keys=False")
                .ReplaceService<IModelCustomizer, OilWorkflowTestModelCustomizer>()
                .AddInterceptors(new ApplicationPersistenceInterceptor(user, clock), new GeneratedValues(), fault).Options;
            var db = new ApplicationDbContext(options, clock);
            await db.Database.OpenConnectionAsync(); await db.Database.EnsureCreatedAsync();
            var platform = await db.ClientPlatforms.SingleOrDefaultAsync(p => p.Code == code) ?? new ClientPlatform { Code = code, NameAr = code, NameEn = code };
            if (db.Entry(platform).State == EntityState.Detached) db.Add(platform);
            var contract = new ClientContract { ClientPlatformId = platform.Id, Code = "DELETE-TEST", DisplayNameAr = code, DisplayNameEn = code };
            db.Add(contract);
            var accounts = new List<PlatformRiderAccount>();
            for (var i = 0; i < 2; i++)
            {
                var employee = new Employee { FullNameAr = "Test rider " + i, IqamaNo = "252277793" + i };
                var rider = new RiderProfile { EmployeeId = employee.Id };
                var account = new PlatformRiderAccount { ClientPlatformId = platform.Id, RegisteredEmployeeId = employee.Id,
                    Code = code + i, ExternalAccountId = "100" + i, OperatingCityId = OperatingCity.RiyadhId,
                    SponsorId = i == 0 ? Sponsor.AlBawabaCommercialEstablishmentId : Sponsor.ExpressGateId };
                var assignment = new RiderClientAssignment { PlatformRiderAccountId = account.Id, ClientContractId = contract.Id, RiderProfileId = rider.Id,
                    EffectiveFrom = new(2026, 10, 1), Status = RiderAssignmentStatus.Active };
                db.AddRange(employee, rider, account, assignment);
                if (code == "CHEFZ") db.Add(new ChefzAccountDetails { PlatformRiderAccountId = account.Id, ReportIdNumber = employee.IqamaNo!, AccountType = ChefzAccountType.Freelancer });
                if (code == "JAHEZ") db.Add(new JahezAccountSession { PlatformRiderAccountId = account.Id, RiderClientAssignmentId = assignment.Id, RiderProfileId = rider.Id,
                    StartedAtUtc = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), CommissionStartsOn = new(2026, 10, 1) });
                accounts.Add(account);
            }
            await db.SaveChangesAsync();
            var protector = new PlatformCredentialProtector(() => new byte[32]);
            return new() { Db = db, Platform = code, Accounts = accounts.ToArray(), Checker = checker, Fault = fault,
                Hunger = new(db, user, checker, clock, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Features:Hunger"] = "true" }).Build()),
                Chefz = new(db, user, checker, clock, protector), Jahez = new(db, user, checker, clock) };
        }
        public async Task<Result<int>> Delete(DateOnly day, Guid? sponsor, string reason = "Wrong assignment")
        {
            var ct = TestContext.Current.CancellationToken;
            if (Platform == "HUNGER") { var r = await Hunger.DeletePerformanceAsync(day, sponsor, reason, ct); return r.IsFailure ? Result.Failure<int>(r.Error) : Result.Success(r.Value!.PerformanceRowsDeleted); }
            if (Platform == "CHEFZ") { var r = await Chefz.DeletePerformanceAsync(day, sponsor, reason, ct); return r.IsFailure ? Result.Failure<int>(r.Error) : Result.Success(r.Value!.PerformanceRowsDeleted); }
            var result = await Jahez.DeletePerformanceAsync(day, sponsor, reason, ct);
            return result.IsFailure ? Result.Failure<int>(result.Error) : Result.Success(result.Value!.PerformanceRowsDeleted);
        }
        public Task<int> Count(DateOnly day) => Platform switch
        {
            "HUNGER" => Db.Set<HungerDailyOperation>().CountAsync(p => p.ReportDate == day),
            "CHEFZ" => Db.ChefzDailyPerformance.CountAsync(p => p.ReportDate == day),
            _ => Db.Set<JahezDailyDispatch>().CountAsync(p => p.Date == day)
        };
        public async Task<byte[]> Import(DateOnly day, byte[]? content = null)
        {
            var ct = TestContext.Current.CancellationToken;
            content ??= Workbook(day);
            if (Platform == "HUNGER")
            {
                var preview = Value(await Hunger.PreviewOperationsAsync("ops.xlsx", content, day, ct));
                Value(await Hunger.ConfirmOperationsAsync(preview.PreviewId, ct));
            }
            else if (Platform == "CHEFZ") Assert.Empty(Value(await Chefz.ImportPerformanceAsync(day, "performance.xlsx", content, ct)).Issues);
            else
            {
                var preview = Value(await Jahez.UploadAsync(Guid.NewGuid().ToString(), new(JahezImportKind.DailyDispatches, [new("orders.xlsx", content)]), ct));
                Assert.Empty(preview.Issues);
                Assert.Empty(Value(await Jahez.CommitImportAsync(Guid.NewGuid().ToString(), preview.BatchId, new(), ct)).Issues);
            }
            return content;
        }
        private byte[] Workbook(DateOnly day)
        {
            using var book = new XLWorkbook(); var sheet = book.AddWorksheet(Platform == "JAHEZ" ? "Delivery Insights Report" : "Report");
            string[] headers = Platform switch
            {
                "HUNGER" => ["Captain ID", "CompletedOrders", "ActualWorkingHours", "RejectedOrders", "CancelledOrders", "NoShowCount"],
                "CHEFZ" => ["Driver Name", "Id Number", "Phone Number", "Team", "Total Offers", "Total Valid Offers", "Accepted Offers", "Declined Offers", "Missed Offers", "Acceptance Rate"],
                _ => ["Driver ID", "From", "To", "Number Of Dispatches"]
            };
            for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
            for (var i = 0; i < 2; i++)
            {
                var row = i + 2;
                if (Platform == "HUNGER") { sheet.Cell(row, 1).Value = Accounts[i].ExternalAccountId; sheet.Cell(row, 2).Value = 10; sheet.Cell(row, 3).Value = 8; sheet.Cell(row, 6).Value = 1; }
                else if (Platform == "CHEFZ") { sheet.Cell(row, 2).Value = "252277793" + i; sheet.Cell(row, 6).Value = 10; sheet.Cell(row, 7).Value = 8; sheet.Cell(row, 8).Value = 1; sheet.Cell(row, 9).Value = 1; sheet.Cell(row, 10).Value = "80%"; }
                else { sheet.Cell(row, 1).Value = Accounts[i].ExternalAccountId; sheet.Cell(row, 2).Value = day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture); sheet.Cell(row, 3).Value = day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture); sheet.Cell(row, 4).Value = 10; }
            }
            using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
