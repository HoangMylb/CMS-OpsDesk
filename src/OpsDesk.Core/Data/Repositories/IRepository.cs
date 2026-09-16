using System.Linq.Expressions;

namespace OpsDesk.Core.Data.Repositories;

/// <summary>
/// Generic repository interface for common data access operations.
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetAllAsync(bool asNoTracking = true, CancellationToken cancellationToken = default);
    IQueryable<TEntity> Query(bool asNoTracking = true);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
