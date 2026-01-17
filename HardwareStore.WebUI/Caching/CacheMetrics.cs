using System.Diagnostics.Metrics;

namespace HardwareStore.WebUI.Caching;

/// <summary>
/// Custom metrics for cache monitoring
/// </summary>
public class CacheMetrics
{
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _cacheEvictions;
    private readonly Histogram<double> _cacheOperationDuration;
    private readonly Counter<long> _cacheInvalidations;
    private readonly ObservableGauge<int> _cacheSize;

    private int _currentCacheSize;

    public CacheMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("HardwareStore.Cache");

        _cacheHits = meter.CreateCounter<long>(
            "hardwarestore.cache.hits",
            "hits",
            "Number of cache hits");

        _cacheMisses = meter.CreateCounter<long>(
            "hardwarestore.cache.misses",
            "misses",
            "Number of cache misses");

        _cacheEvictions = meter.CreateCounter<long>(
            "hardwarestore.cache.evictions",
            "evictions",
            "Number of cache evictions");

        _cacheOperationDuration = meter.CreateHistogram<double>(
            "hardwarestore.cache.operation.duration",
            "ms",
            "Duration of cache operations in milliseconds");

        _cacheInvalidations = meter.CreateCounter<long>(
            "hardwarestore.cache.invalidations",
            "invalidations",
            "Number of cache invalidations");

        _cacheSize = meter.CreateObservableGauge<int>(
            "hardwarestore.cache.size",
            () => _currentCacheSize,
            "entries",
            "Current number of entries in cache");
    }

    /// <summary>
    /// Record a cache hit
    /// </summary>
    public void RecordHit(string cacheType, string key)
    {
        _cacheHits.Add(1, 
            new KeyValuePair<string, object?>("cache.type", cacheType),
            new KeyValuePair<string, object?>("cache.key_prefix", GetKeyPrefix(key)));
    }

    /// <summary>
    /// Record a cache miss
    /// </summary>
    public void RecordMiss(string cacheType, string key)
    {
        _cacheMisses.Add(1,
            new KeyValuePair<string, object?>("cache.type", cacheType),
            new KeyValuePair<string, object?>("cache.key_prefix", GetKeyPrefix(key)));
    }

    /// <summary>
    /// Record a cache eviction
    /// </summary>
    public void RecordEviction(string cacheType, string reason)
    {
        _cacheEvictions.Add(1,
            new KeyValuePair<string, object?>("cache.type", cacheType),
            new KeyValuePair<string, object?>("eviction.reason", reason));
    }

    /// <summary>
    /// Record cache operation duration
    /// </summary>
    public void RecordOperationDuration(string operation, string cacheType, double durationMs)
    {
        _cacheOperationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("cache.type", cacheType));
    }

    /// <summary>
    /// Record a cache invalidation
    /// </summary>
    public void RecordInvalidation(string cacheType, int keysAffected)
    {
        _cacheInvalidations.Add(keysAffected,
            new KeyValuePair<string, object?>("cache.type", cacheType));
    }

    /// <summary>
    /// Update cache size
    /// </summary>
    public void UpdateCacheSize(int size)
    {
        Interlocked.Exchange(ref _currentCacheSize, size);
    }

    /// <summary>
    /// Get prefix from cache key for grouping metrics
    /// </summary>
    private static string GetKeyPrefix(string key)
    {
        var colonIndex = key.IndexOf(':');
        return colonIndex > 0 ? key[..colonIndex] : key;
    }
}
