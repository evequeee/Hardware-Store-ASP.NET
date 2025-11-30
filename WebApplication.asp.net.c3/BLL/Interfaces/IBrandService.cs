using WebApplication.asp.net.c3.BLL.DTOs;

namespace WebApplication.asp.net.c3.BLL.Interfaces;

public interface IBrandService
{
    Task<IEnumerable<BrandDto>> GetAllBrandsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<BrandDto>> GetActiveBrandsAsync(CancellationToken cancellationToken = default);
    Task<BrandDto?> GetBrandByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<BrandDto>> SearchBrandsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<BrandDto> CreateBrandAsync(CreateBrandDto dto, CancellationToken cancellationToken = default);
    Task<BrandDto> UpdateBrandAsync(UpdateBrandDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteBrandAsync(int id, CancellationToken cancellationToken = default);
}
