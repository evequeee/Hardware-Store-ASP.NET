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
    // Downstream services health checks via URL
    .AddUrlGroup(
        new Uri("http://webapi/health"),
        name: "webapi-downstream",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "downstream", "ready" },
        timeout: TimeSpan.FromSeconds(3))
    .AddUrlGroup(
        new Uri("http://aggregator/health"),
        name: "aggregator-downstream",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "downstream", "ready" },
        timeout: TimeSpan.FromSeconds(3));

// 2. Add ServiceDefaults
builder.AddServiceDefaults();

// 3. Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 4. Add custom gateway metrics
var gatewayMeter = new Meter("HardwareStore.Gateway", "1.0.0");

var gatewayRequestsCounter = gatewayMeter.CreateCounter<long>(
    "gateway.requests.total",
    unit: "{requests}",
    description: "Total gateway requests");

var gatewayLatencyHistogram = gatewayMeter.CreateHistogram<double>(
    "gateway.request.duration",
    unit: "ms",
    description: "Gateway request duration in milliseconds");

var gatewayErrorsCounter = gatewayMeter.CreateCounter<long>(
    "gateway.errors.total",
    unit: "{errors}",
    description: "Total gateway errors");

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("HardwareStore.Gateway");
    });

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Map ServiceDefaults endpoints
app.MapDefaultEndpoints();

app.UseCors("AllowAll");

// Custom middleware for gateway metrics
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    var path = context.Request.Path.Value ?? "/";
    
    // Track request start
    gatewayRequestsCounter.Add(1,
        new KeyValuePair<string, object?>("path", path),
        new KeyValuePair<string, object?>("method", context.Request.Method));
    
    Extensions.ApiRequestsCounter.Add(1,
        new KeyValuePair<string, object?>("service", "gateway"),
        new KeyValuePair<string, object?>("path", path));
    
    Extensions.IncrementActiveConnections();

    try
    {
        await next();
        
        stopwatch.Stop();
        gatewayLatencyHistogram.Record(stopwatch.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("path", path),
            new KeyValuePair<string, object?>("status_code", context.Response.StatusCode));
        
        Extensions.RequestDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("service", "gateway"),
            new KeyValuePair<string, object?>("path", path),
            new KeyValuePair<string, object?>("status", "success"));

        if (context.Response.StatusCode >= 400)
        {
            gatewayErrorsCounter.Add(1,
                new KeyValuePair<string, object?>("path", path),
                new KeyValuePair<string, object?>("status_code", context.Response.StatusCode));
        }
    }
    catch (Exception)
    {
        stopwatch.Stop();
        gatewayErrorsCounter.Add(1,
            new KeyValuePair<string, object?>("path", path),
            new KeyValuePair<string, object?>("error_type", "exception"));
        
        Extensions.RequestDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("service", "gateway"),
            new KeyValuePair<string, object?>("path", path),
            new KeyValuePair<string, object?>("status", "failure"));
        throw;
    }
    finally
    {
        Extensions.DecrementActiveConnections();
    }
});

// Use YARP Reverse Proxy
app.MapReverseProxy(proxyPipeline =>
{
    // Add CorrelationId to forwarded requests
    proxyPipeline.Use((context, next) =>
    {
        var correlationId = context.Items["CorrelationId"]?.ToString();
        if (!string.IsNullOrEmpty(correlationId))
        {
            context.Request.Headers["X-Correlation-Id"] = correlationId;
        }
        return next();
    });
});

app.Run();

