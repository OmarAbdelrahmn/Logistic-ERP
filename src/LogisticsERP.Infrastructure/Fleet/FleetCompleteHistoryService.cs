using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed partial class FleetService
{
    private static readonly JsonSerializerOptions CompleteHistoryJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<CompleteHistoryResponse>> GetCompleteVehicleHistoryAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var vehicle = await dbContext.Vehicles.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.Id == vehicleId, cancellationToken);
        if (vehicle is null) return Result.Failure<CompleteHistoryResponse>(FleetErrors.NotFound);
        if (!await support.HasVehiclePermissionAsync(vehicle, PermissionKeys.Fleet.VehiclesRead, cancellationToken) ||
            !await HasCompleteHistoryPermissionsAsync(cancellationToken))
            return Result.Failure<CompleteHistoryResponse>(FleetErrors.Forbidden);
        return Result.Success(await BuildCompleteHistoryAsync(vehicleId, null, vehicle.AssetNumber, cancellationToken));
    }

    public async Task<Result<CompleteHistoryResponse>> GetCompleteRiderHistoryAsync(Guid riderProfileId, CancellationToken cancellationToken = default)
    {
        var rider = await dbContext.RiderProfiles.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.Id == riderProfileId, cancellationToken);
        if (rider is null) return Result.Failure<CompleteHistoryResponse>(FleetErrors.NotFound);
        if (!await HasCompleteHistoryPermissionsAsync(cancellationToken))
            return Result.Failure<CompleteHistoryResponse>(FleetErrors.Forbidden);
        var name = await dbContext.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.Id == rider.EmployeeId).Select(x => x.FullNameAr).SingleOrDefaultAsync(cancellationToken);
        return Result.Success(await BuildCompleteHistoryAsync(null, riderProfileId, name, cancellationToken));
    }

    private async Task<bool> HasCompleteHistoryPermissionsAsync(CancellationToken cancellationToken)
    {
        string[] permissions = [
            PermissionKeys.Fleet.AssignmentsRead, PermissionKeys.Fleet.IssuesRead,
            PermissionKeys.Fleet.AccidentsRead, PermissionKeys.Maintenance.WorkOrdersRead,
            PermissionKeys.Inventory.StockRead
        ];
        foreach (var permission in permissions)
            if (!await support.HasPermissionAsync(permission, null, cancellationToken)) return false;
        return true;
    }

    private async Task<CompleteHistoryResponse> BuildCompleteHistoryAsync(Guid? vehicleId, Guid? riderId, string? subjectName, CancellationToken ct)
    {
        var events = new List<CompleteHistoryEventResponse>();
        void Add(DateTimeOffset at, string category, string action, Guid id, Guid? vehicle, Guid? rider,
            Guid? assignment, string summary, object details, IReadOnlyList<CompleteHistoryFileResponse>? files = null) =>
            events.Add(new CompleteHistoryEventResponse(at, category, action, id, vehicle, rider, assignment,
                summary, JsonSerializer.SerializeToElement(details, CompleteHistoryJsonOptions), files ?? []));

        var assignments = await dbContext.RiderVehicleAssignments.IgnoreQueryFilters().AsNoTracking()
            .Where(x => vehicleId.HasValue ? x.VehicleId == vehicleId.Value : x.RiderProfileId == riderId!.Value)
            .ToArrayAsync(ct);
        var assignmentIds = assignments.Select(x => x.Id).ToArray();
        var riderIds = assignments.Select(x => x.RiderProfileId).Distinct().ToArray();
        var riderNames = await (from profile in dbContext.RiderProfiles.IgnoreQueryFilters().AsNoTracking()
                                join employee in dbContext.Employees.IgnoreQueryFilters().AsNoTracking() on profile.EmployeeId equals employee.Id
                                where riderIds.Contains(profile.Id)
                                select new { profile.Id, employee.FullNameAr }).ToDictionaryAsync(x => x.Id, x => x.FullNameAr, ct);
        var vehicleIds = assignments.Select(x => x.VehicleId).Distinct().ToArray();
        var vehicleNames = await dbContext.Vehicles.IgnoreQueryFilters().AsNoTracking()
            .Where(x => vehicleIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.AssetNumber, ct);
        var realRiders = await dbContext.RealRiders.IgnoreQueryFilters().AsNoTracking()
            .Where(x => assignmentIds.Contains(x.RiderVehicleAssignmentId)).ToDictionaryAsync(x => x.RiderVehicleAssignmentId, ct);
        foreach (var assignment in assignments)
        {
            realRiders.TryGetValue(assignment.Id, out var realRider);
            riderNames.TryGetValue(assignment.RiderProfileId, out var riderName);
            vehicleNames.TryGetValue(assignment.VehicleId, out var assetNumber);
            Add(assignment.StartedAtUtc, "assignment", "handed_over", assignment.Id, assignment.VehicleId,
                assignment.RiderProfileId, assignment.Id, $"Vehicle {assetNumber} handed to {riderName}",
                new { assignment.OperationId, assignment.PreviousAssignmentId, assignment.StartOdometer,
                    assignment.StartVehicleCondition, assignment.StartFuelLevelPercentage, assignment.StartLocationSnapshot,
                    assignment.PermissionReference, assignment.PermissionStartsOn, assignment.PermissionEndsOn,
                    assignment.AssignmentReason, assignment.AssignedByUserId, assignment.IsRealRider,
                    RealRider = realRider is null ? null : new { realRider.Name, realRider.IqamaNo, realRider.RelationshipToAssignedRider },
                    assignment.Notes });
            if (assignment.EndedAtUtc.HasValue)
                Add(assignment.EndedAtUtc.Value, "assignment", "returned_or_switched", assignment.Id,
                    assignment.VehicleId, assignment.RiderProfileId, assignment.Id, $"Vehicle {assetNumber} assignment ended",
                    new { assignment.EndOdometer, assignment.EndVehicleCondition, assignment.EndFuelLevelPercentage,
                        assignment.EndLocationSnapshot, assignment.CompletionReason, assignment.EndedByUserId,
                        assignment.Status });
        }
        var assignmentEvents = await dbContext.RiderVehicleAssignmentEvents.IgnoreQueryFilters().AsNoTracking()
            .Where(x => assignmentIds.Contains(x.RiderVehicleAssignmentId)).ToArrayAsync(ct);
        foreach (var entry in assignmentEvents)
        {
            var assignment = assignments.First(x => x.Id == entry.RiderVehicleAssignmentId);
            Add(entry.OccurredAtUtc, "assignment_event", entry.EventType.ToString(), entry.Id,
                assignment.VehicleId, assignment.RiderProfileId, assignment.Id, entry.Reason,
                new { entry.OperationId, entry.ActorUserId, entry.ChangeSnapshotJson, entry.CorrelationId });
        }
        var promissoryLinks = await dbContext.RiderVehicleAssignmentPromissoryFiles.IgnoreQueryFilters().AsNoTracking()
            .Where(x => assignmentIds.Contains(x.RiderVehicleAssignmentId)).ToArrayAsync(ct);
        var promissoryIds = promissoryLinks.Select(x => x.RiderPromissoryFileVersionId).ToArray();
        var promissoryVersions = await dbContext.RiderPromissoryFileVersions.IgnoreQueryFilters().AsNoTracking()
            .Where(x => promissoryIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        foreach (var link in promissoryLinks)
        {
            if (!promissoryVersions.TryGetValue(link.RiderPromissoryFileVersionId, out var version)) continue;
            var assignment = assignments.First(x => x.Id == link.RiderVehicleAssignmentId);
            Add(version.UploadedAtUtc, "assignment_file", "promissory_note", version.Id, assignment.VehicleId,
                assignment.RiderProfileId, assignment.Id, version.OriginalFileName,
                new { version.RiderPromissoryFileId, version.VersionNumber, version.UploadedByUserId },
                [new CompleteHistoryFileResponse(version.RiderPromissoryFileId, version.OriginalFileName, version.ContentType,
                    version.FileSizeBytes, $"/api/riders/{assignment.RiderProfileId}/promissory-files/{version.RiderPromissoryFileId}/download?versionId={version.Id}")]);
        }

        var issues = await dbContext.VehicleIssues.IgnoreQueryFilters().AsNoTracking()
            .Where(x => vehicleId.HasValue ? x.VehicleId == vehicleId.Value : x.RelatedAssignmentId.HasValue && assignmentIds.Contains(x.RelatedAssignmentId.Value))
            .ToArrayAsync(ct);
        var issueIds = issues.Select(x => x.Id).ToArray();
        var issueFiles = await dbContext.VehicleIssueEvidenceFiles.IgnoreQueryFilters().AsNoTracking()
            .Where(x => issueIds.Contains(x.VehicleIssueId)).ToArrayAsync(ct);
        foreach (var issue in issues)
        {
            var rider = assignments.FirstOrDefault(x => x.Id == issue.RelatedAssignmentId)?.RiderProfileId;
            var files = issueFiles.Where(x => x.VehicleIssueId == issue.Id)
                .Select(x => new CompleteHistoryFileResponse(x.Id, x.OriginalFileName, x.ContentType, x.FileSizeBytes,
                    $"/api/vehicle-issues/{issue.Id}/evidence/{x.Id}/download")).ToArray();
            Add(issue.ReportedAtUtc, "issue", issue.Category.ToString(), issue.Id, issue.VehicleId, rider,
                issue.RelatedAssignmentId, issue.Description,
                new { issue.IssueNumber, issue.Category, issue.Severity, issue.Status, issue.OdometerAtReport,
                    issue.LocationDescription, issue.EstimatedRepairCost, issue.IsRiderResponsible,
                    issue.BlocksOperation, issue.ResolutionSummary, issue.ResolvedAtUtc, issue.ClosedAtUtc }, files);
        }
        var issueEvents = await dbContext.VehicleIssueEvents.IgnoreQueryFilters().AsNoTracking()
            .Where(x => issueIds.Contains(x.VehicleIssueId)).ToArrayAsync(ct);
        foreach (var entry in issueEvents)
        {
            var issue = issues.First(x => x.Id == entry.VehicleIssueId);
            Add(entry.OccurredAtUtc, "issue_event", entry.EventType.ToString(), entry.Id, issue.VehicleId,
                assignments.FirstOrDefault(x => x.Id == issue.RelatedAssignmentId)?.RiderProfileId,
                issue.RelatedAssignmentId, entry.Reason,
                new { entry.FromStatus, entry.ToStatus, entry.ActorUserId, entry.SnapshotJson });
        }

        var accidents = await dbContext.VehicleAccidents.IgnoreQueryFilters().AsNoTracking()
            .Where(x => vehicleId.HasValue ? x.VehicleId == vehicleId.Value : x.RiderProfileId == riderId!.Value)
            .ToArrayAsync(ct);
        var accidentIds = accidents.Select(x => x.Id).ToArray();
        var accidentFiles = await dbContext.VehicleAccidentAttachments.IgnoreQueryFilters().AsNoTracking()
            .Where(x => accidentIds.Contains(x.VehicleAccidentId)).ToArrayAsync(ct);
        var accidentCases = await dbContext.VehicleAccidentCases.IgnoreQueryFilters().AsNoTracking()
            .Where(x => accidentIds.Contains(x.VehicleAccidentId)).ToArrayAsync(ct);
        var accidentReports = await dbContext.VehicleAccidentReportVersions.IgnoreQueryFilters().AsNoTracking()
            .Where(x => accidentIds.Contains(x.VehicleAccidentId)).ToArrayAsync(ct);
        foreach (var accident in accidents)
        {
            var files = accidentFiles.Where(x => x.VehicleAccidentId == accident.Id)
                .Select(x => new CompleteHistoryFileResponse(x.Id, x.OriginalFileName, x.ContentType, x.FileSizeBytes,
                    $"/api/vehicle-accidents/{accident.Id}/evidence/{x.Id}/download")).ToArray();
            var workflow = accidentCases.FirstOrDefault(x => x.VehicleAccidentId == accident.Id);
            Add(accident.OccurredAtUtc, "accident", "reported", accident.Id, accident.VehicleId,
                accident.RiderProfileId, accident.RiderVehicleAssignmentId, accident.AccidentNumber,
                new { accident.ReportedAtUtc, accident.VehicleIssueId, accident.LocationDescription,
                    accident.Severity, accident.Status, accident.IsDrivable, accident.HasInjuries,
                    accident.InjuryDetails, accident.ThirdPartyDetails, accident.DamageDescription,
                    accident.FaultAssessment, accident.Narrative, accident.PoliceReportNumber,
                    accident.InsuranceClaimNumber, accident.ClosedAtUtc,
                    WorkflowStage = workflow?.Stage,
                    Reports = accidentReports.Where(x => x.VehicleAccidentId == accident.Id)
                        .Select(x => new { x.Id, x.VersionNumber, x.ReportNumber, x.GeneratedAtUtc, x.CorrectionReason }) }, files);
        }
        var accidentEvents = await dbContext.VehicleAccidentEvents.IgnoreQueryFilters().AsNoTracking()
            .Where(x => accidentIds.Contains(x.VehicleAccidentId)).ToArrayAsync(ct);
        foreach (var entry in accidentEvents)
        {
            var accident = accidents.First(x => x.Id == entry.VehicleAccidentId);
            Add(entry.OccurredAtUtc, "accident_event", entry.EventType.ToString(), entry.Id,
                accident.VehicleId, accident.RiderProfileId, accident.RiderVehicleAssignmentId, entry.Reason,
                new { entry.ActorUserId, entry.SnapshotJson });
        }

        var orders = vehicleId.HasValue
            ? await dbContext.MaintenanceWorkOrders.IgnoreQueryFilters().AsNoTracking().Where(x => x.VehicleId == vehicleId.Value).ToArrayAsync(ct)
            : await dbContext.MaintenanceWorkOrders.IgnoreQueryFilters().AsNoTracking().Where(x => x.AttributedRiderProfileId == riderId!.Value).ToArrayAsync(ct);
        foreach (var order in orders)
            Add(order.OpenedAtUtc, "maintenance", "work_order", order.Id, order.VehicleId,
                order.AttributedRiderProfileId, order.RiderVehicleAssignmentId, order.WorkOrderNumber,
                new { order.MaintenanceType, order.Status, order.MaintenanceLocationId, order.VehicleIssueId,
                    order.Diagnosis, order.WorkPerformed, order.OdometerAtOpen, order.OdometerAtCompletion,
                    order.ActualMaterialCost, order.ActualOtherCost, order.ActualTotalCost,
                    order.StartedAtUtc, order.CompletedAtUtc, order.ClosedAtUtc, order.Notes });
        var usages = await dbContext.MaintenanceMaterialUsages.IgnoreQueryFilters().AsNoTracking()
            .Where(x => vehicleId.HasValue ? x.VehicleId == vehicleId.Value : x.RiderProfileId == riderId!.Value)
            .ToArrayAsync(ct);
        var itemIds = usages.Select(x => x.InventoryItemId).Distinct().ToArray();
        var itemNames = await dbContext.InventoryItems.IgnoreQueryFilters().AsNoTracking()
            .Where(x => itemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.NameAr, ct);
        foreach (var usage in usages)
        {
            itemNames.TryGetValue(usage.InventoryItemId, out var itemName);
            Add(usage.UsedAtUtc, "material_usage", usage.UsageType.ToString(), usage.Id,
                usage.VehicleId, usage.RiderProfileId, usage.RiderVehicleAssignmentId,
                itemName ?? usage.InventoryItemId.ToString(),
                new { usage.MaintenanceWorkOrderId, usage.InventoryItemId, usage.InventoryLocationId,
                    usage.Direction, usage.Quantity, usage.UnitOfMeasure, usage.TotalCost,
                    usage.StockMovementId, usage.AttributionStatus, usage.ReversalOfUsageId, usage.Notes });
        }
        var oilOperations = await dbContext.OilChangeOperations.IgnoreQueryFilters().AsNoTracking()
            .Where(x => vehicleId.HasValue ? x.VehicleId == vehicleId.Value : x.VehicleId.HasValue && vehicleIds.Contains(x.VehicleId.Value))
            .ToArrayAsync(ct);
        foreach (var oil in oilOperations)
        {
            var rider = usages.FirstOrDefault(x => x.Id == oil.OilMaterialUsageId)?.RiderProfileId;
            if (riderId.HasValue && rider != riderId) continue;
            Add(oil.PerformedAtUtc, "oil_change", oil.MaintenanceWorkOrderId.HasValue ? "work_order" : "direct",
                oil.Id, oil.VehicleId, rider, null, "Oil and filter service",
                new { oil.MaintenanceWorkOrderId, oil.OdometerAtChange, oil.VehicleTypeSnapshot,
                    oil.OilInventoryItemId, oil.OilQuantityLiters, oil.OilMaterialUsageId, oil.OilCost,
                    oil.OilFilterChanged, oil.OilFilterInventoryItemId, oil.OilFilterMaterialUsageId,
                    oil.OilFilterCost, oil.OtherCost, oil.TotalCost, oil.PerformedByUserId, oil.Notes });
        }

        var expenses = await dbContext.VehicleExpenses.IgnoreQueryFilters().AsNoTracking()
            .Where(x => vehicleId.HasValue ? x.VehicleId == vehicleId.Value : x.RiderProfileId == riderId!.Value)
            .ToArrayAsync(ct);
        foreach (var expense in expenses)
            Add(expense.CreatedAtUtc, "vehicle_expense", expense.ExpenseType, expense.Id,
                expense.VehicleId, expense.RiderProfileId, expense.RiderVehicleAssignmentId,
                expense.Description, new { expense.SourceEntityType, expense.SourceEntityId,
                    expense.OccurredOn, expense.AmountBeforeTax, expense.TaxAmount,
                    expense.TotalAmount, expense.CurrencyCode, expense.ReversalOfExpenseId });

        if (vehicleId.HasValue)
        {
            var registrations = await dbContext.VehicleRegistrations.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.VehicleId == vehicleId.Value).ToArrayAsync(ct);
            foreach (var registration in registrations)
                Add(registration.CreatedAtUtc, "compliance", "registration", registration.Id,
                    vehicleId, null, null, registration.RegistrationNumber,
                    new { registration.IssuingAuthority, registration.IssueDate, registration.ExpiryDate,
                        registration.Status, registration.IsCurrent, registration.PreviousRecordId,
                        registration.ProofAttachmentId, registration.Notes });
            var policies = await dbContext.VehicleInsurancePolicies.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.VehicleId == vehicleId.Value).ToArrayAsync(ct);
            foreach (var policy in policies)
                Add(policy.CreatedAtUtc, "compliance", "insurance", policy.Id,
                    vehicleId, null, null, policy.PolicyNumber,
                    new { policy.ProviderName, policy.CoverageType, policy.EffectiveFrom, policy.ExpiryDate,
                        policy.ClaimReference, policy.Status, policy.IsCurrent, policy.PreviousRecordId,
                        policy.ProofAttachmentId, policy.Notes });
            var inspections = await dbContext.VehiclePeriodicInspections.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.VehicleId == vehicleId.Value).ToArrayAsync(ct);
            foreach (var inspection in inspections)
                Add(inspection.CreatedAtUtc, "compliance", "inspection", inspection.Id,
                    vehicleId, null, null, inspection.InspectionNumber,
                    new { inspection.StationName, inspection.InspectionDate, inspection.ExpiryDate,
                        inspection.Result, inspection.Odometer, inspection.FailureNotes, inspection.Status,
                        inspection.IsCurrent, inspection.PreviousRecordId, inspection.ProofAttachmentId,
                        inspection.Notes });
            var cards = await dbContext.VehicleOperationCards.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.VehicleId == vehicleId.Value).ToArrayAsync(ct);
            foreach (var card in cards)
                Add(card.CreatedAtUtc, "compliance", "operation_card", card.Id,
                    vehicleId, null, null, card.CardNumber,
                    new { card.IssuingAuthority, card.IssueDate, card.ExpiryDate, card.Status,
                        card.IsCurrent, card.PreviousRecordId, card.ProofAttachmentId, card.Notes });
            var statusPeriods = await dbContext.VehicleOperationalStatusPeriods.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.VehicleId == vehicleId.Value).ToArrayAsync(ct);
            foreach (var status in statusPeriods)
                Add(status.EffectiveFromUtc, "vehicle_status", status.Status.ToString(), status.Id,
                    vehicleId, null, null, status.Reason,
                    new { status.EffectiveToUtc, status.SourceType, status.SourceEntityId, status.ChangedByUserId });
            var readings = await dbContext.VehicleOdometerReadings.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.VehicleId == vehicleId.Value).ToArrayAsync(ct);
            foreach (var reading in readings)
                Add(reading.RecordedAtUtc, "odometer", reading.SourceType.ToString(), reading.Id,
                    vehicleId, null, null, $"{reading.Reading} km",
                    new { reading.Reading, reading.SourceEntityId, reading.IsCorrection, reading.CorrectionReason, reading.Notes });
            var corrections = await dbContext.VehicleIdentityCorrections.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.VehicleId == vehicleId.Value).ToArrayAsync(ct);
            foreach (var correction in corrections)
                Add(correction.CreatedAtUtc, "vehicle_identity", "corrected", correction.Id, vehicleId,
                    null, null, "Vehicle identity correction", new { correction.Reason });
            var transitions = await dbContext.VehicleRegistrationTransitions.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.VehicleId == vehicleId.Value).ToArrayAsync(ct);
            foreach (var transition in transitions)
                Add(transition.EffectiveAtUtc, "registration", "type_changed", transition.Id, vehicleId,
                    null, null, transition.Reason, new { transition.FromType, transition.ToType,
                        transition.OldPlateNumberAr, transition.NewPlateNumberAr, transition.OldPlateNumberEn,
                        transition.NewPlateNumberEn, transition.ActorUserId });
            var vehicleFiles = await (from attachment in dbContext.VehicleAttachments.IgnoreQueryFilters().AsNoTracking()
                                      join version in dbContext.VehicleAttachmentVersions.IgnoreQueryFilters().AsNoTracking()
                                          on attachment.Id equals version.VehicleAttachmentId
                                      where attachment.VehicleId == vehicleId.Value
                                      select new { attachment.Kind, version }).ToArrayAsync(ct);
            foreach (var file in vehicleFiles)
                Add(file.version.UploadedAtUtc, "vehicle_file", file.Kind.ToString(), file.version.Id,
                    vehicleId, null, null, file.version.OriginalFileName,
                    new { file.version.VersionNumber, file.version.UploadedByUserId },
                    [new CompleteHistoryFileResponse(file.version.VehicleAttachmentId, file.version.OriginalFileName,
                        file.version.ContentType, file.version.FileSizeBytes,
                        $"/api/vehicles/{vehicleId}/files/{file.version.VehicleAttachmentId}/download?versionId={file.version.Id}")]);
        }
        else
        {
            var issuesForRider = await dbContext.RiderInventoryIssues.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.RiderProfileId == riderId!.Value).ToArrayAsync(ct);
            var riderIssueIds = issuesForRider.Select(x => x.Id).ToArray();
            var lines = await dbContext.RiderInventoryIssueLines.IgnoreQueryFilters().AsNoTracking()
                .Where(x => riderIssueIds.Contains(x.RiderInventoryIssueId)).ToArrayAsync(ct);
            var equipmentIds = lines.Select(x => x.InventoryItemId).Distinct().ToArray();
            var equipmentNames = await dbContext.InventoryItems.IgnoreQueryFilters().AsNoTracking()
                .Where(x => equipmentIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.NameAr, ct);
            foreach (var issue in issuesForRider)
            {
                var ownLines = lines.Where(x => x.RiderInventoryIssueId == issue.Id)
                    .Select(x => new { x.Id, x.InventoryItemId, ItemNameAr = equipmentNames.GetValueOrDefault(x.InventoryItemId),
                        x.Quantity, x.TotalCost, x.ExpectedReturn, x.ReturnedQuantity }).ToArray();
                Add(issue.IssuedAtUtc, "rider_equipment", "issued", issue.Id,
                    assignments.FirstOrDefault(x => x.Id == issue.RelatedAssignmentId)?.VehicleId,
                    riderId, issue.RelatedAssignmentId, issue.IssueNumber,
                    new { issue.IssuedFromLocationId, issue.IssuedByUserId, issue.Status, issue.Notes, Lines = ownLines });
                if (ownLines.Any(x => x.ReturnedQuantity > 0))
                    Add(issue.UpdatedAtUtc ?? issue.IssuedAtUtc, "rider_equipment", "return_balance_recorded", issue.Id,
                        assignments.FirstOrDefault(x => x.Id == issue.RelatedAssignmentId)?.VehicleId,
                        riderId, issue.RelatedAssignmentId, "Equipment return quantities recorded; exact return time unavailable",
                        new { Lines = ownLines.Where(x => x.ReturnedQuantity > 0), ReturnTimestampKnown = false });
            }
            var requests = await dbContext.InventorySupplyRequests.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.RiderProfileId == riderId!.Value).ToArrayAsync(ct);
            foreach (var request in requests)
                Add(request.RequestedAtUtc, "inventory_request", request.Status.ToString(), request.Id,
                    request.VehicleId, riderId, null, request.RequestNumber,
                    new { request.InventoryLocationId, request.RequestedByUserId, request.DecidedAtUtc,
                        request.IssuedAtUtc, request.TotalIssuedCost, request.DecisionNotes, request.Notes });
        }

        var sorted = events.OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Category)
            .ThenBy(x => x.EntityId).ToArray();
        return new CompleteHistoryResponse(vehicleId ?? riderId!.Value, vehicleId.HasValue ? "vehicle" : "rider",
            subjectName, support.UtcNow, sorted.Length, sorted);
    }
}
