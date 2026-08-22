using System.Globalization;
using LogisticsERP.Application.Abstractions.Authentication;
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
    TimeProvider timeProvider) : INotificationService
{
    public async Task<Result<PageResponse<NotificationResponse>>> GetMineAsync(
        bool unreadOnly,
        int pageSize,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure<PageResponse<NotificationResponse>>(SystemErrors.CurrentUserUnavailable);
        if (pageSize is < 1 or > 200 || cursor is not null && !long.TryParse(cursor, out _))
            return Result.Failure<PageResponse<NotificationResponse>>(SystemErrors.InvalidRequest);
        var now = timeProvider.GetUtcNow();
        var query = dbContext.Notifications.AsNoTracking().Where(item =>
            item.RecipientUserId == userId && item.VisibleAtUtc <= now
            && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now) && item.ArchivedAtUtc == null);
        if (unreadOnly) query = query.Where(item => item.ReadAtUtc == null);
        if (cursor is not null)
        {
            var beforeTicks = long.Parse(cursor, CultureInfo.InvariantCulture);
            query = query.Where(item => item.VisibleAtUtc.UtcTicks < beforeTicks);
        }
        var rows = await query.OrderByDescending(item => item.VisibleAtUtc).ThenByDescending(item => item.Id)
            .Take(pageSize + 1).ToArrayAsync(cancellationToken);
        var hasNext = rows.Length > pageSize;
        var page = rows.Take(pageSize).ToArray();
        return Result.Success(new PageResponse<NotificationResponse>(
            page.Select(ToResponse).ToArray(),
            hasNext && page.Length > 0 ? page[^1].VisibleAtUtc.UtcTicks.ToString(CultureInfo.InvariantCulture) : null));
    }

    public async Task<Result<int>> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId) return Result.Failure<int>(SystemErrors.CurrentUserUnavailable);
        var now = timeProvider.GetUtcNow();
        var count = await dbContext.Notifications.CountAsync(item => item.RecipientUserId == userId
            && item.ReadAtUtc == null && item.ArchivedAtUtc == null && item.VisibleAtUtc <= now
            && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now), cancellationToken);
        return Result.Success(count);
    }

    public async Task<Result<NotificationResponse>> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<NotificationSeverity>(request.Severity, true, out var severity)
            || !HrServiceSupport.HasText(request.EventType) || !HrServiceSupport.HasText(request.TitleAr)
            || !HrServiceSupport.HasText(request.TitleEn) || !HrServiceSupport.HasText(request.BodyAr)
            || !HrServiceSupport.HasText(request.BodyEn) || !HrServiceSupport.HasText(request.DeduplicationKey)
            || request.ExpiresAtUtc <= request.VisibleAtUtc)
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
        var item = await dbContext.Notifications.SingleOrDefaultAsync(
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
        item.ReadAtUtc, item.AcknowledgedAtUtc, item.ArchivedAtUtc, HrServiceSupport.EncodeRowVersion(item.RowVersion));
}
