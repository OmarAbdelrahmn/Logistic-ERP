using System.Reflection;
using System.Linq.Expressions;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    private static async Task<JahezImportPreview> PerformanceImport(Fixture f, string external, int day, int count,
        string key, Guid? replaces = null, bool commit = true)
    {
        var ct = TestContext.Current.CancellationToken;
        var upload = Success(await f.Service.UploadAsync(key, new(JahezImportKind.DailyDispatches,
            [new("orders.xlsx", Dispatches(external, $"2026-10-{day:D2}", count))], replaces, replaces.HasValue ? "تصحيح الطلبات" : null), ct));
        Assert.Empty(upload.Issues);
        if (commit) Success(await f.Service.CommitImportAsync($"commit-{key}", upload.BatchId, new(), ct));
        return upload;
    }

    [Fact]
    public async Task RiderPerformanceReportIncludesBothDatesAndSeparatesMissingFromZero()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("performance-period");
        Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام"), ct));
        foreach (var (day, orders) in new[] { (1, 999), (2, 5), (3, 0), (5, 8), (6, 888) })
            await PerformanceImport(f, account.ExternalAccountId, day, orders, $"orders-{day}");
        var employee = await (from r in f.Db.RiderProfiles join e in f.Db.Employees on r.EmployeeId equals e.Id
            where r.Id == f.RiderId select e).SingleAsync(ct);
        employee.IqamaNo = "1234567890"; await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        var report = Success(await f.Service.RiderPerformanceReportAsync(new(2026, 10, 2), new(2026, 10, 5), f.RiderId, ct: ct));
        var rider = Assert.Single(report.Items);
        Assert.Equal(1, report.TotalCount); Assert.Equal(f.RiderId, rider.RiderProfileId);
        Assert.Equal("المندوب الفعلي", rider.RiderName); Assert.Equal("1234567890", rider.RiderIqamaNo);
        Assert.Equal(13, rider.SelectedSummary.Orders);
        Assert.Equal(4, rider.SelectedSummary.PeriodDays); Assert.Equal(3, rider.SelectedSummary.DaysWithData);
        Assert.Equal(2, rider.SelectedSummary.DaysWithOrders); Assert.Equal(1, rider.SelectedSummary.ZeroOrderDays);
        Assert.Equal(1, rider.SelectedSummary.MissingDays); Assert.Equal(4.33m, rider.SelectedSummary.AverageOrdersPerDayWithData);
        Assert.Equal(8, rider.SelectedSummary.MaxOrdersPerDay);
        Assert.Equal(new long?[] { 5, 0, null, 8 }, rider.Daily.Select(d => d.Orders));
        Assert.Equal(new DateOnly(2026, 10, 2), rider.Daily[0].Date); Assert.Equal(new DateOnly(2026, 10, 5), rider.Daily[^1].Date);
        var breakdown = Assert.Single(rider.Accounts);
        Assert.Equal(account.Id, breakdown.AccountId); Assert.Equal(rider.SelectedSummary, breakdown.Summary);
        Assert.Equal(rider.Daily, breakdown.Daily);
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task RiderPerformanceReportUsesOnlyCommittedCurrentImports()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("performance-correction");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام"), ct));
        var old = await PerformanceImport(f, account.ExternalAccountId, 5, 8, "old");
        var draft = await PerformanceImport(f, account.ExternalAccountId, 5, 0, "replacement", old.BatchId, false);
        var start = new DateOnly(2026, 10, 5);
        Assert.Equal(8, Assert.Single(Success(await f.Service.RiderPerformanceReportAsync(start, start, f.RiderId, ct: ct)).Items).SelectedSummary.Orders);
        Success(await f.Service.CommitImportAsync("replacement-commit", draft.BatchId, new(), ct));
        // An uncommitted source cannot contribute even if an invalid/staged daily row exists.
        var uncommitted = new JahezImportBatch { Kind = JahezImportKind.DailyDispatches };
        f.Db.Add(uncommitted);
        f.Db.Add(new JahezDailyDispatch { BatchId = uncommitted.Id, HandoverId = h.Id, Date = start, Count = 999, ImportRowId = Guid.NewGuid() });
        await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        var rider = Assert.Single(Success(await f.Service.RiderPerformanceReportAsync(start, start, f.RiderId, ct: ct)).Items);
        Assert.Equal(0, rider.SelectedSummary.Orders); Assert.Equal(1, rider.SelectedSummary.ZeroOrderDays);
        Assert.Equal(0, Assert.Single(rider.Daily).Orders);
        var deletedReplacement = await f.Db.Set<JahezImportBatch>().SingleAsync(b => b.Id == draft.BatchId, ct);
        deletedReplacement.IsDeleted = true; await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        Assert.Equal(8, Assert.Single(Success(await f.Service.RiderPerformanceReportAsync(start, start, f.RiderId, ct: ct)).Items).SelectedSummary.Orders);
        var deletedSource = await f.Db.Set<JahezImportBatch>().SingleAsync(b => b.Id == old.BatchId, ct);
        deletedSource.IsDeleted = true; await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        Assert.Equal(0, Assert.Single(Success(await f.Service.RiderPerformanceReportAsync(start, start, f.RiderId, ct: ct)).Items).SelectedSummary.DaysWithData);
    }

    [Fact]
    public async Task RiderPerformanceReportKeepsActualHistoricalRidersAndMultipleAccountsOnSharedDay()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("performance-shared");
        var another = await f.Account("performance-other");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام"), ct));
        var withdraw = Success(await f.Service.CreateOperationAsync("withdraw", new(h.Id, JahezOperationMode.Withdraw,
            new(2026, 10, 6), At(6, 12), 0, "سحب", DayUsage: new(OrdersCount: 2)), ct));
        Success(await f.Service.ExecuteOperationAsync("execute", withdraw.Id, ct));
        Success(await f.Service.HandoverAsync("next-rider", new(account.Id, f.OtherRiderId, At(6, 13), "المندوب التالي"), ct));
        Success(await f.Service.HandoverAsync("next-account", new(another.Id, f.RiderId, At(6, 14), "حساب آخر"), ct));
        await PerformanceImport(f, account.ExternalAccountId, 6, 6, "shared");
        await PerformanceImport(f, another.ExternalAccountId, 6, 3, "another");
        var date = new DateOnly(2026, 10, 6);
        var report = Success(await f.Service.RiderPerformanceReportAsync(date, date, ct: ct));
        Assert.Equal(2, report.TotalCount);
        var first = Assert.Single(report.Items, r => r.RiderProfileId == f.RiderId);
        Assert.Equal(5, first.SelectedSummary.Orders); Assert.Equal(1, first.SelectedSummary.DaysWithData);
        Assert.Equal(2, first.Accounts.Count); Assert.Equal(5, Assert.Single(first.Daily).Orders);
        var next = Assert.Single(report.Items, r => r.RiderProfileId == f.OtherRiderId);
        Assert.Equal(4, next.SelectedSummary.Orders); Assert.Single(next.Accounts);
        var filtered = Success(await f.Service.RiderPerformanceReportAsync(date, date, f.RiderId, account.Id, ct: ct));
        Assert.Equal(2, Assert.Single(filtered.Items).SelectedSummary.Orders);
        Assert.Single(filtered.Items[0].Accounts);
        var accountOnly = Success(await f.Service.RiderPerformanceReportAsync(date, date, accountId: account.Id, ct: ct));
        Assert.Equal(2, accountOnly.Items.Count); Assert.Equal(6, accountOnly.Items.Sum(x => x.SelectedSummary.Orders));
        var page1 = Success(await f.Service.RiderPerformanceReportAsync(date, date, pageSize: 1, ct: ct));
        var page2 = Success(await f.Service.RiderPerformanceReportAsync(date, date, page: 2, pageSize: 1, ct: ct));
        Assert.Equal(2, page1.TotalCount); Assert.Equal(2, page2.TotalCount);
        Assert.Equal(report.Items[0].RiderProfileId, Assert.Single(page1.Items).RiderProfileId);
        Assert.Equal(report.Items[1].RiderProfileId, Assert.Single(page2.Items).RiderProfileId);
        Assert.Empty(Success(await f.Service.RiderPerformanceReportAsync(date, date, page: 3, pageSize: 1, ct: ct)).Items);
    }

    [Fact]
    public async Task RiderPerformanceReportIncludesAssignedRidersWithoutDataAndRetainsArchivedAccounts()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("performance-empty");
        Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام"), ct));
        var tracked = await f.Db.PlatformRiderAccounts.SingleAsync(a => a.Id == account.Id, ct);
        tracked.IsDeleted = true; await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        var rider = Assert.Single(Success(await f.Service.RiderPerformanceReportAsync(new(2026, 10, 2), new(2026, 10, 4), accountId: account.Id, ct: ct)).Items);
        Assert.Equal(0, rider.SelectedSummary.Orders); Assert.Equal(0, rider.SelectedSummary.DaysWithData);
        Assert.Equal(0, rider.SelectedSummary.ZeroOrderDays); Assert.Equal(3, rider.SelectedSummary.MissingDays);
        Assert.Null(rider.SelectedSummary.AverageOrdersPerDayWithData); Assert.Null(rider.SelectedSummary.MaxOrdersPerDay);
        Assert.All(rider.Daily, d => Assert.Null(d.Orders)); Assert.Single(rider.Accounts);
        Assert.Empty(Success(await f.Service.RiderPerformanceReportAsync(new(2026, 10, 2), new(2026, 10, 4), f.OtherRiderId, ct: ct)).Items);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("reversed")]
    [InlineData("future")]
    [InlineData("long")]
    [InlineData("page")]
    [InlineData("size")]
    [InlineData("emptyId")]
    public async Task RiderPerformanceReportRejectsInvalidInputs(string kind)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var start = kind == "missing" ? default : kind == "long" ? new DateOnly(2025, 1, 1) : new DateOnly(2026, 10, 1);
        var end = kind == "reversed" ? start.AddDays(-1) : kind == "future" ? new DateOnly(2026, 10, 7) : new DateOnly(2026, 10, 6);
        var result = await f.Service.RiderPerformanceReportAsync(start, end, kind == "emptyId" ? Guid.Empty : null,
            page: kind == "page" ? 0 : 1, pageSize: kind == "size" ? 101 : 50, ct: ct);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task RiderPerformanceReportExcludesOtherPlatformsAndDeletedRiders()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("performance-foreign");
        Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام"), ct));
        await PerformanceImport(f, account.ExternalAccountId, 5, 7, "orders");
        var other = new ClientPlatform { Code = "OTHER-PERFORMANCE", NameAr = "منصة أخرى", NameEn = "Other platform" };
        f.Db.Add(other);
        var tracked = await f.Db.PlatformRiderAccounts.SingleAsync(a => a.Id == account.Id, ct);
        tracked.ClientPlatformId = other.Id; await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        var date = new DateOnly(2026, 10, 5);
        Assert.Empty(Success(await f.Service.RiderPerformanceReportAsync(date, date, ct: ct)).Items);
        Assert.Equal(ErrorType.NotFound, (await f.Service.RiderPerformanceReportAsync(date, date, accountId: account.Id, ct: ct)).Error.Type);
        tracked = await f.Db.PlatformRiderAccounts.SingleAsync(a => a.Id == account.Id, ct);
        tracked.ClientPlatformId = f.PlatformId;
        var rider = await f.Db.RiderProfiles.SingleAsync(r => r.Id == f.RiderId, ct);
        rider.IsDeleted = true; await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        Assert.Empty(Success(await f.Service.RiderPerformanceReportAsync(date, date, ct: ct)).Items);
        Assert.Equal(ErrorType.NotFound, (await f.Service.RiderPerformanceReportAsync(date, date, f.RiderId, ct: ct)).Error.Type);
    }

    [Fact]
    public async Task RiderPerformanceReportAccepts366InclusiveDays()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var end = new DateOnly(2026, 10, 6);
        var report = Success(await f.Service.RiderPerformanceReportAsync(end.AddDays(-365), end, ct: ct));
        Assert.Empty(report.Items); Assert.Equal(0, report.TotalCount);
    }

    [Fact]
    public async Task RiderPerformanceReportRequiresJahezScopeAndValidFilters()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 6);
        f.Permissions.Denied = PermissionKeys.Jahez.Read;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.RiderPerformanceReportAsync(date, date, ct: ct)).Error.Type);
        f.Permissions.Denied = null; f.Permissions.RequiredScope = f.PlatformId;
        Assert.Equal(ErrorType.NotFound, (await f.Service.RiderPerformanceReportAsync(date, date, Guid.NewGuid(), ct: ct)).Error.Type);
        Assert.Equal(ErrorType.NotFound, (await f.Service.RiderPerformanceReportAsync(date, date, accountId: Guid.NewGuid(), ct: ct)).Error.Type);
        Assert.Empty(Success(await f.Service.RiderPerformanceReportAsync(date, date, ct: ct)).Items);
        Assert.All(f.Permissions.Scopes, s => Assert.Equal(f.PlatformId, s.TargetId));
    }

    [Fact]
    public async Task RiderPerformanceReportHostedReadOnly()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_HOSTED_READS") == "1", "Opt-in read-only hosted verification.");
        var ct = TestContext.Current.CancellationToken;
        var workspace = Path.GetFullPath("../../../../..", AppContext.BaseDirectory);
        using var config = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(workspace, "src/LogisticsERP.Api/appsettings.json"), ct));
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__LogisticsDatabase")
            ?? config.RootElement.GetProperty("ConnectionStrings").GetProperty("LogisticsDatabase").GetString()!;
        var target = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
        Assert.Equal("db67927.public.databaseasp.net", target.DataSource); Assert.Equal("db67927", target.InitialCatalog);
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__ApplicationMigrationsHistory", "migration")).Options);
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        var service = new JahezWorkflowService(db, new MutableUser(), new TestPermissions(), TimeProvider.System);
        var to = LogisticsERP.Domain.Jahez.JahezRules.RiyadhDate(TimeProvider.System.GetUtcNow());
        var from = to.AddDays(-30);
        var report = Success(await service.RiderPerformanceReportAsync(from, to, pageSize: 5, ct: ct));
        foreach (var rider in report.Items)
        {
            Assert.Equal(rider.Daily.Where(d => d.Orders.HasValue).Sum(d => d.Orders!.Value), rider.SelectedSummary.Orders);
            Assert.Equal(rider.Accounts.Sum(a => a.Summary.Orders), rider.SelectedSummary.Orders);
            Assert.Equal(31, rider.Daily.Count);
        }
        if (report.Items.Count > 0)
        {
            var first = report.Items[0];
            var filtered = Success(await service.RiderPerformanceReportAsync(from, to, first.RiderProfileId, ct: ct));
            Assert.Equal(first.SelectedSummary, Assert.Single(filtered.Items).SelectedSummary);
            var account = first.Accounts[0];
            var accountReport = Success(await service.RiderPerformanceReportAsync(from, to, first.RiderProfileId, account.AccountId, ct: ct));
            Assert.Equal(account.Summary, Assert.Single(accountReport.Items).SelectedSummary);
        }
        Assert.Empty(db.ChangeTracker.Entries()); Assert.False(db.Database.HasPendingModelChanges());
        await File.WriteAllTextAsync(Path.Combine(workspace, "outputs/jahez-rider-performance-hosted-read-verification.json"),
            System.Text.Json.JsonSerializer.Serialize(new { VerifiedAtUtc = DateTimeOffset.UtcNow, StartDate = from, EndDate = to,
                report.TotalCount, ReturnedRiders = report.Items.Count, ReadOnlyQueriesSucceeded = true,
                ApplicationModelMatchesSnapshot = true }), ct);
    }

    [Fact]
    public void RiderPerformanceReportQueriesTranslateWithSqlServer()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UnusedPerformance;Trusted_Connection=True").Options);
        var service = new JahezWorkflowService(db, new MutableUser(), new TestPermissions(), new MutableClock { UtcNow = At(6) });
        IQueryable Query(string name) => (IQueryable)typeof(JahezWorkflowService).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, [new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 6), null, null])!;
        var metrics = Query("RiderPerformanceMetrics");
        Assert.Contains("GROUP BY", metrics.ToQueryString()); Assert.Contains("NOT EXISTS", metrics.ToQueryString());
        var members = Query("RiderPerformanceMembers");
        Assert.Contains("UNION", members.ToQueryString()); Assert.Contains("Employees", members.ToQueryString());
        // The page's rider-ID filter must remain server-side after grouping / roster union.
        foreach (var query in new[] { metrics, members })
        {
            var parameter = Expression.Parameter(query.ElementType, "row");
            var predicate = Expression.Lambda(Expression.Equal(Expression.Property(parameter, "RiderProfileId"), Expression.Constant(Guid.NewGuid())), parameter);
            var filtered = query.Provider.CreateQuery(Expression.Call(typeof(Queryable), nameof(Queryable.Where), [query.ElementType], query.Expression, Expression.Quote(predicate)));
            Assert.Contains("WHERE", filtered.ToQueryString());
        }
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
