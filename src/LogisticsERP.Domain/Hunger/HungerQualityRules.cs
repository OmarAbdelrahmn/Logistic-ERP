namespace LogisticsERP.Domain.Hunger;

/// <summary>Platform-account counters. These are informational and never operational order totals.</summary>
public sealed record HungerQualityCounters(long GrossOrders = 0, long CompletedOrders = 0,
    long CompletedOrdersInTime = 0, long TotalVerificationRequests = 0,
    long SuccessfulVerificationRequests = 0, long FailedOrdersByRider = 0)
{
    public static HungerQualityCounters operator +(HungerQualityCounters a, HungerQualityCounters b) => new(
        checked(a.GrossOrders + b.GrossOrders), checked(a.CompletedOrders + b.CompletedOrders),
        checked(a.CompletedOrdersInTime + b.CompletedOrdersInTime), checked(a.TotalVerificationRequests + b.TotalVerificationRequests),
        checked(a.SuccessfulVerificationRequests + b.SuccessfulVerificationRequests), checked(a.FailedOrdersByRider + b.FailedOrdersByRider));
    public static HungerQualityCounters operator -(HungerQualityCounters a, HungerQualityCounters b) => new(
        checked(a.GrossOrders - b.GrossOrders), checked(a.CompletedOrders - b.CompletedOrders),
        checked(a.CompletedOrdersInTime - b.CompletedOrdersInTime), checked(a.TotalVerificationRequests - b.TotalVerificationRequests),
        checked(a.SuccessfulVerificationRequests - b.SuccessfulVerificationRequests), checked(a.FailedOrdersByRider - b.FailedOrdersByRider));
    public bool NonNegativeCore => GrossOrders >= 0 && CompletedOrders >= 0 && CompletedOrdersInTime >= 0
        && TotalVerificationRequests >= 0 && SuccessfulVerificationRequests >= 0;
    public bool ValidCumulative => NonNegativeCore && FailedOrdersByRider >= 0;
}

public static class HungerQualityRules
{
    public static int? Rank(string? segment)
    {
        var text = segment?.Trim().ToUpperInvariant();
        return text is { Length: 1 } && text[0] is >= 'A' and <= 'Z' ? text[0] - 'A' + 1 : null;
    }

    public static (HungerQualityCounters Daily, HungerQualityCounters? NextDaily, string? Error) Split(
        HungerQualityCounters uploaded, HungerQualityCounters prior, HungerQualityCounters? nextCumulative)
    {
        if (!uploaded.ValidCumulative) return (new(), null, "Cumulative counters must be non-negative.");
        var daily = uploaded - prior;
        if (!daily.NonNegativeCore) return (new(), null, "Cumulative verification or order totals are lower than the previous snapshot.");
        var nextDaily = nextCumulative is null ? null : nextCumulative - uploaded;
        if (nextDaily is { NonNegativeCore: false }) return (new(), null, "Cumulative verification or order totals exceed the next snapshot.");
        return (daily, nextDaily, null);
    }
}
