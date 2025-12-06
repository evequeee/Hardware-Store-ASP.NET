using AutoMapper;
using WebApplication.asp.net.c3.BLL.DTOs;
using WebApplication.asp.net.c3.BLL.Exceptions;
using WebApplication.asp.net.c3.BLL.Interfaces;
using WebApplication.asp.net.c3.BLL.Models;
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
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            var createdProduct = await _unitOfWork.Products.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Product created successfully with Id: {ProductId}", createdProduct.Id);

            return _mapper.Map<ProductDto>(createdProduct);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
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
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Product updated successfully with Id: {ProductId}", dto.Id);

            return _mapper.Map<ProductDto>(product);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error updating product: {ProductId}", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        // Check if product exists
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException(nameof(Product), id);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            await _unitOfWork.Products.DeleteAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Product deleted successfully with Id: {ProductId}", id);

            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
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
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            var result = await _unitOfWork.Products.UpdateStockAsync(dto.ProductId, dto.Quantity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Product stock updated successfully for Id: {ProductId}, Quantity: {Quantity}", 
                dto.ProductId, dto.Quantity);

            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error updating product stock: {ProductId}", dto.ProductId);
            throw;
        }
    }

    public async Task<PagedResult<ProductDto>> GetFilteredProductsAsync(
        ProductFilterParams filterParams, 
        CancellationToken cancellationToken = default)
    {
        // Build filter expression
        System.Linq.Expressions.Expression<Func<Product, bool>>? filter = null;
        
        if (filterParams.CategoryId.HasValue || 
            filterParams.BrandId.HasValue || 
            !string.IsNullOrWhiteSpace(filterParams.SearchTerm) ||
            filterParams.MinPrice.HasValue ||
            filterParams.MaxPrice.HasValue ||
            filterParams.InStock.HasValue ||
            filterParams.IsFeatured.HasValue ||
            filterParams.MinRating.HasValue)
        {
            filter = p => 
                (!filterParams.CategoryId.HasValue || p.CategoryId == filterParams.CategoryId.Value) &&
                (!filterParams.BrandId.HasValue || p.BrandId == filterParams.BrandId.Value) &&
                (string.IsNullOrWhiteSpace(filterParams.SearchTerm) || 
                    p.Name.Contains(filterParams.SearchTerm) ||
                    (p.Description != null && p.Description.Contains(filterParams.SearchTerm)) ||
                    (p.Tags != null && p.Tags.Contains(filterParams.SearchTerm))) &&
                (!filterParams.MinPrice.HasValue || p.Price >= filterParams.MinPrice.Value) &&
                (!filterParams.MaxPrice.HasValue || p.Price <= filterParams.MaxPrice.Value) &&
                (!filterParams.InStock.HasValue || (filterParams.InStock.Value ? p.StockQuantity > 0 : p.StockQuantity == 0)) &&
                (!filterParams.IsFeatured.HasValue || p.IsFeatured == filterParams.IsFeatured.Value) &&
                (!filterParams.MinRating.HasValue || p.AverageRating >= filterParams.MinRating.Value) &&
                p.IsAvailable;
        }

        // Build sorting function
        Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = null;
        
        var sortBy = filterParams.SortBy?.ToLower() ?? "name";
        var sortOrder = filterParams.SortOrder?.ToLower() ?? "asc";

        orderBy = sortBy switch
        {
            "price" => query => sortOrder == "desc" 
                ? query.OrderByDescending(p => p.Price) 
                : query.OrderBy(p => p.Price),
            "rating" => query => sortOrder == "desc"
                ? query.OrderByDescending(p => p.AverageRating)
                : query.OrderBy(p => p.AverageRating),
            "date" => query => sortOrder == "desc"
                ? query.OrderByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.CreatedAt),
            "name" => query => sortOrder == "desc"
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            _ => query => query.OrderBy(p => p.Name)
        };

        // Get filtered data
        var (items, totalCount) = await _unitOfWork.Products.GetFilteredAsync(
            filter,
            orderBy,
            filterParams.Skip,
            filterParams.PageSize,
            cancellationToken);

        var productDtos = _mapper.Map<IEnumerable<ProductDto>>(items);

        return new PagedResult<ProductDto>(
            productDtos,
            filterParams.Page,
            filterParams.PageSize,
            totalCount);
    }
}

