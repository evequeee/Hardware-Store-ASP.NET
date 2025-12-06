using Microsoft.EntityFrameworkCore;
using WebApplication.asp.net.c3.DAL.Interfaces;
using WebApplication.asp.net.c3.Data;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.DAL.Repositories;

/// <summary>
/// Repository for Category entity with specific operations
/// Uses Eager Loading, Explicit Loading, and LINQ to Entities
/// </summary>
public class CategoryRepository : AsyncRepository<Category>, ICategoryRepository
{
    public CategoryRepository(HardwareStoreDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get all active categories - LINQ to Entities
    /// </summary>
    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get subcategories for a parent category - LINQ to Entities
    /// </summary>
    public async Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentCategoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.ParentCategoryId == parentCategoryId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get category by name - LINQ to Entities
    /// </summary>
    public async Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
    }

    /// <summary>
    /// Get category with products - Eager Loading
    /// </summary>
    public async Task<Category?> GetWithProductsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Products)
                .ThenInclude(p => p.Brand)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
    }

    /// <summary>
    /// Check if category has any products - LINQ to Entities
    /// </summary>
    public async Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.CategoryId == categoryId, cancellationToken);
    }

    /// <summary>
    /// Override GetByIdAsync to include parent category - Eager Loading
    /// </summary>
    public override async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// Get all categories with subcategories - Eager Loading
    /// </summary>
    public override async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.SubCategories)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
