using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Hunger;

/// <summary>Observed missed shifts awaiting invoice evidence; never a rider debt.</summary>
public sealed class HungerPotentialFine : AuditableEntity
{
    public Guid DailyOperationId { get; set; }
    public int NoShowCount { get; set; }
    public string Status { get; set; } = "Potential";
    public Guid? InvoiceDocumentId { get; set; }
    public string? InvoiceSourceKey { get; set; }
    public string? ResolutionEvidence { get; set; }
}
