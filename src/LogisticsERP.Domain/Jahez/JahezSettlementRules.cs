using LogisticsERP.Domain.Entities.Jahez;

namespace LogisticsERP.Domain.Jahez;

public static class JahezSettlementRules
{
    public static decimal Remaining(decimal net, decimal commission) => JahezRules.Money(net + commission);
    public static JahezMoneyDirection Direction(decimal remaining) => remaining switch
    {
        > 0 => JahezMoneyDirection.Receipt, < 0 => JahezMoneyDirection.Disbursement, _ => JahezMoneyDirection.None
    };
    public static bool Allows(JahezMoneyFund fund, JahezMoneyDirection direction, JahezMoneyPurpose purpose) =>
        direction switch
        {
            JahezMoneyDirection.Receipt => fund == JahezMoneyFund.Cashbox
                || fund == JahezMoneyFund.PetroApp && purpose is JahezMoneyPurpose.Settlement or JahezMoneyPurpose.Fuel,
            JahezMoneyDirection.Disbursement => purpose != JahezMoneyPurpose.Fuel
                && fund is JahezMoneyFund.Cashbox or JahezMoneyFund.Bank,
            _ => false
        };
}
