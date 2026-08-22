using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Fleet;

public sealed class VehicleOperationalStatusPeriod : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public VehicleOperationalStatus Status { get; set; }
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveToUtc { get; set; }
    public string? ReasonCode { get; set; }
    public string Reason { get; set; } = string.Empty;
    public VehicleStatusSourceType SourceType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public Guid ChangedByUserId { get; set; }
}

public sealed class VehicleOdometerReading : HistoryEntity
{
    public Guid VehicleId { get; set; }
    public long Reading { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public VehicleOdometerSourceType SourceType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public Guid? EvidenceAttachmentId { get; set; }
    public string? Notes { get; set; }
    public bool IsCorrection { get; set; }
    public string? CorrectionReason { get; set; }
}

public sealed class RiderVehicleAssignment : AuditableEntity
{
    public Guid RiderProfileId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid OperationId { get; set; }
    public Guid? PreviousAssignmentId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public Guid? StartLocationId { get; set; }
    public long StartOdometer { get; set; }
    public VehicleCondition StartVehicleCondition { get; set; }
    public byte? StartFuelLevelPercentage { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
    public Guid? EndLocationId { get; set; }
    public long? EndOdometer { get; set; }
    public VehicleCondition? EndVehicleCondition { get; set; }
    public byte? EndFuelLevelPercentage { get; set; }
    public string? PermissionReference { get; set; }
    public DateOnly? PermissionStartsOn { get; set; }
    public DateOnly? PermissionEndsOn { get; set; }
    public RiderVehicleAssignmentStatus Status { get; set; } = RiderVehicleAssignmentStatus.Active;
    public string AssignmentReason { get; set; } = string.Empty;
    public string? CompletionReason { get; set; }
    public Guid AssignedByUserId { get; set; }
    public Guid? EndedByUserId { get; set; }
    public bool WasBackdated { get; set; }
    public string? BackdatedReason { get; set; }
    public Guid? CorrectionOfAssignmentId { get; set; }
    public string? CorrectionReason { get; set; }
    public string? Notes { get; set; }
}

public sealed class RiderVehicleAssignmentEvent : HistoryEntity
{
    public Guid RiderVehicleAssignmentId { get; set; }
    public Guid OperationId { get; set; }
    public RiderVehicleAssignmentEventType EventType { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid ActorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ChangeSnapshotJson { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class FleetCommandReceipt : HistoryEntity
{
    public string CommandName { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public Guid ResultEntityId { get; set; }
}
