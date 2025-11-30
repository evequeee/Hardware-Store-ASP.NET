using Microsoft.AspNetCore.Mvc;
using WebApplication.asp.net.c3.BLL.DTOs;
using WebApplication.asp.net.c3.BLL.Interfaces;

namespace WebApplication.asp.net.c3.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all products
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var products = await _productService.GetAllProductsAsync(cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(
        int id, 
        [FromQuery] bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        var product = includeDetails
            ? await _productService.GetProductWithDetailsAsync(id, cancellationToken)
            : await _productService.GetProductByIdAsync(id, cancellationToken);

        return Ok(product);
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    [HttpGet("category/{categoryId:int}")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetByCategory(
        int categoryId, 
        CancellationToken cancellationToken = default)
    {
        var products = await _productService.GetProductsByCategoryAsync(categoryId, cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Get products by brand
    /// </summary>
    [HttpGet("brand/{brandId:int}")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetByBrand(
        int brandId, 
        CancellationToken cancellationToken = default)
    {
        var products = await _productService.GetProductsByBrandAsync(brandId, cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Search products
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> Search(
        [FromQuery] string query,
        CancellationToken cancellationToken = default)
    {
        var products = await _productService.SearchProductsAsync(query, cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Get products in stock
    /// </summary>
    [HttpGet("in-stock")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetInStock(CancellationToken cancellationToken = default)
    {
        var products = await _productService.GetInStockProductsAsync(cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductDto dto, 
        CancellationToken cancellationToken = default)
    {
        var product = await _productService.CreateProductAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(
        int id, 
        [FromBody] UpdateProductDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return BadRequest("ID mismatch");
        }

        var product = await _productService.UpdateProductAsync(dto, cancellationToken);
        return Ok(product);
    }

    /// <summary>
    /// Update product stock
    /// </summary>
    [HttpPatch("{id:int}/stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStock(
        int id,
        [FromBody] UpdateStockDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (id != dto.ProductId)
        {
            return BadRequest("ID mismatch");
        }

        await _productService.UpdateStockAsync(dto, cancellationToken);
        return Ok(new { message = "Stock updated successfully" });
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        await _productService.DeleteProductAsync(id, cancellationToken);
        return NoContent();
    }
}
