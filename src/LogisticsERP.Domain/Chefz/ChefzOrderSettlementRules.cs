using LogisticsERP.Domain.Entities.Chefz;

namespace LogisticsERP.Domain.Chefz;

public static class ChefzOrderSettlementRules
{
    public const decimal VatRate = 0.15m;
    public static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static ChefzWalletSettlement Calculate(ChefzAccountType type, int year, int month, int half,
        DateOnly assignedFrom, DateOnly? assignedTo, int sourceRows, decimal deliveryFees,
        decimal tips, decimal compensation, decimal bonuses, decimal deductions)
    {
        // Reuse the assignment/calendar-day rules of manual settlements.
        var result = ChefzRules.Calculate(type, year, month, half, assignedFrom, assignedTo, 0, 0);
        if (sourceRows < 1 || deductions < 0) throw new ArgumentException("Invalid order totals.");
        result.Source = "Orders";
        result.SourceRows = sourceRows;
        result.DeliveryFees = Money(deliveryFees);
        result.VatAmount = result.DeliveryFees - Money(result.DeliveryFees / (1 + VatRate));
        result.Tips = Money(tips);
        result.Compensation = Money(compensation);
        result.Bonuses = Money(bonuses);
        result.Deductions = Money(deductions);
        result.GrossEarnings = result.DeliveryFees - result.VatAmount + result.Tips
            + result.Compensation + result.Bonuses - result.Deductions;
        result.CompanyAmount = type == ChefzAccountType.Freelancer
            ? Money((result.DeliveryFees - result.VatAmount) * ChefzRules.FreelancerRate)
            : result.AssignedDays * ChefzRules.FullTimeDailyFee;
        result.RiderAmount = result.GrossEarnings - result.CompanyAmount;
        return result;
    }

    public static void ReconcileRiderCycles(IEnumerable<ChefzWalletSettlement> settlements)
    {
        foreach (var cycle in settlements.GroupBy(s => new { s.RiderProfileId, s.PeriodFrom }))
        {
            // Fees include VAT: extract it once per rider/cycle across accounts/assignments.
            var rows = cycle.OrderByDescending(s => Math.Abs(s.DeliveryFees)).ThenBy(s => s.PlatformRiderAccountId)
                .ThenBy(s => s.RiderClientAssignmentId).ToArray();
            var fees = rows.Sum(s => s.DeliveryFees);
            var vat = fees - Money(fees / (1 + VatRate));
            rows[0].VatAmount += vat - rows.Sum(s => s.VatAmount);
            foreach (var s in rows)
            {
                s.GrossEarnings = s.DeliveryFees - s.VatAmount + s.Tips + s.Compensation + s.Bonuses - s.Deductions;
                if (s.AccountType == ChefzAccountType.Freelancer)
                    s.CompanyAmount = Money((s.DeliveryFees - s.VatAmount) * ChefzRules.FreelancerRate);
            }
            // Reconcile commission on VAT-exclusive delivery fees; adjustments belong to the rider.
            var freelancers = rows.Where(s => s.AccountType == ChefzAccountType.Freelancer)
                .OrderByDescending(s => Math.Abs(s.DeliveryFees - s.VatAmount)).ThenBy(s => s.PlatformRiderAccountId).ThenBy(s => s.RiderClientAssignmentId).ToArray();
            if (freelancers.Length > 0)
                freelancers[0].CompanyAmount += Money(freelancers.Sum(s => s.DeliveryFees - s.VatAmount) * ChefzRules.FreelancerRate)
                    - freelancers.Sum(s => s.CompanyAmount);
            foreach (var s in rows) s.RiderAmount = s.GrossEarnings - s.CompanyAmount;
        }
    }
}
