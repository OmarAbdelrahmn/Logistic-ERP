namespace LogisticsERP.Application.Features.Jahez;

public sealed record JahezProfitReport(DateOnly FromDate, DateOnly ToDate,
    decimal AccountOpeningFeesTotal, decimal SettlementCommissionTotal)
{
    public decimal TotalProfit => AccountOpeningFeesTotal + SettlementCommissionTotal;
}
