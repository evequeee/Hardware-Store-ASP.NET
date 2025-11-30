using System.Data;
using Dapper;
using WebApplication.asp.net.c3.DAL.Interfaces;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.DAL.Repositories;

/// <summary>
/// Brand repository implementation using combined ADO.NET + Dapper
/// Demonstrates: Dapper for reading with mapping, ADO.NET for complex scenarios
/// </summary>
public class BrandRepository : IBrandRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public BrandRepository(IDbConnection connection, IDbTransaction? transaction)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction;
    }

    public async Task<Brand?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, description AS Description, logo_url AS LogoUrl, 
                   website_url AS WebsiteUrl, country AS Country, is_active AS IsActive,
                   created_at AS CreatedAt, updated_at AS UpdatedAt, is_deleted AS IsDeleted
            FROM brands
            WHERE id = @Id AND is_deleted = FALSE";

        return await _connection.QueryFirstOrDefaultAsync<Brand>(
            new CommandDefinition(sql, new { Id = id }, _transaction, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<Brand>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, description AS Description, logo_url AS LogoUrl, 
                   website_url AS WebsiteUrl, country AS Country, is_active AS IsActive,
                   created_at AS CreatedAt, updated_at AS UpdatedAt, is_deleted AS IsDeleted
            FROM brands
            WHERE is_deleted = FALSE
            ORDER BY name";

        return await _connection.QueryAsync<Brand>(
            new CommandDefinition(sql, transaction: _transaction, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<Brand>> GetActiveBrandsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, description AS Description, logo_url AS LogoUrl, 
                   website_url AS WebsiteUrl, country AS Country, is_active AS IsActive,
                   created_at AS CreatedAt, updated_at AS UpdatedAt, is_deleted AS IsDeleted
            FROM brands
            WHERE is_deleted = FALSE AND is_active = TRUE
            ORDER BY name";

        return await _connection.QueryAsync<Brand>(
            new CommandDefinition(sql, transaction: _transaction, cancellationToken: cancellationToken));
    }

    public async Task<Brand?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, description AS Description, logo_url AS LogoUrl, 
                   website_url AS WebsiteUrl, country AS Country, is_active AS IsActive,
                   created_at AS CreatedAt, updated_at AS UpdatedAt, is_deleted AS IsDeleted
            FROM brands
            WHERE is_deleted = FALSE AND LOWER(name) = LOWER(@Name)";

        return await _connection.QueryFirstOrDefaultAsync<Brand>(
            new CommandDefinition(sql, new { Name = name }, _transaction, cancellationToken: cancellationToken));
    }

    public async Task<bool> HasProductsAsync(int brandId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM products
            WHERE brand_id = @BrandId AND is_deleted = FALSE";

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { BrandId = brandId }, _transaction, cancellationToken: cancellationToken));
        
        return count > 0;
    }

    public async Task<IEnumerable<Brand>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, description AS Description, logo_url AS LogoUrl, 
                   website_url AS WebsiteUrl, country AS Country, is_active AS IsActive,
                   created_at AS CreatedAt, updated_at AS UpdatedAt, is_deleted AS IsDeleted
            FROM brands
            WHERE is_deleted = FALSE AND LOWER(name) LIKE LOWER(@SearchTerm)
            ORDER BY name";

        return await _connection.QueryAsync<Brand>(
            new CommandDefinition(sql, new { SearchTerm = $"%{searchTerm}%" }, _transaction, cancellationToken: cancellationToken));
    }

    public async Task<int> AddAsync(Brand entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO brands (name, description, logo_url, website_url, country, is_active, created_at, is_deleted)
            VALUES (@Name, @Description, @LogoUrl, @WebsiteUrl, @Country, @IsActive, @CreatedAt, FALSE)
            RETURNING id";

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                entity.Name,
                entity.Description,
                entity.LogoUrl,
                entity.WebsiteUrl,
                entity.Country,
                entity.IsActive,
                entity.CreatedAt
            }, _transaction, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(Brand entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE brands
            SET name = @Name,
                description = @Description,
                logo_url = @LogoUrl,
                website_url = @WebsiteUrl,
                country = @Country,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id AND is_deleted = FALSE";

        var rowsAffected = await _connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                entity.Id,
                entity.Name,
                entity.Description,
                entity.LogoUrl,
                entity.WebsiteUrl,
                entity.Country,
                entity.IsActive,
                UpdatedAt = DateTime.UtcNow
            }, _transaction, cancellationToken: cancellationToken));

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // Soft delete
        const string sql = @"
            UPDATE brands
            SET is_deleted = TRUE, updated_at = @UpdatedAt
            WHERE id = @Id AND is_deleted = FALSE";

        var rowsAffected = await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }, _transaction, cancellationToken: cancellationToken));

        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM brands WHERE id = @Id AND is_deleted = FALSE";

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Id = id }, _transaction, cancellationToken: cancellationToken));

        return count > 0;
    }
}
