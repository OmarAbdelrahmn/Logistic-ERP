using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Fleet;

public sealed record AccidentFaultParty(string Name, decimal FaultPercentage, string? VehiclePlate = null, string? InsuranceCompany = null);

// Action-specific requirements are documented in docs/accident-workflow-api.md.
public sealed record AccidentWorkflowRequest(
    AccidentWorkflowAction Action, string RowVersion, string Notes, DateTimeOffset OccurredAtUtc,
    Guid? AttachmentId = null, decimal? Amount = null, decimal? RiderFaultPercentage = null,
    IReadOnlyList<AccidentFaultParty>? OtherParties = null, AccidentClaimType? ClaimType = null,
    Guid? SupplierId = null, string? Reference = null, string? Location = null,
    string? Contact = null, DateTimeOffset? AppointmentAtUtc = null);

public sealed record AccidentAttachmentMetadata(string? Description = null, string? FromLocation = null,
    string? ToLocation = null, DateTimeOffset? TransportedAtUtc = null, decimal? Amount = null);
public sealed record AccidentInstallmentRequest(string RowVersion, DateOnly PeriodFrom, DateOnly PeriodTo,
    DateOnly PaidOn, decimal Amount, decimal RefundEligibleAmount, Guid ReceiptAttachmentId, string Notes);
public sealed record AccidentInstallmentResponse(Guid Id, DateOnly PeriodFrom, DateOnly PeriodTo, DateOnly PaidOn,
    decimal Amount, decimal RefundEligibleAmount, Guid ReceiptAttachmentId, string Notes);
public sealed record AccidentSourceDocument(string Kind, Guid? VersionId, string? OriginalFileName, string? ContentType, string? DownloadUrl);
public sealed record AccidentTimelineEvent(Guid Id, VehicleAccidentEventType EventType, DateTimeOffset OccurredAtUtc,
    Guid ActorUserId, string Reason, string? SnapshotJson);
public sealed record AccidentWorkflowSummary(Guid AccidentId, string AccidentNumber, string? TrafficReportNumber, Guid VehicleId,
    Guid RiderProfileId, AccidentCaseStage Stage, AccidentClaimType? RequestedClaimType, AccidentClaimOutcome? Outcome,
    string? ClaimNumber, Guid? SupplierId, DateTimeOffset OccurredAtUtc, DateTimeOffset? IncidentEndedAtUtc,
    DateTimeOffset? DeadlineAtUtc, bool IsOverdue, AccidentRefundStatus RefundStatus, string RowVersion);
public sealed record AccidentFaultResponse(decimal? RiderFaultPercentage, IReadOnlyList<AccidentFaultParty> OtherParties,
    Guid? NajmAttachmentId, string? DamageAssessment, decimal? EstimatedRepairCost, Guid? DamagePromissoryNoteAttachmentId,
    decimal? OpeningFeeAmount, Guid? OpeningFeeAttachmentId, DateTimeOffset? OpeningFeePaidAtUtc);
public sealed record AccidentClaimResponse(AccidentClaimType? RequestedType, AccidentClaimOutcome? Outcome, Guid? SupplierId,
    string? Number, DateTimeOffset? SubmittedAtUtc, Guid? SubmissionAttachmentId);
public sealed record AccidentSettlementResponse(decimal? Amount, Guid? AssessmentReceiptAttachmentId,
    DateTimeOffset? InsuranceSubmittedAtUtc, DateTimeOffset? InsuranceDueAtUtc, DateTimeOffset? InsuranceRespondedAtUtc,
    string? InsuranceRejectionReason, Guid? PaymentReceiptAttachmentId, DateTimeOffset? SupplierSubmittedAtUtc,
    DateTimeOffset? SupplierTransferDueAtUtc, DateTimeOffset? TransferReceivedAtUtc, decimal? TransferReceivedAmount);
public sealed record AccidentRepairResponse(string? Location, string? Contact, DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc,
    string? ReinspectionLocation, DateTimeOffset? ReinspectionAppointmentAtUtc, DateTimeOffset? TotalLossConfirmedAtUtc,
    DateTimeOffset? VehicleCollectedAtUtc);
public sealed record AccidentRefundResponse(AccidentRefundStatus Status, string? Reference, decimal? RequestedAmount,
    decimal? ReceivedAmount, DateTimeOffset? SubmittedAtUtc, DateTimeOffset? ReceivedAtUtc,
    decimal RecordedPaidAmount, decimal RecordedEligibleAmount, IReadOnlyList<AccidentInstallmentResponse> Installments);
public sealed record AccidentWorkflowResponse(Guid AccidentId, AccidentCaseStage Stage, string RowVersion,
    DateTimeOffset IncidentStartedAtUtc, DateTimeOffset? IncidentEndedAtUtc, int IncidentCalendarDays,
    DateTimeOffset? DeadlineAtUtc, long? RemainingSeconds, bool IsOverdue,
    AccidentFaultResponse Fault, AccidentClaimResponse Claim, AccidentSettlementResponse Settlement,
    AccidentRepairResponse Repair, AccidentRefundResponse Refund, IReadOnlyList<AccidentSourceDocument> SourceDocuments,
    IReadOnlyList<VehicleAccidentAttachmentResponse> Attachments, IReadOnlyList<AccidentTimelineEvent> Timeline);

public interface IAccidentWorkflowService
{
    Task<Result<PagedResponse<AccidentWorkflowSummary>>> GetWorkflowQueueAsync(Guid? vehicleId, Guid? riderProfileId,
        AccidentCaseStage? stage, bool overdueOnly, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<AccidentWorkflowResponse>> GetWorkflowAsync(Guid accidentId, CancellationToken cancellationToken = default);
    Task<Result<AccidentWorkflowResponse>> ExecuteWorkflowAsync(Guid accidentId, AccidentWorkflowRequest request, CancellationToken cancellationToken = default);
    Task<Result<AccidentWorkflowResponse>> AddInstallmentAsync(Guid accidentId, AccidentInstallmentRequest request, CancellationToken cancellationToken = default);
    Task<Result<VehicleAccidentAttachmentResponse>> UploadWorkflowAttachmentAsync(Guid accidentId, VehicleAccidentEvidenceType type,
        AccidentAttachmentMetadata metadata, PrivateFileUpload file, CancellationToken cancellationToken = default);
    Task<Result<PrivateFileDownload>> DownloadSourceDocumentAsync(Guid accidentId, string kind, CancellationToken cancellationToken = default);
}

public interface IAccidentNotificationService
{
    Task QueueAsync(Guid accidentId, Guid vehicleId, string accidentNumber, string eventKey, string description,
        CancellationToken cancellationToken = default);
    Task RunDueNotificationsAsync(CancellationToken cancellationToken = default);
}
