using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.DAL.Interfaces;

public interface ICategoryRepository : IAsyncRepository<Category>
{
    Task<IEnumerable<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId, CancellationToken cancellationToken = default);
    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Category?> GetWithProductsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default);
}
