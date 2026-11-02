using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Fact]
    public async Task OrderInactivityUsesCurrentSessionAndEffectiveImports()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("inactivity");
        var handover = Success(await f.Service.HandoverAsync("inactivity-start", new(account.Id, f.RiderId, At(1), "Start", 0), ct));
        var original = new JahezImportBatch { CommittedAtUtc = At(2) };
        var replacement = new JahezImportBatch { CommittedAtUtc = At(3), ReplacesBatchId = original.Id };
        var pending = new JahezImportBatch();
        f.Db.AddRange(original, replacement, pending);
        f.Db.AddRange(
            new JahezDailyDispatch { BatchId = original.Id, HandoverId = handover.Id, Date = DateOnly.FromDateTime(At(5).Date), Count = 10 },
            new JahezDailyDispatch { BatchId = replacement.Id, HandoverId = handover.Id, Date = DateOnly.FromDateTime(At(3).Date), Count = 1 },
            new JahezDailyDispatch { BatchId = replacement.Id, HandoverId = handover.Id, Date = DateOnly.FromDateTime(At(5).Date), Count = 0 },
            new JahezDailyDispatch { BatchId = pending.Id, HandoverId = handover.Id, Date = DateOnly.FromDateTime(At(6).Date), Count = 5 });
        await f.Db.SaveChangesAsync(ct);

        var row = Assert.Single(Success(await f.Service.SettlementRemindersAsync(1, 50, ct)).Items);
        Assert.True(row.IsAssigned);
        Assert.Equal(DateOnly.FromDateTime(At(3).Date), row.LastOrderDate);
        Assert.Equal(3, row.DaysWithoutOrders);
        Assert.True(row.NoOrdersForThreeDays);
        f.Clock.UtcNow = At(5);
        row = Assert.Single(Success(await f.Service.SettlementRemindersAsync(1, 50, ct)).Items);
        Assert.Equal(2, row.DaysWithoutOrders);
        Assert.False(row.NoOrdersForThreeDays);
    }
    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void SettlementReminderDebtQueryTranslatesToSql(int count)
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UnusedReminderDebt;Trusted_Connection=True").Options);
        var service = new JahezWorkflowService(db, new MutableUser(), new TestPermissions(), new MutableClock { UtcNow = At(6) });
        var accounts = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();
        var query = (IQueryable<JahezAdministrativeDebt>)typeof(JahezWorkflowService)
            .GetMethod("AdministrativeDebtAccountsFor", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, [accounts])!;
        var sql = query.ToQueryString();
        Assert.Contains("SUM", sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("AccountId", sql);
    }

    [Theory]
    [InlineData(JahezOperationMode.Settle, 0)]
    [InlineData(JahezOperationMode.Settle, 3)]
    [InlineData(JahezOperationMode.Settle, 11)]
    [InlineData(JahezOperationMode.SettleAndWithdraw, 3)]
    [InlineData(JahezOperationMode.SettleAndWithdraw, 11)]
    public async Task SettlementRemindersIncludeOnlyAssignedSessions(JahezOperationMode mode, int days)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("settlement-reminder");
        var handover = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(handover.Id, 100, mode), ct));
        Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var settledAt = f.Clock.UtcNow;
        f.Clock.UtcNow = settledAt.AddDays(days);

        var rows = Success(await f.Service.SettlementRemindersAsync(1, 50, ct)).Items;
        if (mode == JahezOperationMode.SettleAndWithdraw)
        {
            Assert.Empty(rows);
            return;
        }
        var row = Assert.Single(rows);

        Assert.Equal(handover.Id, row.HandoverId);
        Assert.Equal(account.Id, row.AccountId);
        Assert.Equal(f.RiderId, row.RiderProfileId);
        Assert.Equal(settledAt, row.ReminderAnchorAtUtc);
        Assert.Equal(days, row.DaysSinceSettlement);
        Assert.Equal(0m, row.AdministrativeDebt);
    }

    [Fact]
    public async Task SettlementRemindersReturnCurrentAccountDebtAcrossSessions()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("reminder-debt");
        var other = await f.Account("reminder-credit");
        var closed = Success(await f.Service.HandoverAsync("closed-start", new(account.Id, f.RiderId, At(1), "Start", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("close-draft", Settle(closed.Id, 100, JahezOperationMode.SettleAndWithdraw), ct));
        Success(await f.Service.ExecuteOperationAsync("close", draft.Id, ct));
        f.Clock.UtcNow = At(7).ToUniversalTime();
        var current = Success(await f.Service.HandoverAsync("current-start", new(account.Id, f.OtherRiderId, At(7), "Start", 0), ct));
        Success(await f.Service.HandoverAsync("other-start", new(other.Id, f.RiderId, At(7), "Start", 0), ct));

        void Entry(Guid? accountId, Guid? handoverId, JahezLedgerBucket bucket, decimal amount) =>
            f.Db.Add(new JahezLedgerEntry { AccountId = accountId, HandoverId = handoverId,
                Bucket = bucket, Kind = JahezLedgerKind.Adjustment, Amount = amount,
                SourceId = Guid.NewGuid(), OccurredAtUtc = f.Clock.UtcNow, Reason = "Test balance" });
        Entry(account.Id, null, JahezLedgerBucket.PlatformDebt, 250);
        Entry(null, closed.Id, JahezLedgerBucket.PlatformDebt, -25);
        Entry(account.Id, current.Id, JahezLedgerBucket.PlatformDebt, 10);
        Entry(account.Id, current.Id, JahezLedgerBucket.Commission, 999);
        Entry(other.Id, null, JahezLedgerBucket.PlatformDebt, -50);
        await f.Db.SaveChangesAsync(ct);

        var rows = Success(await f.Service.SettlementRemindersAsync(1, 50, ct)).Items;
        var expected = Success(await f.Service.AdministrativeDebtsAsync(1, 50, ct)).Items.ToDictionary(x => x.AccountId, x => x.PlatformDebt);
        Assert.Equal(2, rows.Count);
        Assert.DoesNotContain(rows, x => x.HandoverId == closed.Id);
        Assert.Equal(235m, rows.Single(x => x.HandoverId == current.Id).AdministrativeDebt);
        Assert.Equal(-50m, rows.Single(x => x.AccountId == other.Id).AdministrativeDebt);
        Assert.All(rows, row => Assert.Equal(expected[row.AccountId], row.AdministrativeDebt));
        var paged = Success(await f.Service.SettlementRemindersAsync(2, 1, ct));
        Assert.Equal(rows[1], Assert.Single(paged.Items));
    }

    [Fact]
    public async Task SettlementRemindersIncludeNewSessionsAndKeepPaginationAndPermissionChecks()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var first = await f.Account("reminder-first");
        var second = await f.Account("reminder-second");
        var h1 = Success(await f.Service.HandoverAsync("first", new(first.Id, f.RiderId, At(5), "استلام", 0), ct));
        var h2 = Success(await f.Service.HandoverAsync("second", new(second.Id, f.OtherRiderId, At(6), "استلام", 0), ct));

        var all = Success(await f.Service.SettlementRemindersAsync(1, 50, ct));
        Assert.Equal(2, all.Items.Count);
        Assert.Contains(all.Items, x => x.HandoverId == h1.Id && x.ReminderAnchorAtUtc == At(5) && x.DaysSinceSettlement == 1);
        Assert.Contains(all.Items, x => x.HandoverId == h2.Id && x.ReminderAnchorAtUtc == At(6) && x.DaysSinceSettlement == 0);
        var page1 = Success(await f.Service.SettlementRemindersAsync(1, 1, ct));
        var page2 = Success(await f.Service.SettlementRemindersAsync(2, 1, ct));
        Assert.Equal(all.Items[0], Assert.Single(page1.Items));
        Assert.Equal(all.Items[1], Assert.Single(page2.Items));
        Assert.Empty(Success(await f.Service.SettlementRemindersAsync(3, 1, ct)).Items);
        var futureSession = await f.Db.JahezAccountSessions.SingleAsync(x => x.Id == h2.Id, ct);
        futureSession.StartedAtUtc = f.Clock.UtcNow.AddDays(1);
        await f.Db.SaveChangesAsync(ct);
        var current = Success(await f.Service.SettlementRemindersAsync(1, 1, ct));
        Assert.Equal(h1.Id, Assert.Single(current.Items).HandoverId);
        Assert.Empty(Success(await f.Service.SettlementRemindersAsync(2, 1, ct)).Items);
        Assert.False((await f.Service.SettlementRemindersAsync(0, 50, ct)).IsSuccess);
        Assert.False((await f.Service.SettlementRemindersAsync(1, 101, ct)).IsSuccess);

        f.Permissions.Denied = PermissionKeys.Jahez.Read;
        Assert.False((await f.Service.SettlementRemindersAsync(1, 50, ct)).IsSuccess);
    }
}
