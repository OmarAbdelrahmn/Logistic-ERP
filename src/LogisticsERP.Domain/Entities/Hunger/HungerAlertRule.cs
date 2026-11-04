using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Hunger;

public sealed class HungerAlertRule : HistoryEntity
{
    public Guid ClientPlatformId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Threshold { get; set; }
    public string Severity { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public bool Enabled { get; set; }
    public string EvidenceReference { get; set; } = string.Empty;
}
