using Microsoft.EntityFrameworkCore.Storage;
using WebApplication.asp.net.c3.DAL.Interfaces;
using WebApplication.asp.net.c3.Data;

namespace WebApplication.asp.net.c3.DAL.Repositories;

/// <summary>
/// Unit of Work implementation for managing transactions across repositories
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly HardwareStoreDbContext _context;
    private IDbContextTransaction? _transaction;
    
    private ICategoryRepository? _categories;
    private IBrandRepository? _brands;
    private IProductRepository? _products;

    public UnitOfWork(HardwareStoreDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Category repository - lazy initialization
    /// </summary>
    public ICategoryRepository Categories
    {
        get
        {
            _categories ??= new CategoryRepository(_context);
            return _categories;
        }
    }

    /// <summary>
    /// Brand repository - lazy initialization
    /// </summary>
    public IBrandRepository Brands
    {
        get
        {
            _brands ??= new BrandRepository(_context);
            return _brands;
        }
    }

    /// <summary>
    /// Product repository - lazy initialization
    /// </summary>
    public IProductRepository Products
    {
        get
        {
            _products ??= new ProductRepository(_context);
            return _products;
        }
    }

    /// <summary>
    /// Save all changes to the database
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Begin a new transaction
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commit the current transaction
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to rollback.");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
