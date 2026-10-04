using LogisticsERP.Domain.Entities.Jahez;

namespace LogisticsERP.Domain.Jahez;

public static class JahezRules
{
    public const decimal AccountFee = 200m;
    public const decimal DailyCommission = 15m;
    public const decimal PercentageRate = 0.15m;
    public const int MaximumActiveAccountsPerRider = 2;
    public static DateOnly RiyadhDate(DateTimeOffset time) => DateOnly.FromDateTime(time.ToOffset(TimeSpan.FromHours(3)).DateTime);
    public static DateTimeOffset StartOfDay(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3));
    public static decimal Money(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    public static decimal EarningsBase(JahezEarningsStatement s) => s.TotalDeliveryPrice - s.TotalPenalties
        - s.TotalCashAmount - s.TotalDriverDebit - s.TotalServiceDeduction + s.TotalDriverCredit
        + s.TotalBonuses + s.TotalTips + s.TotalFreeOrders;
    public static decimal PercentageCommission(JahezEarningsStatement s, decimal rate = PercentageRate) => Money(Math.Max(0m, EarningsBase(s)) * rate);
    public static bool IsOverdue(DateTimeOffset anchor, DateOnly today) => today.DayNumber - RiyadhDate(anchor).DayNumber > 10;
    public static bool Contains(JahezAccountHandover handover, DateTimeOffset instant) => handover.StartedAtUtc <= instant
        && (handover.EndedAtUtc is null || instant < handover.EndedAtUtc);
    public static decimal DailyAmount(DateOnly from, DateOnly through) => Math.Max(0, through.DayNumber - from.DayNumber + 1) * DailyCommission;
}
