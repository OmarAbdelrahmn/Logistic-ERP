using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Clients;

public sealed class RiderAssignmentEvent : HistoryEntity
{
    public Guid RiderClientAssignmentId { get; set; }
    public RiderAssignmentStatus FromStatus { get; set; }
    public RiderAssignmentStatus ToStatus { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid ActorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ChangeSnapshotJson { get; set; }
}
