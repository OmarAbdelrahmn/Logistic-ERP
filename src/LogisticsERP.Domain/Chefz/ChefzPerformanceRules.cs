using LogisticsERP.Domain.Entities.Chefz;

namespace LogisticsERP.Domain.Chefz;

public static class ChefzPerformanceRules
{
    public static string Classify(decimal orders, decimal? acceptance, bool daily, ChefzPerformanceSettings s)
    {
        if (orders >= (daily ? s.DailyAOrders : s.OverallAOrders) && acceptance >= s.AAcceptanceRate) return "A";
        if (orders >= (daily ? s.DailyBOrders : s.OverallBOrders) && acceptance >= s.BAcceptanceRate) return "B";
        if (orders >= (daily ? s.DailyCOrders : s.OverallCOrders) && acceptance >= s.CAcceptanceRate) return "C";
        return "D";
    }

    public static decimal? Acceptance(long accepted, long valid) => valid == 0 ? null
        : decimal.Round(100m * accepted / valid, 2, MidpointRounding.AwayFromZero);

    // Missing dates interrupt the sequence: an absent report does not prove zero orders.
    public static bool NeedsChange(IEnumerable<(DateOnly Date, long Orders)> days, int threshold)
    {
        var run = 0;
        DateOnly? previous = null;
        foreach (var day in days.OrderBy(d => d.Date))
        {
            run = day.Orders == 0 ? previous?.AddDays(1) == day.Date ? run + 1 : 1 : 0;
            previous = day.Date;
        }
        return run > threshold;
    }
}
