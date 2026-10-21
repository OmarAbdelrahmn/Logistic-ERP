using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Fact]
    public async Task SharedMoneyDayAssignsEveryRowInOriginalOrderWithItsAmounts()
    {
        await using var f = await Fixture.Create(false); var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        var date = new DateOnly(2026, 10, 16);
        f.First.EffectiveTo = date;
        f.Db.Add(new PlatformAccountDayUsage { PlatformRiderAccountId = f.Account.Id, RiderClientAssignmentId = f.First.Id,
            BusinessDate = date, Sequence = 1, OrdersCount = 2, Reason = "First rider returned" });
        f.Db.Add(new PlatformAccountDayUsage { PlatformRiderAccountId = f.Account.Id, RiderClientAssignmentId = f.Second.Id,
            BusinessDate = date, Sequence = 2, Reason = "Next rider" });
        await f.Db.SaveChangesAsync(ct);
        // Empty order IDs deliberately remain countable rows under the agreed rule.
        var sheet = OrderSheet((date, 115, 1, 2, 3, 4), (date, 230, 5, 6, 7, 8),
            (date, 345, 0, 0, 0, 0), (date, 460, 0, 0, 0, 0), (date, 575, 0, 0, 0, 0), (date, 690, 0, 0, 0, 0));
        var preview = Value(await f.Service.ProcessOrderSettlementsAsync("shared.xlsx", sheet, true, ct));
        Assert.True(preview.CanSave); Assert.Empty(f.Db.ChefzImportedOrderRows);
        var result = Value(await f.Service.ProcessOrderSettlementsAsync("shared.xlsx", sheet, false, ct));
        Assert.Empty(result.Issues); Assert.NotNull(result.ImportId);
        var rows = await f.Db.ChefzImportedOrderRows.OrderBy(r => r.SourceRowNumber).ToArrayAsync(ct);
        Assert.Equal(6, rows.Length);
        Assert.All(rows.Take(2), r => Assert.Equal(f.First.Id, r.RiderClientAssignmentId));
        Assert.All(rows.Skip(2), r => Assert.Equal(f.Second.Id, r.RiderClientAssignmentId));
        var first = f.Db.ChefzWalletSettlements.Single(s => s.RiderClientAssignmentId == f.First.Id);
        var last = f.Db.ChefzWalletSettlements.Single(s => s.RiderClientAssignmentId == f.Second.Id);
        Assert.Equal(345, first.DeliveryFees); Assert.Equal(2070, last.DeliveryFees);
        Assert.Equal(6, first.Tips); Assert.Equal(8, first.Compensation); Assert.Equal(10, first.Bonuses); Assert.Equal(12, first.Deductions);
        Assert.Equal(2, first.SourceRows); Assert.Equal(4, last.SourceRows);
        Assert.Equal(45, first.VatAmount); Assert.Equal(270, last.VatAmount);
        Assert.Equal(result.NetEarnings, f.Db.ChefzWalletSettlements.Sum(s => s.GrossEarnings));
        Assert.True(Value(await f.Service.ProcessOrderSettlementsAsync("renamed.xlsx", sheet, false, ct)).AlreadyImported);
        Assert.Equal(6, f.Db.ChefzImportedOrderRows.Count());
    }

    [Fact]
    public async Task SharedOffersPreserveSourceAndComputeRiderRatesAndCorrectedRemainder()
    {
        await using var f = await Fixture.Create(false); var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        var date = new DateOnly(2026, 10, 16); f.First.EffectiveTo = date;
        f.Db.Add(new PlatformAccountDayUsage { PlatformRiderAccountId = f.Account.Id, RiderClientAssignmentId = f.First.Id,
            BusinessDate = date, Sequence = 1, OrdersCount = 2, TotalValidOffers = 4, DeclinedOffers = 1, MissedOffers = 1, Reason = "Return" });
        f.Db.Add(new PlatformAccountDayUsage { PlatformRiderAccountId = f.Account.Id, RiderClientAssignmentId = f.Second.Id,
            BusinessDate = date, Sequence = 2, Reason = "Assignment" }); await f.Db.SaveChangesAsync(ct);
        Assert.Empty(Value(await f.Service.ImportPerformanceAsync(date, "p.xlsx", Sheet("2522777933", "60%", total: 10, accepted: 6, declined: 2, missed: 2), ct)).Issues);
        var first = f.Db.ChefzDailyPerformance.Single(p => p.RiderClientAssignmentId == f.First.Id);
        var last = f.Db.ChefzDailyPerformance.Single(p => p.RiderClientAssignmentId == f.Second.Id);
        Assert.Equal(50, first.AcceptanceRate); Assert.Equal(66.67m, last.AcceptanceRate);
        Assert.Equal(60, Assert.Single(f.Db.ChefzDailyPerformanceSources).AcceptanceRate);
        Assert.Equal(6, first.AcceptedOffers + last.AcceptedOffers);
        Assert.Empty(Value(await f.Service.ImportPerformanceAsync(date, "corrected.xlsx", Sheet("2522777933", "70%", total: 10, accepted: 7, declined: 1, missed: 2), ct)).Issues);
        Assert.Equal(2, first.AcceptedOffers); Assert.Equal(5, last.AcceptedOffers);
        Assert.Equal(2, f.Db.ChefzDailyPerformance.Count()); Assert.Single(f.Db.ChefzDailyPerformanceSources);
    }

    [Fact]
    public async Task FullTimeConversionRejectsSharedDaysAndFreelancerDateCorrectionAllowsBoundary()
    {
        await using var f = await Fixture.Create(false); var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        f.Second.RowVersion = [1]; await f.Db.SaveChangesAsync(ct);
        Value(await f.Service.UpdateAssignmentStartDateAsync(f.Second.Id, new(new(2026, 10, 15), "AQ=="), ct));
        var details = f.Db.ChefzAccountDetails.Single();
        Assert.True((await f.Service.SaveAccountAsync(f.Account.Id, new("FullTime", RowVersion: Convert.ToBase64String(details.RowVersion)), ct)).IsFailure);
        Assert.Equal(new DateOnly(2026, 10, 15), f.Second.EffectiveFrom);
    }
}
