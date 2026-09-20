using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public sealed record LegalCaseUpsertRequest(
    string CaseNumber,
    string PersonType,
    string? PersonName,
    Guid? EmployeeId,
    Guid? RiderProfileId,
    Guid SponsorId,
    string SponsorPartyRole,
    DateOnly CaseDate,
    TimeOnly CaseTime,
    string Status,
    string Details,
    string? Notes,
    Guid ResponsibleUserId,
    string? RowVersion = null,
    string? ChangeReason = null);

public sealed record LegalCaseHearingUpsertRequest(
    DateOnly HearingDate,
    TimeOnly HearingTime,
    string Status,
    string Details,
    string? Notes,
    string? Location,
    string? RowVersion = null,
    string? ChangeReason = null);

public sealed record LegalCaseArchiveRequest(string RowVersion, string Reason);
public sealed record LegalCaseFileUpload(string? Description, PrivateFileUpload File);

public sealed record LegalCasePartyResponse(
    string Role,
    string PartyType,
    string Name,
    Guid? SponsorId,
    Guid? EmployeeId,
    Guid? RiderProfileId);

public sealed record LegalCaseHearingFileResponse(
    Guid Id,
    Guid HearingId,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string Sha256Checksum,
    string? Description,
    Guid UploadedByUserId,
    DateTimeOffset UploadedAtUtc,
    string RowVersion);

public sealed record LegalCaseHearingResponse(
    Guid Id,
    Guid LegalCaseId,
    int HearingNumber,
    DateOnly HearingDate,
    TimeOnly HearingTime,
    string Status,
    string Details,
    string? Notes,
    string? Location,
    IReadOnlyList<LegalCaseHearingFileResponse> Files,
    string RowVersion);

public sealed record LegalCaseSummaryResponse(
    Guid Id,
    string CaseNumber,
    string PersonName,
    string SponsorName,
    string SponsorPartyRole,
    DateOnly CaseDate,
    TimeOnly CaseTime,
    string Status,
    Guid ResponsibleUserId,
    int HearingCount,
    string RowVersion);

public sealed record LegalCaseResponse(
    Guid Id,
    string CaseNumber,
    LegalCasePartyResponse Claimant,
    LegalCasePartyResponse Defendant,
    DateOnly CaseDate,
    TimeOnly CaseTime,
    string Status,
    string Details,
    string? Notes,
    Guid ResponsibleUserId,
    IReadOnlyList<LegalCaseHearingResponse> Hearings,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string RowVersion);

public sealed record LegalCaseHistoryResponse(
    Guid Id,
    Guid LegalCaseId,
    Guid? HearingId,
    string ChangeType,
    IReadOnlyList<string> ChangedFields,
    string? BeforeJson,
    string AfterJson,
    string ChangeReason,
    Guid? ChangedByUserId,
    DateTimeOffset ChangedAtUtc);

public sealed record LegalCasePageResponse(
    IReadOnlyList<LegalCaseSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record LegalCaseQuery(
    string? Search,
    string? Status,
    Guid? SponsorId,
    Guid? EmployeeId,
    Guid? RiderProfileId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 50);

public interface ILegalCaseService
{
    Task<Result<LegalCasePageResponse>> GetAsync(LegalCaseQuery query, CancellationToken cancellationToken = default);
    Task<Result<LegalCaseResponse>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<LegalCaseResponse>> CreateAsync(LegalCaseUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<LegalCaseResponse>> UpdateAsync(Guid id, LegalCaseUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(Guid id, LegalCaseArchiveRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<LegalCaseHistoryResponse>>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<LegalCaseHearingResponse>> CreateHearingAsync(Guid caseId, LegalCaseHearingUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<LegalCaseHearingResponse>> UpdateHearingAsync(Guid caseId, Guid hearingId, LegalCaseHearingUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveHearingAsync(Guid caseId, Guid hearingId, LegalCaseArchiveRequest request, CancellationToken cancellationToken = default);
    Task<Result<LegalCaseHearingFileResponse>> UploadFileAsync(Guid caseId, Guid hearingId, LegalCaseFileUpload request, CancellationToken cancellationToken = default);
    Task<Result<PrivateFileDownload>> DownloadFileAsync(Guid caseId, Guid hearingId, Guid fileId, CancellationToken cancellationToken = default);
    Task<Result> ArchiveFileAsync(Guid caseId, Guid hearingId, Guid fileId, LegalCaseArchiveRequest request, CancellationToken cancellationToken = default);
}

public static class LegalCaseErrors
{
    public static readonly OperationError NotFound = new("legal_cases.not_found", "القضية أو الجلسة المطلوبة غير موجودة.", ErrorType.NotFound);
    public static readonly OperationError InvalidRequest = new("legal_cases.invalid_request", "بيانات القضية أو الجلسة غير صحيحة.", ErrorType.Validation);
    public static readonly OperationError DuplicateCaseNumber = new("legal_cases.duplicate_case_number", "رقم القضية مستخدم مسبقاً.", ErrorType.Conflict, "caseNumber");
    public static readonly OperationError InvalidPerson = new("legal_cases.invalid_person", "يجب تحديد موظف أو رايدر صالح، أو إدخال اسم شخص خارجي.", ErrorType.Validation, "personType");
    public static readonly OperationError SponsorNotFound = new("legal_cases.sponsor_not_found", "الكفيل المحدد غير موجود أو غير فعال.", ErrorType.Validation, "sponsorId");
    public static readonly OperationError ResponsibleUserNotFound = new("legal_cases.responsible_user_not_found", "المستخدم المسؤول غير موجود أو غير فعال.", ErrorType.Validation, "responsibleUserId");
    public static readonly OperationError FileLimitReached = new("legal_cases.hearing_file_limit", "الحد الأعلى هو خمسة ملفات فعالة لكل جلسة.", ErrorType.Conflict, "file");
    public static readonly OperationError Conflict = new("legal_cases.conflict", "تعذر تنفيذ العملية بسبب تعارض في البيانات.", ErrorType.Conflict);
    public static readonly OperationError ConcurrencyConflict = new("legal_cases.concurrency_conflict", "تم تعديل السجل بواسطة مستخدم آخر. أعد تحميل البيانات وحاول مرة أخرى.", ErrorType.Conflict, "rowVersion");
}
