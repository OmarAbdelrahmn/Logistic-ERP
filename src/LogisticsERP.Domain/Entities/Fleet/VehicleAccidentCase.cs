using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Fleet;

public sealed class VehicleAccidentCase : AuditableEntity
{
    public Guid VehicleAccidentId { get; set; }
    public AccidentCaseStage Stage { get; set; } = AccidentCaseStage.AwaitingNajm;
    public decimal? RiderFaultPercentage { get; set; }
    public string OtherPartiesJson { get; set; } = "[]";
    public Guid? NajmAttachmentId { get; set; }
    public string? DamageAssessment { get; set; }
    public decimal? EstimatedRepairCost { get; set; }
    public Guid? DamagePromissoryNoteAttachmentId { get; set; }
    public decimal? OpeningFeeAmount { get; set; }
    public Guid? OpeningFeeAttachmentId { get; set; }
    public DateTimeOffset? OpeningFeePaidAtUtc { get; set; }
    public AccidentClaimType? RequestedClaimType { get; set; }
    public AccidentClaimOutcome? Outcome { get; set; }
    public Guid? SupplierId { get; set; }
    public string? ClaimNumber { get; set; }
    public DateTimeOffset? ClaimSubmittedAtUtc { get; set; }
    public Guid? ClaimSubmissionAttachmentId { get; set; }
    public Guid? IqamaVersionId { get; set; }
    public Guid? LicenseVersionId { get; set; }
    public Guid? RegistrationVersionId { get; set; }
    public decimal? SettlementAmount { get; set; }
    public Guid? AssessmentReceiptAttachmentId { get; set; }
    public DateTimeOffset? InsuranceSubmittedAtUtc { get; set; }
    public DateTimeOffset? InsuranceDueAtUtc { get; set; }
    public DateTimeOffset? InsuranceRespondedAtUtc { get; set; }
    public string? InsuranceRejectionReason { get; set; }
    public Guid? PaymentReceiptAttachmentId { get; set; }
    public DateTimeOffset? SupplierSubmittedAtUtc { get; set; }
    public DateTimeOffset? SupplierTransferDueAtUtc { get; set; }
    public DateTimeOffset? TransferReceivedAtUtc { get; set; }
    public decimal? TransferReceivedAmount { get; set; }
    public string? RepairLocation { get; set; }
    public string? RepairContact { get; set; }
    public DateTimeOffset? RepairStartedAtUtc { get; set; }
    public DateTimeOffset? RepairCompletedAtUtc { get; set; }
    public string? ReinspectionLocation { get; set; }
    public DateTimeOffset? ReinspectionAppointmentAtUtc { get; set; }
    public DateTimeOffset? TotalLossConfirmedAtUtc { get; set; }
    public DateTimeOffset? VehicleCollectedAtUtc { get; set; }
    public DateTimeOffset? IncidentEndedAtUtc { get; set; }
    public DateTimeOffset? LastActionAtUtc { get; set; }
    public AccidentRefundStatus RefundStatus { get; set; } = AccidentRefundStatus.NotSubmitted;
    public string? RefundReference { get; set; }
    public decimal? RefundRequestedAmount { get; set; }
    public decimal? RefundReceivedAmount { get; set; }
    public DateTimeOffset? RefundSubmittedAtUtc { get; set; }
    public DateTimeOffset? RefundReceivedAtUtc { get; set; }
}

public sealed class VehicleAccidentInstallment : AuditableEntity
{
    public Guid VehicleAccidentId { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly PaidOn { get; set; }
    public decimal Amount { get; set; }
    public decimal RefundEligibleAmount { get; set; }
    public Guid ReceiptAttachmentId { get; set; }
    public string Notes { get; set; } = string.Empty;
}
