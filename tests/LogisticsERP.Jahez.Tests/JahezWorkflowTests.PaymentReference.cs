using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Theory]
    [InlineData(100)]
    [InlineData(-125)]
    public async Task V2PaymentReferenceIsRequiredBeforeSavingMoneyOperations(decimal net)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("payment-reference-create");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var input = Settle(h.Id, net);

        // The UI can calculate the amount and direction before collecting payment evidence.
        var preview = Success(await f.Service.PreviewOperationAsync(input with { PaymentReference = null }, ct));
        Assert.NotEqual(0, preview.Remaining);
        foreach (var reference in new[] { null, "", " \t\r\n", new string('x', 501) })
        {
            var result = await f.Service.CreateOperationAsync("draft", input with { PaymentReference = reference }, ct);
            Assert.True(result.IsFailure);
            Assert.Equal("jahez.invalid_request", result.Error.Code);
            Assert.Empty(await f.Db.JahezOperations.ToArrayAsync(ct));
            Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
            Assert.False(f.Db.ChangeTracker.HasChanges());
        }

        // A failed validation does not consume the key. The 500-character boundary is valid.
        input = input with { PaymentReference = new string('x', 500) };
        var operation = Success(await f.Service.CreateOperationAsync("draft", input, ct));
        Success(await f.Service.ExecuteOperationAsync("execute", operation.Id, ct));
        var movement = Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Equal(input.PaymentReference, movement.Reference);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(-125)]
    public async Task V2PaymentReferenceCannotBeClearedByUpdatingMoneyOperations(decimal net)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("payment-reference-update");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var input = Settle(h.Id, net);
        var operation = Success(await f.Service.CreateOperationAsync("draft", input, ct));
        var version = Convert.ToBase64String(operation.RowVersion);
        foreach (var reference in new[] { null, "", " \t\r\n", new string('x', 501) })
        {
            var result = await f.Service.UpdateOperationAsync("update", operation.Id,
                new(input with { PaymentReference = reference }, version), ct);
            Assert.True(result.IsFailure);
            Assert.Equal("jahez.invalid_request", result.Error.Code);
            var saved = await f.Db.JahezOperations.AsNoTracking().SingleAsync(ct);
            Assert.Equal(input.PaymentReference, saved.PaymentReference);
            Assert.Equal(version, Convert.ToBase64String(saved.RowVersion));
            Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
            Assert.False(f.Db.ChangeTracker.HasChanges());
        }

        var updated = Success(await f.Service.UpdateOperationAsync("update", operation.Id,
            new(input with { PaymentReference = "corrected-reference" }, version), ct));
        Success(await f.Service.ExecuteOperationAsync("execute", updated.Id, ct));
        Assert.Equal("corrected-reference", (await f.Db.JahezMoneyMovements.SingleAsync(ct)).Reference);
    }

    [Fact]
    public async Task V2PaymentReferenceCanBeRepairedForAnExistingReadyOperation()
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("payment-reference-repair");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var input = Settle(h.Id, 100);
        var operation = Success(await f.Service.CreateOperationAsync("draft", input, ct));
        // Reproduce a ready operation saved before reference validation was added.
        var legacy = await f.Db.JahezOperations.SingleAsync(ct);
        legacy.PaymentReference = null;
        await f.Db.SaveChangesAsync(ct);
        var version = Convert.ToBase64String(legacy.RowVersion);
        f.Db.ChangeTracker.Clear();

        var failed = await f.Service.ExecuteOperationAsync("execute", operation.Id, ct);
        Assert.True(failed.IsFailure);
        Assert.Equal("jahez.invalid_request", failed.Error.Code);
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Equal(JahezOperationStatus.Ready, (await f.Db.JahezOperations.AsNoTracking().SingleAsync(ct)).Status);

        Success(await f.Service.UpdateOperationAsync("repair", operation.Id,
            new(input with { PaymentReference = "receipt-repaired" }, version), ct));
        Success(await f.Service.ExecuteOperationAsync("execute", operation.Id, ct));
        Assert.Equal("receipt-repaired", (await f.Db.JahezMoneyMovements.SingleAsync(ct)).Reference);
    }

    [Theory]
    [InlineData(JahezOperationMode.Withdraw, 0, false)]
    [InlineData(JahezOperationMode.Settle, -75, false)]
    [InlineData(JahezOperationMode.SettleAndWithdraw, 100, true)]
    public async Task V2PaymentReferenceIsOptionalWithoutAnImmediateMoneyMovement(
        JahezOperationMode mode, decimal net, bool transfer)
    {
        await using var f = await Fixture.Create();
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("payment-reference-no-movement");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var input = Settle(h.Id, net, mode, null, transfer);
        var operation = Success(await f.Service.CreateOperationAsync("draft", input, ct));
        var updated = Success(await f.Service.UpdateOperationAsync("update", operation.Id,
            new(input, Convert.ToBase64String(operation.RowVersion)), ct));
        Assert.Null(updated.PaymentReference);
        if (!transfer)
            Success(await f.Service.ExecuteOperationAsync("execute", updated.Id, ct));
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
    }
}
