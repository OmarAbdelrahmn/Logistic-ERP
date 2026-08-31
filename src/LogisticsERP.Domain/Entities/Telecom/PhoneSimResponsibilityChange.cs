using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Telecom;

public sealed class PhoneSimResponsibilityChange : HistoryEntity
{
    public Guid PhoneSimCardId { get; set; }
    public Guid? PreviousResponsibleEmployeeId { get; set; }
    public Guid ResponsibleEmployeeId { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
