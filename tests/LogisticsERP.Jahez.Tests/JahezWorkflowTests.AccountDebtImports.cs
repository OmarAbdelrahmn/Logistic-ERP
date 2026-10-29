using System.Reflection;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdministrativeDebtAcceptsUnassignedAccountAndRetainsBalanceAfterAssignment(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql);
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("account-debt");
        var input = new JahezWorkflowRequestCreate(null, JahezApprovalKind.Adjustment, "Account debt",
            AdjustmentTarget: JahezAdjustmentTarget.AdministrativeDebt, DebtAdjustment: 250, AccountId: account.Id);
        var pending = Success(await f.Service.CreateWorkflowRequestAsync("debt", input, ct));
        Assert.Null(pending.Request.HandoverId);
        Assert.Equal(account.Id, pending.Request.AccountId);
        Assert.Equal(JahezErrors.Forbidden, (await f.Service.DecideWorkflowRequestAsync("self", pending.Request.Id, new(true, "Approve"), ct)).Error);
        f.User.UserId = Guid.NewGuid();
        f.Permissions.Denied = JahezWorkflowPermissions.DebtAdjust;
        Assert.Equal(JahezErrors.Forbidden, (await f.Service.DecideWorkflowRequestAsync("denied", pending.Request.Id, new(true, "Approve"), ct)).Error);
        Assert.Empty(await f.Db.JahezLedgerEntries.ToArrayAsync(ct));
        f.Permissions.Denied = null;
        Success(await f.Service.DecideWorkflowRequestAsync("approve", pending.Request.Id, new(true, "Approve"), ct));
        Success(await f.Service.DecideWorkflowRequestAsync("approve", pending.Request.Id, new(true, "Approve"), ct));
        var row = Assert.Single(Success(await f.Service.AdministrativeDebtsAsync(1, 50, ct)).Items);
        Assert.Equal(250, row.PlatformDebt);
        Assert.Null(row.HandoverId);
        Assert.Null(row.RiderProfileId);
        var detail = Success(await f.Service.RequestV2Async(pending.Request.Id, ct)).Request;
        Assert.Equal(account.Id, detail.Account!.AccountId);
        Assert.Null(detail.Assignment);
        Assert.Null(detail.UserRider);
        var accountDetail = Success(await f.Service.AccountDetailsV2Async(account.Id, 1, 50, ct));
        Assert.Single(accountDetail.Requests.Items);
        Assert.Single(accountDetail.Ledger.Items);

        var h = Success(await f.Service.HandoverAsync("handover", new(account.Id, f.RiderId, At(1), "Start"), ct));
        row = Assert.Single(Success(await f.Service.AdministrativeDebtsAsync(1, 50, ct)).Items);
        Assert.Equal(250, row.PlatformDebt);
        Assert.Equal(h.Id, row.HandoverId);
        Assert.Equal(75, Success(await f.Service.PreviewOperationAsync(Settle(h.Id, 0), ct)).Remaining);
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Empty(await f.Db.JahezTransferredDebts.ToArrayAsync(ct));
    }

    [Fact]
    public async Task AdministrativeDebtRejectsMissingAccountMismatchedAssignmentAndUnassignedOtherRequestKinds()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("first"); var b = await f.Account("second");
        var h = Success(await f.Service.HandoverAsync("start", new(a.Id, f.RiderId, At(1), "Start"), ct));
        var input = new JahezWorkflowRequestCreate(null, JahezApprovalKind.Adjustment, "Debt",
            AdjustmentTarget: JahezAdjustmentTarget.AdministrativeDebt, DebtAdjustment: 100);
        Assert.True((await f.Service.CreateWorkflowRequestAsync("missing", input, ct)).IsFailure);
        Assert.True((await f.Service.CreateWorkflowRequestAsync("mismatch", input with { HandoverId = h.Id, AccountId = b.Id }, ct)).IsFailure);
        Assert.True((await f.Service.CreateWorkflowRequestAsync("fee", new(null, JahezApprovalKind.FeeException, "Fee", AccountId: b.Id, WaiverAmount: 50), ct)).IsFailure);
        Assert.Empty(await f.Db.JahezApprovalRequests.ToArrayAsync(ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransactionSheetReplacesAllAccountHistoryWithoutAssignmentAndKeepsOtherAccounts(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql);
        var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("sheet-account");
        var b = await f.Account("untouched");
        var original = Transactions(a.ExternalAccountId, "2026-10-01 08:00:00", 100, 0, -100, 0);
        async Task<Guid> Import(string key, string driver, byte[] content)
        {
            var preview = Success(await f.Service.UploadAsync(key, new(JahezImportKind.Transactions, [new(driver + ".xlsx", content)]), ct));
            Assert.Empty(preview.Issues);
            Assert.All(preview.Rows, row => Assert.Null(row.HandoverId));
            Success(await f.Service.CommitImportAsync(key + "-commit", preview.BatchId, new(), ct));
            Success(await f.Service.CommitImportAsync(key + "-commit", preview.BatchId, new(), ct));
            return preview.BatchId;
        }
        var first = await Import("first", a.ExternalAccountId, original);
        await Import("other", b.ExternalAccountId, Transactions(b.ExternalAccountId, "2026-10-01 08:00:00", 20, 0, -20, 0));
        var replacement = await Import("replace", a.ExternalAccountId, Transactions(a.ExternalAccountId, "2026-10-05 08:00:00", 45, 0, -45, 0));
        var debts = Success(await f.Service.AdministrativeDebtsAsync(1, 50, ct)).Items.ToDictionary(x => x.AccountId);
        Assert.Equal(45, debts[a.Id].PlatformDebt);
        Assert.Equal(20, debts[b.Id].PlatformDebt);
        Assert.Equal(replacement, (await f.Db.JahezTransactions.SingleAsync(x => x.BatchId == first, ct)).SupersededByBatchId);
        Assert.Equal(2, await f.Db.JahezTransactions.CountAsync(x => x.SupersededByBatchId == null, ct));
        Assert.Single(await f.Db.JahezLedgerEntries.Where(x => x.ReversesEntryId != null).ToArrayAsync(ct));
        await Import("overlapping", a.ExternalAccountId, Transactions(a.ExternalAccountId, "2026-10-05 08:00:00", 60, 0, -60, 0));
        Assert.Equal(60, Success(await f.Service.AdministrativeDebtsAsync(1, 50, ct)).Items.Single(x => x.AccountId == a.Id).PlatformDebt);
        // The same bytes can be uploaded again after a different snapshot, using a fresh command key.
        await Import("restore", a.ExternalAccountId, original);
        Assert.Equal(100, Success(await f.Service.AdministrativeDebtsAsync(1, 50, ct)).Items.Single(x => x.AccountId == a.Id).PlatformDebt);
        Assert.Equal(2, await f.Db.JahezTransactions.CountAsync(x => x.SupersededByBatchId == null, ct));
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
    }

    [Fact]
    public async Task TransactionReplacementRejectsInvalidSheetAndPreservesOtherAccountsFromSharedBatch()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("shared-a"); var b = await f.Account("shared-b");
        JahezUploadFile File(string driver, decimal net) =>
            new(driver + ".xlsx", Transactions(driver, "2026-10-01 08:00:00", 0, 0, net, 0));
        var first = Success(await f.Service.UploadAsync("shared", new(JahezImportKind.Transactions,
            [File(a.ExternalAccountId, -100), File(b.ExternalAccountId, -200)]), ct));
        Success(await f.Service.CommitImportAsync("shared-commit", first.BatchId, new(), ct));
        var invalid = Success(await f.Service.UploadAsync("invalid", new(JahezImportKind.Transactions,
            [File(a.ExternalAccountId, -50), File("unknown-account", -1)]), ct));
        Assert.Contains(invalid.Issues, issue => issue.Code == "unknown_account");
        Assert.True((await f.Service.CommitImportAsync("invalid-commit", invalid.BatchId, new(), ct)).IsFailure);
        Assert.Empty(f.Db.ChangeTracker.Entries());
        Assert.Equal(2, await f.Db.JahezTransactions.CountAsync(x => x.SupersededByBatchId == null, ct));
        Assert.Empty(await f.Db.JahezLedgerEntries.Where(x => x.ReversesEntryId != null).ToArrayAsync(ct));
        var next = Success(await f.Service.UploadAsync("subset", new(JahezImportKind.Transactions, [File(a.ExternalAccountId, -50)]), ct));
        Success(await f.Service.CommitImportAsync("subset-commit", next.BatchId, new(), ct));
        var debts = Success(await f.Service.AdministrativeDebtsAsync(1, 50, ct)).Items.ToDictionary(x => x.AccountId);
        Assert.Equal(50, debts[a.Id].PlatformDebt);
        Assert.Equal(200, debts[b.Id].PlatformDebt);
        Assert.Null((await f.Db.JahezTransactions.SingleAsync(x => x.BatchId == first.BatchId && x.AccountId == b.Id, ct)).SupersededByBatchId);
    }

    [Fact]
    public async Task AdministrativeDebtSheetLeavesDailyOrdersAndTheirValidationUnchanged()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var a = await f.Account("daily-preserved");
        Success(await f.Service.HandoverAsync("handover", new(a.Id, f.RiderId, At(1), "Start"), ct));
        var daily = Success(await f.Service.UploadAsync("daily", new(JahezImportKind.DailyDispatches,
            [new("daily.xlsx", Dispatches(a.ExternalAccountId, "2026-10-05", 7))]), ct));
        Assert.Empty(daily.Issues);
        Success(await f.Service.CommitImportAsync("daily-commit", daily.BatchId, new(), ct));
        foreach (var (key, amount) in new[] { ("first", -100m), ("second", -50m) })
        {
            var debt = Success(await f.Service.UploadAsync(key, new(JahezImportKind.Transactions,
                [new("debt.xlsx", Transactions(a.ExternalAccountId, "2026-10-05 08:00:00", 0, 0, amount, 0))]), ct));
            Assert.Empty(debt.Issues);
            Success(await f.Service.CommitImportAsync(key + "-commit", debt.BatchId, new(), ct));
        }
        Assert.Equal(7, Assert.Single(Success(await f.Service.AccountsV2Async(1, 50, ct)).Items).YesterdayOrders);
        Assert.Equal(daily.BatchId, Assert.Single(await f.Db.JahezDailyDispatches.ToArrayAsync(ct)).BatchId);
        var overlapping = Success(await f.Service.UploadAsync("daily-overlap", new(JahezImportKind.DailyDispatches,
            [new("daily.xlsx", Dispatches(a.ExternalAccountId, "2026-10-05", 8))]), ct));
        Assert.Contains(overlapping.Issues, issue => issue.Code == "overlapping_period");
        var unassigned = await f.Account("daily-still-needs-assignment");
        var missing = Success(await f.Service.UploadAsync("daily-unassigned", new(JahezImportKind.DailyDispatches,
            [new("daily.xlsx", Dispatches(unassigned.ExternalAccountId, "2026-10-05", 3))]), ct));
        Assert.NotEmpty(missing.Issues);
    }

    [Fact]
    public void AdministrativeDebtAccountQueryTranslatesToSql()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UnusedAccountDebt;Trusted_Connection=True").Options);
        var service = new JahezWorkflowService(db, new MutableUser(), new TestPermissions(), new MutableClock { UtcNow = At(6) });
        var query = (IQueryable<JahezAdministrativeDebt>)typeof(JahezWorkflowService)
            .GetMethod("AdministrativeDebtAccounts", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, [])!;
        var sql = query.Skip(0).Take(50).ToQueryString();
        Assert.Contains("LEFT JOIN", sql);
        Assert.Contains("AccountId", sql);
        Assert.Contains("SUM", sql);
        Assert.Contains("FETCH NEXT", sql);
    }
}
