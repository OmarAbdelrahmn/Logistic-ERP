using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Fact]
    public async Task SqlServerSharedDayImportsSerializeAndEnforceUsageConstraints()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("NINJA_SQL_TEST_SERVER") is not null,
            "Set NINJA_SQL_TEST_SERVER to run disposable SQL Server shared-day concurrency/constraint checks.");
        await using var f = await Fixture.Create(true); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0501111111", "initial", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0502222222", null, "Return", first.Assignment.RowVersion, DayUsage: new(2, 3)), ct));
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, date, "0503333333", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        await using var db1 = f.OtherContext(); await using var db2 = f.OtherContext();
        var content = Sheet(false, "272578", 6, 10);
        var results = await Task.WhenAll(f.CreateService(db1).ImportAsync(false, date, "one.xlsx", content, ct),
            f.CreateService(db2).ImportAsync(false, date, "two.xlsx", content, ct));
        Assert.All(results, r => Assert.Empty(Value(r).Issues));
        Assert.Equal(1, results.Sum(r => r.Value!.ImportedRows));
        Assert.Equal(1, await f.Db.NinjaDailyPerformanceSources.CountAsync(ct));
        Assert.Equal(2, await f.Db.NinjaDailyPerformance.CountAsync(ct));
        Assert.Equal(6, await f.Db.NinjaDailyPerformance.SumAsync(p => p.TotalOrders, ct));

        var usageService = new PlatformAccountDayUsageService(f.Db, f.User, f.Checker, f.Clock);
        var before = Value(await usageService.GetAsync(f.Account.Id, date, ct));
        var update = new PlatformAccountDayUpdateRequest(before.Assignments.Select(p => new PlatformAccountDayUsageUpdate(p.AssignmentId,
            p.AssignmentId == first.Assignment.Id ? p.Figures with { CollectedAmount = 30, DeductionsAmount = -5 } : p.Figures,
            p.RowVersion)).ToArray(), "Complete money figures");
        var after = Value(await usageService.UpdateAsync(f.Account.Id, date, update, ct));
        Assert.NotEqual(before.Assignments[0].RowVersion, after.Assignments[0].RowVersion);
        Assert.True((await usageService.UpdateAsync(f.Account.Id, date, update, ct)).IsFailure);

        await using var invalid = f.OtherContext();
        invalid.Add(new PlatformAccountDayUsage { PlatformRiderAccountId = f.Account.Id, RiderClientAssignmentId = first.Assignment.Id,
            BusinessDate = date.AddDays(-1), Sequence = 1, OrdersCount = -1, Reason = "Invalid" });
        await Assert.ThrowsAsync<DbUpdateException>(() => invalid.SaveChangesAsync(ct));
        invalid.ChangeTracker.Clear();
        invalid.Add(new PlatformAccountDayUsage { PlatformRiderAccountId = f.Account.Id, RiderClientAssignmentId = first.Assignment.Id,
            BusinessDate = date, Sequence = 1, Reason = "Duplicate" });
        await Assert.ThrowsAsync<DbUpdateException>(() => invalid.SaveChangesAsync(ct));
    }
}
