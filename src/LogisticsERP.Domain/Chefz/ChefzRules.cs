using LogisticsERP.Domain.Entities.Chefz;

namespace LogisticsERP.Domain.Chefz;

public static class ChefzRules
{
    public const decimal FreelancerRate = 0.15m;
    public const decimal FullTimeDailyFee = 25m;

    public static (DateOnly From, DateOnly To) Period(int year, int month, int half)
    {
        if (year is < 1 or > 9999 || month is < 1 or > 12 || half is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(half));
        return (new(year, month, half == 1 ? 1 : 16),
            new(year, month, half == 1 ? 15 : DateTime.DaysInMonth(year, month)));
    }

    public static ChefzWalletSettlement Calculate(ChefzAccountType type, int year, int month, int half,
        DateOnly assignedFrom, DateOnly? assignedTo, decimal startWallet, decimal endWallet)
    {
        if (!Enum.IsDefined(type) || startWallet < 0 || endWallet < startWallet
            || decimal.Round(startWallet, 2) != startWallet || decimal.Round(endWallet, 2) != endWallet)
            throw new ArgumentException("Enter valid start/end wallet amounts with at most two decimal places; end must be at least start.");
        var period = Period(year, month, half);
        var from = assignedFrom > period.From ? assignedFrom : period.From;
        var to = assignedTo is { } end && end < period.To ? end : period.To;
        if (to < from) throw new ArgumentException("The assignment does not intersect this settlement period.");
        var days = to.DayNumber - from.DayNumber + 1;
        var gross = endWallet - startWallet;
        var company = type == ChefzAccountType.Freelancer
            ? decimal.Round(gross * FreelancerRate, 2, MidpointRounding.AwayFromZero)
            : days * FullTimeDailyFee;
        return new()
        {
            AccountType = type, PeriodFrom = period.From, PeriodTo = period.To,
            ChargedFrom = from, ChargedTo = to, AssignedDays = days,
            StartWalletAmount = startWallet, EndWalletAmount = endWallet, GrossEarnings = gross,
            CommissionRate = type == ChefzAccountType.Freelancer ? FreelancerRate : 0,
            DailyFee = type == ChefzAccountType.FullTime ? FullTimeDailyFee : 0,
            CompanyAmount = company, RiderAmount = gross - company
        };
    }
}
