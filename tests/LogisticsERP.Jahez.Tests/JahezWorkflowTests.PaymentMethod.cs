using System.Text.Json;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Theory]
    [InlineData(false, JahezMoneyFund.Cashbox, JahezMoneyFund.Bank)]
    [InlineData(false, JahezMoneyFund.Bank, JahezMoneyFund.Cashbox)]
    [InlineData(true, JahezMoneyFund.Cashbox, JahezMoneyFund.Bank)]
    [InlineData(true, JahezMoneyFund.Bank, JahezMoneyFund.Cashbox)]
    public async Task V2PaymentMethodMovesPayoutBetweenFundsWithAudit(bool sql, JahezMoneyFund from, JahezMoneyFund to)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("payment-method");
        var handover = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(handover.Id, -125, fund: from), ct));
        var paid = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var original = await f.Db.JahezMoneyMovements.AsNoTracking().SingleAsync(ct);
        var originalLedger = await f.Db.JahezLedgerEntries.AsNoTracking().CountAsync(ct);
        var request = new JahezOperationPaymentMethodUpdate(to, "corrected-payment", "تصحيح طريقة الصرف", Convert.ToBase64String(paid.RowVersion));
        var updated = Success(await f.Service.UpdateOperationPaymentMethodAsync("edit", paid.Id, request, ct));
        Assert.Equal(to, updated.Fund); Assert.Equal(request.PaymentReference, updated.PaymentReference);
        Assert.Equal(paid.Net, updated.Net); Assert.Equal(paid.Commission, updated.Commission);
        Assert.Equal(paid.Remaining, updated.Remaining); Assert.Equal(paid.ExecutedAtUtc, updated.ExecutedAtUtc);
        Assert.Equal(paid.ExecutedByUserId, updated.ExecutedByUserId); Assert.Equal(paid.Reason, updated.Reason);
        var movement = Assert.Single(await f.Db.JahezMoneyMovements.AsNoTracking().ToArrayAsync(ct));
        Assert.Equal(original.Id, movement.Id); Assert.Equal(original.Amount, movement.Amount);
        Assert.Equal(original.OccurredAtUtc, movement.OccurredAtUtc); Assert.Equal(original.ActorUserId, movement.ActorUserId);
        Assert.Equal(original.CreatedAtUtc, movement.CreatedAtUtc); Assert.Equal(original.Reason, movement.Reason);
        Assert.Equal(to, movement.Fund); Assert.Equal(request.PaymentReference, movement.Reference);
        var funds = Success(await f.Service.FundsV2Async(ct));
        Assert.Equal(0, funds.Single(x => x.Fund == from).Balance);
        Assert.Equal(-50, funds.Single(x => x.Fund == to).Balance);
        var correction = await f.Db.JahezLedgerEntries.AsNoTracking().SingleAsync(x => x.Kind == JahezLedgerKind.StatusChange && x.Reason == request.Reason, ct);
        Assert.Equal(0, correction.Amount);
        using var evidence = JsonDocument.Parse(correction.CalculationJson!);
        Assert.Equal(paid.Id, evidence.RootElement.GetProperty("OperationId").GetGuid());
        Assert.Equal((int)from, evidence.RootElement.GetProperty("PreviousFund").GetInt32());
        Assert.Equal((int)to, evidence.RootElement.GetProperty("Fund").GetInt32());
        Assert.Contains(await f.Db.Set<AuditEntry>().AsNoTracking().ToArrayAsync(ct), x =>
            x.EntityId == original.Id && x.Action == "Updated" && x.BeforeJson!.Contains("Fund", StringComparison.Ordinal));
        Success(await f.Service.UpdateOperationPaymentMethodAsync("edit", paid.Id, request, ct));
        Assert.Equal(originalLedger + 1, await f.Db.JahezLedgerEntries.CountAsync(ct));
        var changedKey = await f.Service.UpdateOperationPaymentMethodAsync("edit", paid.Id, request with { Fund = from }, ct);
        Assert.True(changedKey.IsFailure); Assert.Equal(ErrorType.Conflict, changedKey.Error.Type);
        if (sql)
        {
            Assert.NotEqual(paid.RowVersion, updated.RowVersion);
            Assert.True((await f.Service.UpdateOperationPaymentMethodAsync("stale", paid.Id, request with { Fund = from }, ct)).IsFailure);
        }
        var back = Success(await f.Service.UpdateOperationPaymentMethodAsync("back", paid.Id,
            request with { Fund = from, RowVersion = Convert.ToBase64String(updated.RowVersion) }, ct));
        Assert.Equal(from, back.Fund);
        Assert.Equal(-50, Success(await f.Service.FundsV2Async(ct)).Single(x => x.Fund == from).Balance);
        Assert.Equal(0, Success(await f.Service.FundsV2Async(ct)).Single(x => x.Fund == to).Balance);
    }

    [Fact]
    public async Task V2PaymentMethodRejectsInvalidInputsAndRevokedPermissionsWithoutWrites()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("payment-method-invalid");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, -125), ct));
        var request = new JahezOperationPaymentMethodUpdate(JahezMoneyFund.Bank, "bank-ref", "تصحيح", Convert.ToBase64String(draft.RowVersion));
        Assert.True((await f.Service.UpdateOperationPaymentMethodAsync("ready", draft.Id, request, ct)).IsFailure);
        var paid = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        request = request with { RowVersion = Convert.ToBase64String(paid.RowVersion) };
        var count = await f.Db.JahezLedgerEntries.CountAsync(ct);
        foreach (var invalid in new[] { request with { Fund = JahezMoneyFund.PetroApp }, request with { Fund = (JahezMoneyFund)99 },
            request with { Reason = " " }, request with { Reason = new string('x', 2001) }, request with { PaymentReference = " " },
            request with { PaymentReference = new string('x', 501) }, request with { RowVersion = "stale" } })
            Assert.True((await f.Service.UpdateOperationPaymentMethodAsync(Guid.NewGuid().ToString(), paid.Id, invalid, ct)).IsFailure);
        Assert.Equal(count, await f.Db.JahezLedgerEntries.CountAsync(ct));
        Assert.Equal(JahezMoneyFund.Cashbox, (await f.Db.JahezMoneyMovements.AsNoTracking().SingleAsync(ct)).Fund);
        Assert.False(f.Db.ChangeTracker.HasChanges());
        foreach (var denied in new[] { JahezWorkflowPermissions.Settle, JahezWorkflowPermissions.Disburse })
        {
            f.Permissions.Denied = denied;
            var result = await f.Service.UpdateOperationPaymentMethodAsync("edit", paid.Id, request, ct);
            Assert.True(result.IsFailure); Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        }
        f.Permissions.Denied = null;
        Success(await f.Service.UpdateOperationPaymentMethodAsync("edit", paid.Id, request, ct));
        f.Permissions.Denied = JahezWorkflowPermissions.Disburse;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.UpdateOperationPaymentMethodAsync("edit", paid.Id, request, ct)).Error.Type);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(-75)]
    public async Task V2PaymentMethodRejectsReceiptsAndZeroSettlements(decimal net)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("payment-method-ineligible");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, net, fund: net == -75 ? null : JahezMoneyFund.Cashbox), ct));
        var paid = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var result = await f.Service.UpdateOperationPaymentMethodAsync("edit", paid.Id,
            new(JahezMoneyFund.Bank, "bank-ref", "تصحيح", Convert.ToBase64String(paid.RowVersion)), ct);
        Assert.True(result.IsFailure); Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task V2PaymentMethodExceptionsKeepExecutedAmountsAndHistoryProtected()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("payment-method-protected");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, -125), ct));
        Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var operation = await f.Db.JahezOperations.SingleAsync(ct);
        operation.Net = -200;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
        f.Db.ChangeTracker.Clear();
        var movement = await f.Db.JahezMoneyMovements.SingleAsync(ct);
        movement.Fund = JahezMoneyFund.Bank; movement.Amount += 1;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
        f.Db.ChangeTracker.Clear();
        movement = await f.Db.JahezMoneyMovements.SingleAsync(ct);
        f.Db.Remove(movement);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
    }
}
