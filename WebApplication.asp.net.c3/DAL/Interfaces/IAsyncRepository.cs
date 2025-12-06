using System.Linq.Expressions;

namespace WebApplication.asp.net.c3.DAL.Interfaces;

/// <summary>
/// Async Generic Repository pattern interface
/// </summary>
public interface IAsyncRepository<TEntity> where TEntity : class
{
    // Basic CRUD operations
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    // Query operations
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    // Pagination
    Task<IEnumerable<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
