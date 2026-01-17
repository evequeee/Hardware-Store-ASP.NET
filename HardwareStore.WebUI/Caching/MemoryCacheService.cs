using Microsoft.Extensions.Caching.Memory;

namespace HardwareStore.WebUI.Caching;

/// <summary>
/// Interface for cache service with structured logging and metrics
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get value from cache with cache-aside pattern
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);
    
    /// <summary>
    /// Get value from cache
    /// </summary>
    T? Get<T>(string key);
    
    /// <summary>
    /// Set value in cache
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);
    
    /// <summary>
    /// Remove value from cache
    /// </summary>
    void Remove(string key);
    
    /// <summary>
    /// Remove all entries matching a pattern
    /// </summary>
    void RemoveByPattern(string pattern);
    
    /// <summary>
    /// Get cache statistics
    /// </summary>
    CacheStatistics GetStatistics();
}

/// <summary>
/// Cache statistics for monitoring
/// </summary>
public class CacheStatistics
{
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public double HitRatio => TotalHits + TotalMisses > 0 ? (double)TotalHits / (TotalHits + TotalMisses) * 100 : 0;
    public int CurrentEntryCount { get; set; }
    public long TotalEvictions { get; set; }
}

/// <summary>
/// Memory cache service with logging, metrics and eviction policies
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly HashSet<string> _keys = new();
    private readonly object _keysLock = new();
    
    // Cache statistics
    private long _hits;
    private long _misses;
    private long _evictions;

    // Default cache options
    private static readonly TimeSpan DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(2);
    
    // Memory limits
    private const long SizeLimit = 1024; // Maximum number of entries

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, 
        TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
    {
        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            Interlocked.Increment(ref _hits);
            _logger.LogDebug("Cache HIT for key: {CacheKey}, Type: {Type}", key, typeof(T).Name);
            return cachedValue;
        }

        Interlocked.Increment(ref _misses);
        _logger.LogDebug("Cache MISS for key: {CacheKey}, Type: {Type}", key, typeof(T).Name);

        // Fetch from source
        var value = await factory();

        if (value != null)
        {
            Set(key, value, absoluteExpiration, slidingExpiration);
        }

        return value;
    }

    public T? Get<T>(string key)
    {
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            Interlocked.Increment(ref _hits);
            _logger.LogDebug("Cache HIT for key: {CacheKey}", key);
            return value;
        }

        Interlocked.Increment(ref _misses);
        _logger.LogDebug("Cache MISS for key: {CacheKey}", key);
        return default;
    }

    public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration ?? DefaultAbsoluteExpiration,
            SlidingExpiration = slidingExpiration ?? DefaultSlidingExpiration,
            Size = 1,
            Priority = CacheItemPriority.Normal
        };

        // Register callback for eviction
        options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
        {
            Interlocked.Increment(ref _evictions);
            _logger.LogDebug("Cache entry evicted. Key: {CacheKey}, Reason: {Reason}", evictedKey, reason);
            
            lock (_keysLock)
            {
                _keys.Remove(evictedKey.ToString()!);
            }
        });

        _memoryCache.Set(key, value, options);
        
        lock (_keysLock)
        {
            _keys.Add(key);
        }

        _logger.LogDebug("Cache SET for key: {CacheKey}, AbsoluteExpiration: {AbsoluteExpiration}, SlidingExpiration: {SlidingExpiration}", 
            key, absoluteExpiration ?? DefaultAbsoluteExpiration, slidingExpiration ?? DefaultSlidingExpiration);
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
        
        lock (_keysLock)
        {
            _keys.Remove(key);
        }

        _logger.LogInformation("Cache INVALIDATED for key: {CacheKey}", key);
    }

    public void RemoveByPattern(string pattern)
    {
        List<string> keysToRemove;
        
        lock (_keysLock)
        {
            keysToRemove = _keys.Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        foreach (var key in keysToRemove)
        {
            Remove(key);
        }

        _logger.LogInformation("Cache INVALIDATED by pattern: {Pattern}, Keys removed: {Count}", pattern, keysToRemove.Count);
    }

    public CacheStatistics GetStatistics()
    {
        int currentCount;
        lock (_keysLock)
        {
            currentCount = _keys.Count;
        }

        return new CacheStatistics
        {
            TotalHits = Interlocked.Read(ref _hits),
            TotalMisses = Interlocked.Read(ref _misses),
            TotalEvictions = Interlocked.Read(ref _evictions),
            CurrentEntryCount = currentCount
        };
    }
}
