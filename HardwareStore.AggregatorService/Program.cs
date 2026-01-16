using HardwareStore.AggregatorService.DTOs;
using HardwareStore.AggregatorService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults
builder.AddServiceDefaults();

// Register Typed HttpClient with Service Discovery
builder.Services.AddHttpClient<ProductsClient>(client =>
{
    client.BaseAddress = new Uri("http://webapi");
})
.AddServiceDiscovery();

var app = builder.Build();

// Map ServiceDefaults endpoints
app.MapDefaultEndpoints();

// Aggregator endpoint - combines data from multiple microservices
app.MapGet("/api/aggregator/dashboard", async (ProductsClient productsClient, CancellationToken ct) =>
{
    var products = await productsClient.GetAllProductsAsync(ct);

    var aggregatedData = new AggregatedDataDto
    {
        Products = products,
        TotalProducts = products.Count,
        TotalInventoryValue = products.Sum(p => p.Price * p.StockQuantity),
        ProductsByCategory = products
            .GroupBy(p => p.Category)
            .ToDictionary(g => g.Key, g => g.Count()),
        RetrievedAt = DateTime.UtcNow
    };

    return Results.Ok(aggregatedData);
})
.WithName("GetAggregatedDashboard")
.WithOpenApi();

// Aggregator endpoint for specific product with enriched data
app.MapGet("/api/aggregator/product/{id}", async (string id, ProductsClient productsClient, CancellationToken ct) =>
{
    var product = await productsClient.GetProductByIdAsync(id, ct);
    
    if (product == null)
    {
        return Results.NotFound(new { Message = $"Product with ID {id} not found" });
    }

    // Enrich product data with additional computed fields
    var enrichedProduct = new
    {
        Product = product,
        IsLowStock = product.StockQuantity < 10,
        StockValue = product.Price * product.StockQuantity,
        RetrievedAt = DateTime.UtcNow
    };

    return Results.Ok(enrichedProduct);
})
.WithName("GetEnrichedProduct")
.WithOpenApi();

app.Run();

