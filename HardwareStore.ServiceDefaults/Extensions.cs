using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Polly;
using Serilog;
using Serilog.Formatting.Compact;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace Microsoft.Extensions.Hosting;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    // Custom meters for business metrics
    private static readonly Meter HardwareStoreMeter = new("HardwareStore.Metrics", "1.0.0");
    
    // Counters for business operations
    public static readonly Counter<long> ProductsViewedCounter = HardwareStoreMeter.CreateCounter<long>(
        "hardwarestore.products.viewed",
        unit: "{products}",
        description: "Total number of products viewed");
    
    public static readonly Counter<long> OrdersCreatedCounter = HardwareStoreMeter.CreateCounter<long>(
        "hardwarestore.orders.created",
        unit: "{orders}",
        description: "Total number of orders created");
    
    public static readonly Counter<long> ApiRequestsCounter = HardwareStoreMeter.CreateCounter<long>(
        "hardwarestore.api.requests",
        unit: "{requests}",
        description: "Total number of API requests");

    public static readonly Counter<long> CacheHitsCounter = HardwareStoreMeter.CreateCounter<long>(
        "hardwarestore.cache.hits",
        unit: "{hits}",
        description: "Total number of cache hits");

    public static readonly Counter<long> CacheMissesCounter = HardwareStoreMeter.CreateCounter<long>(
        "hardwarestore.cache.misses",
        unit: "{misses}",
        description: "Total number of cache misses");

    // Histograms for latency tracking
    public static readonly Histogram<double> RequestDurationHistogram = HardwareStoreMeter.CreateHistogram<double>(
        "hardwarestore.request.duration",
        unit: "ms",
        description: "Duration of HTTP requests in milliseconds");

    public static readonly Histogram<double> DatabaseOperationDurationHistogram = HardwareStoreMeter.CreateHistogram<double>(
        "hardwarestore.database.operation.duration",
        unit: "ms",
        description: "Duration of database operations in milliseconds");

    // Observable gauges for real-time state
    private static int _activeConnections = 0;
    private static int _cacheSize = 0;
    private static int _queueDepth = 0;

    public static void IncrementActiveConnections() => Interlocked.Increment(ref _activeConnections);
    public static void DecrementActiveConnections() => Interlocked.Decrement(ref _activeConnections);
    public static void SetCacheSize(int size) => _cacheSize = size;
    public static void SetQueueDepth(int depth) => _queueDepth = depth;

    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddSerilogLogging();
        
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default with custom configuration
            http.AddStandardResilienceHandler(ConfigureStandardResilience);

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Register observable gauges
        HardwareStoreMeter.CreateObservableGauge(
            "hardwarestore.connections.active",
            () => _activeConnections,
            unit: "{connections}",
            description: "Number of active connections");

        HardwareStoreMeter.CreateObservableGauge(
            "hardwarestore.cache.size",
            () => _cacheSize,
            unit: "{items}",
            description: "Current cache size in items");

        HardwareStoreMeter.CreateObservableGauge(
            "hardwarestore.queue.depth",
            () => _queueDepth,
            unit: "{items}",
            description: "Current queue depth");

        return builder;
    }

    /// <summary>
    /// Configures Standard Resilience Handler with custom retry, circuit breaker, and timeout policies
    /// </summary>
    private static void ConfigureStandardResilience(Http.Resilience.HttpStandardResilienceOptions options)
    {
        // Configure Retry Policy with exponential backoff and jitter
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;
        options.Retry.OnRetry = args =>
        {
            Log.Warning(
                "Retry attempt {AttemptNumber} for {OperationKey} after {Delay}ms. Exception: {Exception}",
                args.AttemptNumber,
                args.Context.OperationKey,
                args.RetryDelay.TotalMilliseconds,
                args.Outcome.Exception?.Message ?? "No exception");
            return ValueTask.CompletedTask;
        };

        // Configure Circuit Breaker
        options.CircuitBreaker.FailureRatio = 0.5; // Open circuit after 50% failure rate
        options.CircuitBreaker.MinimumThroughput = 10; // Minimum 10 requests to calculate ratio
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
        options.CircuitBreaker.OnOpened = args =>
        {
            Log.Error(
                "Circuit breaker OPENED for {OperationKey}. Break duration: {BreakDuration}s",
                args.Context.OperationKey,
                args.BreakDuration.TotalSeconds);
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnClosed = args =>
        {
            Log.Information(
                "Circuit breaker CLOSED for {OperationKey}",
                args.Context.OperationKey);
            return ValueTask.CompletedTask;
        };
        options.CircuitBreaker.OnHalfOpened = args =>
        {
            Log.Information(
                "Circuit breaker HALF-OPENED for {OperationKey}",
                args.Context.OperationKey);
            return ValueTask.CompletedTask;
        };

        // Configure Total Request Timeout (includes all retries)
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        options.TotalRequestTimeout.OnTimeout = args =>
        {
            Log.Error(
                "Total request timeout exceeded for {OperationKey}",
                args.Context.OperationKey);
            return ValueTask.CompletedTask;
        };

        // Configure Attempt Timeout (per-request timeout)
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.AttemptTimeout.OnTimeout = args =>
        {
            Log.Warning(
                "Attempt timeout exceeded for {OperationKey}",
                args.Context.OperationKey);
            return ValueTask.CompletedTask;
        };
    }

    /// <summary>
    /// Adds resilience handler with custom configuration for specific service criticality
    /// </summary>
    public static IHttpClientBuilder AddCustomResilienceHandler(
        this IHttpClientBuilder builder, 
        string serviceName,
        bool isCritical = false)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            if (isCritical)
            {
                // Critical service - aggressive resilience
                options.Retry.MaxRetryAttempts = 5;
                options.Retry.Delay = TimeSpan.FromMilliseconds(500);
                options.CircuitBreaker.FailureRatio = 0.3; // More sensitive
                options.CircuitBreaker.MinimumThroughput = 5;
            }
            else
            {
                // Non-critical service - relaxed resilience
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromSeconds(2);
                options.CircuitBreaker.FailureRatio = 0.7; // Less sensitive
                options.CircuitBreaker.MinimumThroughput = 20;
            }

            // Add service-specific logging
            options.Retry.OnRetry = args =>
            {
                Log.Warning(
                    "[{ServiceName}] Retry {AttemptNumber} after {Delay}ms",
                    serviceName,
                    args.AttemptNumber,
                    args.RetryDelay.TotalMilliseconds);
                return ValueTask.CompletedTask;
            };

            options.CircuitBreaker.OnOpened = args =>
            {
                Log.Error("[{ServiceName}] Circuit breaker OPENED", serviceName);
                return ValueTask.CompletedTask;
            };
        });

        return builder;
    }

    public static IHostApplicationBuilder AddSerilogLogging(this IHostApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("ServiceName", builder.Environment.ApplicationName)
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateLogger();

        builder.Services.AddSerilog();

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    // Add custom meter for business metrics
                    .AddMeter(HardwareStoreMeter.Name);
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Adds MongoDB health check with proper configuration
    /// </summary>
    public static IHealthChecksBuilder AddMongoDbHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string? name = null,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        TimeSpan? timeout = null)
    {
        return builder.AddMongoDb(
            connectionString,
            name: name ?? "mongodb",
            failureStatus: failureStatus,
            tags: new[] { "database", "mongodb", "ready" },
            timeout: timeout ?? TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Adds Redis health check with graceful degradation
    /// </summary>
    public static IHealthChecksBuilder AddRedisHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string? name = null,
        TimeSpan? timeout = null)
    {
        return builder.AddRedis(
            connectionString,
            name: name ?? "redis-cache",
            failureStatus: HealthStatus.Degraded, // Not Unhealthy since cache is optional
            tags: new[] { "cache", "redis", "ready" },
            timeout: timeout ?? TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// Adds SQL Server health check
    /// </summary>
    public static IHealthChecksBuilder AddSqlServerHealthCheck(
        this IHealthChecksBuilder builder,
        string connectionString,
        string? name = null,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        TimeSpan? timeout = null)
    {
        return builder.AddSqlServer(
            connectionString,
            name: name ?? "sqlserver-database",
            failureStatus: failureStatus,
            tags: new[] { "database", "sql", "ready" },
            timeout: timeout ?? TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Adds HTTP endpoint health check for downstream services
    /// </summary>
    public static IHealthChecksBuilder AddDownstreamServiceHealthCheck(
        this IHealthChecksBuilder builder,
        string uri,
        string name,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        TimeSpan? timeout = null)
    {
        return builder.AddUrlGroup(
            new Uri(uri),
            name: name,
            failureStatus: failureStatus,
            tags: new[] { "downstream", "http", "ready" },
            timeout: timeout ?? TimeSpan.FromSeconds(3));
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Add CorrelationId middleware
        app.UseCorrelationId();
        
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // Combined endpoint - all health checks for development
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = WriteHealthCheckResponse
            });

            // Liveness endpoint - only "live" tagged checks (basic app responsiveness)
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live"),
                AllowCachingResponses = false,
                ResponseWriter = WriteHealthCheckResponse
            });

            // Readiness endpoint - only "ready" tagged checks (all dependencies available)
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                AllowCachingResponses = false,
                ResponseWriter = WriteHealthCheckResponse
            });

            // Legacy endpoint for compatibility
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }

    /// <summary>
    /// Custom health check response writer with detailed JSON output
    /// </summary>
    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                tags = entry.Value.Tags,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data.Count > 0 ? entry.Value.Data : null
            })
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            context.Items["CorrelationId"] = correlationId;
            context.Response.Headers["X-Correlation-Id"] = correlationId;

            // Add to LogContext for Serilog
            using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next();
            }
        });
    }
}

