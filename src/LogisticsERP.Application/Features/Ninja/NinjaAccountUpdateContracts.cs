namespace LogisticsERP.Application.Features.Ninja;

public sealed record NinjaAccountUpdateNoteRequest(string? Note, string RowVersion);
public sealed record NinjaAccountUpdateStatusCount(string SheetStatus, int Count);
public sealed record NinjaAccountUpdateSummary(int TotalRows, int FoundRows, int NotFoundRows, int AmbiguousRows,
    int RowsWithProblems, int CityMismatchRows, int IqamaProblemRows, int NaqlBlockedRows, int SuspendedRows);
public sealed record NinjaAccountUpdateUploadResponse(Guid Id, string FileName, string SheetName, string ContentHash,
    string? Note, DateTimeOffset CreatedAtUtc, Guid? CreatedByUserId, DateTimeOffset? UpdatedAtUtc,
    string RowVersion, int IgnoredFooterRows, NinjaAccountUpdateSummary Summary,
    IReadOnlyList<NinjaAccountUpdateStatusCount> SheetStatuses);
public sealed record NinjaAccountUpdatePage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record NinjaAccountUpdateProblem(string Code, string MessageAr, string MessageEn);
public sealed record NinjaAccountUpdateSystemAccount(Guid AccountId, string CaptainId, string AccountCode, string Status,
    Guid? OwnerEmployeeId, string? OwnerNameAr, string? OwnerNameEn, string? OwnerIqamaNo, string? OwnerStatus,
    Guid OperatingCityId, string? CityCode, string? CityNameAr, string? CityNameEn);
public sealed record NinjaAccountUpdateRowResponse(Guid Id, int SourceRowNumber, string CaptainId, string SheetOwnerName,
    string SheetCity, string SheetIqamaNo, string SheetStatus, bool? SheetSuspended, string MatchStatus,
    NinjaAccountUpdateSystemAccount? System, IReadOnlyList<NinjaAccountUpdateProblem> Problems);
