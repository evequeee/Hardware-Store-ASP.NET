using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication.asp.net.c3.Data;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ProductCatalogDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ProductCatalogDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories([FromQuery] bool includeInactive = false)
        {
            try
            {
                var query = _context.Categories
                    .Include(c => c.ParentCategory)
                    .Include(c => c.SubCategories)
                    .AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(c => c.IsActive);
                }

                var categories = await query
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Categories/tree
        [HttpGet("tree")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategoriesTree()
        {
            try
            {
                // Отримуємо тільки кореневі категорії (без батьківських)
                var rootCategories = await _context.Categories
                    .Include(c => c.SubCategories.OrderBy(sc => sc.SortOrder))
                        .ThenInclude(sc => sc.SubCategories.OrderBy(ssc => ssc.SortOrder))
                    .Where(c => c.ParentCategoryId == null && c.IsActive)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                return Ok(rootCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories tree");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(long id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.ParentCategory)
                    .Include(c => c.SubCategories)
                    .Include(c => c.Products.Where(p => p.IsActive))
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return NotFound(new { message = $"Category with ID {id} not found" });
                }

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category {CategoryId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Categories/5/products
        [HttpGet("{id}/products")]
        public async Task<ActionResult<IEnumerable<Product>>> GetCategoryProducts(
            long id,
            [FromQuery] bool includeSubcategories = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == id);
                if (!categoryExists)
                {
                    return NotFound(new { message = $"Category with ID {id} not found" });
                }

                IQueryable<Product> query;

                if (includeSubcategories)
                {
                    // Отримуємо всі ID підкатегорій
                    var subcategoryIds = await GetAllSubcategoryIds(id);
                    subcategoryIds.Add(id);

                    query = _context.Products
                        .Include(p => p.Brand)
                        .Include(p => p.Category)
                        .Include(p => p.ProductImages.Where(img => img.IsPrimary))
                        .Where(p => subcategoryIds.Contains(p.CategoryId) && p.IsActive);
                }
                else
                {
                    query = _context.Products
                        .Include(p => p.Brand)
                        .Include(p => p.Category)
                        .Include(p => p.ProductImages.Where(img => img.IsPrimary))
                        .Where(p => p.CategoryId == id && p.IsActive);
                }

                var totalCount = await query.CountAsync();

                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for category {CategoryId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/Categories
        [HttpPost]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Перевірка, чи існує батьківська категорія
                if (category.ParentCategoryId.HasValue)
                {
                    var parentExists = await _context.Categories
                        .AnyAsync(c => c.Id == category.ParentCategoryId.Value);

                    if (!parentExists)
                    {
                        return BadRequest(new { message = "Parent category does not exist" });
                    }
                }

                category.CreatedAt = DateTime.UtcNow;
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(long id, Category category)
        {
            if (id != category.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            try
            {
                var existingCategory = await _context.Categories.FindAsync(id);
                if (existingCategory == null)
                {
                    return NotFound(new { message = $"Category with ID {id} not found" });
                }

                // Перевірка циклічних посилань
                if (category.ParentCategoryId.HasValue &&
                    await IsCircularReference(id, category.ParentCategoryId.Value))
                {
                    return BadRequest(new { message = "Circular reference detected" });
                }

                existingCategory.Name = category.Name;
                existingCategory.Description = category.Description;
                existingCategory.ImageUrl = category.ImageUrl;
                existingCategory.IsActive = category.IsActive;
                existingCategory.SortOrder = category.SortOrder;
                existingCategory.ParentCategoryId = category.ParentCategoryId;
                existingCategory.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CategoryExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(long id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.SubCategories)
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return NotFound(new { message = $"Category with ID {id} not found" });
                }

                // Перевірка чи є підкатегорії або продукти
                if (category.SubCategories.Any())
                {
                    return BadRequest(new { message = "Cannot delete category with subcategories" });
                }

                if (category.Products.Any())
                {
                    return BadRequest(new { message = "Cannot delete category with products" });
                }

                // М'яке видалення
                category.IsDeleted = true;
                category.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // Допоміжні методи
        private async Task<List<long>> GetAllSubcategoryIds(long parentId)
        {
            var result = new List<long>();
            var directChildren = await _context.Categories
                .Where(c => c.ParentCategoryId == parentId)
                .Select(c => c.Id)
                .ToListAsync();

            result.AddRange(directChildren);

            foreach (var childId in directChildren)
            {
                var descendants = await GetAllSubcategoryIds(childId);
                result.AddRange(descendants);
            }

            return result;
        }

        private async Task<bool> IsCircularReference(long categoryId, long parentId)
        {
            if (categoryId == parentId)
            {
                return true;
            }

            var parent = await _context.Categories.FindAsync(parentId);
            if (parent?.ParentCategoryId == null)
            {
                return false;
            }

            return await IsCircularReference(categoryId, parent.ParentCategoryId.Value);
        }

        private async Task<bool> CategoryExists(long id)
        {
            return await _context.Categories.AnyAsync(e => e.Id == id);
        }
    }
}