using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace HardwareStore.AggregatorService.Caching;

/// <summary>
/// Service for caching aggregated results in Redis
/// </summary>
public interface IAggregatorCacheService
{
    /// <summary>
    /// Get or create cached value
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    
    /// <summary>
    /// Set value in cache
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    
    /// <summary>
    /// Remove value from cache
    /// </summary>
    Task RemoveAsync(string key);
    
    /// <summary>
    /// Get cache statistics
    /// </summary>
    AggregatorCacheStatistics GetStatistics();
}

/// <summary>
/// Cache statistics
/// </summary>
public class AggregatorCacheStatistics
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public double HitRatio => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) * 100 : 0;
}

/// <summary>
/// Implementation of aggregator cache using Redis
/// </summary>
public class AggregatorCacheService : IAggregatorCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<AggregatorCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Cache TTL - shorter for aggregated data (30-60 seconds)
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromSeconds(30);

    // Statistics
    private long _hits;
    private long _misses;

    public AggregatorCacheService(IDistributedCache cache, ILogger<AggregatorCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key);
            
            if (!string.IsNullOrEmpty(cachedValue))
            {
                Interlocked.Increment(ref _hits);
                _logger.LogDebug("Cache HIT for aggregated key: {CacheKey}", key);
                return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from Redis cache for key: {CacheKey}", key);
        }

        Interlocked.Increment(ref _misses);
        _logger.LogDebug("Cache MISS for aggregated key: {CacheKey}", key);

        var value = await factory();

        if (value != null)
        {
            await SetAsync(key, value, expiration);
        }

        return value;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
            };

            await _cache.SetStringAsync(key, serializedValue, options);
            _logger.LogDebug("Set aggregated cache for key: {CacheKey}, TTL: {TTL}", key, expiration ?? DefaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error writing to Redis cache for key: {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogInformation("Removed aggregated cache for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing from Redis cache for key: {CacheKey}", key);
        }
    }

    public AggregatorCacheStatistics GetStatistics()
    {
        return new AggregatorCacheStatistics
        {
            Hits = Interlocked.Read(ref _hits),
            Misses = Interlocked.Read(ref _misses)
        };
    }
}

/// <summary>
/// Cache key constants for aggregator
/// </summary>
public static class AggregatorCacheKeys
{
    public const string Dashboard = "aggregator:dashboard";
    public const string EnrichedProduct = "aggregator:product:{0}";

    public static string GetEnrichedProductKey(string id) => string.Format(EnrichedProduct, id);
}
