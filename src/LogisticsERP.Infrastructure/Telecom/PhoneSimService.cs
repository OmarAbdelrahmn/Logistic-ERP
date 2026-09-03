using System.Security.Cryptography;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Telecom;
using LogisticsERP.Domain.Entities.Telecom;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Telecom;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Telecom;

internal sealed class PhoneSimService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IPrivateFileStorage fileStorage,
    TimeProvider timeProvider) : IPhoneSimService
{
    private const long MaximumReceiptFormSize = 10 * 1024 * 1024;

    public async Task<Result<PhoneSimPageResponse>> GetAllAsync(
        string? search,
        string? status,
        Guid? responsibleEmployeeId,
        Guid? riderProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = NormalizePage(page, pageSize);
        var query = dbContext.PhoneSimCards.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseStatus(status, out var parsedStatus))
            {
                return Result.Failure<PhoneSimPageResponse>(PhoneSimErrors.InvalidStatus);
            }

            query = query.Where(sim => sim.Status == parsedStatus);
        }

        if (responsibleEmployeeId.HasValue)
        {
            query = query.Where(sim => sim.ResponsibleEmployeeId == responsibleEmployeeId.Value);
        }

        if (riderProfileId.HasValue)
        {
            query = query.Where(sim => dbContext.RiderPhoneSimAssignments.Any(assignment =>
                assignment.PhoneSimCardId == sim.Id
                && assignment.RiderProfileId == riderProfileId.Value
                && assignment.EffectiveTo == null));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var hasNormalizedPhone = PhoneSimRules.TryNormalizePhoneNumber(term, out var normalizedPhone);
            query = query.Where(sim =>
                sim.PhoneNumber.Contains(term)
                || sim.NormalizedPhoneNumber.Contains(term)
                || hasNormalizedPhone && sim.NormalizedPhoneNumber == normalizedPhone
                || sim.Iccid != null && sim.Iccid.Contains(term)
                || sim.CarrierName != null && sim.CarrierName.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await (
            from sim in query
            join responsible in dbContext.Employees.AsNoTracking()
                on sim.ResponsibleEmployeeId equals responsible.Id
            orderby sim.PhoneNumber
            select new PhoneSimProjection(sim, responsible.FullNameAr, responsible.FullNameEn))
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToArrayAsync(cancellationToken);

        var currentRiders = await GetCurrentRidersAsync(
            rows.Select(row => row.Sim.Id).ToArray(),
            cancellationToken);
        var items = rows.Select(row => MapSim(row, currentRiders.GetValueOrDefault(row.Sim.Id))).ToArray();

        return Result.Success(new PhoneSimPageResponse(items, normalizedPage, normalizedPageSize, totalCount));
    }

    public async Task<Result<PhoneSimResponse>> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var projection = await GetProjectionAsync(id, cancellationToken);
        if (projection is null)
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.NotFound);
        }

        var currentRider = (await GetCurrentRidersAsync([id], cancellationToken)).GetValueOrDefault(id);
        return Result.Success(MapSim(projection, currentRider));
    }

    public async Task<Result<PrivateFileDownload>> DownloadReceiptFormAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var sim = await dbContext.PhoneSimCards.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.ReceiptFormStoragePath,
                item.ReceiptFormContentType,
                item.ReceiptFormOriginalFileName,
                item.ReceiptFormSizeBytes
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (sim is null)
        {
            return Result.Failure<PrivateFileDownload>(PhoneSimErrors.NotFound);
        }
        if (string.IsNullOrWhiteSpace(sim.ReceiptFormStoragePath)
            || string.IsNullOrWhiteSpace(sim.ReceiptFormContentType)
            || string.IsNullOrWhiteSpace(sim.ReceiptFormOriginalFileName)
            || !sim.ReceiptFormSizeBytes.HasValue)
        {
            return Result.Failure<PrivateFileDownload>(PhoneSimErrors.ReceiptFormNotFound);
        }

        var file = await fileStorage.OpenReadAsync(
            sim.ReceiptFormStoragePath,
            sim.ReceiptFormContentType,
            sim.ReceiptFormOriginalFileName,
            sim.ReceiptFormSizeBytes.Value,
            cancellationToken);
        return file.IsSuccess
            ? file
            : Result.Failure<PrivateFileDownload>(PhoneSimErrors.ReceiptFormNotFound);
    }

    public async Task<Result<PhoneSimResponse>> CreateAsync(
        CreatePhoneSimRequest request,
        PrivateFileUpload receiptForm,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } actorId)
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.CurrentUserUnavailable);
        }

        var identifiers = NormalizeIdentifiers(request.PhoneNumber, request.Iccid);
        if (identifiers.IsFailure)
        {
            return Result.Failure<PhoneSimResponse>(identifiers.Error);
        }

        if (!ValidOptionalText(request.CarrierName, 200) || !ValidOptionalText(request.Notes, 4000))
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.InvalidRequest);
        }

        var responsibleResult = await ValidateResponsibleEmployeeAsync(
            request.ResponsibleEmployeeId,
            cancellationToken);
        if (responsibleResult.IsFailure)
        {
            return Result.Failure<PhoneSimResponse>(responsibleResult.Error);
        }

        var duplicate = await FindDuplicateAsync(
            Guid.Empty,
            identifiers.Value!.PhoneNumber,
            identifiers.Value.Iccid,
            cancellationToken);
        if (duplicate is not null)
        {
            return Result.Failure<PhoneSimResponse>(duplicate);
        }

        var sim = new PhoneSimCard
        {
            PhoneNumber = identifiers.Value.PhoneNumber,
            NormalizedPhoneNumber = identifiers.Value.PhoneNumber,
            Iccid = identifiers.Value.Iccid,
            NormalizedIccid = identifiers.Value.Iccid,
            CarrierName = TrimOrNull(request.CarrierName),
            ResponsibleEmployeeId = request.ResponsibleEmployeeId,
            Status = PhoneSimStatus.Available,
            StatusReason = "SIM inventory record created.",
            Notes = TrimOrNull(request.Notes)
        };

        var storedReceiptForm = await fileStorage.StoreAsync(
            $"phone-sims/{sim.Id:N}/receipt-form",
            receiptForm,
            MaximumReceiptFormSize,
            cancellationToken);
        if (storedReceiptForm.IsFailure)
        {
            return Result.Failure<PhoneSimResponse>(storedReceiptForm.Error);
        }

        sim.ReceiptFormOriginalFileName = storedReceiptForm.Value!.OriginalFileName;
        sim.ReceiptFormStoredFileName = storedReceiptForm.Value.StoredFileName;
        sim.ReceiptFormContentType = storedReceiptForm.Value.ContentType;
        sim.ReceiptFormSizeBytes = storedReceiptForm.Value.Length;
        sim.ReceiptFormSha256Checksum = storedReceiptForm.Value.Sha256Checksum;
        sim.ReceiptFormStoragePath = storedReceiptForm.Value.StoragePath;

        dbContext.PhoneSimCards.Add(sim);
        dbContext.PhoneSimResponsibilityChanges.Add(new PhoneSimResponsibilityChange
        {
            PhoneSimCardId = sim.Id,
            ResponsibleEmployeeId = request.ResponsibleEmployeeId,
            ChangedAtUtc = timeProvider.GetUtcNow(),
            ChangedByUserId = actorId,
            Reason = "Initial responsible employee assigned when the SIM was created."
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            fileStorage.DeleteBestEffort(storedReceiptForm.Value.StoragePath);
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.PersistenceConflict);
        }
        catch
        {
            fileStorage.DeleteBestEffort(storedReceiptForm.Value.StoragePath);
            throw;
        }

        return await GetAsync(sim.Id, cancellationToken);
    }

    public async Task<Result<PhoneSimResponse>> UpdateAsync(
        Guid id,
        UpdatePhoneSimRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.CurrentUserUnavailable);
        }

        var sim = await dbContext.PhoneSimCards.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (sim is null)
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.NotFound);
        }
        if (!MatchesRowVersion(sim.RowVersion, request.RowVersion))
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.ConcurrencyConflict);
        }

        var identifiers = NormalizeIdentifiers(request.PhoneNumber, request.Iccid);
        if (identifiers.IsFailure)
        {
            return Result.Failure<PhoneSimResponse>(identifiers.Error);
        }
        if (!ValidOptionalText(request.CarrierName, 200) || !ValidOptionalText(request.Notes, 4000))
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.InvalidRequest);
        }

        var duplicate = await FindDuplicateAsync(
            sim.Id,
            identifiers.Value!.PhoneNumber,
            identifiers.Value.Iccid,
            cancellationToken);
        if (duplicate is not null)
        {
            return Result.Failure<PhoneSimResponse>(duplicate);
        }

        sim.PhoneNumber = identifiers.Value.PhoneNumber;
        sim.NormalizedPhoneNumber = identifiers.Value.PhoneNumber;
        sim.Iccid = identifiers.Value.Iccid;
        sim.NormalizedIccid = identifiers.Value.Iccid;
        sim.CarrierName = TrimOrNull(request.CarrierName);
        sim.Notes = TrimOrNull(request.Notes);

        var saveResult = await SaveAsync(cancellationToken);
        return saveResult.IsFailure
            ? Result.Failure<PhoneSimResponse>(saveResult.Error)
            : await GetAsync(id, cancellationToken);
    }

    public async Task<Result<PhoneSimResponse>> ChangeResponsibleEmployeeAsync(
        Guid id,
        ChangePhoneSimResponsibleEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } actorId)
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.CurrentUserUnavailable);
        }
        if (!ValidRequiredText(request.Reason, 1000))
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.InvalidRequest);
        }

        var sim = await dbContext.PhoneSimCards.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (sim is null)
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.NotFound);
        }
        if (!MatchesRowVersion(sim.RowVersion, request.RowVersion))
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.ConcurrencyConflict);
        }
        if (sim.ResponsibleEmployeeId == request.ResponsibleEmployeeId)
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.InvalidRequest);
        }

        var responsibleResult = await ValidateResponsibleEmployeeAsync(
            request.ResponsibleEmployeeId,
            cancellationToken);
        if (responsibleResult.IsFailure)
        {
            return Result.Failure<PhoneSimResponse>(responsibleResult.Error);
        }

        var previousResponsibleEmployeeId = sim.ResponsibleEmployeeId;
        sim.ResponsibleEmployeeId = request.ResponsibleEmployeeId;
        dbContext.PhoneSimResponsibilityChanges.Add(new PhoneSimResponsibilityChange
        {
            PhoneSimCardId = sim.Id,
            PreviousResponsibleEmployeeId = previousResponsibleEmployeeId,
            ResponsibleEmployeeId = request.ResponsibleEmployeeId,
            ChangedAtUtc = timeProvider.GetUtcNow(),
            ChangedByUserId = actorId,
            Reason = request.Reason.Trim()
        });

        var saveResult = await SaveAsync(cancellationToken);
        return saveResult.IsFailure
            ? Result.Failure<PhoneSimResponse>(saveResult.Error)
            : await GetAsync(id, cancellationToken);
    }

    public async Task<Result<PhoneSimResponse>> ChangeStatusAsync(
        Guid id,
        ChangePhoneSimStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.CurrentUserUnavailable);
        }
        if (!TryParseStatus(request.Status, out var status)
            || !PhoneSimRules.CanSetStatusDirectly(status))
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.InvalidStatus);
        }
        if (!ValidRequiredText(request.Reason, 500))
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.InvalidRequest);
        }

        var sim = await dbContext.PhoneSimCards.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (sim is null)
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.NotFound);
        }
        if (!MatchesRowVersion(sim.RowVersion, request.RowVersion))
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.ConcurrencyConflict);
        }
        if (await HasActiveAssignmentAsync(id, cancellationToken))
        {
            return Result.Failure<PhoneSimResponse>(PhoneSimErrors.ActiveAssignmentConflict);
        }

        sim.Status = status;
        sim.StatusReason = request.Reason.Trim();
        var saveResult = await SaveAsync(cancellationToken);
        return saveResult.IsFailure
            ? Result.Failure<PhoneSimResponse>(saveResult.Error)
            : await GetAsync(id, cancellationToken);
    }

    public async Task<Result> ArchiveAsync(
        Guid id,
        ArchivePhoneSimRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure(PhoneSimErrors.CurrentUserUnavailable);
        }
        if (!ValidRequiredText(request.Reason, 500))
        {
            return Result.Failure(PhoneSimErrors.InvalidRequest);
        }

        var sim = await dbContext.PhoneSimCards.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (sim is null)
        {
            return Result.Failure(PhoneSimErrors.NotFound);
        }
        if (!MatchesRowVersion(sim.RowVersion, request.RowVersion))
        {
            return Result.Failure(PhoneSimErrors.ConcurrencyConflict);
        }
        if (await HasActiveAssignmentAsync(id, cancellationToken))
        {
            return Result.Failure(PhoneSimErrors.ActiveAssignmentConflict);
        }

        sim.Status = PhoneSimStatus.Deactivated;
        sim.StatusReason = request.Reason.Trim();
        sim.IsDeleted = true;
        sim.DeletionReason = request.Reason.Trim();
        return await SaveAsync(cancellationToken);
    }

    public async Task<Result<IReadOnlyList<PhoneSimResponsibilityHistoryResponse>>> GetResponsibilityHistoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.PhoneSimCards.AnyAsync(item => item.Id == id, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<PhoneSimResponsibilityHistoryResponse>>(PhoneSimErrors.NotFound);
        }

        var employees = dbContext.Employees.IgnoreQueryFilters().AsNoTracking();
        var rows = await (
            from change in dbContext.PhoneSimResponsibilityChanges.AsNoTracking()
            join responsible in employees on change.ResponsibleEmployeeId equals responsible.Id
            join previous in employees on change.PreviousResponsibleEmployeeId equals previous.Id into previousEmployees
            from previous in previousEmployees.DefaultIfEmpty()
            where change.PhoneSimCardId == id
            orderby change.ChangedAtUtc descending
            select new PhoneSimResponsibilityHistoryResponse(
                change.Id,
                change.PhoneSimCardId,
                change.PreviousResponsibleEmployeeId,
                previous == null ? null : previous.FullNameAr,
                previous == null ? null : previous.FullNameEn,
                change.ResponsibleEmployeeId,
                responsible.FullNameAr,
                responsible.FullNameEn,
                change.ChangedAtUtc,
                change.ChangedByUserId,
                change.Reason))
            .ToArrayAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PhoneSimResponsibilityHistoryResponse>>(rows);
    }

    public async Task<Result<IReadOnlyList<PhoneSimAssignmentResponse>>> GetAssignmentsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.PhoneSimCards.AnyAsync(item => item.Id == id, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<PhoneSimAssignmentResponse>>(PhoneSimErrors.NotFound);
        }

        var rows = await BuildAssignmentQuery(id, null)
            .OrderByDescending(row => row.Assignment.EffectiveFrom)
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PhoneSimAssignmentResponse>>(rows.Select(MapAssignment).ToArray());
    }

    public async Task<Result<PhoneSimAssignmentResponse>> AssignAsync(
        Guid id,
        AssignPhoneSimRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } actorId)
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.CurrentUserUnavailable);
        }
        if (!ValidRequiredText(request.Reason, 1000)
            || !ValidOptionalText(request.Notes, 4000))
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.InvalidRequest);
        }
        if (request.EffectiveFrom > RiyadhToday())
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.InvalidDateRange);
        }

        var sim = await dbContext.PhoneSimCards.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (sim is null)
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.NotFound);
        }
        if (!MatchesRowVersion(sim.RowVersion, request.RowVersion))
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.ConcurrencyConflict);
        }

        var hasCurrentAssignment = await HasActiveAssignmentAsync(id, cancellationToken);
        if (!PhoneSimRules.CanAssign(sim.Status, hasCurrentAssignment))
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.ActiveAssignmentConflict);
        }

        var rider = await (
            from riderProfile in dbContext.RiderProfiles.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking() on riderProfile.EmployeeId equals employee.Id
            where riderProfile.Id == request.RiderProfileId
            select new RiderEligibility(employee.IsEmployee, employee.Status))
            .SingleOrDefaultAsync(cancellationToken);
        if (rider is null)
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.RiderNotFound);
        }
        if (rider.IsEmployee || rider.Status != EmployeeStatus.Active)
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.RiderUnavailable);
        }

        var latestEnd = await dbContext.RiderPhoneSimAssignments
            .Where(item => item.PhoneSimCardId == id && item.EffectiveTo != null)
            .MaxAsync(item => item.EffectiveTo, cancellationToken);
        if (latestEnd.HasValue && request.EffectiveFrom <= latestEnd.Value)
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.AssignmentConflict);
        }

        var assignment = new RiderPhoneSimAssignment
        {
            PhoneSimCardId = id,
            RiderProfileId = request.RiderProfileId,
            EffectiveFrom = request.EffectiveFrom,
            AssignedByUserId = actorId,
            AssignmentReason = request.Reason.Trim(),
            Notes = TrimOrNull(request.Notes)
        };
        dbContext.RiderPhoneSimAssignments.Add(assignment);
        sim.Status = PhoneSimRules.GetStatusAfterAssignment(sim.Status, hasCurrentAssignment);
        sim.StatusReason = "Assigned to a rider.";

        var saveResult = await SaveAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result.Failure<PhoneSimAssignmentResponse>(saveResult.Error);
        }

        return await GetAssignmentAsync(id, assignment.Id, cancellationToken);
    }

    public async Task<Result<PhoneSimAssignmentResponse>> CloseAssignmentAsync(
        Guid id,
        Guid assignmentId,
        ClosePhoneSimAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.CurrentUserUnavailable);
        }
        if (!ValidRequiredText(request.Reason, 1000))
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.InvalidRequest);
        }
        if (request.EffectiveTo > RiyadhToday())
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.InvalidDateRange);
        }

        var sim = await dbContext.PhoneSimCards.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (sim is null)
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.NotFound);
        }
        var assignment = await dbContext.RiderPhoneSimAssignments.SingleOrDefaultAsync(item =>
            item.Id == assignmentId && item.PhoneSimCardId == id,
            cancellationToken);
        if (assignment is null)
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.AssignmentNotFound);
        }
        if (!MatchesRowVersion(assignment.RowVersion, request.RowVersion))
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.ConcurrencyConflict);
        }
        if (assignment.EffectiveTo is not null || request.EffectiveTo < assignment.EffectiveFrom)
        {
            return Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.InvalidDateRange);
        }

        assignment.EffectiveTo = request.EffectiveTo;
        assignment.EndReason = request.Reason.Trim();
        sim.Status = PhoneSimRules.GetStatusAfterRelease(sim.Status);
        sim.StatusReason = "Returned from rider assignment.";

        var saveResult = await SaveAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result.Failure<PhoneSimAssignmentResponse>(saveResult.Error);
        }

        return await GetAssignmentAsync(id, assignmentId, cancellationToken);
    }

    private async Task<Result<PhoneSimAssignmentResponse>> GetAssignmentAsync(
        Guid phoneSimCardId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var row = await BuildAssignmentQuery(phoneSimCardId, assignmentId)
            .SingleOrDefaultAsync(cancellationToken);
        return row is null
            ? Result.Failure<PhoneSimAssignmentResponse>(PhoneSimErrors.AssignmentNotFound)
            : Result.Success(MapAssignment(row));
    }

    private IQueryable<AssignmentProjection> BuildAssignmentQuery(Guid phoneSimCardId, Guid? assignmentId)
    {
        var sims = dbContext.PhoneSimCards.IgnoreQueryFilters().AsNoTracking();
        var riders = dbContext.RiderProfiles.IgnoreQueryFilters().AsNoTracking();
        var employees = dbContext.Employees.IgnoreQueryFilters().AsNoTracking();
        return from assignment in dbContext.RiderPhoneSimAssignments.AsNoTracking()
               join sim in sims on assignment.PhoneSimCardId equals sim.Id
               join rider in riders on assignment.RiderProfileId equals rider.Id
               join employee in employees on rider.EmployeeId equals employee.Id
               where assignment.PhoneSimCardId == phoneSimCardId
                   && (!assignmentId.HasValue || assignment.Id == assignmentId.Value)
               select new AssignmentProjection(assignment, sim.PhoneNumber, rider.EmployeeId,
                   employee.FullNameAr, employee.FullNameEn);
    }

    private async Task<PhoneSimProjection?> GetProjectionAsync(Guid id, CancellationToken cancellationToken) =>
        await (from sim in dbContext.PhoneSimCards.AsNoTracking()
               join responsible in dbContext.Employees.AsNoTracking()
                   on sim.ResponsibleEmployeeId equals responsible.Id
               where sim.Id == id
               select new PhoneSimProjection(sim, responsible.FullNameAr, responsible.FullNameEn))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<Dictionary<Guid, CurrentRiderProjection>> GetCurrentRidersAsync(
        Guid[] phoneSimCardIds,
        CancellationToken cancellationToken)
    {
        if (phoneSimCardIds.Length == 0)
        {
            return [];
        }

        var riders = dbContext.RiderProfiles.IgnoreQueryFilters().AsNoTracking();
        var employees = dbContext.Employees.IgnoreQueryFilters().AsNoTracking();
        var rows = await (
            from assignment in dbContext.RiderPhoneSimAssignments.AsNoTracking()
            join rider in riders on assignment.RiderProfileId equals rider.Id
            join employee in employees on rider.EmployeeId equals employee.Id
            where phoneSimCardIds.Contains(assignment.PhoneSimCardId)
                && assignment.EffectiveTo == null
            select new CurrentRiderProjection(
                assignment.PhoneSimCardId,
                assignment.Id,
                assignment.RiderProfileId,
                rider.EmployeeId,
                employee.FullNameAr,
                employee.FullNameEn,
                assignment.EffectiveFrom,
                assignment.RowVersion))
            .ToArrayAsync(cancellationToken);

        return rows.ToDictionary(row => row.PhoneSimCardId);
    }

    private async Task<Result> ValidateResponsibleEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await dbContext.Employees.AsNoTracking()
            .Where(item => item.Id == employeeId)
            .Select(item => new ResponsibleEligibility(item.IsEmployee, item.Status))
            .SingleOrDefaultAsync(cancellationToken);
        if (employee is null)
        {
            return Result.Failure(PhoneSimErrors.ResponsibleEmployeeNotFound);
        }

        return !employee.IsEmployee || employee.Status is not (EmployeeStatus.Active or EmployeeStatus.OnLeave)
            ? Result.Failure(PhoneSimErrors.ResponsibleEmployeeUnavailable)
            : Result.Success();
    }

    private async Task<OperationError?> FindDuplicateAsync(
        Guid excludedId,
        string phoneNumber,
        string? iccid,
        CancellationToken cancellationToken)
    {
        if (await dbContext.PhoneSimCards.AnyAsync(item =>
            item.Id != excludedId && item.NormalizedPhoneNumber == phoneNumber,
            cancellationToken))
        {
            return PhoneSimErrors.DuplicatePhoneNumber;
        }
        if (iccid is not null && await dbContext.PhoneSimCards.AnyAsync(item =>
            item.Id != excludedId && item.NormalizedIccid == iccid,
            cancellationToken))
        {
            return PhoneSimErrors.DuplicateIccid;
        }

        return null;
    }

    private Task<bool> HasActiveAssignmentAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.RiderPhoneSimAssignments.AnyAsync(item =>
            item.PhoneSimCardId == id && item.EffectiveTo == null,
            cancellationToken);

    private async Task<Result> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(PhoneSimErrors.ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            return Result.Failure(PhoneSimErrors.PersistenceConflict);
        }
    }

    private static Result<NormalizedIdentifiers> NormalizeIdentifiers(string phoneNumber, string? iccid)
    {
        if (!PhoneSimRules.TryNormalizePhoneNumber(phoneNumber, out var normalizedPhoneNumber))
        {
            return Result.Failure<NormalizedIdentifiers>(PhoneSimErrors.InvalidPhoneNumber);
        }

        try
        {
            return Result.Success(new NormalizedIdentifiers(
                normalizedPhoneNumber,
                PhoneSimRules.NormalizeIccid(iccid)));
        }
        catch (ArgumentException)
        {
            return Result.Failure<NormalizedIdentifiers>(PhoneSimErrors.InvalidIccid);
        }
    }

    private DateOnly RiyadhToday() =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);

    private static PhoneSimResponse MapSim(PhoneSimProjection row, CurrentRiderProjection? currentRider) => new(
        row.Sim.Id,
        row.Sim.PhoneNumber,
        row.Sim.Iccid,
        row.Sim.CarrierName,
        row.Sim.Status.ToString(),
        row.Sim.StatusReason,
        row.Sim.ResponsibleEmployeeId,
        row.ResponsibleEmployeeNameAr,
        row.ResponsibleEmployeeNameEn,
        currentRider is null ? null : new PhoneSimCurrentRiderResponse(
            currentRider.AssignmentId,
            currentRider.RiderProfileId,
            currentRider.EmployeeId,
            currentRider.FullNameAr,
            currentRider.FullNameEn,
            currentRider.EffectiveFrom,
            EncodeRowVersion(currentRider.RowVersion)),
        row.Sim.Notes,
        row.Sim.ReceiptFormStoragePath is null ? null : new PhoneSimReceiptFormResponse(
            row.Sim.ReceiptFormOriginalFileName!,
            row.Sim.ReceiptFormContentType!,
            row.Sim.ReceiptFormSizeBytes!.Value,
            row.Sim.ReceiptFormSha256Checksum!),
        row.Sim.CreatedAtUtc,
        row.Sim.UpdatedAtUtc,
        EncodeRowVersion(row.Sim.RowVersion));

    private static PhoneSimAssignmentResponse MapAssignment(AssignmentProjection row) => new(
        row.Assignment.Id,
        row.Assignment.PhoneSimCardId,
        row.PhoneNumber,
        row.Assignment.RiderProfileId,
        row.EmployeeId,
        row.RiderNameAr,
        row.RiderNameEn,
        row.Assignment.EffectiveFrom,
        row.Assignment.EffectiveTo,
        row.Assignment.AssignmentReason,
        row.Assignment.EndReason,
        row.Assignment.Notes,
        row.Assignment.AssignedByUserId,
        row.Assignment.ClosedByUserId,
        EncodeRowVersion(row.Assignment.RowVersion));

    private static bool TryParseStatus(string value, out PhoneSimStatus status) =>
        Enum.TryParse(value, true, out status) && Enum.IsDefined(status);

    private static bool MatchesRowVersion(byte[] current, string? supplied)
    {
        if (string.IsNullOrWhiteSpace(supplied))
        {
            return false;
        }

        try
        {
            return CryptographicOperations.FixedTimeEquals(current, Convert.FromBase64String(supplied));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string EncodeRowVersion(byte[] value) => Convert.ToBase64String(value);
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool ValidRequiredText(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength;
    private static bool ValidOptionalText(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length <= maxLength;
    private static (int Page, int PageSize) NormalizePage(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 200));

    private sealed record PhoneSimProjection(
        PhoneSimCard Sim,
        string ResponsibleEmployeeNameAr,
        string? ResponsibleEmployeeNameEn);

    private sealed record CurrentRiderProjection(
        Guid PhoneSimCardId,
        Guid AssignmentId,
        Guid RiderProfileId,
        Guid EmployeeId,
        string FullNameAr,
        string? FullNameEn,
        DateOnly EffectiveFrom,
        byte[] RowVersion);

    private sealed record AssignmentProjection(
        RiderPhoneSimAssignment Assignment,
        string PhoneNumber,
        Guid EmployeeId,
        string RiderNameAr,
        string? RiderNameEn);

    private sealed record RiderEligibility(bool IsEmployee, EmployeeStatus Status);
    private sealed record ResponsibleEligibility(bool IsEmployee, EmployeeStatus Status);
    private sealed record NormalizedIdentifiers(string PhoneNumber, string? Iccid);
}
