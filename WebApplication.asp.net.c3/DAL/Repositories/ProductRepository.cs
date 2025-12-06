using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WebApplication.asp.net.c3.DAL.Interfaces;
using WebApplication.asp.net.c3.Data;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.DAL.Repositories;

/// <summary>
/// Repository for Product entity with specific operations
/// Uses Eager Loading, Explicit Loading, and LINQ to Entities
/// </summary>
public class ProductRepository : AsyncRepository<Product>, IProductRepository
{
    public ProductRepository(HardwareStoreDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get products by category - LINQ to Entities with Eager Loading
    /// </summary>
    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId && p.IsAvailable)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.AverageRating)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get products by brand - LINQ to Entities with Eager Loading
    /// </summary>
    public async Task<IEnumerable<Product>> GetByBrandAsync(int brandId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.BrandId == brandId && p.IsAvailable)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.AverageRating)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Search products by name, description, or tags - LINQ to Entities
    /// </summary>
    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        
        return await _dbSet
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => 
                (p.Name.ToLower().Contains(lowerSearchTerm) || 
                 (p.Description != null && p.Description.ToLower().Contains(lowerSearchTerm)) ||
                 (p.Tags != null && p.Tags.ToLower().Contains(lowerSearchTerm))) &&
                p.IsAvailable)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.AverageRating)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get product with all details - Eager Loading
    /// </summary>
    public async Task<Product?> GetWithDetailsAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Include(p => p.ProductAttributes)
            .Include(p => p.ProductReviews)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
    }

    /// <summary>
    /// Get products in stock - LINQ to Entities
    /// </summary>
    public async Task<IEnumerable<Product>> GetInStockAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.StockQuantity > 0 && p.IsAvailable)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.AverageRating)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get featured products - LINQ to Entities
    /// </summary>
    public async Task<IEnumerable<Product>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.IsFeatured && p.IsAvailable)
            .OrderByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.ReviewCount)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get all featured products - LINQ to Entities (overload for interface)
    /// </summary>
    public async Task<IEnumerable<Product>> GetFeaturedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.IsFeatured && p.IsAvailable)
            .OrderByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.ReviewCount)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Update product stock quantity - LINQ to Entities
    /// </summary>
    public async Task<bool> UpdateStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        var product = await _dbSet.FindAsync(new object[] { productId }, cancellationToken);
        
        if (product == null)
        {
            return false;
        }

        product.StockQuantity = quantity;
        product.IsAvailable = quantity > 0;
        product.UpdatedAt = DateTime.UtcNow;

        return true;
    }

    /// <summary>
    /// Override GetByIdAsync to include basic relations - Eager Loading
    /// </summary>
    public override async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// Get all products with basic relations - Eager Loading
    /// </summary>
    public override async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .OrderByDescending(p => p.IsFeatured)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get filtered, sorted and paginated products - LINQ to Entities
    /// </summary>
    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetFilteredAsync(
        Expression<Func<Product, bool>>? filter = null,
        Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = null,
        int skip = 0,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _dbSet
            .Include(p => p.Brand)
            .Include(p => p.Category);

        // Apply filter
        if (filter != null)
        {
            query = query.Where(filter);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        if (orderBy != null)
        {
            query = orderBy(query);
        }
        else
        {
            // Default sorting
            query = query.OrderBy(p => p.Name);
        }

        // Apply pagination
        var items = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}

