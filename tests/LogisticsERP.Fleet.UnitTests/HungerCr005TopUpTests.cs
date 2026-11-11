using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Infrastructure.Files;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

// CR-005 phase 4: before/after screenshots, collected top-ups routed to the owner's collections, custody proposals.
public sealed partial class HungerFinanceTests
{
    private static async Task<HungerMoneyRequest> WithShots(Fixture f, HungerMoneyRequest request)
    {
        var ct = TestContext.Current.CancellationToken;
        var before = Value(await f.Service.UploadEvidenceAsync(new(f.Platform.Id, f.Account.Id, "WalletBefore", new(2026, 11, 1)), PixelImage(), ct));
        var after = Value(await f.Service.UploadEvidenceAsync(new(f.Platform.Id, f.Account.Id, "WalletAfter", new(2026, 11, 1)), PixelImage(), ct));
        return request with { EvidenceBeforeId = before.EvidenceFileId, EvidenceAfterId = after.EvidenceFileId };
    }

    // CR-005 funding chain: supervisor request -> accountant approval -> teller delivery.
    private static async Task FundCustody(Fixture f, decimal amount, string key)
    {
        var ct = TestContext.Current.CancellationToken;
        var request = Value(await f.Service.RequestMoneyAsync(f.Request("Funding", amount, key) with { AccountId = null, RiderProfileId = null }, ct));
        Value(await f.Service.DecideFundingAsync(request.EventId, true, new(null, f.Bank.Id), ct));
        Value(await f.Service.ExecuteMoneyRequestAsync(request.EventId, ct));
    }

    private static async Task TopUpFixture(Func<Fixture, CancellationToken, Task> body)
    {
        var root = TempRoot();
        try
        {
            await using var f = await Fixture.Create(intercept: true, files: new PrivateFileStorage(new TestHostEnvironment(root)), cr005: true);
            var ct = TestContext.Current.CancellationToken;
            await FundCustody(f, 1000, "fund");
            await body(f, ct);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public Task TopUpNeedsUnusedBeforeAndAfterScreenshotsOfTheSameAccount() => TopUpFixture(async (f, ct) =>
    {
        var plain = f.Request("TopUpAdvance", 100, "advance");
        Assert.True((await f.Service.PostMoneyAsync(plain, ct)).IsFailure);
        var shots = await WithShots(f, plain);
        Assert.True((await f.Service.PostMoneyAsync(shots with { EvidenceAfterId = null }, ct)).IsFailure);
        Assert.True((await f.Service.PostMoneyAsync(shots with { EvidenceBeforeId = shots.EvidenceAfterId, EvidenceAfterId = shots.EvidenceBeforeId }, ct)).IsFailure);
        Assert.True((await f.Service.PostMoneyAsync(shots with { EvidenceAfterId = shots.EvidenceBeforeId }, ct)).IsFailure);
        Assert.Equal(1000, await Custody(f));

        var posted = Value(await f.Service.PostMoneyAsync(shots, ct));
        Assert.Equal(posted.EventId, Value(await f.Service.PostMoneyAsync(shots, ct)).EventId);
        f.Db.ChangeTracker.Clear();
        Assert.All(await f.Db.Set<HungerEvidenceFile>().ToArrayAsync(ct), x => Assert.Equal(posted.EventId, x.FinancialEventId));
        // A screenshot proves one top-up only.
        Assert.True((await f.Service.PostMoneyAsync(shots with { IdempotencyKey = "again" }, ct)).IsFailure);
        Assert.Equal(900, await Custody(f));
    });

    [Fact]
    public Task CollectedTopUpDebitsCustodyAndRoutesCashToOwnerCollectionsOutsideRemittances() => TopUpFixture(async (f, ct) =>
    {
        var topUp = Value(await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpCollected", 100, "collected")), ct));
        Assert.Equal(0, topUp.RiderBalanceChange);
        var report = Value(await f.Service.FinanceReportAsync(f.Platform.Id, f.Supervisor.Id, 1, 50, ct));
        Assert.Equal(900, report.CustodyBalance); Assert.Equal(0, report.PendingRemittances);
        f.Db.ChangeTracker.Clear();
        var row = await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == topUp.EventId, ct);
        Assert.Equal("OwnerVault", row.DestinationChannel); Assert.Equal("Custody", row.Role);
        // Only custody -> wallet is posted; the rider's cash never touches a company sub-account.
        var postings = await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().Where(p => p.SourceEventId == topUp.EventId).ToArrayAsync(ct);
        Assert.Equal(2, postings.Length); Assert.Equal(0, postings.Sum(p => p.Amount));
        Assert.Contains(postings, p => p.SubAccountId == row.FromSubAccountId && p.Amount == -100);
        Assert.True((await f.Service.SubmitRemittanceAsync(Batch(f, topUp.EventId), ct)).IsFailure);
        Assert.Empty(Value(await f.Service.PendingCollectionsAsync(f.Platform.Id, f.Supervisor.Id, 1, 50, ct)).Sources);

        var receipt = await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Kind == "OwnerCollectionReceipt", ct);
        Assert.Equal("PendingReceipt", receipt.Status); Assert.Equal(100, receipt.Amount); Assert.Equal(topUp.EventId, receipt.RelatedEventId);
        var confirm = new HungerOwnerCollectionConfirmation("owner-confirm", new(2026, 11, 2), "handed to owner", "Owner");
        Assert.True((await f.Service.ConfirmOwnerCollectionAsync(receipt.Id, confirm with { Capacity = "Supervisor" }, ct)).IsFailure);
        f.Permissions.Allowed = [PermissionKeys.Hunger.FinanceManage, PermissionKeys.Hunger.RemittancesConfirm];
        Assert.Equal("hunger.forbidden", (await f.Service.ConfirmOwnerCollectionAsync(receipt.Id, confirm, ct)).Error.Code);
        f.Permissions.Allowed = null;
        var received = Value(await f.Service.ConfirmOwnerCollectionAsync(receipt.Id, confirm, ct));
        Assert.Equal("Received", received.Status);
        Assert.Equal(received, Value(await f.Service.ConfirmOwnerCollectionAsync(receipt.Id, confirm, ct)));
        Assert.True((await f.Service.ConfirmOwnerCollectionAsync(receipt.Id, confirm with { IdempotencyKey = "twice" }, ct)).IsFailure);
        f.Db.ChangeTracker.Clear();
        var audit = await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Kind == "OwnerCollectionConfirmation", ct);
        Assert.Equal(receipt.Id, audit.RelatedEventId); Assert.Contains("\"capacity\":\"Owner\"", audit.DetailsJson);
        Assert.Equal(postings.Length + 2, await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().CountAsync(ct));
        Assert.Equal(900, await Custody(f));
    });

