namespace LogisticsERP.Domain.Enums;

public enum AccidentCaseStage
{
    AwaitingNajm = 1, Assessed = 2, LocalRepair = 3, ClaimDraft = 4,
    AwaitingAssessment = 5, CompensationOffered = 6, AwaitingInsurance = 7,
    InsuranceRejected = 8, InsuranceApproved = 9, AwaitingSupplierTransfer = 10,
    RepairDirected = 11, Repairing = 12, TotalLossProposed = 13,
    AwaitingReinspection = 14, TotalLossConfirmed = 15, AwaitingValuation = 16,
    TotalLossValued = 17, Completed = 18
}
public enum AccidentClaimType { Repair = 1, Compensation = 2 }
public enum AccidentClaimOutcome { Repair = 1, Compensation = 2, TotalLoss = 3 }
public enum AccidentWorkflowAction
{
    AssessFault = 1, StartLocalRepair = 2, CompleteLocalRepair = 3, OpenClaim = 4,
    SubmitClaim = 5, ReceiveCompensationOffer = 6, SubmitToInsurance = 7,
    ApproveInsurance = 8, RejectInsurance = 9, SubmitToSupplier = 10,
    ConfirmTransfer = 11, ReceiveRepairDirection = 12, StartRepair = 13,
    RepairProgress = 14, CompleteRepair = 15, ProposeTotalLoss = 16,
    RequestReinspection = 17, ConfirmTotalLoss = 18, RecordVehicleCollection = 19,
    RecordValuation = 20, FollowUp = 21, SubmitInstallmentRefund = 22,
    ReceiveInstallmentRefund = 23, RejectInstallmentRefund = 24, MarkNoInstallments = 25
}
public enum AccidentRefundStatus { NotSubmitted = 1, Submitted = 2, Received = 3, Rejected = 4, NotApplicable = 5 }
