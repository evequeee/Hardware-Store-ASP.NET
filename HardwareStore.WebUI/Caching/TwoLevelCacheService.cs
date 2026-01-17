using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace HardwareStore.WebUI.Caching;

/// <summary>
/// Two-level cache service: L1 (Memory) for ultra-fast access, L2 (Redis) for consistency
/// </summary>
public interface ITwoLevelCacheService
{
    /// <summary>
    /// Get value using L1 -> L2 -> Factory pattern
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, 
        TimeSpan? l1AbsoluteExpiration = null,
        TimeSpan? l2AbsoluteExpiration = null);
    
    /// <summary>
    /// Set value in both L1 and L2 caches
    /// </summary>
    Task SetAsync<T>(string key, T value, 
        TimeSpan? l1AbsoluteExpiration = null,
        TimeSpan? l2AbsoluteExpiration = null);
    
    /// <summary>
    /// Remove value from both L1 and L2 caches
    /// </summary>
    Task RemoveAsync(string key);
    
    /// <summary>
    /// Remove values by pattern from both caches
    /// </summary>
    Task RemoveByPatternAsync(string pattern);
    
    /// <summary>
    /// Get combined cache statistics
    /// </summary>
    TwoLevelCacheStatistics GetStatistics();
}

/// <summary>
/// Statistics for two-level cache
/// </summary>
public class TwoLevelCacheStatistics
{
    public long L1Hits { get; set; }
    public long L2Hits { get; set; }
    public long TotalMisses { get; set; }
    public double L1HitRatio { get; set; }
    public double L2HitRatio { get; set; }
    public double TotalHitRatio { get; set; }
}

/// <summary>
/// Implementation of two-level cache with Memory (L1) and Redis (L2)
/// </summary>
public class TwoLevelCacheService : ITwoLevelCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<TwoLevelCacheService> _logger;
    private readonly HashSet<string> _keys = new();
    private readonly object _keysLock = new();
    private readonly JsonSerializerOptions _jsonOptions;

    // Statistics
    private long _l1Hits;
    private long _l2Hits;
    private long _misses;

    // Default cache durations
    // L1 (Memory) should have shorter TTL for eventual consistency with L2
    private static readonly TimeSpan DefaultL1Expiration = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DefaultL2Expiration = TimeSpan.FromMinutes(5);

    public TwoLevelCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<TwoLevelCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory,
        TimeSpan? l1AbsoluteExpiration = null,
        TimeSpan? l2AbsoluteExpiration = null)
    {
        // L1: Check Memory Cache first
        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            Interlocked.Increment(ref _l1Hits);
            _logger.LogDebug("L1 Cache HIT for key: {CacheKey}", key);
            return cachedValue;
        }

        // L2: Check Redis Cache
        try
        {
            var distributedValue = await _distributedCache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(distributedValue))
            {
                Interlocked.Increment(ref _l2Hits);
                _logger.LogDebug("L2 Cache HIT for key: {CacheKey}", key);

                var value = JsonSerializer.Deserialize<T>(distributedValue, _jsonOptions);
                
                // Populate L1 cache
                if (value != null)
                {
                    SetL1Cache(key, value, l1AbsoluteExpiration ?? DefaultL1Expiration);
                }

                return value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from L2 cache for key: {CacheKey}. Falling back to database.", key);
        }

        // Cache miss - fetch from source
        Interlocked.Increment(ref _misses);
        _logger.LogDebug("Cache MISS for key: {CacheKey}", key);

        var freshValue = await factory();

        if (freshValue != null)
        {
            await SetAsync(key, freshValue, l1AbsoluteExpiration, l2AbsoluteExpiration);
        }

        return freshValue;
    }

    public async Task SetAsync<T>(string key, T value,
        TimeSpan? l1AbsoluteExpiration = null,
        TimeSpan? l2AbsoluteExpiration = null)
    {
        // Set L1 (Memory)
        SetL1Cache(key, value, l1AbsoluteExpiration ?? DefaultL1Expiration);

        // Set L2 (Redis)
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = l2AbsoluteExpiration ?? DefaultL2Expiration
            };

            await _distributedCache.SetStringAsync(key, serializedValue, options);
            _logger.LogDebug("Set L2 cache for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error writing to L2 cache for key: {CacheKey}", key);
        }

        lock (_keysLock)
        {
            _keys.Add(key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        // Remove from L1
        _memoryCache.Remove(key);

        // Remove from L2
        try
        {
            await _distributedCache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing from L2 cache for key: {CacheKey}", key);
        }

        lock (_keysLock)
        {
            _keys.Remove(key);
        }

        _logger.LogInformation("Cache INVALIDATED for key: {CacheKey}", key);
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        List<string> keysToRemove;
        
        lock (_keysLock)
        {
            keysToRemove = _keys.Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        foreach (var key in keysToRemove)
        {
            await RemoveAsync(key);
        }

        _logger.LogInformation("Cache INVALIDATED by pattern: {Pattern}, Keys removed: {Count}", pattern, keysToRemove.Count);
    }

    public TwoLevelCacheStatistics GetStatistics()
    {
        var l1Hits = Interlocked.Read(ref _l1Hits);
        var l2Hits = Interlocked.Read(ref _l2Hits);
        var misses = Interlocked.Read(ref _misses);
        var total = l1Hits + l2Hits + misses;

        return new TwoLevelCacheStatistics
        {
            L1Hits = l1Hits,
            L2Hits = l2Hits,
            TotalMisses = misses,
            L1HitRatio = total > 0 ? (double)l1Hits / total * 100 : 0,
            L2HitRatio = total > 0 ? (double)l2Hits / total * 100 : 0,
            TotalHitRatio = total > 0 ? (double)(l1Hits + l2Hits) / total * 100 : 0
        };
    }

    private void SetL1Cache<T>(string key, T value, TimeSpan expiration)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            Size = 1
        };

        _memoryCache.Set(key, value, options);
        _logger.LogDebug("Set L1 cache for key: {CacheKey}", key);
    }
}
