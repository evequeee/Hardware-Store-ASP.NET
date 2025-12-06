using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.DAL.Interfaces;

public interface IBrandRepository : IAsyncRepository<Brand>
{
    Task<IEnumerable<Brand>> GetActiveBrandsAsync(CancellationToken cancellationToken = default);
    Task<Brand?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Brand>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<bool> HasProductsAsync(int brandId, CancellationToken cancellationToken = default);
}
