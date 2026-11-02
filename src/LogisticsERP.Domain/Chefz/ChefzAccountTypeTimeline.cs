using LogisticsERP.Domain.Entities.Chefz;

namespace LogisticsERP.Domain.Chefz;

public static class ChefzAccountTypeTimeline
{
    public sealed record Segment(DateOnly From, DateOnly To, ChefzAccountType Type)
    {
        public int Days => To.DayNumber - From.DayNumber + 1;
    }

    public static Segment[] Split(ChefzAccountType current, IEnumerable<ChefzAccountTypeChange> changes,
        DateOnly from, DateOnly to)
    {
        var ordered = changes.OrderBy(c => c.EffectiveFrom).ToArray();
        var type = ordered.Length == 0 ? current : ordered[0].PreviousType;
        var start = from;
        var result = new List<Segment>();
        foreach (var change in ordered)
        {
            if (change.EffectiveFrom <= from) { type = change.AccountType; continue; }
            if (change.EffectiveFrom > to) break;
            result.Add(new(start, change.EffectiveFrom.AddDays(-1), type));
            start = change.EffectiveFrom;
            type = change.AccountType;
        }
        result.Add(new(start, to, type));
        return result.ToArray();
    }

    public static void ApplyMixed(ChefzWalletSettlement settlement, int fullTimeDays, decimal commissionBase)
    {
        settlement.FullTimeDays = fullTimeDays;
        settlement.FreelancerCommissionBase = commissionBase;
        settlement.CommissionRate = ChefzRules.FreelancerRate;
        settlement.DailyFee = ChefzRules.FullTimeDailyFee;
        settlement.CompanyAmount = ChefzOrderSettlementRules.Money(commissionBase * ChefzRules.FreelancerRate)
            + fullTimeDays * ChefzRules.FullTimeDailyFee;
        settlement.RiderAmount = settlement.GrossEarnings - settlement.CompanyAmount;
    }
}
