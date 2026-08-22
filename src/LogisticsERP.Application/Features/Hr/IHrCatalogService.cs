using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public interface IHrCatalogService
{
    Task<Result<IReadOnlyList<GlobalCityResponse>>> GetGlobalCitiesAsync(CancellationToken cancellationToken = default);
    Task<Result<GlobalCityResponse>> UpsertGlobalCityAsync(Guid? id, GlobalCityUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<OperatingCityResponse>> UpsertOperatingCityAsync(Guid? id, OperatingCityUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<CatalogResponse>>> GetJobTitlesAsync(CancellationToken cancellationToken = default);
    Task<Result<CatalogResponse>> UpsertJobTitleAsync(Guid? id, CatalogUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<CatalogResponse>>> GetResidencyProfessionsAsync(CancellationToken cancellationToken = default);
    Task<Result<CatalogResponse>> UpsertResidencyProfessionAsync(Guid? id, CatalogUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<CatalogResponse>>> GetOperationalWorkTypesAsync(CancellationToken cancellationToken = default);
    Task<Result<CatalogResponse>> UpsertOperationalWorkTypeAsync(Guid? id, CatalogUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<CatalogResponse>>> GetDriverLicenseCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Result<CatalogResponse>> UpsertDriverLicenseCategoryAsync(Guid? id, CatalogUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<DocumentTypeResponse>>> GetDocumentTypesAsync(CancellationToken cancellationToken = default);
    Task<Result<DocumentTypeResponse>> UpsertDocumentTypeAsync(Guid? id, DocumentTypeUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<DocumentRequirementResponse>>> GetDocumentRequirementsAsync(Guid? documentTypeId, CancellationToken cancellationToken = default);
    Task<Result<DocumentRequirementResponse>> UpsertDocumentRequirementAsync(Guid? id, DocumentRequirementUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<OperatingCityResponse>>> GetOperatingCitiesAsync(CancellationToken cancellationToken = default);
    Task<Result> SetJobTitleWorkTypesAsync(Guid jobTitleId, SetJobTitleWorkTypesRequest request, CancellationToken cancellationToken = default);
}

public sealed record CatalogUpsertRequest(
    string Code,
    string NameAr,
    string? NameEn,
    string Status,
    string? DescriptionAr,
    string? DescriptionEn,
    string? RowVersion);

public sealed record SetJobTitleWorkTypesRequest(IReadOnlyCollection<Guid> OperationalWorkTypeIds);

public sealed record GlobalCityUpsertRequest(
    string Code,
    string NameAr,
    string NameEn,
    string RegionAr,
    string RegionEn,
    string CountryCode,
    decimal? Latitude,
    decimal? Longitude,
    int DisplayOrder,
    string Status,
    string? RowVersion);

public sealed record GlobalCityResponse(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    string RegionAr,
    string RegionEn,
    string CountryCode,
    decimal? Latitude,
    decimal? Longitude,
    int DisplayOrder,
    string Status,
    string RowVersion);

public sealed record OperatingCityUpsertRequest(
    Guid GlobalCityId,
    DateOnly EnabledFrom,
    DateOnly? DisabledAt,
    string Status,
    string? RowVersion);

public sealed record OperatingCityResponse(
    Guid Id,
    Guid GlobalCityId,
    string Code,
    string NameAr,
    string NameEn,
    string Status,
    DateOnly EnabledFrom = default,
    DateOnly? DisabledAt = null,
    string RowVersion = "");

public sealed record DocumentTypeUpsertRequest(
    string Code,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    bool AppliesToSponsoredInternal,
    bool AppliesToOutsideRider,
    bool AppliesToRiderProfile,
    bool RequiresNumber,
    bool RequiresIssueDate,
    bool RequiresExpiryDate,
    bool RequiresFile,
    IReadOnlyCollection<string> AllowedMimeTypes,
    long MaxFileSizeBytes,
    string Status,
    string? RowVersion);

public sealed record DocumentTypeResponse(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    bool AppliesToSponsoredInternal,
    bool AppliesToOutsideRider,
    bool AppliesToRiderProfile,
    bool RequiresNumber,
    bool RequiresIssueDate,
    bool RequiresExpiryDate,
    bool RequiresFile,
    IReadOnlyList<string> AllowedMimeTypes,
    long MaxFileSizeBytes,
    string Status,
    string RowVersion);

public sealed record DocumentRequirementUpsertRequest(
    Guid DocumentTypeId,
    string? RelationshipType,
    bool AppliesToRiderProfile,
    bool IsRequired,
    IReadOnlyCollection<int> ReminderOffsetsDays,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    string? RowVersion);

public sealed record DocumentRequirementResponse(
    Guid Id,
    Guid DocumentTypeId,
    string DocumentTypeCode,
    string? RelationshipType,
    bool AppliesToRiderProfile,
    bool IsRequired,
    IReadOnlyList<int> ReminderOffsetsDays,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    string RowVersion);
