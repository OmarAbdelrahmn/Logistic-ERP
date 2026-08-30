using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Fleet;

public sealed class VehiclePlatformAccountAssignment : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public Guid PlatformRiderAccountId { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }
    public string? AssignmentReason { get; set; }
    public VehiclePlatformAccountApprovalStatus ApprovalStatus { get; set; } =
        VehiclePlatformAccountApprovalStatus.Approved;
    public DateTimeOffset ApprovedAtUtc { get; set; }
    public Guid ApprovedByUserId { get; set; }
    public VehiclePlatformAccountAssignmentStatus Status { get; set; } =
        VehiclePlatformAccountAssignmentStatus.Active;
    public DateTimeOffset? EndedAtUtc { get; set; }
    public Guid? EndedByUserId { get; set; }
    public string? EndReason { get; set; }
}
