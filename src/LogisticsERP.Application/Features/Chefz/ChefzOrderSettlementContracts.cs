namespace LogisticsERP.Application.Features.Chefz;

public sealed record ChefzOrderSettlementResponse(Guid Id, Guid AccountId, string DriverId,
    Guid AssignmentId, Guid RiderProfileId, string RiderName, string AccountType,
    DateOnly PeriodFrom, DateOnly PeriodTo, DateOnly ChargedFrom, DateOnly ChargedTo,
    int AssignedDays, int SourceRows, decimal DeliveryFees, decimal VatAmount,
    decimal DeliveryFeesAfterVat, decimal Tips, decimal Compensation, decimal Bonuses,
    decimal Deductions, decimal NetEarnings, decimal CompanyAmount, decimal RiderAmount)
{
    public string? RiderIqamaNo { get; init; }
}

public sealed record ChefzRiderCycleSettlement(Guid RiderProfileId, string RiderName,
    DateOnly PeriodFrom, DateOnly PeriodTo, int SourceRows, decimal DeliveryFees,
    decimal VatAmount, decimal DeliveryFeesAfterVat, decimal Tips, decimal Compensation,
    decimal Bonuses, decimal Deductions, decimal NetEarnings, decimal CompanyAmount, decimal RiderAmount,
    IReadOnlyList<ChefzOrderSettlementResponse> Accounts)
{
    public string? RiderIqamaNo { get; init; }
}

public sealed record ChefzOrderSettlementImportResponse(Guid? ImportId, bool IsPreview,
    bool AlreadyImported, bool CanSave, int SourceRows,
    IReadOnlyList<ChefzRiderCycleSettlement> Riders, decimal NetEarnings,
    decimal CompanyAmount, decimal RiderAmount, IReadOnlyList<ChefzImportIssue> Issues);
