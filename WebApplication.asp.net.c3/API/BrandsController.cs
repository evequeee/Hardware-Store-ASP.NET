using Microsoft.AspNetCore.Mvc;
using WebApplication.asp.net.c3.BLL.DTOs;
using WebApplication.asp.net.c3.BLL.Interfaces;

namespace WebApplication.asp.net.c3.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;
    private readonly ILogger<BrandsController> _logger;

    public BrandsController(IBrandService brandService, ILogger<BrandsController> logger)
    {
        _brandService = brandService ?? throw new ArgumentNullException(nameof(brandService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all brands
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BrandDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BrandDto>>> GetAll(
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var brands = activeOnly
            ? await _brandService.GetActiveBrandsAsync(cancellationToken)
            : await _brandService.GetAllBrandsAsync(cancellationToken);

        return Ok(brands);
    }

    /// <summary>
    /// Get brand by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var brand = await _brandService.GetBrandByIdAsync(id, cancellationToken);
        return Ok(brand);
    }

    /// <summary>
    /// Search brands by name
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<BrandDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BrandDto>>> Search(
        [FromQuery] string query,
        CancellationToken cancellationToken = default)
    {
        var brands = await _brandService.SearchBrandsAsync(query, cancellationToken);
        return Ok(brands);
    }

    /// <summary>
    /// Create a new brand
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BrandDto>> Create(
        [FromBody] CreateBrandDto dto, 
        CancellationToken cancellationToken = default)
    {
        var brand = await _brandService.CreateBrandAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = brand.Id }, brand);
    }

    /// <summary>
    /// Update an existing brand
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BrandDto>> Update(
        int id, 
        [FromBody] UpdateBrandDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return BadRequest("ID mismatch");
        }

        var brand = await _brandService.UpdateBrandAsync(dto, cancellationToken);
        return Ok(brand);
    }

    /// <summary>
    /// Delete a brand
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        await _brandService.DeleteBrandAsync(id, cancellationToken);
        return NoContent();
    }
}
