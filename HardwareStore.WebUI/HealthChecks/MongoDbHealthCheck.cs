using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HardwareStore.WebUI.HealthChecks;

/// <summary>
/// Health check for MongoDB database connectivity
/// </summary>
public class MongoDbHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MongoDbHealthCheck> _logger;

    public MongoDbHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<MongoDbHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            // Get MongoDB context and perform ping
            using var scope = _serviceProvider.CreateScope();
            var mongoContext = scope.ServiceProvider
                .GetService<HardwareStore.Infrastructure.Data.MongoDbContext>();

            if (mongoContext == null)
            {
                return HealthCheckResult.Unhealthy("MongoDB context not configured");
            }

            // Perform a simple database operation to verify connectivity
            var database = mongoContext.Database;
            var command = new MongoDB.Bson.BsonDocument("ping", 1);
            await database.RunCommandAsync<MongoDB.Bson.BsonDocument>(command, cancellationToken: cts.Token);

            _logger.LogDebug("MongoDB health check passed");
            
            return HealthCheckResult.Healthy("MongoDB is responsive", new Dictionary<string, object>
            {
                { "database", database.DatabaseNamespace.DatabaseName }
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MongoDB health check timed out");
            return HealthCheckResult.Degraded("MongoDB health check timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB health check failed");
            return HealthCheckResult.Unhealthy("MongoDB is unavailable", ex);
        }
    }
}
