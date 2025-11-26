using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication.asp.net.c3.Data;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductCatalogDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ProductCatalogDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] long? categoryId = null,
            [FromQuery] long? brandId = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? isFeatured = null,
            [FromQuery] string? sortBy = "name",
            [FromQuery] string? sortOrder = "asc")
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.ProductImages.Where(img => img.IsPrimary))
                    .AsQueryable();

                // Фільтрація за пошуковим запитом
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p =>
                        p.Name.Contains(search) ||
                        p.Description!.Contains(search) ||
                        p.Tags!.Contains(search));
                }

                // Фільтрація за категорією
                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                // Фільтрація за брендом
                if (brandId.HasValue)
                {
                    query = query.Where(p => p.BrandId == brandId.Value);
                }

                // Фільтрація за ціною
                if (minPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= maxPrice.Value);
                }

                // Фільтрація Featured продуктів
                if (isFeatured.HasValue)
                {
                    query = query.Where(p => p.IsFeatured == isFeatured.Value);
                }

                // Тільки активні продукти
                query = query.Where(p => p.IsActive);

                // Сортування
                query = sortBy?.ToLower() switch
                {
                    "price" => sortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.Price)
                        : query.OrderBy(p => p.Price),
                    "rating" => sortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.AverageRating)
                        : query.OrderBy(p => p.AverageRating),
                    "date" => sortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.CreatedAt)
                        : query.OrderBy(p => p.CreatedAt),
                    _ => sortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(p => p.Name)
                        : query.OrderBy(p => p.Name)
                };

                // Підрахунок загальної кількості
                var totalCount = await query.CountAsync();

                // Пагінація
                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Додаємо метадані до відповіді
                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", ((int)Math.Ceiling(totalCount / (double)pageSize)).ToString());

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(long id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductAttributes)
                    .Include(p => p.ProductReviews.Where(r => r.IsApproved))
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return NotFound(new { message = $"Product with ID {id} not found" });
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Products/featured
        [HttpGet("featured")]
        public async Task<ActionResult<IEnumerable<Product>>> GetFeaturedProducts([FromQuery] int count = 6)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.ProductImages.Where(img => img.IsPrimary))
                    .Where(p => p.IsFeatured && p.IsActive)
                    .OrderByDescending(p => p.AverageRating)
                    .Take(count)
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving featured products");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Products/5/reviews
        [HttpGet("{id}/reviews")]
        public async Task<ActionResult<IEnumerable<ProductReview>>> GetProductReviews(long id)
        {
            try
            {
                var productExists = await _context.Products.AnyAsync(p => p.Id == id);
                if (!productExists)
                {
                    return NotFound(new { message = $"Product with ID {id} not found" });
                }

                var reviews = await _context.ProductReviews
                    .Where(r => r.ProductId == id && r.IsApproved)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reviews for product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            try
            {
                // Перевірка валідності моделі
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Перевірка існування категорії та бренду
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == product.CategoryId);
                var brandExists = await _context.Brands.AnyAsync(b => b.Id == product.BrandId);

                if (!categoryExists || !brandExists)
                {
                    return BadRequest(new { message = "Invalid CategoryId or BrandId" });
                }

                product.CreatedAt = DateTime.UtcNow;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(long id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    return NotFound(new { message = $"Product with ID {id} not found" });
                }

                // Оновлюємо поля
                existingProduct.Name = product.Name;
                existingProduct.SKU = product.SKU;
                existingProduct.Description = product.Description;
                existingProduct.DetailedDescription = product.DetailedDescription;
                existingProduct.Price = product.Price;
                existingProduct.DiscountedPrice = product.DiscountedPrice;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.IsActive = product.IsActive;
                existingProduct.IsFeatured = product.IsFeatured;
                existingProduct.Tags = product.Tags;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.BrandId = product.BrandId;
                existingProduct.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ProductExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/Products/5 (М'яке видалення)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(long id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = $"Product with ID {id} not found" });
                }

                // М'яке видалення
                product.IsDeleted = true;
                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<bool> ProductExists(long id)
        {
            return await _context.Products.AnyAsync(e => e.Id == id);
        }
    }
}