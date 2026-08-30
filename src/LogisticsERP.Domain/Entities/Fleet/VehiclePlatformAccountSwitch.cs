using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Fleet;

public sealed class VehiclePlatformAccountSwitch : AuditableEntity
{
    public Guid SourceAssignmentId { get; set; }
    public Guid SourceVehicleId { get; set; }
    public Guid TargetVehicleId { get; set; }
    public Guid PlatformRiderAccountId { get; set; }
    public VehiclePlatformAccountSwitchMode Mode { get; set; }
    public VehiclePlatformAccountSwitchStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateTimeOffset? EffectiveAtUtc { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public Guid? NewAssignmentId { get; set; }
}
