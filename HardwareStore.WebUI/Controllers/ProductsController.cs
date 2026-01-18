using HardwareStore.Application.Products.Commands.CreateProduct;
using HardwareStore.Application.Products.Commands.DeleteProduct;
using HardwareStore.Application.Products.Commands.UpdateProduct;
using HardwareStore.Application.Products.Queries.GetProductById;
using HardwareStore.Application.Products.Queries.GetProductsList;
using HardwareStore.Domain.Events;
using HardwareStore.Infrastructure.Messaging;
using HardwareStore.WebUI.Metrics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace HardwareStore.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEventPublisher _eventPublisher;
    private readonly BusinessMetricsService _metrics;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IMediator mediator,
        IEventPublisher eventPublisher,
        BusinessMetricsService metrics,
        ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _eventPublisher = eventPublisher;
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// Get all products
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Extensions.ApiRequestsCounter.Add(1,
                new KeyValuePair<string, object?>("service", "webapi"),
                new KeyValuePair<string, object?>("endpoint", "products/getall"));

            var query = new GetProductsListQuery();
            var result = await _mediator.Send(query);
            
            stopwatch.Stop();
            _metrics.RecordProductOperationDuration("get_all", stopwatch.ElapsedMilliseconds, true);
            Extensions.RequestDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("operation", "get_all_products"),
                new KeyValuePair<string, object?>("status", "success"));
            
            _logger.LogInformation("Retrieved {Count} products in {Duration}ms", result.Count, stopwatch.ElapsedMilliseconds);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordProductOperationDuration("get_all", stopwatch.ElapsedMilliseconds, false);
            _logger.LogError(ex, "Failed to retrieve products");
            throw;
        }
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(string id)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Extensions.ProductsViewedCounter.Add(1,
                new KeyValuePair<string, object?>("source", "webapi"));

            var query = new GetProductByIdQuery(id);
            var result = await _mediator.Send(query);

            stopwatch.Stop();

            if (result == null)
            {
                _metrics.RecordProductOperationDuration("get_by_id", stopwatch.ElapsedMilliseconds, false);
                return NotFound();
            }

            _metrics.RecordProductOperationDuration("get_by_id", stopwatch.ElapsedMilliseconds, true);
            _metrics.RecordProductViewed(id, result.Category);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordProductOperationDuration("get_by_id", stopwatch.ElapsedMilliseconds, false);
            _logger.LogError(ex, "Failed to retrieve product {ProductId}", id);
            throw;
        }
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<string>> Create([FromBody] CreateProductCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Extensions.ApiRequestsCounter.Add(1,
                new KeyValuePair<string, object?>("service", "webapi"),
                new KeyValuePair<string, object?>("endpoint", "products/create"));

            var id = await _mediator.Send(command);
            
            // Publish ProductCreatedEvent
            var @event = new ProductCreatedEvent
            {
                ProductId = id,
                Name = command.Name,
                Category = command.Category,
                Price = command.Price,
                StockQuantity = command.StockQuantity
            };
            await _eventPublisher.PublishAsync(@event, "products.created");
            
            stopwatch.Stop();
            _metrics.RecordProductCreated(command.Category);
            _metrics.RecordProductOperationDuration("create", stopwatch.ElapsedMilliseconds, true);
            
            _logger.LogInformation("Created product {ProductId} in category {Category}, event published", id, command.Category);
            
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordProductOperationDuration("create", stopwatch.ElapsedMilliseconds, false);
            _logger.LogError(ex, "Failed to create product");
            throw;
        }
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, [FromBody] UpdateProductCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (id != command.Id)
                return BadRequest("ID mismatch");

            Extensions.ApiRequestsCounter.Add(1,
                new KeyValuePair<string, object?>("service", "webapi"),
                new KeyValuePair<string, object?>("endpoint", "products/update"));

            // Get existing product for comparison
            var existingQuery = new GetProductByIdQuery(id);
            var existing = await _mediator.Send(existingQuery);
            
            var result = await _mediator.Send(command);

            stopwatch.Stop();

            if (!result)
            {
                _metrics.RecordProductOperationDuration("update", stopwatch.ElapsedMilliseconds, false);
                return NotFound();
            }

            // Publish ProductUpdatedEvent
            var @event = new ProductUpdatedEvent
            {
                ProductId = id,
                Name = command.Name,
                Category = command.Category,
                Price = command.Price,
                PreviousPrice = existing?.Price ?? 0,
                StockQuantity = command.StockQuantity,
                PreviousStockQuantity = existing?.StockQuantity ?? 0
            };
            await _eventPublisher.PublishAsync(@event, "products.updated");
            
            _metrics.RecordProductUpdated(command.Category);
            _metrics.RecordProductOperationDuration("update", stopwatch.ElapsedMilliseconds, true);
            
            _logger.LogInformation("Updated product {ProductId}, event published", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordProductOperationDuration("update", stopwatch.ElapsedMilliseconds, false);
            _logger.LogError(ex, "Failed to update product {ProductId}", id);
            throw;
        }
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Extensions.ApiRequestsCounter.Add(1,
                new KeyValuePair<string, object?>("service", "webapi"),
                new KeyValuePair<string, object?>("endpoint", "products/delete"));

            // Get product details before deletion for event
            var existingQuery = new GetProductByIdQuery(id);
            var existing = await _mediator.Send(existingQuery);
            
            var command = new DeleteProductCommand(id);
            var result = await _mediator.Send(command);

            stopwatch.Stop();

            if (!result)
            {
                _metrics.RecordProductOperationDuration("delete", stopwatch.ElapsedMilliseconds, false);
                return NotFound();
            }

            // Publish ProductDeletedEvent
            var @event = new ProductDeletedEvent
            {
                ProductId = id,
                ProductName = existing?.Name ?? "Unknown",
                Reason = "Deleted by user"
            };
            await _eventPublisher.PublishAsync(@event, "products.deleted");

            _metrics.RecordProductDeleted(existing?.Category ?? "unknown");
            _metrics.RecordProductOperationDuration("delete", stopwatch.ElapsedMilliseconds, true);
            
            _logger.LogInformation("Deleted product {ProductId}, event published", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordProductOperationDuration("delete", stopwatch.ElapsedMilliseconds, false);
            _logger.LogError(ex, "Failed to delete product {ProductId}", id);
            throw;
        }
    }
}
