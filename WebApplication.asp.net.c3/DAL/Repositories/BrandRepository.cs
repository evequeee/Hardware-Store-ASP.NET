using Microsoft.EntityFrameworkCore;
using WebApplication.asp.net.c3.DAL.Interfaces;
using WebApplication.asp.net.c3.Data;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.DAL.Repositories;

/// <summary>
/// Repository for Brand entity with specific operations
/// Uses Eager Loading, Explicit Loading, and LINQ to Entities
/// </summary>
public class BrandRepository : AsyncRepository<Brand>, IBrandRepository
{
    public BrandRepository(HardwareStoreDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get all active brands - LINQ to Entities
    /// </summary>
    public async Task<IEnumerable<Brand>> GetActiveBrandsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get brand by name - LINQ to Entities
    /// </summary>
    public async Task<Brand?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(b => b.Name == name, cancellationToken);
    }

    /// <summary>
    /// Search brands by name - LINQ to Entities
    /// </summary>
    public async Task<IEnumerable<Brand>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.Name.Contains(searchTerm) && b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Check if brand has any products - LINQ to Entities
    /// </summary>
    public async Task<bool> HasProductsAsync(int brandId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.BrandId == brandId, cancellationToken);
    }

    /// <summary>
    /// Override GetByIdAsync to include products - Eager Loading
    /// </summary>
    public override async Task<Brand?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Products.Where(p => p.IsAvailable))
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    /// <summary>
    /// Get all brands ordered by name - LINQ to Entities
    /// </summary>
    public override async Task<IEnumerable<Brand>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }
}
