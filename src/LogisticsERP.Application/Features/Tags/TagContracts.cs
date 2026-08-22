using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Tags;

public sealed record TagUpsertRequest(
    string Code,
    string NameAr,
    string NameEn,
    string Color,
    bool AppliesToEmployees,
    bool AppliesToHousing,
    bool AppliesToClientContracts,
    bool AppliesToPlatformAccounts,
    string Status,
    string? RowVersion);

public sealed record TagResponse(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    string Color,
    bool AppliesToEmployees,
    bool AppliesToHousing,
    bool AppliesToClientContracts,
    bool AppliesToPlatformAccounts,
    string Status,
    string RowVersion);

public sealed record ReplaceTagAssignmentsRequest(IReadOnlyCollection<Guid> TagIds, string ParentRowVersion);

public interface ITagService
{
    Task<Result<IReadOnlyList<TagResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<TagResponse>> UpsertAsync(Guid? id, TagUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(Guid id, string reason, string rowVersion, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TagResponse>>> GetAssignmentsAsync(string resource, Guid resourceId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TagResponse>>> ReplaceAssignmentsAsync(string resource, Guid resourceId, ReplaceTagAssignmentsRequest request, CancellationToken cancellationToken = default);
}

