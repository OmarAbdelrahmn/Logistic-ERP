using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hunger;

public sealed record HungerAlertRuleRequest(Guid PlatformId, string Code, decimal Threshold, string Severity,
    DateOnly EffectiveFrom, bool Enabled, string EvidenceReference);
public sealed record HungerAlertRuleItem(Guid Id, Guid PlatformId, string Code, decimal Threshold, string Severity,
    DateOnly EffectiveFrom, bool Enabled, string EvidenceReference);
public sealed record HungerSummary(int InventoryAccounts, int AssignedNormalAccounts, int AvailableNormalAccounts,
    IReadOnlyDictionary<string, int> Dispositions, int ActualRiders, IReadOnlyDictionary<string, int> RegisteredVehicleTypes);
public interface IHungerReportingService
{
    Task<Result<HungerFile>> ExportQualityAsync(DateOnly month, Guid? accountId, CancellationToken ct = default);
    Task<Result<HungerFile>> ExportOperationsAsync(DateOnly from, DateOnly through, Guid? accountId, CancellationToken ct = default);
    Task<Result<HungerSummary>> SummaryAsync(DateOnly month, CancellationToken ct = default);
    Task<Result<IReadOnlyList<HungerAlertRuleItem>>> AlertRulesAsync(CancellationToken ct = default);
    Task<Result<Guid>> AddAlertRuleAsync(HungerAlertRuleRequest request, CancellationToken ct = default);
}