/// <summary>
/// Service for caching with resilience and graceful degradation
/// </summary>
public interface IResilientCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resilient cache service with fallback to database on cache unavailability
/// </summary>
public class ResilientCacheService : IResilientCacheService
{
    private readonly IDistributedCache? _cache;
    private readonly ILogger<ResilientCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ResilientCacheService(
        IDistributedCache? cache,
        ILogger<ResilientCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache == null)
        {
            _logger.LogDebug("Cache not available, skipping cache lookup for {Key}", key);
            Extensions.CacheMissesCounter.Add(1, new KeyValuePair<string, object?>("reason", "unavailable"));
            return default;
        }

        try
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            if (cached != null)
            {
                Extensions.CacheHitsCounter.Add(1, new KeyValuePair<string, object?>("key_prefix", GetKeyPrefix(key)));
                _logger.LogDebug("Cache HIT for {Key}", key);
                return JsonSerializer.Deserialize<T>(cached, _jsonOptions);
            }

            Extensions.CacheMissesCounter.Add(1, new KeyValuePair<string, object?>("reason", "not_found"));
            _logger.LogDebug("Cache MISS for {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache unavailable, falling back to database for {Key}", key);
            Extensions.CacheMissesCounter.Add(1, new KeyValuePair<string, object?>("reason", "error"));
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (_cache == null)
        {
            _logger.LogDebug("Cache not available, skipping cache set for {Key}", key);
            return;
        }

        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
            };

            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await _cache.SetStringAsync(key, json, options, cancellationToken);
            _logger.LogDebug("Cache SET for {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache for {Key}, ignoring", key);
            // Ignore cache write failures - cache is optional
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache == null) return;

        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache REMOVE for {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache for {Key}, ignoring", key);
        }
    }

    private static string GetKeyPrefix(string key)
    {
        var colonIndex = key.IndexOf(':');
        return colonIndex > 0 ? key[..colonIndex] : key;
    }
}

