using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class HrWorkflowService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IPermissionChecker permissionChecker,
    TimeProvider timeProvider) : IHrWorkflowService
{
    public async Task<Result<IReadOnlyList<LeaveTypeResponse>>> GetLeaveTypesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.LeaveTypes.AsNoTracking().OrderBy(item => item.NameAr).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<LeaveTypeResponse>>(rows.Select(ToLeaveType).ToArray());
    }

    public async Task<Result<LeaveTypeResponse>> UpsertLeaveTypeAsync(Guid? id, LeaveTypeUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Code) || !HrServiceSupport.HasText(request.NameAr)
            || !HrServiceSupport.HasText(request.NameEn) || request.MaximumCalendarDays is <= 0
            || !TryParseEnum<CatalogStatus>(request.Status, out var status))
            return Result.Failure<LeaveTypeResponse>(HrErrors.InvalidRequest);
        LeaveType entity;
        if (id is null)
        {
            entity = new LeaveType();
            dbContext.LeaveTypes.Add(entity);
        }
        else
        {
            entity = await dbContext.LeaveTypes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<LeaveTypeResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<LeaveTypeResponse>(HrErrors.ConcurrencyConflict);
        }
        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.LeaveTypes.AnyAsync(item => item.Id != entity.Id && item.Code == code, cancellationToken))
            return Result.Failure<LeaveTypeResponse>(HrErrors.Duplicate);
        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = request.NameEn.Trim();
        entity.DescriptionAr = HrServiceSupport.TrimOrNull(request.DescriptionAr);
        entity.DescriptionEn = HrServiceSupport.TrimOrNull(request.DescriptionEn);
        entity.RequiresBalance = request.RequiresBalance;
        entity.RequiresHrDocuments = request.RequiresHrDocuments;
        entity.RequiresExitReentryVisa = request.RequiresExitReentryVisa;
        entity.MaximumCalendarDays = request.MaximumCalendarDays;
        entity.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToLeaveType(entity));
    }

    public async Task<Result<IReadOnlyList<LeaveWorkflowResponse>>> GetLeaveWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var workflows = await dbContext.LeaveApprovalWorkflows.AsNoTracking().OrderByDescending(item => item.Priority).ThenBy(item => item.Code).ToArrayAsync(cancellationToken);
        var ids = workflows.Select(item => item.Id).ToArray();
        var steps = await dbContext.LeaveApprovalWorkflowSteps.AsNoTracking().Where(item => ids.Contains(item.LeaveApprovalWorkflowId))
            .OrderBy(item => item.Sequence).ToArrayAsync(cancellationToken);
        var lookup = steps.ToLookup(item => item.LeaveApprovalWorkflowId);
        return Result.Success<IReadOnlyList<LeaveWorkflowResponse>>(workflows.Select(item => ToWorkflow(item, lookup[item.Id])).ToArray());
    }

    public async Task<Result<LeaveWorkflowResponse>> UpsertLeaveWorkflowAsync(Guid? id, LeaveWorkflowUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!ValidateWorkflow(request, out var status, out var relationshipType)
            || request.LeaveTypeId is not null && !await dbContext.LeaveTypes.AnyAsync(item => item.Id == request.LeaveTypeId, cancellationToken)
            || request.ClientPlatformId is not null && !await dbContext.ClientPlatforms.AnyAsync(item => item.Id == request.ClientPlatformId, cancellationToken))
            return Result.Failure<LeaveWorkflowResponse>(HrErrors.InvalidRequest);
        LeaveApprovalWorkflow entity;
        if (id is null)
        {
            entity = new LeaveApprovalWorkflow();
            dbContext.LeaveApprovalWorkflows.Add(entity);
        }
        else
        {
            entity = await dbContext.LeaveApprovalWorkflows.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<LeaveWorkflowResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<LeaveWorkflowResponse>(HrErrors.ConcurrencyConflict);
        }
        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.LeaveApprovalWorkflows.AnyAsync(item => item.Id != entity.Id && item.Code == code && item.Version == request.Version, cancellationToken))
            return Result.Failure<LeaveWorkflowResponse>(HrErrors.Duplicate);
        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = request.NameEn.Trim();
        entity.Version = request.Version;
        entity.LeaveTypeId = request.LeaveTypeId;
        entity.RelationshipType = relationshipType;
        entity.AppliesToRider = request.AppliesToRider;
        entity.ClientPlatformId = request.ClientPlatformId;
        entity.Priority = request.Priority;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.Status = status;
        var existingSteps = id is null ? [] : await dbContext.LeaveApprovalWorkflowSteps.Where(item => item.LeaveApprovalWorkflowId == entity.Id).ToListAsync(cancellationToken);
        var requestedKeys = request.Steps.Select(item => HrServiceSupport.NormalizeCode(item.StepKey)).ToHashSet();
        foreach (var old in existingSteps.Where(item => !requestedKeys.Contains(item.StepKey)))
        {
            old.IsDeleted = true;
            old.DeletionReason = "Removed from workflow definition.";
        }
        foreach (var stepRequest in request.Steps)
        {
            var key = HrServiceSupport.NormalizeCode(stepRequest.StepKey);
            var step = existingSteps.SingleOrDefault(item => item.StepKey == key);
            if (step is null)
            {
                step = new LeaveApprovalWorkflowStep { LeaveApprovalWorkflowId = entity.Id, StepKey = key };
                dbContext.LeaveApprovalWorkflowSteps.Add(step);
            }
            step.Sequence = stepRequest.Sequence;
            step.NameAr = stepRequest.NameAr.Trim();
            step.NameEn = stepRequest.NameEn.Trim();
            step.RequiredPermissionKey = stepRequest.RequiredPermissionKey.Trim();
            step.ScopeSource = Enum.Parse<LeaveApprovalScopeSource>(stepRequest.ScopeSource, true);
            step.AllowsReturnForChanges = stepRequest.AllowsReturnForChanges;
            step.RequiresCommentOnApproval = stepRequest.RequiresCommentOnApproval;
            step.TargetResponseHours = stepRequest.TargetResponseHours;
            step.Status = CatalogStatus.Active;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetLeaveWorkflowsAsync(cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<IReadOnlyList<LeaveRequestResponse>>> GetLeaveRequestsAsync(Guid? employeeId, CancellationToken cancellationToken = default)
    {
        var rows = await (from request in dbContext.LeaveRequests.AsNoTracking()
                          join employee in dbContext.Employees.AsNoTracking() on request.EmployeeId equals employee.Id
                          join type in dbContext.LeaveTypes.AsNoTracking() on request.LeaveTypeId equals type.Id
                          where employeeId == null || request.EmployeeId == employeeId
                          orderby request.StartDate descending
                          select new LeaveProjection(request, employee.FullNameAr, type.NameAr)).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<LeaveRequestResponse>>(rows.Select(ToLeave).ToArray());
    }

    public async Task<Result<LeaveRequestResponse>> UpsertLeaveRequestAsync(Guid? id, LeaveRequestUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!ValidateLeaveRequest(request, out var days)) return Result.Failure<LeaveRequestResponse>(HrErrors.InvalidRequest);
        var leaveType = await dbContext.LeaveTypes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.LeaveTypeId && item.Status == CatalogStatus.Active, cancellationToken);
        if (leaveType is null || !await dbContext.Employees.AnyAsync(item => item.Id == request.EmployeeId, cancellationToken)
            || request.RelatedClientContractId is not null && !await dbContext.ClientContracts.AnyAsync(item => item.Id == request.RelatedClientContractId, cancellationToken)
            || leaveType.MaximumCalendarDays is not null && days > leaveType.MaximumCalendarDays)
            return Result.Failure<LeaveRequestResponse>(HrErrors.InvalidRequest);
        var overlap = await dbContext.LeaveRequests.AnyAsync(item => item.Id != id && item.EmployeeId == request.EmployeeId
            && item.Status != LeaveWorkflowStatus.Cancelled && item.Status != LeaveWorkflowStatus.Rejected
            && item.StartDate <= request.EndDate && item.EndDate >= request.StartDate, cancellationToken);
        if (overlap) return Result.Failure<LeaveRequestResponse>(HrErrors.Conflict);
        LeaveRequest entity;
        if (id is null)
        {
            entity = new LeaveRequest { RequestNumber = NewNumber("LV") };
            dbContext.LeaveRequests.Add(entity);
        }
        else
        {
            entity = await dbContext.LeaveRequests.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<LeaveRequestResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<LeaveRequestResponse>(HrErrors.ConcurrencyConflict);
            if (entity.Status is not (LeaveWorkflowStatus.Draft or LeaveWorkflowStatus.ReturnedForChanges)) return Result.Failure<LeaveRequestResponse>(HrErrors.Conflict);
        }
        entity.EmployeeId = request.EmployeeId;
        entity.LeaveTypeId = request.LeaveTypeId;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.ExpectedReturnDate = request.ExpectedReturnDate;
        entity.CalendarDays = days;
        entity.Reason = request.Reason.Trim();
        entity.DestinationCountryCode = HrServiceSupport.TrimOrNull(request.DestinationCountryCode)?.ToUpperInvariant();
        entity.ContactPhoneDuringLeave = HrServiceSupport.TrimOrNull(request.ContactPhoneDuringLeave);
        entity.EmergencyContactName = HrServiceSupport.TrimOrNull(request.EmergencyContactName);
        entity.EmergencyContactPhone = HrServiceSupport.TrimOrNull(request.EmergencyContactPhone);
        entity.RelatedClientContractId = request.RelatedClientContractId;
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetLeaveRequestsAsync(null, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<LeaveRequestResponse>> TransitionLeaveAsync(Guid id, LeaveTransitionRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || !HrServiceSupport.HasText(request.Action))
            return Result.Failure<LeaveRequestResponse>(HrErrors.CurrentUserUnavailable);
        var entity = await dbContext.LeaveRequests.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return Result.Failure<LeaveRequestResponse>(HrErrors.NotFound);
        if (request.RowVersion is not null && !HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            return Result.Failure<LeaveRequestResponse>(HrErrors.ConcurrencyConflict);
        var action = request.Action.Trim().ToLowerInvariant();
        Result transition = action switch
        {
            "submit" => await SubmitLeave(entity, cancellationToken),
            "approve" => await DecideLeave(entity, LeaveDecisionType.Approved, request.Comment, cancellationToken),
            "reject" => await DecideLeave(entity, LeaveDecisionType.Rejected, request.Comment, cancellationToken),
            "return" => await DecideLeave(entity, LeaveDecisionType.ReturnedForChanges, request.Comment, cancellationToken),
            "activate" when entity.Status == LeaveWorkflowStatus.Approved => ActivateLeave(entity),
            "complete" when entity.Status == LeaveWorkflowStatus.Active => CompleteLeave(entity),
            "force-cancel" when entity.Status is not (LeaveWorkflowStatus.Completed or LeaveWorkflowStatus.Cancelled) => CancelLeave(entity, request.Comment, userId),
            _ => Result.Failure(HrErrors.Conflict)
        };
        if (transition.IsFailure) return Result.Failure<LeaveRequestResponse>(transition.Error);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetLeaveRequestsAsync(null, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<IReadOnlyList<LeaveDateChangeResponse>>> GetLeaveDateChangesAsync(
        Guid leaveRequestId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.LeaveRequests.AsNoTracking().AnyAsync(item => item.Id == leaveRequestId, cancellationToken))
            return Result.Failure<IReadOnlyList<LeaveDateChangeResponse>>(HrErrors.NotFound);
        var rows = await dbContext.LeaveDateChangeRequests.AsNoTracking()
            .Where(item => item.LeaveRequestId == leaveRequestId)
            .OrderByDescending(item => item.RequestedAtUtc)
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<LeaveDateChangeResponse>>(rows.Select(ToDateChange).ToArray());
    }

    public async Task<Result<LeaveDateChangeResponse>> RequestLeaveDateChangeAsync(
        Guid leaveRequestId,
        LeaveDateChangeCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId
            || request.RequestedEndDate < request.RequestedStartDate
            || string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure<LeaveDateChangeResponse>(HrErrors.InvalidRequest);
        var leave = await dbContext.LeaveRequests.SingleOrDefaultAsync(item => item.Id == leaveRequestId, cancellationToken);
        if (leave is null) return Result.Failure<LeaveDateChangeResponse>(HrErrors.NotFound);
        if (leave.Status is not (LeaveWorkflowStatus.Approved or LeaveWorkflowStatus.Active))
            return Result.Failure<LeaveDateChangeResponse>(HrErrors.Conflict);
        if (await dbContext.LeaveDateChangeRequests.AnyAsync(
            item => item.LeaveRequestId == leaveRequestId && item.Status == LeaveChangeRequestStatus.Pending,
            cancellationToken))
            return Result.Failure<LeaveDateChangeResponse>(HrErrors.Conflict);
        var overlap = await dbContext.LeaveRequests.AnyAsync(item => item.Id != leave.Id
            && item.EmployeeId == leave.EmployeeId
            && item.Status != LeaveWorkflowStatus.Cancelled && item.Status != LeaveWorkflowStatus.Rejected
            && item.StartDate <= request.RequestedEndDate && item.EndDate >= request.RequestedStartDate,
            cancellationToken);
        if (overlap) return Result.Failure<LeaveDateChangeResponse>(HrErrors.Conflict);

        var change = new LeaveDateChangeRequest
        {
            LeaveRequestId = leave.Id,
            PreviousStartDate = leave.StartDate,
            PreviousEndDate = leave.EndDate,
            RequestedStartDate = request.RequestedStartDate,
            RequestedEndDate = request.RequestedEndDate,
            Reason = request.Reason.Trim(),
            RequestedByUserId = userId,
            RequestedAtUtc = timeProvider.GetUtcNow()
        };
        dbContext.LeaveDateChangeRequests.Add(change);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToDateChange(change));
    }

    public async Task<Result<LeaveDateChangeResponse>> ResolveLeaveDateChangeAsync(
        Guid leaveRequestId,
        Guid changeId,
        LeaveChangeResolveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || string.IsNullOrWhiteSpace(request.ResolutionReason))
            return Result.Failure<LeaveDateChangeResponse>(HrErrors.InvalidRequest);
        var change = await dbContext.LeaveDateChangeRequests.SingleOrDefaultAsync(
            item => item.Id == changeId && item.LeaveRequestId == leaveRequestId,
            cancellationToken);
        if (change is null) return Result.Failure<LeaveDateChangeResponse>(HrErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(change.RowVersion, request.RowVersion))
            return Result.Failure<LeaveDateChangeResponse>(HrErrors.ConcurrencyConflict);
        if (change.Status != LeaveChangeRequestStatus.Pending)
            return Result.Failure<LeaveDateChangeResponse>(HrErrors.Conflict);
        var leave = await dbContext.LeaveRequests.SingleAsync(item => item.Id == leaveRequestId, cancellationToken);
        if (request.Approve)
        {
            var overlap = await dbContext.LeaveRequests.AnyAsync(item => item.Id != leave.Id
                && item.EmployeeId == leave.EmployeeId
                && item.Status != LeaveWorkflowStatus.Cancelled && item.Status != LeaveWorkflowStatus.Rejected
                && item.StartDate <= change.RequestedEndDate && item.EndDate >= change.RequestedStartDate,
                cancellationToken);
            if (overlap) return Result.Failure<LeaveDateChangeResponse>(HrErrors.Conflict);
            leave.StartDate = change.RequestedStartDate;
            leave.EndDate = change.RequestedEndDate;
            leave.CalendarDays = change.RequestedEndDate.DayNumber - change.RequestedStartDate.DayNumber + 1;
            if (leave.ExpectedReturnDate < leave.EndDate)
                leave.ExpectedReturnDate = leave.EndDate.AddDays(1);
            change.Status = LeaveChangeRequestStatus.Approved;
        }
        else
        {
            change.Status = LeaveChangeRequestStatus.Rejected;
        }
        change.ResolvedByUserId = userId;
        change.ResolvedAtUtc = timeProvider.GetUtcNow();
        change.ResolutionReason = request.ResolutionReason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToDateChange(change));
    }

    public async Task<Result<IReadOnlyList<LeaveCancellationResponse>>> GetLeaveCancellationsAsync(
        Guid leaveRequestId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.LeaveRequests.AsNoTracking().AnyAsync(item => item.Id == leaveRequestId, cancellationToken))
            return Result.Failure<IReadOnlyList<LeaveCancellationResponse>>(HrErrors.NotFound);
        var rows = await dbContext.LeaveCancellationRequests.AsNoTracking()
            .Where(item => item.LeaveRequestId == leaveRequestId)
            .OrderByDescending(item => item.RequestedAtUtc)
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<LeaveCancellationResponse>>(rows.Select(ToCancellation).ToArray());
    }

    public async Task<Result<LeaveCancellationResponse>> RequestLeaveCancellationAsync(
        Guid leaveRequestId,
        LeaveCancellationCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure<LeaveCancellationResponse>(HrErrors.InvalidRequest);
        var leave = await dbContext.LeaveRequests.SingleOrDefaultAsync(item => item.Id == leaveRequestId, cancellationToken);
        if (leave is null) return Result.Failure<LeaveCancellationResponse>(HrErrors.NotFound);
        if (leave.Status is not (LeaveWorkflowStatus.PendingApproval or LeaveWorkflowStatus.Approved or LeaveWorkflowStatus.Active))
            return Result.Failure<LeaveCancellationResponse>(HrErrors.Conflict);
        if (await dbContext.LeaveCancellationRequests.AnyAsync(
            item => item.LeaveRequestId == leaveRequestId && item.Status == LeaveChangeRequestStatus.Pending,
            cancellationToken))
            return Result.Failure<LeaveCancellationResponse>(HrErrors.Conflict);
        var cancellation = new LeaveCancellationRequest
        {
            LeaveRequestId = leave.Id,
            Reason = request.Reason.Trim(),
            PreviousLeaveStatus = leave.Status,
            RequestedByUserId = userId,
            RequestedAtUtc = timeProvider.GetUtcNow()
        };
        leave.Status = LeaveWorkflowStatus.CancellationPending;
        dbContext.LeaveCancellationRequests.Add(cancellation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToCancellation(cancellation));
    }

    public async Task<Result<LeaveCancellationResponse>> ResolveLeaveCancellationAsync(
        Guid leaveRequestId,
        Guid cancellationId,
        LeaveChangeResolveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || string.IsNullOrWhiteSpace(request.ResolutionReason))
            return Result.Failure<LeaveCancellationResponse>(HrErrors.InvalidRequest);
        var cancellation = await dbContext.LeaveCancellationRequests.SingleOrDefaultAsync(
            item => item.Id == cancellationId && item.LeaveRequestId == leaveRequestId,
            cancellationToken);
        if (cancellation is null) return Result.Failure<LeaveCancellationResponse>(HrErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(cancellation.RowVersion, request.RowVersion))
            return Result.Failure<LeaveCancellationResponse>(HrErrors.ConcurrencyConflict);
        if (cancellation.Status != LeaveChangeRequestStatus.Pending)
            return Result.Failure<LeaveCancellationResponse>(HrErrors.Conflict);
        var leave = await dbContext.LeaveRequests.SingleAsync(item => item.Id == leaveRequestId, cancellationToken);
        if (request.Approve)
        {
            var result = CancelLeave(leave, cancellation.Reason, userId);
            if (result.IsFailure) return Result.Failure<LeaveCancellationResponse>(result.Error);
            cancellation.Status = LeaveChangeRequestStatus.Approved;
        }
        else
        {
            leave.Status = cancellation.PreviousLeaveStatus ?? LeaveWorkflowStatus.Approved;
            cancellation.Status = LeaveChangeRequestStatus.Rejected;
        }
        cancellation.ResolvedByUserId = userId;
        cancellation.ResolvedAtUtc = timeProvider.GetUtcNow();
        cancellation.ResolutionReason = request.ResolutionReason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToCancellation(cancellation));
    }

    public async Task<Result<IReadOnlyList<AbsenceCaseResponse>>> GetAbsenceCasesAsync(Guid? employeeId, CancellationToken cancellationToken = default)
    {
        var rows = await (from item in dbContext.EmployeeAbsenceComplianceCases.AsNoTracking()
                          join employee in dbContext.Employees.AsNoTracking() on item.EmployeeId equals employee.Id
                          where employeeId == null || item.EmployeeId == employeeId
                          orderby item.RemovalDeadline
                          select new AbsenceProjection(item, employee.FullNameAr)).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<AbsenceCaseResponse>>(rows.Select(ToAbsence).ToArray());
    }

    public async Task<Result<AbsenceCaseResponse>> UpsertAbsenceCaseAsync(Guid? id, AbsenceCaseUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || !TryParseEnum<AbsenceCasePath>(request.CurrentPath, out var path)
            || !ValidateAbsencePath(request, path) || !await dbContext.Employees.AnyAsync(item => item.Id == request.EmployeeId, cancellationToken))
            return Result.Failure<AbsenceCaseResponse>(HrErrors.InvalidRequest);
        EmployeeAbsenceComplianceCase entity;
        AbsenceCaseEventType eventType;
        string? before;
        if (id is null)
        {
            entity = new EmployeeAbsenceComplianceCase { CaseNumber = NewNumber("AB"), Status = AbsenceCaseStatus.Open };
            dbContext.EmployeeAbsenceComplianceCases.Add(entity);
            eventType = AbsenceCaseEventType.Opened;
            before = null;
        }
        else
        {
            entity = await dbContext.EmployeeAbsenceComplianceCases.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<AbsenceCaseResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<AbsenceCaseResponse>(HrErrors.ConcurrencyConflict);
            if (entity.Status is AbsenceCaseStatus.Closed) return Result.Failure<AbsenceCaseResponse>(HrErrors.Conflict);
            eventType = entity.CurrentPath == path ? AbsenceCaseEventType.Corrected : AbsenceCaseEventType.PathChanged;
            before = JsonSerializer.Serialize(new { entity.CurrentPath, entity.RemovalDeadline });
        }
        entity.EmployeeId = request.EmployeeId;
        entity.AbsenceDate = request.AbsenceDate;
        entity.CurrentPath = path;
        entity.ReportedToAuthoritiesDate = request.ReportedToAuthoritiesDate;
        entity.AuthorityReportReference = HrServiceSupport.TrimOrNull(request.AuthorityReportReference);
        entity.ExitOrOutageDate = request.ExitOrOutageDate;
        entity.ExitVisaNumber = HrServiceSupport.TrimOrNull(request.ExitVisaNumber);
        entity.RemovalDeadline = request.RemovalDeadline;
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        dbContext.EmployeeAbsenceComplianceCaseEvents.Add(CreateAbsenceEvent(entity, eventType, userId, "Case data saved.", before));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAbsenceCasesAsync(null, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<AbsenceCaseResponse>> TransitionAbsenceCaseAsync(Guid id, AbsenceCaseTransitionRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || !TryParseEnum<AbsenceCaseStatus>(request.Status, out var status)
            || !HrServiceSupport.HasText(request.Reason)) return Result.Failure<AbsenceCaseResponse>(HrErrors.InvalidRequest);
        var entity = await dbContext.EmployeeAbsenceComplianceCases.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return Result.Failure<AbsenceCaseResponse>(HrErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<AbsenceCaseResponse>(HrErrors.ConcurrencyConflict);
        if (entity.Status == AbsenceCaseStatus.Closed) return Result.Failure<AbsenceCaseResponse>(HrErrors.Conflict);
        var before = JsonSerializer.Serialize(new { entity.Status, entity.ResolutionCode });
        entity.Status = status;
        if (status == AbsenceCaseStatus.Resolved)
        {
            entity.ResolvedAtUtc = timeProvider.GetUtcNow();
            entity.ResolvedByUserId = userId;
            entity.ResolutionCode = HrServiceSupport.TrimOrNull(request.ResolutionCode);
            entity.ResolutionNotes = HrServiceSupport.TrimOrNull(request.ResolutionNotes);
        }
        if (status == AbsenceCaseStatus.Closed)
        {
            entity.ClosedAtUtc = timeProvider.GetUtcNow();
            entity.ClosedByUserId = userId;
        }
        var eventType = status switch { AbsenceCaseStatus.Resolved => AbsenceCaseEventType.Resolved, AbsenceCaseStatus.Cancelled => AbsenceCaseEventType.Cancelled, AbsenceCaseStatus.Closed => AbsenceCaseEventType.Closed, _ => AbsenceCaseEventType.Corrected };
        dbContext.EmployeeAbsenceComplianceCaseEvents.Add(CreateAbsenceEvent(entity, eventType, userId, request.Reason.Trim(), before));
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAbsenceCasesAsync(null, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<IReadOnlyList<EmployeeStatusChangeResponse>>> GetStatusChangeRequestsAsync(Guid? employeeId, CancellationToken cancellationToken = default)
    {
        var rows = await (from item in dbContext.EmployeeStatusChangeRequests.AsNoTracking()
                          join employee in dbContext.Employees.AsNoTracking() on item.EmployeeId equals employee.Id
                          where employeeId == null || item.EmployeeId == employeeId
                          orderby item.RequestedAtUtc descending
                          select new StatusChangeProjection(item, employee.FullNameAr)).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<EmployeeStatusChangeResponse>>(rows.Select(ToStatusChange).ToArray());
    }

    public async Task<Result<EmployeeStatusChangeResponse>> CreateStatusChangeRequestAsync(EmployeeStatusChangeCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || !TryParseEnum<EmployeeStatus>(request.RequestedStatus, out var requestedStatus)
            || requestedStatus == EmployeeStatus.Archived
            || !HrServiceSupport.HasText(request.Reason)) return Result.Failure<EmployeeStatusChangeResponse>(HrErrors.InvalidRequest);
        var employee = await dbContext.Employees.SingleOrDefaultAsync(item => item.Id == request.EmployeeId, cancellationToken);
        if (employee is null) return Result.Failure<EmployeeStatusChangeResponse>(HrErrors.NotFound);
        if (employee.Status == requestedStatus || await dbContext.EmployeeStatusChangeRequests.AnyAsync(item => item.EmployeeId == request.EmployeeId && item.Status == EmployeeStatusChangeRequestStatus.Pending, cancellationToken))
            return Result.Failure<EmployeeStatusChangeResponse>(HrErrors.Conflict);
        var entity = new EmployeeStatusChangeRequest
        {
            RequestNumber = NewNumber("SC"), EmployeeId = request.EmployeeId, FromStatus = employee.Status,
            RequestedStatus = requestedStatus, EffectiveFrom = request.EffectiveFrom, Reason = request.Reason.Trim(),
            Status = EmployeeStatusChangeRequestStatus.Pending, RequestedByUserId = userId,
            RequestedAtUtc = timeProvider.GetUtcNow()
        };
        dbContext.EmployeeStatusChangeRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetStatusChangeRequestsAsync(null, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<EmployeeStatusChangeResponse>> ResolveStatusChangeRequestAsync(Guid id, EmployeeStatusChangeResolveRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || !HrServiceSupport.HasText(request.ResolutionReason))
            return Result.Failure<EmployeeStatusChangeResponse>(HrErrors.InvalidRequest);
        var entity = await dbContext.EmployeeStatusChangeRequests.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return Result.Failure<EmployeeStatusChangeResponse>(HrErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<EmployeeStatusChangeResponse>(HrErrors.ConcurrencyConflict);
        if (entity.Status != EmployeeStatusChangeRequestStatus.Pending) return Result.Failure<EmployeeStatusChangeResponse>(HrErrors.Conflict);
        entity.Status = request.Approve ? EmployeeStatusChangeRequestStatus.Approved : EmployeeStatusChangeRequestStatus.Rejected;
        entity.ResolvedByUserId = userId;
        entity.ResolvedAtUtc = timeProvider.GetUtcNow();
        entity.ResolutionReason = request.ResolutionReason.Trim();
        if (request.Approve)
        {
            var employee = await dbContext.Employees.SingleAsync(item => item.Id == entity.EmployeeId, cancellationToken);
            if (entity.RequestedStatus == EmployeeStatus.Active
                && (employee.IqamaNo is not { Length: 10 } iqamaNo || !iqamaNo.All(char.IsAsciiDigit)
                    || employee.EngagementType == EmployeeRelationshipType.SponsoredInternal && employee.SponsorId is null
                    || !employee.IsEmployee && !await dbContext.RiderProfiles.AnyAsync(item => item.EmployeeId == employee.Id, cancellationToken)))
                return Result.Failure<EmployeeStatusChangeResponse>(HrErrors.InvalidRequest);
            var history = new EmployeeWorkHistory
            {
                EmployeeId = entity.EmployeeId,
                ChangeType = EmployeeWorkChangeType.Status,
                OldValue = employee.Status.ToString(),
                NewValue = entity.RequestedStatus.ToString(),
                EffectiveDate = entity.EffectiveFrom,
                Reason = entity.Reason,
                ChangedByUserId = userId,
                CreatedAtUtc = timeProvider.GetUtcNow(),
                CreatedByUserId = userId
            };
            dbContext.EmployeeWorkHistory.Add(history);
            employee.Status = entity.RequestedStatus;
            employee.StatusReason = entity.RequestedStatus is EmployeeStatus.Suspended or EmployeeStatus.Terminated
                ? entity.Reason
                : null;
            if (entity.RequestedStatus == EmployeeStatus.Terminated) employee.TerminationDate = entity.EffectiveFrom;
            entity.ResultingWorkHistoryId = history.Id;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetStatusChangeRequestsAsync(null, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    private async Task<Result> SubmitLeave(LeaveRequest entity, CancellationToken cancellationToken)
    {
        if (entity.Status is not (LeaveWorkflowStatus.Draft or LeaveWorkflowStatus.ReturnedForChanges)) return Result.Failure(HrErrors.Conflict);
        var relationship = await dbContext.Employees.Where(item => item.Id == entity.EmployeeId).Select(item => (EmployeeRelationshipType?)item.EngagementType).SingleAsync(cancellationToken);
        var isRider = await dbContext.RiderProfiles.AnyAsync(item => item.EmployeeId == entity.EmployeeId, cancellationToken);
        var platformId = entity.RelatedClientContractId is null ? null : await dbContext.ClientContracts.Where(item => item.Id == entity.RelatedClientContractId).Select(item => (Guid?)item.ClientPlatformId).SingleOrDefaultAsync(cancellationToken);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var workflow = await dbContext.LeaveApprovalWorkflows
            .Where(item => item.Status == CatalogStatus.Active && item.EffectiveFrom <= today && (item.EffectiveTo == null || item.EffectiveTo >= today)
                && (item.LeaveTypeId == null || item.LeaveTypeId == entity.LeaveTypeId)
                && (item.RelationshipType == null || item.RelationshipType == relationship)
                && (item.AppliesToRider == null || item.AppliesToRider == isRider)
                && (item.ClientPlatformId == null || item.ClientPlatformId == platformId))
            .OrderByDescending(item => item.Priority).ThenByDescending(item => item.Version).FirstOrDefaultAsync(cancellationToken);
        if (workflow is null) return Result.Failure(HrErrors.Conflict);
        var steps = await dbContext.LeaveApprovalWorkflowSteps.AsNoTracking().Where(item => item.LeaveApprovalWorkflowId == workflow.Id && item.Status == CatalogStatus.Active)
            .OrderBy(item => item.Sequence).Select(item => new WorkflowStepSnapshot(item.StepKey, item.Sequence, item.RequiredPermissionKey,
                item.ScopeSource, item.AllowsReturnForChanges, item.RequiresCommentOnApproval)).ToArrayAsync(cancellationToken);
        if (steps.Length == 0) return Result.Failure(HrErrors.Conflict);
        entity.Status = LeaveWorkflowStatus.PendingApproval;
        entity.SubmittedAtUtc = timeProvider.GetUtcNow();
        entity.ApprovalWorkflowId = workflow.Id;
        entity.ApprovalWorkflowVersion = workflow.Version;
        entity.ApprovalWorkflowSnapshotJson = JsonSerializer.Serialize(steps);
        entity.CurrentApprovalStepKey = steps[0].StepKey;
        entity.CurrentApprovalStepSequence = steps[0].Sequence;
        return Result.Success();
    }

    private async Task<Result> DecideLeave(LeaveRequest entity, LeaveDecisionType decision, string comment, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || currentUser.AuthorizationVersion is not { } version
            || entity.Status != LeaveWorkflowStatus.PendingApproval || entity.ApprovalWorkflowSnapshotJson is null)
            return Result.Failure(HrErrors.Conflict);
        var steps = JsonSerializer.Deserialize<WorkflowStepSnapshot[]>(entity.ApprovalWorkflowSnapshotJson) ?? [];
        var step = steps.SingleOrDefault(item => item.Sequence == entity.CurrentApprovalStepSequence);
        if (step is null || (decision != LeaveDecisionType.Approved || step.RequiresCommentOnApproval) && !HrServiceSupport.HasText(comment))
            return Result.Failure(HrErrors.InvalidRequest);
        var scope = await ResolveScope(entity, step.ScopeSource, cancellationToken);
        if (!await permissionChecker.HasPermissionAsync(userId, version, step.RequiredPermissionKey, scope, cancellationToken))
            return Result.Failure(new OperationError("leave.approval_forbidden", "The user does not have the permission or scope required by this workflow step.", ErrorType.Forbidden));
        var fromStatus = entity.Status;
        if (decision == LeaveDecisionType.Approved)
        {
            var next = steps.Where(item => item.Sequence > step.Sequence).OrderBy(item => item.Sequence).FirstOrDefault();
            if (next is null)
            {
                entity.Status = LeaveWorkflowStatus.Approved;
                entity.ApprovedAtUtc = timeProvider.GetUtcNow();
                entity.ApprovedByUserId = userId;
                entity.CurrentApprovalStepKey = null;
                entity.CurrentApprovalStepSequence = null;
            }
            else
            {
                entity.CurrentApprovalStepKey = next.StepKey;
                entity.CurrentApprovalStepSequence = next.Sequence;
            }
        }
        else if (decision == LeaveDecisionType.Rejected)
        {
            entity.Status = LeaveWorkflowStatus.Rejected;
            entity.RejectedAtUtc = timeProvider.GetUtcNow();
            entity.RejectedByUserId = userId;
            entity.RejectionReason = comment.Trim();
            entity.CurrentApprovalStepKey = null;
            entity.CurrentApprovalStepSequence = null;
        }
        else
        {
            if (!step.AllowsReturnForChanges) return Result.Failure(HrErrors.Conflict);
            entity.Status = LeaveWorkflowStatus.ReturnedForChanges;
            entity.CurrentApprovalStepKey = null;
            entity.CurrentApprovalStepSequence = null;
        }
        dbContext.LeaveApprovalDecisions.Add(new LeaveApprovalDecision
        {
            LeaveRequestId = entity.Id, StepKey = step.StepKey, StepSequence = step.Sequence,
            RequiredPermissionKey = step.RequiredPermissionKey, DecidedByUserId = userId,
            DecidedAtUtc = timeProvider.GetUtcNow(), Decision = decision, FromStatus = fromStatus,
            ToStatus = entity.Status, Comment = comment?.Trim() ?? string.Empty,
            AuthorizationSnapshotJson = JsonSerializer.Serialize(new { UserId = userId, AuthorizationVersion = version, Scope = scope })
        });
        return Result.Success();
    }

    private async Task<PermissionScope?> ResolveScope(LeaveRequest request, LeaveApprovalScopeSource source, CancellationToken cancellationToken) => source switch
    {
        LeaveApprovalScopeSource.EmployeeHousing => await dbContext.HousingResidencePeriods.Where(item => item.EmployeeId == request.EmployeeId && item.EffectiveTo == null)
            .Select(item => new PermissionScope(AccessScopeType.Housing, item.HousingId)).SingleOrDefaultAsync(cancellationToken),
        LeaveApprovalScopeSource.ActiveClientContract when request.RelatedClientContractId is not null => new PermissionScope(AccessScopeType.ClientContract, request.RelatedClientContractId.Value),
        LeaveApprovalScopeSource.ActiveClientPlatform => await (from assignment in dbContext.RiderClientAssignments
            join rider in dbContext.RiderProfiles on assignment.RiderProfileId equals rider.Id
            join contract in dbContext.ClientContracts on assignment.ClientContractId equals contract.Id
            where rider.EmployeeId == request.EmployeeId && assignment.EffectiveTo == null
            select new PermissionScope(AccessScopeType.ClientPlatform, contract.ClientPlatformId)).SingleOrDefaultAsync(cancellationToken),
        _ => null
    };

    private Result ActivateLeave(LeaveRequest item) { item.Status = LeaveWorkflowStatus.Active; item.ActivatedAtUtc = timeProvider.GetUtcNow(); return Result.Success(); }
    private Result CompleteLeave(LeaveRequest item) { item.Status = LeaveWorkflowStatus.Completed; item.CompletedAtUtc = timeProvider.GetUtcNow(); return Result.Success(); }
    private Result CancelLeave(LeaveRequest item, string comment, Guid userId)
    {
        if (!HrServiceSupport.HasText(comment)) return Result.Failure(HrErrors.InvalidRequest);
        item.Status = LeaveWorkflowStatus.Cancelled; item.CancelledAtUtc = timeProvider.GetUtcNow();
        item.CancelledByUserId = userId; item.CancellationReason = comment.Trim(); return Result.Success();
    }

    private EmployeeAbsenceComplianceCaseEvent CreateAbsenceEvent(EmployeeAbsenceComplianceCase entity,
        AbsenceCaseEventType type, Guid userId, string reason, string? before) => new()
    {
        EmployeeAbsenceComplianceCaseId = entity.Id, EventType = type, OccurredAtUtc = timeProvider.GetUtcNow(),
        ActorUserId = userId, Reason = reason, BeforeJson = before,
        AfterJson = JsonSerializer.Serialize(new { entity.CurrentPath, entity.Status, entity.RemovalDeadline }),
        CorrelationId = currentUser.CorrelationId ?? Guid.CreateVersion7().ToString()
    };

    private static bool ValidateWorkflow(LeaveWorkflowUpsertRequest request, out CatalogStatus status, out EmployeeRelationshipType? relationship)
    {
        status = default; relationship = null;
        if (!HrServiceSupport.HasText(request.Code) || !HrServiceSupport.HasText(request.NameAr) || !HrServiceSupport.HasText(request.NameEn)
            || request.Version <= 0 || request.EffectiveTo is not null && request.EffectiveTo < request.EffectiveFrom
            || !TryParseEnum(request.Status, out status) || request.Steps.Count == 0
            || request.Steps.Select(item => item.Sequence).Distinct().Count() != request.Steps.Count
            || request.Steps.Select(item => HrServiceSupport.NormalizeCode(item.StepKey)).Distinct().Count() != request.Steps.Count
            || request.Steps.Any(item => item.Sequence <= 0 || !PermissionKeys.All.Contains(item.RequiredPermissionKey)
                || !Enum.TryParse<LeaveApprovalScopeSource>(item.ScopeSource, true, out _)
                || item.TargetResponseHours is <= 0)) return false;
        if (request.RelationshipType is not null)
        {
            if (!TryParseEnum<EmployeeRelationshipType>(request.RelationshipType, out var parsed)) return false;
            relationship = parsed;
        }
        return true;
    }

    private static bool ValidateLeaveRequest(LeaveRequestUpsertRequest request, out int days)
    {
        days = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        return request.EndDate >= request.StartDate && request.ExpectedReturnDate >= request.EndDate
            && days > 0 && HrServiceSupport.HasText(request.Reason)
            && request.DestinationCountryCode?.Trim().Length is not > 2;
    }

    private static bool ValidateAbsencePath(AbsenceCaseUpsertRequest request, AbsenceCasePath path) => path switch
    {
        AbsenceCasePath.ReportedToAuthorities => request.ReportedToAuthoritiesDate is not null
            && request.ExitOrOutageDate is null && request.RemovalDeadline >= request.ReportedToAuthoritiesDate,
        AbsenceCasePath.ExitOrSystemOutage => request.ExitOrOutageDate is not null
            && request.ReportedToAuthoritiesDate is null && request.RemovalDeadline >= request.ExitOrOutageDate,
        _ => false
    };

    private static string NewNumber(string prefix) => $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.CreateVersion7():N}"[..20].ToUpperInvariant();
    private static bool TryParseEnum<TEnum>(string? value, out TEnum parsed) where TEnum : struct, Enum => Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed);
    private static LeaveTypeResponse ToLeaveType(LeaveType item) => new(item.Id, item.Code, item.NameAr, item.NameEn,
        item.DescriptionAr, item.DescriptionEn, item.RequiresBalance, item.RequiresHrDocuments,
        item.RequiresExitReentryVisa, item.MaximumCalendarDays, item.Status.ToString(), HrServiceSupport.EncodeRowVersion(item.RowVersion));
    private static LeaveWorkflowResponse ToWorkflow(LeaveApprovalWorkflow item, IEnumerable<LeaveApprovalWorkflowStep> steps) =>
        new(item.Id, item.Code, item.NameAr, item.NameEn, item.Version, item.LeaveTypeId, item.RelationshipType?.ToString(),
            item.AppliesToRider, item.ClientPlatformId, item.Priority, item.EffectiveFrom, item.EffectiveTo,
            item.Status.ToString(), steps.OrderBy(step => step.Sequence).Select(step => new LeaveWorkflowStepResponse(step.Id,
                step.StepKey, step.Sequence, step.NameAr, step.NameEn, step.RequiredPermissionKey, step.ScopeSource.ToString(),
                step.AllowsReturnForChanges, step.RequiresCommentOnApproval, step.TargetResponseHours)).ToArray(),
            HrServiceSupport.EncodeRowVersion(item.RowVersion));
    private static LeaveRequestResponse ToLeave(LeaveProjection row) => new(row.Item.Id, row.Item.RequestNumber,
        row.Item.EmployeeId, row.EmployeeNameAr, row.Item.LeaveTypeId, row.LeaveTypeNameAr, row.Item.StartDate,
        row.Item.EndDate, row.Item.ExpectedReturnDate, row.Item.CalendarDays, row.Item.Reason, row.Item.Status.ToString(),
        row.Item.HrStatus.ToString(), row.Item.ApprovalWorkflowId, row.Item.CurrentApprovalStepKey,
        row.Item.CurrentApprovalStepSequence, row.Item.SubmittedAtUtc, row.Item.ApprovedAtUtc, row.Item.ActivatedAtUtc,
        row.Item.CompletedAtUtc, row.Item.RejectionReason, row.Item.CancellationReason, row.Item.RelatedClientContractId,
        row.Item.Notes, HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));
    private static LeaveDateChangeResponse ToDateChange(LeaveDateChangeRequest item) => new(
        item.Id, item.LeaveRequestId, item.PreviousStartDate, item.PreviousEndDate,
        item.RequestedStartDate, item.RequestedEndDate, item.Reason, item.Status.ToString(),
        item.RequestedByUserId, item.RequestedAtUtc, item.ResolvedByUserId, item.ResolvedAtUtc,
        item.ResolutionReason, HrServiceSupport.EncodeRowVersion(item.RowVersion));
    private static LeaveCancellationResponse ToCancellation(LeaveCancellationRequest item) => new(
        item.Id, item.LeaveRequestId, item.Reason, item.PreviousLeaveStatus?.ToString(), item.Status.ToString(),
        item.RequestedByUserId, item.RequestedAtUtc, item.ResolvedByUserId, item.ResolvedAtUtc,
        item.ResolutionReason, HrServiceSupport.EncodeRowVersion(item.RowVersion));
    private static AbsenceCaseResponse ToAbsence(AbsenceProjection row) => new(row.Item.Id, row.Item.CaseNumber,
        row.Item.EmployeeId, row.EmployeeNameAr, row.Item.AbsenceDate, row.Item.CurrentPath.ToString(), row.Item.Status.ToString(),
        row.Item.ReportedToAuthoritiesDate, row.Item.AuthorityReportReference, row.Item.ExitOrOutageDate,
        row.Item.ExitVisaNumber, row.Item.RemovalDeadline, row.Item.Notes, row.Item.ResolvedAtUtc,
        row.Item.ResolutionCode, row.Item.ResolutionNotes, row.Item.ClosedAtUtc, HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));
    private static EmployeeStatusChangeResponse ToStatusChange(StatusChangeProjection row) => new(row.Item.Id,
        row.Item.RequestNumber, row.Item.EmployeeId, row.EmployeeNameAr, row.Item.FromStatus.ToString(),
        row.Item.RequestedStatus.ToString(), row.Item.EffectiveFrom, row.Item.Reason, row.Item.Status.ToString(),
        row.Item.RequestedAtUtc, row.Item.ResolvedByUserId, row.Item.ResolvedAtUtc, row.Item.ResolutionReason,
        row.Item.ResultingWorkHistoryId, HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));

    private sealed record WorkflowStepSnapshot(string StepKey, int Sequence, string RequiredPermissionKey,
        LeaveApprovalScopeSource ScopeSource, bool AllowsReturnForChanges, bool RequiresCommentOnApproval);
    private sealed record LeaveProjection(LeaveRequest Item, string EmployeeNameAr, string LeaveTypeNameAr);
    private sealed record AbsenceProjection(EmployeeAbsenceComplianceCase Item, string EmployeeNameAr);
    private sealed record StatusChangeProjection(EmployeeStatusChangeRequest Item, string EmployeeNameAr);
}
