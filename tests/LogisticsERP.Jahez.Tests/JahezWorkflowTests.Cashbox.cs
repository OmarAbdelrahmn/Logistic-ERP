using System.Reflection;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
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
    public async Task V2CashboxBalancesClearOnlyOnAccountantConfirmationAndKeepHistory(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql);
        var ct = TestContext.Current.CancellationToken;
        await SeedCashboxMovements(f);
        var originalLedgerCount = await f.Db.JahezLedgerEntries.CountAsync(ct);
        f.Clock.UtcNow = At(7, 1).ToUniversalTime();
        Assert.Equal(50, Success(await f.Service.FundsV2Async(ct)).Single(x => x.Fund == JahezMoneyFund.Cashbox).Balance);

        var request = new JahezCashboxHandoverCreate(new(2026, 10, 6), "تسليم الأموال للمحاسب");
        var pending = Success(await f.Service.SubmitCashboxHandoverV2Async("submit", request, ct));
        Assert.Equal(50, pending.CashboxAmount); Assert.Equal(75, pending.PetroAppAmount); Assert.Equal(-20, pending.BankAmount);
        Assert.Equal(4, pending.MovementCount);
        Assert.Equal(pending.Id, Success(await f.Service.SubmitCashboxHandoverV2Async("submit", request, ct)).Id);
        Assert.Equal(50, Success(await f.Service.FundsV2Async(ct)).Single(x => x.Fund == JahezMoneyFund.Cashbox).Balance);
        Assert.All(Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items, x =>
        {
            Assert.Equal(pending.Id, x.CashboxHandoverId);
            Assert.False(x.IsHandedToAccountant);
        });
        Assert.Equal(ErrorType.Forbidden, (await f.Service.ConfirmCashboxHandoverV2Async("self", pending.Id, CashboxConfirmation(pending), ct)).Error.Type);
        f.User.UserId = Guid.NewGuid();
        var confirmed = Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm", pending.Id, CashboxConfirmation(pending), ct));
        Assert.Equal(JahezCashboxHandoverStatus.Confirmed, confirmed.Status);
        Assert.Equal(f.User.UserId, confirmed.AccountantUserId);
        Assert.NotNull(confirmed.ConfirmedAtUtc);
        Assert.All(Success(await f.Service.FundsV2Async(ct)), x =>
        {
            Assert.Equal(0, x.Receipts); Assert.Equal(0, x.Disbursements); Assert.Equal(0, x.Balance);
        });
        Assert.Equal(4, await f.Db.JahezMoneyMovements.CountAsync(ct));
        Assert.Equal(originalLedgerCount, await f.Db.JahezLedgerEntries.CountAsync(ct));
        Assert.All(Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items, x => Assert.True(x.IsHandedToAccountant));
        Assert.Single(Success(await f.Service.CashboxHandoversV2Async(1, 50, ct)).Items);
        Assert.Equal(confirmed.Id, Success(await f.Service.CashboxHandoverV2Async(confirmed.Id, ct)).Id);
        Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm", pending.Id, CashboxConfirmation(pending), ct));
        Assert.Equal(ErrorType.Conflict, (await f.Service.ConfirmCashboxHandoverV2Async("again", pending.Id, CashboxConfirmation(confirmed), ct)).Error.Type);
        Assert.Equal(ErrorType.Conflict, (await f.Service.SubmitCashboxHandoverV2Async("empty", request, ct)).Error.Type);
        Assert.False(f.Db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task V2CashboxConfirmationDoesNotClearMoneyRecordedAfterSubmission()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var sessionId = await SeedCashboxMovements(f);
        var pending = Success(await f.Service.SubmitCashboxHandoverV2Async("submit", new(new(2026, 10, 6), "تسليم"), ct));
        Success(await f.Service.RecordFeeV2Async("later", new(sessionId, 10, JahezMoneyFund.Cashbox, "later-receipt", "قبض لاحق"), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm", pending.Id, CashboxConfirmation(pending), ct));
        var funds = Success(await f.Service.FundsV2Async(ct));
        Assert.Equal(10, funds.Single(x => x.Fund == JahezMoneyFund.Cashbox).Balance);
        Assert.Equal(0, funds.Single(x => x.Fund == JahezMoneyFund.Bank).Balance);
        Assert.Equal(0, funds.Single(x => x.Fund == JahezMoneyFund.PetroApp).Balance);
        var rows = Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items;
        Assert.Equal(4, rows.Count(x => x.IsHandedToAccountant));
        Assert.Null(rows.Single(x => x.Reference == "later-receipt").CashboxHandoverId);
        var next = Success(await f.Service.SubmitCashboxHandoverV2Async("next", new(new(2026, 10, 6), "تسليم لاحق"), ct));
        Assert.Equal(1, next.MovementCount); Assert.Equal(10, next.CashboxAmount);
    }

    [Fact]
    public async Task V2CashboxRequiresExactFundAmountsPermissionsAndUnchangedSnapshot()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        await SeedCashboxMovements(f);
        var request = new JahezCashboxHandoverCreate(new(2026, 10, 6), "تسليم");
        Assert.Equal(ErrorType.Validation, (await f.Service.SubmitCashboxHandoverV2Async("future", request with { BusinessDate = new(2026, 10, 7) }, ct)).Error.Type);
        Assert.Equal(ErrorType.Conflict, (await f.Service.SubmitCashboxHandoverV2Async("before", request with { BusinessDate = new(2026, 10, 5) }, ct)).Error.Type);
        f.Permissions.Denied = PermissionKeys.Jahez.CashboxSubmit;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.SubmitCashboxHandoverV2Async("submit", request, ct)).Error.Type);
        f.Permissions.Denied = null;
        var pending = Success(await f.Service.SubmitCashboxHandoverV2Async("submit", request, ct));
        Assert.Equal(ErrorType.Conflict, (await f.Service.SubmitCashboxHandoverV2Async("second", request, ct)).Error.Type);
        Assert.Equal(ErrorType.Conflict, (await f.Service.SubmitCashboxHandoverV2Async("submit", request with { Reason = "different" }, ct)).Error.Type);
        f.Permissions.Denied = PermissionKeys.Jahez.CashboxSubmit;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.SubmitCashboxHandoverV2Async("submit", request, ct)).Error.Type);
        f.Permissions.Denied = null;
        f.User.UserId = Guid.NewGuid();
        var confirmation = CashboxConfirmation(pending);
        // Equal grand totals cannot hide a discrepancy between funds.
        Assert.Equal(ErrorType.Validation, (await f.Service.ConfirmCashboxHandoverV2Async("mismatch", pending.Id,
            confirmation with { CashboxAmount = 51, PetroAppAmount = 74 }, ct)).Error.Type);
        Assert.Equal(ErrorType.Conflict, (await f.Service.ConfirmCashboxHandoverV2Async("stale", pending.Id,
            confirmation with { RowVersion = "stale" }, ct)).Error.Type);
        f.Permissions.Denied = PermissionKeys.Jahez.CashboxConfirm;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.ConfirmCashboxHandoverV2Async("confirm", pending.Id, confirmation, ct)).Error.Type);
        f.Permissions.Denied = null;
        Assert.Equal(JahezCashboxHandoverStatus.Pending, Success(await f.Service.CashboxHandoverV2Async(pending.Id, ct)).Status);
        Assert.Equal(50, Success(await f.Service.FundsV2Async(ct)).Single(x => x.Fund == JahezMoneyFund.Cashbox).Balance);
        Assert.False(f.Db.ChangeTracker.HasChanges());
        Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm", pending.Id, confirmation, ct));
        f.Permissions.Denied = PermissionKeys.Jahez.CashboxConfirm;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.ConfirmCashboxHandoverV2Async("confirm", pending.Id, confirmation, ct)).Error.Type);
    }

    [Fact]
    public async Task V2CashboxCancellationReleasesMovementsAndPaymentMethodLocks()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var sessionId = await SeedCashboxMovements(f);
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(sessionId, -125), ct));
        var paid = Success(await f.Service.ExecuteOperationAsync("pay", draft.Id, ct));
        var pending = Success(await f.Service.SubmitCashboxHandoverV2Async("submit", new(new(2026, 10, 6), "تسليم"), ct));
        var correction = new JahezOperationPaymentMethodUpdate(JahezMoneyFund.Bank, "corrected", "تصحيح", Convert.ToBase64String(paid.RowVersion));
        Assert.Equal(ErrorType.Conflict, (await f.Service.UpdateOperationPaymentMethodAsync("edit", paid.Id, correction, ct)).Error.Type);
        var submitter = f.User.UserId;
        f.User.UserId = Guid.NewGuid();
        var cancel = new JahezCashboxHandoverCancel("إلغاء وإعادة المطابقة", Convert.ToBase64String(pending.RowVersion));
        Assert.Equal(ErrorType.Forbidden, (await f.Service.CancelCashboxHandoverV2Async("cancel", pending.Id, cancel, ct)).Error.Type);
        f.User.UserId = submitter;
        var cancelled = Success(await f.Service.CancelCashboxHandoverV2Async("cancel", pending.Id, cancel, ct));
        Assert.Equal(JahezCashboxHandoverStatus.Cancelled, cancelled.Status);
        Assert.All(Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items, x => Assert.Null(x.CashboxHandoverId));
        var changed = Success(await f.Service.UpdateOperationPaymentMethodAsync("edit", paid.Id, correction, ct));
        var replacement = Success(await f.Service.SubmitCashboxHandoverV2Async("replacement", new(new(2026, 10, 6), "تسليم بديل"), ct));
        Assert.Equal(5, replacement.MovementCount);
        Assert.Equal(50, replacement.CashboxAmount); Assert.Equal(-70, replacement.BankAmount);
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm", replacement.Id, CashboxConfirmation(replacement), ct));
        Assert.Equal(ErrorType.Conflict, (await f.Service.UpdateOperationPaymentMethodAsync("edit-confirmed", paid.Id,
            correction with { Fund = JahezMoneyFund.Cashbox, RowVersion = Convert.ToBase64String(changed.RowVersion) }, ct)).Error.Type);
        Assert.All(Success(await f.Service.FundsV2Async(ct)), x => Assert.Equal(0, x.Balance));
        Assert.Equal(10, await f.Db.JahezCashboxHandoverMovements.CountAsync(ct));
    }

    [Fact]
    public async Task V2CashboxProtectsSnapshotsAndConfirmedHandoversFromEditsOrDeletion()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        await SeedCashboxMovements(f);
        var pending = Success(await f.Service.SubmitCashboxHandoverV2Async("submit", new(new(2026, 10, 6), "تسليم"), ct));
        var tracked = await f.Db.JahezCashboxHandovers.SingleAsync(ct);
        tracked.CashboxAmount += 1;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
        f.Db.ChangeTracker.Clear();
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm", pending.Id, CashboxConfirmation(pending), ct));
        tracked = await f.Db.JahezCashboxHandovers.SingleAsync(ct);
        f.Db.Remove(tracked);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
        f.Db.ChangeTracker.Clear();
        var link = await f.Db.JahezCashboxHandoverMovements.FirstAsync(ct);
        f.Db.Remove(link);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
    }

    [Fact]
    public void V2OutstandingFundsQueryTranslatesToSql()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UnusedCashbox;Trusted_Connection=True").Options);
        var service = new JahezWorkflowService(db, new MutableUser(), new TestPermissions(), new MutableClock { UtcNow = At(6) });
        var query = (IQueryable<JahezMoneyMovement>)typeof(JahezWorkflowService)
            .GetMethod("OutstandingMoneyMovements", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, null)!;
        var grouped = query.GroupBy(x => new { x.Fund, x.Direction }).Select(g => new { g.Key.Fund, g.Key.Direction, Amount = g.Sum(x => x.Amount) });
        var sql = grouped.ToQueryString();
        Assert.Contains("NOT EXISTS", sql); Assert.Contains("CashboxHandovers", sql); Assert.Contains("GROUP BY", sql);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static JahezCashboxHandoverConfirm CashboxConfirmation(JahezCashboxHandover h) =>
        new(h.CashboxAmount, h.PetroAppAmount, h.BankAmount, "تم الاستلام والمطابقة", Convert.ToBase64String(h.RowVersion));

    private static async Task<Guid> SeedCashboxMovements(Fixture f)
    {
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("cashbox");
        var session = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        (JahezMoneyFund Fund, JahezMoneyDirection Direction, decimal Amount)[] data =
        [
            (JahezMoneyFund.Cashbox, JahezMoneyDirection.Receipt, 100),
            (JahezMoneyFund.Cashbox, JahezMoneyDirection.Disbursement, 50),
            (JahezMoneyFund.PetroApp, JahezMoneyDirection.Receipt, 75),
            (JahezMoneyFund.Bank, JahezMoneyDirection.Disbursement, 20)
        ];
        f.Db.AddRange(data.Select(x => new JahezMoneyMovement
        {
            HandoverId = session.Id, RiderProfileId = f.RiderId, Fund = x.Fund, Direction = x.Direction,
            Amount = x.Amount, Purpose = x.Fund == JahezMoneyFund.PetroApp ? JahezMoneyPurpose.Fuel : JahezMoneyPurpose.Settlement,
            ActorUserId = f.User.UserId!.Value, OccurredAtUtc = At(6), Reference = "cashbox-test", Reason = "financial reconciliation"
        }));
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();
        return session.Id;
    }
}
