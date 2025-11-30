using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.DAL.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetByBrandAsync(int brandId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Product?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetInStockAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
}
