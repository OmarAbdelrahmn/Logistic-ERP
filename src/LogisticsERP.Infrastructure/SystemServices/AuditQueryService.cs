using System.Globalization;
using System.Text.Json;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.SystemServices;

internal sealed class AuditQueryService(
    ApplicationDbContext dbContext,
    IdentityDbContext identityDbContext) : IAuditQueryService
{
    public async Task<Result<PageResponse<AuditEntryResponse>>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        if (query.PageSize is < 1 or > 200 || query.ToUtc < query.FromUtc)
            return Result.Failure<PageResponse<AuditEntryResponse>>(SystemErrors.InvalidRequest);
        var rows = dbContext.AuditEntries.AsNoTracking().AsQueryable();
        if (query.ActorUserId.HasValue) rows = rows.Where(item => item.ActorUserId == query.ActorUserId);
        if (!string.IsNullOrWhiteSpace(query.EntityType)) rows = rows.Where(item => item.EntityType == query.EntityType.Trim());
        if (query.EntityId.HasValue) rows = rows.Where(item => item.EntityId == query.EntityId);
        if (!string.IsNullOrWhiteSpace(query.Action)) rows = rows.Where(item => item.Action == query.Action.Trim());
        if (!string.IsNullOrWhiteSpace(query.CorrelationId)) rows = rows.Where(item => item.CorrelationId == query.CorrelationId.Trim());
        if (query.FromUtc.HasValue) rows = rows.Where(item => item.OccurredAtUtc >= query.FromUtc);
        if (query.ToUtc.HasValue) rows = rows.Where(item => item.OccurredAtUtc <= query.ToUtc);
        if (query.BeforeSequence.HasValue) rows = rows.Where(item => item.Sequence < query.BeforeSequence);
        var page = await rows.OrderByDescending(item => item.Sequence).Take(query.PageSize + 1).ToArrayAsync(cancellationToken);
        var hasNext = page.Length > query.PageSize;
        var items = page.Take(query.PageSize).ToArray();
        return Result.Success(new PageResponse<AuditEntryResponse>(
            await ToResponsesAsync(items, cancellationToken),
            hasNext && items.Length > 0 ? items[^1].Sequence.ToString(CultureInfo.InvariantCulture) : null));
    }

    public async Task<Result<AuditEntryResponse>> GetAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.AuditEntries.AsNoTracking().SingleOrDefaultAsync(row => row.EventId == eventId, cancellationToken);
        if (item is null)
        {
            return Result.Failure<AuditEntryResponse>(SystemErrors.NotFound);
        }

        return Result.Success((await ToResponsesAsync([item], cancellationToken))[0]);
    }

    private async Task<AuditEntryResponse[]> ToResponsesAsync(AuditEntry[] items, CancellationToken cancellationToken)
    {
        var actorIds = items.Where(item => item.ActorUserId.HasValue).Select(item => item.ActorUserId!.Value).Distinct().ToArray();
        // Audit evidence must still identify accounts that were subsequently archived.
        var users = await identityDbContext.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(user => actorIds.Contains(user.Id))
            .Select(user => new AuditActorProjection(user.Id, user.UserName, user.DisplayNameAr, user.DisplayNameEn, user.EmployeeId, user.Status))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        return items.Select(item => ToResponse(item, users)).ToArray();
    }

    private static AuditEntryResponse ToResponse(AuditEntry item, IReadOnlyDictionary<Guid, AuditActorProjection> users)
    {
        users.TryGetValue(item.ActorUserId ?? Guid.Empty, out var user);
        var before = ReadJsonObject(item.BeforeJson);
        var after = ReadJsonObject(item.AfterJson);
        var fields = before.Keys.Union(after.Keys, StringComparer.Ordinal)
            .OrderBy(field => field, StringComparer.Ordinal)
            .Select(field => new AuditFieldChangeResponse(field, ReadValue(before, field), ReadValue(after, field)))
            .ToArray();

        return new AuditEntryResponse(
            item.EventId, item.Sequence, item.ActorUserId, item.ActorType, item.Action, item.Category,
            item.EntityType, item.EntityId, item.OccurredAtUtc, item.CorrelationId, item.Reason,
            item.BeforeJson, item.AfterJson, item.Source, item.SchemaVersion,
            new AuditActorResponse(item.ActorUserId, item.ActorType, user?.UserName, user?.DisplayNameAr,
                user?.DisplayNameEn, user?.EmployeeId, user?.Status.ToString()),
            new AuditRecordResponse(item.EntityType, item.EntityId, FindDisplayLabel(after, before), FindDisplayCode(after, before)),
            fields,
            new AuditRequestDetailsResponse(item.SessionId, item.SupportAccessGrantId, item.CorrelationId,
                item.TraceId, item.IpAddress, item.UserAgent, item.Source));
    }

    private static Dictionary<string, JsonElement> ReadJsonObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new(StringComparer.Ordinal);
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind != JsonValueKind.Object
                ? new(StringComparer.Ordinal)
                : document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new(StringComparer.Ordinal);
        }
    }

    private static object? ReadValue(Dictionary<string, JsonElement> values, string field) =>
        !values.TryGetValue(field, out var value) ? null : JsonSerializer.Deserialize<object>(value.GetRawText());

    private static string? FindDisplayLabel(params Dictionary<string, JsonElement>[] values) =>
        FindFirstString(values, "FullNameAr", "FullNameEn", "DisplayNameAr", "DisplayNameEn", "NameAr", "NameEn", "Name", "TitleAr", "TitleEn", "Title");

    private static string? FindDisplayCode(params Dictionary<string, JsonElement>[] values) =>
        FindFirstString(values, "Code", "EmployeeNumber", "PlateNumber", "ReferenceNumber", "SerialNumber");

    private static string? FindFirstString(IEnumerable<Dictionary<string, JsonElement>> values, params string[] fields)
    {
        foreach (var valuesByField in values)
        foreach (var field in fields)
            if (valuesByField.TryGetValue(field, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        return null;
    }

    private sealed record AuditActorProjection(
        Guid Id,
        string? UserName,
        string DisplayNameAr,
        string DisplayNameEn,
        Guid? EmployeeId,
        UserAccountStatus Status);
}
