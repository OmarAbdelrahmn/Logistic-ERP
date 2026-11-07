using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Infrastructure.Jahez;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    private static JahezWorkflowService ReportingTreasury(Fixture f) =>
        new(f.Db, f.User, f.Permissions, f.Clock, configuration: new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Features:JahezTreasuryReportsOnly"] = "true" }).Build());

    [Fact]
    public async Task ReportingTreasuryRecordsUnfundedSettlementAndCorrectionsWithoutBlockingExecution()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var service = ReportingTreasury(f);
        var account = await f.Account("report-only-unfunded");
        var handover = Success(await service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "تسليم", 0), ct));
        var draft = Success(await service.CreateOperationAsync("draft", Settle(handover.Id, -100, fund: JahezMoneyFund.Bank), ct));
        var executed = Success(await service.ExecuteOperationAsync("execute", draft.Id, ct));
        var replay = Success(await service.ExecuteOperationAsync("execute", draft.Id, ct));
        Assert.Equal(executed.Id, replay.Id);
        var movement = await f.Db.JahezMoneyMovements.AsNoTracking().SingleAsync(ct);
        var original = await f.Db.Set<TreasuryPosting>().AsNoTracking().SingleAsync(ct);
        Assert.Equal(-movement.Amount, original.Amount);
        Assert.True(original.Amount < 0);
        Success(await service.UpdateOperationPaymentMethodAsync("correct", executed.Id,
            new(JahezMoneyFund.Cashbox, "cash-correction", "تصحيح الجهة", Convert.ToBase64String(executed.RowVersion)), ct));
        var postings = await f.Db.Set<TreasuryPosting>().AsNoTracking().ToArrayAsync(ct);
        Assert.Equal(3, postings.Length);
        Assert.Single(postings, posting => posting.ReversesPostingId == original.Id);
        Assert.Equal(original.Amount, postings.Sum(posting => posting.Amount));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReportingTreasuryAllowsV2CashboxConfirmationAndProjectsItIntoTheJournal(bool negativeCustody)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var service = ReportingTreasury(f);
        var account = await f.Account("report-only-cashbox");
        var handover = Success(await service.HandoverAsync("start",
            new(account.Id, f.RiderId, At(1), "تسليم", negativeCustody ? 0 : 200, negativeCustody ? null : "fee-reference"), ct));
        if (negativeCustody)
        {
            var draft = Success(await service.CreateOperationAsync("draft", Settle(handover.Id, -100), ct));
            Success(await service.ExecuteOperationAsync("execute", draft.Id, ct));
        }
        var pending = Success(await service.SubmitCashboxHandoverV2Async("submit", new(new(2026, 10, 6), "تسليم العهدة"), ct));
        f.User.UserId = Guid.NewGuid();
        var confirmed = Success(await service.ConfirmCashboxHandoverV2Async("confirm", pending.Id,
            new(pending.CashboxAmount, pending.PetroAppAmount, pending.BankAmount, "مطابقة المحضر", Convert.ToBase64String(pending.RowVersion)), ct));
        Assert.Equal(JahezCashboxHandoverStatus.Confirmed, confirmed.Status);
        Assert.NotEmpty(await f.Db.Set<TreasuryHandoverLine>().Where(line => line.HandoverId == confirmed.Id).ToArrayAsync(ct));
        var balances = Success(await service.TreasuryBalancesAsync(ct));
        Assert.Equal(0, balances.Where(balance => balance.IsPosting && balance.Location == TreasuryLocation.Supervisor).Sum(balance => balance.Balance));
        Assert.Equal(pending.CashboxAmount, balances.Where(balance => balance.IsPosting && balance.Location == TreasuryLocation.Central).Sum(balance => balance.Balance));
    }

    [Fact]
    public async Task ReportingTreasuryStillChecksTheSettlementPermission()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var service = ReportingTreasury(f);
        var account = await f.Account("report-only-permissions");
        var handover = Success(await service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "تسليم", 0), ct));
        var draft = Success(await service.CreateOperationAsync("draft", Settle(handover.Id, -100), ct));
        f.Permissions.Denied = JahezWorkflowPermissions.Settle;
        var result = await service.ExecuteOperationAsync("execute", draft.Id, ct);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
    }
}
