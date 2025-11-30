using System.Data;

namespace WebApplication.asp.net.c3.DAL.Interfaces;

/// <summary>
/// Unit of Work pattern for managing database transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Repositories
    ICategoryRepository Categories { get; }
    IBrandRepository Brands { get; }
    IProductRepository Products { get; }

    // Transaction management
    void BeginTransaction();
    Task CommitAsync(CancellationToken cancellationToken = default);
    void Rollback();

    // Connection management
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
}
