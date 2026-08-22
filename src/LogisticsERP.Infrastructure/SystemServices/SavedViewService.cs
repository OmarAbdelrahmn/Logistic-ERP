using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.System;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.SystemServices;

internal sealed class SavedViewService(ApplicationDbContext dbContext, ICurrentUser currentUser) : ISavedViewService
{
    public async Task<Result<IReadOnlyList<SavedViewResponse>>> GetMineAsync(
        string? moduleKey,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure<IReadOnlyList<SavedViewResponse>>(SystemErrors.CurrentUserUnavailable);
        var query = dbContext.SavedViews.AsNoTracking().Where(item => item.UserId == userId);
        if (!string.IsNullOrWhiteSpace(moduleKey)) query = query.Where(item => item.ModuleKey == moduleKey.Trim());
        var rows = await query.OrderBy(item => item.ModuleKey).ThenBy(item => item.Name).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<SavedViewResponse>>(rows.Select(ToResponse).ToArray());
    }

    public async Task<Result<SavedViewResponse>> UpsertAsync(
        Guid? id,
        SavedViewUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || !Validate(request))
            return Result.Failure<SavedViewResponse>(SystemErrors.InvalidRequest);
        SavedView item;
        if (id is null)
        {
            item = new SavedView { UserId = userId };
            dbContext.SavedViews.Add(item);
        }
        else
        {
            item = await dbContext.SavedViews.SingleOrDefaultAsync(row => row.Id == id && row.UserId == userId, cancellationToken) ?? null!;
            if (item is null) return Result.Failure<SavedViewResponse>(SystemErrors.NotFound);
            if (!HrServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion))
                return Result.Failure<SavedViewResponse>(SystemErrors.ConcurrencyConflict);
        }
        if (await dbContext.SavedViews.AnyAsync(row => row.Id != item.Id && row.UserId == userId
            && row.ModuleKey == request.ModuleKey.Trim() && row.Name == request.Name.Trim(), cancellationToken))
            return Result.Failure<SavedViewResponse>(SystemErrors.Conflict);
        item.ModuleKey = request.ModuleKey.Trim().ToLowerInvariant();
        item.Name = request.Name.Trim();
        item.SchemaVersion = request.SchemaVersion;
        item.FiltersJson = NormalizeJson(request.FiltersJson);
        item.SortingJson = NormalizeJson(request.SortingJson);
        item.ColumnsJson = NormalizeJson(request.ColumnsJson);
        item.ColumnOrderJson = NormalizeJson(request.ColumnOrderJson);
        item.Density = request.Density.Trim().ToLowerInvariant();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(item));
    }

    public async Task<Result> DeleteAsync(Guid id, string rowVersion, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure(SystemErrors.CurrentUserUnavailable);
        var item = await dbContext.SavedViews.SingleOrDefaultAsync(row => row.Id == id && row.UserId == userId, cancellationToken);
        if (item is null) return Result.Failure(SystemErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(item.RowVersion, rowVersion)) return Result.Failure(SystemErrors.ConcurrencyConflict);
        item.DeletionReason = "Deleted by the owning user.";
        dbContext.SavedViews.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static bool Validate(SavedViewUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ModuleKey) || request.ModuleKey.Trim().Length > 100
            || request.ModuleKey.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_' and not '.')
            || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 150
            || request.SchemaVersion < 1 || request.Density.Trim().ToLowerInvariant() is not ("compact" or "comfortable" or "spacious"))
            return false;
        return IsJson(request.FiltersJson, JsonValueKind.Object)
            && IsJson(request.SortingJson, JsonValueKind.Array)
            && IsJson(request.ColumnsJson, JsonValueKind.Array)
            && IsJson(request.ColumnOrderJson, JsonValueKind.Array);
    }

    private static bool IsJson(string value, JsonValueKind kind)
    {
        try { using var document = JsonDocument.Parse(value); return document.RootElement.ValueKind == kind; }
        catch (JsonException) { return false; }
    }

    private static string NormalizeJson(string value)
    {
        using var document = JsonDocument.Parse(value);
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static SavedViewResponse ToResponse(SavedView item) => new(
        item.Id, item.ModuleKey, item.Name, item.SchemaVersion, item.FiltersJson, item.SortingJson,
        item.ColumnsJson, item.ColumnOrderJson, item.Density, HrServiceSupport.EncodeRowVersion(item.RowVersion));
}

