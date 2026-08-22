using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.System;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.SystemServices;

internal sealed class DatasetVersionService(ApplicationDbContext dbContext, TimeProvider timeProvider) : IDatasetVersionService
{
    public async Task<Result<IReadOnlyList<DatasetVersionResponse>>> GetAsync(
        string? moduleKey,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.DatasetVersions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(moduleKey))
        {
            var key = moduleKey.Trim().ToLowerInvariant();
            query = query.Where(item => item.ModuleKey == key);
        }
        var rows = await query.OrderBy(item => item.ModuleKey).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<DatasetVersionResponse>>(rows.Select(ToResponse).ToArray());
    }

    public async Task<long> IncrementAsync(string moduleKey, CancellationToken cancellationToken = default)
    {
        var key = moduleKey.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100)
            throw new ArgumentException("A valid dataset module key is required.", nameof(moduleKey));
        var item = await dbContext.DatasetVersions.SingleOrDefaultAsync(row => row.ModuleKey == key, cancellationToken);
        if (item is null)
        {
            item = new DatasetVersion { ModuleKey = key, Version = 1, LastChangedAtUtc = timeProvider.GetUtcNow() };
            dbContext.DatasetVersions.Add(item);
        }
        else
        {
            item.Version++;
            item.LastChangedAtUtc = timeProvider.GetUtcNow();
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return item.Version;
    }

    private static DatasetVersionResponse ToResponse(DatasetVersion item) => new(
        item.ModuleKey, item.Version, item.LastChangedAtUtc, HrServiceSupport.EncodeRowVersion(item.RowVersion));
}
