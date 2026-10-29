namespace LogisticsERP.Domain.Ninja;

/// <summary>Monitoring estimates only. This component has no persistence or financial side effects.</summary>
public static class NinjaSuggestionCalculator
{
    public const string PolicyVersion = "NINJA-SUGGESTION-V1";
    public static NinjaSuggestionRates? Rates(string? type) => type switch
    {
        "BIKE" => new(450, 5700, 11, 16, 17, 18, 9),
        "CAR" => new(460, 6500, 12, 22, 22.5m, 23, 10),
        _ => null
    };

    public static NinjaOrderSuggestion? Orders(string? type, int monthDays, decimal responsibility, decimal orders)
    {
        var rates = Rates(type);
        if (rates is null || monthDays is < 28 or > 31 || responsibility <= 0 || orders < 0) return null;
        var target = rates.Target * responsibility / monthDays;
        var basis = rates.Base * responsibility / monthDays;
        var shortage = Math.Max(target - orders, 0);
        var extra = Math.Max(orders - target, 0);
        string band; decimal rate; decimal value;
        if (shortage == 0) { band = extra > 0 ? "AboveTarget" : "TargetAchieved"; rate = rates.Bonus; value = basis + extra * rate; }
        else if (shortage * 10 <= target) { band = "Shortage10"; rate = rates.Rate1; value = basis - shortage * rate; }
        else if (shortage * 5 <= target) { band = "Shortage20"; rate = rates.Rate2; value = basis - shortage * rate; }
        else if (shortage * 10 <= target * 3) { band = "Shortage30"; rate = rates.Rate3; value = basis - shortage * rate; }
        else { band = "PerOrderFallback"; rate = rates.Fallback; value = orders * rate; }
        // Keep the amount unrounded so mixed pricing segments round only once after summation.
        return new(target, basis, orders, shortage, extra, band, rate, value);
    }

    public static string Attendance(decimal scheduled, decimal qualified, decimal unknown)
    {
        if (scheduled <= 0) return "Unknown";
        if (qualified >= scheduled * .85m) return "Pass";
        return qualified + unknown < scheduled * .85m ? "Fail" : "Unknown";
    }

    public static decimal Money(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    public static decimal Adjust(decimal amount, string attendance) => Money(attendance == "Fail" ? amount * .85m : amount);
}

public sealed record NinjaSuggestionRates(decimal Target, decimal Base, decimal Bonus, decimal Rate1,
    decimal Rate2, decimal Rate3, decimal Fallback);
public sealed record NinjaOrderSuggestion(decimal Target, decimal Base, decimal Orders, decimal Shortage,
    decimal Extra, string Band, decimal Rate, decimal Amount);
