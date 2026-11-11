using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Hunger;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

// CR-005 phase 3: a reconciled close adjustment becomes rider entries, never a custody or treasury movement.
public sealed partial class HungerFinanceTests
{
    private static readonly DateOnly October = new(2026, 10, 1);

    private static async Task<(Guid Close, Guid Adjustment)> CloseWithAdjustment(Fixture f, decimal amount)
    {
        var ct = TestContext.Current.CancellationToken;
        var close = Value(await f.Service.RecordSettlementAsync(Close(f, October, "close"), ct));
        var adjustment = Value(await f.Service.RecordSettlementAsync(new(f.Platform.Id, f.Account.Id, "Adjustment", October, October,
            new(2026, 10, 31), new(2026, 11, 1), amount, null, null, null, null, "adj", "adjustment evidence", "Statement", "HS-250", close.DocumentId), ct));
        return (close.DocumentId, adjustment.DocumentId);
    }

    private static async Task<RiderProfile> OctoberRider(Fixture f)
    {
        var employee = new Employee { FullNameAr = "October rider", Status = EmployeeStatus.Active };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        f.Db.AddRange(employee, rider, new RiderClientAssignment { PlatformRiderAccountId = f.Account.Id, RiderProfileId = rider.Id,
            EffectiveFrom = October, EffectiveTo = new(2026, 10, 31), Status = RiderAssignmentStatus.Ended, RowVersion = [1] });
        await f.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return rider;
    }

    [Fact]
    public void CloseAdjustmentRuleMirrorsHolderAndCloserAndReducesTheTopUpLine()
    {
        Assert.Equal((-250m, 250m), HungerWalletRules.CloseAdjustment(250));
        Assert.Equal((40m, -40m), HungerWalletRules.CloseAdjustment(-40));
        var (holder, closer) = HungerWalletRules.CloseAdjustment(250); Assert.Equal(0, holder + closer);
        // Timing gap: close 250, top-up 300. After the platform reverses 250 the top-up line is 50.
        Assert.Equal(50, HungerWalletRules.TopUpAfterCloseAdjustment(300, 250));
    }

