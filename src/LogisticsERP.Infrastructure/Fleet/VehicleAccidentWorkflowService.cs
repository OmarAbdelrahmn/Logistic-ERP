using System.Text.Json;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed partial class VehicleAccidentService
{
    private static OperationError WorkflowError(string message) => new("Fleet.AccidentWorkflow", message, ErrorType.Validation);
    private static bool ValidMoney(decimal? value) => value is > 0 and <= 9999999999999999.99m && decimal.Round(value.Value, 2) == value;

    public async Task<Result<PagedResponse<AccidentWorkflowSummary>>> GetWorkflowQueueAsync(Guid? vehicleId, Guid? riderProfileId,
        AccidentCaseStage? stage, bool overdueOnly, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AccidentsRead, null, cancellationToken))
            return Result.Failure<PagedResponse<AccidentWorkflowSummary>>(FleetErrors.Forbidden);
        if (stage.HasValue && !Enum.IsDefined(stage.Value)) return Result.Failure<PagedResponse<AccidentWorkflowSummary>>(FleetErrors.InvalidRequest);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var now = support.UtcNow;
        var query = from item in dbContext.VehicleAccidentCases.AsNoTracking()
                    join accident in dbContext.VehicleAccidents.AsNoTracking() on item.VehicleAccidentId equals accident.Id
                    join vehicle in dbContext.Vehicles.AsNoTracking() on accident.VehicleId equals vehicle.Id
                    where (!vehicleId.HasValue || accident.VehicleId == vehicleId) && (!riderProfileId.HasValue || accident.RiderProfileId == riderProfileId)
                        && (!stage.HasValue || item.Stage == stage)
                    select new { Item = item, Accident = accident, Deadline =
                        item.Stage == AccidentCaseStage.AwaitingInsurance ? item.InsuranceDueAtUtc
                        : item.Stage == AccidentCaseStage.AwaitingSupplierTransfer ? item.SupplierTransferDueAtUtc : null };
        if (overdueOnly) query = query.Where(x => x.Deadline < now && x.Accident.Status != VehicleAccidentStatus.Closed);
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.Accident.OccurredAtUtc).ThenByDescending(x => x.Accident.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        return Result.Success(new PagedResponse<AccidentWorkflowSummary>(rows.Select(x => new AccidentWorkflowSummary(x.Accident.Id,
            x.Accident.AccidentNumber, x.Accident.PoliceReportNumber, x.Accident.VehicleId, x.Accident.RiderProfileId,
            x.Item.Stage, x.Item.RequestedClaimType, x.Item.Outcome, x.Item.ClaimNumber, x.Item.SupplierId, x.Accident.OccurredAtUtc,
            x.Item.IncidentEndedAtUtc, x.Deadline, x.Deadline < now, x.Item.RefundStatus, FleetServiceSupport.EncodeRowVersion(x.Item.RowVersion))).ToArray(), page, pageSize, count));
    }

    public async Task<Result<AccidentWorkflowResponse>> GetWorkflowAsync(Guid accidentId, CancellationToken cancellationToken = default)
    {
        var accident = await dbContext.VehicleAccidents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accidentId, cancellationToken);
        if (accident is null) return Result.Failure<AccidentWorkflowResponse>(FleetErrors.NotFound);
        var access = await GetVehicleAsync(accident.VehicleId, PermissionKeys.Fleet.AccidentsRead, cancellationToken);
        if (access.IsFailure) return Result.Failure<AccidentWorkflowResponse>(access.Error);
        var item = await dbContext.VehicleAccidentCases.AsNoTracking().SingleOrDefaultAsync(x => x.VehicleAccidentId == accidentId, cancellationToken);
        return item is null ? Result.Failure<AccidentWorkflowResponse>(FleetErrors.NotFound)
            : Result.Success(await BuildWorkflowAsync(accident, item, cancellationToken));
    }

    public async Task<Result<AccidentWorkflowResponse>> ExecuteWorkflowAsync(Guid accidentId, AccidentWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        var accident = await dbContext.VehicleAccidents.SingleOrDefaultAsync(x => x.Id == accidentId, cancellationToken);
        if (accident is null) return Result.Failure<AccidentWorkflowResponse>(FleetErrors.NotFound);
        var access = await GetVehicleAsync(accident.VehicleId, PermissionKeys.Fleet.AccidentsFinalize, cancellationToken, tracking: true);
        if (access.IsFailure) return Result.Failure<AccidentWorkflowResponse>(access.Error);
        var item = await dbContext.VehicleAccidentCases.SingleOrDefaultAsync(x => x.VehicleAccidentId == accidentId, cancellationToken);
        if (item is null) return Result.Failure<AccidentWorkflowResponse>(FleetErrors.NotFound);
        if (!FleetServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion)) return Result.Failure<AccidentWorkflowResponse>(FleetErrors.Conflict);
        if (accident.Status == VehicleAccidentStatus.Closed) return Result.Failure<AccidentWorkflowResponse>(FleetErrors.InvalidState);
        var next = AccidentWorkflowRules.NextStage(item.Stage, request.Action);
        if (next is null) return Result.Failure<AccidentWorkflowResponse>(WorkflowError($"Action {request.Action} is not allowed in {item.Stage}."));
        if (string.IsNullOrWhiteSpace(request.Notes) || request.Notes.Length > 1000
            || request.OccurredAtUtc < accident.OccurredAtUtc || request.OccurredAtUtc > support.UtcNow
            || item.LastActionAtUtc.HasValue && request.OccurredAtUtc < item.LastActionAtUtc
            || request.Reference?.Length > 150 || request.Location?.Length > 1000 || request.Contact?.Length > 300)
            return Result.Failure<AccidentWorkflowResponse>(WorkflowError("Provide notes and a chronological action time between the accident and now; respect field length limits."));
        var files = await dbContext.VehicleAccidentAttachments.AsNoTracking().Where(x => x.VehicleAccidentId == accidentId).ToArrayAsync(cancellationToken);
        var documents = await LoadSourceDocumentsAsync(accident, item, cancellationToken);
        var error = ValidateAction(accident, item, request, files, documents);
        if (error is not null) return Result.Failure<AccidentWorkflowResponse>(WorkflowError(error));
        if (request.Action == AccidentWorkflowAction.SubmitClaim)
        {
            foreach (var document in documents)
            {
                var available = await fileStorage.OpenReadAsync(document.Path!, document.ContentType!, document.Name!, document.Length, cancellationToken);
                if (available.IsFailure) return Result.Failure<AccidentWorkflowResponse>(WorkflowError($"The {document.Kind} file is missing from storage; upload it to the source record before submitting."));
                await available.Value!.Content.DisposeAsync();
            }
        }
        if (request.Action == AccidentWorkflowAction.OpenClaim)
        {
            var supplierId = request.SupplierId ?? access.Value!.PurchasedFromSupplierId;
            if (!supplierId.HasValue || !await dbContext.VehicleSuppliers.AnyAsync(x => x.Id == supplierId && x.Status == VehicleCatalogStatus.Active, cancellationToken))
                return Result.Failure<AccidentWorkflowResponse>(WorkflowError("Select an active vehicle purchase supplier."));
            item.SupplierId = supplierId;
        }
        if (request.Action == AccidentWorkflowAction.SubmitInstallmentRefund)
        {
            var installments = await dbContext.VehicleAccidentInstallments.AsNoTracking().Where(x => x.VehicleAccidentId == accidentId).ToArrayAsync(cancellationToken);
            if (installments.Length == 0 || request.Amount != installments.Sum(x => x.RefundEligibleAmount))
                return Result.Failure<AccidentWorkflowResponse>(WorkflowError("Refund amount must equal the eligible total of recorded paid installments."));
        }
        if (request.Action == AccidentWorkflowAction.MarkNoInstallments
            && (item.RefundStatus != AccidentRefundStatus.NotSubmitted
                || await dbContext.VehicleAccidentInstallments.AnyAsync(x => x.VehicleAccidentId == accidentId, cancellationToken)))
            return Result.Failure<AccidentWorkflowResponse>(WorkflowError("Only an unsubmitted refund with no recorded installments can be marked not applicable; explain why in notes."));
        var fromStage = item.Stage;
        ApplyWorkflowAction(accident, item, request, documents);
        item.Stage = next.Value;
        item.LastActionAtUtc = request.OccurredAtUtc;
        item.UpdatedByUserId = support.UserId;
        item.UpdatedAtUtc = support.UtcNow;
        dbContext.Entry(item).Property(x => x.UpdatedAtUtc).IsModified = true;
        // Serialize workflow writes with legacy close/correct operations as well as other workflow commands.
        accident.UpdatedAtUtc = support.UtcNow;
        dbContext.Entry(accident).Property(x => x.UpdatedAtUtc).IsModified = true;
        if (request.Action is AccidentWorkflowAction.OpenClaim or AccidentWorkflowAction.StartLocalRepair)
        {
            var issue = await dbContext.VehicleIssues.SingleAsync(x => x.Id == accident.VehicleIssueId, cancellationToken);
            issue.BlocksOperation = true;
            issue.Status = VehicleIssueStatus.UnderReview;
            issue.IsRiderResponsible = item.RiderFaultPercentage == 100;
            issue.EstimatedRepairCost = item.EstimatedRepairCost;
            await SetAccidentHoldAsync(access.Value!, accident, support.UserId!.Value, cancellationToken);
        }
        if (request.Action == AccidentWorkflowAction.RecordVehicleCollection)
            await DecommissionAfterTotalLossAsync(access.Value!, accident, request.OccurredAtUtc, cancellationToken);
        if (request.Action is AccidentWorkflowAction.CompleteLocalRepair or AccidentWorkflowAction.CompleteRepair)
            await ResolveAccidentHoldAsync(access.Value!, accident, request.Notes, cancellationToken);
        var eventId = Guid.CreateVersion7();
        dbContext.VehicleAccidentEvents.Add(new VehicleAccidentEvent
        {
            Id = eventId, VehicleAccidentId = accidentId, EventType = VehicleAccidentEventType.WorkflowAction,
            ActorUserId = support.UserId!.Value, OccurredAtUtc = request.OccurredAtUtc, Reason = request.Notes.Trim(),
            SnapshotJson = JsonSerializer.Serialize(new { FromStage = fromStage, ToStage = item.Stage, Request = request })
        });
        await notifications.QueueAsync(accident.Id, accident.VehicleId, accident.AccidentNumber, $"action:{eventId:N}", $"{request.Action}: {item.Stage}", cancellationToken);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Result.Failure<AccidentWorkflowResponse>(FleetErrors.Conflict); }
        return Result.Success(await BuildWorkflowAsync(accident, item, cancellationToken));
    }

    private static string? ValidateAction(VehicleAccident accident, VehicleAccidentCase item, AccidentWorkflowRequest r,
        IReadOnlyList<VehicleAccidentAttachment> files, IReadOnlyList<SourceFile> documents)
    {
        bool Has(VehicleAccidentEvidenceType type) => files.Any(x => x.Id == r.AttachmentId && x.EvidenceType == type && x.ContentType == "application/pdf");
        switch (r.Action)
        {
            case AccidentWorkflowAction.AssessFault:
                if (!Has(VehicleAccidentEvidenceType.NajmReport) || !r.RiderFaultPercentage.HasValue || r.OtherParties is null
                    || r.OtherParties.Any(x => x is null || string.IsNullOrWhiteSpace(x.Name) || x.Name.Length > 300 || x.VehiclePlate?.Length > 100 || x.InsuranceCompany?.Length > 300)
                    || !AccidentWorkflowRules.ValidFaultShares(r.RiderFaultPercentage.Value, r.OtherParties.Select(x => x.FaultPercentage)))
                    return "Attach the Najm PDF and enter each party's fault share; shares must total exactly 100%.";
                break;
            case AccidentWorkflowAction.StartLocalRepair:
                if (item.RiderFaultPercentage != 100 || accident.Severity != VehicleAccidentSeverity.Minor
                    || !ValidMoney(r.Amount) || !Has(VehicleAccidentEvidenceType.DamagePromissoryNote)
                    || files.Count(x => x.EvidenceType == VehicleAccidentEvidenceType.DamagePhoto && x.ContentType.StartsWith("image/", StringComparison.Ordinal)) < 2)
                    return "Local repair requires a minor accident, 100% rider fault, at least two damage photos, an estimated cost and its promissory note PDF. Notes describe the assessed damage.";
                break;
            case AccidentWorkflowAction.OpenClaim:
                if (!item.RiderFaultPercentage.HasValue || !r.ClaimType.HasValue || !Enum.IsDefined(r.ClaimType.Value)) return "Choose Repair or Compensation as the requested claim type.";
                if (item.RiderFaultPercentage == 100 && accident.Severity == VehicleAccidentSeverity.Minor)
                    return "A minor accident with 100% rider fault follows local repair.";
                if (AccidentWorkflowRules.RequiresOpeningFee(item.RiderFaultPercentage.Value, accident.Severity)
                    && (r.Amount != 2500 || !Has(VehicleAccidentEvidenceType.ClaimOpeningFeeReceipt)))
                    return "A non-minor accident with 100% rider fault requires a paid SAR 2500 opening fee and its PDF receipt.";
                break;
            case AccidentWorkflowAction.SubmitClaim:
                if (string.IsNullOrWhiteSpace(r.Reference) || !Has(VehicleAccidentEvidenceType.ClaimSubmissionReport) || documents.Any(x => x.VersionId is null))
                    return "Submission requires the external claim number, submission PDF, and the driver's iqama, license and vehicle registration from the system.";
                break;
            case AccidentWorkflowAction.ReceiveCompensationOffer:
                if (!ValidMoney(r.Amount) || !Has(VehicleAccidentEvidenceType.AssessmentReceipt)) return "Provide the compensation amount and assessment receipt PDF.";
                break;
            case AccidentWorkflowAction.ApproveInsurance:
                if (!Has(VehicleAccidentEvidenceType.PaymentReceipt)) return "Attach the approved payment receipt PDF.";
                break;
            case AccidentWorkflowAction.RejectInsurance:
                if (!Has(VehicleAccidentEvidenceType.InsuranceDecision)) return "Attach the rejection PDF and record the reason in notes.";
                break;
            case AccidentWorkflowAction.SubmitToSupplier:
                if (!item.SupplierId.HasValue || !Has(VehicleAccidentEvidenceType.ClaimSubmissionReport)) return "Supplier handoff requires a supplier and proof-of-submission PDF.";
                break;
            case AccidentWorkflowAction.ConfirmTransfer:
                if (!ValidMoney(r.Amount) || !Has(VehicleAccidentEvidenceType.TransferReceipt)) return "Record the actual amount received by our company and the transfer receipt PDF.";
                if (r.Amount != item.SettlementAmount) return "The received transfer must settle the assessed amount; record partial payments as follow-ups until fully received.";
                break;
            case AccidentWorkflowAction.ReceiveRepairDirection:
                if (string.IsNullOrWhiteSpace(r.Location) || string.IsNullOrWhiteSpace(r.Contact) || !Has(VehicleAccidentEvidenceType.RepairDirection)) return "Provide the repair location, contact and direction PDF.";
                break;
            case AccidentWorkflowAction.CompleteRepair:
            case AccidentWorkflowAction.CompleteLocalRepair:
                if (!Has(VehicleAccidentEvidenceType.RepairCompletion)) return "Attach the repair completion PDF.";
                break;
            case AccidentWorkflowAction.ProposeTotalLoss:
                if (!Has(VehicleAccidentEvidenceType.AssessmentReceipt)) return "Attach the total-loss assessment PDF.";
                break;
            case AccidentWorkflowAction.RequestReinspection:
                if (string.IsNullOrWhiteSpace(r.Location) || !r.AppointmentAtUtc.HasValue || r.AppointmentAtUtc < r.OccurredAtUtc)
                    return "Provide the reinspection location and appointment time.";
                break;
            case AccidentWorkflowAction.ConfirmTotalLoss:
                if (!Has(VehicleAccidentEvidenceType.TotalLossConfirmation)) return "Attach the final total-loss confirmation PDF.";
                break;
            case AccidentWorkflowAction.RecordVehicleCollection:
                if (!Has(VehicleAccidentEvidenceType.VehicleCollectionReceipt)) return "Attach the vehicle collection receipt PDF.";
                break;
            case AccidentWorkflowAction.RecordValuation:
                if (!ValidMoney(r.Amount) || !Has(VehicleAccidentEvidenceType.ValuationReceipt)) return "Provide the final vehicle valuation and its PDF receipt.";
                break;
            case AccidentWorkflowAction.FollowUp:
            case AccidentWorkflowAction.RepairProgress:
                if (r.AttachmentId.HasValue && !files.Any(x => x.Id == r.AttachmentId)) return "Attachment must belong to this accident.";
                break;
            case AccidentWorkflowAction.SubmitInstallmentRefund:
                if (!item.RequestedClaimType.HasValue || !item.IncidentEndedAtUtc.HasValue
                    || item.RefundStatus is not (AccidentRefundStatus.NotSubmitted or AccidentRefundStatus.Rejected)
                    || string.IsNullOrWhiteSpace(r.Reference) || !ValidMoney(r.Amount) || !Has(VehicleAccidentEvidenceType.InstallmentRefundRequest))
                    return "An ended claim, refund reference, amount and request PDF are required; a pending or received refund cannot be resubmitted.";
                break;
            case AccidentWorkflowAction.ReceiveInstallmentRefund:
                if (item.RefundStatus != AccidentRefundStatus.Submitted || !ValidMoney(r.Amount)
                    || r.Amount > item.RefundRequestedAmount || !Has(VehicleAccidentEvidenceType.InstallmentRefundReceipt))
                    return "A pending refund and its receipt PDF are required; record an amount up to the requested amount and explain any shortfall in notes.";
                break;
            case AccidentWorkflowAction.RejectInstallmentRefund:
                if (item.RefundStatus != AccidentRefundStatus.Submitted || !Has(VehicleAccidentEvidenceType.InsuranceDecision)) return "A pending refund, rejection PDF and reason are required.";
                break;
        }
        return null;
    }

    private static void ApplyWorkflowAction(VehicleAccident accident, VehicleAccidentCase item, AccidentWorkflowRequest r, IReadOnlyList<SourceFile> documents)
    {
        var at = r.OccurredAtUtc;
        switch (r.Action)
        {
            case AccidentWorkflowAction.AssessFault:
                item.RiderFaultPercentage = r.RiderFaultPercentage; item.OtherPartiesJson = JsonSerializer.Serialize(r.OtherParties);
                item.NajmAttachmentId = r.AttachmentId; break;
            case AccidentWorkflowAction.StartLocalRepair:
                item.EstimatedRepairCost = r.Amount; item.DamageAssessment = r.Notes.Trim(); item.DamagePromissoryNoteAttachmentId = r.AttachmentId;
                item.RepairStartedAtUtc = at; break;
            case AccidentWorkflowAction.OpenClaim:
                item.RequestedClaimType = r.ClaimType;
                if (AccidentWorkflowRules.RequiresOpeningFee(item.RiderFaultPercentage!.Value, accident.Severity))
                { item.OpeningFeeAmount = 2500; item.OpeningFeeAttachmentId = r.AttachmentId; item.OpeningFeePaidAtUtc = at; }
                break;
            case AccidentWorkflowAction.SubmitClaim:
                item.ClaimNumber = r.Reference!.Trim(); accident.InsuranceClaimNumber = item.ClaimNumber;
                item.ClaimSubmittedAtUtc = at; item.ClaimSubmissionAttachmentId = r.AttachmentId;
                item.IqamaVersionId = documents.Single(x => x.Kind == "iqama").VersionId;
                item.LicenseVersionId = documents.Single(x => x.Kind == "license").VersionId;
                item.RegistrationVersionId = documents.Single(x => x.Kind == "registration").VersionId;
                break;
            case AccidentWorkflowAction.ReceiveCompensationOffer:
                item.Outcome = AccidentClaimOutcome.Compensation; item.SettlementAmount = r.Amount; item.AssessmentReceiptAttachmentId = r.AttachmentId; break;
            case AccidentWorkflowAction.SubmitToInsurance:
                item.InsuranceSubmittedAtUtc = at; item.InsuranceDueAtUtc = at.AddDays(15); item.InsuranceRespondedAtUtc = null; item.InsuranceRejectionReason = null; break;
            case AccidentWorkflowAction.ApproveInsurance:
                item.InsuranceRespondedAtUtc = at; item.PaymentReceiptAttachmentId = r.AttachmentId; break;
            case AccidentWorkflowAction.RejectInsurance:
                item.InsuranceRespondedAtUtc = at; item.InsuranceRejectionReason = r.Notes.Trim(); break;
            case AccidentWorkflowAction.SubmitToSupplier:
                item.SupplierSubmittedAtUtc = at; item.SupplierTransferDueAtUtc = at.AddDays(10); break;
            case AccidentWorkflowAction.ConfirmTransfer:
                item.TransferReceivedAtUtc = at; item.TransferReceivedAmount = r.Amount;
                item.IncidentEndedAtUtc ??= at; break;
            case AccidentWorkflowAction.ReceiveRepairDirection:
                item.Outcome = AccidentClaimOutcome.Repair; item.RepairLocation = r.Location!.Trim(); item.RepairContact = r.Contact!.Trim(); break;
            case AccidentWorkflowAction.StartRepair: item.RepairStartedAtUtc = at; break;
            case AccidentWorkflowAction.CompleteRepair:
            case AccidentWorkflowAction.CompleteLocalRepair:
                item.RepairCompletedAtUtc = at; item.IncidentEndedAtUtc = at; break;
            case AccidentWorkflowAction.ProposeTotalLoss:
                item.Outcome = AccidentClaimOutcome.TotalLoss; item.AssessmentReceiptAttachmentId = r.AttachmentId; break;
            case AccidentWorkflowAction.RequestReinspection:
                item.ReinspectionLocation = r.Location!.Trim(); item.ReinspectionAppointmentAtUtc = r.AppointmentAtUtc; break;
            case AccidentWorkflowAction.ConfirmTotalLoss: item.TotalLossConfirmedAtUtc = at; break;
            case AccidentWorkflowAction.RecordVehicleCollection: item.VehicleCollectedAtUtc = at; item.IncidentEndedAtUtc = at; break;
            case AccidentWorkflowAction.RecordValuation: item.SettlementAmount = r.Amount; item.PaymentReceiptAttachmentId = r.AttachmentId; break;
            case AccidentWorkflowAction.SubmitInstallmentRefund:
                item.RefundStatus = AccidentRefundStatus.Submitted; item.RefundReference = r.Reference!.Trim(); item.RefundRequestedAmount = r.Amount; item.RefundSubmittedAtUtc = at; break;
            case AccidentWorkflowAction.ReceiveInstallmentRefund:
                item.RefundStatus = AccidentRefundStatus.Received; item.RefundReceivedAmount = r.Amount; item.RefundReceivedAtUtc = at; break;
            case AccidentWorkflowAction.RejectInstallmentRefund: item.RefundStatus = AccidentRefundStatus.Rejected; break;
            case AccidentWorkflowAction.MarkNoInstallments: item.RefundStatus = AccidentRefundStatus.NotApplicable; break;
        }
    }

    public async Task<Result<VehicleAccidentAttachmentResponse>> UploadWorkflowAttachmentAsync(Guid accidentId, VehicleAccidentEvidenceType type,
        AccidentAttachmentMetadata metadata, PrivateFileUpload file, CancellationToken cancellationToken = default)
    {
        var accident = await dbContext.VehicleAccidents.SingleOrDefaultAsync(x => x.Id == accidentId, cancellationToken);
        if (accident is null) return Result.Failure<VehicleAccidentAttachmentResponse>(FleetErrors.NotFound);
        var access = await GetVehicleAsync(accident.VehicleId, PermissionKeys.Fleet.AccidentsReport, cancellationToken);
        if (access.IsFailure) return Result.Failure<VehicleAccidentAttachmentResponse>(access.Error);
        if (accident.Status == VehicleAccidentStatus.Closed) return Result.Failure<VehicleAccidentAttachmentResponse>(FleetErrors.InvalidState);
        var isImage = type is VehicleAccidentEvidenceType.Image or VehicleAccidentEvidenceType.DamagePhoto;
        if (!Enum.IsDefined(type) || isImage && !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || !isImage && type != VehicleAccidentEvidenceType.Other && !string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || metadata.Description?.Length > 1000 || metadata.FromLocation?.Length > 1000 || metadata.ToLocation?.Length > 1000
            || metadata.Amount.HasValue && !ValidMoney(metadata.Amount))
            return Result.Failure<VehicleAccidentAttachmentResponse>(FleetErrors.InvalidFile);
        if (type == VehicleAccidentEvidenceType.TowingReceipt && (string.IsNullOrWhiteSpace(metadata.Description)
            || string.IsNullOrWhiteSpace(metadata.FromLocation) || string.IsNullOrWhiteSpace(metadata.ToLocation)
            || !metadata.TransportedAtUtc.HasValue || metadata.TransportedAtUtc < accident.OccurredAtUtc || metadata.TransportedAtUtc > support.UtcNow || !ValidMoney(metadata.Amount)))
            return Result.Failure<VehicleAccidentAttachmentResponse>(WorkflowError("A towing receipt requires a description, origin, destination, transport time and amount."));
        var id = Guid.CreateVersion7();
        var stored = await fileStorage.StoreAsync($"vehicle-accidents/{accidentId:N}/evidence/{id:N}", file, MaximumEvidenceSize, cancellationToken);
        if (stored.IsFailure) return Result.Failure<VehicleAccidentAttachmentResponse>(FleetErrors.InvalidFile);
        var s = stored.Value!;
        var attachment = new VehicleAccidentAttachment
        {
            Id = id, VehicleAccidentId = accidentId, EvidenceType = type, OriginalFileName = s.OriginalFileName,
            StoredFileName = s.StoredFileName, ContentType = s.ContentType, FileSizeBytes = s.Length, Sha256Checksum = s.Sha256Checksum,
            StoragePath = s.StoragePath, UploadedByUserId = support.UserId!.Value, UploadedAtUtc = support.UtcNow,
            Description = FleetServiceSupport.TrimOrNull(metadata.Description), FromLocation = FleetServiceSupport.TrimOrNull(metadata.FromLocation),
            ToLocation = FleetServiceSupport.TrimOrNull(metadata.ToLocation), TransportedAtUtc = metadata.TransportedAtUtc, Amount = metadata.Amount
        };
        dbContext.VehicleAccidentAttachments.Add(attachment);
        accident.UpdatedAtUtc = support.UtcNow;
        dbContext.Entry(accident).Property(x => x.UpdatedAtUtc).IsModified = true;
        dbContext.VehicleAccidentEvents.Add(new VehicleAccidentEvent { VehicleAccidentId = accidentId, EventType = VehicleAccidentEventType.EvidenceAdded,
            ActorUserId = support.UserId.Value, OccurredAtUtc = support.UtcNow, Reason = s.OriginalFileName,
            SnapshotJson = JsonSerializer.Serialize(new { AttachmentId = id, Type = type, Metadata = metadata }) });
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { fileStorage.DeleteBestEffort(s.StoragePath); return Result.Failure<VehicleAccidentAttachmentResponse>(FleetErrors.Conflict); }
        catch { fileStorage.DeleteBestEffort(s.StoragePath); throw; }
        return Result.Success(MapAttachment(attachment));
    }

    public async Task<Result<AccidentWorkflowResponse>> AddInstallmentAsync(Guid accidentId, AccidentInstallmentRequest request, CancellationToken cancellationToken = default)
    {
        var accident = await dbContext.VehicleAccidents.SingleOrDefaultAsync(x => x.Id == accidentId, cancellationToken);
        if (accident is null) return Result.Failure<AccidentWorkflowResponse>(FleetErrors.NotFound);
        var access = await GetVehicleAsync(accident.VehicleId, PermissionKeys.Fleet.AccidentsFinalize, cancellationToken);
        if (access.IsFailure) return Result.Failure<AccidentWorkflowResponse>(access.Error);
        var item = await dbContext.VehicleAccidentCases.SingleOrDefaultAsync(x => x.VehicleAccidentId == accidentId, cancellationToken);
        if (item is null) return Result.Failure<AccidentWorkflowResponse>(FleetErrors.NotFound);
        if (!FleetServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion)) return Result.Failure<AccidentWorkflowResponse>(FleetErrors.Conflict);
        if (accident.Status == VehicleAccidentStatus.Closed || !item.IncidentEndedAtUtc.HasValue || !item.RequestedClaimType.HasValue
            || item.RefundStatus is not (AccidentRefundStatus.NotSubmitted or AccidentRefundStatus.Rejected)) return Result.Failure<AccidentWorkflowResponse>(FleetErrors.InvalidState);
        var start = DateOnly.FromDateTime(accident.OccurredAtUtc.ToOffset(TimeSpan.FromHours(3)).DateTime);
        var end = DateOnly.FromDateTime(item.IncidentEndedAtUtc.Value.ToOffset(TimeSpan.FromHours(3)).DateTime);
        if (request.PeriodTo < request.PeriodFrom || request.PeriodFrom > end || request.PeriodTo < start
            || request.PaidOn < start || request.PaidOn > end || !ValidMoney(request.Amount) || !ValidMoney(request.RefundEligibleAmount)
            || request.RefundEligibleAmount > request.Amount || string.IsNullOrWhiteSpace(request.Notes) || request.Notes.Length > 1000
            || !await dbContext.VehicleAccidentAttachments.AnyAsync(x => x.Id == request.ReceiptAttachmentId && x.VehicleAccidentId == accidentId && x.EvidenceType == VehicleAccidentEvidenceType.InstallmentReceipt, cancellationToken))
            return Result.Failure<AccidentWorkflowResponse>(WorkflowError("Record a paid installment within the incident dates, an overlapping installment period, a receipt and a valid eligible amount."));
        if (await dbContext.VehicleAccidentInstallments.AnyAsync(x => x.VehicleAccidentId == accidentId
            && (x.ReceiptAttachmentId == request.ReceiptAttachmentId || x.PeriodFrom <= request.PeriodTo && x.PeriodTo >= request.PeriodFrom), cancellationToken))
            return Result.Failure<AccidentWorkflowResponse>(WorkflowError("This receipt or installment period has already been recorded."));
        dbContext.VehicleAccidentInstallments.Add(new VehicleAccidentInstallment { VehicleAccidentId = accidentId,
            PeriodFrom = request.PeriodFrom, PeriodTo = request.PeriodTo, PaidOn = request.PaidOn, Amount = request.Amount,
            RefundEligibleAmount = request.RefundEligibleAmount, ReceiptAttachmentId = request.ReceiptAttachmentId, Notes = request.Notes.Trim() });
        item.UpdatedAtUtc = support.UtcNow;
        dbContext.Entry(item).Property(x => x.UpdatedAtUtc).IsModified = true;
        accident.UpdatedAtUtc = support.UtcNow;
        dbContext.Entry(accident).Property(x => x.UpdatedAtUtc).IsModified = true;
        dbContext.VehicleAccidentEvents.Add(new VehicleAccidentEvent { VehicleAccidentId = accidentId, EventType = VehicleAccidentEventType.InstallmentAdded,
            OccurredAtUtc = support.UtcNow, ActorUserId = support.UserId!.Value, Reason = request.Notes.Trim(), SnapshotJson = JsonSerializer.Serialize(request) });
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Result.Failure<AccidentWorkflowResponse>(FleetErrors.Conflict); }
        return Result.Success(await BuildWorkflowAsync(accident, item, cancellationToken));
    }

    private async Task<AccidentWorkflowResponse> BuildWorkflowAsync(VehicleAccident accident, VehicleAccidentCase item, CancellationToken ct)
    {
        var attachments = await dbContext.VehicleAccidentAttachments.AsNoTracking().Where(x => x.VehicleAccidentId == accident.Id).OrderBy(x => x.UploadedAtUtc).ToArrayAsync(ct);
        var events = await dbContext.VehicleAccidentEvents.AsNoTracking().Where(x => x.VehicleAccidentId == accident.Id).OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id)
            .Select(x => new AccidentTimelineEvent(x.Id, x.EventType, x.OccurredAtUtc, x.ActorUserId, x.Reason, x.SnapshotJson)).ToArrayAsync(ct);
        var installments = await dbContext.VehicleAccidentInstallments.AsNoTracking().Where(x => x.VehicleAccidentId == accident.Id).OrderBy(x => x.PeriodFrom)
            .Select(x => new AccidentInstallmentResponse(x.Id, x.PeriodFrom, x.PeriodTo, x.PaidOn, x.Amount, x.RefundEligibleAmount, x.ReceiptAttachmentId, x.Notes)).ToArrayAsync(ct);
        var source = await LoadSourceDocumentsAsync(accident, item, ct);
        DateTimeOffset? deadline = item.Stage switch { AccidentCaseStage.AwaitingInsurance => item.InsuranceDueAtUtc, AccidentCaseStage.AwaitingSupplierTransfer => item.SupplierTransferDueAtUtc, _ => null };
        var now = support.UtcNow;
        return new AccidentWorkflowResponse(accident.Id, item.Stage, FleetServiceSupport.EncodeRowVersion(item.RowVersion), accident.OccurredAtUtc,
            item.IncidentEndedAtUtc, AccidentWorkflowRules.IncidentCalendarDays(accident.OccurredAtUtc, item.IncidentEndedAtUtc ?? now), deadline,
            deadline.HasValue ? (long)Math.Ceiling((deadline.Value - now).TotalSeconds) : null, deadline < now,
            new(item.RiderFaultPercentage, JsonSerializer.Deserialize<AccidentFaultParty[]>(item.OtherPartiesJson) ?? [], item.NajmAttachmentId,
                item.DamageAssessment, item.EstimatedRepairCost, item.DamagePromissoryNoteAttachmentId, item.OpeningFeeAmount, item.OpeningFeeAttachmentId, item.OpeningFeePaidAtUtc),
            new(item.RequestedClaimType, item.Outcome, item.SupplierId, item.ClaimNumber, item.ClaimSubmittedAtUtc, item.ClaimSubmissionAttachmentId),
            new(item.SettlementAmount, item.AssessmentReceiptAttachmentId, item.InsuranceSubmittedAtUtc, item.InsuranceDueAtUtc, item.InsuranceRespondedAtUtc,
                item.InsuranceRejectionReason, item.PaymentReceiptAttachmentId, item.SupplierSubmittedAtUtc, item.SupplierTransferDueAtUtc, item.TransferReceivedAtUtc, item.TransferReceivedAmount),
            new(item.RepairLocation, item.RepairContact, item.RepairStartedAtUtc, item.RepairCompletedAtUtc, item.ReinspectionLocation,
                item.ReinspectionAppointmentAtUtc, item.TotalLossConfirmedAtUtc, item.VehicleCollectedAtUtc),
            new(item.RefundStatus, item.RefundReference, item.RefundRequestedAmount, item.RefundReceivedAmount, item.RefundSubmittedAtUtc,
                item.RefundReceivedAtUtc, installments.Sum(x => x.Amount), installments.Sum(x => x.RefundEligibleAmount), installments),
            source.Select(x => new AccidentSourceDocument(x.Kind, x.VersionId, x.Name, x.ContentType,
                x.VersionId.HasValue ? $"/api/vehicle-accidents/{accident.Id}/workflow/documents/{x.Kind}/download" : null)).ToArray(),
            attachments.Select(MapAttachment).ToArray(), events);
    }

    private sealed record SourceFile(string Kind, Guid? VersionId, string? Name = null, string? ContentType = null, string? Path = null, long Length = 0);

    private async Task<IReadOnlyList<SourceFile>> LoadSourceDocumentsAsync(VehicleAccident accident, VehicleAccidentCase item, CancellationToken ct)
    {
        var result = new List<SourceFile>();
        foreach (var (kind, typeId, snapshotId) in new[] { ("iqama", DocumentType.ResidencyPermitId, item.IqamaVersionId), ("license", DocumentType.DriverLicenseId, item.LicenseVersionId) })
        {
            var query = from document in dbContext.EmployeeDocuments.AsNoTracking()
                        join version in dbContext.EmployeeDocumentVersions.AsNoTracking() on document.Id equals version.EmployeeDocumentId
                        where document.EmployeeId == accident.EmployeeId && document.DocumentTypeId == typeId
                            && (snapshotId.HasValue ? version.Id == snapshotId : version.Id == document.CurrentVersionId && document.Status == DocumentStatus.Active)
                        orderby version.UploadedAtUtc descending
                        select new SourceFile(kind, version.Id, version.OriginalFileName, version.ContentType, version.StoragePath, version.FileSizeBytes);
            result.Add(await query.FirstOrDefaultAsync(ct) ?? new SourceFile(kind, null));
        }
        var registration = await (from attachment in dbContext.VehicleAttachments.AsNoTracking()
                                  join version in dbContext.VehicleAttachmentVersions.AsNoTracking() on attachment.Id equals version.VehicleAttachmentId
                                  where attachment.VehicleId == accident.VehicleId && attachment.Kind == VehicleFileKind.Istimara
                                      && (item.RegistrationVersionId.HasValue ? version.Id == item.RegistrationVersionId : version.Id == attachment.CurrentVersionId)
                                  orderby version.UploadedAtUtc descending
                                  select new SourceFile("registration", version.Id, version.OriginalFileName, version.ContentType, version.StoragePath, version.FileSizeBytes)).FirstOrDefaultAsync(ct);
        result.Add(registration ?? new SourceFile("registration", null));
        return result;
    }

    public async Task<Result<PrivateFileDownload>> DownloadSourceDocumentAsync(Guid accidentId, string kind, CancellationToken cancellationToken = default)
    {
        var accident = await dbContext.VehicleAccidents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accidentId, cancellationToken);
        if (accident is null) return Result.Failure<PrivateFileDownload>(FleetErrors.NotFound);
        // Claim administrators can retrieve only the driver and vehicle documents tied to this accident.
        var access = await GetVehicleAsync(accident.VehicleId, PermissionKeys.Fleet.AccidentsFinalize, cancellationToken);
        if (access.IsFailure) return Result.Failure<PrivateFileDownload>(access.Error);
        var item = await dbContext.VehicleAccidentCases.AsNoTracking().SingleOrDefaultAsync(x => x.VehicleAccidentId == accidentId, cancellationToken);
        if (item is null) return Result.Failure<PrivateFileDownload>(FleetErrors.NotFound);
        var file = (await LoadSourceDocumentsAsync(accident, item, cancellationToken)).SingleOrDefault(x => x.Kind == kind);
        if (file?.VersionId is null) return Result.Failure<PrivateFileDownload>(FleetErrors.FileMissing);
        return await fileStorage.OpenReadAsync(file.Path!, file.ContentType!, file.Name!, file.Length, cancellationToken);
    }

    private async Task ResolveAccidentHoldAsync(Vehicle vehicle, VehicleAccident accident, string notes, CancellationToken ct)
    {
        var issue = await dbContext.VehicleIssues.SingleAsync(x => x.Id == accident.VehicleIssueId, ct);
        issue.Status = VehicleIssueStatus.Resolved; issue.BlocksOperation = false; issue.ResolvedAtUtc = support.UtcNow;
        issue.ResolvedByUserId = support.UserId; issue.ResolutionSummary = notes.Trim();
        dbContext.VehicleIssueEvents.Add(new VehicleIssueEvent { VehicleIssueId = issue.Id, EventType = VehicleIssueEventType.Resolved,
            ToStatus = issue.Status, OccurredAtUtc = support.UtcNow, ActorUserId = support.UserId!.Value, Reason = notes.Trim() });
        if (vehicle.CurrentOperationalStatus != VehicleOperationalStatus.AccidentHold
            || await dbContext.VehicleIssues.AnyAsync(x => x.VehicleId == vehicle.Id && x.Id != issue.Id && x.BlocksOperation
                && x.Status != VehicleIssueStatus.Resolved && x.Status != VehicleIssueStatus.Closed && x.Status != VehicleIssueStatus.Rejected, ct)) return;
        var period = await dbContext.VehicleOperationalStatusPeriods.SingleOrDefaultAsync(x => x.VehicleId == vehicle.Id && x.EffectiveToUtc == null, ct);
        if (period is not null) period.EffectiveToUtc = support.UtcNow;
        vehicle.CurrentOperationalStatus = await dbContext.RiderVehicleAssignments.AnyAsync(x => x.VehicleId == vehicle.Id && x.EndedAtUtc == null, ct)
            ? VehicleOperationalStatus.Assigned : VehicleOperationalStatus.Available;
        dbContext.VehicleOperationalStatusPeriods.Add(new VehicleOperationalStatusPeriod { VehicleId = vehicle.Id, Status = vehicle.CurrentOperationalStatus,
            EffectiveFromUtc = support.UtcNow, Reason = notes.Trim(), SourceType = VehicleStatusSourceType.Accident, SourceEntityId = accident.Id, ChangedByUserId = support.UserId!.Value });
    }

    private async Task DecommissionAfterTotalLossAsync(Vehicle vehicle, VehicleAccident accident, DateTimeOffset collectedAt, CancellationToken ct)
    {
        var activeAssignments = await dbContext.RiderVehicleAssignments.Where(x => x.VehicleId == vehicle.Id && x.EndedAtUtc == null).ToArrayAsync(ct);
        foreach (var assignment in activeAssignments)
        {
            assignment.EndedAtUtc = support.UtcNow; assignment.Status = RiderVehicleAssignmentStatus.Completed;
            assignment.EndedByUserId = support.UserId; assignment.CompletionReason = $"Vehicle collected after total loss: {accident.AccidentNumber}";
            assignment.EndVehicleCondition = VehicleCondition.Unsafe;
            dbContext.RiderVehicleAssignmentEvents.Add(new RiderVehicleAssignmentEvent { RiderVehicleAssignmentId = assignment.Id,
                OperationId = Guid.CreateVersion7(), EventType = RiderVehicleAssignmentEventType.Returned, OccurredAtUtc = support.UtcNow,
                ActorUserId = support.UserId!.Value, Reason = assignment.CompletionReason });
        }
        var platformAssignments = await dbContext.VehiclePlatformAccountAssignments.Where(x => x.VehicleId == vehicle.Id && x.EndedAtUtc == null).ToArrayAsync(ct);
        foreach (var assignment in platformAssignments)
        {
            assignment.EndedAtUtc = support.UtcNow; assignment.Status = VehiclePlatformAccountAssignmentStatus.Ended;
            assignment.EndedByUserId = support.UserId; assignment.EndReason = $"Total loss: {accident.AccidentNumber}";
        }
        var period = await dbContext.VehicleOperationalStatusPeriods.SingleOrDefaultAsync(x => x.VehicleId == vehicle.Id && x.EffectiveToUtc == null, ct);
        if (period is not null) period.EffectiveToUtc = support.UtcNow;
        vehicle.CurrentOperationalStatus = VehicleOperationalStatus.Decommissioned;
        vehicle.CurrentAssignmentId = null;
        vehicle.DecommissionedAtUtc = collectedAt; vehicle.DecommissionReason = $"Total loss: {accident.AccidentNumber}";
        dbContext.VehicleOperationalStatusPeriods.Add(new VehicleOperationalStatusPeriod { VehicleId = vehicle.Id, Status = vehicle.CurrentOperationalStatus,
            EffectiveFromUtc = support.UtcNow, Reason = $"Total loss collected at {collectedAt:O}; accident {accident.AccidentNumber}",
            SourceType = VehicleStatusSourceType.Accident, SourceEntityId = accident.Id, ChangedByUserId = support.UserId!.Value });
    }
}
