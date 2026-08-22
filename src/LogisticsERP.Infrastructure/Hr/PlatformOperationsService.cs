using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class PlatformOperationsService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IPlatformCredentialProtector credentialProtector) : IPlatformOperationsService
{
    public async Task<Result<IReadOnlyList<ClientPlatformResponse>>> GetPlatformsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.ClientPlatforms.AsNoTracking().OrderBy(item => item.NameAr).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ClientPlatformResponse>>(rows.Select(ToPlatform).ToArray());
    }

    public async Task<Result<ClientPlatformResponse>> UpsertPlatformAsync(Guid? id, ClientPlatformUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Code) || !HrServiceSupport.HasText(request.NameAr)
            || !HrServiceSupport.HasText(request.NameEn) || !TryParseEnum<CatalogStatus>(request.Status, out var status))
            return Result.Failure<ClientPlatformResponse>(HrErrors.InvalidRequest);
        ClientPlatform entity;
        if (id is null)
        {
            entity = new ClientPlatform();
            dbContext.ClientPlatforms.Add(entity);
        }
        else
        {
            entity = await dbContext.ClientPlatforms.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<ClientPlatformResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<ClientPlatformResponse>(HrErrors.ConcurrencyConflict);
        }
        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.ClientPlatforms.AnyAsync(item => item.Id != entity.Id && item.Code == code, cancellationToken))
            return Result.Failure<ClientPlatformResponse>(HrErrors.Duplicate);
        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = request.NameEn.Trim();
        entity.Status = status;
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToPlatform(entity));
    }

    public async Task<Result<IReadOnlyList<ClientContractResponse>>> GetContractsAsync(Guid? platformId, CancellationToken cancellationToken = default)
    {
        var rows = await (from contract in dbContext.ClientContracts.AsNoTracking()
                          join platform in dbContext.ClientPlatforms.AsNoTracking() on contract.ClientPlatformId equals platform.Id
                          where platformId == null || contract.ClientPlatformId == platformId
                          orderby platform.NameAr, contract.DisplayNameAr
                          select new ContractProjection(contract, platform.NameAr)).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ClientContractResponse>>(rows.Select(ToContract).ToArray());
    }

    public async Task<Result<ClientContractResponse>> UpsertContractAsync(Guid? id, ClientContractUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Code) || !HrServiceSupport.HasText(request.DisplayNameAr)
            || !HrServiceSupport.HasText(request.DisplayNameEn) || !TryParseEnum<ClientContractStatus>(request.Status, out var status)
            || request.EndDate is not null && request.StartDate is not null && request.EndDate < request.StartDate
            || !await dbContext.ClientPlatforms.AnyAsync(item => item.Id == request.ClientPlatformId, cancellationToken))
            return Result.Failure<ClientContractResponse>(HrErrors.InvalidRequest);
        ClientContract entity;
        if (id is null)
        {
            entity = new ClientContract();
            dbContext.ClientContracts.Add(entity);
        }
        else
        {
            entity = await dbContext.ClientContracts.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<ClientContractResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<ClientContractResponse>(HrErrors.ConcurrencyConflict);
        }
        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.ClientContracts.AnyAsync(item => item.Id != entity.Id && item.Code == code, cancellationToken))
            return Result.Failure<ClientContractResponse>(HrErrors.Duplicate);
        entity.ClientPlatformId = request.ClientPlatformId;
        entity.Code = code;
        entity.DisplayNameAr = request.DisplayNameAr.Trim();
        entity.DisplayNameEn = request.DisplayNameEn.Trim();
        entity.ExternalBusinessAccountId = HrServiceSupport.TrimOrNull(request.ExternalBusinessAccountId);
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.Status = status;
        entity.StatusReason = HrServiceSupport.TrimOrNull(request.StatusReason);
        entity.ContactName = HrServiceSupport.TrimOrNull(request.ContactName);
        entity.ContactPhone = HrServiceSupport.TrimOrNull(request.ContactPhone);
        entity.ContactEmail = HrServiceSupport.TrimOrNull(request.ContactEmail);
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetContractsAsync(null, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<IReadOnlyList<PlatformAccountResponse>>> GetAccountsAsync(Guid? platformId, CancellationToken cancellationToken = default)
    {
        var rows = await (from account in dbContext.PlatformRiderAccounts.AsNoTracking()
                          join contract in dbContext.ClientContracts.AsNoTracking() on account.ClientContractId equals contract.Id
                          join platform in dbContext.ClientPlatforms.AsNoTracking() on account.ClientPlatformId equals platform.Id
                          join employee in dbContext.Employees.AsNoTracking() on account.RegisteredEmployeeId equals employee.Id into employees
                          from employee in employees.DefaultIfEmpty()
                          join sponsor in dbContext.Sponsors.AsNoTracking() on account.SponsorId equals sponsor.Id into sponsors
                          from sponsor in sponsors.DefaultIfEmpty()
                          join operatingCity in dbContext.OperatingCities.AsNoTracking() on account.OperatingCityId equals operatingCity.Id
                          join city in dbContext.GlobalCities.AsNoTracking() on operatingCity.GlobalCityId equals city.Id
                          where platformId == null || account.ClientPlatformId == platformId
                          orderby platform.NameAr, account.ExternalAccountId
                          select new AccountProjection(account, contract.DisplayNameAr, platform.NameAr,
                              employee == null ? null : employee.FullNameAr, sponsor == null ? null : sponsor.RegistryNameAr, city.NameAr))
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PlatformAccountResponse>>(rows.Select(ToAccount).ToArray());
    }

    public async Task<Result<PlatformAccountResponse>> UpsertAccountAsync(Guid? id, PlatformAccountUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryParseEnum<PlatformRegistrationType>(request.RegistrationType, out var registrationType)
            || !TryParseEnum<PlatformBillingMode>(request.BillingMode, out var billingMode)
            || !TryParseEnum<PlatformRiderAccountStatus>(request.Status, out var status)
            || !HrServiceSupport.HasText(request.Code) || !HrServiceSupport.HasText(request.ExternalAccountId)
            || registrationType == PlatformRegistrationType.Sponsored && request.SponsorId is null
            || registrationType == PlatformRegistrationType.Freelancer && request.SponsorId is not null
            || request.EndDate is not null && request.StartDate is not null && request.EndDate < request.StartDate)
            return Result.Failure<PlatformAccountResponse>(HrErrors.InvalidRequest);
        var contractPlatformId = await dbContext.ClientContracts.Where(item => item.Id == request.ClientContractId)
            .Select(item => (Guid?)item.ClientPlatformId).SingleOrDefaultAsync(cancellationToken);
        var refsValid = contractPlatformId == request.ClientPlatformId
            && await dbContext.OperatingCities.AnyAsync(item => item.Id == request.OperatingCityId, cancellationToken)
            && (request.RegisteredEmployeeId is null || await dbContext.Employees.AnyAsync(item => item.Id == request.RegisteredEmployeeId, cancellationToken))
            && (request.SponsorId is null || await dbContext.Sponsors.AnyAsync(item => item.Id == request.SponsorId, cancellationToken));
        if (!refsValid) return Result.Failure<PlatformAccountResponse>(HrErrors.NotFound);
        PlatformRiderAccount entity;
        if (id is null)
        {
            entity = new PlatformRiderAccount();
            dbContext.PlatformRiderAccounts.Add(entity);
        }
        else
        {
            entity = await dbContext.PlatformRiderAccounts.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<PlatformAccountResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<PlatformAccountResponse>(HrErrors.ConcurrencyConflict);
        }
        var code = HrServiceSupport.NormalizeCode(request.Code);
        var externalId = HrServiceSupport.NormalizeIdentifier(request.ExternalAccountId);
        if (await dbContext.PlatformRiderAccounts.AnyAsync(item => item.Id != entity.Id && (item.Code == code || item.ClientPlatformId == request.ClientPlatformId && item.NormalizedExternalAccountId == externalId), cancellationToken))
            return Result.Failure<PlatformAccountResponse>(HrErrors.Duplicate);
        entity.ClientContractId = request.ClientContractId;
        entity.ClientPlatformId = request.ClientPlatformId;
        entity.RegisteredEmployeeId = request.RegisteredEmployeeId;
        entity.SponsorId = request.SponsorId;
        entity.OperatingCityId = request.OperatingCityId;
        entity.RegistrationType = registrationType;
        entity.BillingMode = billingMode;
        entity.Code = code;
        entity.ExternalAccountId = request.ExternalAccountId.Trim();
        entity.NormalizedExternalAccountId = externalId;
        entity.UserName = HrServiceSupport.TrimOrNull(request.UserName);
        entity.LabelAr = HrServiceSupport.TrimOrNull(request.LabelAr);
        entity.LabelEn = HrServiceSupport.TrimOrNull(request.LabelEn);
        entity.Status = status;
        entity.StatusReason = HrServiceSupport.TrimOrNull(request.StatusReason);
        entity.AcquisitionDate = request.AcquisitionDate;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.OwnershipNotes = HrServiceSupport.TrimOrNull(request.OwnershipNotes);
        entity.OperationalNotes = HrServiceSupport.TrimOrNull(request.OperationalNotes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAccountsAsync(null, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<IReadOnlyList<PlatformCredentialVersionResponse>>> GetCredentialVersionsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.PlatformRiderAccounts.AsNoTracking().AnyAsync(item => item.Id == accountId, cancellationToken))
            return Result.Failure<IReadOnlyList<PlatformCredentialVersionResponse>>(HrErrors.NotFound);
        var versions = await dbContext.PlatformAccountCredentialVersions.AsNoTracking()
            .Where(item => item.PlatformRiderAccountId == accountId)
            .OrderByDescending(item => item.KeyVersion)
            .Select(item => new PlatformCredentialVersionResponse(
                item.Id,
                item.PlatformRiderAccountId,
                item.KeyVersion,
                item.RotatedAtUtc,
                item.RotatedByUserId,
                item.RotationReason,
                item.SupersededVersionId))
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PlatformCredentialVersionResponse>>(versions);
    }

    public async Task<Result<PlatformCredentialVersionResponse>> RotateCredentialAsync(
        Guid accountId,
        RotatePlatformCredentialRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId
            || string.IsNullOrWhiteSpace(request.Secret)
            || request.Secret.Length > 4096
            || string.IsNullOrWhiteSpace(request.Reason)
            || request.Reason.Trim().Length > 1000)
            return Result.Failure<PlatformCredentialVersionResponse>(HrErrors.InvalidRequest);
        if (!await dbContext.PlatformRiderAccounts.AnyAsync(item => item.Id == accountId, cancellationToken))
            return Result.Failure<PlatformCredentialVersionResponse>(HrErrors.NotFound);

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
            return Result.Failure<PlatformCredentialVersionResponse>(HrErrors.ConcurrencyConflict);
        }
        return Result.Success(new PlatformCredentialVersionResponse(
            version.Id,
            version.PlatformRiderAccountId,
            version.KeyVersion,
            version.RotatedAtUtc,
            version.RotatedByUserId,
            version.RotationReason,
            version.SupersededVersionId));
    }

    public async Task<Result<IReadOnlyList<PlatformRegistrationResponse>>> GetRegistrationsAsync(Guid? riderProfileId, CancellationToken cancellationToken = default)
    {
        var rows = await (from registration in dbContext.PlatformAccountRegistrations.AsNoTracking()
                          join employee in dbContext.Employees.AsNoTracking() on registration.RegisteredEmployeeId equals employee.Id
                          join platform in dbContext.ClientPlatforms.AsNoTracking() on registration.ClientPlatformId equals platform.Id
                          join contract in dbContext.ClientContracts.AsNoTracking() on registration.ClientContractId equals contract.Id
                          join sponsor in dbContext.Sponsors.AsNoTracking() on registration.SponsorId equals sponsor.Id into sponsors
                          from sponsor in sponsors.DefaultIfEmpty()
                          join operatingCity in dbContext.OperatingCities.AsNoTracking() on registration.OperatingCityId equals operatingCity.Id
                          join city in dbContext.GlobalCities.AsNoTracking() on operatingCity.GlobalCityId equals city.Id
                          where riderProfileId == null || registration.RiderProfileId == riderProfileId
                          orderby registration.CreatedAtUtc descending
                          select new RegistrationProjection(registration, employee.FullNameAr, platform.NameAr, contract.DisplayNameAr,
                              sponsor == null ? null : sponsor.RegistryNameAr, city.NameAr)).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PlatformRegistrationResponse>>(rows.Select(ToRegistration).ToArray());
    }

    public async Task<Result<PlatformRegistrationResponse>> UpsertRegistrationAsync(Guid? id, PlatformRegistrationUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryParseEnum<PlatformRegistrationType>(request.RegistrationType, out var registrationType)
            || !TryParseEnum<PlatformAccountRegistrationStatus>(request.Status, out var status)
            || registrationType == PlatformRegistrationType.Sponsored && request.SponsorId is null
            || registrationType == PlatformRegistrationType.Freelancer && request.SponsorId is not null
            || request.ActivatedAtUtc is not null && request.RequestedAtUtc is not null && request.ActivatedAtUtc < request.RequestedAtUtc)
            return Result.Failure<PlatformRegistrationResponse>(HrErrors.InvalidRequest);
        var riderEmployeeId = await dbContext.RiderProfiles.Where(item => item.Id == request.RiderProfileId).Select(item => (Guid?)item.EmployeeId).SingleOrDefaultAsync(cancellationToken);
        var contractPlatform = await dbContext.ClientContracts.Where(item => item.Id == request.ClientContractId).Select(item => (Guid?)item.ClientPlatformId).SingleOrDefaultAsync(cancellationToken);
        var refsValid = riderEmployeeId == request.RegisteredEmployeeId && contractPlatform == request.ClientPlatformId
            && await dbContext.OperatingCities.AnyAsync(item => item.Id == request.OperatingCityId, cancellationToken)
            && (request.SponsorId is null || await dbContext.Sponsors.AnyAsync(item => item.Id == request.SponsorId, cancellationToken))
            && (request.PlatformRiderAccountId is null || await dbContext.PlatformRiderAccounts.AnyAsync(item => item.Id == request.PlatformRiderAccountId && item.ClientPlatformId == request.ClientPlatformId, cancellationToken));
        if (!refsValid) return Result.Failure<PlatformRegistrationResponse>(HrErrors.NotFound);
        PlatformAccountRegistration entity;
        if (id is null)
        {
            entity = new PlatformAccountRegistration();
            dbContext.PlatformAccountRegistrations.Add(entity);
        }
        else
        {
            entity = await dbContext.PlatformAccountRegistrations.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<PlatformRegistrationResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<PlatformRegistrationResponse>(HrErrors.ConcurrencyConflict);
        }
        entity.RegisteredEmployeeId = request.RegisteredEmployeeId;
        entity.RiderProfileId = request.RiderProfileId;
        entity.ClientPlatformId = request.ClientPlatformId;
        entity.ClientContractId = request.ClientContractId;
        entity.SponsorId = request.SponsorId;
        entity.OperatingCityId = request.OperatingCityId;
        entity.RegistrationType = registrationType;
        entity.Status = status;
        entity.StatusReason = HrServiceSupport.TrimOrNull(request.StatusReason);
        entity.RequestedAtUtc = request.RequestedAtUtc;
        entity.ActivatedAtUtc = request.ActivatedAtUtc;
        entity.PlatformRiderAccountId = request.PlatformRiderAccountId;
        entity.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetRegistrationsAsync(null, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<IReadOnlyList<PlatformAssignmentResponse>>> GetAssignmentsAsync(Guid? riderProfileId, bool currentOnly, CancellationToken cancellationToken = default)
    {
        var rows = await (from assignment in dbContext.RiderClientAssignments.AsNoTracking()
                          join employee in dbContext.Employees.AsNoTracking() on assignment.ActualEmployeeId equals employee.Id
                          join contract in dbContext.ClientContracts.AsNoTracking() on assignment.ClientContractId equals contract.Id
                          join account in dbContext.PlatformRiderAccounts.AsNoTracking() on assignment.PlatformRiderAccountId equals account.Id
                          where (riderProfileId == null || assignment.RiderProfileId == riderProfileId) && (!currentOnly || assignment.EffectiveTo == null)
                          orderby assignment.EffectiveFrom descending
                          select new AssignmentProjection(assignment, employee.FullNameAr, contract.DisplayNameAr, account.ExternalAccountId)).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PlatformAssignmentResponse>>(rows.Select(ToAssignment).ToArray());
    }

    public async Task<Result<PlatformAssignmentResponse>> AssignAccountAsync(AssignPlatformAccountRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure<PlatformAssignmentResponse>(HrErrors.CurrentUserUnavailable);
        if (!TryParseEnum<RiderAssignmentStatus>(request.Status, out var status)
            || status is RiderAssignmentStatus.Ended or RiderAssignmentStatus.Cancelled
            || request.WasBackdated && !HrServiceSupport.HasText(request.BackdatedReason))
            return Result.Failure<PlatformAssignmentResponse>(HrErrors.InvalidRequest);
        var riderEmployee = await dbContext.RiderProfiles.Where(item => item.Id == request.RiderProfileId).Select(item => (Guid?)item.EmployeeId).SingleOrDefaultAsync(cancellationToken);
        var accountContract = await dbContext.PlatformRiderAccounts.Where(item => item.Id == request.PlatformRiderAccountId).Select(item => (Guid?)item.ClientContractId).SingleOrDefaultAsync(cancellationToken);
        if (riderEmployee != request.ActualEmployeeId || accountContract != request.ClientContractId)
            return Result.Failure<PlatformAssignmentResponse>(HrErrors.NotFound);
        if (await dbContext.RiderClientAssignments.AnyAsync(item => item.EffectiveTo == null && (item.ActualEmployeeId == request.ActualEmployeeId || item.PlatformRiderAccountId == request.PlatformRiderAccountId), cancellationToken))
            return Result.Failure<PlatformAssignmentResponse>(HrErrors.Conflict);
        var entity = new RiderClientAssignment
        {
            ActualEmployeeId = request.ActualEmployeeId,
            RiderProfileId = request.RiderProfileId,
            ClientContractId = request.ClientContractId,
            PlatformRiderAccountId = request.PlatformRiderAccountId,
            EffectiveFrom = request.EffectiveFrom,
            Status = status,
            StartReason = HrServiceSupport.TrimOrNull(request.StartReason),
            OperationalAgreementReference = HrServiceSupport.TrimOrNull(request.OperationalAgreementReference),
            OperationalAgreementNotes = HrServiceSupport.TrimOrNull(request.OperationalAgreementNotes),
            AssignedByUserId = userId,
            WasBackdated = request.WasBackdated,
            BackdatedReason = HrServiceSupport.TrimOrNull(request.BackdatedReason)
        };
        dbContext.RiderClientAssignments.Add(entity);
        dbContext.RiderAssignmentEvents.Add(new RiderAssignmentEvent
        {
            RiderClientAssignmentId = entity.Id,
            FromStatus = RiderAssignmentStatus.Planned,
            ToStatus = status,
            OccurredAtUtc = timeProvider.GetUtcNow(),
            ActorUserId = userId,
            Reason = request.StartReason?.Trim() ?? "Platform account assigned.",
            ChangeSnapshotJson = JsonSerializer.Serialize(new { request.ActualEmployeeId, request.PlatformRiderAccountId, request.EffectiveFrom })
        });
        var account = await dbContext.PlatformRiderAccounts.SingleAsync(item => item.Id == request.PlatformRiderAccountId, cancellationToken);
        account.Status = PlatformRiderAccountStatus.Assigned;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAssignmentsAsync(null, false, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<PlatformAssignmentResponse>> CloseAssignmentAsync(Guid id, ClosePlatformAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || !TryParseEnum<RiderAssignmentStatus>(request.Status, out var status)
            || status is not (RiderAssignmentStatus.Ended or RiderAssignmentStatus.Cancelled) || !HrServiceSupport.HasText(request.EndReason))
            return Result.Failure<PlatformAssignmentResponse>(HrErrors.InvalidRequest);
        var entity = await dbContext.RiderClientAssignments.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return Result.Failure<PlatformAssignmentResponse>(HrErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<PlatformAssignmentResponse>(HrErrors.ConcurrencyConflict);
        if (entity.EffectiveTo is not null || request.EffectiveTo < entity.EffectiveFrom) return Result.Failure<PlatformAssignmentResponse>(HrErrors.Conflict);
        var fromStatus = entity.Status;
        entity.EffectiveTo = request.EffectiveTo;
        entity.Status = status;
        entity.EndReason = request.EndReason.Trim();
        entity.EndedByUserId = userId;
        dbContext.RiderAssignmentEvents.Add(new RiderAssignmentEvent
        {
            RiderClientAssignmentId = entity.Id,
            FromStatus = fromStatus,
            ToStatus = status,
            OccurredAtUtc = timeProvider.GetUtcNow(),
            ActorUserId = userId,
            Reason = request.EndReason.Trim(),
            ChangeSnapshotJson = JsonSerializer.Serialize(new { request.EffectiveTo, Status = status.ToString() })
        });
        var account = await dbContext.PlatformRiderAccounts.SingleAsync(item => item.Id == entity.PlatformRiderAccountId, cancellationToken);
        account.Status = PlatformRiderAccountStatus.Available;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAssignmentsAsync(null, false, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result> ArchiveAsync(string resource, Guid id, ArchiveRequest request, CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Reason)) return Result.Failure(HrErrors.InvalidRequest);
        AuditableEntity? entity = resource.Trim().ToLowerInvariant() switch
        {
            "platform" => await dbContext.ClientPlatforms.SingleOrDefaultAsync(item => item.Id == id, cancellationToken),
            "contract" => await dbContext.ClientContracts.SingleOrDefaultAsync(item => item.Id == id, cancellationToken),
            "account" => await dbContext.PlatformRiderAccounts.SingleOrDefaultAsync(item => item.Id == id, cancellationToken),
            "registration" => await dbContext.PlatformAccountRegistrations.SingleOrDefaultAsync(item => item.Id == id, cancellationToken),
            _ => null
        };
        if (entity is null) return Result.Failure(HrErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure(HrErrors.ConcurrencyConflict);
        if (entity is PlatformRiderAccount account && await dbContext.RiderClientAssignments.AnyAsync(item => item.PlatformRiderAccountId == account.Id && item.EffectiveTo == null, cancellationToken))
            return Result.Failure(HrErrors.Conflict);
        entity.IsDeleted = true;
        entity.DeletionReason = request.Reason.Trim();
        if (entity is ClientPlatform platform) platform.Status = CatalogStatus.Archived;
        if (entity is ClientContract contract) contract.Status = ClientContractStatus.Archived;
        if (entity is PlatformRiderAccount riderAccount) riderAccount.Status = PlatformRiderAccountStatus.Archived;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static ClientPlatformResponse ToPlatform(ClientPlatform item) => new(item.Id, item.Code, item.NameAr, item.NameEn,
        item.Status.ToString(), item.Notes, HrServiceSupport.EncodeRowVersion(item.RowVersion));
    private static ClientContractResponse ToContract(ContractProjection row) => new(row.Item.Id, row.Item.ClientPlatformId,
        row.PlatformNameAr, row.Item.Code, row.Item.DisplayNameAr, row.Item.DisplayNameEn, row.Item.ExternalBusinessAccountId,
        row.Item.StartDate, row.Item.EndDate, row.Item.Status.ToString(), row.Item.StatusReason, row.Item.ContactName,
        row.Item.ContactPhone, row.Item.ContactEmail, row.Item.Notes, HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));
    private static PlatformAccountResponse ToAccount(AccountProjection row) => new(row.Item.Id, row.Item.ClientContractId,
        row.ContractNameAr, row.Item.ClientPlatformId, row.PlatformNameAr, row.Item.RegisteredEmployeeId, row.EmployeeNameAr,
        row.Item.SponsorId, row.SponsorNameAr, row.Item.OperatingCityId, row.CityNameAr, row.Item.RegistrationType.ToString(),
        row.Item.BillingMode.ToString(), row.Item.Code, row.Item.ExternalAccountId, row.Item.UserName, row.Item.LabelAr,
        row.Item.LabelEn, row.Item.Status.ToString(), row.Item.StatusReason, row.Item.AcquisitionDate, row.Item.StartDate,
        row.Item.EndDate, row.Item.OwnershipNotes, row.Item.OperationalNotes, HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));
    private static PlatformRegistrationResponse ToRegistration(RegistrationProjection row) => new(row.Item.Id,
        row.Item.RegisteredEmployeeId, row.EmployeeNameAr, row.Item.RiderProfileId, row.Item.ClientPlatformId,
        row.PlatformNameAr, row.Item.ClientContractId, row.ContractNameAr, row.Item.SponsorId, row.SponsorNameAr,
        row.Item.OperatingCityId, row.CityNameAr, row.Item.RegistrationType.ToString(), row.Item.Status.ToString(),
        row.Item.StatusReason, row.Item.RequestedAtUtc, row.Item.ActivatedAtUtc, row.Item.PlatformRiderAccountId,
        row.Item.Notes, HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));
    private static PlatformAssignmentResponse ToAssignment(AssignmentProjection row) => new(row.Item.Id,
        row.Item.ActualEmployeeId, row.EmployeeNameAr, row.Item.RiderProfileId, row.Item.ClientContractId,
        row.ContractNameAr, row.Item.PlatformRiderAccountId, row.ExternalAccountId, row.Item.EffectiveFrom,
        row.Item.EffectiveTo, row.Item.Status.ToString(), row.Item.StartReason, row.Item.EndReason,
        row.Item.OperationalAgreementReference, row.Item.OperationalAgreementNotes, row.Item.WasBackdated,
        row.Item.BackdatedReason, HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));

    private static bool TryParseEnum<TEnum>(string? value, out TEnum parsed) where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed);

    private sealed record ContractProjection(ClientContract Item, string PlatformNameAr);
    private sealed record AccountProjection(PlatformRiderAccount Item, string ContractNameAr, string PlatformNameAr,
        string? EmployeeNameAr, string? SponsorNameAr, string CityNameAr);
    private sealed record RegistrationProjection(PlatformAccountRegistration Item, string EmployeeNameAr,
        string PlatformNameAr, string ContractNameAr, string? SponsorNameAr, string CityNameAr);
    private sealed record AssignmentProjection(RiderClientAssignment Item, string EmployeeNameAr, string ContractNameAr, string ExternalAccountId);
}
