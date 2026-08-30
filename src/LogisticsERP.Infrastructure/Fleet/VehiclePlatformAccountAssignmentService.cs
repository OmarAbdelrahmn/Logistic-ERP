using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed class VehiclePlatformAccountAssignmentService(
    ApplicationDbContext dbContext,
    FleetServiceSupport support) : IVehiclePlatformAccountAssignmentService
{
    public async Task<Result<IReadOnlyList<VehiclePlatformAccountAssignmentResponse>>> GetAssignmentsAsync(
        Guid? vehicleId,
        Guid? platformRiderAccountId,
        Guid? platformId,
        Guid? operatingCityId,
        Guid? sponsorId,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsRead, null, cancellationToken))
            return Result.Failure<IReadOnlyList<VehiclePlatformAccountAssignmentResponse>>(FleetErrors.Forbidden);

        var query = CreateProjectionQuery(activeOnly, newestFirst: true);
        query = ApplyFilters(query, vehicleId, platformRiderAccountId, platformId, operatingCityId, sponsorId);
        var rows = await query.ToArrayAsync(cancellationToken);
        var problems = await LoadActiveProblemsAsync(cancellationToken);

        return Result.Success<IReadOnlyList<VehiclePlatformAccountAssignmentResponse>>(
            rows.Select(row => ToResponse(row, problems.GetValueOrDefault(row.Assignment.Id))).ToArray());
    }

    public async Task<Result<VehiclePlatformAccountAssignmentResponse>> GetAssignmentAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsRead, null, cancellationToken))
            return Result.Failure<VehiclePlatformAccountAssignmentResponse>(FleetErrors.Forbidden);

        var row = await CreateProjectionQuery(false, assignmentId)
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
            return Result.Failure<VehiclePlatformAccountAssignmentResponse>(FleetErrors.NotFound);

        var problems = await LoadActiveProblemsAsync(cancellationToken);
        return Result.Success(ToResponse(row, problems.GetValueOrDefault(assignmentId)));
    }

    public async Task<Result<VehiclePlatformAccountAssignmentResponse>> ApproveAsync(
        ApproveVehiclePlatformAccountAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsManage, null, cancellationToken))
            return Result.Failure<VehiclePlatformAccountAssignmentResponse>(FleetErrors.Forbidden);
        if (support.UserId is not { } userId)
            return Result.Failure<VehiclePlatformAccountAssignmentResponse>(FleetErrors.CurrentUserUnavailable);
        if (request.VehicleId == Guid.Empty
            || request.PlatformRiderAccountId == Guid.Empty
            || FleetServiceSupport.TrimOrNull(request.Reason) is { Length: > 1000 })
            return Result.Failure<VehiclePlatformAccountAssignmentResponse>(FleetErrors.InvalidRequest);

        var vehicleExists = await dbContext.Vehicles.IgnoreQueryFilters()
            .AnyAsync(item => item.Id == request.VehicleId, cancellationToken);
        var accountExists = await dbContext.PlatformRiderAccounts.IgnoreQueryFilters()
            .AnyAsync(item => item.Id == request.PlatformRiderAccountId, cancellationToken);
        if (!vehicleExists || !accountExists)
            return Result.Failure<VehiclePlatformAccountAssignmentResponse>(FleetErrors.NotFound);

        var now = support.UtcNow;
        var assignment = new VehiclePlatformAccountAssignment
        {
            VehicleId = request.VehicleId,
            PlatformRiderAccountId = request.PlatformRiderAccountId,
            AssignedAtUtc = request.EffectiveFromUtc ?? now,
            AssignmentReason = FleetServiceSupport.TrimOrNull(request.Reason),
            ApprovalStatus = VehiclePlatformAccountApprovalStatus.Approved,
            ApprovedAtUtc = now,
            ApprovedByUserId = userId,
            Status = VehiclePlatformAccountAssignmentStatus.Active
        };
        dbContext.VehiclePlatformAccountAssignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var row = await CreateProjectionQuery(false, assignment.Id)
            .SingleAsync(cancellationToken);
        var problems = await LoadActiveProblemsAsync(cancellationToken);
        return Result.Success(ToResponse(row, problems.GetValueOrDefault(assignment.Id)));
    }

    public async Task<Result<VehiclePlatformAccountAssignmentResponse>> CloseAsync(
        Guid assignmentId,
        CloseVehiclePlatformAccountAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsManage, null, cancellationToken))
            return Result.Failure<VehiclePlatformAccountAssignmentResponse>(FleetErrors.Forbidden);
        if (support.UserId is not { } userId)
            return Result.Failure<VehiclePlatformAccountAssignmentResponse>(FleetErrors.CurrentUserUnavailable);
        if (assignmentId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.Reason)
            || request.Reason.Trim().Length > 1000)
            return Result.Failure<VehiclePlatformAccountAssignmentResponse>(FleetErrors.InvalidRequest);

        var assignment = await dbContext.VehiclePlatformAccountAssignments
            .SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);
        if (assignment is null)
            return Result.Failure<VehiclePlatformAccountAssignmentResponse>(FleetErrors.NotFound);
        if (!FleetServiceSupport.MatchesRowVersion(assignment.RowVersion, request.RowVersion))
            return Result.Failure<VehiclePlatformAccountAssignmentResponse>(FleetErrors.ConcurrencyConflict);

        var endedAtUtc = request.EffectiveToUtc ?? support.UtcNow;
        if (assignment.Status != VehiclePlatformAccountAssignmentStatus.Active
            || assignment.EndedAtUtc is not null
            || endedAtUtc < assignment.AssignedAtUtc)
            return Result.Failure<VehiclePlatformAccountAssignmentResponse>(FleetErrors.InvalidState);

        assignment.Status = VehiclePlatformAccountAssignmentStatus.Ended;
        assignment.EndedAtUtc = endedAtUtc;
        assignment.EndedByUserId = userId;
        assignment.EndReason = request.Reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        var row = await CreateProjectionQuery(false, assignment.Id)
            .SingleAsync(cancellationToken);
        return Result.Success(ToResponse(row, null));
    }

    public async Task<Result<IReadOnlyList<VehiclePlatformAccountSwitchResponse>>> GetSwitchesAsync(
        bool pendingOnly,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsRead, null, cancellationToken))
            return Result.Failure<IReadOnlyList<VehiclePlatformAccountSwitchResponse>>(FleetErrors.Forbidden);

        var query = CreateSwitchProjectionQuery(
            status: pendingOnly ? VehiclePlatformAccountSwitchStatus.Pending : null,
            orderByRequestedAt: true);

        var rows = await query.ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<VehiclePlatformAccountSwitchResponse>>(
            rows.Select(ToSwitchResponse).ToArray());
    }

    public async Task<Result<VehiclePlatformAccountSwitchResponse>> GetSwitchAsync(
        Guid switchId,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsRead, null, cancellationToken))
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.Forbidden);

        var row = await CreateSwitchProjectionQuery(switchId).SingleOrDefaultAsync(cancellationToken);
        return row is null
            ? Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.NotFound)
            : Result.Success(ToSwitchResponse(row));
    }

    public async Task<Result<VehiclePlatformAccountSwitchResponse>> SwitchAsync(
        Guid assignmentId,
        SwitchVehiclePlatformAccountAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsManage, null, cancellationToken))
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.Forbidden);
        if (support.UserId is not { } userId)
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.CurrentUserUnavailable);
        if (assignmentId == Guid.Empty
            || request.TargetVehicleId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.Reason)
            || request.Reason.Trim().Length > 1000
            || !Enum.TryParse(request.Mode, true, out VehiclePlatformAccountSwitchMode mode)
            || !Enum.IsDefined(mode)
            || mode == VehiclePlatformAccountSwitchMode.Pending && request.EffectiveAtUtc.HasValue)
        {
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.InvalidRequest);
        }

        var source = await dbContext.VehiclePlatformAccountAssignments
            .SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);
        if (source is null)
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.NotFound);
        if (!FleetServiceSupport.MatchesRowVersion(source.RowVersion, request.RowVersion))
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.ConcurrencyConflict);
        if (source.Status != VehiclePlatformAccountAssignmentStatus.Active
            || source.EndedAtUtc is not null
            || source.VehicleId == request.TargetVehicleId)
        {
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.InvalidState);
        }

        var targetExists = await dbContext.Vehicles.IgnoreQueryFilters()
            .AnyAsync(item => item.Id == request.TargetVehicleId, cancellationToken);
        if (!targetExists)
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.NotFound);

        if (await dbContext.VehiclePlatformAccountSwitches.AnyAsync(
                item => item.SourceAssignmentId == assignmentId
                    && item.Status == VehiclePlatformAccountSwitchStatus.Pending,
                cancellationToken))
        {
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.InvalidState);
        }

        var now = support.UtcNow;
        var reason = request.Reason.Trim();
        var switchEntity = new VehiclePlatformAccountSwitch
        {
            Id = Guid.CreateVersion7(),
            SourceAssignmentId = source.Id,
            SourceVehicleId = source.VehicleId,
            TargetVehicleId = request.TargetVehicleId,
            PlatformRiderAccountId = source.PlatformRiderAccountId,
            Mode = mode,
            Status = mode == VehiclePlatformAccountSwitchMode.Immediate
                ? VehiclePlatformAccountSwitchStatus.Accepted
                : VehiclePlatformAccountSwitchStatus.Pending,
            Reason = reason,
            RequestedAtUtc = now,
            RequestedByUserId = userId
        };

        if (mode == VehiclePlatformAccountSwitchMode.Immediate)
        {
            var effectiveAtUtc = request.EffectiveAtUtc ?? now;
            if (effectiveAtUtc < source.AssignedAtUtc || effectiveAtUtc > now)
                return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.InvalidState);

            var newAssignment = ApplyVehicleSwitch(source, request.TargetVehicleId, effectiveAtUtc, now, userId, reason);
            dbContext.VehiclePlatformAccountAssignments.Add(newAssignment);
            switchEntity.EffectiveAtUtc = effectiveAtUtc;
            switchEntity.AcceptedAtUtc = now;
            switchEntity.AcceptedByUserId = userId;
            switchEntity.NewAssignmentId = newAssignment.Id;
        }

        dbContext.VehiclePlatformAccountSwitches.Add(switchEntity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.ConcurrencyConflict);
        }

        var row = await CreateSwitchProjectionQuery(switchEntity.Id).SingleAsync(cancellationToken);
        return Result.Success(ToSwitchResponse(row));
    }

    public async Task<Result<VehiclePlatformAccountSwitchResponse>> AcceptSwitchAsync(
        Guid switchId,
        AcceptVehiclePlatformAccountSwitchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsManage, null, cancellationToken))
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.Forbidden);
        if (support.UserId is not { } userId)
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.CurrentUserUnavailable);
        if (switchId == Guid.Empty)
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.InvalidRequest);

        var switchEntity = await dbContext.VehiclePlatformAccountSwitches
            .SingleOrDefaultAsync(item => item.Id == switchId, cancellationToken);
        if (switchEntity is null)
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.NotFound);
        if (!FleetServiceSupport.MatchesRowVersion(switchEntity.RowVersion, request.RowVersion))
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.ConcurrencyConflict);
        if (switchEntity.Mode != VehiclePlatformAccountSwitchMode.Pending
            || switchEntity.Status != VehiclePlatformAccountSwitchStatus.Pending)
        {
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.InvalidState);
        }

        var source = await dbContext.VehiclePlatformAccountAssignments
            .SingleOrDefaultAsync(item => item.Id == switchEntity.SourceAssignmentId, cancellationToken);
        if (source is null
            || source.Status != VehiclePlatformAccountAssignmentStatus.Active
            || source.EndedAtUtc is not null
            || source.VehicleId != switchEntity.SourceVehicleId
            || source.PlatformRiderAccountId != switchEntity.PlatformRiderAccountId)
        {
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.InvalidState);
        }

        var targetExists = await dbContext.Vehicles.IgnoreQueryFilters()
            .AnyAsync(item => item.Id == switchEntity.TargetVehicleId, cancellationToken);
        if (!targetExists)
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.NotFound);

        var now = support.UtcNow;
        var effectiveAtUtc = request.EffectiveAtUtc ?? now;
        if (effectiveAtUtc < source.AssignedAtUtc || effectiveAtUtc > now)
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.InvalidState);

        var newAssignment = ApplyVehicleSwitch(
            source,
            switchEntity.TargetVehicleId,
            effectiveAtUtc,
            now,
            userId,
            switchEntity.Reason);
        dbContext.VehiclePlatformAccountAssignments.Add(newAssignment);
        switchEntity.Status = VehiclePlatformAccountSwitchStatus.Accepted;
        switchEntity.EffectiveAtUtc = effectiveAtUtc;
        switchEntity.AcceptedAtUtc = now;
        switchEntity.AcceptedByUserId = userId;
        switchEntity.NewAssignmentId = newAssignment.Id;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<VehiclePlatformAccountSwitchResponse>(FleetErrors.ConcurrencyConflict);
        }

        var row = await CreateSwitchProjectionQuery(switchEntity.Id).SingleAsync(cancellationToken);
        return Result.Success(ToSwitchResponse(row));
    }

    public async Task<Result<IReadOnlyList<VehiclePlatformAccountAssignmentResponse>>> GetProblemsAsync(
        Guid? vehicleId,
        Guid? platformRiderAccountId,
        Guid? platformId,
        Guid? operatingCityId,
        Guid? sponsorId,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.AssignmentsRead, null, cancellationToken))
            return Result.Failure<IReadOnlyList<VehiclePlatformAccountAssignmentResponse>>(FleetErrors.Forbidden);

        var activeRows = await CreateProjectionQuery(true).ToArrayAsync(cancellationToken);
        var problems = BuildProblems(activeRows);
        var filtered = ApplyFilters(
                activeRows.AsQueryable(),
                vehicleId,
                platformRiderAccountId,
                platformId,
                operatingCityId,
                sponsorId)
            .Where(row => problems.ContainsKey(row.Assignment.Id))
            .OrderByDescending(row => row.Assignment.ApprovedAtUtc)
            .ThenByDescending(row => row.Assignment.Id)
            .Select(row => ToResponse(row, problems[row.Assignment.Id]))
            .ToArray();

        return Result.Success<IReadOnlyList<VehiclePlatformAccountAssignmentResponse>>(filtered);
    }

    private static VehiclePlatformAccountAssignment ApplyVehicleSwitch(
        VehiclePlatformAccountAssignment source,
        Guid targetVehicleId,
        DateTimeOffset effectiveAtUtc,
        DateTimeOffset acceptedAtUtc,
        Guid userId,
        string reason)
    {
        source.Status = VehiclePlatformAccountAssignmentStatus.Ended;
        source.EndedAtUtc = effectiveAtUtc;
        source.EndedByUserId = userId;
        source.EndReason = reason;

        return new VehiclePlatformAccountAssignment
        {
            Id = Guid.CreateVersion7(),
            VehicleId = targetVehicleId,
            PlatformRiderAccountId = source.PlatformRiderAccountId,
            AssignedAtUtc = effectiveAtUtc,
            AssignmentReason = reason,
            ApprovalStatus = VehiclePlatformAccountApprovalStatus.Approved,
            ApprovedAtUtc = acceptedAtUtc,
            ApprovedByUserId = userId,
            Status = VehiclePlatformAccountAssignmentStatus.Active
        };
    }

    private IQueryable<SwitchProjection> CreateSwitchProjectionQuery(
        Guid? switchId = null,
        VehiclePlatformAccountSwitchStatus? status = null,
        bool orderByRequestedAt = false)
    {
        var switches = dbContext.VehiclePlatformAccountSwitches.AsNoTracking();
        if (switchId.HasValue)
            switches = switches.Where(item => item.Id == switchId.Value);
        if (status.HasValue)
            switches = switches.Where(item => item.Status == status.Value);
        if (orderByRequestedAt)
            switches = switches.OrderBy(item => item.RequestedAtUtc).ThenBy(item => item.Id);

        var vehicles = dbContext.Vehicles.IgnoreQueryFilters().AsNoTracking();
        var accounts = dbContext.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking();
        var currentRegistrations = dbContext.VehicleRegistrations.AsNoTracking()
            .Where(item => item.IsCurrent);
        return from item in switches
               join sourceVehicle in vehicles on item.SourceVehicleId equals sourceVehicle.Id
               join targetVehicle in vehicles on item.TargetVehicleId equals targetVehicle.Id
               join account in accounts on item.PlatformRiderAccountId equals account.Id
               join sourceRegistrationRow in currentRegistrations on sourceVehicle.Id equals sourceRegistrationRow.VehicleId into sourceRegistrations
               from sourceRegistration in sourceRegistrations.DefaultIfEmpty()
               join targetRegistrationRow in currentRegistrations on targetVehicle.Id equals targetRegistrationRow.VehicleId into targetRegistrations
               from targetRegistration in targetRegistrations.DefaultIfEmpty()
               select new SwitchProjection(
                   item,
                   sourceVehicle,
                   targetVehicle,
                   account,
                   sourceRegistration == null ? null : sourceRegistration.RegistrationNumber,
                   targetRegistration == null ? null : targetRegistration.RegistrationNumber);
    }

    private static VehiclePlatformAccountSwitchResponse ToSwitchResponse(SwitchProjection row) => new(
        row.Switch.Id,
        row.Switch.SourceAssignmentId,
        row.Switch.SourceVehicleId,
        row.SourceVehicle.AssetNumber,
        row.SourceVehicleRegistrationNumber,
        row.SourceVehicle.PlateNumberAr,
        row.SourceVehicle.PlateNumberEn,
        row.Switch.TargetVehicleId,
        row.TargetVehicle.AssetNumber,
        row.TargetVehicleRegistrationNumber,
        row.TargetVehicle.PlateNumberAr,
        row.TargetVehicle.PlateNumberEn,
        row.Switch.PlatformRiderAccountId,
        row.Account.Code,
        row.Switch.Mode.ToString(),
        row.Switch.Status.ToString(),
        row.Switch.Reason,
        row.Switch.RequestedAtUtc,
        row.Switch.RequestedByUserId,
        row.Switch.EffectiveAtUtc,
        row.Switch.AcceptedAtUtc,
        row.Switch.AcceptedByUserId,
        row.Switch.NewAssignmentId,
        FleetServiceSupport.EncodeRowVersion(row.Switch.RowVersion));

    private async Task<Dictionary<Guid, IReadOnlyList<VehiclePlatformAssignmentProblemResponse>>> LoadActiveProblemsAsync(
        CancellationToken cancellationToken)
    {
        var activeRows = await CreateProjectionQuery(true).ToArrayAsync(cancellationToken);
        return BuildProblems(activeRows);
    }

    private static Dictionary<Guid, IReadOnlyList<VehiclePlatformAssignmentProblemResponse>> BuildProblems(
        AssignmentProjection[] rows)
    {
        var problems = rows.ToDictionary(
            row => row.Assignment.Id,
            _ => new List<VehiclePlatformAssignmentProblemResponse>());

        foreach (var row in rows)
        {
            if (row.Vehicle.IsDeleted)
                Add(problems, row, "VehicleArchived", "The vehicle is archived, but the assignment was approved.", "Active vehicle", "Archived vehicle");
            if (row.Account.IsDeleted)
                Add(problems, row, "PlatformAccountArchived", "The platform account is archived, but the assignment was approved.", "Non-archived account", "Archived account");
            if (!VehiclePlatformAccountAssignmentPolicy.IsOperationalStatusAllowed(row.Vehicle.CurrentOperationalStatus))
                Add(problems, row, "VehicleOperationalStatus", "The vehicle operational status is not suitable for platform-account use.", "Available or Assigned", row.Vehicle.CurrentOperationalStatus.ToString());
            if (!VehiclePlatformAccountAssignmentPolicy.IsPlatformAccountStatusAllowed(row.Account.Status))
                Add(problems, row, "PlatformAccountStatus", "The platform account is not available for operational use.", "Available or Assigned", row.Account.Status.ToString());

            var maximum = VehiclePlatformAccountAssignmentPolicy.GetMaximumAccounts(row.Vehicle.VehicleType);
            if (!maximum.HasValue)
                Add(problems, row, "UnsupportedVehicleType", "No platform-account capacity rule is configured for this vehicle type.", "Car or Motorcycle", row.Vehicle.VehicleType.ToString());

            if (row.Vehicle.SponsorId is null)
                Add(problems, row, "VehicleSponsorMissing", "The vehicle has no sponsor while the platform account has a required sponsor.", row.Account.SponsorId.ToString(), null);
            else if (row.Vehicle.SponsorId != row.Account.SponsorId)
                Add(problems, row, "SponsorMismatch", "The vehicle and platform account belong to different sponsors.", row.Vehicle.SponsorId.ToString(), row.Account.SponsorId.ToString());

            if (row.Vehicle.OperatingCityId is null)
                Add(problems, row, "VehicleCityMissing", "The vehicle has no operating city while the platform account has a required city.", row.Account.OperatingCityId.ToString(), null);
            else if (row.Vehicle.OperatingCityId != row.Account.OperatingCityId)
                Add(problems, row, "OperatingCityMismatch", "The vehicle and platform account are assigned to different operating cities.", row.Vehicle.OperatingCityId.ToString(), row.Account.OperatingCityId.ToString());
        }

        foreach (var duplicateGroup in rows.GroupBy(row => new
                 {
                     row.Assignment.VehicleId,
                     row.Assignment.PlatformRiderAccountId
                 }))
        {
            foreach (var duplicate in duplicateGroup
                         .OrderBy(row => row.Assignment.ApprovedAtUtc)
                         .ThenBy(row => row.Assignment.Id)
                         .Skip(1))
            {
                Add(problems, duplicate, "DuplicateActiveAssignment", "This vehicle is already actively linked to the same platform account.", "One active link per vehicle and account", $"{duplicateGroup.Count()} active links");
            }
        }

        foreach (var capacityGroup in rows.GroupBy(row => new
                 {
                     row.Assignment.VehicleId,
                     row.Account.ClientPlatformId,
                     row.Account.OperatingCityId
                 }))
        {
            var orderedAccounts = capacityGroup
                .GroupBy(row => row.Account.Id)
                .Select(group => group
                    .OrderBy(row => row.Assignment.ApprovedAtUtc)
                    .ThenBy(row => row.Assignment.Id)
                    .First())
                .OrderBy(row => row.Assignment.ApprovedAtUtc)
                .ThenBy(row => row.Assignment.Id)
                .ToArray();
            var maximum = VehiclePlatformAccountAssignmentPolicy.GetMaximumAccounts(orderedAccounts[0].Vehicle.VehicleType);
            if (!maximum.HasValue || orderedAccounts.Length <= maximum.Value)
                continue;

            var overflowAccountIds = orderedAccounts
                .Skip(maximum.Value)
                .Select(row => row.Account.Id)
                .ToHashSet();
            foreach (var overflow in capacityGroup.Where(row => overflowAccountIds.Contains(row.Account.Id)))
            {
                Add(
                    problems,
                    overflow,
                    "PlatformCityCapacityExceeded",
                    "The vehicle exceeds its active account limit for this platform and operating city.",
                    $"Maximum {maximum.Value}",
                    $"{orderedAccounts.Length} distinct active accounts",
                    maximum.Value,
                    orderedAccounts.Length);
            }
        }

        return problems
            .Where(pair => pair.Value.Count > 0)
            .ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<VehiclePlatformAssignmentProblemResponse>)pair.Value.ToArray());
    }

    private static void Add(
        Dictionary<Guid, List<VehiclePlatformAssignmentProblemResponse>> problems,
        AssignmentProjection row,
        string code,
        string message,
        string? expected,
        string? actual,
        int? maximumAccounts = null,
        int? activeAccountCount = null) =>
        problems[row.Assignment.Id].Add(new VehiclePlatformAssignmentProblemResponse(
            code,
            "Warning",
            message,
            expected,
            actual,
            maximumAccounts,
            activeAccountCount));

    private IQueryable<AssignmentProjection> CreateProjectionQuery(
        bool activeOnly,
        Guid? assignmentId = null,
        bool newestFirst = false)
    {
        var assignments = dbContext.VehiclePlatformAccountAssignments.AsNoTracking();
        if (assignmentId.HasValue)
            assignments = assignments.Where(item => item.Id == assignmentId.Value);

        var vehicles = dbContext.Vehicles.IgnoreQueryFilters().AsNoTracking();
        var accounts = dbContext.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking();
        var platforms = dbContext.ClientPlatforms.IgnoreQueryFilters().AsNoTracking();
        var sponsors = dbContext.Sponsors.IgnoreQueryFilters().AsNoTracking();
        var operatingCities = dbContext.OperatingCities.IgnoreQueryFilters().AsNoTracking();
        var cities = dbContext.GlobalCities.IgnoreQueryFilters().AsNoTracking();
        var employees = dbContext.Employees.IgnoreQueryFilters().AsNoTracking();
        var currentRegistrations = dbContext.VehicleRegistrations.AsNoTracking()
            .Where(item => item.IsCurrent);

        var joinedQuery = from assignment in assignments
                          join vehicle in vehicles on assignment.VehicleId equals vehicle.Id
                          join account in accounts on assignment.PlatformRiderAccountId equals account.Id
                          join platform in platforms on account.ClientPlatformId equals platform.Id
                          join accountSponsor in sponsors on account.SponsorId equals accountSponsor.Id
                          join accountOperatingCity in operatingCities on account.OperatingCityId equals accountOperatingCity.Id
                          join accountCity in cities on accountOperatingCity.GlobalCityId equals accountCity.Id
                          join vehicleSponsorRow in sponsors on vehicle.SponsorId equals (Guid?)vehicleSponsorRow.Id into vehicleSponsors
                          from vehicleSponsor in vehicleSponsors.DefaultIfEmpty()
                          join vehicleOperatingCityRow in operatingCities on vehicle.OperatingCityId equals (Guid?)vehicleOperatingCityRow.Id into vehicleOperatingCities
                          from vehicleOperatingCity in vehicleOperatingCities.DefaultIfEmpty()
                          join vehicleCityRow in cities on (Guid?)vehicleOperatingCity.GlobalCityId equals (Guid?)vehicleCityRow.Id into vehicleCities
                          from vehicleCity in vehicleCities.DefaultIfEmpty()
                           join ownerEmployeeRow in employees on account.RegisteredEmployeeId equals (Guid?)ownerEmployeeRow.Id into ownerEmployees
                           from ownerEmployee in ownerEmployees.DefaultIfEmpty()
                           join registrationRow in currentRegistrations on vehicle.Id equals registrationRow.VehicleId into registrations
                           from registration in registrations.DefaultIfEmpty()
                           where !activeOnly || assignment.Status == VehiclePlatformAccountAssignmentStatus.Active && assignment.EndedAtUtc == null
                          select new
                          {
                              Assignment = assignment,
                              Vehicle = vehicle,
                              Account = account,
                              PlatformCode = platform.Code,
                              PlatformNameAr = platform.NameAr,
                              AccountSponsorNameAr = accountSponsor.RegistryNameAr,
                              AccountCityNameAr = accountCity.NameAr,
                               VehicleSponsorNameAr = vehicleSponsor == null ? null : vehicleSponsor.RegistryNameAr,
                               VehicleCityNameAr = vehicleCity == null ? null : vehicleCity.NameAr,
                               AccountOwnerNameAr = ownerEmployee == null ? null : ownerEmployee.FullNameAr,
                               VehicleRegistrationNumber = registration == null ? null : registration.RegistrationNumber
                          };

        if (newestFirst)
        {
            joinedQuery = joinedQuery
                .OrderByDescending(row => row.Assignment.ApprovedAtUtc)
                .ThenByDescending(row => row.Assignment.Id);
        }

        return joinedQuery.Select(row => new AssignmentProjection(
            row.Assignment,
            row.Vehicle,
            row.Account,
            row.PlatformCode,
            row.PlatformNameAr,
            row.AccountSponsorNameAr,
            row.AccountCityNameAr,
            row.VehicleSponsorNameAr,
            row.VehicleCityNameAr,
            row.AccountOwnerNameAr,
            row.VehicleRegistrationNumber));
    }

    private static IQueryable<AssignmentProjection> ApplyFilters(
        IQueryable<AssignmentProjection> query,
        Guid? vehicleId,
        Guid? platformRiderAccountId,
        Guid? platformId,
        Guid? operatingCityId,
        Guid? sponsorId)
    {
        if (vehicleId.HasValue)
            query = query.Where(row => row.Assignment.VehicleId == vehicleId.Value);
        if (platformRiderAccountId.HasValue)
            query = query.Where(row => row.Assignment.PlatformRiderAccountId == platformRiderAccountId.Value);
        if (platformId.HasValue)
            query = query.Where(row => row.Account.ClientPlatformId == platformId.Value);
        if (operatingCityId.HasValue)
            query = query.Where(row => row.Account.OperatingCityId == operatingCityId.Value);
        if (sponsorId.HasValue)
            query = query.Where(row => row.Account.SponsorId == sponsorId.Value);
        return query;
    }

    private static VehiclePlatformAccountAssignmentResponse ToResponse(
        AssignmentProjection row,
        IReadOnlyList<VehiclePlatformAssignmentProblemResponse>? problems)
    {
        problems ??= [];
        return new VehiclePlatformAccountAssignmentResponse(
            row.Assignment.Id,
            row.Vehicle.Id,
            row.Vehicle.AssetNumber,
            row.VehicleRegistrationNumber,
            row.Vehicle.PlateNumberAr,
            row.Vehicle.PlateNumberEn,
            row.Vehicle.VehicleType.ToString(),
            row.Vehicle.CurrentOperationalStatus.ToString(),
            row.Vehicle.SponsorId,
            row.VehicleSponsorNameAr,
            row.Vehicle.OperatingCityId,
            row.VehicleCityNameAr,
            row.Account.Id,
            row.Account.Code,
            row.Account.ExternalAccountId,
            row.Account.Status.ToString(),
            row.Account.ClientPlatformId,
            row.PlatformCode,
            row.PlatformNameAr,
            row.Account.SponsorId,
            row.AccountSponsorNameAr,
            row.Account.OperatingCityId,
            row.AccountCityNameAr,
            row.Account.RegisteredEmployeeId,
            row.AccountOwnerNameAr,
            row.Assignment.AssignedAtUtc,
            row.Assignment.AssignmentReason,
            row.Assignment.ApprovalStatus.ToString(),
            row.Assignment.ApprovedAtUtc,
            row.Assignment.ApprovedByUserId,
            row.Assignment.Status.ToString(),
            row.Assignment.EndedAtUtc,
            row.Assignment.EndedByUserId,
            row.Assignment.EndReason,
            problems.Count > 0,
            problems,
            FleetServiceSupport.EncodeRowVersion(row.Assignment.RowVersion));
    }

    private sealed record AssignmentProjection(
        VehiclePlatformAccountAssignment Assignment,
        Vehicle Vehicle,
        PlatformRiderAccount Account,
        string PlatformCode,
        string PlatformNameAr,
        string AccountSponsorNameAr,
        string AccountCityNameAr,
        string? VehicleSponsorNameAr,
        string? VehicleCityNameAr,
        string? AccountOwnerNameAr,
        string? VehicleRegistrationNumber);

    private sealed record SwitchProjection(
        VehiclePlatformAccountSwitch Switch,
        Vehicle SourceVehicle,
        Vehicle TargetVehicle,
        PlatformRiderAccount Account,
        string? SourceVehicleRegistrationNumber,
        string? TargetVehicleRegistrationNumber);
}
