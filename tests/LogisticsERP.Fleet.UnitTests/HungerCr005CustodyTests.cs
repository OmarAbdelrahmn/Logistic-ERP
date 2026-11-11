using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

// CR-005 phase 6: custody funding chain and the custody settlement identity.
public sealed partial class HungerFinanceTests
{
    [Fact]
    public Task CustodyIsFundedOnlyThroughRequestApprovalAndDelivery() => Cr005Fixture(async (f, ct) =>
    {
        // Direct posting would bypass the accountant.
        Assert.True((await f.Service.PostMoneyAsync(f.Request("Funding", 1000, "direct", f.Bank.Id), ct)).IsFailure);
        var request = Value(await f.Service.RequestMoneyAsync(f.Request("Funding", 1000, "request") with { AccountId = null, RiderProfileId = null }, ct));
        Assert.True((await f.Service.ExecuteMoneyRequestAsync(request.EventId, ct)).IsFailure);
        Assert.Equal(0, await Custody(f));

        Assert.True((await f.Service.DecideFundingAsync(request.EventId, true, new(800), ct)).IsFailure);
        var custody = (await f.Db.Set<HungerFinanceConfiguration>().SingleAsync(ct)).CustodySubAccountId;
        Assert.True((await f.Service.DecideFundingAsync(request.EventId, true, new(800, custody), ct)).IsFailure);
        Assert.True((await f.Service.DecideFundingAsync(request.EventId, false, new(), ct)).IsFailure);
        f.Permissions.Allowed = [PermissionKeys.Hunger.FinanceManage, PermissionKeys.Hunger.FundingExecute, PermissionKeys.Hunger.FundingRequest];
        Assert.Equal("hunger.forbidden", (await f.Service.DecideFundingAsync(request.EventId, true, new(800, f.Bank.Id), ct)).Error.Code);
        f.Permissions.Allowed = null;

        // The accountant may edit the amount and picks the source account.
        var approved = Value(await f.Service.DecideFundingAsync(request.EventId, true, new(800, f.Bank.Id, "partial"), ct));
        Assert.Equal("Approved", approved.Status); Assert.Equal(800, approved.Amount);
        Assert.Equal(approved, Value(await f.Service.DecideFundingAsync(request.EventId, true, new(800, f.Bank.Id, "partial"), ct)));
        Assert.True((await f.Service.DecideFundingAsync(request.EventId, false, new(Note: "changed my mind"), ct)).IsFailure);
        Assert.Equal(0, await Custody(f));

        var delivered = Value(await f.Service.ExecuteMoneyRequestAsync(request.EventId, ct));
        Assert.Equal("Funding", delivered.Kind); Assert.Equal(800, delivered.Amount);
        Assert.Equal(delivered.EventId, Value(await f.Service.ExecuteMoneyRequestAsync(request.EventId, ct)).EventId);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(800, await Custody(f));
        Assert.Equal("Executed", (await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == request.EventId, ct)).Status);
        var postings = await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().ToArrayAsync(ct);
        Assert.Equal(2, postings.Length); Assert.Equal(-800, postings.Single(p => p.SubAccountId == f.Bank.Id).Amount);
    });

    [Fact]
    public Task RejectedFundingRequestCanNeverBeDelivered() => Cr005Fixture(async (f, ct) =>
    {
        var request = Value(await f.Service.RequestMoneyAsync(f.Request("Funding", 1000, "request") with { AccountId = null, RiderProfileId = null }, ct));
        Assert.Equal("Rejected", Value(await f.Service.DecideFundingAsync(request.EventId, false, new(Note: "no budget"), ct)).Status);
        Assert.True((await f.Service.ExecuteMoneyRequestAsync(request.EventId, ct)).IsFailure);
        Assert.True((await f.Service.DecideFundingAsync(request.EventId, true, new(1000, f.Bank.Id), ct)).IsFailure);
        Assert.Equal(0, await Custody(f));
        Assert.Empty(await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().ToArrayAsync(ct));
    });

    [Fact]
    public Task CustodySettlementMismatchBlocksTopUpsUntilThatDifferenceIsApproved() => TopUpFixture(async (f, ct) =>
    {
        Value(await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpAdvance", 100, "advance")), ct));
        Value(await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpCollected", 50, "collected")), ct));
        var settlement = Value(await f.Service.CustodySettlementAsync(f.Platform.Id, f.Supervisor.Id, ct));
        Assert.Equal(new HungerCustodySettlement(1000, 100, 50, 0, 850, 850, 0, true, false), settlement);

        // A posting on the custody account that no event explains.
        var c = await f.Db.Set<HungerFinanceConfiguration>().SingleAsync(ct);
        f.Db.Add(new TreasuryPosting { ClientPlatformId = f.Platform.Id, SupervisorEmployeeId = f.Supervisor.Id, SubAccountId = c.CustodySubAccountId, SponsorId = c.SponsorId,
            BatchId = Guid.NewGuid(), Kind = TreasuryEntryKind.Adjustment, Amount = 25, ActorUserId = Guid.NewGuid(), ActorNameAr = "stray", Reference = "stray", Reason = "stray" });
        await f.Db.SaveChangesAsync(ct);
        settlement = Value(await f.Service.CustodySettlementAsync(f.Platform.Id, f.Supervisor.Id, ct));
        Assert.Equal(25, settlement.Difference); Assert.False(settlement.Balanced); Assert.False(settlement.ExceptionApproved);
        var blocked = await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpAdvance", 10, "blocked")), ct);
        Assert.True(blocked.IsFailure); Assert.Equal("hunger.conflict", blocked.Error.Code);

        // An approval for another amount does not cover this difference.
        async Task Approve(decimal amount, string key)
        {
            var created = Value(await f.Service.RequestExceptionAsync(new(f.Platform.Id, "CustodyIdentityMismatch", null, f.Supervisor.Id, amount, "counted cash", key), ct));
            var row = await f.Db.Set<HungerExceptionApproval>().SingleAsync(e => e.Id == created.ExceptionId, ct);
            row.RequestedByUserId = Guid.NewGuid(); await f.Db.SaveChangesAsync(ct);
            Value(await f.Service.DecideExceptionAsync(created.ExceptionId, true, new(), ct));
        }
        await Approve(20, "wrong");
        Assert.True((await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpAdvance", 10, "still-blocked")), ct)).IsFailure);
        await Approve(25, "right");
        Assert.True(Value(await f.Service.CustodySettlementAsync(f.Platform.Id, f.Supervisor.Id, ct)).ExceptionApproved);
        Value(await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpAdvance", 10, "allowed")), ct));
        Value(await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpAdvance", 5, "allowed-again")), ct));
        Assert.Equal(25, Value(await f.Service.CustodySettlementAsync(f.Platform.Id, f.Supervisor.Id, ct)).Difference);
    });
}
