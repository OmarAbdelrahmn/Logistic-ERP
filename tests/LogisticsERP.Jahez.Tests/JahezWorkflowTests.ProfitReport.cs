using System.Reflection;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProfitReportUsesReceiptAndExecutionDatesAndSurvivesAccountantHandover(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql);
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("profit-dates");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 50, "initial"), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, -75, fund: null), ct));
        f.Clock.UtcNow = At(7).ToUniversalTime();
        Success(await f.Service.RecordFeeV2Async("fee", new(h.Id, 25, JahezMoneyFund.Cashbox, "partial", "قبض جزئي"), ct));
        var executed = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        Assert.Equal(0, executed.Remaining);
        // A ready operation must not contribute any commission.
        Success(await f.Service.CreateOperationAsync("next", Settle(h.Id, 100) with { ThroughDate = new(2026, 10, 7) }, ct));

        var day6 = Success(await f.Service.ProfitReportAsync(new(2026, 10, 6), new(2026, 10, 6), ct));
        Assert.Equal(50, day6.AccountOpeningFeesTotal);
        Assert.Equal(0, day6.SettlementCommissionTotal);
        var day7 = Success(await f.Service.ProfitReportAsync(new(2026, 10, 7), new(2026, 10, 7), ct));
        Assert.Equal(25, day7.AccountOpeningFeesTotal);
        Assert.Equal(75, day7.SettlementCommissionTotal);
        Assert.Equal(100, day7.TotalProfit);
        var before = Success(await f.Service.ProfitReportAsync(new(2026, 10, 6), new(2026, 10, 7), ct));
        Assert.Equal(150, before.TotalProfit);

        var handover = Success(await f.Service.SubmitCashboxHandoverV2Async("submit", new(new(2026, 10, 7), "تسليم الأموال"), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm", handover.Id, CashboxConfirmation(handover), ct));
        f.Db.ChangeTracker.Clear();
        Assert.Equal(before, Success(await f.Service.ProfitReportAsync(new(2026, 10, 6), new(2026, 10, 7), ct)));
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(100, false, false)]
    [InlineData(-125, false, false)]
    [InlineData(-75, false, false)]
    [InlineData(100, true, false)]
    [InlineData(100, false, true)]
    public async Task ProfitReportCountsSavedCommissionOnceRegardlessOfSettlementPayment(decimal net, bool approvedOverride, bool transfer)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("profit-commission");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 50, "fee"), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft",
            Settle(h.Id, net, JahezOperationMode.SettleAndWithdraw, transfer || net == -75 ? null : JahezMoneyFund.Cashbox, transfer), ct));
        if (approvedOverride)
        {
            var adjustment = Success(await f.Service.CreateWorkflowRequestAsync("adjust",
                new(h.Id, JahezApprovalKind.Adjustment, "تعديل العمولة", OperationId: draft.Id,
                    AdjustmentTarget: JahezAdjustmentTarget.Settlement, ProposedCommission: 50), ct));
            f.User.UserId = Guid.NewGuid();
            Success(await f.Service.DecideWorkflowRequestAsync("approve", adjustment.Request.Id, new(true, "اعتماد"), ct));
        }
        if (transfer)
        {
            var request = Success(await f.Service.CreateWorkflowRequestAsync("transfer",
                new(h.Id, JahezApprovalKind.ResetAccount, "ترحيل", OperationId: draft.Id), ct));
            f.User.UserId = Guid.NewGuid();
            Success(await f.Service.DecideWorkflowRequestAsync("approve-transfer", request.Request.Id, new(true, "اعتماد"), ct));
        }
        else
        {
            Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
            // A command replay must not add profit a second time.
            Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        }
        var report = Success(await f.Service.ProfitReportAsync(ct: ct));
        Assert.Equal(50, report.AccountOpeningFeesTotal);
        Assert.Equal(approvedOverride ? 50 : 75, report.SettlementCommissionTotal);
        Assert.Equal(approvedOverride ? 100 : 125, report.TotalProfit);
        if (transfer)
        {
            var debt = await f.Db.JahezTransferredDebts.SingleAsync(ct);
            Success(await f.Service.RepayTransferredDebtAsync("repay", debt.Id,
                new(JahezMoneyFund.Cashbox, "debt-payment", "سداد"), ct));
            Assert.Equal(report, Success(await f.Service.ProfitReportAsync(ct: ct)));
        }
    }

    [Fact]
    public async Task ProfitReportIncludesBothRiyadhBoundariesAndAllRowsWithoutOtherMoneyOrDrafts()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var from = At(6, 0).ToUniversalTime();
        var to = At(7, 0).ToUniversalTime();
        var h = Guid.NewGuid();
        JahezMoneyMovement Fee(DateTimeOffset at, decimal amount) => new()
        {
            HandoverId = h, RiderProfileId = f.RiderId, Fund = JahezMoneyFund.Cashbox,
            Direction = JahezMoneyDirection.Receipt, Purpose = JahezMoneyPurpose.AccountFee,
            Amount = amount, OccurredAtUtc = at, Reference = "profit-seed", Reason = "اختبار"
        };
        JahezOperation Settlement(DateTimeOffset at, decimal commission) => new()
        {
            HandoverId = h, Mode = JahezOperationMode.Settle, Status = JahezOperationStatus.Executed,
            ExecutedAtUtc = at, EffectiveAtUtc = At(2), ThroughDate = new(2026, 10, 2), Commission = commission
        };
        f.Db.AddRange(Fee(from.AddTicks(-1), 999), Fee(to, 999), Fee(from, 10), Fee(to.AddTicks(-1), 20));
        f.Db.AddRange(Settlement(from.AddTicks(-1), 999), Settlement(to, 999), Settlement(from, 30), Settlement(to.AddTicks(-1), 40));
        for (var i = 0; i < 120; i++)
        {
            f.Db.Add(Fee(from.AddHours(1), 0.25m));
            f.Db.Add(Settlement(from.AddHours(1), 0.50m));
        }
        foreach (var purpose in new[] { JahezMoneyPurpose.Settlement, JahezMoneyPurpose.Fuel, JahezMoneyPurpose.TransferredDebtRepayment })
        {
            var movement = Fee(from, 999); movement.Purpose = purpose; f.Db.Add(movement);
        }
        var disbursement = Fee(from, 999); disbursement.Direction = JahezMoneyDirection.Disbursement; f.Db.Add(disbursement);
        var withdraw = Settlement(from, 999); withdraw.Mode = JahezOperationMode.Withdraw; f.Db.Add(withdraw);
        foreach (var status in new[] { JahezOperationStatus.Ready, JahezOperationStatus.UnderReview })
        {
            var draft = Settlement(from, 999); draft.Status = status; f.Db.Add(draft);
        }
        var unexecuted = Settlement(from, 999); unexecuted.ExecutedAtUtc = null; f.Db.Add(unexecuted);
        var deleted = Settlement(from, 999); deleted.IsDeleted = true; f.Db.Add(deleted);
        await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();

        var report = Success(await f.Service.ProfitReportAsync(new(2026, 10, 6), new(2026, 10, 6), ct));
        Assert.Equal(60, report.AccountOpeningFeesTotal);
        Assert.Equal(130, report.SettlementCommissionTotal);
        Assert.Equal(190, report.TotalProfit);
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ProfitReportDefaultsEmptyTotalsValidatesDatesAndRequiresFinancialReportPermission()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        // UTC still says October, but Riyadh has entered November.
        f.Clock.UtcNow = new(2026, 10, 31, 22, 0, 0, TimeSpan.Zero);
        var empty = Success(await f.Service.ProfitReportAsync(ct: ct));
        Assert.Equal(new DateOnly(2026, 11, 1), empty.FromDate);
        Assert.Equal(empty.FromDate, empty.ToDate);
        Assert.Equal(0, empty.AccountOpeningFeesTotal);
        Assert.Equal(0, empty.SettlementCommissionTotal);
        Assert.Equal(0, empty.TotalProfit);
        Assert.Equal(ErrorType.Validation, (await f.Service.ProfitReportAsync(new(2026, 11, 2), new(2026, 11, 1), ct)).Error.Type);
        Assert.Equal(ErrorType.Validation, (await f.Service.ProfitReportAsync(DateOnly.MinValue, DateOnly.MinValue, ct)).Error.Type);
        Assert.Equal(ErrorType.Validation, (await f.Service.ProfitReportAsync(DateOnly.MaxValue, DateOnly.MaxValue, ct)).Error.Type);
        f.Permissions.Denied = JahezTreasuryPermissions.ReportsRead;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.ProfitReportAsync(ct: ct)).Error.Type);
        f.Permissions.Denied = null; f.Permissions.RequiredScope = Guid.NewGuid();
        Assert.Equal(ErrorType.Forbidden, (await f.Service.ProfitReportAsync(ct: ct)).Error.Type);
        var permission = typeof(JahezWorkflowController).GetMethod(nameof(JahezWorkflowController.ProfitReport))!
            .GetCustomAttribute<RequirePermissionAttribute>();
        Assert.Equal("permission:" + JahezTreasuryPermissions.ReportsRead, permission!.Policy);
    }
}
