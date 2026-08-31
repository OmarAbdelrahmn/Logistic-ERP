using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class HrCatalogService(ApplicationDbContext dbContext) : IHrCatalogService
{
    private static readonly HashSet<string> SupportedDocumentMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png", "image/webp", "image/gif", "image/bmp"
    };

    public async Task<Result<IReadOnlyList<GlobalCityResponse>>> GetGlobalCitiesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.GlobalCities.AsNoTracking()
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.NameAr)
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<GlobalCityResponse>>(rows.Select(ToGlobalCity).ToArray());
    }

    public async Task<Result<GlobalCityResponse>> UpsertGlobalCityAsync(
        Guid? id,
        GlobalCityUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Code)
            || !HrServiceSupport.HasText(request.NameAr)
            || !HrServiceSupport.HasText(request.NameEn)
            || !HrServiceSupport.HasText(request.RegionAr)
            || !HrServiceSupport.HasText(request.RegionEn)
            || request.CountryCode.Trim().Length != 2
            || request.Latitude is < -90 or > 90
            || request.Longitude is < -180 or > 180
            || request.DisplayOrder < 0
            || !Enum.TryParse<CatalogStatus>(request.Status, true, out var status))
        {
            return Result.Failure<GlobalCityResponse>(HrErrors.InvalidRequest);
        }

        GlobalCity entity;
        if (id is null)
        {
            entity = new GlobalCity();
            dbContext.GlobalCities.Add(entity);
        }
        else
        {
            entity = await dbContext.GlobalCities.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<GlobalCityResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
                return Result.Failure<GlobalCityResponse>(HrErrors.ConcurrencyConflict);
        }
        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.GlobalCities.AnyAsync(item => item.Id != entity.Id && item.Code == code, cancellationToken))
            return Result.Failure<GlobalCityResponse>(HrErrors.Duplicate);
        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = request.NameEn.Trim();
        entity.RegionAr = request.RegionAr.Trim();
        entity.RegionEn = request.RegionEn.Trim();
        entity.CountryCode = request.CountryCode.Trim().ToUpperInvariant();
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.DisplayOrder = request.DisplayOrder;
        entity.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToGlobalCity(entity));
    }

    public async Task<Result<OperatingCityResponse>> UpsertOperatingCityAsync(
        Guid? id,
        OperatingCityUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<CatalogStatus>(request.Status, true, out var status)
            || request.DisabledAt is not null && request.DisabledAt < request.EnabledFrom
            || !await dbContext.GlobalCities.AnyAsync(item => item.Id == request.GlobalCityId, cancellationToken))
            return Result.Failure<OperatingCityResponse>(HrErrors.InvalidRequest);

        OperatingCity entity;
        if (id is null)
        {
            entity = new OperatingCity();
            dbContext.OperatingCities.Add(entity);
        }
        else
        {
            entity = await dbContext.OperatingCities.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<OperatingCityResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
                return Result.Failure<OperatingCityResponse>(HrErrors.ConcurrencyConflict);
        }
        if (await dbContext.OperatingCities.AnyAsync(
            item => item.Id != entity.Id && item.GlobalCityId == request.GlobalCityId,
            cancellationToken))
            return Result.Failure<OperatingCityResponse>(HrErrors.Duplicate);
        entity.GlobalCityId = request.GlobalCityId;
        entity.EnabledFrom = request.EnabledFrom;
        entity.DisabledAt = request.DisabledAt;
        entity.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetOperatingCitiesAsync(cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<IReadOnlyList<CatalogResponse>>> GetJobTitlesAsync(CancellationToken cancellationToken = default) =>
        Result.Success<IReadOnlyList<CatalogResponse>>(await dbContext.JobTitles.AsNoTracking()
            .OrderBy(item => item.NameAr)
            .Select(item => ToCatalog(item.Id, item.Code, item.NameAr, item.NameEn, item.Status, item.RowVersion))
            .ToArrayAsync(cancellationToken));

    public Task<Result<CatalogResponse>> UpsertJobTitleAsync(Guid? id, CatalogUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpsertCatalogAsync(id, request, dbContext.JobTitles, cancellationToken);

    public async Task<Result<IReadOnlyList<CatalogResponse>>> GetResidencyProfessionsAsync(CancellationToken cancellationToken = default) =>
        Result.Success<IReadOnlyList<CatalogResponse>>(await dbContext.ResidencyProfessions.AsNoTracking()
            .OrderBy(item => item.NameAr)
            .Select(item => ToCatalog(item.Id, item.Code, item.NameAr, item.NameEn, item.Status, item.RowVersion))
            .ToArrayAsync(cancellationToken));

    public Task<Result<CatalogResponse>> UpsertResidencyProfessionAsync(Guid? id, CatalogUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpsertCatalogAsync(id, request, dbContext.ResidencyProfessions, cancellationToken);

    public async Task<Result<IReadOnlyList<CatalogResponse>>> GetOperationalWorkTypesAsync(CancellationToken cancellationToken = default) =>
        Result.Success<IReadOnlyList<CatalogResponse>>(await dbContext.OperationalWorkTypes.AsNoTracking()
            .OrderBy(item => item.NameAr)
            .Select(item => ToCatalog(item.Id, item.Code, item.NameAr, item.NameEn, item.Status, item.RowVersion))
            .ToArrayAsync(cancellationToken));

    public Task<Result<CatalogResponse>> UpsertOperationalWorkTypeAsync(Guid? id, CatalogUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpsertOperationalWorkTypeCoreAsync(id, request, cancellationToken);

    public async Task<Result<IReadOnlyList<CatalogResponse>>> GetDriverLicenseCategoriesAsync(CancellationToken cancellationToken = default) =>
        Result.Success<IReadOnlyList<CatalogResponse>>(await dbContext.DriverLicenseCategories.AsNoTracking()
            .OrderBy(item => item.NameAr)
            .Select(item => ToCatalog(item.Id, item.Code, item.NameAr, item.NameEn, item.Status, item.RowVersion))
            .ToArrayAsync(cancellationToken));

    public Task<Result<CatalogResponse>> UpsertDriverLicenseCategoryAsync(Guid? id, CatalogUpsertRequest request, CancellationToken cancellationToken = default) =>
        UpsertDriverLicenseCategoryCoreAsync(id, request, cancellationToken);

    public async Task<Result<IReadOnlyList<DocumentTypeResponse>>> GetDocumentTypesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.DocumentTypes.AsNoTracking().OrderBy(item => item.NameAr).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<DocumentTypeResponse>>(rows.Select(ToDocumentType).ToArray());
    }

    public async Task<Result<DocumentTypeResponse>> UpsertDocumentTypeAsync(
        Guid? id,
        DocumentTypeUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var mimeTypes = request.AllowedMimeTypes.Select(item => item.Trim().ToLowerInvariant()).Where(item => item.Length > 0).Distinct().ToArray();
        if (!HrServiceSupport.HasText(request.Code) || !HrServiceSupport.HasText(request.NameAr)
            || !HrServiceSupport.HasText(request.NameEn) || mimeTypes.Length == 0
            || mimeTypes.Any(item => !item.Contains('/') || item.Length > 150 || !SupportedDocumentMimeTypes.Contains(item))
            || !request.AppliesToSponsoredInternal && !request.AppliesToOutsideRider && !request.AppliesToRiderProfile
            || request.MaxFileSizeBytes is <= 0 or > 100 * 1024 * 1024
            || !Enum.TryParse<CatalogStatus>(request.Status, true, out var status))
            return Result.Failure<DocumentTypeResponse>(HrErrors.InvalidRequest);
        DocumentType entity;
        if (id is null)
        {
            entity = new DocumentType();
            dbContext.DocumentTypes.Add(entity);
        }
        else
        {
            entity = await dbContext.DocumentTypes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<DocumentTypeResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
                return Result.Failure<DocumentTypeResponse>(HrErrors.ConcurrencyConflict);
        }
        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.DocumentTypes.AnyAsync(item => item.Id != entity.Id && item.Code == code, cancellationToken))
            return Result.Failure<DocumentTypeResponse>(HrErrors.Duplicate);
        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = request.NameEn.Trim();
        entity.DescriptionAr = HrServiceSupport.TrimOrNull(request.DescriptionAr);
        entity.DescriptionEn = HrServiceSupport.TrimOrNull(request.DescriptionEn);
        entity.AppliesToSponsoredInternal = request.AppliesToSponsoredInternal;
        entity.AppliesToOutsideRider = request.AppliesToOutsideRider;
        entity.AppliesToRiderProfile = request.AppliesToRiderProfile;
        entity.RequiresNumber = request.RequiresNumber;
        entity.RequiresIssueDate = request.RequiresIssueDate;
        entity.RequiresExpiryDate = request.RequiresExpiryDate;
        entity.RequiresFile = request.RequiresFile;
        entity.AllowedMimeTypes = string.Join(',', mimeTypes);
        entity.MaxFileSizeBytes = request.MaxFileSizeBytes;
        entity.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToDocumentType(entity));
    }

    public async Task<Result<IReadOnlyList<DocumentRequirementResponse>>> GetDocumentRequirementsAsync(
        Guid? documentTypeId,
        CancellationToken cancellationToken = default)
    {
        var rows = await (from requirement in dbContext.DocumentRequirements.AsNoTracking()
                          join type in dbContext.DocumentTypes.AsNoTracking() on requirement.DocumentTypeId equals type.Id
                          where documentTypeId == null || requirement.DocumentTypeId == documentTypeId
                          orderby type.NameAr, requirement.EffectiveFrom descending
                          select new DocumentRequirementProjection(requirement, type.Code)).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<DocumentRequirementResponse>>(rows.Select(ToDocumentRequirement).ToArray());
    }

    public async Task<Result<DocumentRequirementResponse>> UpsertDocumentRequirementAsync(
        Guid? id,
        DocumentRequirementUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        EmployeeRelationshipType parsedRelationship = default;
        EmployeeRelationshipType? relationshipType = null;
        if (request.RelationshipType is not null
            && !Enum.TryParse<EmployeeRelationshipType>(request.RelationshipType, true, out parsedRelationship))
            return Result.Failure<DocumentRequirementResponse>(HrErrors.InvalidRequest);
        if (request.RelationshipType is not null) relationshipType = parsedRelationship;
        var offsets = request.ReminderOffsetsDays.Distinct().OrderDescending().ToArray();
        if (offsets.Any(value => value is < 0 or > 365) || request.EffectiveTo < request.EffectiveFrom
            || !Enum.TryParse<CatalogStatus>(request.Status, true, out var status)
            || !await dbContext.DocumentTypes.AnyAsync(item => item.Id == request.DocumentTypeId, cancellationToken))
            return Result.Failure<DocumentRequirementResponse>(HrErrors.InvalidRequest);
        DocumentRequirement entity;
        if (id is null)
        {
            entity = new DocumentRequirement();
            dbContext.DocumentRequirements.Add(entity);
        }
        else
        {
            entity = await dbContext.DocumentRequirements.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<DocumentRequirementResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
                return Result.Failure<DocumentRequirementResponse>(HrErrors.ConcurrencyConflict);
        }
        if (await dbContext.DocumentRequirements.AnyAsync(item => item.Id != entity.Id
            && item.DocumentTypeId == request.DocumentTypeId && item.RelationshipType == relationshipType
            && item.AppliesToRiderProfile == request.AppliesToRiderProfile && item.EffectiveFrom == request.EffectiveFrom,
            cancellationToken)) return Result.Failure<DocumentRequirementResponse>(HrErrors.Duplicate);
        entity.DocumentTypeId = request.DocumentTypeId;
        entity.RelationshipType = relationshipType;
        entity.AppliesToRiderProfile = request.AppliesToRiderProfile;
        entity.IsRequired = request.IsRequired;
        entity.ReminderOffsetsDays = string.Join(',', offsets);
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetDocumentRequirementsAsync(request.DocumentTypeId, cancellationToken)).MapSingle(item => item.Id == entity.Id);
    }

    public async Task<Result<IReadOnlyList<OperatingCityResponse>>> GetOperatingCitiesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await (from operatingCity in dbContext.OperatingCities.AsNoTracking()
                          join city in dbContext.GlobalCities.AsNoTracking() on operatingCity.GlobalCityId equals city.Id
                          orderby city.DisplayOrder, city.NameAr
                          select new OperatingCityProjection(operatingCity, city))
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<OperatingCityResponse>>(rows.Select(row => new OperatingCityResponse(
            row.OperatingCity.Id,
            row.City.Id,
            row.City.Code,
            row.City.NameAr,
            row.City.NameEn,
            row.OperatingCity.Status.ToString(),
            row.OperatingCity.EnabledFrom,
            row.OperatingCity.DisabledAt,
            HrServiceSupport.EncodeRowVersion(row.OperatingCity.RowVersion))).ToArray());
    }

    public async Task<Result> SetJobTitleWorkTypesAsync(
        Guid jobTitleId,
        SetJobTitleWorkTypesRequest request,
        CancellationToken cancellationToken = default)
    {
        var ids = request.OperationalWorkTypeIds.Distinct().ToArray();
        if (ids.Length == 0
            || !await dbContext.JobTitles.AnyAsync(item => item.Id == jobTitleId, cancellationToken)
            || await dbContext.OperationalWorkTypes.CountAsync(item => ids.Contains(item.Id), cancellationToken) != ids.Length)
        {
            return Result.Failure(HrErrors.InvalidRequest);
        }

        var existing = await dbContext.JobTitleOperationalWorkTypes
            .Where(item => item.JobTitleId == jobTitleId)
            .ToListAsync(cancellationToken);
        foreach (var item in existing.Where(item => !ids.Contains(item.OperationalWorkTypeId)))
        {
            item.IsDeleted = true;
            item.DeletionReason = "Removed from the job title's allowed operational work types.";
        }

        var existingIds = existing.Select(item => item.OperationalWorkTypeId).ToHashSet();
        foreach (var id in ids.Where(id => !existingIds.Contains(id)))
        {
            dbContext.JobTitleOperationalWorkTypes.Add(new JobTitleOperationalWorkType
            {
                JobTitleId = jobTitleId,
                OperationalWorkTypeId = id
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result<CatalogResponse>> UpsertCatalogAsync(
        Guid? id,
        CatalogUpsertRequest request,
        DbSet<JobTitle> set,
        CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var status))
        {
            return Result.Failure<CatalogResponse>(HrErrors.InvalidRequest);
        }

        JobTitle entity;
        if (id is null)
        {
            entity = new JobTitle();
            set.Add(entity);
        }
        else
        {
            entity = await set.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null)
            {
                return Result.Failure<CatalogResponse>(HrErrors.NotFound);
            }
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            {
                return Result.Failure<CatalogResponse>(HrErrors.ConcurrencyConflict);
            }
        }

        entity.Code = HrServiceSupport.NormalizeCode(request.Code);
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = HrServiceSupport.TrimOrNull(request.NameEn) ?? entity.NameAr;
        entity.DescriptionAr = HrServiceSupport.TrimOrNull(request.DescriptionAr);
        entity.DescriptionEn = HrServiceSupport.TrimOrNull(request.DescriptionEn);
        entity.Status = status;
        if (await set.AnyAsync(item => item.Id != entity.Id && item.Code == entity.Code, cancellationToken))
        {
            return Result.Failure<CatalogResponse>(HrErrors.Duplicate);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToCatalog(entity.Id, entity.Code, entity.NameAr, entity.NameEn, entity.Status, entity.RowVersion));
    }

    private async Task<Result<CatalogResponse>> UpsertOperationalWorkTypeCoreAsync(
        Guid? id, CatalogUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var status)) return Result.Failure<CatalogResponse>(HrErrors.InvalidRequest);
        OperationalWorkType entity;
        if (id is null)
        {
            entity = new OperationalWorkType();
            dbContext.OperationalWorkTypes.Add(entity);
        }
        else
        {
            entity = await dbContext.OperationalWorkTypes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<CatalogResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<CatalogResponse>(HrErrors.ConcurrencyConflict);
        }
        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.OperationalWorkTypes.AnyAsync(item => item.Id != entity.Id && item.Code == code, cancellationToken))
            return Result.Failure<CatalogResponse>(HrErrors.Duplicate);
        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = HrServiceSupport.TrimOrNull(request.NameEn) ?? entity.NameAr;
        entity.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToCatalog(entity.Id, entity.Code, entity.NameAr, entity.NameEn, entity.Status, entity.RowVersion));
    }

    private async Task<Result<CatalogResponse>> UpsertDriverLicenseCategoryCoreAsync(
        Guid? id, CatalogUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var status)) return Result.Failure<CatalogResponse>(HrErrors.InvalidRequest);
        DriverLicenseCategory entity;
        if (id is null)
        {
            entity = new DriverLicenseCategory();
            dbContext.DriverLicenseCategories.Add(entity);
        }
        else
        {
            entity = await dbContext.DriverLicenseCategories.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<CatalogResponse>(HrErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion)) return Result.Failure<CatalogResponse>(HrErrors.ConcurrencyConflict);
        }
        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.DriverLicenseCategories.AnyAsync(item => item.Id != entity.Id && item.Code == code, cancellationToken))
            return Result.Failure<CatalogResponse>(HrErrors.Duplicate);
        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = HrServiceSupport.TrimOrNull(request.NameEn) ?? entity.NameAr;
        entity.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToCatalog(entity.Id, entity.Code, entity.NameAr, entity.NameEn, entity.Status, entity.RowVersion));
    }

    private async Task<Result<CatalogResponse>> UpsertCatalogAsync(
        Guid? id,
        CatalogUpsertRequest request,
        DbSet<ResidencyProfession> set,
        CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var status))
        {
            return Result.Failure<CatalogResponse>(HrErrors.InvalidRequest);
        }

        ResidencyProfession entity;
        if (id is null)
        {
            entity = new ResidencyProfession();
            set.Add(entity);
        }
        else
        {
            entity = await set.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null)
            {
                return Result.Failure<CatalogResponse>(HrErrors.NotFound);
            }
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
            {
                return Result.Failure<CatalogResponse>(HrErrors.ConcurrencyConflict);
            }
        }

        entity.Code = HrServiceSupport.NormalizeCode(request.Code);
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = HrServiceSupport.TrimOrNull(request.NameEn);
        entity.Status = status;
        if (await set.AnyAsync(item => item.Id != entity.Id && item.Code == entity.Code, cancellationToken))
        {
            return Result.Failure<CatalogResponse>(HrErrors.Duplicate);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToCatalog(entity.Id, entity.Code, entity.NameAr, entity.NameEn, entity.Status, entity.RowVersion));
    }

    private static bool TryValidate(CatalogUpsertRequest request, out CatalogStatus status)
    {
        status = default;
        return HrServiceSupport.HasText(request.Code)
            && HrServiceSupport.HasText(request.NameAr)
            && Enum.TryParse(request.Status, true, out status)
            && Enum.IsDefined(status);
    }

    private static CatalogResponse ToCatalog(Guid id, string code, string nameAr, string? nameEn, CatalogStatus status, byte[] rowVersion) =>
        new(id, code, nameAr, nameEn, status.ToString(), HrServiceSupport.EncodeRowVersion(rowVersion));

    private static GlobalCityResponse ToGlobalCity(GlobalCity item) => new(
        item.Id, item.Code, item.NameAr, item.NameEn, item.RegionAr, item.RegionEn, item.CountryCode,
        item.Latitude, item.Longitude, item.DisplayOrder, item.Status.ToString(), HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private static DocumentTypeResponse ToDocumentType(DocumentType item) => new(
        item.Id, item.Code, item.NameAr, item.NameEn, item.DescriptionAr, item.DescriptionEn,
        item.AppliesToSponsoredInternal, item.AppliesToOutsideRider, item.AppliesToRiderProfile,
        item.RequiresNumber, item.RequiresIssueDate, item.RequiresExpiryDate, item.RequiresFile,
        item.AllowedMimeTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        item.MaxFileSizeBytes, item.Status.ToString(), HrServiceSupport.EncodeRowVersion(item.RowVersion));

    private static DocumentRequirementResponse ToDocumentRequirement(DocumentRequirementProjection row) => new(
        row.Item.Id, row.Item.DocumentTypeId, row.DocumentTypeCode, row.Item.RelationshipType?.ToString(),
        row.Item.AppliesToRiderProfile, row.Item.IsRequired,
        row.Item.ReminderOffsetsDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var parsed) ? parsed : 0).ToArray(),
        row.Item.EffectiveFrom, row.Item.EffectiveTo, row.Item.Status.ToString(),
        HrServiceSupport.EncodeRowVersion(row.Item.RowVersion));

    private sealed record DocumentRequirementProjection(DocumentRequirement Item, string DocumentTypeCode);
    private sealed record OperatingCityProjection(OperatingCity OperatingCity, GlobalCity City);
}
