using System.Linq.Expressions;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.DAL.Interfaces;

public interface IProductRepository : IAsyncRepository<Product>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetByBrandAsync(int brandId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Product?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetInStockAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetFeaturedAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    
    // Pagination, Filtering, Sorting
    Task<(IEnumerable<Product> Items, int TotalCount)> GetFilteredAsync(
        Expression<Func<Product, bool>>? filter = null,
        Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = null,
        int skip = 0,
        int take = 10,
        CancellationToken cancellationToken = default);
}
