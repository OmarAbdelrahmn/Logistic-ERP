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
    // A close adjustment moves wallet money on the phone only. A positive adjustment is money the platform returned into the wallet:
    // it belongs to the rider who closed the month (already deducted from their pay) but is now held by whoever has the account.
    // When both are the same rider the two entries net to zero.
    public static (decimal HolderChange, decimal CloserChange) CloseAdjustment(decimal adjustment) => (-adjustment, adjustment);
    // What is left of a funded top-up once a close adjustment has been attributed against it.
    public static decimal TopUpAfterCloseAdjustment(decimal topUp, decimal adjustment) => topUp - adjustment;
}
