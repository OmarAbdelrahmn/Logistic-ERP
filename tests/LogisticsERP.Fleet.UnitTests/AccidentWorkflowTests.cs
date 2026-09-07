using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using LogisticsERP.Infrastructure.SystemServices;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class AccidentWorkflowTests
{
    [Theory]
    [InlineData(100, 0, true)]
    [InlineData(75, 25, true)]
    [InlineData(0, 100, true)]
    [InlineData(101, -1, false)]
    [InlineData(75, 24, false)]
    [InlineData(-1, 101, false)]
    public void FaultSharesMustAccountForExactlyOneHundredPercent(decimal rider, decimal other, bool expected) =>
        Assert.Equal(expected, AccidentWorkflowRules.ValidFaultShares(rider, [other]));

    [Fact]
    public void MultiplePartiesAndDecimalSharesAreSupportedWithoutRoundingAwayErrors()
    {
        Assert.True(AccidentWorkflowRules.ValidFaultShares(33.33m, [33.33m, 33.34m]));
        Assert.False(AccidentWorkflowRules.ValidFaultShares(33.333m, [66.667m]));
    }

    [Theory]
    [InlineData(100, VehicleAccidentSeverity.Serious, true)]
    [InlineData(100, VehicleAccidentSeverity.Minor, false)]
    [InlineData(75, VehicleAccidentSeverity.Serious, false)]
    [InlineData(0, VehicleAccidentSeverity.Critical, false)]
    public void OnlyFullyResponsibleNonMinorClaimsRequireOpeningFee(decimal rider, VehicleAccidentSeverity severity, bool required) =>
        Assert.Equal(required, AccidentWorkflowRules.RequiresOpeningFee(rider, severity));

    [Theory]
    [InlineData(AccidentCaseStage.AwaitingNajm, AccidentWorkflowAction.OpenClaim)]
    [InlineData(AccidentCaseStage.ClaimDraft, AccidentWorkflowAction.ConfirmTransfer)]
    [InlineData(AccidentCaseStage.AwaitingAssessment, AccidentWorkflowAction.CompleteRepair)]
    [InlineData(AccidentCaseStage.TotalLossProposed, AccidentWorkflowAction.RecordVehicleCollection)]
    [InlineData(AccidentCaseStage.AwaitingInsurance, AccidentWorkflowAction.SubmitToSupplier)]
    [InlineData(AccidentCaseStage.InsuranceRejected, AccidentWorkflowAction.ApproveInsurance)]
    public void CannotSkipRequiredWorkflowStages(AccidentCaseStage stage, AccidentWorkflowAction action) =>
        Assert.Null(AccidentWorkflowRules.NextStage(stage, action));

    [Fact]
    public void ReinspectionCanConfirmTotalLossOrRedirectToRepair()
    {
        Assert.Equal(AccidentCaseStage.TotalLossConfirmed, AccidentWorkflowRules.NextStage(AccidentCaseStage.AwaitingReinspection, AccidentWorkflowAction.ConfirmTotalLoss));
        Assert.Equal(AccidentCaseStage.RepairDirected, AccidentWorkflowRules.NextStage(AccidentCaseStage.AwaitingReinspection, AccidentWorkflowAction.ReceiveRepairDirection));
    }

    [Fact]
    public void IncidentPeriodUsesInclusiveRiyadhCalendarDates()
    {
        var start = DateTimeOffset.Parse("2026-08-01T20:59:00Z", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(1, AccidentWorkflowRules.IncidentCalendarDays(start, start));
        Assert.Equal(2, AccidentWorkflowRules.IncidentCalendarDays(start, start.AddMinutes(2)));
    }

    [Fact]
    public void NotificationCursorPreservesTimeAndTieBreakerAndAcceptsLegacyCursors()
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        Assert.True(NotificationService.TryParseCursor($"{now.UtcTicks}:{id:N}", out var before, out var parsed));
        Assert.Equal(now, before); Assert.Equal(id, parsed);
        Assert.True(NotificationService.TryParseCursor(now.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture), out _, out var legacy));
        Assert.Null(legacy);
        Assert.False(NotificationService.TryParseCursor("9223372036854775807", out _, out _));
        Assert.False(NotificationService.TryParseCursor($"{now.UtcTicks}:invalid", out _, out _));
    }
}
