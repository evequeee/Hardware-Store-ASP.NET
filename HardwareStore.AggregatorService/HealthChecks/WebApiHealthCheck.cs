using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HardwareStore.AggregatorService.HealthChecks;

/// <summary>
/// Health check for WebAPI downstream service
/// </summary>
public class WebApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebApiHealthCheck> _logger;

    public WebApiHealthCheck(
        IHttpClientFactory httpClientFactory,
        ILogger<WebApiHealthCheck> logger)
    {
        _httpClient = httpClientFactory.CreateClient("WebApiHealthCheck");
        _httpClient.BaseAddress = new Uri("http://webapi");
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            var response = await _httpClient.GetAsync("/health", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("WebAPI health check passed");
                return HealthCheckResult.Healthy("WebAPI is responsive", new Dictionary<string, object>
                {
                    { "status_code", (int)response.StatusCode },
                    { "endpoint", "http://webapi/health" }
                });
            }

            _logger.LogWarning("WebAPI returned non-success status: {StatusCode}", response.StatusCode);
            return HealthCheckResult.Degraded($"WebAPI returned status code {response.StatusCode}");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("WebAPI health check timed out");
            return HealthCheckResult.Unhealthy("WebAPI health check timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "WebAPI is unavailable");
            return HealthCheckResult.Unhealthy("WebAPI is unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebAPI health check failed");
            return HealthCheckResult.Unhealthy("WebAPI health check failed", ex);
        }
    }
}
