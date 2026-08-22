using System.Text.Json;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed class VehicleAccidentService(
    ApplicationDbContext dbContext,
    FleetServiceSupport support,
    IPrivateFileStorage fileStorage,
    IAccidentPdfGenerator pdfGenerator) : IVehicleAccidentService
{
    private const long MaximumEvidenceSize = 10 * 1024 * 1024;
    private const long MaximumGeneratedPdfSize = 25 * 1024 * 1024;

    public async Task<Result<PagedResponse<VehicleAccidentSummaryResponse>>> GetAsync(Guid? vehicleId, Guid? riderProfileId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 200);
        if (vehicleId.HasValue)
        {
            var access = await GetVehicleAsync(vehicleId.Value, PermissionKeys.Fleet.AccidentsRead, cancellationToken);
            if (access.IsFailure) return Result.Failure<PagedResponse<VehicleAccidentSummaryResponse>>(access.Error);
        }
        var locations = await support.AccessibleLocationIdsAsync(PermissionKeys.Fleet.AccidentsRead, cancellationToken);
        var global = await support.HasPermissionAsync(PermissionKeys.Fleet.AccidentsRead, null, cancellationToken);
        var vehicleIds = dbContext.Vehicles.AsNoTracking().Where(v => global || v.CurrentLocationId != null && locations.Contains(v.CurrentLocationId.Value)).Select(v => v.Id);
        var query = dbContext.VehicleAccidents.AsNoTracking().Where(x => vehicleIds.Contains(x.VehicleId));
        if (vehicleId.HasValue) query = query.Where(x => x.VehicleId == vehicleId);
        if (riderProfileId.HasValue) query = query.Where(x => x.RiderProfileId == riderProfileId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        return Result.Success(new PagedResponse<VehicleAccidentSummaryResponse>(items.Select(MapSummary).ToArray(), page, pageSize, count));
    }

    public async Task<Result<VehicleAccidentDetailResponse>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var accident = await dbContext.VehicleAccidents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (accident is null) return Result.Failure<VehicleAccidentDetailResponse>(FleetErrors.NotFound);
        var access = await GetVehicleAsync(accident.VehicleId, PermissionKeys.Fleet.AccidentsRead, cancellationToken);
        return access.IsFailure ? Result.Failure<VehicleAccidentDetailResponse>(access.Error) : Result.Success(await BuildDetailAsync(accident, cancellationToken));
    }

    public async Task<Result<VehicleAccidentDetailResponse>> CreateAsync(CreateVehicleAccidentRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return Result.Failure<VehicleAccidentDetailResponse>(FleetErrors.IdempotencyRequired);
        var hash = FleetServiceSupport.HashRequest(request);
        var receipt = await dbContext.FleetCommandReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.CommandName == "create-accident" && x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (receipt is not null)
        {
            if (receipt.RequestHash != hash) return Result.Failure<VehicleAccidentDetailResponse>(FleetErrors.IdempotencyConflict);
            var existing = await dbContext.VehicleAccidents.AsNoTracking().SingleAsync(x => x.Id == receipt.ResultEntityId, cancellationToken);
            return Result.Success(await BuildDetailAsync(existing, cancellationToken));
        }
        var vehicleResult = await GetVehicleAsync(request.VehicleId, PermissionKeys.Fleet.AccidentsReport, cancellationToken, tracking: true);
        if (vehicleResult.IsFailure) return Result.Failure<VehicleAccidentDetailResponse>(vehicleResult.Error);
        if (string.IsNullOrWhiteSpace(request.LocationDescription) || string.IsNullOrWhiteSpace(request.DamageDescription) || string.IsNullOrWhiteSpace(request.Narrative)
            || request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180 || request.HasInjuries && string.IsNullOrWhiteSpace(request.InjuryDetails)) return Result.Failure<VehicleAccidentDetailResponse>(FleetErrors.InvalidRequest);
        var assignment = await dbContext.RiderVehicleAssignments.SingleOrDefaultAsync(x => x.VehicleId == request.VehicleId && x.RiderProfileId == request.RiderProfileId
            && x.StartedAtUtc <= request.OccurredAtUtc && (x.EndedAtUtc == null || x.EndedAtUtc >= request.OccurredAtUtc), cancellationToken);
        if (assignment is null) return Result.Failure<VehicleAccidentDetailResponse>(FleetErrors.AccidentAssignmentMismatch);
        var actor = support.UserId;
        if (!actor.HasValue) return Result.Failure<VehicleAccidentDetailResponse>(FleetErrors.CurrentUserUnavailable);
        var vehicle = vehicleResult.Value!;
        var insurance = await dbContext.VehicleInsurancePolicies.AsNoTracking().Where(x => x.VehicleId == vehicle.Id && x.EffectiveFrom <= DateOnly.FromDateTime(request.OccurredAtUtc.UtcDateTime) && x.ExpiryDate >= DateOnly.FromDateTime(request.OccurredAtUtc.UtcDateTime)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(cancellationToken);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var issueId = Guid.CreateVersion7();
        var accidentId = Guid.CreateVersion7();
        var issue = new VehicleIssue
        {
            Id = issueId, IssueNumber = FleetServiceSupport.NewNumber("ISS", support.UtcNow, issueId), VehicleId = vehicle.Id,
            Category = VehicleIssueCategory.Accident, Severity = ToIssueSeverity(request.Severity), Description = request.DamageDescription.Trim(),
            ReportedAtUtc = support.UtcNow, LocationId = request.LocationId ?? vehicle.CurrentLocationId, OdometerAtReport = vehicle.CurrentOdometer,
            RelatedAssignmentId = assignment.Id, BlocksOperation = !request.IsDrivable, ReportedByUserId = actor.Value
        };
        var accident = new VehicleAccident
        {
            Id = accidentId, AccidentNumber = FleetServiceSupport.NewNumber("ACC", support.UtcNow, accidentId), VehicleId = vehicle.Id,
            RiderProfileId = assignment.RiderProfileId, EmployeeId = assignment.EmployeeId, RiderVehicleAssignmentId = assignment.Id, VehicleIssueId = issue.Id,
            VehicleInsurancePolicyId = insurance?.Id, OccurredAtUtc = request.OccurredAtUtc, ReportedAtUtc = support.UtcNow, LocationId = request.LocationId ?? vehicle.CurrentLocationId,
            LocationDescription = request.LocationDescription.Trim(), Latitude = request.Latitude, Longitude = request.Longitude,
            PoliceReportNumber = FleetServiceSupport.TrimOrNull(request.PoliceReportNumber), InsuranceClaimNumber = FleetServiceSupport.TrimOrNull(request.InsuranceClaimNumber),
            Severity = request.Severity, IsDrivable = request.IsDrivable, HasInjuries = request.HasInjuries, InjuryDetails = FleetServiceSupport.TrimOrNull(request.InjuryDetails),
            ThirdPartyDetails = FleetServiceSupport.TrimOrNull(request.ThirdPartyDetails), DamageDescription = request.DamageDescription.Trim(),
            FaultAssessment = FleetServiceSupport.TrimOrNull(request.FaultAssessment), Narrative = request.Narrative.Trim(), ReportedByUserId = actor.Value
        };
        dbContext.VehicleIssues.Add(issue);
        dbContext.VehicleIssueEvents.Add(new VehicleIssueEvent { VehicleIssueId = issue.Id, EventType = VehicleIssueEventType.Reported, ToStatus = VehicleIssueStatus.Open, OccurredAtUtc = support.UtcNow, ActorUserId = actor.Value, Reason = request.DamageDescription.Trim() });
        dbContext.VehicleAccidents.Add(accident);
        dbContext.VehicleAccidentEvents.Add(new VehicleAccidentEvent { VehicleAccidentId = accident.Id, EventType = VehicleAccidentEventType.Reported, OccurredAtUtc = support.UtcNow, ActorUserId = actor.Value, Reason = request.Narrative.Trim() });
        if (!request.IsDrivable && assignment.EndedAtUtc is null && vehicle.CurrentAssignmentId == assignment.Id)
        {
            assignment.EndedAtUtc = support.UtcNow; assignment.EndLocationId = vehicle.CurrentLocationId; assignment.EndOdometer = vehicle.CurrentOdometer;
            assignment.EndVehicleCondition = VehicleCondition.Damaged; assignment.Status = RiderVehicleAssignmentStatus.Completed; assignment.CompletionReason = $"Accident {accident.AccidentNumber}"; assignment.EndedByUserId = actor.Value;
            vehicle.CurrentAssignmentId = null;
            dbContext.RiderVehicleAssignmentEvents.Add(new RiderVehicleAssignmentEvent { RiderVehicleAssignmentId = assignment.Id, OperationId = assignment.OperationId, EventType = RiderVehicleAssignmentEventType.Returned, OccurredAtUtc = support.UtcNow, ActorUserId = actor.Value, Reason = $"Non-drivable accident {accident.AccidentNumber}" });
            await SetAccidentHoldAsync(vehicle, accident, actor.Value, cancellationToken);
        }
        dbContext.FleetCommandReceipts.Add(new FleetCommandReceipt { CommandName = "create-accident", IdempotencyKey = idempotencyKey.Trim(), RequestHash = hash, ResultEntityId = accident.Id });
        try { await dbContext.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken); }
        catch (DbUpdateException) { await tx.RollbackAsync(cancellationToken); return Result.Failure<VehicleAccidentDetailResponse>(FleetErrors.Conflict); }
        return Result.Success(await BuildDetailAsync(accident, cancellationToken));
    }

    public async Task<Result<VehicleAccidentAttachmentResponse>> UploadEvidenceAsync(Guid accidentId, VehicleAccidentEvidenceType evidenceType, PrivateFileUpload file, CancellationToken cancellationToken = default)
    {
        var accident = await dbContext.VehicleAccidents.SingleOrDefaultAsync(x => x.Id == accidentId, cancellationToken);
        if (accident is null) return Result.Failure<VehicleAccidentAttachmentResponse>(FleetErrors.NotFound);
        var access = await GetVehicleAsync(accident.VehicleId, PermissionKeys.Fleet.AccidentsReport, cancellationToken);
        if (access.IsFailure) return Result.Failure<VehicleAccidentAttachmentResponse>(access.Error);
        if (await dbContext.VehicleAccidentAttachments.CountAsync(x => x.VehicleAccidentId == accidentId, cancellationToken) >= 5) return Result.Failure<VehicleAccidentAttachmentResponse>(FleetErrors.FileLimit);
        var id = Guid.CreateVersion7();
        var stored = await fileStorage.StoreAsync($"vehicle-accidents/{accidentId:N}/evidence/{id:N}", file, MaximumEvidenceSize, cancellationToken);
        if (stored.IsFailure) return Result.Failure<VehicleAccidentAttachmentResponse>(FleetErrors.InvalidFile);
        var attachment = new VehicleAccidentAttachment { Id = id, VehicleAccidentId = accidentId, EvidenceType = evidenceType, OriginalFileName = stored.Value!.OriginalFileName, StoredFileName = stored.Value.StoredFileName, ContentType = stored.Value.ContentType, FileSizeBytes = stored.Value.Length, Sha256Checksum = stored.Value.Sha256Checksum, StoragePath = stored.Value.StoragePath, UploadedByUserId = support.UserId!.Value, UploadedAtUtc = support.UtcNow };
        dbContext.VehicleAccidentAttachments.Add(attachment);
        dbContext.VehicleAccidentEvents.Add(new VehicleAccidentEvent { VehicleAccidentId = accident.Id, EventType = VehicleAccidentEventType.EvidenceAdded, OccurredAtUtc = support.UtcNow, ActorUserId = support.UserId.Value, Reason = stored.Value.OriginalFileName });
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch { fileStorage.DeleteBestEffort(stored.Value.StoragePath); throw; }
        return Result.Success(MapAttachment(attachment));
    }

    public async Task<Result<PrivateFileDownload>> DownloadEvidenceAsync(Guid accidentId, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await dbContext.VehicleAccidentAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == attachmentId && x.VehicleAccidentId == accidentId, cancellationToken);
        if (attachment is null) return Result.Failure<PrivateFileDownload>(FleetErrors.NotFound);
        var accident = await dbContext.VehicleAccidents.AsNoTracking().SingleAsync(x => x.Id == accidentId, cancellationToken);
        var access = await GetVehicleAsync(accident.VehicleId, PermissionKeys.Fleet.AccidentsDownload, cancellationToken);
        if (access.IsFailure) return Result.Failure<PrivateFileDownload>(access.Error);
        var file = await fileStorage.OpenReadAsync(attachment.StoragePath, attachment.ContentType, attachment.OriginalFileName, attachment.FileSizeBytes, cancellationToken);
        return file.IsFailure ? Result.Failure<PrivateFileDownload>(FleetErrors.FileMissing) : file;
    }

    public async Task<Result<VehicleAccidentReportVersionResponse>> FinalizeAsync(Guid accidentId, AccidentActionRequest request, CancellationToken cancellationToken = default)
    {
        var accident = await dbContext.VehicleAccidents.SingleOrDefaultAsync(x => x.Id == accidentId, cancellationToken);
        if (accident is null) return Result.Failure<VehicleAccidentReportVersionResponse>(FleetErrors.NotFound);
        var access = await GetVehicleAsync(accident.VehicleId, PermissionKeys.Fleet.AccidentsFinalize, cancellationToken);
        if (access.IsFailure) return Result.Failure<VehicleAccidentReportVersionResponse>(access.Error);
        if (accident.Status != VehicleAccidentStatus.Reported || !FleetServiceSupport.MatchesRowVersion(accident.RowVersion, request.RowVersion) || string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure<VehicleAccidentReportVersionResponse>(FleetErrors.InvalidState);
        var result = await GenerateReportAsync(accident, null, request.Reason, cancellationToken);
        if (result.IsFailure) return result;
        accident.Status = VehicleAccidentStatus.Finalized; accident.ReviewedByUserId = support.UserId;
        dbContext.VehicleAccidentEvents.Add(new VehicleAccidentEvent { VehicleAccidentId = accident.Id, EventType = VehicleAccidentEventType.Finalized, OccurredAtUtc = support.UtcNow, ActorUserId = support.UserId!.Value, Reason = request.Reason.Trim() });
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<Result<VehicleAccidentReportVersionResponse>> CorrectAsync(Guid accidentId, CorrectVehicleAccidentRequest request, CancellationToken cancellationToken = default)
    {
        var accident = await dbContext.VehicleAccidents.SingleOrDefaultAsync(x => x.Id == accidentId, cancellationToken);
        if (accident is null) return Result.Failure<VehicleAccidentReportVersionResponse>(FleetErrors.NotFound);
        var access = await GetVehicleAsync(accident.VehicleId, PermissionKeys.Fleet.AccidentsFinalize, cancellationToken);
        if (access.IsFailure) return Result.Failure<VehicleAccidentReportVersionResponse>(access.Error);
        if (accident.Status != VehicleAccidentStatus.Finalized || !FleetServiceSupport.MatchesRowVersion(accident.RowVersion, request.RowVersion) || string.IsNullOrWhiteSpace(request.CorrectionReason)
            || string.IsNullOrWhiteSpace(request.LocationDescription) || string.IsNullOrWhiteSpace(request.DamageDescription) || string.IsNullOrWhiteSpace(request.Narrative)) return Result.Failure<VehicleAccidentReportVersionResponse>(FleetErrors.InvalidRequest);
        ApplyCorrection(accident, request);
        dbContext.VehicleAccidentEvents.Add(new VehicleAccidentEvent { VehicleAccidentId = accident.Id, EventType = VehicleAccidentEventType.Corrected, OccurredAtUtc = support.UtcNow, ActorUserId = support.UserId!.Value, Reason = request.CorrectionReason.Trim(), SnapshotJson = JsonSerializer.Serialize(request) });
        var report = await GenerateReportAsync(accident, accident.CurrentReportVersionId, request.CorrectionReason, cancellationToken);
        if (report.IsFailure) return report;
        await dbContext.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<Result<VehicleAccidentDetailResponse>> CloseAsync(Guid accidentId, AccidentActionRequest request, CancellationToken cancellationToken = default)
    {
        var accident = await dbContext.VehicleAccidents.SingleOrDefaultAsync(x => x.Id == accidentId, cancellationToken);
        if (accident is null) return Result.Failure<VehicleAccidentDetailResponse>(FleetErrors.NotFound);
        var access = await GetVehicleAsync(accident.VehicleId, PermissionKeys.Fleet.AccidentsFinalize, cancellationToken);
        if (access.IsFailure) return Result.Failure<VehicleAccidentDetailResponse>(access.Error);
        if (accident.Status != VehicleAccidentStatus.Finalized || !FleetServiceSupport.MatchesRowVersion(accident.RowVersion, request.RowVersion) || string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure<VehicleAccidentDetailResponse>(FleetErrors.InvalidState);
        accident.Status = VehicleAccidentStatus.Closed; accident.ClosedAtUtc = support.UtcNow; accident.ClosedByUserId = support.UserId;
        dbContext.VehicleAccidentEvents.Add(new VehicleAccidentEvent { VehicleAccidentId = accident.Id, EventType = VehicleAccidentEventType.Closed, OccurredAtUtc = support.UtcNow, ActorUserId = support.UserId!.Value, Reason = request.Reason.Trim() });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(await BuildDetailAsync(accident, cancellationToken));
    }

    public async Task<Result<PrivateFileDownload>> DownloadReportAsync(Guid accidentId, Guid? reportVersionId, CancellationToken cancellationToken = default)
    {
        var accident = await dbContext.VehicleAccidents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accidentId, cancellationToken);
        if (accident is null) return Result.Failure<PrivateFileDownload>(FleetErrors.NotFound);
        var access = await GetVehicleAsync(accident.VehicleId, PermissionKeys.Fleet.AccidentsDownload, cancellationToken);
        if (access.IsFailure) return Result.Failure<PrivateFileDownload>(access.Error);
        var version = await dbContext.VehicleAccidentReportVersions.AsNoTracking().SingleOrDefaultAsync(x => x.VehicleAccidentId == accidentId && (reportVersionId.HasValue ? x.Id == reportVersionId : x.Id == accident.CurrentReportVersionId), cancellationToken);
        if (version is null) return Result.Failure<PrivateFileDownload>(FleetErrors.NotFound);
        var file = await fileStorage.OpenReadAsync(version.StoragePath, "application/pdf", $"{version.ReportNumber}.pdf", version.FileSizeBytes, cancellationToken);
        return file.IsFailure ? Result.Failure<PrivateFileDownload>(FleetErrors.FileMissing) : file;
    }

    private async Task<Result<VehicleAccidentReportVersionResponse>> GenerateReportAsync(VehicleAccident accident, Guid? supersedes, string? correctionReason, CancellationToken cancellationToken)
    {
        var nextVersion = await dbContext.VehicleAccidentReportVersions.Where(x => x.VehicleAccidentId == accident.Id).MaxAsync(x => (int?)x.VersionNumber, cancellationToken) + 1 ?? 1;
        var reportNumber = $"AR-{accident.AccidentNumber}-V{nextVersion}";
        var snapshot = await BuildPdfSnapshotAsync(accident, reportNumber, cancellationToken);
        byte[] bytes;
        try { bytes = pdfGenerator.Generate(snapshot); }
        catch (Exception) { return Result.Failure<VehicleAccidentReportVersionResponse>(FleetErrors.InvalidFile); }
        await using var stream = new MemoryStream(bytes, writable: false);
        var stored = await fileStorage.StoreAsync($"vehicle-accidents/{accident.Id:N}/reports/{Guid.CreateVersion7():N}", new PrivateFileUpload(stream, $"{reportNumber}.pdf", "application/pdf", bytes.LongLength), MaximumGeneratedPdfSize, cancellationToken);
        if (stored.IsFailure) return Result.Failure<VehicleAccidentReportVersionResponse>(FleetErrors.InvalidFile);
        var version = new VehicleAccidentReportVersion
        {
            VehicleAccidentId = accident.Id, VersionNumber = nextVersion, ReportNumber = reportNumber,
            SnapshotJson = JsonSerializer.Serialize(snapshot with { Evidence = snapshot.Evidence.Select(x => x with { ImageBytes = null }).ToArray() }),
            StoredFileName = stored.Value!.StoredFileName, StoragePath = stored.Value.StoragePath, FileSizeBytes = stored.Value.Length,
            Sha256Checksum = stored.Value.Sha256Checksum, GeneratedAtUtc = support.UtcNow, GeneratedByUserId = support.UserId!.Value,
            SupersedesReportVersionId = supersedes, CorrectionReason = FleetServiceSupport.TrimOrNull(correctionReason)
        };
        dbContext.VehicleAccidentReportVersions.Add(version);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            accident.CurrentReportVersionId = version.Id;
        }
        catch { fileStorage.DeleteBestEffort(stored.Value.StoragePath); throw; }
        return Result.Success(MapReport(version));
    }

    private async Task<AccidentPdfSnapshot> BuildPdfSnapshotAsync(VehicleAccident accident, string reportNumber, CancellationToken cancellationToken)
    {
        var identity = await (from employee in dbContext.Employees.AsNoTracking()
                              join vehicle in dbContext.Vehicles.AsNoTracking() on accident.VehicleId equals vehicle.Id
                              where employee.Id == accident.EmployeeId
                              select new { employee.FullNameAr, employee.FullNameEn, employee.EmployeeNumber, vehicle.AssetNumber, vehicle.PlateNumberAr, vehicle.PlateNumberEn }).SingleAsync(cancellationToken);
        var insurance = accident.VehicleInsurancePolicyId.HasValue ? await dbContext.VehicleInsurancePolicies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accident.VehicleInsurancePolicyId, cancellationToken) : null;
        var files = await dbContext.VehicleAccidentAttachments.AsNoTracking().Where(x => x.VehicleAccidentId == accident.Id).OrderBy(x => x.UploadedAtUtc).ToArrayAsync(cancellationToken);
        var evidence = new List<AccidentPdfEvidence>();
        foreach (var file in files)
        {
            byte[]? image = null;
            if (file.ContentType is "image/jpeg" or "image/png")
            {
                var opened = await fileStorage.OpenReadAsync(file.StoragePath, file.ContentType, file.OriginalFileName, file.FileSizeBytes, cancellationToken);
                if (opened.IsSuccess)
                {
                    await using var content = opened.Value!.Content;
                    using var memory = new MemoryStream();
                    await content.CopyToAsync(memory, cancellationToken);
                    image = memory.ToArray();
                }
            }
            evidence.Add(new AccidentPdfEvidence(file.OriginalFileName, file.ContentType, file.Sha256Checksum, image));
        }
        return new AccidentPdfSnapshot(reportNumber, accident.AccidentNumber, accident.OccurredAtUtc, identity.FullNameAr, identity.FullNameEn, identity.EmployeeNumber, identity.AssetNumber, identity.PlateNumberAr, identity.PlateNumberEn, accident.LocationDescription, accident.Severity, accident.IsDrivable, accident.HasInjuries, accident.InjuryDetails, accident.ThirdPartyDetails, accident.DamageDescription, accident.FaultAssessment, accident.Narrative, accident.PoliceReportNumber, accident.InsuranceClaimNumber, insurance?.ProviderName, insurance?.PolicyNumber, support.UtcNow, evidence);
    }

    private async Task SetAccidentHoldAsync(Vehicle vehicle, VehicleAccident accident, Guid actor, CancellationToken cancellationToken)
    {
        var status = await dbContext.VehicleOperationalStatusPeriods.SingleOrDefaultAsync(x => x.VehicleId == vehicle.Id && x.EffectiveToUtc == null, cancellationToken);
        if (status is not null) status.EffectiveToUtc = support.UtcNow;
        vehicle.CurrentOperationalStatus = VehicleOperationalStatus.AccidentHold;
        dbContext.VehicleOperationalStatusPeriods.Add(new VehicleOperationalStatusPeriod { VehicleId = vehicle.Id, Status = VehicleOperationalStatus.AccidentHold, EffectiveFromUtc = support.UtcNow, Reason = $"Non-drivable accident {accident.AccidentNumber}", SourceType = VehicleStatusSourceType.Accident, SourceEntityId = accident.Id, ChangedByUserId = actor });
    }

    private async Task<Result<Vehicle>> GetVehicleAsync(Guid id, string permission, CancellationToken cancellationToken, bool tracking = false)
    {
        var query = tracking ? dbContext.Vehicles.AsQueryable() : dbContext.Vehicles.AsNoTracking();
        var vehicle = await query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (vehicle is null) return Result.Failure<Vehicle>(FleetErrors.NotFound);
        return await support.HasVehiclePermissionAsync(vehicle, permission, cancellationToken) ? Result.Success(vehicle) : Result.Failure<Vehicle>(FleetErrors.Forbidden);
    }

    private async Task<VehicleAccidentDetailResponse> BuildDetailAsync(VehicleAccident accident, CancellationToken cancellationToken)
    {
        var identity = await (from employee in dbContext.Employees.AsNoTracking() join vehicle in dbContext.Vehicles.AsNoTracking() on accident.VehicleId equals vehicle.Id where employee.Id == accident.EmployeeId select new { RiderName = employee.FullNameAr, vehicle.AssetNumber, vehicle.PlateNumberAr, vehicle.PlateNumberEn }).SingleAsync(cancellationToken);
        var attachments = await dbContext.VehicleAccidentAttachments.AsNoTracking().Where(x => x.VehicleAccidentId == accident.Id).OrderBy(x => x.UploadedAtUtc).ToArrayAsync(cancellationToken);
        var reports = await dbContext.VehicleAccidentReportVersions.AsNoTracking().Where(x => x.VehicleAccidentId == accident.Id).OrderByDescending(x => x.VersionNumber).ToArrayAsync(cancellationToken);
        return new VehicleAccidentDetailResponse(MapSummary(accident), identity.RiderName, identity.AssetNumber, identity.PlateNumberAr, identity.PlateNumberEn, accident.PoliceReportNumber, accident.InsuranceClaimNumber, accident.HasInjuries, accident.InjuryDetails, accident.ThirdPartyDetails, accident.DamageDescription, accident.FaultAssessment, accident.Narrative, attachments.Select(MapAttachment).ToArray(), reports.Select(MapReport).ToArray());
    }

    private static void ApplyCorrection(VehicleAccident accident, CorrectVehicleAccidentRequest request)
    {
        accident.PoliceReportNumber = FleetServiceSupport.TrimOrNull(request.PoliceReportNumber); accident.InsuranceClaimNumber = FleetServiceSupport.TrimOrNull(request.InsuranceClaimNumber);
        accident.LocationDescription = request.LocationDescription.Trim(); accident.Latitude = request.Latitude; accident.Longitude = request.Longitude; accident.Severity = request.Severity;
        accident.IsDrivable = request.IsDrivable; accident.HasInjuries = request.HasInjuries; accident.InjuryDetails = FleetServiceSupport.TrimOrNull(request.InjuryDetails);
        accident.ThirdPartyDetails = FleetServiceSupport.TrimOrNull(request.ThirdPartyDetails); accident.DamageDescription = request.DamageDescription.Trim(); accident.FaultAssessment = FleetServiceSupport.TrimOrNull(request.FaultAssessment); accident.Narrative = request.Narrative.Trim();
    }

    private static VehicleIssueSeverity ToIssueSeverity(VehicleAccidentSeverity value) => value switch { VehicleAccidentSeverity.Minor => VehicleIssueSeverity.Low, VehicleAccidentSeverity.Moderate => VehicleIssueSeverity.Medium, VehicleAccidentSeverity.Serious => VehicleIssueSeverity.High, _ => VehicleIssueSeverity.Critical };
    private static VehicleAccidentSummaryResponse MapSummary(VehicleAccident x) => new(x.Id, x.AccidentNumber, x.VehicleId, x.RiderProfileId, x.RiderVehicleAssignmentId, x.VehicleIssueId, x.OccurredAtUtc, x.Severity, x.IsDrivable, x.Status, x.LocationDescription, FleetServiceSupport.EncodeRowVersion(x.RowVersion));
    private static VehicleAccidentAttachmentResponse MapAttachment(VehicleAccidentAttachment x) => new(x.Id, x.EvidenceType, x.OriginalFileName, x.ContentType, x.FileSizeBytes, x.Sha256Checksum, x.UploadedAtUtc, FleetServiceSupport.EncodeRowVersion(x.RowVersion));
    private static VehicleAccidentReportVersionResponse MapReport(VehicleAccidentReportVersion x) => new(x.Id, x.VersionNumber, x.ReportNumber, x.FileSizeBytes, x.Sha256Checksum, x.GeneratedAtUtc, x.SupersedesReportVersionId, x.CorrectionReason);
}
