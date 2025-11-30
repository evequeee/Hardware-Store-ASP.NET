using AutoMapper;
using WebApplication.asp.net.c3.BLL.DTOs;
using WebApplication.asp.net.c3.BLL.Exceptions;
using WebApplication.asp.net.c3.BLL.Interfaces;
using WebApplication.asp.net.c3.DAL.Interfaces;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.BLL.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Products.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        
        if (product == null)
        {
            throw new NotFoundException(nameof(Product), id);
        }

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto?> GetProductWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetWithDetailsAsync(id, cancellationToken);
        
        if (product == null)
        {
            throw new NotFoundException(nameof(Product), id);
        }

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        // Validate category exists
        var categoryExists = await _unitOfWork.Categories.ExistsAsync(categoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException(nameof(Category), categoryId);
        }

        var products = await _unitOfWork.Products.GetByCategoryAsync(categoryId, cancellationToken);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByBrandAsync(int brandId, CancellationToken cancellationToken = default)
    {
        // Validate brand exists
        var brandExists = await _unitOfWork.Brands.ExistsAsync(brandId, cancellationToken);
        if (!brandExists)
        {
            throw new NotFoundException(nameof(Brand), brandId);
        }

        var products = await _unitOfWork.Products.GetByBrandAsync(brandId, cancellationToken);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllProductsAsync(cancellationToken);
        }

        var products = await _unitOfWork.Products.SearchAsync(searchTerm, cancellationToken);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> GetInStockProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Products.GetInStockAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        // Business validation: check if category exists
        var categoryExists = await _unitOfWork.Categories.ExistsAsync(dto.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException(nameof(Category), dto.CategoryId);
        }

        // Business validation: check if brand exists
        var brandExists = await _unitOfWork.Brands.ExistsAsync(dto.BrandId, cancellationToken);
        if (!brandExists)
        {
            throw new NotFoundException(nameof(Brand), dto.BrandId);
        }

        // Business validation: validate discount price
        if (dto.DiscountPrice.HasValue && dto.DiscountPrice.Value >= dto.Price)
        {
            throw new BusinessConflictException("Discount price must be less than regular price.");
        }

        // Business validation: validate stock quantity
        if (dto.StockQuantity < 0)
        {
            throw new BusinessConflictException("Stock quantity cannot be negative.");
        }

        var product = _mapper.Map<Product>(dto);

        try
        {
            _unitOfWork.BeginTransaction();
            var id = await _unitOfWork.Products.AddAsync(product, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            product.Id = id;
            _logger.LogInformation("Product created successfully with Id: {ProductId}", id);

            return _mapper.Map<ProductDto>(product);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            _logger.LogError(ex, "Error creating product: {ProductName}", dto.Name);
            throw;
        }
    }

    public async Task<ProductDto> UpdateProductAsync(UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        // Check if product exists
        var exists = await _unitOfWork.Products.ExistsAsync(dto.Id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(Product), dto.Id);
        }

        // Business validation: check if category exists
        var categoryExists = await _unitOfWork.Categories.ExistsAsync(dto.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException(nameof(Category), dto.CategoryId);
        }

        // Business validation: check if brand exists
        var brandExists = await _unitOfWork.Brands.ExistsAsync(dto.BrandId, cancellationToken);
        if (!brandExists)
        {
            throw new NotFoundException(nameof(Brand), dto.BrandId);
        }

        // Business validation: validate discount price
        if (dto.DiscountPrice.HasValue && dto.DiscountPrice.Value >= dto.Price)
        {
            throw new BusinessConflictException("Discount price must be less than regular price.");
        }

        // Business validation: validate stock quantity
        if (dto.StockQuantity < 0)
        {
            throw new BusinessConflictException("Stock quantity cannot be negative.");
        }

        var product = _mapper.Map<Product>(dto);

        try
        {
            _unitOfWork.BeginTransaction();
            await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Product updated successfully with Id: {ProductId}", dto.Id);

            return _mapper.Map<ProductDto>(product);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            _logger.LogError(ex, "Error updating product: {ProductId}", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        // Check if product exists
        var exists = await _unitOfWork.Products.ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(Product), id);
        }

        try
        {
            _unitOfWork.BeginTransaction();
            var result = await _unitOfWork.Products.DeleteAsync(id, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Product deleted successfully with Id: {ProductId}", id);

            return result;
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            _logger.LogError(ex, "Error deleting product: {ProductId}", id);
            throw;
        }
    }

    public async Task<bool> UpdateStockAsync(UpdateStockDto dto, CancellationToken cancellationToken = default)
    {
        // Check if product exists
        var exists = await _unitOfWork.Products.ExistsAsync(dto.ProductId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(Product), dto.ProductId);
        }

        try
        {
            _unitOfWork.BeginTransaction();
            var result = await _unitOfWork.Products.UpdateStockAsync(dto.ProductId, dto.Quantity, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Product stock updated successfully for Id: {ProductId}, Quantity: {Quantity}", 
                dto.ProductId, dto.Quantity);

            return result;
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            _logger.LogError(ex, "Error updating product stock: {ProductId}", dto.ProductId);
            throw;
        }
    }
}
