using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Fleet;

public static class AccidentWorkflowRules
{
    public static bool ValidFaultShares(decimal rider, IEnumerable<decimal> others)
    {
        var shares = others.ToArray();
        return rider is >= 0 and <= 100 && decimal.Round(rider, 2) == rider
            && shares.All(x => x is >= 0 and <= 100 && decimal.Round(x, 2) == x)
            && rider + shares.Sum() == 100;
    }

    public static bool RequiresOpeningFee(decimal riderFault, VehicleAccidentSeverity severity) =>
        riderFault == 100 && severity != VehicleAccidentSeverity.Minor;

    public static AccidentCaseStage? NextStage(AccidentCaseStage stage, AccidentWorkflowAction action) => (stage, action) switch
    {
        (AccidentCaseStage.AwaitingNajm or AccidentCaseStage.Assessed, AccidentWorkflowAction.AssessFault) => AccidentCaseStage.Assessed,
        (AccidentCaseStage.Assessed, AccidentWorkflowAction.StartLocalRepair) => AccidentCaseStage.LocalRepair,
        (AccidentCaseStage.LocalRepair, AccidentWorkflowAction.CompleteLocalRepair) => AccidentCaseStage.Completed,
        (AccidentCaseStage.Assessed, AccidentWorkflowAction.OpenClaim) => AccidentCaseStage.ClaimDraft,
        (AccidentCaseStage.ClaimDraft, AccidentWorkflowAction.SubmitClaim) => AccidentCaseStage.AwaitingAssessment,
        (AccidentCaseStage.AwaitingAssessment, AccidentWorkflowAction.ReceiveCompensationOffer) => AccidentCaseStage.CompensationOffered,
        (AccidentCaseStage.CompensationOffered or AccidentCaseStage.InsuranceRejected, AccidentWorkflowAction.SubmitToInsurance) => AccidentCaseStage.AwaitingInsurance,
        (AccidentCaseStage.AwaitingInsurance, AccidentWorkflowAction.ApproveInsurance) => AccidentCaseStage.InsuranceApproved,
        (AccidentCaseStage.AwaitingInsurance, AccidentWorkflowAction.RejectInsurance) => AccidentCaseStage.InsuranceRejected,
        (AccidentCaseStage.InsuranceApproved or AccidentCaseStage.TotalLossValued, AccidentWorkflowAction.SubmitToSupplier) => AccidentCaseStage.AwaitingSupplierTransfer,
        (AccidentCaseStage.AwaitingSupplierTransfer, AccidentWorkflowAction.ConfirmTransfer) => AccidentCaseStage.Completed,
        (AccidentCaseStage.AwaitingAssessment or AccidentCaseStage.AwaitingReinspection, AccidentWorkflowAction.ReceiveRepairDirection) => AccidentCaseStage.RepairDirected,
        (AccidentCaseStage.RepairDirected, AccidentWorkflowAction.StartRepair) => AccidentCaseStage.Repairing,
        (AccidentCaseStage.Repairing, AccidentWorkflowAction.RepairProgress) => AccidentCaseStage.Repairing,
        (AccidentCaseStage.Repairing, AccidentWorkflowAction.CompleteRepair) => AccidentCaseStage.Completed,
        (AccidentCaseStage.AwaitingAssessment, AccidentWorkflowAction.ProposeTotalLoss) => AccidentCaseStage.TotalLossProposed,
        (AccidentCaseStage.TotalLossProposed, AccidentWorkflowAction.RequestReinspection) => AccidentCaseStage.AwaitingReinspection,
        (AccidentCaseStage.TotalLossProposed or AccidentCaseStage.AwaitingReinspection, AccidentWorkflowAction.ConfirmTotalLoss) => AccidentCaseStage.TotalLossConfirmed,
        (AccidentCaseStage.TotalLossConfirmed, AccidentWorkflowAction.RecordVehicleCollection) => AccidentCaseStage.AwaitingValuation,
        (AccidentCaseStage.AwaitingValuation, AccidentWorkflowAction.RecordValuation) => AccidentCaseStage.TotalLossValued,
        (_, AccidentWorkflowAction.FollowUp) => stage,
        (AccidentCaseStage.Completed, AccidentWorkflowAction.SubmitInstallmentRefund or AccidentWorkflowAction.ReceiveInstallmentRefund or AccidentWorkflowAction.RejectInstallmentRefund or AccidentWorkflowAction.MarkNoInstallments) => stage,
        _ => null
    };

    public static int IncidentCalendarDays(DateTimeOffset start, DateTimeOffset end) =>
        DateOnly.FromDateTime(end.ToOffset(TimeSpan.FromHours(3)).DateTime).DayNumber
        - DateOnly.FromDateTime(start.ToOffset(TimeSpan.FromHours(3)).DateTime).DayNumber + 1;
}
