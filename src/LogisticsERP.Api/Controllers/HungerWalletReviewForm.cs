using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Features.Hunger;

namespace LogisticsERP.Api.Controllers;

public sealed class HungerWalletReviewForm
{
    public string Action { get; init; } = string.Empty;
    public Guid RiderProfileId { get; init; }
    public Guid SupervisorEmployeeId { get; init; }
    public DateOnly BusinessDate { get; init; }
    public decimal MobileBalance { get; init; }
    public decimal? ClosingBalance { get; init; }
    public decimal ProvenAdjustments { get; init; }
    public IFormFile? WalletEvidence { get; init; }
    public string? ClosingEvidence { get; init; }
    public string? AdjustmentEvidence { get; init; }
    public AssignSimplePlatformAccountRequest? Assignment { get; init; }
    public ReleaseSimplePlatformAccountRequest? Return { get; init; }
    public Guid? ExceptionApprovalId { get; init; }

    public HungerWalletReviewRequest ToRequest(IFormCollection? fields = null)
    {
        var assignment = Assignment;
        if (fields is not null && assignment?.PreviousDayUsages is { } usages)
        {
            // MVC does not populate the nested IReadOnlyDictionary on this multipart record.
            // Normalize both bracket notation and the existing client's dotted dictionary fields.
            // Copy only client-supplied tokens: never replace them with current database versions.
            assignment = assignment with
            {
                PreviousDayUsages = usages.Select((usage, index) => usage with
                {
                    UsageRowVersions = ReadUsageVersions(fields, index, usage)
                }).ToArray()
            };
        }
        return new(Action, RiderProfileId, SupervisorEmployeeId, BusinessDate, MobileBalance, ClosingBalance,
            ProvenAdjustments, string.Empty, ClosingEvidence, AdjustmentEvidence, assignment, Return, ExceptionApprovalId);
    }

    private static IReadOnlyDictionary<Guid, string?>? ReadUsageVersions(IFormCollection fields, int index, PreviousDayUsageInput usage)
    {
        var prefix = $"assignment.previousDayUsages[{index}].usageRowVersions";
        var versions = usage.UsageRowVersions?.ToDictionary(v => v.Key, v => v.Value) ?? [];
        foreach (var (name, values) in fields)
        {
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var suffix = name[prefix.Length..];
            var key = suffix.StartsWith('.') ? suffix[1..]
                : suffix.StartsWith('[') && suffix.EndsWith(']') ? suffix[1..^1] : string.Empty;
            if (!Guid.TryParse(key, out var id) || values.Count != 1 || usage.AssignmentIds?.Contains(id) != true)
                throw new ArgumentException("Invalid day-usage version field.");
            var token = string.IsNullOrEmpty(values[0]) ? null : values[0];
            if (versions.TryGetValue(id, out var previous) && previous != token)
                throw new ArgumentException("Conflicting day-usage version fields.");
            versions[id] = token;
        }
        return versions.Count > 0 ? versions : usage.UsageRowVersions;
    }
}
