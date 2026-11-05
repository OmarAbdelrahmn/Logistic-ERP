using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Hunger;
using LogisticsERP.Infrastructure.Hunger;
using LogisticsERP.Infrastructure.Hr;

using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HungerFinanceTests
{
    [Theory]
    [InlineData("Deliver", -100)]
    [InlineData("Deliver", 0)]
    [InlineData("Deliver", 100)]
    [InlineData("Return", -100)]
    [InlineData("Return", 0)]
    [InlineData("Return", 100)]
    public async Task WalletReviewAcceptsEitherSignWithoutExceptionApproval(string action, decimal balance)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var request = new HungerWalletReviewRequest(action, f.Rider.Id, f.Supervisor.Id, new(2026, 11, 1), balance, 0, 0,
            "signed wallet evidence", null, null,
            Assignment: action == "Deliver" ? new(f.Rider.Id, new(2026, 11, 1), "test", false, null) : null,
            Return: action == "Return" ? new(new(2026, 11, 1), "Ended", "test", "token") : null);
        var result = Value(await f.Service.ReviewWalletAsync(f.Account.Id, request, ct));
        Assert.Equal(balance, result.AttributedBalance);
        Assert.Equal(action == "Deliver" ? -balance : balance, result.RiderBalanceChange);
    }
    [Theory]
    [InlineData("+100.25", 100.25)]
    [InlineData("-100.25", -100.25)]
    public void WalletJsonAcceptsExplicitSignedStrings(string text, decimal expected)
    {
        var json = "{\"Action\":\"Deliver\",\"MobileBalance\":\"" + text + "\",\"ClosingBalance\":0,\"ProvenAdjustments\":0}";
        var request = System.Text.Json.JsonSerializer.Deserialize<HungerWalletReviewRequest>(json);
        Assert.Equal(expected, request!.MobileBalance);
    }
    [Theory]
    [InlineData(-50, -100, 0, 50)]
    [InlineData(50, -100, 100, 50)]
    [InlineData(-100, -200, 0, 100)]
    [InlineData(0, -200, 100, 100)]
    [InlineData(-250, -200, 0, -50)]
    public void ClosingBalanceIsRemovedWithoutFlippingCurrentBalance(decimal mobile, decimal closing, decimal adjustments, decimal expected)
        => Assert.Equal(expected, HungerWalletRules.CurrentPeriod(mobile, closing, adjustments));
    [Fact]
    public void PreviousHolderRightsAreNotAvailableToNewHolder()
    {
        Assert.Equal(30, HungerWalletRules.Usable(130, 100));
        Assert.Equal(-100, HungerWalletRules.RiderBalanceChange("Deliver", 100));
        Assert.Equal(100, HungerWalletRules.RiderBalanceChange("Return", 100));
        Assert.Equal(100, HungerWalletRules.RiderBalanceChange("Deliver", -100));
        Assert.Equal(-100, HungerWalletRules.RiderBalanceChange("Return", -100));
    }
    [Fact]
    public async Task CollectedTopupReducesFundingAndPartialReceiptDoesNotReplenishIt()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.PostMoneyAsync(f.Request("Funding", 1000, "fund", f.Bank.Id), ct));
        var topup = Value(await f.Service.PostMoneyAsync(f.Request("TopUpCollected", 100, "topup"), ct));
        var report = Value(await f.Service.FinanceReportAsync(f.Platform.Id, f.Supervisor.Id, 1, 50, ct));
        Assert.Equal(900, report.CustodyBalance); Assert.Equal(100, report.PendingRemittances); Assert.Equal(0, topup.RiderBalanceChange);
        var remit = Value(await f.Service.SubmitRemittanceAsync(new(f.Platform.Id, f.Supervisor.Id, [new(topup.EventId, 100)], "remit", new(2026, 11, 1), "cash evidence"), ct));
        Value(await f.Service.ConfirmRemittanceAsync(remit.EventId, new(f.Bank.Id, [new(topup.EventId, 60)], "receipt-60", new(2026, 11, 1), "bank receipt"), ct));
        report = Value(await f.Service.FinanceReportAsync(f.Platform.Id, f.Supervisor.Id, 1, 50, ct));
        Assert.Equal(900, report.CustodyBalance); Assert.Equal(40, report.PendingRemittances);
        var over = await f.Service.ConfirmRemittanceAsync(remit.EventId, new(f.Bank.Id, [new(topup.EventId, 41)], "receipt-over", new(2026, 11, 1), "bank receipt"), ct);
        Assert.True(over.IsFailure);
        Value(await f.Service.ConfirmRemittanceAsync(remit.EventId, new(f.Bank.Id, null, "receipt-rest", new(2026, 11, 1), "bank receipt"), ct));
        report = Value(await f.Service.FinanceReportAsync(f.Platform.Id, f.Supervisor.Id, 1, 50, ct)); Assert.Equal(0, report.PendingRemittances);
        Assert.Empty(await f.Db.Set<TreasuryPosting>().ToArrayAsync(ct));
        Assert.Equal(0, await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().SumAsync(p => p.Amount, ct));
    }
    [Fact]
    public async Task AdvanceAndCollectionRemainDistinctAndDuplicateCannotRepost()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var funding = f.Request("Funding", 1000, "fund", f.Bank.Id);
        var first = Value(await f.Service.PostMoneyAsync(funding, ct));
        var replay = Value(await f.Service.PostMoneyAsync(funding, ct)); Assert.Equal(first.EventId, replay.EventId);
        Assert.True((await f.Service.PostMoneyAsync(funding with { Amount = 500 }, ct)).IsFailure);
        var advance = Value(await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 100, "advance"), ct));
        Assert.Equal(-100, advance.RiderBalanceChange);
        Value(await f.Service.PostMoneyAsync(f.Request("Collection", 40, "collect") with { RelatedEventId = advance.EventId }, ct));
        var report = Value(await f.Service.FinanceReportAsync(f.Platform.Id, f.Supervisor.Id, 1, 50, ct));
        Assert.Equal(900, report.CustodyBalance); Assert.Equal(40, report.PendingRemittances);
        Assert.Equal(-60, await f.Db.Set<HungerFinancialEvent>().SumAsync(e => e.RiderBalanceChange, ct));
        Assert.True((await f.Service.PostMoneyAsync(f.Request("Collection", 61, "overcollect") with { RelatedEventId = advance.EventId }, ct)).IsFailure);
        Assert.True((await f.Service.PostMoneyAsync(f.Request("TopUpAdvance", 901, "overfund"), ct)).IsFailure);
    }
    private static HungerWalletReviewRequest Delivery(Fixture f, decimal? closing = 0, decimal adjustments = 0) =>
        new("Deliver", f.Rider.Id, f.Supervisor.Id, new(2026, 11, 1), 100, closing, adjustments,
            "wallet snapshot", "closing evidence", "adjustment evidence", new(f.Rider.Id, new(2026, 11, 1), "test", false, null));
    [Fact]
    public async Task UnknownPriorCloseAllowsFlaggedPreviewButBlocksConfirmation()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var assignment = await f.Db.RiderClientAssignments.SingleAsync(ct); assignment.EffectiveFrom = new(2026, 10, 1);
        await f.Db.SaveChangesAsync(ct);
        var preview = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f, null), ct));
        Assert.Null(preview.ClosingBalance); Assert.Null(preview.CurrentPeriodBalance); Assert.Null(preview.AttributedBalance);
        Assert.Null(preview.RiderBalanceChange); Assert.False(preview.CanConfirm);
        var zeroPreview = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f, 0), ct));
        Assert.Null(zeroPreview.ClosingBalance); Assert.False(zeroPreview.CanConfirm);
        var confirm = await f.Service.ConfirmWalletAsync(preview.ReviewId, ct);
        Assert.True(confirm.IsFailure); Assert.Contains("غير معروف", confirm.Error.Description);
        Assert.Empty(await f.Db.Set<HungerFinancialEvent>().ToArrayAsync(ct));
    }
    [Fact]
    public async Task FirstPeriodHasLegitimateZeroCloseAndCanBeConfirmed()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        // A first-ever assignment, with no previous holder or period.
        f.Db.RemoveRange(await f.Db.RiderClientAssignments.ToArrayAsync(ct)); await f.Db.SaveChangesAsync(ct);
        var preview = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f), ct));
        Assert.Equal(0, preview.ClosingBalance); Assert.Equal(100, preview.CurrentPeriodBalance); Assert.True(preview.CanConfirm);
        Value(await f.Service.ConfirmWalletAsync(preview.ReviewId, ct));
    }
    private static HungerSettlementRequest Close(Fixture f, DateOnly month, string key) => new(f.Platform.Id, f.Account.Id, "MonthClose", month,
        month, month.AddMonths(1).AddDays(-1), month.AddMonths(1).AddDays(-1), 20, null, null, null, null, key, "close evidence");
    [Fact]
    public async Task AdjustmentsRequireUniqueEconomicSourceAndExplicitCloseReconciliation()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var close = Value(await f.Service.RecordSettlementAsync(Close(f, new(2026, 10, 1), "close"), ct));
        var request = new HungerSettlementRequest(f.Platform.Id, f.Account.Id, "Adjustment", new(2026, 10, 1), new(2026, 10, 1),
            new(2026, 10, 31), new(2026, 11, 1), 5, null, null, null, null, "adj", "adjustment evidence", "Statement", "HS-123", close.DocumentId);
        var adjustment = Value(await f.Service.RecordSettlementAsync(request, ct)); Assert.Equal("Unmatched", adjustment.Status);
        var duplicate = await f.Service.RecordSettlementAsync(request with { IdempotencyKey = "different-key" }, ct);
        Assert.True(duplicate.IsFailure);
        var preview = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f, 20), ct)); Assert.Equal(80, preview.CurrentPeriodBalance);
        Value(await f.Service.ReconcileAdjustmentAsync(adjustment.DocumentId, new(close.DocumentId, "reconciled statement"), ct));
        preview = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f, 20, 5), ct)); Assert.Equal(75, preview.CurrentPeriodBalance);
        // A matched adjustment linked to another close cannot affect the November review.
        var other = Value(await f.Service.RecordSettlementAsync(Close(f, new(2026, 9, 1), "older-close"), ct));
        var otherAdjustment = Value(await f.Service.RecordSettlementAsync(request with { InvoicePeriod = new(2026, 9, 1), ClosingSnapshotId = other.DocumentId,
            IdempotencyKey = "other-adj", SourceReference = "HS-124", WalletBalance = 99 }, ct));
        Value(await f.Service.ReconcileAdjustmentAsync(otherAdjustment.DocumentId, new(other.DocumentId, "other statement"), ct));
        preview = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f, 20, 5), ct)); Assert.Equal(75, preview.CurrentPeriodBalance);
    }
    private static async Task<HungerMoneyRequest> Withdrawal(Fixture f, string key)
    {
        var c = await f.Db.Set<HungerFinanceConfiguration>().SingleAsync(TestContext.Current.CancellationToken);
        return f.Request("WithdrawalCompany", 40, key, f.Bank.Id) with { SourceWalletBalance = 100,
            DestinationType = "Company", DestinationOwnerId = c.SponsorId, TransferReference = "bank-transfer-1", TransferDate = new(2026, 11, 1) };
    }
    [Fact]
    public async Task WithdrawalPostsOnlyAfterProvenArrivalAndRetryCannotRepost()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var pending = Value(await f.Service.PostMoneyAsync(await Withdrawal(f, "withdraw"), ct)); Assert.Equal("PendingArrival", pending.Status);
        Assert.Empty(await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().ToArrayAsync(ct));
        var reservedReview = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f), ct));
        Assert.Equal(40, reservedReview.RestrictedRights); Assert.Equal(60, reservedReview.AttributedBalance);
        var stored = await f.Db.Set<HungerFinancialEvent>().SingleAsync(ct); Assert.Equal(100, stored.SourceWalletBalance);
        var evidence = new HungerWithdrawalAction("arrived", new(2026, 11, 2), "bank arrival receipt");
        var arrived = Value(await f.Service.ConfirmWithdrawalArrivalAsync(pending.EventId, evidence, ct)); Assert.Equal("Arrived", arrived.Status);
        Value(await f.Service.ConfirmWithdrawalArrivalAsync(pending.EventId, evidence, ct));
        var postings = await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().ToArrayAsync(ct);
        Assert.Equal(2, postings.Length); Assert.Equal(40, postings.Single(p => p.SubAccountId == f.Bank.Id).Amount);
        Assert.Equal(0, postings.Sum(p => p.Amount));
        Assert.True((await f.Service.CancelWithdrawalAsync(pending.EventId, evidence with { IdempotencyKey = "cancel" }, ct)).IsFailure);
    }
    [Fact]
    public async Task PendingWithdrawalCanBeCancelledWithoutPostingAndReleasesReservation()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var request = await Withdrawal(f, "withdraw-cancel");
        var pending = Value(await f.Service.PostMoneyAsync(request with { Amount = 100 }, ct));
        Assert.True((await f.Service.PostMoneyAsync(request with { IdempotencyKey = "over-reservation" }, ct)).IsFailure);
        var cancel = new HungerWithdrawalAction("cancel", new(2026, 11, 2), "cancel receipt");
        Assert.Equal("Cancelled", Value(await f.Service.CancelWithdrawalAsync(pending.EventId, cancel, ct)).Status);
        Value(await f.Service.CancelWithdrawalAsync(pending.EventId, cancel, ct));
        Assert.True((await f.Service.ConfirmWithdrawalArrivalAsync(pending.EventId, cancel with { IdempotencyKey = "arrival" }, ct)).IsFailure);
        Value(await f.Service.PostMoneyAsync(request with { IdempotencyKey = "replacement" }, ct));
        Assert.Empty(await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().ToArrayAsync(ct));
    }
    [Fact]
    public async Task WithdrawalRejectsMissingReservedSourceAndUnapprovedOwner()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var request = await Withdrawal(f, "invalid");
        Assert.True((await f.Service.PostMoneyAsync(request with { AgainstReservedRights = true }, ct)).IsFailure);
        Assert.True((await f.Service.PostMoneyAsync(request with { DestinationOwnerId = Guid.NewGuid() }, ct)).IsFailure);
        f.Db.Add(new HungerFinancialEvent { ClientPlatformId = f.Platform.Id, PlatformRiderAccountId = f.Account.Id, Kind = "WalletReturn", RiderBalanceChange = 100 });
        await f.Db.SaveChangesAsync(ct);
        Assert.True((await f.Service.PostMoneyAsync(request, ct)).IsFailure);
    }
    [Fact]
    public async Task SupervisorWithdrawalIsNotRemittableBeforeArrival()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var c = await f.Db.Set<HungerFinanceConfiguration>().SingleAsync(ct);
        var account = await f.Db.Set<TreasurySubAccount>().SingleAsync(a => a.Id == c.CollectionsSubAccountId, ct);
        account.Location = TreasuryLocation.Supervisor; await f.Db.SaveChangesAsync(ct);
        var request = (await Withdrawal(f, "supervisor-withdraw")) with { Kind = "WithdrawalSupervisor", DestinationType = "Supervisor",
            DestinationOwnerId = f.Supervisor.Id, CounterSubAccountId = account.Id };
        var pending = Value(await f.Service.PostMoneyAsync(request, ct));
        var remit = new HungerRemittanceRequest(f.Platform.Id, f.Supervisor.Id, [new(pending.EventId, 40)], "pending-remit", new(2026, 11, 2), "remit evidence");
        Assert.True((await f.Service.SubmitRemittanceAsync(remit, ct)).IsFailure);
        Assert.Equal(0, Value(await f.Service.FinanceReportAsync(f.Platform.Id, f.Supervisor.Id, 1, 50, ct)).PendingRemittances);
        Value(await f.Service.ConfirmWithdrawalArrivalAsync(pending.EventId, new("supervisor-arrival", new(2026, 11, 2), "supervisor receipt"), ct));
        Value(await f.Service.SubmitRemittanceAsync(remit with { IdempotencyKey = "arrived-remit" }, ct));
        Assert.Equal(40, Value(await f.Service.FinanceReportAsync(f.Platform.Id, f.Supervisor.Id, 1, 50, ct)).PendingRemittances);
    }
    [Fact]
    public async Task PendingReservedWithdrawalHoldsRightsAndCancellationRestoresCapacity()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var right = new HungerFinancialEvent { ClientPlatformId = f.Platform.Id, PlatformRiderAccountId = f.Account.Id, Kind = "WalletReturn", RiderBalanceChange = 40 };
        f.Db.Add(right); await f.Db.SaveChangesAsync(ct);
        var request = (await Withdrawal(f, "reserved-withdraw")) with { RelatedEventId = right.Id, AgainstReservedRights = true };
        var pending = Value(await f.Service.PostMoneyAsync(request, ct));
        Assert.True((await f.Service.PostMoneyAsync(request with { IdempotencyKey = "reserved-over" }, ct)).IsFailure);
        Value(await f.Service.CancelWithdrawalAsync(pending.EventId, new("reserved-cancel", new(2026, 11, 2), "cancel receipt"), ct));
        Value(await f.Service.PostMoneyAsync(request with { IdempotencyKey = "reserved-replacement" }, ct));
    }
    private static T Value<T>(Result<T> result) { Assert.True(result.IsSuccess, result.Error.Description); return result.Value!; }
    private sealed class User : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid(); public Guid? SessionId => null; public long? AuthorizationVersion => 1; public string? CorrelationId => null;
    }
    private sealed class Checker : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
    private sealed class Clock : TimeProvider
    { public override DateTimeOffset GetUtcNow() => new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero); }
    private sealed class Fixture : IAsyncDisposable
    {
        internal required ApplicationDbContext Db { get; init; }
        internal required HungerQualityService Service { get; init; }
        internal required ClientPlatform Platform { get; init; }
        internal required Employee Supervisor { get; init; }
        internal required RiderProfile Rider { get; init; }
        internal required PlatformRiderAccount Account { get; init; }
        internal required TreasurySubAccount Bank { get; init; }
        internal static async Task<Fixture> Create()
        {
            var ct = TestContext.Current.CancellationToken;
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase($"hunger-finance-{Guid.NewGuid():N}", m => m.EnableNullChecks(false)).Options);
            var platform = new ClientPlatform { Code = "HUNGER" }; var supervisor = new Employee { FullNameAr = "Test supervisor" };
            var employee = new Employee { FullNameAr = "Test rider", Status = EmployeeStatus.Active }; var rider = new RiderProfile { EmployeeId = employee.Id };
            var account = new PlatformRiderAccount { ClientPlatformId = platform.Id, Code = "HS1", ExternalAccountId = "1" };
            var sponsor = new Sponsor(); var custody = Sub("custody"); var collections = Sub("collections"); var wallet = Sub("wallet"); var clearing = Sub("clearing"); var bank = Sub("bank"); bank.SponsorId = sponsor.Id; bank.Location = TreasuryLocation.Bank;
            db.AddRange(platform, supervisor, employee, rider, account, sponsor, custody, collections, wallet, clearing, bank,
                new RiderClientAssignment { PlatformRiderAccountId = account.Id, RiderProfileId = rider.Id, EffectiveFrom = new(2026, 11, 1), Status = RiderAssignmentStatus.Active });
            await db.SaveChangesAsync(ct);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Features:Hunger"] = "true", ["Features:HungerFinance"] = "true" }).Build();
            var service = new HungerQualityService(db, new User(), new Checker(), new Clock(), config,
                new SimplePlatformService(db, new User(), new Clock(), new PlatformCredentialProtector(() => new byte[32])));
            var setup = await service.ConfigureFinanceAsync(new(platform.Id, supervisor.Id, sponsor.Id, custody.Id, collections.Id, wallet.Id, clearing.Id, "approved mapping", true), ct);
            Assert.True(setup.IsSuccess, setup.Error.Description);
            return new() { Db = db, Service = service, Platform = platform, Supervisor = supervisor, Rider = rider, Account = account, Bank = bank };
        }
        private static TreasurySubAccount Sub(string code) => new() { Code = code, NameAr = code, NameEn = code, IsPosting = true, IsActive = true, AllowedDirections = TreasuryDirections.Dual };
        internal HungerMoneyRequest Request(string kind, decimal amount, string key, Guid? counter = null) => new(Platform.Id, Supervisor.Id, kind, amount,
            Account.Id, Rider.Id, counter, null, new(2026, 11, 1), key, "test evidence");
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
