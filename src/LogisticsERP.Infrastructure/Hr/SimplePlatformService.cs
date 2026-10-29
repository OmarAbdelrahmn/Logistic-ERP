using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Chefz;
using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Platform;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class SimplePlatformService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IPlatformCredentialProtector credentialProtector) : ISimplePlatformService
{
    public async Task<Result<IReadOnlyList<SimplePlatformResponse>>> GetPlatformsAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var query = includeArchived
            ? dbContext.ClientPlatforms.IgnoreQueryFilters().AsNoTracking()
            : dbContext.ClientPlatforms.AsNoTracking();
        var rows = await query.OrderBy(item => item.NameAr).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<SimplePlatformResponse>>(rows.Select(ToPlatform).ToArray());
    }

    public Task<Result<SimplePlatformResponse>> CreatePlatformAsync(
        SimplePlatformUpsertRequest request,
        CancellationToken cancellationToken = default) =>
        UpsertPlatformAsync(null, request, cancellationToken);

    public Task<Result<SimplePlatformResponse>> UpdatePlatformAsync(
        Guid id,
        SimplePlatformUpsertRequest request,
        CancellationToken cancellationToken = default) =>
        UpsertPlatformAsync(id, request, cancellationToken);

    public async Task<Result<IReadOnlyList<SimplePlatformAccountResponse>>> GetAccountsAsync(
        Guid? accountId,
        Guid? platformId,
        Guid? operatingCityId,
        Guid? sponsorId,
        Guid? ownerRiderProfileId,
        Guid? actualRiderProfileId,
        string? status,
        string? paymentModel,
        bool currentOnly,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        PlatformRiderAccountStatus? parsedStatus = null;
        if (HrServiceSupport.HasText(status))
        {
            if (!TryParseEnum(status, out PlatformRiderAccountStatus value))
            {
                return Result.Failure<IReadOnlyList<SimplePlatformAccountResponse>>(HrErrors.InvalidRequest);
            }

            parsedStatus = value;
        }

        PlatformAccountPaymentModel? parsedPaymentModel = null;
        if (HrServiceSupport.HasText(paymentModel))
        {
            if (!TryParseEnum(paymentModel, out PlatformAccountPaymentModel value))
            {
                return Result.Failure<IReadOnlyList<SimplePlatformAccountResponse>>(HrErrors.InvalidRequest);
            }

            parsedPaymentModel = value;
        }

        var accountQuery = includeArchived
            ? dbContext.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking()
            : dbContext.PlatformRiderAccounts.AsNoTracking();

        if (accountId is not null)
        {
            accountQuery = accountQuery.Where(item => item.Id == accountId);
        }

        if (platformId is not null)
        {
            accountQuery = accountQuery.Where(item => item.ClientPlatformId == platformId);
        }

        if (operatingCityId is not null)
        {
            accountQuery = accountQuery.Where(item => item.OperatingCityId == operatingCityId);
        }

        if (sponsorId is not null)
        {
            accountQuery = accountQuery.Where(item => item.SponsorId == sponsorId);
        }

        if (parsedStatus is not null)
        {
            accountQuery = accountQuery.Where(item => item.Status == parsedStatus);
        }

        if (parsedPaymentModel is not null)
        {
            accountQuery = accountQuery.Where(item => item.PaymentModel == parsedPaymentModel);
        }

        if (ownerRiderProfileId is not null)
        {
            accountQuery = accountQuery.Where(account => dbContext.RiderProfiles.Any(
                rider => rider.Id == ownerRiderProfileId && account.RegisteredEmployeeId == rider.EmployeeId));
        }

        if (actualRiderProfileId is not null)
        {
            accountQuery = accountQuery.Where(account => dbContext.RiderClientAssignments.Any(assignment =>
                assignment.PlatformRiderAccountId == account.Id
                && assignment.RiderProfileId == actualRiderProfileId
                && (!currentOnly || assignment.EffectiveTo == null)));
        }
        else if (currentOnly)
        {
            accountQuery = accountQuery.Where(account => dbContext.RiderClientAssignments.Any(assignment =>
                assignment.PlatformRiderAccountId == account.Id && assignment.EffectiveTo == null));
        }

        var projections = await CreateAccountProjectionQuery(accountQuery, includeArchived)
            .ToArrayAsync(cancellationToken);
        if (projections.Length == 0)
        {
            return Result.Success<IReadOnlyList<SimplePlatformAccountResponse>>([]);
        }

        var accountIds = projections.Select(item => item.Account.Id).ToArray();
        var ownerEmployeeIds = projections
            .Select(item => item.Account.RegisteredEmployeeId)
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        var owners = await LoadOwnersAsync(ownerEmployeeIds, cancellationToken);
        var currentAssignments = await LoadAssignmentsAsync(accountIds, true, null, cancellationToken);
        var currentByAccount = currentAssignments
            .GroupBy(item => item.AccountId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.EffectiveFrom).First());
        var response = projections.Select(projection => ToAccount(
            projection,
            projection.Account.RegisteredEmployeeId is { } employeeId && owners.TryGetValue(employeeId, out var owner) ? owner : null,
            currentByAccount.GetValueOrDefault(projection.Account.Id))).ToArray();
        return Result.Success<IReadOnlyList<SimplePlatformAccountResponse>>(response);
    }

    public Task<Result<SimplePlatformAccountResponse>> GetAccountAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        LoadAccountAsync(id, false, cancellationToken);

    public async Task<Result<SimplePlatformAccountResponse>> CreateAccountAsync(
        SimplePlatformAccountUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAccountRequestAsync(null, request, false, cancellationToken);
        if (validation.IsFailure)
        {
            return Result.Failure<SimplePlatformAccountResponse>(validation.Error);
        }

        var status = ParseEnum<PlatformRiderAccountStatus>(request.Status);
        var entity = new PlatformRiderAccount
        {
            ClientPlatformId = request.PlatformId,
            RegisteredEmployeeId = validation.Value,
            OperatingCityId = request.OperatingCityId,
            SponsorId = request.SponsorId,
            Code = HrServiceSupport.NormalizeCode(request.Code),
            ExternalAccountId = request.ExternalAccountId.Trim(),
            UserName = HrServiceSupport.TrimOrNull(request.UserName),
            PaymentModel = ParseEnum<PlatformAccountPaymentModel>(request.PaymentModel),
            Status = status,
            StatusReason = HrServiceSupport.TrimOrNull(request.StatusReason),
            AcquisitionDate = request.AcquisitionDate,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            OperationalNotes = HrServiceSupport.TrimOrNull(request.Notes)
        };
        dbContext.PlatformRiderAccounts.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Failure<SimplePlatformAccountResponse>(HrErrors.Duplicate);
        }

        return await LoadAccountAsync(entity.Id, false, cancellationToken);
    }

    public async Task<Result<SimplePlatformAccountResponse>> UpdateAccountAsync(
        Guid id,
        SimplePlatformAccountUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PlatformRiderAccounts.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<SimplePlatformAccountResponse>(HrErrors.NotFound);
        }

        if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
        {
            return Result.Failure<SimplePlatformAccountResponse>(HrErrors.ConcurrencyConflict);
        }

        var validation = await ValidateAccountRequestAsync(id, request, true, cancellationToken);
        if (validation.IsFailure)
        {
            return Result.Failure<SimplePlatformAccountResponse>(validation.Error);
        }

        var status = ParseEnum<PlatformRiderAccountStatus>(request.Status);
        var paymentModel = ParseEnum<PlatformAccountPaymentModel>(request.PaymentModel);
        var isNinja = await dbContext.ClientPlatforms.AnyAsync(p => p.Id == entity.ClientPlatformId && p.Code == "NINJA", cancellationToken);
        if (isNinja && (status != entity.Status || entity.ClientPlatformId != request.PlatformId
            || HrServiceSupport.TrimOrNull(request.StatusReason) != entity.StatusReason))
            return Result.Failure<SimplePlatformAccountResponse>(LogisticsERP.Application.Features.Ninja.NinjaErrors.UseNinjaWorkflow);
        if (await dbContext.Set<LogisticsERP.Domain.Entities.Jahez.JahezAccountSession>().AnyAsync(x => x.PlatformRiderAccountId == entity.Id, cancellationToken)
            && (entity.ClientPlatformId != request.PlatformId || entity.ExternalAccountId != request.ExternalAccountId.Trim()))
            return Result.Failure<SimplePlatformAccountResponse>(JahezErrors.Conflict("لا يمكن تغيير منصة أو رقم حساب له تاريخ مالي في جاهز."));

        var activeAssignment = await dbContext.RiderClientAssignments.SingleOrDefaultAsync(
            item => item.PlatformRiderAccountId == id && item.EffectiveTo == null,
            cancellationToken);
        var hasActiveAssignment = activeAssignment is not null;
        if (isNinja && activeAssignment is not null && paymentModel != activeAssignment.PaymentModel)
            return Result.Failure<SimplePlatformAccountResponse>(LogisticsERP.Application.Features.Ninja.NinjaErrors.UseNinjaWorkflow);
        var isChefz = await dbContext.ClientPlatforms.AnyAsync(p => p.Id == request.PlatformId
            && (p.Code == "CHEFZ" || p.Code == "THECHEFZ" || p.Code == "CHEFSZ" || p.Code == "SHIFTZ"), cancellationToken);
        if (hasActiveAssignment && status != PlatformRiderAccountStatus.Assigned
                && !((isChefz || isNinja) && ChefzAccountStatusRules.IsManual(status))
            || !hasActiveAssignment && status == PlatformRiderAccountStatus.Assigned)
        {
            return Result.Failure<SimplePlatformAccountResponse>(HrErrors.Conflict);
        }

        if (hasActiveAssignment
            && (entity.ClientPlatformId != request.PlatformId
                || entity.RegisteredEmployeeId != validation.Value
                || entity.OperatingCityId != request.OperatingCityId
                || entity.SponsorId != request.SponsorId))
        {
            return Result.Failure<SimplePlatformAccountResponse>(HrErrors.Conflict);
        }

        if (activeAssignment is not null && entity.PaymentModel != paymentModel)
        {
            var otherAssignments = await PlatformAssignmentPolicySupport.CurrentAssignments(dbContext, activeAssignment.RiderProfileId, activeAssignment.Id)
                .ToArrayAsync(cancellationToken);
            var platformCode = await PlatformAssignmentPolicySupport.PlatformCodeAsync(dbContext, entity.Id, cancellationToken);
            var decision = PlatformAssignmentPolicySupport.Evaluate(platformCode, otherAssignments, paymentModel);
            if (decision != PlatformAccountAssignmentDecision.Allowed)
            {
                return Result.Failure<SimplePlatformAccountResponse>(ToAssignmentError(decision));
            }

            activeAssignment.PaymentModel = paymentModel;
        }

        if (status == PlatformRiderAccountStatus.Archived && !isChefz && !isNinja)
        {
            if (hasActiveAssignment || !HrServiceSupport.HasText(request.ArchiveReason))
            {
                return Result.Failure<SimplePlatformAccountResponse>(HrErrors.Conflict);
            }

            entity.IsDeleted = true;
            entity.DeletionReason = request.ArchiveReason!.Trim();
        }

        entity.ClientPlatformId = request.PlatformId;
        entity.RegisteredEmployeeId = validation.Value;
        entity.OperatingCityId = request.OperatingCityId;
        entity.SponsorId = request.SponsorId;
        entity.Code = HrServiceSupport.NormalizeCode(request.Code);
        entity.ExternalAccountId = request.ExternalAccountId.Trim();
        entity.UserName = HrServiceSupport.TrimOrNull(request.UserName);
        entity.PaymentModel = paymentModel;
        entity.Status = status;
        entity.StatusReason = HrServiceSupport.TrimOrNull(request.StatusReason);
        entity.AcquisitionDate = request.AcquisitionDate;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.OperationalNotes = HrServiceSupport.TrimOrNull(request.Notes);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Failure<SimplePlatformAccountResponse>(HrErrors.Duplicate);
        }

        return await LoadAccountAsync(entity.Id, entity.IsDeleted, cancellationToken);
    }

    public async Task<Result<SimplePlatformAssignmentResponse>> AssignAccountAsync(
        Guid accountId,
        AssignSimplePlatformAccountRequest request,
        CancellationToken cancellationToken = default) =>
        await AssignAccountCoreAsync(accountId, request, null, cancellationToken);

    internal Task<Result<SimplePlatformAssignmentResponse>> AssignNinjaAccountAsync(Guid accountId,
        AssignSimplePlatformAccountRequest request, NinjaRiderType riderType, PlatformAccountPaymentModel paymentModel,
        Action stageCredentials, CancellationToken ct) =>
        AssignAccountCoreAsync(accountId, request, stageCredentials, ct, riderType, paymentModel);

    private async Task<Result<SimplePlatformAssignmentResponse>> AssignAccountCoreAsync(Guid accountId,
        AssignSimplePlatformAccountRequest request, Action? stageCredentials, CancellationToken cancellationToken,
        NinjaRiderType? ninjaRiderType = null, PlatformAccountPaymentModel? ninjaPaymentModel = null)
    {
        if (currentUser.UserId is not { } userId
            || request.ActualRiderProfileId == Guid.Empty
            || request.WasBackdated && !HrServiceSupport.HasText(request.BackdatedReason))
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.InvalidRequest);
        }

        await using var accountScope = await PlatformAccountWriteScope.AcquireAsync(dbContext, [accountId], cancellationToken);
        var account = await dbContext.PlatformRiderAccounts.SingleOrDefaultAsync(
            item => item.Id == accountId,
            cancellationToken);
        if (account is not null && await dbContext.ClientPlatforms.AnyAsync(x => x.Id == account.ClientPlatformId && x.Code == "JAHEZ", cancellationToken))
            return Result.Failure<SimplePlatformAssignmentResponse>(JahezErrors.UseJahezWorkflow);

        if (stageCredentials is null && account is not null && await dbContext.ClientPlatforms.AnyAsync(x => x.Id == account.ClientPlatformId && x.Code == "NINJA", cancellationToken))
            return Result.Failure<SimplePlatformAssignmentResponse>(LogisticsERP.Application.Features.Ninja.NinjaErrors.UseNinjaWorkflow);

        if (account is null)
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.PlatformAccountNotFound(accountId));
        }

        var paymentModel = ninjaPaymentModel ?? account.PaymentModel;
        if (ninjaPaymentModel is not null)
        {
            var supported = await dbContext.ClientPlatforms.Where(p => p.Id == account.ClientPlatformId)
                .Select(p => p.SupportedPaymentModels).SingleAsync(cancellationToken);
            if (!IsPaymentModelSupported(supported, paymentModel))
                return Result.Failure<SimplePlatformAssignmentResponse>(PlatformAccountErrors.UnsupportedPaymentModel);
        }

        if (await RiderOperationalBlock.IsBlocked(dbContext, request.ActualRiderProfileId, account.ClientPlatformId, cancellationToken))
            return Result.Failure<SimplePlatformAssignmentResponse>(RiderOperationalBlock.Error);
        if (account.Status != PlatformRiderAccountStatus.Available
            || await dbContext.RiderClientAssignments.AnyAsync(
                item => item.EffectiveTo == null && item.PlatformRiderAccountId == accountId,
                cancellationToken))
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(
                HrErrors.PlatformAccountUnavailable(accountId, account.Status));
        }

        var actualRider = await LoadRiderAsync(request.ActualRiderProfileId, true, cancellationToken);
        if (actualRider is null)
        {
            var riderExists = await dbContext.RiderProfiles.AsNoTracking()
                .AnyAsync(item => item.Id == request.ActualRiderProfileId, cancellationToken);
            return Result.Failure<SimplePlatformAssignmentResponse>(riderExists
                ? HrErrors.RiderProfileUnavailable("actualRiderProfileId", request.ActualRiderProfileId)
                : HrErrors.RiderProfileNotFound("actualRiderProfileId", request.ActualRiderProfileId));
        }

        var activeAssignments = await PlatformAssignmentPolicySupport.CurrentAssignments(dbContext, request.ActualRiderProfileId)
            .ToArrayAsync(cancellationToken);
        var platformCode = await PlatformAssignmentPolicySupport.PlatformCodeAsync(dbContext, account.Id, cancellationToken);
        var assignmentDecision = PlatformAssignmentPolicySupport.Evaluate(platformCode, activeAssignments, paymentModel);
        if (assignmentDecision != PlatformAccountAssignmentDecision.Allowed)
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(
                await CreateAssignmentLimitErrorAsync(
                    assignmentDecision,
                    request.ActualRiderProfileId,
                    account.Id,
                    paymentModel,
                    cancellationToken));
        }
        var riderAccountSlot = PlatformAssignmentPolicySupport.NextSlot(activeAssignments);

        var dayAllocation = new PlatformAccountDayAllocationService(dbContext);
        var validDates = await dayAllocation.ValidateStart(accountId, request.EffectiveFrom, null, null, cancellationToken);
        if (validDates.IsFailure) return Result.Failure<SimplePlatformAssignmentResponse>(validDates.Error);

        OwnerProjection? owner;
        if (account.RegisteredEmployeeId is { } ownerEmployeeId)
        {
            owner = await LoadOwnerAsync(ownerEmployeeId, cancellationToken);
            if (owner is null)
            {
                return Result.Failure<SimplePlatformAssignmentResponse>(
                    HrErrors.PlatformAccountOwnerNotFound(account.Id, ownerEmployeeId));
            }
            if (owner.RiderProfileId is null)
            {
                return Result.Failure<SimplePlatformAssignmentResponse>(
                    PlatformAccountErrors.OwnerProfileRequired(account.Id, ownerEmployeeId));
            }
        }
        else
        {
            owner = actualRider;
            account.RegisteredEmployeeId = actualRider.EmployeeId;
        }

        var previous = await dayAllocation.PrepareAssignment(accountId, request.EffectiveFrom, request.ActualRiderProfileId, request.PreviousDayUsages, request.Reason ?? "Account assigned", cancellationToken);
        if (previous.IsFailure) { dbContext.ChangeTracker.Clear(); return Result.Failure<SimplePlatformAssignmentResponse>(previous.Error); }
        stageCredentials?.Invoke();
        var contract = await GetOrCreateInternalContractAsync(account.ClientPlatformId, account.Id, cancellationToken);
        await EnsureInternalRegistrationAsync(account, owner, contract.Id, cancellationToken);

        var assignment = new RiderClientAssignment
        {
            RiderProfileId = actualRider.RiderProfileId!.Value,
            ClientContractId = contract.Id,
            PlatformRiderAccountId = account.Id,
            PaymentModel = paymentModel,
            RiderAccountSlot = riderAccountSlot,
            NinjaRiderType = ninjaRiderType,
            EffectiveFrom = request.EffectiveFrom,
            Status = RiderAssignmentStatus.Active,
            StartReason = HrServiceSupport.TrimOrNull(request.Reason),
            AssignedByUserId = userId,
            WasBackdated = request.WasBackdated,
            BackdatedReason = HrServiceSupport.TrimOrNull(request.BackdatedReason)
        };
        dbContext.RiderClientAssignments.Add(assignment);
        var stagedUsage = await dayAllocation.Stage(accountId, assignment.Id, request.EffectiveFrom, null, request.Reason ?? "Account assigned", true, cancellationToken);
        if (stagedUsage.IsFailure) { dbContext.ChangeTracker.Clear(); return Result.Failure<SimplePlatformAssignmentResponse>(stagedUsage.Error); }
        await dayAllocation.LinkContinuation(accountId, request.EffectiveFrom, request.Reason ?? "Account assigned", cancellationToken);
        dbContext.RiderAssignmentEvents.Add(new RiderAssignmentEvent
        {
            RiderClientAssignmentId = assignment.Id,
            FromStatus = RiderAssignmentStatus.Planned,
            ToStatus = RiderAssignmentStatus.Active,
            OccurredAtUtc = timeProvider.GetUtcNow(),
            ActorUserId = userId,
            Reason = request.Reason?.Trim() ?? "Platform account assigned.",
            ChangeSnapshotJson = JsonSerializer.Serialize(new
            {
                AccountId = account.Id,
                OwnerRiderProfileId = owner.RiderProfileId,
                ActualRiderProfileId = actualRider.RiderProfileId,
                RiderType = assignment.NinjaRiderType?.ToString(),
                PaymentModel = assignment.PaymentModel.ToString(),
                request.EffectiveFrom
            })
        });
        account.Status = PlatformRiderAccountStatus.Assigned;
        account.PaymentModel = paymentModel;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.Conflict);
        }

        return Result.Success(ToAssignment(new AssignmentProjection(
            assignment,
            actualRider.EmployeeId,
            actualRider.NameAr,
            actualRider.NameEn)));
    }

    public async Task<Result<SimplePlatformAssignmentResponse>> ReleaseAccountAsync(
        Guid accountId,
        ReleaseSimplePlatformAccountRequest request,
        CancellationToken cancellationToken = default) =>
        await ReleaseAccountCoreAsync(accountId, request, null, cancellationToken);

    internal Task<Result<SimplePlatformAssignmentResponse>> ReleaseNinjaAccountAsync(Guid accountId,
        ReleaseSimplePlatformAccountRequest request, Action stageCredentials, CancellationToken ct) =>
        ReleaseAccountCoreAsync(accountId, request, stageCredentials, ct);

    private async Task<Result<SimplePlatformAssignmentResponse>> ReleaseAccountCoreAsync(Guid accountId,
        ReleaseSimplePlatformAccountRequest request, Action? stageCredentials, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId
            || !TryParseEnum(request.Status, out RiderAssignmentStatus status)
            || status is not (RiderAssignmentStatus.Ended or RiderAssignmentStatus.Cancelled)
            || !HrServiceSupport.HasText(request.Reason))
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.InvalidRequest);
        }

        await using var accountScope = await PlatformAccountWriteScope.AcquireAsync(dbContext, [accountId], cancellationToken);
        var account = await dbContext.PlatformRiderAccounts.SingleOrDefaultAsync(
            item => item.Id == accountId,
            cancellationToken);
        var assignment = await dbContext.RiderClientAssignments.SingleOrDefaultAsync(
            item => item.PlatformRiderAccountId == accountId && item.EffectiveTo == null,
            cancellationToken);
        if (account is not null && await dbContext.ClientPlatforms.AnyAsync(x => x.Id == account.ClientPlatformId && x.Code == "JAHEZ", cancellationToken))
            return Result.Failure<SimplePlatformAssignmentResponse>(JahezErrors.UseJahezWorkflow);

        if (stageCredentials is null && account is not null && await dbContext.ClientPlatforms.AnyAsync(x => x.Id == account.ClientPlatformId && x.Code == "NINJA", cancellationToken))
            return Result.Failure<SimplePlatformAssignmentResponse>(LogisticsERP.Application.Features.Ninja.NinjaErrors.UseNinjaWorkflow);

        if (account is null || assignment is null)
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.NotFound);
        }

        if (!HrServiceSupport.MatchesRowVersion(assignment.RowVersion, request.RowVersion))
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.ConcurrencyConflict);
        }

        if (request.EffectiveTo < assignment.EffectiveFrom)
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.Conflict);
        }

        var rider = await LoadRiderAsync(assignment.RiderProfileId, false, cancellationToken);
        if (rider is null)
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.NotFound);
        }

        var dayAllocation = new PlatformAccountDayAllocationService(dbContext);
        var validEnd = await dayAllocation.ValidateEnd(assignment, request.EffectiveTo, status == RiderAssignmentStatus.Cancelled, cancellationToken);
        if (validEnd.IsFailure) return Result.Failure<SimplePlatformAssignmentResponse>(validEnd.Error);
        var stagedUsage = await dayAllocation.Stage(accountId, assignment.Id, request.EffectiveTo, request.DayUsage, request.Reason, false, cancellationToken);
        if (stagedUsage.IsFailure) { dbContext.ChangeTracker.Clear(); return Result.Failure<SimplePlatformAssignmentResponse>(stagedUsage.Error); }
        stageCredentials?.Invoke();
        var fromStatus = assignment.Status;
        assignment.EffectiveTo = request.EffectiveTo;
        assignment.Status = status;
        assignment.EndReason = request.Reason.Trim();
        assignment.EndedByUserId = userId;
        dbContext.RiderAssignmentEvents.Add(new RiderAssignmentEvent
        {
            RiderClientAssignmentId = assignment.Id,
            FromStatus = fromStatus,
            ToStatus = status,
            OccurredAtUtc = timeProvider.GetUtcNow(),
            ActorUserId = userId,
            Reason = request.Reason.Trim(),
            ChangeSnapshotJson = JsonSerializer.Serialize(new { request.EffectiveTo, Status = status.ToString() })
        });
        var isChefz = await dbContext.ClientPlatforms.AnyAsync(p => p.Id == account.ClientPlatformId
            && (p.Code == "CHEFZ" || p.Code == "THECHEFZ" || p.Code == "CHEFSZ" || p.Code == "SHIFTZ"), cancellationToken);
        account.Status = stageCredentials is not null ? LogisticsERP.Domain.Ninja.NinjaAccountStatusRules.AfterRelease(account.Status)
            : isChefz ? ChefzAccountStatusRules.AfterRelease(account.Status) : PlatformRiderAccountStatus.Available;
        if (isChefz) dbContext.Entry(account).Property(a => a.Status).IsModified = true;
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.ConcurrencyConflict);
        }
        return Result.Success(ToAssignment(new AssignmentProjection(
            assignment,
            rider.EmployeeId,
            rider.NameAr,
            rider.NameEn)));
    }

    public async Task<Result<SimplePlatformCredentialVersionResponse>> RotateCredentialAsync(
        Guid accountId,
        RotateSimplePlatformCredentialRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId
            || !HrServiceSupport.HasText(request.Secret)
            || request.Secret.Length > 4096
            || !HrServiceSupport.HasText(request.Reason)
            || request.Reason.Trim().Length > 1000)
        {
            return Result.Failure<SimplePlatformCredentialVersionResponse>(HrErrors.InvalidRequest);
        }

        if (!await dbContext.PlatformRiderAccounts.AnyAsync(item => item.Id == accountId, cancellationToken))
        {
            return Result.Failure<SimplePlatformCredentialVersionResponse>(HrErrors.NotFound);
        }

        if (await (from a in dbContext.PlatformRiderAccounts join p in dbContext.ClientPlatforms on a.ClientPlatformId equals p.Id
            where a.Id == accountId && p.Code == "NINJA" select a.Id).AnyAsync(cancellationToken))
            return Result.Failure<SimplePlatformCredentialVersionResponse>(LogisticsERP.Application.Features.Ninja.NinjaErrors.UseNinjaWorkflow);

        var latest = await dbContext.PlatformAccountCredentialVersions
            .Where(item => item.PlatformRiderAccountId == accountId)
            .OrderByDescending(item => item.KeyVersion)
            .FirstOrDefaultAsync(cancellationToken);
        var protectedValue = credentialProtector.Protect(request.Secret);
        var version = new PlatformAccountCredentialVersion
        {
            PlatformRiderAccountId = accountId,
            Ciphertext = protectedValue.Ciphertext,
            Nonce = protectedValue.Nonce,
            AuthenticationTag = protectedValue.AuthenticationTag,
            KeyVersion = (latest?.KeyVersion ?? 0) + 1,
            RotatedAtUtc = timeProvider.GetUtcNow(),
            RotatedByUserId = userId,
            RotationReason = request.Reason.Trim(),
            SupersededVersionId = latest?.Id
        };
        dbContext.PlatformAccountCredentialVersions.Add(version);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Failure<SimplePlatformCredentialVersionResponse>(HrErrors.ConcurrencyConflict);
        }

        return Result.Success(ToCredential(version));
    }

    public async Task<Result<RiderPlatformHistoryResponse>> GetRiderPlatformHistoryAsync(
        Guid riderProfileId,
        CancellationToken cancellationToken = default)
    {
        var rider = await LoadRiderAsync(riderProfileId, false, cancellationToken);
        if (rider is null)
        {
            return Result.Failure<RiderPlatformHistoryResponse>(HrErrors.NotFound);
        }

        var rows = await (from assignment in dbContext.RiderClientAssignments.AsNoTracking()
                          join account in dbContext.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking()
                              on assignment.PlatformRiderAccountId equals account.Id
                          join platform in dbContext.ClientPlatforms.IgnoreQueryFilters().AsNoTracking()
                              on account.ClientPlatformId equals platform.Id
                          join ownerProfile in dbContext.RiderProfiles.IgnoreQueryFilters().AsNoTracking()
                              on account.RegisteredEmployeeId equals ownerProfile.EmployeeId into ownerProfiles
                          from ownerProfile in ownerProfiles.DefaultIfEmpty()
                          join ownerEmployee in dbContext.Employees.IgnoreQueryFilters().AsNoTracking()
                              on account.RegisteredEmployeeId equals ownerEmployee.Id into ownerEmployees
                          from ownerEmployee in ownerEmployees.DefaultIfEmpty()
                          where assignment.RiderProfileId == riderProfileId
                          orderby assignment.EffectiveFrom descending
                          select new RiderPlatformHistoryItemResponse(
                              assignment.Id,
                              platform.Id,
                              platform.Code,
                              platform.NameAr,
                              platform.NameEn,
                              account.Id,
                              account.Code,
                              account.ExternalAccountId,
                              assignment.PaymentModel.ToString(),
                              ownerProfile == null ? null : ownerProfile.Id,
                              ownerEmployee == null ? null : ownerEmployee.FullNameAr,
                              ownerEmployee == null ? null : ownerEmployee.FullNameEn,
                              assignment.EffectiveFrom,
                              assignment.EffectiveTo,
                              assignment.Status.ToString(),
                              assignment.StartReason,
                              assignment.EndReason,
                              assignment.WasBackdated,
                              assignment.BackdatedReason))
            .ToArrayAsync(cancellationToken);

        return Result.Success(new RiderPlatformHistoryResponse(
            rider.RiderProfileId!.Value,
            rider.EmployeeId,
            rider.NameAr,
            rider.NameEn,
            rows));
    }

    public async Task<Result<IReadOnlyList<SimplePlatformAssignmentResponse>>> GetAccountAssignmentHistoryAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.PlatformRiderAccounts.IgnoreQueryFilters().AnyAsync(
            item => item.Id == accountId,
            cancellationToken))
        {
            return Result.Failure<IReadOnlyList<SimplePlatformAssignmentResponse>>(HrErrors.NotFound);
        }

        var assignments = await LoadAssignmentsAsync([accountId], false, null, cancellationToken);
        return Result.Success<IReadOnlyList<SimplePlatformAssignmentResponse>>(assignments);
    }

    public async Task<Result<IReadOnlyList<SimplePlatformCredentialVersionResponse>>> GetCredentialHistoryAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.PlatformRiderAccounts.IgnoreQueryFilters().AnyAsync(
            item => item.Id == accountId,
            cancellationToken))
        {
            return Result.Failure<IReadOnlyList<SimplePlatformCredentialVersionResponse>>(HrErrors.NotFound);
        }

        var credentials = await dbContext.PlatformAccountCredentialVersions.AsNoTracking()
            .Where(item => item.PlatformRiderAccountId == accountId)
            .OrderByDescending(item => item.KeyVersion)
            .Select(item => new SimplePlatformCredentialVersionResponse(
                item.Id,
                item.KeyVersion,
                item.RotatedAtUtc,
                item.RotatedByUserId,
                item.RotationReason))
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<SimplePlatformCredentialVersionResponse>>(credentials);
    }

    private async Task<Result<SimplePlatformResponse>> UpsertPlatformAsync(
        Guid? id,
        SimplePlatformUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!HrServiceSupport.HasText(request.Code)
            || !HrServiceSupport.HasText(request.NameAr)
            || !HrServiceSupport.HasText(request.NameEn)
            || !TryParseSupportedPaymentModels(request.SupportedPaymentModels, out var supportedPaymentModels)
            || !TryParseEnum(request.Status, out CatalogStatus status)
            || id is null && status == CatalogStatus.Archived)
        {
            return Result.Failure<SimplePlatformResponse>(HrErrors.InvalidRequest);
        }

        ClientPlatform entity;
        if (id is null)
        {
            entity = new ClientPlatform();
            dbContext.ClientPlatforms.Add(entity);
        }
        else
        {
            entity = await dbContext.ClientPlatforms.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null)
            {
                return Result.Failure<SimplePlatformResponse>(HrErrors.NotFound);
            }

            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            {
                return Result.Failure<SimplePlatformResponse>(HrErrors.ConcurrencyConflict);
            }
        }

        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.ClientPlatforms.IgnoreQueryFilters().AnyAsync(
            item => item.Id != entity.Id && item.Code == code,
            cancellationToken))
        {
            return Result.Failure<SimplePlatformResponse>(HrErrors.Duplicate);
        }

        if (status == CatalogStatus.Archived)
        {
            if (!HrServiceSupport.HasText(request.ArchiveReason)
                || await dbContext.PlatformRiderAccounts.AnyAsync(
                    item => item.ClientPlatformId == entity.Id,
                    cancellationToken))
            {
                return Result.Failure<SimplePlatformResponse>(HrErrors.Conflict);
            }

            entity.IsDeleted = true;
            entity.DeletionReason = request.ArchiveReason!.Trim();
        }

        else if (id is not null && await dbContext.PlatformRiderAccounts.AnyAsync(
            item => item.ClientPlatformId == entity.Id
                && (item.PaymentModel == PlatformAccountPaymentModel.PayPerOrder
                        && (supportedPaymentModels & SupportedPlatformPaymentModels.PayPerOrder) == 0
                    || item.PaymentModel == PlatformAccountPaymentModel.Salary
                        && (supportedPaymentModels & SupportedPlatformPaymentModels.Salary) == 0),
            cancellationToken))
        {
            return Result.Failure<SimplePlatformResponse>(HrErrors.PlatformPaymentModelsInUse);
        }

        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = request.NameEn.Trim();
        entity.SupportedPaymentModels = supportedPaymentModels;
        entity.Status = status;
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Failure<SimplePlatformResponse>(HrErrors.Duplicate);
        }

        return Result.Success(ToPlatform(entity));
    }

    private async Task<Result<Guid>> ValidateAccountRequestAsync(
        Guid? accountId,
        SimplePlatformAccountUpsertRequest request,
        bool isUpdate,
        CancellationToken cancellationToken)
    {
        if (request.PlatformId == Guid.Empty)
            return Result.Failure<Guid>(PlatformAccountErrors.Required("platformId", "المنصة"));
        if (request.OperatingCityId == Guid.Empty)
            return Result.Failure<Guid>(PlatformAccountErrors.Required("operatingCityId", "مدينة التشغيل"));
        if (request.SponsorId == Guid.Empty)
            return Result.Failure<Guid>(PlatformAccountErrors.Required("sponsorId", "الكفيل"));
        if (request.OwnerRiderProfileId == Guid.Empty)
            return Result.Failure<Guid>(PlatformAccountErrors.Required("ownerRiderProfileId", "مالك الحساب (الرايدر)"));
        if (!HrServiceSupport.HasText(request.Code))
            return Result.Failure<Guid>(PlatformAccountErrors.Required("code", "رمز الحساب"));
        if (!HrServiceSupport.HasText(request.ExternalAccountId))
            return Result.Failure<Guid>(PlatformAccountErrors.Required("externalAccountId", "رقم الحساب الخارجي"));
        if (!TryParseEnum(request.PaymentModel, out PlatformAccountPaymentModel paymentModel))
            return Result.Failure<Guid>(PlatformAccountErrors.Invalid("paymentModel", "نموذج الدفع غير صالح. القيم المسموحة: PayPerOrder أو Salary."));
        if (!TryParseEnum(request.Status, out PlatformRiderAccountStatus status))
            return Result.Failure<Guid>(PlatformAccountErrors.Invalid("status", "حالة الحساب غير صالحة. القيم المسموحة: Available أو Assigned أو Suspended أو Retired أو Archived."));
        if (request.EndDate is not null && request.StartDate is not null && request.EndDate < request.StartDate)
            return Result.Failure<Guid>(PlatformAccountErrors.Invalid("endDate", "تاريخ نهاية الحساب يجب أن يكون في تاريخ البداية أو بعده."));
        if (!isUpdate && status is PlatformRiderAccountStatus.Assigned or PlatformRiderAccountStatus.Archived)
            return Result.Failure<Guid>(PlatformAccountErrors.Invalid("status", "لا يمكن إنشاء حساب بحالة Assigned أو Archived. أنشئه بحالة Available أو Suspended أو Retired، ثم استخدم إجراء الإسناد عند الحاجة."));

        var textFields = new (string Field, string? Value, int Maximum, string Label)[]
        {
            ("code", HrServiceSupport.NormalizeCode(request.Code), 32, "رمز الحساب"),
            ("externalAccountId", request.ExternalAccountId, 150, "رقم الحساب الخارجي"),
            ("userName", request.UserName, 150, "اسم المستخدم"),
            ("statusReason", request.StatusReason, 500, "سبب الحالة"),
            ("notes", request.Notes, 4000, "الملاحظات")
        };
        foreach (var textField in textFields)
        {
            if (textField.Value?.Trim().Length > textField.Maximum)
                return Result.Failure<Guid>(PlatformAccountErrors.Invalid(textField.Field,
                    $"حقل {textField.Label} يجب ألا يتجاوز {textField.Maximum} حرفًا."));
        }

        var owner = await LoadRiderAsync(request.OwnerRiderProfileId, false, cancellationToken);
        if (owner is null)
            return Result.Failure<Guid>(PlatformAccountErrors.ReferenceUnavailable("ownerRiderProfileId", "مالك الحساب المحدد (يجب اختيار ملف رايدر أو ملف موظف مرتبط)"));
        var supportedPaymentModels = await dbContext.ClientPlatforms
            .Where(item => item.Id == request.PlatformId)
            .Select(item => (SupportedPlatformPaymentModels?)item.SupportedPaymentModels)
            .SingleOrDefaultAsync(cancellationToken);
        if (supportedPaymentModels is null)
            return Result.Failure<Guid>(PlatformAccountErrors.ReferenceUnavailable("platformId", "المنصة المحددة"));
        if (!await dbContext.OperatingCities.AnyAsync(item => item.Id == request.OperatingCityId, cancellationToken))
            return Result.Failure<Guid>(PlatformAccountErrors.ReferenceUnavailable("operatingCityId", "مدينة التشغيل المحددة"));
        if (!await dbContext.Sponsors.AnyAsync(
                item => item.Id == request.SponsorId && item.Status == CatalogStatus.Active,
                cancellationToken))
            return Result.Failure<Guid>(PlatformAccountErrors.ReferenceUnavailable("sponsorId", "الكفيل المحدد"));

        if (!IsPaymentModelSupported(supportedPaymentModels.Value, paymentModel))
        {
            return Result.Failure<Guid>(PlatformAccountErrors.UnsupportedPaymentModel);
        }

        var code = HrServiceSupport.NormalizeCode(request.Code);
        var externalAccountId = request.ExternalAccountId.Trim();
        var otherAccounts = dbContext.PlatformRiderAccounts.IgnoreQueryFilters().Where(item => item.Id != accountId);
        if (await otherAccounts.AnyAsync(item => item.Code == code, cancellationToken))
            return Result.Failure<Guid>(PlatformAccountErrors.DuplicateCode);
        if (await otherAccounts.AnyAsync(item => item.ClientPlatformId == request.PlatformId
            && item.ExternalAccountId == externalAccountId, cancellationToken))
            return Result.Failure<Guid>(PlatformAccountErrors.DuplicateExternalAccountId);
        if (await otherAccounts.AnyAsync(item => !item.IsDeleted
                    && (item.Status == PlatformRiderAccountStatus.Available
                        || item.Status == PlatformRiderAccountStatus.Assigned)
                    && item.ClientPlatformId == request.PlatformId
                    && item.RegisteredEmployeeId == owner.EmployeeId
                    && item.OperatingCityId == request.OperatingCityId
                    && item.SponsorId == request.SponsorId,
            cancellationToken))
        {
            return Result.Failure<Guid>(PlatformAccountErrors.DuplicateOwnerAccount);
        }

        return Result.Success(owner.EmployeeId);
    }

    private async Task<Result<SimplePlatformAccountResponse>> LoadAccountAsync(
        Guid id,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var accountQuery = includeArchived
            ? dbContext.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking()
            : dbContext.PlatformRiderAccounts.AsNoTracking();
        var projection = await CreateAccountProjectionQuery(
                accountQuery.Where(item => item.Id == id),
                includeArchived)
            .SingleOrDefaultAsync(cancellationToken);
        if (projection is null)
        {
            return Result.Failure<SimplePlatformAccountResponse>(HrErrors.NotFound);
        }

        OwnerProjection? owner = null;
        if (projection.Account.RegisteredEmployeeId is { } employeeId)
        {
            owner = await LoadOwnerAsync(employeeId, cancellationToken);
        }

        var current = (await LoadAssignmentsAsync([id], true, null, cancellationToken)).FirstOrDefault();
        return Result.Success(ToAccount(projection, owner, current));
    }

    private IQueryable<AccountProjection> CreateAccountProjectionQuery(
        IQueryable<PlatformRiderAccount> accounts,
        bool includeArchived)
    {
        var platforms = includeArchived
            ? dbContext.ClientPlatforms.IgnoreQueryFilters().AsNoTracking()
            : dbContext.ClientPlatforms.AsNoTracking();
        var operatingCities = includeArchived
            ? dbContext.OperatingCities.IgnoreQueryFilters().AsNoTracking()
            : dbContext.OperatingCities.AsNoTracking();
        var cities = includeArchived
            ? dbContext.GlobalCities.IgnoreQueryFilters().AsNoTracking()
            : dbContext.GlobalCities.AsNoTracking();
        var sponsors = includeArchived
            ? dbContext.Sponsors.IgnoreQueryFilters().AsNoTracking()
            : dbContext.Sponsors.AsNoTracking();

        return from account in accounts
               join platform in platforms on account.ClientPlatformId equals platform.Id
               join operatingCity in operatingCities on account.OperatingCityId equals operatingCity.Id
               join city in cities on operatingCity.GlobalCityId equals city.Id
               join sponsorRow in sponsors on account.SponsorId equals (Guid?)sponsorRow.Id into sponsorRows
               from sponsor in sponsorRows.DefaultIfEmpty()
               orderby platform.NameAr, account.ExternalAccountId
               select new AccountProjection(
                   account,
                   platform.Code,
                   platform.NameAr,
                   platform.NameEn,
                   city.NameAr,
                   city.NameEn,
                   sponsor == null ? "خارج الكفالة" : sponsor.RegistryNameAr,
                   sponsor == null ? null : sponsor.RegistryNameEn);
    }

    private async Task<Dictionary<Guid, OwnerProjection>> LoadOwnersAsync(
        Guid[] employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Length == 0)
        {
            return [];
        }

        var rows = await (from employee in dbContext.Employees.AsNoTracking()
                          join riderRow in dbContext.RiderProfiles.AsNoTracking() on employee.Id equals riderRow.EmployeeId into riderRows
                          from rider in riderRows.DefaultIfEmpty()
                          where employeeIds.Contains(employee.Id)
                          select new OwnerProjection(
                              rider == null ? null : rider.Id,
                              employee.Id,
                              employee.FullNameAr,
                              employee.FullNameEn))
            .ToArrayAsync(cancellationToken);
        return rows.GroupBy(item => item.EmployeeId).ToDictionary(group => group.Key, group => group.First());
    }

    private async Task<OwnerProjection?> LoadOwnerAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await (from employee in dbContext.Employees.AsNoTracking()
               join riderRow in dbContext.RiderProfiles.AsNoTracking() on employee.Id equals riderRow.EmployeeId into riderRows
               from rider in riderRows.DefaultIfEmpty()
               where employee.Id == employeeId
               select new OwnerProjection(
                   rider == null ? null : rider.Id,
                   employee.Id,
                   employee.FullNameAr,
                   employee.FullNameEn))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<OwnerProjection?> LoadRiderAsync(
        Guid riderProfileId,
        bool requireActive,
        CancellationToken cancellationToken) =>
        await (from rider in dbContext.RiderProfiles.AsNoTracking()
               join employee in dbContext.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
               where rider.Id == riderProfileId
                   && (!requireActive || employee.Status == EmployeeStatus.Active)
               select new OwnerProjection(
                   rider.Id,
                   employee.Id,
                   employee.FullNameAr,
                   employee.FullNameEn))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<SimplePlatformAssignmentResponse[]> LoadAssignmentsAsync(
        Guid[] accountIds,
        bool currentOnly,
        Guid? actualRiderProfileId,
        CancellationToken cancellationToken)
    {
        var query = from assignment in dbContext.RiderClientAssignments.AsNoTracking()
                    join rider in dbContext.RiderProfiles.AsNoTracking() on assignment.RiderProfileId equals rider.Id
                    join employee in dbContext.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
                    where accountIds.Contains(assignment.PlatformRiderAccountId)
                        && (!currentOnly || assignment.EffectiveTo == null)
                        && (actualRiderProfileId == null || assignment.RiderProfileId == actualRiderProfileId)
                    orderby assignment.EffectiveFrom descending
                    select new AssignmentProjection(
                        assignment,
                        employee.Id,
                        employee.FullNameAr,
                        employee.FullNameEn);
        var rows = await query.ToArrayAsync(cancellationToken);
        var blocked = await RiderOperationalBlock.BlockedAssignmentIds(dbContext, rows.Select(x => x.Assignment.Id).ToArray(), cancellationToken);
        return rows.Select(x => ToAssignment(x) with { IsOperationallyBlocked = blocked.Contains(x.Assignment.Id),
            OperationalBlockReason = blocked.Contains(x.Assignment.Id) ? RiderOperationalBlock.Reason : null }).ToArray();
    }

    private async Task<ClientContract> GetOrCreateInternalContractAsync(
        Guid platformId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var registrationContractId = await dbContext.PlatformAccountRegistrations
            .Where(item => item.PlatformRiderAccountId == accountId)
            .Select(item => (Guid?)item.ClientContractId)
            .SingleOrDefaultAsync(cancellationToken);
        if (registrationContractId is not null)
        {
            var registrationContract = await dbContext.ClientContracts.SingleOrDefaultAsync(
                item => item.Id == registrationContractId && item.Status == ClientContractStatus.Active,
                cancellationToken);
            if (registrationContract is not null)
            {
                return registrationContract;
            }
        }

        var historicalContractId = await dbContext.RiderClientAssignments
            .Where(item => item.PlatformRiderAccountId == accountId)
            .OrderByDescending(item => item.EffectiveFrom)
            .Select(item => (Guid?)item.ClientContractId)
            .FirstOrDefaultAsync(cancellationToken);
        if (historicalContractId is not null)
        {
            var historicalContract = await dbContext.ClientContracts.SingleOrDefaultAsync(
                item => item.Id == historicalContractId && item.Status == ClientContractStatus.Active,
                cancellationToken);
            if (historicalContract is not null)
            {
                return historicalContract;
            }
        }

        var existing = await dbContext.ClientContracts
            .Where(item => item.ClientPlatformId == platformId && item.Status == ClientContractStatus.Active)
            .OrderBy(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var platform = await dbContext.ClientPlatforms.SingleAsync(item => item.Id == platformId, cancellationToken);
        var code = $"SYS-{platform.Id:N}"[..32];
        var archived = await dbContext.ClientContracts.IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Code == code, cancellationToken);
        if (archived is not null)
        {
            archived.IsDeleted = false;
            archived.DeletedAtUtc = null;
            archived.DeletedByUserId = null;
            archived.DeletionReason = null;
            archived.Status = ClientContractStatus.Active;
            return archived;
        }

        var contract = new ClientContract
        {
            ClientPlatformId = platform.Id,
            Code = code,
            DisplayNameAr = Clip($"تشغيل {platform.NameAr}", 200),
            DisplayNameEn = Clip($"{platform.NameEn} operations", 200),
            Status = ClientContractStatus.Active,
            Notes = "System-managed contract used by the simplified platform API."
        };
        dbContext.ClientContracts.Add(contract);
        return contract;
    }

    private async Task EnsureInternalRegistrationAsync(
        PlatformRiderAccount account,
        OwnerProjection owner,
        Guid contractId,
        CancellationToken cancellationToken)
    {
        var registration = await dbContext.PlatformAccountRegistrations.SingleOrDefaultAsync(
            item => item.PlatformRiderAccountId == account.Id,
            cancellationToken);
        if (registration is null)
        {
            registration = new PlatformAccountRegistration
            {
                PlatformRiderAccountId = account.Id
            };
            dbContext.PlatformAccountRegistrations.Add(registration);
        }

        registration.RegisteredEmployeeId = owner.EmployeeId;
        registration.RiderProfileId = owner.RiderProfileId!.Value;
        registration.ClientPlatformId = account.ClientPlatformId;
        registration.ClientContractId = contractId;
        registration.SponsorId = account.SponsorId;
        registration.OperatingCityId = account.OperatingCityId;
        registration.RegistrationType = account.SponsorId is not null ? PlatformRegistrationType.Sponsored : PlatformRegistrationType.Freelancer;
        registration.Status = PlatformAccountRegistrationStatus.Activated;
        registration.RequestedAtUtc ??= timeProvider.GetUtcNow();
        registration.ActivatedAtUtc ??= timeProvider.GetUtcNow();
        registration.StatusReason = null;
        registration.Notes = "Managed by the simplified platform API.";
    }

    private static SimplePlatformResponse ToPlatform(ClientPlatform item) => new(
        item.Id,
        item.Code,
        item.NameAr,
        item.NameEn,
        ToSupportedPaymentModels(item.SupportedPaymentModels),
        item.Status.ToString(),
        item.Notes,
        HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private static SimplePlatformAccountResponse ToAccount(
        AccountProjection projection,
        OwnerProjection? owner,
        SimplePlatformAssignmentResponse? currentAssignment) => new(
            projection.Account.Id,
            projection.Account.ClientPlatformId,
            projection.PlatformCode,
            projection.PlatformNameAr,
            projection.PlatformNameEn,
            projection.Account.OperatingCityId,
            projection.CityNameAr,
            projection.CityNameEn,
            projection.Account.SponsorId,
            projection.SponsorNameAr,
            projection.SponsorNameEn,
            owner?.RiderProfileId,
            owner?.EmployeeId,
            owner?.NameAr,
            owner?.NameEn,
            projection.Account.Code,
            projection.Account.ExternalAccountId,
            projection.Account.UserName,
            projection.Account.PaymentModel.ToString(),
            projection.Account.Status.ToString(),
            projection.Account.StatusReason,
            projection.Account.AcquisitionDate,
            projection.Account.StartDate,
            projection.Account.EndDate,
            projection.Account.OperationalNotes,
            currentAssignment,
            HrServiceSupport.EncodeRowVersion(projection.Account.RowVersion));

    private static SimplePlatformAssignmentResponse ToAssignment(AssignmentProjection projection) => new(
        projection.Assignment.Id,
        projection.Assignment.PlatformRiderAccountId,
        projection.Assignment.PaymentModel.ToString(),
        projection.Assignment.RiderProfileId,
        projection.EmployeeId,
        projection.EmployeeNameAr,
        projection.EmployeeNameEn,
        projection.Assignment.EffectiveFrom,
        projection.Assignment.EffectiveTo,
        projection.Assignment.Status.ToString(),
        projection.Assignment.StartReason,
        projection.Assignment.EndReason,
        projection.Assignment.WasBackdated,
        projection.Assignment.BackdatedReason,
        projection.Assignment.AssignedByUserId,
        projection.Assignment.EndedByUserId,
        HrServiceSupport.EncodeRowVersion(projection.Assignment.RowVersion))
    {
        RiderType = projection.Assignment.NinjaRiderType?.ToString()
    };

    private static SimplePlatformCredentialVersionResponse ToCredential(PlatformAccountCredentialVersion version) => new(
        version.Id,
        version.KeyVersion,
        version.RotatedAtUtc,
        version.RotatedByUserId,
        version.RotationReason);

    private async Task<OperationError> CreateAssignmentLimitErrorAsync(
        PlatformAccountAssignmentDecision decision,
        Guid riderProfileId,
        Guid requestedAccountId,
        PlatformAccountPaymentModel requestedPaymentModel,
        CancellationToken cancellationToken)
    {
        var activeAccounts = await (from assignment in dbContext.RiderClientAssignments.AsNoTracking()
                                    join account in dbContext.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking()
                                        on assignment.PlatformRiderAccountId equals account.Id
                                    join platform in dbContext.ClientPlatforms.IgnoreQueryFilters().AsNoTracking()
                                        on account.ClientPlatformId equals platform.Id
                                    where assignment.RiderProfileId == riderProfileId && assignment.EffectiveTo == null
                                    orderby assignment.PaymentModel descending, assignment.EffectiveFrom
                                    select new ActivePlatformAccountErrorDetail(
                                        assignment.Id,
                                        account.Id,
                                        platform.Id,
                                        platform.Code,
                                        platform.NameAr,
                                        platform.NameEn,
                                        account.ExternalAccountId,
                                        assignment.PaymentModel.ToString()))
            .ToArrayAsync(cancellationToken);
        var requestedPlatformCode = await PlatformAssignmentPolicySupport.PlatformCodeAsync(dbContext, requestedAccountId, cancellationToken);
        var maximumAccounts = ChefzAccountAssignmentRules.MaximumActiveAccounts(requestedPlatformCode, activeAccounts.Select(a => a.PlatformCode));
        var details = new PlatformAssignmentLimitErrorDetail(
            riderProfileId,
            requestedAccountId,
            requestedPaymentModel.ToString(),
            maximumAccounts,
            1,
            activeAccounts,
            GetAllowedPaymentModels(activeAccounts, requestedPlatformCode));
        return ToAssignmentError(decision) with
        {
            Details = new Dictionary<string, object?>
            {
                ["assignmentLimit"] = details
            }
        };
    }

    private static bool TryParseEnum<TEnum>(string? value, out TEnum parsed) where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed);

    private static bool TryParseSupportedPaymentModels(
        IReadOnlyList<string>? values,
        out SupportedPlatformPaymentModels supportedPaymentModels)
    {
        supportedPaymentModels = SupportedPlatformPaymentModels.None;
        if (values is null || values.Count is < 1 or > 2)
        {
            return false;
        }

        foreach (var value in values)
        {
            if (!TryParseEnum(value, out PlatformAccountPaymentModel paymentModel))
            {
                return false;
            }

            var flag = ToSupportedPaymentModel(paymentModel);
            if ((supportedPaymentModels & flag) != 0)
            {
                return false;
            }

            supportedPaymentModels |= flag;
        }

        return supportedPaymentModels != SupportedPlatformPaymentModels.None;
    }

    private static List<string> ToSupportedPaymentModels(SupportedPlatformPaymentModels value)
    {
        var models = new List<string>(2);
        if ((value & SupportedPlatformPaymentModels.PayPerOrder) != 0)
        {
            models.Add(nameof(PlatformAccountPaymentModel.PayPerOrder));
        }

        if ((value & SupportedPlatformPaymentModels.Salary) != 0)
        {
            models.Add(nameof(PlatformAccountPaymentModel.Salary));
        }

        return models;
    }

    private static bool IsPaymentModelSupported(
        SupportedPlatformPaymentModels supportedPaymentModels,
        PlatformAccountPaymentModel paymentModel) =>
        (supportedPaymentModels & ToSupportedPaymentModel(paymentModel)) != 0;

    private static SupportedPlatformPaymentModels ToSupportedPaymentModel(PlatformAccountPaymentModel paymentModel) =>
        paymentModel == PlatformAccountPaymentModel.Salary
            ? SupportedPlatformPaymentModels.Salary
            : SupportedPlatformPaymentModels.PayPerOrder;

    private static OperationError ToAssignmentError(PlatformAccountAssignmentDecision decision) => decision switch
    {
        PlatformAccountAssignmentDecision.SalaryAccountLimitReached => HrErrors.RiderSalaryAccountLimitReached,
        PlatformAccountAssignmentDecision.AccountLimitReached => HrErrors.RiderAccountLimitReached,
        _ => HrErrors.Conflict
    };

    private static string[] GetAllowedPaymentModels(
        ActivePlatformAccountErrorDetail[] activeAccounts, string? requestedPlatformCode)
    {
        return Enum.GetValues<PlatformAccountPaymentModel>().Where(model => ChefzAccountAssignmentRules.Evaluate(requestedPlatformCode,
                activeAccounts.Select(a => ((string?)a.PlatformCode, Enum.Parse<PlatformAccountPaymentModel>(a.PaymentModel))), model)
            == PlatformAccountAssignmentDecision.Allowed).Select(model => model.ToString()).ToArray();
    }

    private static TEnum ParseEnum<TEnum>(string value) where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(value, true);

    private static string Clip(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private sealed record AccountProjection(
        PlatformRiderAccount Account,
        string PlatformCode,
        string PlatformNameAr,
        string PlatformNameEn,
        string CityNameAr,
        string CityNameEn,
        string SponsorNameAr,
        string? SponsorNameEn);

    private sealed record OwnerProjection(
        Guid? RiderProfileId,
        Guid EmployeeId,
        string NameAr,
        string? NameEn);

    private sealed record AssignmentProjection(
        RiderClientAssignment Assignment,
        Guid EmployeeId,
        string EmployeeNameAr,
        string? EmployeeNameEn);
}