    [Fact]
    public Task AdjustmentApprovedUnderAnotherRiderAdvancesTheHolderAndCreditsTheCloser() => TopUpFixture(async (f, ct) =>
    {
        var closer = await OctoberRider(f);
        var topUp = Value(await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpAdvance", 300, "topup")), ct));
        var (close, adjustment) = await CloseWithAdjustment(f, 250);
        var postings = await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().CountAsync(ct);

        Assert.True((await f.Service.ReconcileAdjustmentAsync(adjustment, new(close, "statement"), ct)).IsFailure);
        Assert.True((await f.Service.ReconcileAdjustmentAsync(adjustment, new(close, "statement", new(2026, 11, 2)), ct)).IsFailure);
        Assert.True((await f.Service.ReconcileAdjustmentAsync(adjustment, new(close, "statement", new(2026, 10, 31), f.Supervisor.Id), ct)).IsFailure);
        Assert.True((await f.Service.ReconcileAdjustmentAsync(adjustment, new(close, "statement", new(2026, 11, 5), f.Supervisor.Id), ct)).IsFailure);

        var request = new HungerAdjustmentReconciliation(close, "statement", new(2026, 11, 2), f.Supervisor.Id);
        Assert.Equal("Matched", Value(await f.Service.ReconcileAdjustmentAsync(adjustment, request, ct)).Status);
        Assert.Equal("Matched", Value(await f.Service.ReconcileAdjustmentAsync(adjustment, request, ct)).Status);
        Assert.True((await f.Service.ReconcileAdjustmentAsync(adjustment, request with { ApprovalDate = new(2026, 11, 3) }, ct)).IsFailure);
        f.Db.ChangeTracker.Clear();

        var entries = await f.Db.Set<HungerFinancialEvent>().Where(e => e.Kind.StartsWith("CloseAdjustment")).ToArrayAsync(ct);
        Assert.Equal(2, entries.Length);
        var advance = entries.Single(e => e.Kind == "CloseAdjustmentAdvance"); var wash = entries.Single(e => e.Kind == "CloseAdjustmentWash");
        Assert.Equal(f.Rider.Id, advance.RiderProfileId); Assert.Equal(-250, advance.RiderBalanceChange); Assert.Equal(topUp.EventId, advance.RelatedEventId);
        Assert.Equal(closer.Id, wash.RiderProfileId); Assert.Equal(250, wash.RiderBalanceChange); Assert.Null(wash.RelatedEventId);
        Assert.All(entries, e => { Assert.Equal(250, e.Amount); Assert.Equal(new DateOnly(2026, 11, 2), e.BusinessDate);
            Assert.Equal("PendingSeal", e.ApprovalStatus); Assert.Equal("RiderClearing", e.Role); Assert.Equal(f.Account.Id, e.PlatformRiderAccountId); });
        // The phone ledger moved; custody and treasury did not.
        Assert.Equal(postings, await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().CountAsync(ct));
        Assert.Equal(700, await Custody(f));
        Assert.Equal(new DateOnly(2026, 11, 2), (await f.Db.Set<HungerSettlementDocument>().SingleAsync(d => d.Id == adjustment, ct)).ApprovalDate);
        // The holder's advance is a real receivable and can be sealed like any other rider entry.
        Assert.Equal("Sealed", Value(await f.Service.SealEventAsync(advance.Id, new(), ct)).ApprovalStatus);
    });

    [Fact]
    public async Task AdjustmentApprovedUnderTheClosingRiderNetsToZero()
    {
        await using var f = await Fixture.Create(cr005: true); var ct = TestContext.Current.CancellationToken;
        var assignment = await f.Db.RiderClientAssignments.SingleAsync(ct); assignment.EffectiveFrom = October; await f.Db.SaveChangesAsync(ct);
        var (close, adjustment) = await CloseWithAdjustment(f, 250);
        Value(await f.Service.ReconcileAdjustmentAsync(adjustment, new(close, "statement", new(2026, 11, 2), f.Supervisor.Id), ct));
        var entries = await f.Db.Set<HungerFinancialEvent>().ToArrayAsync(ct);
        Assert.Equal(2, entries.Length);
        Assert.All(entries, e => Assert.Equal(f.Rider.Id, e.RiderProfileId));
        Assert.Equal(0, entries.Sum(e => e.RiderBalanceChange));
        Assert.Empty(await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().ToArrayAsync(ct));
    }

    [Fact]
    public async Task AdjustmentWithoutAKnownClosingRiderWaitsInReviewAndPostsOnceResolved()
    {
        await using var f = await Fixture.Create(intercept: true, cr005: true); var ct = TestContext.Current.CancellationToken;
        var (close, adjustment) = await CloseWithAdjustment(f, 250);
        var request = new HungerAdjustmentReconciliation(close, "statement", new(2026, 11, 2), f.Supervisor.Id);
        Assert.Equal("NeedsReview", Value(await f.Service.ReconcileAdjustmentAsync(adjustment, request, ct)).Status);
        Assert.Empty(await f.Db.Set<HungerFinancialEvent>().ToArrayAsync(ct));
        // An unattributed adjustment stays out of the wallet formula, which only counts matched documents.
        f.Db.ChangeTracker.Clear();
        var waiting = await f.Db.Set<HungerSettlementDocument>().SingleAsync(d => d.Id == adjustment, ct);
        Assert.Equal("NeedsReview", waiting.Status); Assert.Equal(new DateOnly(2026, 11, 2), waiting.ApprovalDate);
        f.Db.ChangeTracker.Clear();

        await OctoberRider(f);
        Assert.Equal("Matched", Value(await f.Service.ReconcileAdjustmentAsync(adjustment, request, ct)).Status);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(2, await f.Db.Set<HungerFinancialEvent>().CountAsync(e => e.Kind.StartsWith("CloseAdjustment"), ct));
    }

    [Fact]
    public async Task WithoutTheFlagReconciliationOnlyMatchesTheDocumentAndPassesThePersistenceGuard()
    {
        await using var f = await Fixture.Create(intercept: true); var ct = TestContext.Current.CancellationToken;
        var (close, adjustment) = await CloseWithAdjustment(f, 5);
        Assert.Equal("Matched", Value(await f.Service.ReconcileAdjustmentAsync(adjustment, new(close, "statement"), ct)).Status);
        Assert.Empty(await f.Db.Set<HungerFinancialEvent>().ToArrayAsync(ct));
        var preview = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f), ct));
        Assert.Equal(5, preview.ProvenAdjustments); Assert.Equal(75, preview.CurrentPeriodBalance);
    }
}
