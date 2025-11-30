using AutoMapper;
using WebApplication.asp.net.c3.BLL.DTOs;
using WebApplication.asp.net.c3.BLL.Exceptions;
using WebApplication.asp.net.c3.BLL.Interfaces;
using WebApplication.asp.net.c3.DAL.Interfaces;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.BLL.Services;

public class BrandService : IBrandService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<BrandService> _logger;

    public BrandService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<BrandService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<BrandDto>> GetAllBrandsAsync(CancellationToken cancellationToken = default)
    {
        var brands = await _unitOfWork.Brands.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<BrandDto>>(brands);
    }

    public async Task<IEnumerable<BrandDto>> GetActiveBrandsAsync(CancellationToken cancellationToken = default)
    {
        var brands = await _unitOfWork.Brands.GetActiveBrandsAsync(cancellationToken);
        return _mapper.Map<IEnumerable<BrandDto>>(brands);
    }

    public async Task<BrandDto?> GetBrandByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var brand = await _unitOfWork.Brands.GetByIdAsync(id, cancellationToken);
        
        if (brand == null)
        {
            throw new NotFoundException(nameof(Brand), id);
        }

        return _mapper.Map<BrandDto>(brand);
    }

    public async Task<IEnumerable<BrandDto>> SearchBrandsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllBrandsAsync(cancellationToken);
        }

        var brands = await _unitOfWork.Brands.SearchByNameAsync(searchTerm, cancellationToken);
        return _mapper.Map<IEnumerable<BrandDto>>(brands);
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandDto dto, CancellationToken cancellationToken = default)
    {
        // Business validation: check if name already exists
        var existingBrand = await _unitOfWork.Brands.GetByNameAsync(dto.Name, cancellationToken);
        if (existingBrand != null)
        {
            throw new BusinessConflictException($"Brand with name '{dto.Name}' already exists.");
        }

        var brand = _mapper.Map<Brand>(dto);

        try
        {
            _unitOfWork.BeginTransaction();
            var id = await _unitOfWork.Brands.AddAsync(brand, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            brand.Id = id;
            _logger.LogInformation("Brand created successfully with Id: {BrandId}", id);

            return _mapper.Map<BrandDto>(brand);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            _logger.LogError(ex, "Error creating brand: {BrandName}", dto.Name);
            throw;
        }
    }

    public async Task<BrandDto> UpdateBrandAsync(UpdateBrandDto dto, CancellationToken cancellationToken = default)
    {
        // Check if brand exists
        var exists = await _unitOfWork.Brands.ExistsAsync(dto.Id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(Brand), dto.Id);
        }

        // Business validation: check if name already exists (excluding current brand)
        var existingBrand = await _unitOfWork.Brands.GetByNameAsync(dto.Name, cancellationToken);
        if (existingBrand != null && existingBrand.Id != dto.Id)
        {
            throw new BusinessConflictException($"Brand with name '{dto.Name}' already exists.");
        }

        var brand = _mapper.Map<Brand>(dto);

        try
        {
            _unitOfWork.BeginTransaction();
            await _unitOfWork.Brands.UpdateAsync(brand, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Brand updated successfully with Id: {BrandId}", dto.Id);

            return _mapper.Map<BrandDto>(brand);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            _logger.LogError(ex, "Error updating brand: {BrandId}", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteBrandAsync(int id, CancellationToken cancellationToken = default)
    {
        // Check if brand exists
        var exists = await _unitOfWork.Brands.ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(Brand), id);
        }

        // Business rule: cannot delete brand with products
        var hasProducts = await _unitOfWork.Brands.HasProductsAsync(id, cancellationToken);
        if (hasProducts)
        {
            throw new BusinessConflictException("Cannot delete brand that has products. Remove or reassign products first.");
        }

        try
        {
            _unitOfWork.BeginTransaction();
            var result = await _unitOfWork.Brands.DeleteAsync(id, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Brand deleted successfully with Id: {BrandId}", id);

            return result;
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            _logger.LogError(ex, "Error deleting brand: {BrandId}", id);
            throw;
        }
    }
}
