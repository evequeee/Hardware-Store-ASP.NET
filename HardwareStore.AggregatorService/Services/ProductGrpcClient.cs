using Grpc.Core;
using HardwareStore.AggregatorService.DTOs;
using HardwareStore.AggregatorService.Protos;

namespace HardwareStore.AggregatorService.Services;

/// <summary>
/// gRPC client for Product service with error handling and logging
/// </summary>
public class ProductGrpcClient
{
    private readonly ProductGrpcService.ProductGrpcServiceClient _client;
    private readonly ILogger<ProductGrpcClient> _logger;

    public ProductGrpcClient(
        ProductGrpcService.ProductGrpcServiceClient client,
        ILogger<ProductGrpcClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Get all products via gRPC
    /// </summary>
    public async Task<List<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("gRPC GetAllProducts - calling WebAPI");
            
            var request = new GetAllProductsRequest();
            var response = await _client.GetAllProductsAsync(request, cancellationToken: cancellationToken);

            var products = response.Products.Select(MapToProductDto).ToList();
            
            _logger.LogInformation("gRPC GetAllProducts - received {Count} products", products.Count);
            return products;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error in GetAllProducts. Status: {StatusCode}", ex.StatusCode);
            throw;
        }
    }

    /// <summary>
    /// Get product by ID via gRPC
    /// </summary>
    public async Task<ProductDto?> GetProductByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("gRPC GetProductById - calling WebAPI for {ProductId}", id);
            
            var request = new GetProductByIdRequest { Id = id };
            var response = await _client.GetProductByIdAsync(request, cancellationToken: cancellationToken);

            if (!response.Found)
            {
                _logger.LogWarning("gRPC GetProductById - product {ProductId} not found", id);
                return null;
            }

            var product = MapToProductDto(response.Product);
            _logger.LogInformation("gRPC GetProductById - received product {ProductId}", id);
            return product;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning("gRPC GetProductById - product {ProductId} not found", id);
            return null;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error in GetProductById for {ProductId}. Status: {StatusCode}", id, ex.StatusCode);
            throw;
        }
    }

    /// <summary>
    /// Create product via gRPC
    /// </summary>
    public async Task<string> CreateProductAsync(CreateProductDto product, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("gRPC CreateProduct - calling WebAPI for {ProductName}", product.Name);
            
            var request = new CreateProductRequest
            {
                Name = product.Name,
                Description = product.Description,
                Category = product.Category,
                Price = (double)product.Price,
                StockQuantity = product.StockQuantity,
                Manufacturer = product.Manufacturer
            };
            
            var response = await _client.CreateProductAsync(request, cancellationToken: cancellationToken);

            if (!response.Success)
            {
                _logger.LogWarning("gRPC CreateProduct failed: {Message}", response.Message);
                throw new InvalidOperationException(response.Message);
            }

            _logger.LogInformation("gRPC CreateProduct - created product {ProductId}", response.Id);
            return response.Id;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error in CreateProduct. Status: {StatusCode}", ex.StatusCode);
            throw;
        }
    }

    /// <summary>
    /// Update product via gRPC
    /// </summary>
    public async Task<bool> UpdateProductAsync(UpdateProductDto product, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("gRPC UpdateProduct - calling WebAPI for {ProductId}", product.Id);
            
            var request = new UpdateProductRequest
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Category = product.Category,
                Price = (double)product.Price,
                StockQuantity = product.StockQuantity,
                Manufacturer = product.Manufacturer,
                IsAvailable = product.IsAvailable
            };
            
            var response = await _client.UpdateProductAsync(request, cancellationToken: cancellationToken);

            _logger.LogInformation("gRPC UpdateProduct - updated product {ProductId}: {Success}", product.Id, response.Success);
            return response.Success;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning("gRPC UpdateProduct - product {ProductId} not found", product.Id);
            return false;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error in UpdateProduct for {ProductId}. Status: {StatusCode}", product.Id, ex.StatusCode);
            throw;
        }
    }

    /// <summary>
    /// Delete product via gRPC
    /// </summary>
    public async Task<bool> DeleteProductAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("gRPC DeleteProduct - calling WebAPI for {ProductId}", id);
            
            var request = new DeleteProductRequest { Id = id };
            var response = await _client.DeleteProductAsync(request, cancellationToken: cancellationToken);

            _logger.LogInformation("gRPC DeleteProduct - deleted product {ProductId}: {Success}", id, response.Success);
            return response.Success;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning("gRPC DeleteProduct - product {ProductId} not found", id);
            return false;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error in DeleteProduct for {ProductId}. Status: {StatusCode}", id, ex.StatusCode);
            throw;
        }
    }

    /// <summary>
    /// Map ProductMessage to ProductDto
    /// </summary>
    private static ProductDto MapToProductDto(ProductMessage message)
    {
        return new ProductDto
        {
            Id = message.Id,
            Name = message.Name,
            Description = message.Description,
            Category = message.Category,
            Price = (decimal)message.Price,
            StockQuantity = message.StockQuantity
        };
    }
}

/// <summary>
/// DTO for creating a product
/// </summary>
public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
}

/// <summary>
/// DTO for updating a product
/// </summary>
public class UpdateProductDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}
