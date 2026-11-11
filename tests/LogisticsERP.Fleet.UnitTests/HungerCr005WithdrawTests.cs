using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Files;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

// CR-005 phase 5: withdrawal channel and screenshot, custody proposal with the WalletPending status, wallet alert.
public sealed partial class HungerFinanceTests
{
    private static async Task Cr005Fixture(Func<Fixture, CancellationToken, Task> body, bool relational = false)
    {
        var root = TempRoot();
        try
        {
            await using var f = await Fixture.Create(intercept: true, relational: relational, files: new PrivateFileStorage(new TestHostEnvironment(root)), cr005: true);
            await body(f, TestContext.Current.CancellationToken);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static HungerWalletReviewRequest ReturnOf(Fixture f, decimal balance) => new("Return", f.Rider.Id, f.Supervisor.Id, new(2026, 11, 1), balance, null, 0,
        string.Empty, null, null, Return: new(new(2026, 11, 1), "Ended", "return", Convert.ToBase64String(f.Db.RiderClientAssignments.Single().RowVersion), new(OrdersCount: 0)));

    private static async Task<RiderProfile> NextRider(Fixture f)
    {
        var employee = new Employee { FullNameAr = "Next rider", Status = EmployeeStatus.Active };
        var rider = new RiderProfile { EmployeeId = employee.Id }; f.Db.AddRange(employee, rider);
        await f.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return rider;
    }

    private static HungerWalletReviewRequest DeliverTo(Fixture f, Guid rider, decimal balance) => new("Deliver", rider, f.Supervisor.Id, new(2026, 11, 2), balance, null, 0,
        string.Empty, null, null, Assignment: new(rider, new(2026, 11, 2), "hand-over", false, null));

    [Fact]
    public Task WithdrawalNeedsAChannelMatchingItsDestinationAndAWalletScreenshot() => TopUpFixture(async (f, ct) =>
    {
        var request = await Withdrawal(f, "withdraw");
        Assert.True((await f.Service.PostMoneyAsync(request, ct)).IsFailure);
        Assert.True((await f.Service.PostMoneyAsync(request with { DestinationChannel = "Bank" }, ct)).IsFailure);
        var shot = Value(await f.Service.UploadEvidenceAsync(new(f.Platform.Id, f.Account.Id, "Wallet", new(2026, 11, 1)), PixelImage(), ct)).EvidenceFileId;
        request = request with { WalletEvidenceId = shot };
        // The destination here is a bank account, and a company withdrawal is never the supervisor's STC.
        Assert.True((await f.Service.PostMoneyAsync(request with { DestinationChannel = "Cashbox" }, ct)).IsFailure);
        Assert.True((await f.Service.PostMoneyAsync(request with { DestinationChannel = "Stc" }, ct)).IsFailure);
        Assert.True((await f.Service.PostMoneyAsync(request with { DestinationChannel = "Wire" }, ct)).IsFailure);
        var posted = Value(await f.Service.PostMoneyAsync(request with { DestinationChannel = "Bank" }, ct));
        Assert.Equal("PendingArrival", posted.Status); Assert.Equal("PendingSeal", posted.ApprovalStatus);
        f.Db.ChangeTracker.Clear();
        Assert.Equal("Bank", (await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == posted.EventId, ct)).DestinationChannel);
        Assert.Equal(posted.EventId, (await f.Db.Set<HungerEvidenceFile>().SingleAsync(ct)).FinancialEventId);
    });

    [Fact]
    public Task RemittanceConfirmationStatesItsReceivingChannelExplicitly() => TopUpFixture(async (f, ct) =>
    {
        var advance = Value(await f.Service.PostMoneyAsync(await WithShots(f, f.Request("TopUpAdvance", 100, "advance")), ct));
        var collection = Value(await f.Service.PostMoneyAsync(f.Request("Collection", 40, "collect") with { RelatedEventId = advance.EventId }, ct));
        var batch = Value(await f.Service.SubmitRemittanceAsync(new(f.Platform.Id, f.Supervisor.Id, [new(collection.EventId, 40)], "batch", new(2026, 11, 1), "cash"), ct));
        var sponsor = (await f.Db.Set<HungerFinanceConfiguration>().SingleAsync(ct)).SponsorId;
        var confirm = new HungerRemittanceConfirmation(f.Bank.Id, null, "receipt", new(2026, 11, 2), "bank receipt");
        Assert.True((await f.Service.ConfirmRemittanceAsync(batch.EventId, confirm, ct)).IsFailure);
        Assert.True((await f.Service.ConfirmRemittanceAsync(batch.EventId, confirm with { DestinationChannel = "Bank" }, ct)).IsFailure);
        confirm = confirm with { DestinationType = "Company", DestinationOwnerId = sponsor };
        Assert.True((await f.Service.ConfirmRemittanceAsync(batch.EventId, confirm with { DestinationChannel = "Cashbox" }, ct)).IsFailure);
        Assert.True((await f.Service.ConfirmRemittanceAsync(batch.EventId, confirm with { DestinationChannel = "Stc" }, ct)).IsFailure);
        var receipt = Value(await f.Service.ConfirmRemittanceAsync(batch.EventId, confirm with { DestinationChannel = "Bank" }, ct));
        f.Db.ChangeTracker.Clear();
        Assert.Equal("Bank", (await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Id == receipt.EventId, ct)).DestinationChannel);
    });