    [Fact]
    public Task ReplacingARejectedCollectedTopUpMovesTheExpectedOwnerReceipt() => TopUpFixture(async (f, ct) =>
    {
        var original = Value(await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpCollected", 100, "collected")), ct));
        Value(await f.Service.RejectEventAsync(original.EventId, new("wrong amount"), ct));
        var replacement = Value(await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpCollected", 80, "fixed") with { SupersedesEventId = original.EventId }), ct));
        Assert.Equal("AwaitingSeal", replacement.Status);
        Assert.Single(await f.Db.Set<HungerFinancialEvent>().Where(e => e.Kind == "OwnerCollectionReceipt").ToArrayAsync(ct));
        Value(await f.Service.SealEventAsync(replacement.EventId, new(), ct));
        f.Db.ChangeTracker.Clear();
        var receipts = await f.Db.Set<HungerFinancialEvent>().Where(e => e.Kind == "OwnerCollectionReceipt").ToArrayAsync(ct);
        Assert.Equal("Cancelled", receipts.Single(r => r.RelatedEventId == original.EventId).Status);
        var live = receipts.Single(r => r.RelatedEventId == replacement.EventId);
        Assert.Equal("PendingReceipt", live.Status); Assert.Equal(80, live.Amount);
        Assert.Equal(920, await Custody(f));
        Assert.Equal(0, await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().SumAsync(p => p.Amount, ct));
    });

    [Fact]
    public Task ReplacementIsRefusedOnceTheOwnerConfirmedTheOriginalCash() => TopUpFixture(async (f, ct) =>
    {
        var original = Value(await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpCollected", 100, "collected")), ct));
        var receipt = await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Kind == "OwnerCollectionReceipt", ct);
        Value(await f.Service.ConfirmOwnerCollectionAsync(receipt.Id, new("owner-confirm", new(2026, 11, 2), "handed to owner", "Accountant"), ct));
        Value(await f.Service.RejectEventAsync(original.EventId, new("wrong amount"), ct));
        var replacement = Value(await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpCollected", 80, "fixed") with { SupersedesEventId = original.EventId }), ct));
        var refused = await f.Service.SealEventAsync(replacement.EventId, new(), ct);
        Assert.True(refused.IsFailure); Assert.Equal("hunger.conflict", refused.Error.Code);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(900, await Custody(f));
        Assert.Equal("Rejected", (await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == original.EventId, ct)).ApprovalStatus);
    });

    [Fact]
    public Task RequestedTopUpTakesScreenshotsAtExecutionAndRaisesAProposalWhenCustodyIsShort() => TopUpFixture(async (f, ct) =>
    {
        var covered = Value(await f.Service.RequestMoneyAsync(f.Request("TopUpAdvance", 100, "request"), ct));
        Assert.Empty(await f.Db.Set<HungerFinancialEvent>().Where(e => e.Kind == "CustodyProposal").ToArrayAsync(ct));
        Assert.True((await f.Service.ExecuteMoneyRequestAsync(covered.EventId, ct)).IsFailure);
        var shots = await WithShots(f, f.Request("TopUpAdvance", 100, "unused"));
        var executed = Value(await f.Service.ExecuteMoneyRequestAsync(covered.EventId, new(shots.EvidenceBeforeId, shots.EvidenceAfterId), ct));
        Assert.Equal("PendingSeal", executed.ApprovalStatus); Assert.Equal(900, await Custody(f));

        var shortRequest = Value(await f.Service.RequestMoneyAsync(f.Request("TopUpAdvance", 1500, "short"), ct));
        f.Db.ChangeTracker.Clear();
        var proposal = await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Kind == "CustodyProposal", ct);
        Assert.Equal(600, proposal.Amount); Assert.Equal("Open", proposal.Status); Assert.Equal(shortRequest.EventId, proposal.RelatedEventId);
        Assert.Equal(f.Account.Id, proposal.PlatformRiderAccountId);
        var again = await WithShots(f, f.Request("TopUpAdvance", 1500, "unused-2"));
        Assert.True((await f.Service.ExecuteMoneyRequestAsync(shortRequest.EventId, new(again.EvidenceBeforeId, again.EvidenceAfterId), ct)).IsFailure);
        Assert.Equal(900, await Custody(f));
    });
}
