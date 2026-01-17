using Grpc.Core;
using HardwareStore.AggregatorService.Caching;
using HardwareStore.AggregatorService.DTOs;
using HardwareStore.AggregatorService.Protos;
using HardwareStore.AggregatorService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults
builder.AddServiceDefaults();

// Add Redis distributed caching
builder.AddRedisDistributedCache("redis");

// Register gRPC Client with Service Discovery
builder.Services.AddGrpcClient<ProductGrpcService.ProductGrpcServiceClient>(options =>
{
    options.Address = new Uri("http://webapi");
})
.AddServiceDiscovery();

// Register ProductGrpcClient wrapper
builder.Services.AddScoped<ProductGrpcClient>();

// Register Legacy HTTP Client (fallback)
builder.Services.AddHttpClient<ProductsClient>(client =>
{
    client.BaseAddress = new Uri("http://webapi");
})
.AddServiceDiscovery();

// Register Aggregator Cache Service
builder.Services.AddSingleton<IAggregatorCacheService, AggregatorCacheService>();

var app = builder.Build();

// Map ServiceDefaults endpoints
app.MapDefaultEndpoints();

// Aggregator endpoint - combines data from multiple microservices via gRPC
app.MapGet("/api/aggregator/dashboard", async (
    ProductGrpcClient grpcClient,
    IAggregatorCacheService cacheService,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    logger.LogInformation("Aggregator dashboard requested");

    var aggregatedData = await cacheService.GetOrCreateAsync(
        AggregatorCacheKeys.Dashboard,
        async () =>
        {
            try
            {
                // Use gRPC to fetch products
                var products = await grpcClient.GetAllProductsAsync(ct);

                return new AggregatedDataDto
                {
                    Products = products,
                    TotalProducts = products.Count,
                    TotalInventoryValue = products.Sum(p => p.Price * p.StockQuantity),
                    ProductsByCategory = products
                        .GroupBy(p => p.Category)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    RetrievedAt = DateTime.UtcNow
                };
            }
            catch (RpcException ex)
            {
                logger.LogError(ex, "gRPC error fetching products for dashboard. Status: {StatusCode}", ex.StatusCode);
                throw;
            }
        },
        TimeSpan.FromSeconds(30));

    return Results.Ok(aggregatedData);
})
.WithName("GetAggregatedDashboard")
.WithOpenApi();

// Aggregator endpoint for specific product with enriched data via gRPC
app.MapGet("/api/aggregator/product/{id}", async (
    string id,
    ProductGrpcClient grpcClient,
    IAggregatorCacheService cacheService,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    logger.LogInformation("Aggregator enriched product requested: {ProductId}", id);

    var cacheKey = AggregatorCacheKeys.GetEnrichedProductKey(id);

    var enrichedProduct = await cacheService.GetOrCreateAsync(
        cacheKey,
        async () =>
        {
            try
            {
                var product = await grpcClient.GetProductByIdAsync(id, ct);

                if (product == null)
                {
                    return null;
                }

                return new EnrichedProductDto
                {
                    Product = product,
                    IsLowStock = product.StockQuantity < 10,
                    StockValue = product.Price * product.StockQuantity,
                    RetrievedAt = DateTime.UtcNow
                };
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                logger.LogWarning("Product {ProductId} not found via gRPC", id);
                return null;
            }
        },
        TimeSpan.FromSeconds(30));

    if (enrichedProduct == null)
    {
        return Results.NotFound(new { Message = $"Product with ID {id} not found" });
    }

    return Results.Ok(enrichedProduct);
})
.WithName("GetEnrichedProduct")
.WithOpenApi();

// Aggregator endpoint for parallel gRPC calls
app.MapGet("/api/aggregator/summary", async (
    ProductGrpcClient grpcClient,
    IAggregatorCacheService cacheService,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    logger.LogInformation("Aggregator summary requested - parallel gRPC calls");

    const string cacheKey = "aggregator:summary";

    var summary = await cacheService.GetOrCreateAsync(
        cacheKey,
        async () =>
        {
            // Parallel gRPC calls using Task.WhenAll
            var productsTask = grpcClient.GetAllProductsAsync(ct);
            
            // Add more parallel calls here if there are other services
            // var customersTask = customersClient.GetAllCustomersAsync(ct);
            // var ordersTask = ordersClient.GetAllOrdersAsync(ct);

            await Task.WhenAll(productsTask);

            var products = await productsTask;

            return new
            {
                Products = new
                {
                    Total = products.Count,
                    InStock = products.Count(p => p.StockQuantity > 0),
                    LowStock = products.Count(p => p.StockQuantity > 0 && p.StockQuantity < 10),
                    OutOfStock = products.Count(p => p.StockQuantity == 0),
                    TotalValue = products.Sum(p => p.Price * p.StockQuantity),
                    Categories = products.Select(p => p.Category).Distinct().Count()
                },
                RetrievedAt = DateTime.UtcNow
            };
        },
        TimeSpan.FromSeconds(30));

    return Results.Ok(summary);
})
.WithName("GetAggregatedSummary")
.WithOpenApi();

// Cache statistics endpoint
app.MapGet("/api/aggregator/cache/stats", (IAggregatorCacheService cacheService) =>
{
    var stats = cacheService.GetStatistics();
    return Results.Ok(stats);
}).WithName("GetAggregatorCacheStats").WithOpenApi();

app.Run();

