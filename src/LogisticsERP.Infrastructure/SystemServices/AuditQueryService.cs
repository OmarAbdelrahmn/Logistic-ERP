using System.Globalization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.System;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.SystemServices;

internal sealed class AuditQueryService(ApplicationDbContext dbContext) : IAuditQueryService
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
            items.Select(ToResponse).ToArray(),
            hasNext && items.Length > 0 ? items[^1].Sequence.ToString(CultureInfo.InvariantCulture) : null));
    }

    public async Task<Result<AuditEntryResponse>> GetAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.AuditEntries.AsNoTracking().SingleOrDefaultAsync(row => row.EventId == eventId, cancellationToken);
        return item is null ? Result.Failure<AuditEntryResponse>(SystemErrors.NotFound) : Result.Success(ToResponse(item));
    }

    private static AuditEntryResponse ToResponse(AuditEntry item) => new(
        item.EventId, item.Sequence, item.ActorUserId, item.ActorType, item.Action, item.Category,
        item.EntityType, item.EntityId, item.OccurredAtUtc, item.CorrelationId, item.Reason,
        item.BeforeJson, item.AfterJson, item.Source, item.SchemaVersion);
}