    [Fact]
    public Task NegativeReturnThatCustodyCannotZeroParksTheAccountUntilAHandoverProvesZero() => Cr005Fixture(async (f, ct) =>
    {
        f.Db.Add(new PlatformAccountBusinessDetails { PlatformRiderAccountId = f.Account.Id }); await f.Db.SaveChangesAsync(ct);
        var review = Value(await f.Service.ReviewWalletWithImageAsync(f.Account.Id, ReturnOf(f, -100), PixelImage(), ct));
        var returned = Value(await f.Service.ConfirmWalletAsync(review.ReviewId, ct));
        Assert.Equal(-100, returned.RiderBalanceChange);
        f.Db.ChangeTracker.Clear();
        var proposal = await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Kind == "CustodyProposal", ct);
        Assert.Equal("Open", proposal.Status); Assert.Equal(100, proposal.Amount); Assert.Equal(returned.EventId, proposal.RelatedEventId);
        Assert.Equal("WalletPending", (await f.Db.Set<PlatformAccountBusinessDetails>().SingleAsync(ct)).OperationalDisposition);
        Assert.Single(await f.Db.Set<PlatformAccountBusinessRevision>().ToArrayAsync(ct));
        var wallet = Assert.Single(Value(await f.Service.WalletsAsync(new() { AlertOnly = true }, ct: ct)).Items);
        Assert.Equal("CustodyProposalOpen", wallet.Alert);

        var next = await NextRider(f);
        var blocked = await f.Service.ReviewWalletWithImageAsync(f.Account.Id, DeliverTo(f, next.Id, -100), PixelImage(), ct);
        Assert.True(blocked.IsFailure); Assert.Equal("hunger.conflict", blocked.Error.Code);
        var zero = Value(await f.Service.ReviewWalletWithImageAsync(f.Account.Id, DeliverTo(f, next.Id, 0), PixelImage(), ct));
        Value(await f.Service.ConfirmWalletAsync(zero.ReviewId, ct));
        f.Db.ChangeTracker.Clear();
        Assert.Equal("Resolved", (await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Kind == "CustodyProposal", ct)).Status);
        Assert.Equal("Normal", (await f.Db.Set<PlatformAccountBusinessDetails>().SingleAsync(ct)).OperationalDisposition);
        Assert.Empty(Value(await f.Service.WalletsAsync(new() { AlertOnly = true }, ct: ct)).Items);
        Assert.Equal("None", Assert.Single(Value(await f.Service.WalletsAsync(new(), ct: ct)).Items).Alert);
    });

    [Fact]
    public Task NegativeReturnCoveredByCustodyOnlyRaisesTheWalletAlert() => TopUpFixture(async (f, ct) =>
    {
        f.Db.Add(new PlatformAccountBusinessDetails { PlatformRiderAccountId = f.Account.Id }); await f.Db.SaveChangesAsync(ct);
        var review = Value(await f.Service.ReviewWalletWithImageAsync(f.Account.Id, ReturnOf(f, -100), PixelImage(), ct));
        Value(await f.Service.ConfirmWalletAsync(review.ReviewId, ct));
        f.Db.ChangeTracker.Clear();
        Assert.Empty(await f.Db.Set<HungerFinancialEvent>().Where(e => e.Kind == "CustodyProposal").ToArrayAsync(ct));
        Assert.Equal("Normal", (await f.Db.Set<PlatformAccountBusinessDetails>().SingleAsync(ct)).OperationalDisposition);
        Assert.Equal("WalletNotZero", Assert.Single(Value(await f.Service.WalletsAsync(new() { AlertOnly = true }, ct: ct)).Items).Alert);
    });

    [Fact]
    public Task CustodyProposalOfAShortRequestClosesWhenTheRequestExecutes() => TopUpFixture(async (f, ct) =>
    {
        var request = Value(await f.Service.RequestMoneyAsync(f.Request("TopUpAdvance", 1500, "short"), ct));
        Assert.Equal("Open", (await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Kind == "CustodyProposal", ct)).Status);
        await FundCustody(f, 600, "fund-more");
        var shots = await WithShots(f, f.Request("TopUpAdvance", 1500, "unused"));
        Value(await f.Service.ExecuteMoneyRequestAsync(request.EventId, new(shots.EvidenceBeforeId, shots.EvidenceAfterId), ct));
        f.Db.ChangeTracker.Clear();
        Assert.Equal("Resolved", (await f.Db.Set<HungerFinancialEvent>().SingleAsync(e => e.Kind == "CustodyProposal", ct)).Status);
        Assert.Equal(100, await Custody(f));
    });
}
