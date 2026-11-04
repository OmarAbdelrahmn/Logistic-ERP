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
            var employee = new Employee { FullNameAr = "Test rider" }; var rider = new RiderProfile { EmployeeId = employee.Id };
            var account = new PlatformRiderAccount { ClientPlatformId = platform.Id, Code = "HS1", ExternalAccountId = "1" };
            var sponsor = new Sponsor(); var custody = Sub("custody"); var collections = Sub("collections"); var wallet = Sub("wallet"); var clearing = Sub("clearing"); var bank = Sub("bank");
            db.AddRange(platform, supervisor, employee, rider, account, sponsor, custody, collections, wallet, clearing, bank,
                new RiderClientAssignment { PlatformRiderAccountId = account.Id, RiderProfileId = rider.Id, EffectiveFrom = new(2026, 11, 1), Status = RiderAssignmentStatus.Active });
            await db.SaveChangesAsync(ct);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Features:Hunger"] = "true", ["Features:HungerFinance"] = "true" }).Build();
            var service = new HungerQualityService(db, new User(), new Checker(), new Clock(), config);
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
