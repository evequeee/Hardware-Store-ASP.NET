using AutoMapper;
using WebApplication.asp.net.c3.BLL.DTOs;
using WebApplication.asp.net.c3.BLL.Exceptions;
using WebApplication.asp.net.c3.BLL.Interfaces;
using WebApplication.asp.net.c3.DAL.Interfaces;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.BLL.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<IEnumerable<CategoryDto>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetActiveCategoriesAsync(cancellationToken);
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
        
        if (category == null)
        {
            throw new NotFoundException(nameof(Category), id);
        }

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(int parentId, CancellationToken cancellationToken = default)
    {
        // Validate parent exists
        var parentExists = await _unitOfWork.Categories.ExistsAsync(parentId, cancellationToken);
        if (!parentExists)
        {
            throw new NotFoundException(nameof(Category), parentId);
        }

        var categories = await _unitOfWork.Categories.GetSubCategoriesAsync(parentId, cancellationToken);
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        // Business validation: check if name already exists
        var existingCategory = await _unitOfWork.Categories.GetByNameAsync(dto.Name, cancellationToken);
        if (existingCategory != null)
        {
            throw new BusinessConflictException($"Category with name '{dto.Name}' already exists.");
        }

        // Validate parent category if specified
        if (dto.ParentCategoryId.HasValue)
        {
            var parentExists = await _unitOfWork.Categories.ExistsAsync(dto.ParentCategoryId.Value, cancellationToken);
            if (!parentExists)
            {
                throw new NotFoundException(nameof(Category), dto.ParentCategoryId.Value);
            }
        }

        var category = _mapper.Map<Category>(dto);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            var createdCategory = await _unitOfWork.Categories.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Category created successfully with Id: {CategoryId}", createdCategory.Id);

            return _mapper.Map<CategoryDto>(createdCategory);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error creating category: {CategoryName}", dto.Name);
            throw;
        }
    }

    public async Task<CategoryDto> UpdateCategoryAsync(UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        // Check if category exists
        var exists = await _unitOfWork.Categories.ExistsAsync(dto.Id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(Category), dto.Id);
        }

        // Business validation: check if name already exists (excluding current category)
        var existingCategory = await _unitOfWork.Categories.GetByNameAsync(dto.Name, cancellationToken);
        if (existingCategory != null && existingCategory.Id != dto.Id)
        {
            throw new BusinessConflictException($"Category with name '{dto.Name}' already exists.");
        }

        // Validate parent category if specified
        if (dto.ParentCategoryId.HasValue)
        {
            if (dto.ParentCategoryId.Value == dto.Id)
            {
                throw new BusinessConflictException("Category cannot be its own parent.");
            }

            var parentExists = await _unitOfWork.Categories.ExistsAsync(dto.ParentCategoryId.Value, cancellationToken);
            if (!parentExists)
            {
                throw new NotFoundException(nameof(Category), dto.ParentCategoryId.Value);
            }
        }

        var category = _mapper.Map<Category>(dto);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            await _unitOfWork.Categories.UpdateAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Category updated successfully with Id: {CategoryId}", dto.Id);

            return _mapper.Map<CategoryDto>(category);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error updating category: {CategoryId}", dto.Id);
            throw;
        }
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        // Check if category exists
        var exists = await _unitOfWork.Categories.ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException(nameof(Category), id);
        }

        // Business rule: cannot delete category with products
        var hasProducts = await _unitOfWork.Categories.HasProductsAsync(id, cancellationToken);
        if (hasProducts)
        {
            throw new BusinessConflictException("Cannot delete category that has products. Remove or reassign products first.");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);
            if (category != null)
            {
                await _unitOfWork.Categories.DeleteAsync(category, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Category deleted successfully with Id: {CategoryId}", id);

            return category != null;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
            throw;
        }
    }
}
