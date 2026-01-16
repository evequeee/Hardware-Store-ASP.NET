using HardwareStore.Domain.Entities;

namespace HardwareStore.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetAvailableProductsAsync(CancellationToken cancellationToken = default);
}
