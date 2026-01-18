using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HardwareStore.Infrastructure.Messaging;

/// <summary>
/// Interface for idempotency checking to prevent duplicate message processing
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Check if message was already processed
    /// </summary>
    Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mark message as processed
    /// </summary>
    Task MarkAsProcessedAsync(Guid eventId, string eventType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Try to acquire a lock for processing (returns false if already being processed)
    /// </summary>
    Task<bool> TryAcquireLockAsync(Guid eventId, TimeSpan lockDuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Release the processing lock
    /// </summary>
    Task ReleaseLockAsync(Guid eventId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Redis-based idempotency service implementation
/// </summary>
public class RedisIdempotencyService : IIdempotencyService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisIdempotencyService> _logger;
    
    private const string ProcessedKeyPrefix = "event:processed:";
    private const string LockKeyPrefix = "event:lock:";
    private static readonly TimeSpan DefaultProcessedTtl = TimeSpan.FromDays(7);

    public RedisIdempotencyService(
        IDistributedCache cache,
        ILogger<RedisIdempotencyService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var key = $"{ProcessedKeyPrefix}{eventId}";
        var value = await _cache.GetStringAsync(key, cancellationToken);
        
        if (value is not null)
        {
            _logger.LogDebug("Event {EventId} was already processed", eventId);
            return true;
        }
        
        return false;
    }

    public async Task MarkAsProcessedAsync(Guid eventId, string eventType, CancellationToken cancellationToken = default)
    {
        var key = $"{ProcessedKeyPrefix}{eventId}";
        var value = JsonSerializer.Serialize(new ProcessedEventRecord(
            eventId,
            eventType,
            DateTime.UtcNow
        ));
        
        await _cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultProcessedTtl
        }, cancellationToken);
        
        _logger.LogDebug("Marked event {EventId} ({EventType}) as processed", eventId, eventType);
    }

    public async Task<bool> TryAcquireLockAsync(Guid eventId, TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        var key = $"{LockKeyPrefix}{eventId}";
        
        // Check if lock already exists
        var existingLock = await _cache.GetStringAsync(key, cancellationToken);
        if (existingLock is not null)
        {
            _logger.LogDebug("Lock already exists for event {EventId}", eventId);
            return false;
        }
        
        // Try to set lock (note: this is not atomic in IDistributedCache, 
        // in production you'd use Redis SET NX directly)
        await _cache.SetStringAsync(key, Environment.MachineName, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lockDuration
        }, cancellationToken);
        
        _logger.LogDebug("Acquired lock for event {EventId}", eventId);
        return true;
    }

    public async Task ReleaseLockAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var key = $"{LockKeyPrefix}{eventId}";
        await _cache.RemoveAsync(key, cancellationToken);
        _logger.LogDebug("Released lock for event {EventId}", eventId);
    }
    
    private record ProcessedEventRecord(Guid EventId, string EventType, DateTime ProcessedAt);
}

/// <summary>
/// In-memory idempotency service for development/testing
/// </summary>
public class InMemoryIdempotencyService : IIdempotencyService
{
    private readonly HashSet<Guid> _processedEvents = new();
    private readonly HashSet<Guid> _locks = new();
    private readonly object _lockObj = new();
    private readonly ILogger<InMemoryIdempotencyService> _logger;

    public InMemoryIdempotencyService(ILogger<InMemoryIdempotencyService> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        lock (_lockObj)
        {
            return Task.FromResult(_processedEvents.Contains(eventId));
        }
    }

    public Task MarkAsProcessedAsync(Guid eventId, string eventType, CancellationToken cancellationToken = default)
    {
        lock (_lockObj)
        {
            _processedEvents.Add(eventId);
            _logger.LogDebug("Marked event {EventId} ({EventType}) as processed", eventId, eventType);
        }
        return Task.CompletedTask;
    }

    public Task<bool> TryAcquireLockAsync(Guid eventId, TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        lock (_lockObj)
        {
            if (_locks.Contains(eventId))
            {
                return Task.FromResult(false);
            }
            
            _locks.Add(eventId);
            return Task.FromResult(true);
        }
    }

    public Task ReleaseLockAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        lock (_lockObj)
        {
            _locks.Remove(eventId);
        }
        return Task.CompletedTask;
    }
}
