using System.Globalization;
using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.System;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.SystemServices;

internal sealed class NotificationService(
    ApplicationDbContext dbContext,
    IdentityDbContext identityDbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IPermissionChecker permissionChecker) : INotificationService
{
    public async Task<Result<PageResponse<NotificationResponse>>> GetMineAsync(
        bool unreadOnly,
        int pageSize,
        string? cursor,
        IReadOnlyList<string>? permissions = null,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync(new(permissions, unreadOnly, pageSize, cursor), cancellationToken);
        return result.IsFailure ? Result.Failure<PageResponse<NotificationResponse>>(result.Error)
            : Result.Success(new PageResponse<NotificationResponse>(result.Value!.Items, result.Value.NextCursor));
    }

    public async Task<Result<NotificationFeedResponse>> QueryAsync(NotificationQueryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PageSize is < 1 or > 200 || !TryParseCursor(request.Cursor, out var before, out var beforeId))
            return Result.Failure<NotificationFeedResponse>(SystemErrors.InvalidRequest);
        var effective = await ResolvePermissionsAsync(request.Permissions, cancellationToken);
        if (effective.IsFailure) return Result.Failure<NotificationFeedResponse>(effective.Error);
        var query = VisibleQuery(effective.Value!, request.Permissions is null);
        var count = await query.CountAsync(item => item.ReadAtUtc == null, cancellationToken);
        if (request.UnreadOnly) query = query.Where(item => item.ReadAtUtc == null);
        if (request.Cursor is not null)
        {
            query = query.Where(item => item.VisibleAtUtc < before
                || beforeId.HasValue && item.VisibleAtUtc == before && item.Id.CompareTo(beforeId.Value) < 0);
        }
        var rows = await query.OrderByDescending(item => item.VisibleAtUtc).ThenByDescending(item => item.Id)
            .Take(request.PageSize + 1).ToArrayAsync(cancellationToken);
        var hasNext = rows.Length > request.PageSize;
        var page = rows.Take(request.PageSize).ToArray();
        return Result.Success(new NotificationFeedResponse(
            page.Select(ToResponse).ToArray(),
            hasNext && page.Length > 0 ? $"{page[^1].VisibleAtUtc.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{page[^1].Id:N}" : null,
            count, effective.Value!));
    }

    internal static bool TryParseCursor(string? cursor, out DateTimeOffset before, out Guid? beforeId)
    {
        before = default; beforeId = null;
        if (cursor is null) return true;
        var parts = cursor.Split(':');
        if (parts.Length is < 1 or > 2 || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || ticks < DateTimeOffset.MinValue.Ticks || ticks > DateTimeOffset.MaxValue.Ticks) return false;
        if (parts.Length == 2)
        {
            if (!Guid.TryParseExact(parts[1], "N", out var id)) return false;
            beforeId = id;
        }
        before = new DateTimeOffset(ticks, TimeSpan.Zero);
        return true;
    }

    public async Task<Result<int>> GetUnreadCountAsync(IReadOnlyList<string>? permissions = null, CancellationToken cancellationToken = default)
    {
        var effective = await ResolvePermissionsAsync(permissions, cancellationToken);
        if (effective.IsFailure) return Result.Failure<int>(effective.Error);
        var count = await VisibleQuery(effective.Value!, permissions is null).CountAsync(item => item.ReadAtUtc == null, cancellationToken);
        return Result.Success(count);
    }

    private async Task<Result<string[]>> ResolvePermissionsAsync(IReadOnlyList<string>? requested, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId || currentUser.AuthorizationVersion is not { } version)
            return Result.Failure<string[]>(SystemErrors.CurrentUserUnavailable);
        if (requested is not null && (requested.Count > 256 || requested.Any(x => string.IsNullOrWhiteSpace(x) || !PermissionKeys.All.Contains(x.Trim()))))
            return Result.Failure<string[]>(SystemErrors.InvalidRequest);
        var candidates = requested is null ? PermissionKeys.All.ToArray() : requested.Select(x => x.Trim()).Distinct(StringComparer.Ordinal).ToArray();
        var effective = new List<string>();
        foreach (var key in candidates)
            if (await permissionChecker.HasPermissionAsync(userId, version, key, null, ct)) effective.Add(key);
        return Result.Success(effective.Order(StringComparer.Ordinal).ToArray());
    }

    private IQueryable<Notification> VisibleQuery(IReadOnlyList<string> effectivePermissions, bool includePersonal)
    {
        var now = timeProvider.GetUtcNow();
        var userId = currentUser.UserId!.Value;
        return NotificationPermissionFilter.Apply(dbContext.Notifications.AsNoTracking().Where(item => item.RecipientUserId == userId
            && item.VisibleAtUtc <= now && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now) && item.ArchivedAtUtc == null), effectivePermissions, includePersonal);
    }

    public async Task<Result<NotificationResponse>> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<NotificationSeverity>(request.Severity, true, out var severity)
            || !HrServiceSupport.HasText(request.EventType) || !HrServiceSupport.HasText(request.TitleAr)
            || !HrServiceSupport.HasText(request.TitleEn) || !HrServiceSupport.HasText(request.BodyAr)
            || !HrServiceSupport.HasText(request.BodyEn) || !HrServiceSupport.HasText(request.DeduplicationKey)
            || request.ExpiresAtUtc <= request.VisibleAtUtc
            || request.PermissionKeys is not null && (request.PermissionKeys.Count is 0 or > 256
                || request.PermissionKeys.Any(x => string.IsNullOrWhiteSpace(x) || !PermissionKeys.All.Contains(x.Trim()))))
            return Result.Failure<NotificationResponse>(SystemErrors.InvalidRequest);
        if (!await identityDbContext.Users.AsNoTracking().AnyAsync(item => item.Id == request.RecipientUserId, cancellationToken))
            return Result.Failure<NotificationResponse>(SystemErrors.NotFound);
        var item = new Notification
        {
            RecipientUserId = request.RecipientUserId,
            EventType = request.EventType.Trim(),
            Severity = severity,
            TitleAr = request.TitleAr.Trim(),
            TitleEn = request.TitleEn.Trim(),
            BodyAr = request.BodyAr.Trim(),
            BodyEn = request.BodyEn.Trim(),
            SourceEntityType = HrServiceSupport.TrimOrNull(request.SourceEntityType),
            SourceEntityId = request.SourceEntityId,
            DeepLink = HrServiceSupport.TrimOrNull(request.DeepLink),
            ScopeSnapshotJson = HrServiceSupport.TrimOrNull(request.ScopeSnapshotJson),
            AudiencePermissionKeysJson = request.PermissionKeys is null ? null : JsonSerializer.Serialize(request.PermissionKeys.Select(x => x.Trim()).Distinct(StringComparer.Ordinal)),
            DeduplicationKey = request.DeduplicationKey.Trim(),
            VisibleAtUtc = request.VisibleAtUtc ?? timeProvider.GetUtcNow(),
            ExpiresAtUtc = request.ExpiresAtUtc
        };
        dbContext.Notifications.Add(item);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Result.Failure<NotificationResponse>(SystemErrors.Conflict); }
        return Result.Success(ToResponse(item));
    }

    public async Task<Result<NotificationResponse>> ChangeStateAsync(
        Guid id,
        NotificationStateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId || string.IsNullOrWhiteSpace(request.Action))
            return Result.Failure<NotificationResponse>(SystemErrors.InvalidRequest);
        var effective = await ResolvePermissionsAsync(null, cancellationToken);
        if (effective.IsFailure) return Result.Failure<NotificationResponse>(effective.Error);
        var item = await NotificationPermissionFilter.Apply(dbContext.Notifications, effective.Value!, includePersonal: true).SingleOrDefaultAsync(
            notification => notification.Id == id && notification.RecipientUserId == userId,
            cancellationToken);
        if (item is null) return Result.Failure<NotificationResponse>(SystemErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(item.RowVersion, request.RowVersion))
            return Result.Failure<NotificationResponse>(SystemErrors.ConcurrencyConflict);
        var now = timeProvider.GetUtcNow();
        switch (request.Action.Trim().ToLowerInvariant())
        {
            case "read": item.ReadAtUtc ??= now; break;
            case "unread": item.ReadAtUtc = null; break;
            case "acknowledge": item.AcknowledgedAtUtc ??= now; item.AcknowledgedByUserId ??= userId; item.ReadAtUtc ??= now; break;
            case "archive": item.ArchivedAtUtc ??= now; item.ArchivedByUserId ??= userId; break;
            default: return Result.Failure<NotificationResponse>(SystemErrors.InvalidRequest);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(item));
    }

    private static NotificationResponse ToResponse(Notification item) => new(
        item.Id, item.EventType, item.Severity.ToString(), item.TitleAr, item.TitleEn, item.BodyAr, item.BodyEn,
        item.SourceEntityType, item.SourceEntityId, item.DeepLink, item.VisibleAtUtc, item.ExpiresAtUtc,
        item.ReadAtUtc, item.AcknowledgedAtUtc, item.ArchivedAtUtc, HrServiceSupport.EncodeRowVersion(item.RowVersion),
        item.AudiencePermissionKeysJson is null ? [] : JsonSerializer.Deserialize<string[]>(item.AudiencePermissionKeysJson) ?? []);
}
