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
    public string Metric { get; set; } = "WorkingHours";
    public string Operator { get; set; } = "LessThan";
    public string Kind { get; set; } = "Alert";
    public string Period { get; set; } = "Day";
    public Guid? OperatingCityId { get; set; }
    public DateOnly? EffectiveThrough { get; set; }
}
