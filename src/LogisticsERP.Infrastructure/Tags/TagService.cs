using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Tags;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Domain.Entities.Tags;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Tags;

internal sealed class TagService(ApplicationDbContext dbContext) : ITagService
{
    public async Task<Result<IReadOnlyList<TagResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tags = await dbContext.Tags.AsNoTracking().OrderBy(item => item.NameAr).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<TagResponse>>(tags.Select(ToResponse).ToArray());
    }

    public async Task<Result<TagResponse>> UpsertAsync(
        Guid? id,
        TagUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HrServiceSupport.HasText(request.Code)
            || !HrServiceSupport.HasText(request.NameAr)
            || !HrServiceSupport.HasText(request.NameEn)
            || !HrServiceSupport.HasText(request.Color)
            || request.Color.Trim().Length > 32
            || !Enum.TryParse<CatalogStatus>(request.Status, true, out var status))
        {
            return Result.Failure<TagResponse>(TagErrors.InvalidRequest);
        }

        Tag entity;
        if (id is null)
        {
            entity = new Tag();
            dbContext.Tags.Add(entity);
        }
        else
        {
            entity = await dbContext.Tags.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? null!;
            if (entity is null) return Result.Failure<TagResponse>(TagErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(entity.RowVersion, request.RowVersion))
                return Result.Failure<TagResponse>(TagErrors.ConcurrencyConflict);
        }

        var code = HrServiceSupport.NormalizeCode(request.Code);
        if (await dbContext.Tags.AnyAsync(item => item.Id != entity.Id && item.Code == code, cancellationToken))
            return Result.Failure<TagResponse>(TagErrors.Duplicate);
        entity.Code = code;
        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = request.NameEn.Trim();
        entity.Color = request.Color.Trim().ToLowerInvariant();
        entity.AppliesToEmployees = request.AppliesToEmployees;
        entity.AppliesToHousing = request.AppliesToHousing;
        entity.AppliesToClientContracts = request.AppliesToClientContracts;
        entity.AppliesToPlatformAccounts = request.AppliesToPlatformAccounts;
        entity.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(entity));
    }

    public async Task<Result> ArchiveAsync(
        Guid id,
        string reason,
        string rowVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return Result.Failure(TagErrors.InvalidRequest);
        var tag = await dbContext.Tags.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (tag is null) return Result.Failure(TagErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(tag.RowVersion, rowVersion)) return Result.Failure(TagErrors.ConcurrencyConflict);
        var inUse = await dbContext.EmployeeTags.AnyAsync(item => item.TagId == id, cancellationToken)
            || await dbContext.HousingTags.AnyAsync(item => item.TagId == id, cancellationToken)
            || await dbContext.ClientContractTags.AnyAsync(item => item.TagId == id, cancellationToken)
            || await dbContext.PlatformRiderAccountTags.AnyAsync(item => item.TagId == id, cancellationToken);
        if (inUse) return Result.Failure(TagErrors.InvalidRequest);
        tag.Status = CatalogStatus.Archived;
        tag.DeletionReason = reason.Trim();
        dbContext.Tags.Remove(tag);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<TagResponse>>> GetAssignmentsAsync(
        string resource,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var tagIds = await GetAssignedTagIdsAsync(resource, resourceId, cancellationToken);
        if (tagIds is null) return Result.Failure<IReadOnlyList<TagResponse>>(TagErrors.InvalidRequest);
        if (!await ParentExistsAsync(resource, resourceId, cancellationToken))
            return Result.Failure<IReadOnlyList<TagResponse>>(TagErrors.NotFound);
        var tags = await dbContext.Tags.AsNoTracking().Where(item => tagIds.Contains(item.Id))
            .OrderBy(item => item.NameAr).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<TagResponse>>(tags.Select(ToResponse).ToArray());
    }

    public async Task<Result<IReadOnlyList<TagResponse>>> ReplaceAssignmentsAsync(
        string resource,
        Guid resourceId,
        ReplaceTagAssignmentsRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeResource(resource);
        if (normalized is null || request.TagIds.Distinct().Count() != request.TagIds.Count)
            return Result.Failure<IReadOnlyList<TagResponse>>(TagErrors.InvalidRequest);
        var parent = await GetParentAsync(normalized, resourceId, cancellationToken);
        if (parent is null) return Result.Failure<IReadOnlyList<TagResponse>>(TagErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(parent.RowVersion, request.ParentRowVersion))
            return Result.Failure<IReadOnlyList<TagResponse>>(TagErrors.ConcurrencyConflict);

        var requestedIds = request.TagIds.ToHashSet();
        var tags = requestedIds.Count == 0 ? [] : await dbContext.Tags
            .Where(item => requestedIds.Contains(item.Id) && item.Status == CatalogStatus.Active)
            .ToArrayAsync(cancellationToken);
        if (tags.Length != requestedIds.Count || tags.Any(item => !IsApplicable(item, normalized)))
            return Result.Failure<IReadOnlyList<TagResponse>>(TagErrors.InvalidRequest);

        await ReplaceLinksAsync(normalized, resourceId, requestedIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<TagResponse>>(tags.OrderBy(item => item.NameAr).Select(ToResponse).ToArray());
    }

    private async Task ReplaceLinksAsync(
        string resource,
        Guid resourceId,
        HashSet<Guid> requestedIds,
        CancellationToken cancellationToken)
    {
        switch (resource)
        {
            case "employees":
                await ReplaceLinksAsync(dbContext.EmployeeTags, item => item.EmployeeId == resourceId, item => item.TagId,
                    id => new EmployeeTag { EmployeeId = resourceId, TagId = id }, requestedIds, cancellationToken);
                break;
            case "housing":
                await ReplaceLinksAsync(dbContext.HousingTags, item => item.HousingId == resourceId, item => item.TagId,
                    id => new HousingTag { HousingId = resourceId, TagId = id }, requestedIds, cancellationToken);
                break;
            case "client-contracts":
                await ReplaceLinksAsync(dbContext.ClientContractTags, item => item.ClientContractId == resourceId, item => item.TagId,
                    id => new ClientContractTag { ClientContractId = resourceId, TagId = id }, requestedIds, cancellationToken);
                break;
            case "platform-accounts":
                await ReplaceLinksAsync(dbContext.PlatformRiderAccountTags, item => item.PlatformRiderAccountId == resourceId, item => item.TagId,
                    id => new PlatformRiderAccountTag { PlatformRiderAccountId = resourceId, TagId = id }, requestedIds, cancellationToken);
                break;
        }
    }

    private static async Task ReplaceLinksAsync<TLink>(
        DbSet<TLink> set,
        System.Linq.Expressions.Expression<Func<TLink, bool>> resourcePredicate,
        Func<TLink, Guid> tagId,
        Func<Guid, TLink> create,
        HashSet<Guid> requestedIds,
        CancellationToken cancellationToken)
        where TLink : AuditableEntity
    {
        var links = await set.IgnoreQueryFilters().Where(resourcePredicate).ToListAsync(cancellationToken);
        foreach (var link in links.Where(item => !requestedIds.Contains(tagId(item)) && !item.IsDeleted))
        {
            set.Remove(link);
        }
        foreach (var id in requestedIds)
        {
            var existing = links.SingleOrDefault(item => tagId(item) == id);
            if (existing is null)
            {
                set.Add(create(id));
            }
            else if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.DeletedAtUtc = null;
                existing.DeletedByUserId = null;
                existing.DeletionReason = null;
            }
        }
    }

    private async Task<HashSet<Guid>?> GetAssignedTagIdsAsync(string resource, Guid resourceId, CancellationToken cancellationToken)
    {
        return NormalizeResource(resource) switch
        {
            "employees" => (await dbContext.EmployeeTags.AsNoTracking().Where(item => item.EmployeeId == resourceId)
                .Select(item => item.TagId).ToArrayAsync(cancellationToken)).ToHashSet(),
            "housing" => (await dbContext.HousingTags.AsNoTracking().Where(item => item.HousingId == resourceId)
                .Select(item => item.TagId).ToArrayAsync(cancellationToken)).ToHashSet(),
            "client-contracts" => (await dbContext.ClientContractTags.AsNoTracking().Where(item => item.ClientContractId == resourceId)
                .Select(item => item.TagId).ToArrayAsync(cancellationToken)).ToHashSet(),
            "platform-accounts" => (await dbContext.PlatformRiderAccountTags.AsNoTracking().Where(item => item.PlatformRiderAccountId == resourceId)
                .Select(item => item.TagId).ToArrayAsync(cancellationToken)).ToHashSet(),
            _ => null
        };
    }

    private async Task<AuditableEntity?> GetParentAsync(string resource, Guid resourceId, CancellationToken cancellationToken) => resource switch
    {
        "employees" => await dbContext.Employees.SingleOrDefaultAsync(item => item.Id == resourceId, cancellationToken),
        "housing" => await dbContext.Housing.SingleOrDefaultAsync(item => item.Id == resourceId, cancellationToken),
        "client-contracts" => await dbContext.ClientContracts.SingleOrDefaultAsync(item => item.Id == resourceId, cancellationToken),
        "platform-accounts" => await dbContext.PlatformRiderAccounts.SingleOrDefaultAsync(item => item.Id == resourceId, cancellationToken),
        _ => null
    };

    private async Task<bool> ParentExistsAsync(string resource, Guid resourceId, CancellationToken cancellationToken) =>
        await GetParentAsync(NormalizeResource(resource) ?? string.Empty, resourceId, cancellationToken) is not null;

    private static bool IsApplicable(Tag tag, string resource) => resource switch
    {
        "employees" => tag.AppliesToEmployees,
        "housing" => tag.AppliesToHousing,
        "client-contracts" => tag.AppliesToClientContracts,
        "platform-accounts" => tag.AppliesToPlatformAccounts,
        _ => false
    };

    private static string? NormalizeResource(string resource) => resource.Trim().ToLowerInvariant() switch
    {
        "employees" or "employee" => "employees",
        "housing" => "housing",
        "client-contracts" or "client-contract" => "client-contracts",
        "platform-accounts" or "platform-account" => "platform-accounts",
        _ => null
    };

    private static TagResponse ToResponse(Tag item) => new(
        item.Id, item.Code, item.NameAr, item.NameEn, item.Color, item.AppliesToEmployees,
        item.AppliesToHousing, item.AppliesToClientContracts, item.AppliesToPlatformAccounts,
        item.Status.ToString(), HrServiceSupport.EncodeRowVersion(item.RowVersion));
}

