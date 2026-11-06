using System.Globalization;

namespace LogisticsERP.Domain.Hunger;

public static class HungerPerformanceRules
{
    public static decimal? Hours(string value, string unit)
    {
        if (unit is not ("DecimalHours" or "Duration" or "FractionOfDay")) throw new FormatException("Unknown hours unit.");
        if (string.IsNullOrWhiteSpace(value)) return null;
        decimal hours;
        if (unit == "Duration")
        {
            var parts = value.Split(':');
            if (parts.Length is not (2 or 3) || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var h)
                || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var m) || m > 59
                || parts.Length == 3 && (!int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var sec) || sec > 59))
                throw new FormatException("Use hh:mm or hh:mm:ss duration.");
            hours = h + m / 60m + (parts.Length == 3 ? int.Parse(parts[2], CultureInfo.InvariantCulture) / 3600m : 0);

        }
        else
        {
            if (unit is not ("DecimalHours" or "FractionOfDay") || !decimal.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out hours))
                throw new FormatException("Explicit hours unit must be DecimalHours, Duration or FractionOfDay.");
            if (unit == "FractionOfDay") hours *= 24;
        }
        if (hours < 0 || hours > 24) throw new FormatException("Daily hours must be between 0 and 24.");
        // Match persisted decimal(18,8) precision before allocation so source and rider shares agree.
        return decimal.Round(hours, 8, MidpointRounding.ToEven);
    }

    public static bool Compare(decimal actual, string op, decimal threshold) => op switch
    {
        "LessThan" => actual < threshold, "LessThanOrEqual" => actual <= threshold,
        "GreaterThan" => actual > threshold, "GreaterThanOrEqual" => actual >= threshold,
        "Equal" => actual == threshold, _ => false
    };
    public static decimal Deficit(decimal actual, decimal target, string op) => op switch
    {
        "LessThan" or "LessThanOrEqual" => Math.Max(actual - target, 0), "Equal" => Math.Abs(target - actual), _ => Math.Max(target - actual, 0)
    };
    public static bool Mandatory(DateOnly day) => day.DayOfWeek is DayOfWeek.Thursday or DayOfWeek.Friday or DayOfWeek.Saturday;
    public static (decimal? Forecast, decimal? RequiredPace) Forecast(decimal orders, decimal target, int elapsed, int remaining, bool complete) =>
        !complete ? (null, null) : remaining == 0 ? (orders, null) :
        (elapsed == 0 ? null : orders + orders / elapsed * remaining, Math.Max(target - orders, 0) / remaining);
}
