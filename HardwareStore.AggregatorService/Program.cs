using HardwareStore.AggregatorService.DTOs;
using HardwareStore.AggregatorService.HealthChecks;
using HardwareStore.AggregatorService.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// 1. First register health checks (BEFORE AddServiceDefaults)
builder.Services.AddHealthChecks()
    // Liveness check - basic app responsiveness
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    // Downstream WebAPI service health check
    .AddCheck<WebApiHealthCheck>(
        name: "webapi-downstream",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "downstream", "http", "ready" });

// 2. Add ServiceDefaults
builder.AddServiceDefaults();

// 3. Add RabbitMQ Client
builder.AddRabbitMQClient("rabbitmq");

// 4. Add HttpClientFactory for health checks
builder.Services.AddHttpClient("WebApiHealthCheck");

// 5. Register Typed HttpClient with Service Discovery and Custom Resilience
builder.Services.AddHttpClient<ProductsClient>(client =>
{
    client.BaseAddress = new Uri("http://webapi");
})
.AddServiceDiscovery()
.AddCustomResilienceHandler("ProductsClient", isCritical: true);

// 6. Add custom aggregator metrics
var aggregatorMeter = new Meter("HardwareStore.Aggregator", "1.0.0");

var aggregatorRequestsCounter = aggregatorMeter.CreateCounter<long>(
    "aggregator.requests.total",
    unit: "{requests}",
    description: "Total aggregator requests");

var aggregatorLatencyHistogram = aggregatorMeter.CreateHistogram<double>(
    "aggregator.request.duration",
    unit: "ms",
    description: "Aggregator request duration in milliseconds");

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("HardwareStore.Aggregator");
    });

var app = builder.Build();

// Map ServiceDefaults endpoints
app.MapDefaultEndpoints();

// Aggregator endpoint - combines data from multiple microservices
app.MapGet("/api/aggregator/dashboard", async (ProductsClient productsClient, CancellationToken ct) =>
{
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        aggregatorRequestsCounter.Add(1, 
            new KeyValuePair<string, object?>("endpoint", "dashboard"),
            new KeyValuePair<string, object?>("status", "started"));
        
        // Record request start in Extensions metrics
        Extensions.ApiRequestsCounter.Add(1, 
            new KeyValuePair<string, object?>("service", "aggregator"),
            new KeyValuePair<string, object?>("endpoint", "dashboard"));

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

        stopwatch.Stop();
        aggregatorLatencyHistogram.Record(stopwatch.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("endpoint", "dashboard"),
            new KeyValuePair<string, object?>("status", "success"));
        
        Extensions.RequestDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("service", "aggregator"),
            new KeyValuePair<string, object?>("endpoint", "dashboard"),
            new KeyValuePair<string, object?>("status", "success"));

        return Results.Ok(aggregatedData);
    }
    catch (Exception)
    {
        stopwatch.Stop();
        aggregatorLatencyHistogram.Record(stopwatch.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("endpoint", "dashboard"),
            new KeyValuePair<string, object?>("status", "failure"));
        
        Extensions.RequestDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("service", "aggregator"),
            new KeyValuePair<string, object?>("endpoint", "dashboard"),
            new KeyValuePair<string, object?>("status", "failure"));
        
        throw;
    }
})
.WithName("GetAggregatedDashboard")
.WithOpenApi();

// Aggregator endpoint for specific product with enriched data
app.MapGet("/api/aggregator/product/{id}", async (string id, ProductsClient productsClient, CancellationToken ct) =>
{
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        aggregatorRequestsCounter.Add(1,
            new KeyValuePair<string, object?>("endpoint", "product"),
            new KeyValuePair<string, object?>("status", "started"));
        
        Extensions.ProductsViewedCounter.Add(1,
            new KeyValuePair<string, object?>("source", "aggregator"));

        var product = await productsClient.GetProductByIdAsync(id, ct);
    
        if (product == null)
        {
            stopwatch.Stop();
            aggregatorLatencyHistogram.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("endpoint", "product"),
                new KeyValuePair<string, object?>("status", "not_found"));
            
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

        stopwatch.Stop();
        aggregatorLatencyHistogram.Record(stopwatch.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("endpoint", "product"),
            new KeyValuePair<string, object?>("status", "success"));

        return Results.Ok(enrichedProduct);
    }
    catch (Exception)
    {
        stopwatch.Stop();
        aggregatorLatencyHistogram.Record(stopwatch.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("endpoint", "product"),
            new KeyValuePair<string, object?>("status", "failure"));
        throw;
    }
})
.WithName("GetEnrichedProduct")
.WithOpenApi();

app.Run();