/// <summary>
/// RabbitMQ connection extension methods
/// </summary>
public static class RabbitMQExtensions
{
    /// <summary>
    /// Adds RabbitMQ client connection configured from Aspire service discovery
    /// </summary>
    public static IHostApplicationBuilder AddRabbitMQClient(this IHostApplicationBuilder builder, string connectionName)
    {
        builder.Services.AddSingleton<RabbitMQ.Client.IConnection>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration[$"ConnectionStrings:{connectionName}"];
            
            if (string.IsNullOrEmpty(connectionString))
            {
                Log.Warning("RabbitMQ connection string not found for {ConnectionName}, using default localhost", connectionName);
                connectionString = "amqp://guest:guest@localhost:5672/";
            }
            
            var factory = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = new Uri(connectionString)
            };
            
            // Retry logic for connection
            var maxRetries = 10;
            var delay = TimeSpan.FromSeconds(5);
            
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Log.Information("Attempting to connect to RabbitMQ (attempt {Attempt}/{MaxRetries})...", i + 1, maxRetries);
                    var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                    Log.Information("Successfully connected to RabbitMQ");
                    return connection;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to connect to RabbitMQ (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);
                    if (i == maxRetries - 1)
                    {
                        throw;
                    }
                    Thread.Sleep(delay);
                }
            }
            
            throw new InvalidOperationException("Failed to connect to RabbitMQ after maximum retries");
        });
        
        return builder;
    }
}
