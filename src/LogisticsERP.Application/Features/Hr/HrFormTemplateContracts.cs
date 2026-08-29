using System.Text.Json;
using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public sealed record HrFormTemplateCreateRequest(
    string Code,
    string NameAr,
    string? NameEn,
    string Category,
    string? DescriptionAr,
    string? DescriptionEn,
    JsonElement Definition,
    string? ChangeNote);

public sealed record HrFormTemplateMetadataRequest(
    string NameAr,
    string? NameEn,
    string Category,
    string? DescriptionAr,
    string? DescriptionEn,
    bool IsActive,
    string RowVersion);

public sealed record HrFormTemplateVersionCreateRequest(
    JsonElement Definition,
    string? ChangeNote,
    string RowVersion);

public sealed record HrFormTemplatePublishRequest(
    string RowVersion);

public sealed record HrFormTemplateSummaryResponse(
    Guid Id,
    string Code,
    string NameAr,
    string? NameEn,
    string Category,
    string? DescriptionAr,
    string? DescriptionEn,
    bool IsActive,
    Guid? CurrentDraftVersionId,
    int? CurrentDraftVersionNumber,
    Guid? CurrentPublishedVersionId,
    int? CurrentPublishedVersionNumber,
    string RowVersion);

public sealed record HrFormTemplateVersionResponse(
    Guid Id,
    Guid HrFormTemplateId,
    int VersionNumber,
    int DefinitionSchemaVersion,
    JsonElement Definition,
    string DefinitionSha256,
    string? ChangeNote,
    Guid? CreatedByUserId,
    DateTimeOffset CreatedAtUtc);

public sealed record HrFormTemplateResponse(
    HrFormTemplateSummaryResponse Template,
    HrFormTemplateVersionResponse? DraftVersion,
    HrFormTemplateVersionResponse? PublishedVersion);

public interface IHrFormTemplateService
{
    Task<Result<IReadOnlyList<HrFormTemplateSummaryResponse>>> GetAllAsync(
        string? search,
        string? category,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<Result<HrFormTemplateResponse>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<HrFormTemplateResponse>> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Result<HrFormTemplateResponse>> CreateAsync(HrFormTemplateCreateRequest request, CancellationToken cancellationToken = default);
    Task<Result<HrFormTemplateResponse>> UpdateMetadataAsync(Guid id, HrFormTemplateMetadataRequest request, CancellationToken cancellationToken = default);
    Task<Result<HrFormTemplateVersionResponse>> CreateVersionAsync(Guid id, HrFormTemplateVersionCreateRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HrFormTemplateVersionResponse>>> GetVersionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<HrFormTemplateResponse>> PublishAsync(Guid id, Guid versionId, HrFormTemplatePublishRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(Guid id, ArchiveRequest request, CancellationToken cancellationToken = default);
}
