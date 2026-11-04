namespace LogisticsERP.Domain.Hunger;

public static class HungerWalletRules
{
    public static decimal CurrentPeriod(decimal mobileBalance, decimal closingBalance, decimal provenAdjustments) => mobileBalance - closingBalance - provenAdjustments;
    public static decimal Usable(decimal currentPeriod, decimal restrictedOtherPartyRights) => currentPeriod - restrictedOtherPartyRights;
    // Positive means a credit in favour of the rider; negative is a debt/advance.
    public static decimal RiderBalanceChange(string action, decimal attributedBalance) => action switch
    {
        "Deliver" => -attributedBalance,
        "Return" => attributedBalance,
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };
}
