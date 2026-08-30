using System.Reflection;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehiclePlatformAccountSwitchTransitionTests
{
    [Fact]
    public void SwitchTransitionEndsSourceAndCreatesApprovedTargetAssignment()
    {
        var sourceVehicleId = Guid.NewGuid();
        var targetVehicleId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assignedAt = new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero);
        var effectiveAt = new DateTimeOffset(2026, 8, 30, 8, 0, 0, TimeSpan.Zero);
        var acceptedAt = effectiveAt.AddMinutes(5);
        var source = new VehiclePlatformAccountAssignment
        {
            VehicleId = sourceVehicleId,
            PlatformRiderAccountId = accountId,
            AssignedAtUtc = assignedAt,
            Status = VehiclePlatformAccountAssignmentStatus.Active
        };
        var method = typeof(VehiclePlatformAccountAssignmentService).GetMethod(
            "ApplyVehicleSwitch",
            BindingFlags.NonPublic | BindingFlags.Static);

        var target = Assert.IsType<VehiclePlatformAccountAssignment>(method!.Invoke(
            null,
            [source, targetVehicleId, effectiveAt, acceptedAt, userId, "Physical vehicle handover"]));

        Assert.Equal(VehiclePlatformAccountAssignmentStatus.Ended, source.Status);
        Assert.Equal(effectiveAt, source.EndedAtUtc);
        Assert.Equal(userId, source.EndedByUserId);
        Assert.Equal(targetVehicleId, target.VehicleId);
        Assert.Equal(accountId, target.PlatformRiderAccountId);
        Assert.Equal(effectiveAt, target.AssignedAtUtc);
        Assert.Equal(VehiclePlatformAccountApprovalStatus.Approved, target.ApprovalStatus);
        Assert.Equal(VehiclePlatformAccountAssignmentStatus.Active, target.Status);
        Assert.NotEqual(Guid.Empty, target.Id);
    }
}
