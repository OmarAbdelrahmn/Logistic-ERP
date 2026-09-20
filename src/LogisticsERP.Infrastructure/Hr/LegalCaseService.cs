using System.Data;
using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class LegalCaseService(
    ApplicationDbContext dbContext,
    IdentityDbContext identityDbContext,
    ICurrentUser currentUser,
    IPrivateFileStorage fileStorage,
    TimeProvider timeProvider) : ILegalCaseService
{
    internal const int MaximumFilesPerHearing = 5;
    internal const long MaximumFileBytes = 10 * 1024 * 1024;
    private const string CaseSourceType = "HrLegalCase";
    private const string HearingSourceType = "HrLegalCaseHearing";
    private static readonly TimeSpan RiyadhOffset = TimeSpan.FromHours(3);

    public async Task<Result<LegalCasePageResponse>> GetAsync(
        LegalCaseQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 200 || query.FromDate > query.ToDate)
            return Result.Failure<LegalCasePageResponse>(LegalCaseErrors.InvalidRequest);

        LegalCaseStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!TryParseEnum(query.Status, out LegalCaseStatus parsedStatus))
                return Result.Failure<LegalCasePageResponse>(LegalCaseErrors.InvalidRequest);
            status = parsedStatus;
        }

        var rows = dbContext.HrLegalCases.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(x => x.CaseNumber.Contains(search) || x.PersonName.Contains(search));
        }
        if (status.HasValue) rows = rows.Where(x => x.Status == status.Value);
        if (query.SponsorId.HasValue) rows = rows.Where(x => x.SponsorId == query.SponsorId.Value);
        if (query.EmployeeId.HasValue) rows = rows.Where(x => x.EmployeeId == query.EmployeeId.Value);
        if (query.RiderProfileId.HasValue) rows = rows.Where(x => x.RiderProfileId == query.RiderProfileId.Value);
        if (query.FromDate.HasValue) rows = rows.Where(x => x.CaseDate >= query.FromDate.Value);
        if (query.ToDate.HasValue) rows = rows.Where(x => x.CaseDate <= query.ToDate.Value);

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .OrderBy(x => x.CaseDate).ThenBy(x => x.CaseTime).ThenBy(x => x.CaseNumber)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ToArrayAsync(cancellationToken);
        var sponsorIds = page.Select(x => x.SponsorId).Distinct().ToArray();
        var caseIds = page.Select(x => x.Id).ToArray();
        var sponsors = await dbContext.Sponsors.AsNoTracking().Where(x => sponsorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.RegistryNameAr, cancellationToken);
        var hearingCounts = await dbContext.HrLegalCaseHearings.AsNoTracking().Where(x => caseIds.Contains(x.LegalCaseId))
            .GroupBy(x => x.LegalCaseId).Select(x => new { CaseId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.CaseId, x => x.Count, cancellationToken);

        var items = page.Select(x => new LegalCaseSummaryResponse(
            x.Id, x.CaseNumber, x.PersonName, sponsors.GetValueOrDefault(x.SponsorId, string.Empty),
            x.SponsorPartyRole.ToString(), x.CaseDate, x.CaseTime, x.Status.ToString(), x.ResponsibleUserId,
            hearingCounts.GetValueOrDefault(x.Id), HrServiceSupport.EncodeRowVersion(x.RowVersion))).ToArray();
        return Result.Success(new LegalCasePageResponse(items, query.Page, query.PageSize, total));
    }

    public async Task<Result<LegalCaseResponse>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await BuildCaseResponseAsync(id, cancellationToken);
        return response is null
            ? Result.Failure<LegalCaseResponse>(LegalCaseErrors.NotFound)
            : Result.Success(response);
    }

    public async Task<Result<LegalCaseResponse>> CreateAsync(
        LegalCaseUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var validated = await ValidateCaseRequestAsync(null, request, cancellationToken);
        if (validated.IsFailure) return Result.Failure<LegalCaseResponse>(validated.Error);
        var data = validated.Value!;
        var item = new HrLegalCase
        {
            CaseNumber = request.CaseNumber.Trim(),
            PersonName = data.PersonName,
            PersonType = data.PersonType,
            EmployeeId = data.EmployeeId,
            RiderProfileId = data.RiderProfileId,
            SponsorId = request.SponsorId,
            SponsorPartyRole = data.SponsorPartyRole,
            CaseDate = request.CaseDate,
            CaseTime = request.CaseTime,
            Status = data.Status,
            Details = request.Details.Trim(),
            Notes = HrServiceSupport.TrimOrNull(request.Notes),
            ResponsibleUserId = request.ResponsibleUserId
        };
        dbContext.HrLegalCases.Add(item);
        var history = CreateHistory(item.Id, null, "Created", null, SerializeCase(item), CaseFields,
            string.IsNullOrWhiteSpace(request.ChangeReason) ? "Case created." : request.ChangeReason.Trim());
        dbContext.HrLegalCaseHistory.Add(history);
        QueueImmediate(item, history.Id, "legal_case.created", "تم إنشاء قضية", "Legal case created");
        await SyncCaseRemindersAsync(item, null, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Failure<LegalCaseResponse>(LegalCaseErrors.DuplicateCaseNumber);
        }
        return Result.Success((await BuildCaseResponseAsync(item.Id, cancellationToken))!);
    }

    public async Task<Result<LegalCaseResponse>> UpdateAsync(
        Guid id,
        LegalCaseUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ChangeReason))
            return Result.Failure<LegalCaseResponse>(LegalCaseErrors.InvalidRequest);
        var item = await dbContext.HrLegalCases.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return Result.Failure<LegalCaseResponse>(LegalCaseErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion))
            return Result.Failure<LegalCaseResponse>(LegalCaseErrors.ConcurrencyConflict);
        var validated = await ValidateCaseRequestAsync(id, request, cancellationToken);
        if (validated.IsFailure) return Result.Failure<LegalCaseResponse>(validated.Error);
        var data = validated.Value!;
        var before = SerializeCase(item);
        var oldValues = CaseValues(item);
        var previousResponsibleUserId = item.ResponsibleUserId;
        item.CaseNumber = request.CaseNumber.Trim();
        item.PersonName = data.PersonName;
        item.PersonType = data.PersonType;
        item.EmployeeId = data.EmployeeId;
        item.RiderProfileId = data.RiderProfileId;
        item.SponsorId = request.SponsorId;
        item.SponsorPartyRole = data.SponsorPartyRole;
        item.CaseDate = request.CaseDate;
        item.CaseTime = request.CaseTime;
        item.Status = data.Status;
        item.Details = request.Details.Trim();
        item.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        item.ResponsibleUserId = request.ResponsibleUserId;
        var changedFields = ChangedFields(oldValues, CaseValues(item));
        if (changedFields.Length == 0)
            return Result.Success((await BuildCaseResponseAsync(item.Id, cancellationToken))!);
        var history = CreateHistory(item.Id, null, "Updated", before, SerializeCase(item), changedFields, request.ChangeReason.Trim());
        dbContext.HrLegalCaseHistory.Add(history);
        QueueImmediate(item, history.Id, "legal_case.updated", "تم تحديث القضية", "Legal case updated");
        await SyncCaseRemindersAsync(item, previousResponsibleUserId, cancellationToken);
        await SyncHearingRemindersForCaseAsync(item, previousResponsibleUserId, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<LegalCaseResponse>(LegalCaseErrors.ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            return Result.Failure<LegalCaseResponse>(LegalCaseErrors.DuplicateCaseNumber);
        }
        return Result.Success((await BuildCaseResponseAsync(item.Id, cancellationToken))!);
    }

    public async Task<Result> ArchiveAsync(Guid id, LegalCaseArchiveRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure(LegalCaseErrors.InvalidRequest);
        var item = await dbContext.HrLegalCases.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return Result.Failure(LegalCaseErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion)) return Result.Failure(LegalCaseErrors.ConcurrencyConflict);
        var before = SerializeCase(item);
        item.IsDeleted = true;
        item.DeletionReason = request.Reason.Trim();
        var history = CreateHistory(item.Id, null, "Archived", before, SerializeCase(item), ["IsDeleted"], request.Reason.Trim());
        dbContext.HrLegalCaseHistory.Add(history);
        QueueImmediate(item, history.Id, "legal_case.archived", "تمت أرشفة القضية", "Legal case archived");
        await ArchiveRemindersAsync(CaseSourceType, item.Id, cancellationToken);
        var hearingIds = await dbContext.HrLegalCaseHearings.Where(x => x.LegalCaseId == item.Id).Select(x => x.Id).ToArrayAsync(cancellationToken);
        foreach (var hearingId in hearingIds) await ArchiveRemindersAsync(HearingSourceType, hearingId, cancellationToken);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Result.Failure(LegalCaseErrors.ConcurrencyConflict); }
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<LegalCaseHistoryResponse>>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.HrLegalCases.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken))
            return Result.Failure<IReadOnlyList<LegalCaseHistoryResponse>>(LegalCaseErrors.NotFound);
        var rows = await dbContext.HrLegalCaseHistory.AsNoTracking().Where(x => x.LegalCaseId == id)
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<LegalCaseHistoryResponse>>(rows.Select(ToHistoryResponse).ToArray());
    }

    public async Task<Result<LegalCaseHearingResponse>> CreateHearingAsync(
        Guid caseId,
        LegalCaseHearingUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var legalCase = await dbContext.HrLegalCases.SingleOrDefaultAsync(x => x.Id == caseId, cancellationToken);
        if (legalCase is null) return Result.Failure<LegalCaseHearingResponse>(LegalCaseErrors.NotFound);
        if (!ValidateHearingRequest(request, creating: true, out var status))
            return Result.Failure<LegalCaseHearingResponse>(LegalCaseErrors.InvalidRequest);
        var nextNumber = (await dbContext.HrLegalCaseHearings.Where(x => x.LegalCaseId == caseId)
            .MaxAsync(x => (int?)x.HearingNumber, cancellationToken) ?? 0) + 1;
        var hearing = new HrLegalCaseHearing
        {
            LegalCaseId = caseId,
            HearingNumber = nextNumber,
            HearingDate = request.HearingDate,
            HearingTime = request.HearingTime,
            Status = status,
            Details = request.Details.Trim(),
            Notes = HrServiceSupport.TrimOrNull(request.Notes),
            Location = HrServiceSupport.TrimOrNull(request.Location)
        };
        dbContext.HrLegalCaseHearings.Add(hearing);
        var history = CreateHistory(caseId, hearing.Id, "HearingCreated", null, SerializeHearing(hearing), HearingFields,
            string.IsNullOrWhiteSpace(request.ChangeReason) ? "Hearing created." : request.ChangeReason.Trim());
        dbContext.HrLegalCaseHistory.Add(history);
        QueueHearingImmediate(legalCase, hearing, history.Id, "legal_case.hearing.created", "تمت إضافة جلسة", "Legal case hearing added");
        await SyncHearingRemindersAsync(legalCase, hearing, cancellationToken);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Result.Failure<LegalCaseHearingResponse>(LegalCaseErrors.Conflict); }
        return Result.Success(await BuildHearingResponseAsync(hearing, cancellationToken));
    }

    public async Task<Result<LegalCaseHearingResponse>> UpdateHearingAsync(
        Guid caseId,
        Guid hearingId,
        LegalCaseHearingUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateHearingRequest(request, creating: false, out var status))
            return Result.Failure<LegalCaseHearingResponse>(LegalCaseErrors.InvalidRequest);
        var legalCase = await dbContext.HrLegalCases.SingleOrDefaultAsync(x => x.Id == caseId, cancellationToken);
        var hearing = await dbContext.HrLegalCaseHearings.SingleOrDefaultAsync(x => x.Id == hearingId && x.LegalCaseId == caseId, cancellationToken);
        if (legalCase is null || hearing is null) return Result.Failure<LegalCaseHearingResponse>(LegalCaseErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(hearing.RowVersion, request.RowVersion))
            return Result.Failure<LegalCaseHearingResponse>(LegalCaseErrors.ConcurrencyConflict);
        var before = SerializeHearing(hearing);
        var oldValues = HearingValues(hearing);
        hearing.HearingDate = request.HearingDate;
        hearing.HearingTime = request.HearingTime;
        hearing.Status = status;
        hearing.Details = request.Details.Trim();
        hearing.Notes = HrServiceSupport.TrimOrNull(request.Notes);
        hearing.Location = HrServiceSupport.TrimOrNull(request.Location);
        var changedFields = ChangedFields(oldValues, HearingValues(hearing));
        if (changedFields.Length == 0) return Result.Success(await BuildHearingResponseAsync(hearing, cancellationToken));
        var history = CreateHistory(caseId, hearing.Id, "HearingUpdated", before, SerializeHearing(hearing), changedFields, request.ChangeReason!.Trim());
        dbContext.HrLegalCaseHistory.Add(history);
        QueueHearingImmediate(legalCase, hearing, history.Id, "legal_case.hearing.updated", "تم تحديث الجلسة", "Legal case hearing updated");
        await SyncHearingRemindersAsync(legalCase, hearing, cancellationToken);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Result.Failure<LegalCaseHearingResponse>(LegalCaseErrors.ConcurrencyConflict); }
        return Result.Success(await BuildHearingResponseAsync(hearing, cancellationToken));
    }

    public async Task<Result> ArchiveHearingAsync(Guid caseId, Guid hearingId, LegalCaseArchiveRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure(LegalCaseErrors.InvalidRequest);
        var hearing = await dbContext.HrLegalCaseHearings.SingleOrDefaultAsync(x => x.Id == hearingId && x.LegalCaseId == caseId, cancellationToken);
        if (hearing is null) return Result.Failure(LegalCaseErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(hearing.RowVersion, request.RowVersion)) return Result.Failure(LegalCaseErrors.ConcurrencyConflict);
        var before = SerializeHearing(hearing);
        hearing.IsDeleted = true;
        hearing.DeletionReason = request.Reason.Trim();
        dbContext.HrLegalCaseHistory.Add(CreateHistory(caseId, hearing.Id, "HearingArchived", before, SerializeHearing(hearing), ["IsDeleted"], request.Reason.Trim()));
        await ArchiveRemindersAsync(HearingSourceType, hearing.Id, cancellationToken);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Result.Failure(LegalCaseErrors.ConcurrencyConflict); }
        return Result.Success();
    }

    public async Task<Result<LegalCaseHearingFileResponse>> UploadFileAsync(
        Guid caseId,
        Guid hearingId,
        LegalCaseFileUpload request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure<LegalCaseHearingFileResponse>(HrErrors.CurrentUserUnavailable);
        if (!await HearingExistsAsync(caseId, hearingId, cancellationToken))
            return Result.Failure<LegalCaseHearingFileResponse>(LegalCaseErrors.NotFound);
        if (await dbContext.HrLegalCaseHearingFiles.CountAsync(x => x.HearingId == hearingId, cancellationToken) >= MaximumFilesPerHearing)
            return Result.Failure<LegalCaseHearingFileResponse>(LegalCaseErrors.FileLimitReached);

        var fileId = Guid.CreateVersion7();
        var stored = await fileStorage.StoreAsync(
            $"hr/legal-cases/{caseId:N}/hearings/{hearingId:N}/{fileId:N}", request.File, MaximumFileBytes, cancellationToken);
        if (stored.IsFailure) return Result.Failure<LegalCaseHearingFileResponse>(stored.Error);
        var value = stored.Value!;
        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        if (await dbContext.HrLegalCaseHearingFiles.CountAsync(x => x.HearingId == hearingId, cancellationToken) >= MaximumFilesPerHearing)
        {
            fileStorage.DeleteBestEffort(value.StoragePath);
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<LegalCaseHearingFileResponse>(LegalCaseErrors.FileLimitReached);
        }
        var file = new HrLegalCaseHearingFile
        {
            Id = fileId,
            HearingId = hearingId,
            OriginalFileName = value.OriginalFileName,
            StoredFileName = value.StoredFileName,
            StoragePath = value.StoragePath,
            ContentType = value.ContentType,
            FileSizeBytes = value.Length,
            Sha256Checksum = value.Sha256Checksum,
            Description = HrServiceSupport.TrimOrNull(request.Description),
            UploadedByUserId = userId,
            UploadedAtUtc = timeProvider.GetUtcNow()
        };
        dbContext.HrLegalCaseHearingFiles.Add(file);
        dbContext.HrLegalCaseHistory.Add(CreateHistory(caseId, hearingId, "HearingFileUploaded", null,
            SerializeFile(file), ["File"], $"File uploaded: {file.OriginalFileName}"));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            fileStorage.DeleteBestEffort(value.StoragePath);
            return Result.Failure<LegalCaseHearingFileResponse>(LegalCaseErrors.Conflict);
        }
        return Result.Success(ToFileResponse(file));
    }

    public async Task<Result<PrivateFileDownload>> DownloadFileAsync(
        Guid caseId,
        Guid hearingId,
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        if (!await HearingExistsAsync(caseId, hearingId, cancellationToken))
            return Result.Failure<PrivateFileDownload>(LegalCaseErrors.NotFound);
        var file = await dbContext.HrLegalCaseHearingFiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == fileId && x.HearingId == hearingId, cancellationToken);
        return file is null
            ? Result.Failure<PrivateFileDownload>(LegalCaseErrors.NotFound)
            : await fileStorage.OpenReadAsync(file.StoragePath, file.ContentType, file.OriginalFileName, file.FileSizeBytes, cancellationToken);
    }

    public async Task<Result> ArchiveFileAsync(
        Guid caseId,
        Guid hearingId,
        Guid fileId,
        LegalCaseArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || !await HearingExistsAsync(caseId, hearingId, cancellationToken))
            return Result.Failure(LegalCaseErrors.InvalidRequest);
        var file = await dbContext.HrLegalCaseHearingFiles.SingleOrDefaultAsync(x => x.Id == fileId && x.HearingId == hearingId, cancellationToken);
        if (file is null) return Result.Failure(LegalCaseErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(file.RowVersion, request.RowVersion)) return Result.Failure(LegalCaseErrors.ConcurrencyConflict);
        var before = SerializeFile(file);
        file.IsDeleted = true;
        file.DeletionReason = request.Reason.Trim();
        dbContext.HrLegalCaseHistory.Add(CreateHistory(caseId, hearingId, "HearingFileArchived", before,
            SerializeFile(file), ["IsDeleted"], request.Reason.Trim()));
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Result.Failure(LegalCaseErrors.ConcurrencyConflict); }
        return Result.Success();
    }

    private async Task<Result<ValidatedCase>> ValidateCaseRequestAsync(Guid? id, LegalCaseUpsertRequest request, CancellationToken ct)
    {
        if (!HrServiceSupport.HasText(request.CaseNumber) || request.CaseNumber.Trim().Length > 100
            || !HrServiceSupport.HasText(request.Details) || request.Details.Trim().Length > 8000
            || request.CaseDate == default
            || !TryParseEnum(request.PersonType, out LegalCasePersonType personType)
            || !TryParseEnum(request.SponsorPartyRole, out LegalCasePartyRole sponsorRole)
            || !TryParseEnum(request.Status, out LegalCaseStatus status))
            return Result.Failure<ValidatedCase>(LegalCaseErrors.InvalidRequest);
        var caseNumber = request.CaseNumber.Trim();
        if (await dbContext.HrLegalCases.AsNoTracking().AnyAsync(x => x.CaseNumber == caseNumber && x.Id != id, ct))
            return Result.Failure<ValidatedCase>(LegalCaseErrors.DuplicateCaseNumber);
        if (!await dbContext.Sponsors.AsNoTracking().AnyAsync(x => x.Id == request.SponsorId && x.Status == CatalogStatus.Active, ct))
            return Result.Failure<ValidatedCase>(LegalCaseErrors.SponsorNotFound);
        if (!await identityDbContext.Users.AsNoTracking().AnyAsync(x => x.Id == request.ResponsibleUserId && !x.IsDeleted && x.Status == UserAccountStatus.Active, ct))
            return Result.Failure<ValidatedCase>(LegalCaseErrors.ResponsibleUserNotFound);

        string? personName = null;
        Guid? employeeId = null;
        Guid? riderProfileId = null;
        switch (personType)
        {
            case LegalCasePersonType.Employee when request.EmployeeId.HasValue && !request.RiderProfileId.HasValue:
                employeeId = request.EmployeeId;
                personName = await dbContext.Employees.AsNoTracking().Where(x => x.Id == employeeId.Value)
                    .Select(x => x.FullNameAr).SingleOrDefaultAsync(ct);
                break;
            case LegalCasePersonType.Rider when request.RiderProfileId.HasValue && !request.EmployeeId.HasValue:
                riderProfileId = request.RiderProfileId;
                personName = await (from rider in dbContext.RiderProfiles.AsNoTracking()
                    join employee in dbContext.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
                    where rider.Id == riderProfileId.Value
                    select employee.FullNameAr).SingleOrDefaultAsync(ct);
                break;
            case LegalCasePersonType.External when !request.EmployeeId.HasValue && !request.RiderProfileId.HasValue && HrServiceSupport.HasText(request.PersonName):
                personName = request.PersonName!.Trim();
                break;
        }
        if (string.IsNullOrWhiteSpace(personName) || personName.Length > 250)
            return Result.Failure<ValidatedCase>(LegalCaseErrors.InvalidPerson);
        return Result.Success(new ValidatedCase(personType, personName, employeeId, riderProfileId, sponsorRole, status));
    }

    private static bool ValidateHearingRequest(LegalCaseHearingUpsertRequest request, bool creating, out LegalCaseHearingStatus status)
    {
        status = default;
        return request.HearingDate != default && HrServiceSupport.HasText(request.Details)
            && request.Details.Trim().Length <= 8000
            && TryParseEnum(request.Status, out status)
            && (creating || HrServiceSupport.HasText(request.ChangeReason));
    }

    private async Task<LegalCaseResponse?> BuildCaseResponseAsync(Guid id, CancellationToken ct)
    {
        var item = await dbContext.HrLegalCases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return null;
        var sponsorName = await dbContext.Sponsors.AsNoTracking().Where(x => x.Id == item.SponsorId)
            .Select(x => x.RegistryNameAr).SingleAsync(ct);
        var hearings = await dbContext.HrLegalCaseHearings.AsNoTracking().Where(x => x.LegalCaseId == id)
            .OrderBy(x => x.HearingNumber).ToArrayAsync(ct);
        var hearingIds = hearings.Select(x => x.Id).ToArray();
        var files = await dbContext.HrLegalCaseHearingFiles.AsNoTracking().Where(x => hearingIds.Contains(x.HearingId))
            .OrderBy(x => x.UploadedAtUtc).ToArrayAsync(ct);
        var hearingResponses = hearings.Select(x => ToHearingResponse(x, files.Where(f => f.HearingId == x.Id))).ToArray();
        var sponsorParty = new LegalCasePartyResponse(item.SponsorPartyRole.ToString(), "Sponsor", sponsorName, item.SponsorId, null, null);
        var personRole = item.SponsorPartyRole == LegalCasePartyRole.Claimant ? LegalCasePartyRole.Defendant : LegalCasePartyRole.Claimant;
        var personParty = new LegalCasePartyResponse(personRole.ToString(), item.PersonType.ToString(), item.PersonName, null, item.EmployeeId, item.RiderProfileId);
        return new LegalCaseResponse(item.Id, item.CaseNumber,
            item.SponsorPartyRole == LegalCasePartyRole.Claimant ? sponsorParty : personParty,
            item.SponsorPartyRole == LegalCasePartyRole.Defendant ? sponsorParty : personParty,
            item.CaseDate, item.CaseTime, item.Status.ToString(), item.Details, item.Notes, item.ResponsibleUserId,
            hearingResponses, item.CreatedAtUtc, item.UpdatedAtUtc, HrServiceSupport.EncodeRowVersion(item.RowVersion));
    }

    private async Task<LegalCaseHearingResponse> BuildHearingResponseAsync(HrLegalCaseHearing hearing, CancellationToken ct)
    {
        var files = await dbContext.HrLegalCaseHearingFiles.AsNoTracking().Where(x => x.HearingId == hearing.Id)
            .OrderBy(x => x.UploadedAtUtc).ToArrayAsync(ct);
        return ToHearingResponse(hearing, files);
    }

    private static LegalCaseHearingResponse ToHearingResponse(HrLegalCaseHearing hearing, IEnumerable<HrLegalCaseHearingFile> files) => new(
        hearing.Id, hearing.LegalCaseId, hearing.HearingNumber, hearing.HearingDate, hearing.HearingTime,
        hearing.Status.ToString(), hearing.Details, hearing.Notes, hearing.Location,
        files.Select(ToFileResponse).ToArray(), HrServiceSupport.EncodeRowVersion(hearing.RowVersion));

    private static LegalCaseHearingFileResponse ToFileResponse(HrLegalCaseHearingFile file) => new(
        file.Id, file.HearingId, file.OriginalFileName, file.ContentType, file.FileSizeBytes, file.Sha256Checksum,
        file.Description, file.UploadedByUserId, file.UploadedAtUtc, HrServiceSupport.EncodeRowVersion(file.RowVersion));

    private static LegalCaseHistoryResponse ToHistoryResponse(HrLegalCaseHistory item) => new(
        item.Id, item.LegalCaseId, item.HearingId, item.ChangeType,
        JsonSerializer.Deserialize<string[]>(item.ChangedFieldsJson) ?? [], item.BeforeJson, item.AfterJson,
        item.ChangeReason, item.CreatedByUserId, item.CreatedAtUtc);

    private async Task<bool> HearingExistsAsync(Guid caseId, Guid hearingId, CancellationToken ct) =>
        await dbContext.HrLegalCases.AsNoTracking().AnyAsync(x => x.Id == caseId, ct)
        && await dbContext.HrLegalCaseHearings.AsNoTracking().AnyAsync(x => x.Id == hearingId && x.LegalCaseId == caseId, ct);

    private void QueueImmediate(HrLegalCase item, Guid eventId, string eventType, string titleAr, string titleEn)
    {
        dbContext.Notifications.Add(new Notification
        {
            RecipientUserId = item.ResponsibleUserId,
            EventType = eventType,
            Severity = NotificationSeverity.Information,
            TitleAr = titleAr,
            TitleEn = titleEn,
            BodyAr = $"القضية رقم {item.CaseNumber}: {TrimBody(item.Details)}",
            BodyEn = $"Case {item.CaseNumber}: {TrimBody(item.Details)}",
            SourceEntityType = CaseSourceType,
            SourceEntityId = item.Id,
            DeepLink = $"/hr/legal-cases/{item.Id}",
            AudiencePermissionKeysJson = JsonSerializer.Serialize(new[] { PermissionKeys.Workflows.LegalCasesRead }),
            DeduplicationKey = $"legal-case:{item.Id:N}:event:{eventId:N}",
            VisibleAtUtc = timeProvider.GetUtcNow()
        });
    }

    private void QueueHearingImmediate(HrLegalCase item, HrLegalCaseHearing hearing, Guid eventId, string eventType, string titleAr, string titleEn)
    {
        dbContext.Notifications.Add(new Notification
        {
            RecipientUserId = item.ResponsibleUserId,
            EventType = eventType,
            Severity = NotificationSeverity.Information,
            TitleAr = titleAr,
            TitleEn = titleEn,
            BodyAr = $"القضية {item.CaseNumber}، الجلسة {hearing.HearingNumber}: {TrimBody(hearing.Details)}",
            BodyEn = $"Case {item.CaseNumber}, hearing {hearing.HearingNumber}: {TrimBody(hearing.Details)}",
            SourceEntityType = HearingSourceType,
            SourceEntityId = hearing.Id,
            DeepLink = $"/hr/legal-cases/{item.Id}/hearings/{hearing.Id}",
            AudiencePermissionKeysJson = JsonSerializer.Serialize(new[] { PermissionKeys.Workflows.LegalCasesRead }),
            DeduplicationKey = $"legal-case-hearing:{hearing.Id:N}:event:{eventId:N}",
            VisibleAtUtc = timeProvider.GetUtcNow()
        });
    }

    private async Task SyncCaseRemindersAsync(HrLegalCase item, Guid? previousRecipient, CancellationToken cancellationToken)
    {
        if (previousRecipient.HasValue && previousRecipient != item.ResponsibleUserId)
            await ArchiveRemindersAsync(CaseSourceType, item.Id, cancellationToken, previousRecipient);
        if (item.Status == LegalCaseStatus.Closed || item.IsDeleted)
        {
            await ArchiveRemindersAsync(CaseSourceType, item.Id, cancellationToken);
            return;
        }
        var appointment = ToUtc(item.CaseDate, item.CaseTime);
        await UpsertReminderAsync(item.ResponsibleUserId, CaseSourceType, item.Id, $"legal-case:{item.Id:N}", appointment,
            item.CaseNumber, item.Details, $"/hr/legal-cases/{item.Id}", cancellationToken);
    }

    private async Task SyncHearingRemindersForCaseAsync(HrLegalCase item, Guid previousRecipient, CancellationToken ct)
    {
        var hearings = await dbContext.HrLegalCaseHearings.Where(x => x.LegalCaseId == item.Id).ToArrayAsync(ct);
        foreach (var hearing in hearings)
        {
            if (previousRecipient != item.ResponsibleUserId)
                await ArchiveRemindersAsync(HearingSourceType, hearing.Id, ct, previousRecipient);
            await SyncHearingRemindersAsync(item, hearing, ct);
        }
    }

    private async Task SyncHearingRemindersAsync(HrLegalCase item, HrLegalCaseHearing hearing, CancellationToken ct)
    {
        if (item.Status == LegalCaseStatus.Closed || hearing.Status is LegalCaseHearingStatus.Completed or LegalCaseHearingStatus.Cancelled || hearing.IsDeleted)
        {
            await ArchiveRemindersAsync(HearingSourceType, hearing.Id, ct);
            return;
        }
        await UpsertReminderAsync(item.ResponsibleUserId, HearingSourceType, hearing.Id,
            $"legal-case-hearing:{hearing.Id:N}", ToUtc(hearing.HearingDate, hearing.HearingTime),
            $"{item.CaseNumber} / {hearing.HearingNumber}", hearing.Details,
            $"/hr/legal-cases/{item.Id}/hearings/{hearing.Id}", ct);
    }

    private async Task UpsertReminderAsync(Guid recipientId, string sourceType, Guid sourceId, string keyPrefix,
        DateTimeOffset appointmentUtc, string reference, string details, string deepLink, CancellationToken ct)
    {
        foreach (var (suffix, offset, ar, en) in new[]
        {
            ("24h", TimeSpan.FromHours(24), "تذكير بالموعد قبل 24 ساعة", "Appointment reminder: 24 hours"),
            ("1h", TimeSpan.FromHours(1), "تذكير بالموعد قبل ساعة", "Appointment reminder: 1 hour")
        })
        {
            var key = $"{keyPrefix}:reminder:{suffix}";
            var notification = await dbContext.Notifications.SingleOrDefaultAsync(x => x.RecipientUserId == recipientId && x.DeduplicationKey == key, ct);
            if (appointmentUtc.AddDays(1) <= timeProvider.GetUtcNow())
            {
                if (notification is not null) notification.ArchivedAtUtc ??= timeProvider.GetUtcNow();
                continue;
            }
            notification ??= new Notification { RecipientUserId = recipientId, DeduplicationKey = key };
            if (notification.Id == Guid.Empty || dbContext.Entry(notification).State == EntityState.Detached) dbContext.Notifications.Add(notification);
            notification.EventType = "legal_case.appointment.reminder";
            notification.Severity = NotificationSeverity.Warning;
            notification.TitleAr = ar;
            notification.TitleEn = en;
            notification.BodyAr = $"{reference}: {TrimBody(details)}";
            notification.BodyEn = $"{reference}: {TrimBody(details)}";
            notification.SourceEntityType = sourceType;
            notification.SourceEntityId = sourceId;
            notification.DeepLink = deepLink;
            notification.AudiencePermissionKeysJson = JsonSerializer.Serialize(new[] { PermissionKeys.Workflows.LegalCasesRead });
            notification.VisibleAtUtc = appointmentUtc.Subtract(offset) > timeProvider.GetUtcNow() ? appointmentUtc.Subtract(offset) : timeProvider.GetUtcNow();
            notification.ExpiresAtUtc = appointmentUtc.AddDays(1);
            notification.ArchivedAtUtc = null;
            notification.ArchivedByUserId = null;
        }
    }

    private async Task ArchiveRemindersAsync(string sourceType, Guid sourceId, CancellationToken ct, Guid? recipientId = null)
    {
        var query = dbContext.Notifications.Where(x => x.SourceEntityType == sourceType && x.SourceEntityId == sourceId
            && x.EventType == "legal_case.appointment.reminder" && x.ArchivedAtUtc == null);
        if (recipientId.HasValue) query = query.Where(x => x.RecipientUserId == recipientId.Value);
        var reminders = await query.ToArrayAsync(ct);
        foreach (var reminder in reminders)
        {
            reminder.ArchivedAtUtc = timeProvider.GetUtcNow();
            reminder.ArchivedByUserId = currentUser.UserId;
        }
    }

    private static HrLegalCaseHistory CreateHistory(Guid caseId, Guid? hearingId, string changeType,
        string? before, string after, IReadOnlyList<string> changedFields, string reason) => new()
    {
        LegalCaseId = caseId,
        HearingId = hearingId,
        ChangeType = changeType,
        ChangedFieldsJson = JsonSerializer.Serialize(changedFields),
        BeforeJson = before,
        AfterJson = after,
        ChangeReason = reason
    };

    private static readonly string[] CaseFields =
    [
        "CaseNumber", "PersonName", "PersonType", "EmployeeId", "RiderProfileId", "SponsorId",
        "SponsorPartyRole", "CaseDate", "CaseTime", "Status", "Details", "Notes", "ResponsibleUserId", "IsDeleted"
    ];
    private static readonly string[] HearingFields =
    ["HearingNumber", "HearingDate", "HearingTime", "Status", "Details", "Notes", "Location", "IsDeleted"];

    private static Dictionary<string, object?> CaseValues(HrLegalCase x) => new()
    {
        ["CaseNumber"] = x.CaseNumber, ["PersonName"] = x.PersonName, ["PersonType"] = x.PersonType,
        ["EmployeeId"] = x.EmployeeId, ["RiderProfileId"] = x.RiderProfileId, ["SponsorId"] = x.SponsorId,
        ["SponsorPartyRole"] = x.SponsorPartyRole, ["CaseDate"] = x.CaseDate, ["CaseTime"] = x.CaseTime,
        ["Status"] = x.Status, ["Details"] = x.Details, ["Notes"] = x.Notes, ["ResponsibleUserId"] = x.ResponsibleUserId,
        ["IsDeleted"] = x.IsDeleted
    };
    private static Dictionary<string, object?> HearingValues(HrLegalCaseHearing x) => new()
    {
        ["HearingNumber"] = x.HearingNumber, ["HearingDate"] = x.HearingDate, ["HearingTime"] = x.HearingTime,
        ["Status"] = x.Status, ["Details"] = x.Details, ["Notes"] = x.Notes, ["Location"] = x.Location,
        ["IsDeleted"] = x.IsDeleted
    };
    private static string SerializeCase(HrLegalCase x) => JsonSerializer.Serialize(CaseValues(x));
    private static string SerializeHearing(HrLegalCaseHearing x) => JsonSerializer.Serialize(HearingValues(x));
    private static string SerializeFile(HrLegalCaseHearingFile x) => JsonSerializer.Serialize(new
    {
        x.Id, x.HearingId, x.OriginalFileName, x.ContentType, x.FileSizeBytes, x.Sha256Checksum, x.Description, x.IsDeleted
    });
    private static string[] ChangedFields(Dictionary<string, object?> before, Dictionary<string, object?> after) =>
        before.Where(pair => !Equals(pair.Value, after[pair.Key])).Select(pair => pair.Key).ToArray();
    private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time) => new DateTimeOffset(date.ToDateTime(time), RiyadhOffset).ToUniversalTime();
    private static string TrimBody(string value) => value.Length <= 1500 ? value : value[..1500];
    private static bool TryParseEnum<T>(string? value, out T parsed) where T : struct, Enum =>
        Enum.TryParse(value?.Trim(), true, out parsed) && Enum.IsDefined(parsed);

    private sealed record ValidatedCase(LegalCasePersonType PersonType, string PersonName, Guid? EmployeeId,
        Guid? RiderProfileId, LegalCasePartyRole SponsorPartyRole, LegalCaseStatus Status);
}
