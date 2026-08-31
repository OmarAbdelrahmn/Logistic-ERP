using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Telecom;

public sealed class RiderPhoneSimAssignment : TemporalPeriodEntity
{
    public Guid PhoneSimCardId { get; set; }
    public Guid RiderProfileId { get; set; }
    public Guid AssignedByUserId { get; set; }
    public string? AssignmentReason { get; set; }
    public string? EndReason { get; set; }
    public string? Notes { get; set; }
}
