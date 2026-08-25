using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
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
        Guid? ownerRiderProfileId,
        Guid? actualRiderProfileId,
        string? status,
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

        if (parsedStatus is not null)
        {
            accountQuery = accountQuery.Where(item => item.Status == parsedStatus);
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
            Code = HrServiceSupport.NormalizeCode(request.Code),
            ExternalAccountId = request.ExternalAccountId.Trim(),
            UserName = HrServiceSupport.TrimOrNull(request.UserName),
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
        var hasActiveAssignment = await dbContext.RiderClientAssignments.AnyAsync(
            item => item.PlatformRiderAccountId == id && item.EffectiveTo == null,
            cancellationToken);
        if (hasActiveAssignment && status != PlatformRiderAccountStatus.Assigned
            || !hasActiveAssignment && status == PlatformRiderAccountStatus.Assigned)
        {
            return Result.Failure<SimplePlatformAccountResponse>(HrErrors.Conflict);
        }

        if (hasActiveAssignment
            && (entity.ClientPlatformId != request.PlatformId
                || entity.RegisteredEmployeeId != validation.Value
                || entity.OperatingCityId != request.OperatingCityId))
        {
            return Result.Failure<SimplePlatformAccountResponse>(HrErrors.Conflict);
        }

        if (status == PlatformRiderAccountStatus.Archived)
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
        entity.Code = HrServiceSupport.NormalizeCode(request.Code);
        entity.ExternalAccountId = request.ExternalAccountId.Trim();
        entity.UserName = HrServiceSupport.TrimOrNull(request.UserName);
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
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId
            || request.ActualRiderProfileId == Guid.Empty
            || request.WasBackdated && !HrServiceSupport.HasText(request.BackdatedReason))
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.InvalidRequest);
        }

        var account = await dbContext.PlatformRiderAccounts.SingleOrDefaultAsync(
            item => item.Id == accountId,
            cancellationToken);
        if (account is null)
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.NotFound);
        }

        if (account.Status != PlatformRiderAccountStatus.Available
            || await dbContext.RiderClientAssignments.AnyAsync(
                item => item.EffectiveTo == null
                    && (item.PlatformRiderAccountId == accountId || item.RiderProfileId == request.ActualRiderProfileId),
                cancellationToken))
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.Conflict);
        }

        var actualRider = await LoadRiderAsync(request.ActualRiderProfileId, true, cancellationToken);
        if (actualRider is null)
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.NotFound);
        }

        OwnerProjection? owner;
        if (account.RegisteredEmployeeId is { } ownerEmployeeId)
        {
            owner = await LoadOwnerAsync(ownerEmployeeId, cancellationToken);
            if (owner is null)
            {
                return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.NotFound);
            }
        }
        else
        {
            owner = actualRider;
            account.RegisteredEmployeeId = actualRider.EmployeeId;
        }

        var contract = await GetOrCreateInternalContractAsync(account.ClientPlatformId, account.Id, cancellationToken);
        await EnsureInternalRegistrationAsync(account, owner, contract.Id, cancellationToken);

        var assignment = new RiderClientAssignment
        {
            RiderProfileId = actualRider.RiderProfileId,
            ClientContractId = contract.Id,
            PlatformRiderAccountId = account.Id,
            EffectiveFrom = request.EffectiveFrom,
            Status = RiderAssignmentStatus.Active,
            StartReason = HrServiceSupport.TrimOrNull(request.Reason),
            AssignedByUserId = userId,
            WasBackdated = request.WasBackdated,
            BackdatedReason = HrServiceSupport.TrimOrNull(request.BackdatedReason)
        };
        dbContext.RiderClientAssignments.Add(assignment);
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
                request.EffectiveFrom
            })
        });
        account.Status = PlatformRiderAccountStatus.Assigned;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
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
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId
            || !TryParseEnum(request.Status, out RiderAssignmentStatus status)
            || status is not (RiderAssignmentStatus.Ended or RiderAssignmentStatus.Cancelled)
            || !HrServiceSupport.HasText(request.Reason))
        {
            return Result.Failure<SimplePlatformAssignmentResponse>(HrErrors.InvalidRequest);
        }

        var account = await dbContext.PlatformRiderAccounts.SingleOrDefaultAsync(
            item => item.Id == accountId,
            cancellationToken);
        var assignment = await dbContext.RiderClientAssignments.SingleOrDefaultAsync(
            item => item.PlatformRiderAccountId == accountId && item.EffectiveTo == null,
            cancellationToken);
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
        account.Status = PlatformRiderAccountStatus.Available;
        await dbContext.SaveChangesAsync(cancellationToken);
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
            rider.RiderProfileId,
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

        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = request.NameEn.Trim();
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
        if (request.PlatformId == Guid.Empty
            || request.OperatingCityId == Guid.Empty
            || request.OwnerRiderProfileId == Guid.Empty
            || !HrServiceSupport.HasText(request.Code)
            || !HrServiceSupport.HasText(request.ExternalAccountId)
            || !TryParseEnum(request.Status, out PlatformRiderAccountStatus status)
            || request.EndDate is not null && request.StartDate is not null && request.EndDate < request.StartDate
            || !isUpdate && status is PlatformRiderAccountStatus.Assigned or PlatformRiderAccountStatus.Archived)
        {
            return Result.Failure<Guid>(HrErrors.InvalidRequest);
        }

        var owner = await LoadRiderAsync(request.OwnerRiderProfileId, false, cancellationToken);
        var referencesExist = owner is not null
            && await dbContext.ClientPlatforms.AnyAsync(item => item.Id == request.PlatformId, cancellationToken)
            && await dbContext.OperatingCities.AnyAsync(item => item.Id == request.OperatingCityId, cancellationToken);
        if (!referencesExist)
        {
            return Result.Failure<Guid>(HrErrors.NotFound);
        }

        var code = HrServiceSupport.NormalizeCode(request.Code);
        var externalAccountId = request.ExternalAccountId.Trim();
        var duplicate = await dbContext.PlatformRiderAccounts.IgnoreQueryFilters().AnyAsync(item =>
            item.Id != accountId
            && (item.Code == code
                || item.ClientPlatformId == request.PlatformId && item.ExternalAccountId == externalAccountId
                || !item.IsDeleted && item.ClientPlatformId == request.PlatformId && item.RegisteredEmployeeId == owner!.EmployeeId),
            cancellationToken);
        if (duplicate)
        {
            return Result.Failure<Guid>(HrErrors.Duplicate);
        }

        return Result.Success(owner!.EmployeeId);
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

        return from account in accounts
               join platform in platforms on account.ClientPlatformId equals platform.Id
               join operatingCity in operatingCities on account.OperatingCityId equals operatingCity.Id
               join city in cities on operatingCity.GlobalCityId equals city.Id
               orderby platform.NameAr, account.ExternalAccountId
               select new AccountProjection(
                   account,
                   platform.Code,
                   platform.NameAr,
                   platform.NameEn,
                   city.NameAr,
                   city.NameEn);
    }

    private async Task<Dictionary<Guid, OwnerProjection>> LoadOwnersAsync(
        Guid[] employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Length == 0)
        {
            return [];
        }

        var rows = await (from rider in dbContext.RiderProfiles.AsNoTracking()
                          join employee in dbContext.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
                          where employeeIds.Contains(employee.Id)
                          select new OwnerProjection(
                              rider.Id,
                              employee.Id,
                              employee.FullNameAr,
                              employee.FullNameEn,
                              employee.SponsorId))
            .ToArrayAsync(cancellationToken);
        return rows.GroupBy(item => item.EmployeeId).ToDictionary(group => group.Key, group => group.First());
    }

    private async Task<OwnerProjection?> LoadOwnerAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await (from rider in dbContext.RiderProfiles.AsNoTracking()
               join employee in dbContext.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
               where employee.Id == employeeId
               select new OwnerProjection(
                   rider.Id,
                   employee.Id,
                   employee.FullNameAr,
                   employee.FullNameEn,
                   employee.SponsorId))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<OwnerProjection?> LoadRiderAsync(
        Guid riderProfileId,
        bool requireActive,
        CancellationToken cancellationToken) =>
        await (from rider in dbContext.RiderProfiles.AsNoTracking()
               join employee in dbContext.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
               where rider.Id == riderProfileId
                   && !employee.IsEmployee
                   && (!requireActive || employee.Status == EmployeeStatus.Active)
               select new OwnerProjection(
                   rider.Id,
                   employee.Id,
                   employee.FullNameAr,
                   employee.FullNameEn,
                   employee.SponsorId))
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
        return rows.Select(ToAssignment).ToArray();
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
        registration.RiderProfileId = owner.RiderProfileId;
        registration.ClientPlatformId = account.ClientPlatformId;
        registration.ClientContractId = contractId;
        registration.SponsorId = owner.SponsorId;
        registration.OperatingCityId = account.OperatingCityId;
        registration.RegistrationType = owner.SponsorId is null
            ? PlatformRegistrationType.Freelancer
            : PlatformRegistrationType.Sponsored;
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
            owner?.RiderProfileId,
            owner?.EmployeeId,
            owner?.NameAr,
            owner?.NameEn,
            projection.Account.Code,
            projection.Account.ExternalAccountId,
            projection.Account.UserName,
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
        HrServiceSupport.EncodeRowVersion(projection.Assignment.RowVersion));

    private static SimplePlatformCredentialVersionResponse ToCredential(PlatformAccountCredentialVersion version) => new(
        version.Id,
        version.KeyVersion,
        version.RotatedAtUtc,
        version.RotatedByUserId,
        version.RotationReason);

    private static bool TryParseEnum<TEnum>(string? value, out TEnum parsed) where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed);

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
        string CityNameEn);

    private sealed record OwnerProjection(
        Guid RiderProfileId,
        Guid EmployeeId,
        string NameAr,
        string? NameEn,
        Guid? SponsorId);

    private sealed record AssignmentProjection(
        RiderClientAssignment Assignment,
        Guid EmployeeId,
        string EmployeeNameAr,
        string? EmployeeNameEn);
}
