using LogisticsERP.Domain.Common;

namespace LogisticsERP.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    IQueryable<TEntity> Query<TEntity>() where TEntity : Entity;
    void AddEntity<TEntity>(TEntity entity) where TEntity : Entity;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
