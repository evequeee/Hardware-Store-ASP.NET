using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using WebApplication.asp.net.c3.DAL.Interfaces;

namespace WebApplication.asp.net.c3.DAL.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly string _connectionString;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    private ICategoryRepository? _categories;
    private IBrandRepository? _brands;
    private IProductRepository? _products;

    public UnitOfWork(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ProductCatalogDb") 
            ?? throw new InvalidOperationException("Connection string 'ProductCatalogDb' not found.");
    }

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                _connection = new NpgsqlConnection(_connectionString);
                _connection.Open();
            }
            else if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
            return _connection;
        }
    }

    public IDbTransaction? Transaction => _transaction;

    public ICategoryRepository Categories
    {
        get
        {
            _categories ??= new CategoryRepository(Connection, Transaction);
            return _categories;
        }
    }

    public IBrandRepository Brands
    {
        get
        {
            _brands ??= new BrandRepository(Connection, Transaction);
            return _brands;
        }
    }

    public IProductRepository Products
    {
        get
        {
            _products ??= new ProductRepository(Connection, Transaction);
            return _products;
        }
    }

    public void BeginTransaction()
    {
        _transaction?.Dispose();
        _transaction = Connection.BeginTransaction();
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _transaction?.Commit();
        }
        catch
        {
            _transaction?.Rollback();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }

        await Task.CompletedTask;
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }
}
