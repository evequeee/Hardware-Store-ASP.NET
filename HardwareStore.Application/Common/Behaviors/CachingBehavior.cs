using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HardwareStore.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior for caching query results
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only cache queries (not commands)
        var requestName = typeof(TRequest).Name;
        if (!requestName.EndsWith("Query"))
        {
            return await next();
        }

        // Generate cache key from request
        var cacheKey = $"{requestName}:{request.GetHashCode()}";

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse))
        {
            _logger.LogDebug("Cache HIT for {RequestName} with key {CacheKey}", requestName, cacheKey);
            return cachedResponse!;
        }

        _logger.LogDebug("Cache MISS for {RequestName} with key {CacheKey}", requestName, cacheKey);

        // Execute handler
        var response = await next();

        // Cache response
        if (response != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                SlidingExpiration = TimeSpan.FromMinutes(2),
                Size = 1
            };

            _cache.Set(cacheKey, response, cacheOptions);
            _logger.LogDebug("Cached response for {RequestName} with key {CacheKey}", requestName, cacheKey);
        }

        return response;
    }
}
