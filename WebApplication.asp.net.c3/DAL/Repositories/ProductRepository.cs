using System.Data;
using Dapper;
using WebApplication.asp.net.c3.DAL.Interfaces;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.DAL.Repositories;

/// <summary>
/// Product repository implementation using ADO.NET + Dapper with multi-mapping
/// Demonstrates: QueryAsync, ExecuteAsync, multi-mapping for complex operations
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public ProductRepository(IDbConnection connection, IDbTransaction? transaction)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, description AS Description, sku AS Sku, 
                   category_id AS CategoryId, brand_id AS BrandId, price AS Price, 
                   discount_price AS DiscountPrice, stock_quantity AS StockQuantity,
                   is_available AS IsAvailable, is_featured AS IsFeatured,
                   created_at AS CreatedAt, updated_at AS UpdatedAt, is_deleted AS IsDeleted
            FROM products
            WHERE id = @Id AND is_deleted = FALSE";

        return await _connection.QueryFirstOrDefaultAsync<Product>(
            new CommandDefinition(sql, new { Id = id }, _transaction, cancellationToken: cancellationToken));
    }

    public async Task<Product?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        // Multi-mapping: Product + Category + Brand
        const string sql = @"
            SELECT 
                p.id AS Id, p.name AS Name, p.description AS Description, p.sku AS Sku, 
                p.category_id AS CategoryId, p.brand_id AS BrandId, p.price AS Price, 
                p.discount_price AS DiscountPrice, p.stock_quantity AS StockQuantity,
                p.is_available AS IsAvailable, p.is_featured AS IsFeatured,
                p.created_at AS CreatedAt, p.updated_at AS UpdatedAt, p.is_deleted AS IsDeleted,
                c.id AS Id, c.name AS Name, c.description AS Description, c.image_url AS ImageUrl,
                c.parent_category_id AS ParentCategoryId, c.is_active AS IsActive, c.sort_order AS SortOrder,
                c.created_at AS CreatedAt, c.updated_at AS UpdatedAt, c.is_deleted AS IsDeleted,
                b.id AS Id, b.name AS Name, b.description AS Description, b.logo_url AS LogoUrl,
                b.website_url AS WebsiteUrl, b.country AS Country, b.is_active AS IsActive,
                b.created_at AS CreatedAt, b.updated_at AS UpdatedAt, b.is_deleted AS IsDeleted
            FROM products p
            LEFT JOIN categories c ON p.category_id = c.id AND c.is_deleted = FALSE
            LEFT JOIN brands b ON p.brand_id = b.id AND b.is_deleted = FALSE
            WHERE p.id = @Id AND p.is_deleted = FALSE";

        var products = await _connection.QueryAsync<Product, Category, Brand, Product>(
            new CommandDefinition(sql, new { Id = id }, _transaction, cancellationToken: cancellationToken),
            (product, category, brand) =>
            {
                product.Category = category;
                product.Brand = brand;
                return product;
            },
            splitOn: "Id,Id");

        return products.FirstOrDefault();
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, description AS Description, sku AS Sku, 
                   category_id AS CategoryId, brand_id AS BrandId, price AS Price, 
                   discount_price AS DiscountPrice, stock_quantity AS StockQuantity,
                   is_available AS IsAvailable, is_featured AS IsFeatured,
                   created_at AS CreatedAt, updated_at AS UpdatedAt, is_deleted AS IsDeleted
            FROM products
            WHERE is_deleted = FALSE
            ORDER BY name";

        return await _connection.QueryAsync<Product>(
            new CommandDefinition(sql, transaction: _transaction, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, description AS Description, sku AS Sku, 
                   category_id AS CategoryId, brand_id AS BrandId, price AS Price, 
                   discount_price AS DiscountPrice, stock_quantity AS StockQuantity,
                   is_available AS IsAvailable, is_featured AS IsFeatured,
                   created_at AS CreatedAt, updated_at AS UpdatedAt, is_deleted AS IsDeleted
            FROM products
            WHERE category_id = @CategoryId AND is_deleted = FALSE
            ORDER BY name";

        return await _connection.QueryAsync<Product>(
            new CommandDefinition(sql, new { CategoryId = categoryId }, _transaction, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<Product>> GetByBrandAsync(int brandId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, description AS Description, sku AS Sku, 
                   category_id AS CategoryId, brand_id AS BrandId, price AS Price, 
                   discount_price AS DiscountPrice, stock_quantity AS StockQuantity,
                   is_available AS IsAvailable, is_featured AS IsFeatured,
                   created_at AS CreatedAt, updated_at AS UpdatedAt, is_deleted AS IsDeleted
            FROM products
            WHERE brand_id = @BrandId AND is_deleted = FALSE
            ORDER BY name";

        return await _connection.QueryAsync<Product>(
            new CommandDefinition(sql, new { BrandId = brandId }, _transaction, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, description AS Description, sku AS Sku, 
                   category_id AS CategoryId, brand_id AS BrandId, price AS Price, 
                   discount_price AS DiscountPrice, stock_quantity AS StockQuantity,
                   is_available AS IsAvailable, is_featured AS IsFeatured,
                   created_at AS CreatedAt, updated_at AS UpdatedAt, is_deleted AS IsDeleted
            FROM products
            WHERE is_deleted = FALSE 
              AND (LOWER(name) LIKE LOWER(@SearchTerm) OR LOWER(description) LIKE LOWER(@SearchTerm))
            ORDER BY name";

        return await _connection.QueryAsync<Product>(
            new CommandDefinition(sql, new { SearchTerm = $"%{searchTerm}%" }, _transaction, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<Product>> GetInStockAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id AS Id, name AS Name, description AS Description, sku AS Sku, 
                   category_id AS CategoryId, brand_id AS BrandId, price AS Price, 
                   discount_price AS DiscountPrice, stock_quantity AS StockQuantity,
                   is_available AS IsAvailable, is_featured AS IsFeatured,
                   created_at AS CreatedAt, updated_at AS UpdatedAt, is_deleted AS IsDeleted
            FROM products
            WHERE is_deleted = FALSE AND stock_quantity > 0 AND is_available = TRUE
            ORDER BY name";

        return await _connection.QueryAsync<Product>(
            new CommandDefinition(sql, transaction: _transaction, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE products
            SET stock_quantity = stock_quantity + @Quantity,
                updated_at = @UpdatedAt
            WHERE id = @ProductId AND is_deleted = FALSE";

        var rowsAffected = await _connection.ExecuteAsync(
            new CommandDefinition(sql, new 
            { 
                ProductId = productId, 
                Quantity = quantity, 
                UpdatedAt = DateTime.UtcNow 
            }, _transaction, cancellationToken: cancellationToken));

        return rowsAffected > 0;
    }

    public async Task<int> AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO products (name, description, sku, category_id, brand_id, price, discount_price, 
                                  stock_quantity, is_available, is_featured, created_at, is_deleted)
            VALUES (@Name, @Description, @Sku, @CategoryId, @BrandId, @Price, @DiscountPrice, 
                    @StockQuantity, @IsAvailable, @IsFeatured, @CreatedAt, FALSE)
            RETURNING id";

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                entity.Name,
                entity.Description,
                entity.Sku,
                entity.CategoryId,
                entity.BrandId,
                entity.Price,
                entity.DiscountPrice,
                entity.StockQuantity,
                entity.IsAvailable,
                entity.IsFeatured,
                entity.CreatedAt
            }, _transaction, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE products
            SET name = @Name,
                description = @Description,
                sku = @Sku,
                category_id = @CategoryId,
                brand_id = @BrandId,
                price = @Price,
                discount_price = @DiscountPrice,
                stock_quantity = @StockQuantity,
                is_available = @IsAvailable,
                is_featured = @IsFeatured,
                updated_at = @UpdatedAt
            WHERE id = @Id AND is_deleted = FALSE";

        var rowsAffected = await _connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                entity.Id,
                entity.Name,
                entity.Description,
                entity.Sku,
                entity.CategoryId,
                entity.BrandId,
                entity.Price,
                entity.DiscountPrice,
                entity.StockQuantity,
                entity.IsAvailable,
                entity.IsFeatured,
                UpdatedAt = DateTime.UtcNow
            }, _transaction, cancellationToken: cancellationToken));

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // Soft delete
        const string sql = @"
            UPDATE products
            SET is_deleted = TRUE, updated_at = @UpdatedAt
            WHERE id = @Id AND is_deleted = FALSE";

        var rowsAffected = await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }, _transaction, cancellationToken: cancellationToken));

        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM products WHERE id = @Id AND is_deleted = FALSE";

        var count = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Id = id }, _transaction, cancellationToken: cancellationToken));

        return count > 0;
    }
}
