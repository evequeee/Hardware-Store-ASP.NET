using HardwareStore.Domain.Interfaces;

namespace HardwareStore.WebUI.Caching;

/// <summary>
/// Background service for cache warming and periodic refresh
/// </summary>
public class CacheWarmingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheWarmingService> _logger;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(4); // Refresh before cache expires

    public CacheWarmingService(IServiceProvider serviceProvider, ILogger<CacheWarmingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache Warming Service started");

        // Initial cache warming on startup
        await WarmCacheAsync(stoppingToken);

        // Periodic refresh
        using var timer = new PeriodicTimer(_refreshInterval);
        
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await WarmCacheAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache warming");
            }
        }
    }

    private async Task WarmCacheAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting cache warming operation");
        
        using var scope = _serviceProvider.CreateScope();
        var twoLevelCache = scope.ServiceProvider.GetRequiredService<ITwoLevelCacheService>();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        try
        {
            // Warm products cache
            var products = await productRepository.GetAllAsync(cancellationToken);
            
            if (products.Any())
            {
                // Cache all products list
                await twoLevelCache.SetAsync(
                    CacheKeys.AllProducts,
                    products.ToList(),
                    CacheKeys.Durations.Short,
                    CacheKeys.Durations.Medium);

                // Cache individual products for quick access
                foreach (var product in products)
                {
                    var productKey = CacheKeys.GetProductByIdKey(product.Id);
                    await twoLevelCache.SetAsync(
                        productKey,
                        product,
                        CacheKeys.Durations.Short,
                        CacheKeys.Durations.Medium);
                }

                _logger.LogInformation("Cache warming completed. Cached {ProductCount} products", products.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to warm cache for products");
        }
    }
}
