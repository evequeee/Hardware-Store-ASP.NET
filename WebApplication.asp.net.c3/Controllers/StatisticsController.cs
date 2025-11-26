using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication.asp.net.c3.Data;

namespace WebApplication.asp.net.c3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly ProductCatalogDbContext _context;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(ProductCatalogDbContext context, ILogger<StatisticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Statistics/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult> GetDashboardStatistics()
        {
            try
            {
                var stats = new
                {
                    TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                    TotalCategories = await _context.Categories.CountAsync(c => c.IsActive),
                    TotalBrands = await _context.Brands.CountAsync(b => b.IsActive),
                    FeaturedProducts = await _context.Products.CountAsync(p => p.IsFeatured && p.IsActive),
                    OutOfStock = await _context.Products.CountAsync(p => p.StockQuantity == 0 && p.IsActive),
                    TotalReviews = await _context.ProductReviews.CountAsync(r => r.IsApproved),
                    AverageRating = await _context.Products
                        .Where(p => p.AverageRating.HasValue)
                        .AverageAsync(p => p.AverageRating)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Statistics/products/top-rated
        [HttpGet("products/top-rated")]
        public async Task<ActionResult> GetTopRatedProducts([FromQuery] int count = 10)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => p.IsActive && p.AverageRating.HasValue)
                    .OrderByDescending(p => p.AverageRating)
                    .ThenByDescending(p => p.ReviewCount)
                    .Take(count)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Price,
                        p.AverageRating,
                        p.ReviewCount,
                        Category = p.Category.Name,
                        Brand = p.Brand.Name
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top rated products");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Statistics/categories/products-count
        [HttpGet("categories/products-count")]
        public async Task<ActionResult> GetCategoriesProductCount()
        {
            try
            {
                var stats = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        CategoryId = c.Id,
                        CategoryName = c.Name,
                        ProductsCount = c.Products.Count(p => p.IsActive)
                    })
                    .OrderByDescending(x => x.ProductsCount)
                    .ToListAsync();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category statistics");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}