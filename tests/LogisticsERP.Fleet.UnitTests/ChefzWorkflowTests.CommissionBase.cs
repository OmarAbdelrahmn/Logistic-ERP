using LogisticsERP.Domain.Chefz;
using LogisticsERP.Domain.Entities.Chefz;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Theory]
    [InlineData(115, 10, 0, 0, 0, 15, 95)]
    [InlineData(115, 0, 20, 0, 0, 15, 105)]
    [InlineData(115, 0, 0, 30, 0, 15, 115)]
    [InlineData(115, 0, 0, 0, 25, 15, 60)]
    [InlineData(115, 10, 20, 30, 25, 15, 120)]
    [InlineData(115, 0, 0, 0, 200, 15, -115)]
    [InlineData(0, 10, 20, 30, 25, 0, 35)]
    [InlineData(-115, 10, 20, 30, 25, -15, -50)]
    public void FreelancerOrderCommissionUsesOnlyDeliveryFeesAfterVat(decimal fees, decimal tips,
        decimal compensation, decimal bonuses, decimal deductions, decimal company, decimal rider)
    {
        var row = ChefzOrderSettlementRules.Calculate(ChefzAccountType.Freelancer, 2026, 10, 1,
            new(2026, 10, 1), null, 1, fees, tips, compensation, bonuses, deductions);
        Assert.Equal(company, row.CompanyAmount);
        Assert.Equal(rider, row.RiderAmount);
        ChefzOrderSettlementRules.ReconcileRiderCycles([row]);
        Assert.Equal(company, row.CompanyAmount);
        Assert.Equal(rider, row.RiderAmount);
        Assert.Equal(row.GrossEarnings, row.CompanyAmount + row.RiderAmount);
    }

    [Fact]
    public void RiderCycleCommissionRoundingIsIndependentOfAdjustmentsOnEachAccount()
    {
        var rider = Guid.NewGuid();
        var rows = Enumerable.Range(0, 3).Select(_ =>
        {
            var row = ChefzOrderSettlementRules.Calculate(ChefzAccountType.Freelancer, 2026, 10, 1,
                new(2026, 10, 1), null, 1, .05m, 0, 0, 0, 0);
            row.RiderProfileId = rider;
            row.PlatformRiderAccountId = Guid.NewGuid();
            row.RiderClientAssignmentId = Guid.NewGuid();
            return row;
        }).ToArray();
        ChefzOrderSettlementRules.ReconcileRiderCycles(rows);
        Assert.Equal(.02m, rows.Sum(s => s.CompanyAmount));
        var original = rows.Select(s => (s.VatAmount, s.CompanyAmount)).ToArray();

        rows[0].Tips = 100;
        rows[1].Compensation = 200;
        rows[1].Bonuses = 50;
        rows[2].Deductions = 400;
        ChefzOrderSettlementRules.ReconcileRiderCycles(rows.Reverse());

        Assert.Equal(original, rows.Select(s => (s.VatAmount, s.CompanyAmount)).ToArray());
        Assert.Equal(-49.87m, rows.Sum(s => s.GrossEarnings));
        Assert.Equal(-49.89m, rows.Sum(s => s.RiderAmount));
        Assert.All(rows, s => Assert.Equal(s.GrossEarnings, s.CompanyAmount + s.RiderAmount));
    }
}
