using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Telecom;

public sealed class PhoneSimOwnershipRevision : HistoryEntity
{
    public Guid PhoneSimCardId { get; set; }
    public string Ownership { get; set; } = "Unknown";
    public DateOnly EffectiveFrom { get; set; }
    public string EvidenceReference { get; set; } = string.Empty;
}
