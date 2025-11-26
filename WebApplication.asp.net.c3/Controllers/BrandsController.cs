using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication.asp.net.c3.Data;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandsController : ControllerBase
    {
        private readonly ProductCatalogDbContext _context;
        private readonly ILogger<BrandsController> _logger;

        public BrandsController(ProductCatalogDbContext context, ILogger<BrandsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Brands
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Brand>>> GetBrands(
            [FromQuery] bool includeInactive = false,
            [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.Brands.AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(b => b.IsActive);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(b =>
                        b.Name.Contains(search) ||
                        (b.Description != null && b.Description.Contains(search)));
                }

                var brands = await query
                    .OrderBy(b => b.Name)
                    .ToListAsync();

                return Ok(brands);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving brands");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Brands/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Brand>> GetBrand(long id)
        {
            try
            {
                var brand = await _context.Brands
                    .Include(b => b.Products.Where(p => p.IsActive))
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (brand == null)
                {
                    return NotFound(new { message = $"Brand with ID {id} not found" });
                }

                return Ok(brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving brand {BrandId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Brands/5/products
        [HttpGet("{id}/products")]
        public async Task<ActionResult<IEnumerable<Product>>> GetBrandProducts(
            long id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var brandExists = await _context.Brands.AnyAsync(b => b.Id == id);
                if (!brandExists)
                {
                    return NotFound(new { message = $"Brand with ID {id} not found" });
                }

                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.ProductImages.Where(img => img.IsPrimary))
                    .Where(p => p.BrandId == id && p.IsActive);

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
                _logger.LogError(ex, "Error retrieving products for brand {BrandId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/Brands
        [HttpPost]
        public async Task<ActionResult<Brand>> CreateBrand(Brand brand)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                brand.CreatedAt = DateTime.UtcNow;
                _context.Brands.Add(brand);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating brand");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/Brands/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBrand(long id, Brand brand)
        {
            if (id != brand.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            try
            {
                var existingBrand = await _context.Brands.FindAsync(id);
                if (existingBrand == null)
                {
                    return NotFound(new { message = $"Brand with ID {id} not found" });
                }

                existingBrand.Name = brand.Name;
                existingBrand.Description = brand.Description;
                existingBrand.LogoUrl = brand.LogoUrl;
                existingBrand.Website = brand.Website;
                existingBrand.IsActive = brand.IsActive;
                existingBrand.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await BrandExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating brand {BrandId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/Brands/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBrand(long id)
        {
            try
            {
                var brand = await _context.Brands
                    .Include(b => b.Products)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (brand == null)
                {
                    return NotFound(new { message = $"Brand with ID {id} not found" });
                }

                if (brand.Products.Any())
                {
                    return BadRequest(new { message = "Cannot delete brand with associated products" });
                }

                // М'яке видалення
                brand.IsDeleted = true;
                brand.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting brand {BrandId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<bool> BrandExists(long id)
        {
            return await _context.Brands.AnyAsync(e => e.Id == id);
        }
    }
}