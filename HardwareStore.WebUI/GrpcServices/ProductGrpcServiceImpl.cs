using Grpc.Core;
using HardwareStore.Application.Products.Commands.CreateProduct;
using HardwareStore.Application.Products.Commands.DeleteProduct;
using HardwareStore.Application.Products.Commands.UpdateProduct;
using HardwareStore.Application.Products.Queries.GetProductById;
using HardwareStore.Application.Products.Queries.GetProductsList;
using HardwareStore.WebUI.Caching;
using HardwareStore.WebUI.Protos;
using MediatR;

namespace HardwareStore.WebUI.GrpcServices;

/// <summary>
/// gRPC service implementation for Product operations
/// </summary>
public class ProductGrpcServiceImpl : ProductGrpcService.ProductGrpcServiceBase
{
    private readonly IMediator _mediator;
    private readonly ITwoLevelCacheService _cacheService;
    private readonly ILogger<ProductGrpcServiceImpl> _logger;

    public ProductGrpcServiceImpl(
        IMediator mediator, 
        ITwoLevelCacheService cacheService,
        ILogger<ProductGrpcServiceImpl> logger)
    {
        _mediator = mediator;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Get all products via gRPC
    /// </summary>
    public override async Task<GetAllProductsResponse> GetAllProducts(
        GetAllProductsRequest request, 
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetAllProducts called. Peer: {Peer}", context.Peer);

        try
        {
            var products = await _cacheService.GetOrCreateAsync(
                CacheKeys.AllProducts,
                async () =>
                {
                    var query = new GetProductsListQuery();
                    return await _mediator.Send(query, context.CancellationToken);
                },
                CacheKeys.Durations.Short,
                CacheKeys.Durations.Medium);

            var response = new GetAllProductsResponse();
            
            if (products != null)
            {
                foreach (var product in products)
                {
                    response.Products.Add(MapToProductMessage(product));
                }
            }

            _logger.LogInformation("gRPC GetAllProducts returned {Count} products", response.Products.Count);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetAllProducts");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    /// <summary>
    /// Get product by ID via gRPC
    /// </summary>
    public override async Task<GetProductByIdResponse> GetProductById(
        GetProductByIdRequest request, 
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetProductById called. Id: {ProductId}, Peer: {Peer}", 
            request.Id, context.Peer);

        try
        {
            var cacheKey = CacheKeys.GetProductByIdKey(request.Id);
            
            var product = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    var query = new GetProductByIdQuery(request.Id);
                    return await _mediator.Send(query, context.CancellationToken);
                },
                CacheKeys.Durations.Short,
                CacheKeys.Durations.Medium);

            var response = new GetProductByIdResponse
            {
                Found = product != null
            };

            if (product != null)
            {
                response.Product = MapToProductMessage(product);
                _logger.LogInformation("gRPC GetProductById found product {ProductId}", request.Id);
            }
            else
            {
                _logger.LogWarning("gRPC GetProductById - product {ProductId} not found", request.Id);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC GetProductById for {ProductId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    /// <summary>
    /// Create product via gRPC
    /// </summary>
    public override async Task<CreateProductResponse> CreateProduct(
        CreateProductRequest request, 
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC CreateProduct called. Name: {Name}, Peer: {Peer}", 
            request.Name, context.Peer);

        try
        {
            var command = new CreateProductCommand
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                Price = (decimal)request.Price,
                StockQuantity = request.StockQuantity,
                Manufacturer = request.Manufacturer
            };

            var id = await _mediator.Send(command, context.CancellationToken);

            // Invalidate cache after write operation
            await _cacheService.RemoveByPatternAsync(CacheKeys.ProductsPattern);

            _logger.LogInformation("gRPC CreateProduct created product {ProductId}", id);

            return new CreateProductResponse
            {
                Id = id,
                Success = true,
                Message = "Product created successfully"
            };
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning("gRPC CreateProduct validation failed: {Errors}", 
                string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));
            
            throw new RpcException(new Status(StatusCode.InvalidArgument, 
                string.Join(", ", ex.Errors.Select(e => e.ErrorMessage))));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC CreateProduct");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    /// <summary>
    /// Update product via gRPC
    /// </summary>
    public override async Task<UpdateProductResponse> UpdateProduct(
        UpdateProductRequest request, 
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC UpdateProduct called. Id: {ProductId}, Peer: {Peer}", 
            request.Id, context.Peer);

        try
        {
            var command = new UpdateProductCommand
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                Price = (decimal)request.Price,
                StockQuantity = request.StockQuantity,
                Manufacturer = request.Manufacturer,
                IsAvailable = request.IsAvailable
            };

            var result = await _mediator.Send(command, context.CancellationToken);

            if (!result)
            {
                _logger.LogWarning("gRPC UpdateProduct - product {ProductId} not found", request.Id);
                throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} not found"));
            }

            // Invalidate cache after write operation
            await _cacheService.RemoveAsync(CacheKeys.GetProductByIdKey(request.Id));
            await _cacheService.RemoveAsync(CacheKeys.AllProducts);

            _logger.LogInformation("gRPC UpdateProduct updated product {ProductId}", request.Id);

            return new UpdateProductResponse
            {
                Success = true,
                Message = "Product updated successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning("gRPC UpdateProduct validation failed: {Errors}", 
                string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));
            
            throw new RpcException(new Status(StatusCode.InvalidArgument, 
                string.Join(", ", ex.Errors.Select(e => e.ErrorMessage))));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC UpdateProduct for {ProductId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    /// <summary>
    /// Delete product via gRPC
    /// </summary>
    public override async Task<DeleteProductResponse> DeleteProduct(
        DeleteProductRequest request, 
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC DeleteProduct called. Id: {ProductId}, Peer: {Peer}", 
            request.Id, context.Peer);

        try
        {
            var command = new DeleteProductCommand(request.Id);
            var result = await _mediator.Send(command, context.CancellationToken);

            if (!result)
            {
                _logger.LogWarning("gRPC DeleteProduct - product {ProductId} not found", request.Id);
                throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} not found"));
            }

            // Invalidate cache after write operation
            await _cacheService.RemoveAsync(CacheKeys.GetProductByIdKey(request.Id));
            await _cacheService.RemoveAsync(CacheKeys.AllProducts);

            _logger.LogInformation("gRPC DeleteProduct deleted product {ProductId}", request.Id);

            return new DeleteProductResponse
            {
                Success = true,
                Message = "Product deleted successfully"
            };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC DeleteProduct for {ProductId}", request.Id);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    /// <summary>
    /// Map ProductDto to gRPC ProductMessage
    /// </summary>
    private static ProductMessage MapToProductMessage(ProductDto product)
    {
        return new ProductMessage
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Category = product.Category,
            Price = (double)product.Price,
            StockQuantity = product.StockQuantity,
            Manufacturer = product.Manufacturer,
            IsAvailable = product.IsAvailable,
            IsInStock = product.IsInStock
        };
    }
}
